using System.Text.RegularExpressions;
using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: publicacao (ADR-0055, ADR-0115). The workflow is read as text; the scripts run with stand-ins for az, curl,
// npm and terraform, which write down how they were called.
public sealed partial class DeploymentTests : IDisposable
{
    private readonly string _fakes = Directory.CreateTempSubdirectory("fisco-deploy-").FullName;

    public void Dispose() => Directory.Delete(_fakes, recursive: true);

    private static string[] WorkflowLines() => File.ReadAllLines(RepositoryFiles.PathOf(".github", "workflows", "deploy-dev.yml"));

    [Fact(DisplayName = "Publicação a cada merge")]
    public void Workflow_PublishesTheDevelopmentEnvironmentAfterEachMergeToMain()
    {
        var lines = WorkflowLines();
        var workflow = string.Join('\n', lines);

        workflow.Should().Contain("on:\n  push:\n    branches: [main]\n  workflow_dispatch:\n");
        workflow.Should().Contain("    environment: dev\n").And.Contain("      ENVIRONMENT: dev\n");
        lines.Select(line => ScriptStep().Match(line)).Where(match => match.Success).Select(match => match.Groups["script"].Value)
            .Should().Equal(["publish", "deploy", "plan"], "the package is built, published and then the infrastructure is compared");
        workflow.Should().Contain("concurrency:\n  group: deploy-dev\n  cancel-in-progress: false\n", "one publication at a time, none cut in the middle");
    }

    [Fact(DisplayName = "Pipeline sem segredos")]
    public void Workflow_ReachesAzureByFederation_WithoutSecrets()
    {
        var lines = WorkflowLines();
        var workflow = string.Join('\n', lines);

        workflow.Should().NotContain("secrets.").And.NotContain("secrets:");
        workflow.Should().Contain("\npermissions:\n  contents: read\n  id-token: write\n\n");
        Regex.Count(workflow, "permissions:").Should().Be(1);
        lines.Where(line => line.TrimStart().StartsWith("- uses:", StringComparison.Ordinal))
            .Should().AllSatisfy(line => PinnedAction().IsMatch(line).Should().BeTrue($"\"{line.Trim()}\" must be pinned to a commit"));
        lines.Should().Contain("          persist-credentials: false").And.Contain("      ARM_USE_OIDC: \"true\"");
        lines.Where(line => line.TrimStart().StartsWith("run:", StringComparison.Ordinal))
            .Should().AllSatisfy(line => ScriptStep().IsMatch(line).Should().BeTrue("the logic lives in scripts/cd"));
    }

    [Fact(DisplayName = "Demonstração depois da publicação")]
    public void Deploy_PublishesWaitsForHealth_AndLoadsTheDemonstration()
    {
        Shell.SkipWithoutBash();
        Fake("az", "echo \"az $*\" >> \"$FAKES/calls\"");
        Fake("curl", "echo \"curl ${@: -1}\" >> \"$FAKES/calls\"; printf 200");
        Fake("npm", "echo \"npm $* ($(basename \"$PWD\"))\" >> \"$FAKES/calls\"");

        var result = Run("deploy");

        result.ExitCode.Should().Be(0, result.Errors);
        Calls().Should().Equal(
            "az webapp deploy --resource-group rg-fiscoprojects-mlr-dev --name app-fiscoprojects-mlr-dev --src-path artifacts/app.zip --type zip --output none",
            "curl https://app-fiscoprojects-mlr-dev.azurewebsites.net/health",
            "npm run demo:data -- https://app-fiscoprojects-mlr-dev.azurewebsites.net (web)");
    }

    [Fact(DisplayName = "Infraestrutura diferente do código")]
    public void Plan_WithDifferences_WarnsWithoutFailing()
    {
        Shell.SkipWithoutBash();
        FakeTerraform(planExit: 2, planOutput: "  # module.app.azurerm_linux_web_app.main will be updated in-place\nPlan: 0 to add, 1 to change, 0 to destroy.");

        var result = Run("plan");

        result.ExitCode.Should().Be(0, result.Errors);
        result.Output.Should().Contain("::warning::A infraestrutura de dev está diferente do código");
        File.ReadAllText(Path.Combine(_fakes, "summary")).Should().Contain("Plan: 0 to add, 1 to change, 0 to destroy.");
        Calls().Should().Contain(call => call.StartsWith("terraform -chdir=infra/terraform plan -input=false -no-color -lock-timeout=5m -detailed-exitcode -var-file=environments/dev.tfvars", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Infraestrutura igual ao código")]
    public void Plan_WithoutDifferences_Passes_AndAFailureFails()
    {
        Shell.SkipWithoutBash();
        FakeTerraform(planExit: 0, planOutput: "No changes.");
        Run("plan").Output.Should().Contain("Infrastructure of dev matches the code.");

        FakeTerraform(planExit: 1, planOutput: "Error: unauthorized");
        var failed = Run("plan");
        failed.ExitCode.Should().Be(1);
        failed.Errors.Should().Contain("Error: unauthorized");
    }

    private void FakeTerraform(int planExit, string planOutput)
    {
        File.WriteAllText(Path.Combine(_fakes, "plan-output"), planOutput + "\n");
        Fake("terraform", $$"""
            echo "terraform $*" >> "$FAKES/calls"
            case "$*" in
              "version -json") echo '{"terraform_version": "1.16.5"}' ;;
              *" plan "*) cat "$FAKES/plan-output"; exit {{planExit}} ;;
            esac
            """);
    }

    private Shell.Result Run(string script)
    {
        var bin = Path.Combine(_fakes, "bin");
        return Shell.Run(RepositoryFiles.PathOf(), new Dictionary<string, string>
        {
            ["PATH"] = $"{bin}:{Environment.GetEnvironmentVariable("PATH")}",
            ["FAKES"] = _fakes,
            ["ENVIRONMENT"] = "dev",
            ["GITHUB_ACTIONS"] = "true",
            ["GITHUB_STEP_SUMMARY"] = Path.Combine(_fakes, "summary"),
        }, "bash", Path.Combine("scripts", "cd", script + ".sh"));
    }

    private string[] Calls()
    {
        var calls = Path.Combine(_fakes, "calls");
        return File.Exists(calls) ? File.ReadAllLines(calls) : [];
    }

    private void Fake(string name, string body)
    {
        var bin = Directory.CreateDirectory(Path.Combine(_fakes, "bin")).FullName;
        var path = Path.Combine(bin, name);
        File.WriteAllText(path, $"#!/usr/bin/env bash\n{body}\n");
        Shell.Run(bin, null, "chmod", "+x", path);
    }

    [GeneratedRegex(@"^\s+run: bash scripts/cd/(?<script>[a-z-]+)\.sh$")]
    private static partial Regex ScriptStep();

    [GeneratedRegex(@"^\s+- uses: [\w.-]+/[\w.-]+@[0-9a-f]{40} # v\d+(\.\d+)*$")]
    private static partial Regex PinnedAction();
}
