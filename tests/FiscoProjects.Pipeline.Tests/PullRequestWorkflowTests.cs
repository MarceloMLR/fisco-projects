using System.Text.RegularExpressions;

namespace FiscoProjects.Pipeline.Tests;

// Spec: integracao-continua. The workflow keeps a simple, fixed layout so these checks can read it as text.
public sealed partial class PullRequestWorkflowTests
{
    private static readonly string[] Checks = ["build", "test", "format", "frontend", "specs", "spelling"];

    [Fact(DisplayName = "Pull request para a main")]
    public void Workflow_RunsOnPullRequestsToMain()
    {
        var workflow = string.Join('\n', Repository.WorkflowLines());

        workflow.Should().Contain("on:\n  pull_request:\n    branches: [main]\n");
        workflow.Should().NotContain("pull_request_target", "pull requests from forks must not get a privileged token");
    }

    [Fact(DisplayName = "Todas as verificações no pull request")]
    public void Workflow_RunsEveryCheckAsItsOwnStep_InOrder()
    {
        var lines = Repository.WorkflowLines();

        lines.Should().Contain("  verify:").And.Contain("    name: verify", "the \"Protect main\" rule requires this check name");
        ScriptSteps(lines).Should().Equal(Checks);
    }

    [Fact(DisplayName = "Etapas do workflow")]
    public void Workflow_OnlyCallsExistingScripts()
    {
        var runLines = Repository.WorkflowLines().Where(line => line.TrimStart().StartsWith("run:", StringComparison.Ordinal));

        foreach (var line in runLines)
        {
            var match = ScriptStep().Match(line);
            match.Success.Should().BeTrue($"every step that runs commands calls a script of scripts/ci, but found \"{line.Trim()}\"");
            File.Exists(Path.Combine(Repository.ScriptsFolder, match.Groups["script"].Value + ".sh")).Should().BeTrue();
        }
    }

    [Fact(DisplayName = "Sem segredos")]
    public void Workflow_UsesNoSecrets_AndOnlyReadsContents()
    {
        var workflow = string.Join('\n', Repository.WorkflowLines());

        workflow.Should().NotContain("secrets.").And.NotContain("secrets:");
        workflow.Should().Contain("\npermissions:\n  contents: read\n\n");
        Regex.Count(workflow, @"permissions:").Should().Be(1, "no job may ask for more permissions");
        workflow.Should().Contain("persist-credentials: false");
    }

    [Fact(DisplayName = "Versões fixadas")]
    public void Workflow_PinsActionsRunnerAndToolVersions()
    {
        var lines = Repository.WorkflowLines();
        var actions = lines.Where(line => line.TrimStart().StartsWith("- uses:", StringComparison.Ordinal)).ToList();

        actions.Should().NotBeEmpty();
        actions.Should().AllSatisfy(line => PinnedAction().IsMatch(line).Should().BeTrue($"\"{line.Trim()}\" must be pinned to a commit"));
        lines.Should().Contain("    runs-on: ubuntu-24.04");
        lines.Should().Contain("          global-json-file: global.json").And.Contain("          node-version-file: .nvmrc");
        Repository.Read(".nvmrc").Trim().Should().MatchRegex(@"^\d+$");
    }

    private static IEnumerable<string> ScriptSteps(IEnumerable<string> lines) =>
        lines.Select(line => ScriptStep().Match(line)).Where(match => match.Success).Select(match => match.Groups["script"].Value);

    [GeneratedRegex(@"^\s+run: bash scripts/ci/(?<script>[a-z-]+)\.sh$")]
    private static partial Regex ScriptStep();

    [GeneratedRegex(@"^\s+- uses: [\w.-]+/[\w.-]+@[0-9a-f]{40} # v\d+\.\d+\.\d+$")]
    private static partial Regex PinnedAction();
}
