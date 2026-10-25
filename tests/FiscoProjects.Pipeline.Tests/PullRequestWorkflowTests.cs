using System.Text.RegularExpressions;

namespace FiscoProjects.Pipeline.Tests;

// Spec: integracao-continua. The workflow keeps a simple, fixed layout so these checks can read it as text: each job
// starts with "  <name>:" under "jobs:", and the scripts are called by "run: bash scripts/ci/<script>.sh".
public sealed partial class PullRequestWorkflowTests
{
    /// <summary>The jobs, in order, and the scripts each one calls (ADR-0090).</summary>
    private static readonly (string Job, string[] Scripts)[] Jobs =
    [
        ("changes", ["changes"]),
        ("backend", ["build", "licenses", "test"]),
        ("format", ["format"]),
        ("frontend", ["frontend"]),
        ("infra", ["infra"]),
        ("specs", ["specs"]),
        ("verify", ["verdict"]),
    ];

    private const string OnlyWithCode = "    if: ${{ needs.changes.outputs.code == 'true' }}";

    [Fact(DisplayName = "Pull request para a main")]
    public void Workflow_RunsOnPullRequestsToMain()
    {
        var workflow = string.Join('\n', Repository.WorkflowLines());

        workflow.Should().Contain("on:\n  pull_request:\n    branches: [main]\n");
        workflow.Should().NotContain("pull_request_target", "pull requests from forks must not get a privileged token");
    }

    [Fact(DisplayName = "Todas as verificações no pull request")]
    public void Workflow_RunsTheChecksInParallelJobs_AndVerifyWaitsForThemAll()
    {
        var jobs = JobsOf(Repository.WorkflowLines());

        jobs.Keys.Should().Equal(Jobs.Select(job => job.Job));
        foreach (var (job, scripts) in Jobs)
        {
            ScriptSteps(jobs[job]).Should().Equal(scripts, $"the job {job} calls these scripts");
        }

        // The code parts wait only for changes; the specs wait for nothing; verify waits for everything.
        foreach (var job in new[] { "backend", "format", "frontend", "infra" })
        {
            jobs[job].Should().Contain("    needs: changes").And.Contain(OnlyWithCode);
        }

        jobs["specs"].Should().NotContain(line => line.StartsWith("    needs:", StringComparison.Ordinal) || line.StartsWith("    if:", StringComparison.Ordinal));
        jobs["verify"].Should().Contain("    name: verify", "the \"Protect main\" rule requires this check name")
            .And.Contain("    needs: [changes, backend, format, frontend, infra, specs]")
            .And.Contain("    if: ${{ !cancelled() }}");
        // The tests run only after a successful build.
        jobs["backend"].Should().Contain("        if: ${{ !cancelled() && steps.build.outcome == 'success' }}");
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
    public void Workflow_UsesNoSecrets_AndOnlyReads()
    {
        var lines = Repository.WorkflowLines();
        var workflow = string.Join('\n', lines);

        workflow.Should().NotContain("secrets.").And.NotContain("secrets:");
        workflow.Should().Contain("\npermissions:\n  contents: read\n  checks: read\n  pull-requests: read\n\n");
        Regex.Count(workflow, @"permissions:").Should().Be(1, "no job may ask for more permissions");
        lines.Count(line => line.Contains("- uses: actions/checkout@", StringComparison.Ordinal))
            .Should().Be(lines.Count(line => line.Trim() == "persist-credentials: false"), "no checkout keeps the token");
        // The token of the job reaches only changes.sh, to read the checks of the pull request.
        Regex.Count(workflow, @"github\.token").Should().Be(1);
        JobsOf(lines)["changes"].Should().Contain("          GH_TOKEN: ${{ github.token }}");
    }

    [Fact(DisplayName = "Versões fixadas")]
    public void Workflow_PinsActionsRunnerAndToolVersions()
    {
        var lines = Repository.WorkflowLines();
        var actions = lines.Where(line => line.TrimStart().StartsWith("- uses:", StringComparison.Ordinal)).ToList();

        actions.Should().NotBeEmpty();
        actions.Should().AllSatisfy(line => PinnedAction().IsMatch(line).Should().BeTrue($"\"{line.Trim()}\" must be pinned to a commit"));
        lines.Where(line => line.TrimStart().StartsWith("runs-on:", StringComparison.Ordinal))
            .Should().HaveCount(Jobs.Length).And.AllBe("    runs-on: ubuntu-24.04");
        lines.Should().Contain("          global-json-file: global.json").And.Contain("          node-version-file: .nvmrc");
        Repository.Read(".nvmrc").Trim().Should().MatchRegex(@"^\d+$");
    }

    [Fact(DisplayName = "Pacotes em cache")]
    public void Workflow_KeepsThePackagesInCache()
    {
        var jobs = JobsOf(Repository.WorkflowLines());

        foreach (var job in new[] { "backend", "format" })
        {
            jobs[job].Should().Contain(line => line.StartsWith("      - uses: actions/cache@", StringComparison.Ordinal))
                .And.Contain("          path: ~/.nuget/packages")
                .And.Contain("          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/Directory.*.props', 'global.json') }}");
        }

        jobs["frontend"].Should().Contain("          cache: npm").And.Contain("          cache-dependency-path: web/package-lock.json");
        jobs["specs"].Should().Contain("          cache: npm").And.Contain("          cache-dependency-path: scripts/ci/package-lock.json");
    }

    /// <summary>The lines of each job, by name, in the order of the workflow.</summary>
    private static Dictionary<string, List<string>> JobsOf(IEnumerable<string> lines)
    {
        var jobs = new Dictionary<string, List<string>>();
        List<string>? current = null;
        var inJobs = false;
        foreach (var line in lines)
        {
            if (line == "jobs:")
            {
                inJobs = true;
                continue;
            }

            var start = JobStart().Match(line);
            if (inJobs && start.Success)
            {
                current = [];
                jobs.Add(start.Groups["job"].Value, current);
            }
            else
            {
                current?.Add(line);
            }
        }

        return jobs;
    }

    private static IEnumerable<string> ScriptSteps(IEnumerable<string> lines) =>
        lines.Select(line => ScriptStep().Match(line)).Where(match => match.Success).Select(match => match.Groups["script"].Value);

    [GeneratedRegex(@"^  (?<job>[a-z-]+):$")]
    private static partial Regex JobStart();

    [GeneratedRegex(@"^\s+run: bash scripts/ci/(?<script>[a-z-]+)\.sh$")]
    private static partial Regex ScriptStep();

    [GeneratedRegex(@"^\s+- uses: [\w.-]+/[\w.-]+@[0-9a-f]{40} # v\d+\.\d+\.\d+$")]
    private static partial Regex PinnedAction();
}
