using System.Text.RegularExpressions;
using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: publicacao (ADR-0055, ADR-0115, ADR-0116, ADR-0125). The workflows are read as text; the scripts run with
// stand-ins for az, curl, dotnet, npm and terraform, which write down how they were called.
public sealed partial class DeploymentTests : IDisposable
{
    private readonly string _fakes = Directory.CreateTempSubdirectory("fisco-deploy-").FullName;

    public void Dispose() => Directory.Delete(_fakes, recursive: true);

    private static string[] WorkflowLines(string workflow = "deploy-dev") =>
        File.ReadAllLines(RepositoryFiles.PathOf(".github", "workflows", workflow + ".yml"));

    private static string[] ScriptsOf(string[] lines) =>
        [.. lines.Select(line => ScriptStep().Match(line)).Where(match => match.Success).Select(match => match.Groups["script"].Value)];

    [Fact(DisplayName = "Publicação a cada merge")]
    public void Workflow_PublishesTheDevelopmentEnvironmentAfterEachMergeToMain()
    {
        var lines = WorkflowLines();
        var workflow = string.Join('\n', lines);

        workflow.Should().Contain("on:\n  push:\n    branches: [main]\n  workflow_dispatch:\n");
        workflow.Should().Contain("    environment: dev\n").And.Contain("      ENVIRONMENT: dev\n");
        ScriptsOf(lines).Should().Equal(["publish", "deploy", "plan"], "the package is built, published and then the infrastructure is compared");
        workflow.Should().Contain("concurrency:\n  group: deploy-dev\n  cancel-in-progress: false\n", "one publication at a time, none cut in the middle");
    }

    [Fact(DisplayName = "Pipeline sem segredos")]
    public void Workflow_ReachesAzureByFederation_WithoutSecrets()
    {
        foreach (var name in (string[])["deploy-dev", "deploy-prod"])
        {
            var lines = WorkflowLines(name);
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
    }

    // Spec: publicacao, requirement "Publicação em produção" (ADR-0125).
    [Fact(DisplayName = "Publicação em produção por versão")]
    public void ProductionWorkflow_StartsByHand_ChecksMigratesAndPublishes()
    {
        var lines = WorkflowLines("deploy-prod");
        var workflow = string.Join('\n', lines);

        workflow.Should().Contain("on:\n  workflow_dispatch:\n\n", "only by hand, on the tag of the version")
            .And.NotContain("push:").And.NotContain("inputs:");
        workflow.Should().Contain("    environment: prod\n").And.Contain("      ENVIRONMENT: prod\n")
            .And.Contain("      PLAN_MUST_MATCH: \"true\"\n");
        ScriptsOf(lines).Should().Equal(
            ["version", "plan", "publish", "migrate", "deploy"],
            "nothing changes before the version and the infrastructure are checked, and the migrations come before the new version");
        Array.FindIndex(lines, line => line.Contains("scripts/cd/version.sh", StringComparison.Ordinal))
            .Should().BeLessThan(Array.FindIndex(lines, line => line.Contains("azure/login", StringComparison.Ordinal)));
        workflow.Should().Contain("concurrency:\n  group: deploy-prod\n  cancel-in-progress: false\n");
    }

    [Fact(DisplayName = "Produção só por tag de versão")]
    public void Version_AcceptsOnlyTheTagOfAVersion()
    {
        Shell.SkipWithoutBash();

        Run("version", "prod", new() { ["GITHUB_REF_TYPE"] = "branch", ["GITHUB_REF_NAME"] = "main" }).ExitCode.Should().Be(1);
        Run("version", "prod", new() { ["GITHUB_REF_TYPE"] = "tag", ["GITHUB_REF_NAME"] = "v1.2" }).ExitCode.Should().Be(1);
        Run("version", "prod", new() { ["GITHUB_REF_TYPE"] = "tag", ["GITHUB_REF_NAME"] = "release-1.2.3" }).ExitCode.Should().Be(1);
        var version = Run("version", "prod", new() { ["GITHUB_REF_TYPE"] = "tag", ["GITHUB_REF_NAME"] = "v1.2.3" });

        version.ExitCode.Should().Be(0, version.Errors);
        version.Output.Should().Contain("Versão: v1.2.3");
        File.ReadAllText(Path.Combine(_fakes, "summary")).Should().Contain("v1.2.3");
    }

    [Fact(DisplayName = "Produção com a infraestrutura diferente")]
    public void Plan_WithDifferencesInProduction_StopsTheDeployment()
    {
        Shell.SkipWithoutBash();
        FakePlan(planExit: 2, planOutput: "  # module.app.azurerm_linux_web_app.main will be updated in-place\nPlan: 0 to add, 1 to change, 0 to destroy.");

        var result = Run("plan", "prod", new() { ["PLAN_MUST_MATCH"] = "true" });

        result.ExitCode.Should().Be(1);
        result.Errors.Should().Contain("A infraestrutura de prod está diferente do código");
        result.Output.Should().Contain("::error::");
        File.ReadAllText(Path.Combine(_fakes, "summary")).Should().Contain("Plan: 0 to add, 1 to change, 0 to destroy.");
        Calls().Should().Contain(call => call.Contains("-var-file=environments/prod.tfvars", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Migrações antes da publicação")]
    public void Migrate_RunsTheCommandOfThePackage_WithEntraAndTheIdentityOfTheApplication()
    {
        Shell.SkipWithoutBash();
        Fake("az", "echo \"az $*\" >> \"$FAKES/calls\"; echo 7f3e2d1c-0b9a-4876-9543-210fedcba987");
        Fake("dotnet", """
            {
              echo "dotnet $*"
              echo "environment $ASPNETCORE_ENVIRONMENT"
              echo "connection $ConnectionStrings__FiscoProjects"
              echo "identity $Database__AppIdentity__Name $Database__AppIdentity__ObjectId"
            } >> "$FAKES/calls"
            """);

        var result = Run("migrate", "prod");

        result.ExitCode.Should().Be(0, result.Errors);
        Calls().Should().Equal(
            "az webapp identity show --resource-group rg-fiscoprojects-mlr-prod --name app-fiscoprojects-mlr-prod --query principalId --output tsv",
            "dotnet artifacts/app/FiscoProjects.Api.dll migrate",
            "environment Production",
            "connection Server=tcp:sql-fiscoprojects-mlr-prod.database.windows.net,1433;Database=sqldb-fiscoprojects;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False",
            "identity app-fiscoprojects-mlr-prod 7f3e2d1c-0b9a-4876-9543-210fedcba987");

        Fake("az", "true");
        var withoutIdentity = Run("migrate", "prod");
        withoutIdentity.ExitCode.Should().Be(1);
        withoutIdentity.Errors.Should().Contain("has no managed identity");
    }

    [Fact(DisplayName = "Produção sem dados de demonstração")]
    public void Deploy_InProduction_LoadsNoDemonstration()
    {
        Shell.SkipWithoutBash();
        Fake("az", "echo \"az $*\" >> \"$FAKES/calls\"");
        Fake("curl", "echo \"curl ${@: -1}\" >> \"$FAKES/calls\"; printf 200");
        Fake("npm", "echo \"npm $*\" >> \"$FAKES/calls\"");
        Fake("sleep", "true");

        var result = Run("deploy", "prod");

        result.ExitCode.Should().Be(0, result.Errors);
        Calls().Should().Equal(
            "az webapp deploy --resource-group rg-fiscoprojects-mlr-prod --name app-fiscoprojects-mlr-prod --src-path artifacts/app.zip --type zip --track-status false --output none",
            "curl https://app-fiscoprojects-mlr-prod.azurewebsites.net/health");
    }

    [Fact(DisplayName = "Produção com aprovação")]
    public void ProductionEnvironment_AcceptsOnlyTheVersions_AfterTheApprovalOfWhoOperates()
    {
        var script = Repository.Read("infra", "scripts", "Set-GitHubEnvironment.ps1");

        script.Should().Contain("$settings.reviewers = @(@{ type = 'User'; id = [long](gh api user --jq .id) })")
            .And.Contain("$settings.prevent_self_review = $false")
            .And.Contain("$policy = @{ name = 'v*'; type = 'tag' }")
            .And.Contain("$policy = @{ name = 'main'; type = 'branch' }", "dev still accepts only main");
        Regex.Match(script, @"if \(\$Environment -eq 'prod'\) \{\s+\$settings\.reviewers").Success.Should().BeTrue(
            "the approval is only for production");
    }

    [Fact(DisplayName = "Demonstração depois da publicação")]
    public void Deploy_PublishesWaitsForHealth_AndLoadsTheDemonstration()
    {
        Shell.SkipWithoutBash();
        Fake("az", "echo \"az $*\" >> \"$FAKES/calls\"");
        Fake("curl", "echo \"curl ${@: -1}\" >> \"$FAKES/calls\"; printf 200");
        Fake("npm", "echo \"npm $* ($(basename \"$PWD\"))\" >> \"$FAKES/calls\"");
        Fake("sleep", "true");

        var result = Run("deploy");

        result.ExitCode.Should().Be(0, result.Errors);
        Calls().Should().Equal(
            "az webapp deploy --resource-group rg-fiscoprojects-mlr-dev --name app-fiscoprojects-mlr-dev --src-path artifacts/app.zip --type zip --track-status false --output none",
            "curl https://app-fiscoprojects-mlr-dev.azurewebsites.net/health",
            "npm run demo:data -- https://app-fiscoprojects-mlr-dev.azurewebsites.net (web)");
    }

    [Fact(DisplayName = "Demonstração depois de um reinício")]
    public void Deploy_TriesTheDemonstrationAgain_WhileTheApplicationRestarts()
    {
        Shell.SkipWithoutBash();
        Fake("az", "true");
        Fake("curl", "printf 200");
        Fake("sleep", "echo \"sleep $*\" >> \"$FAKES/calls\"");
        // The first load fails, as when the App Service restarts with the new package in the middle of it.
        Fake("npm", """
            echo "npm" >> "$FAKES/calls"
            runs=$(( $(cat "$FAKES/npm-runs" 2>/dev/null || echo 0) + 1 ))
            echo "$runs" > "$FAKES/npm-runs"
            [ "$runs" -ge 2 ]
            """);

        var result = Run("deploy");

        result.ExitCode.Should().Be(0, result.Errors);
        Calls().Should().Equal("sleep 20", "npm", "sleep 30", "npm");

        File.Delete(Path.Combine(_fakes, "calls"));
        Fake("npm", "echo \"npm\" >> \"$FAKES/calls\"; exit 1");
        var failed = Run("deploy");

        failed.ExitCode.Should().Be(1);
        failed.Errors.Should().Contain("did not load in 3 attempts");
        Calls().Should().Equal("sleep 20", "npm", "sleep 30", "npm", "sleep 30", "npm");
    }

    [Fact(DisplayName = "Infraestrutura diferente do código")]
    public void Plan_WithDifferences_WarnsWithoutFailing()
    {
        Shell.SkipWithoutBash();
        FakePlan(planExit: 2, planOutput: "  # module.app.azurerm_linux_web_app.main will be updated in-place\nPlan: 0 to add, 1 to change, 0 to destroy.");

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
        FakePlan(planExit: 0, planOutput: "No changes.");
        Run("plan").Output.Should().Contain("Infrastructure of dev matches the code.");

        FakePlan(planExit: 1, planOutput: "Error: unauthorized");
        var failed = Run("plan");
        failed.ExitCode.Should().Be(1);
        failed.Errors.Should().Contain("Error: unauthorized");
    }

    [Fact(DisplayName = "E-mail dos alertas fora dos logs")]
    public void Plan_ReadsTheAlertEmailFromTheActionGroup_AndMasksItFirst()
    {
        Shell.SkipWithoutBash();
        WorkflowLines().Should().NotContain(line => line.Contains("alert_email", StringComparison.OrdinalIgnoreCase),
            "GitHub shows the variables of the workflow in the public logs");
        FakePlan(planExit: 0, planOutput: "No changes.", alertEmail: "alertas@empresa.example");

        var result = Run("plan");

        result.ExitCode.Should().Be(0, result.Errors);
        result.Output.Split('\n')[0].Should().Be("::add-mask::alertas@empresa.example", "the e-mail is masked before anything is written");
        Calls().Should().ContainInOrder(
            "az monitor action-group show --resource-group rg-fiscoprojects-mlr-dev --name ag-fiscoprojects-mlr-dev --query emailReceivers[0].emailAddress --output tsv",
            "terraform sees the alert e-mail alertas@empresa.example");

        FakePlan(planExit: 0, planOutput: "No changes.", alertEmail: "");
        var withoutEmail = Run("plan");
        withoutEmail.ExitCode.Should().Be(1);
        withoutEmail.Errors.Should().Contain("has no e-mail");
    }

    // Spec: publicacao, requirement "Site do SharePoint fora dos logs" (ADR-0120).
    [Fact(DisplayName = "Site do SharePoint fora dos logs")]
    public void Plan_ReadsTheSharePointSiteFromTheApplication_AndMasksIt()
    {
        Shell.SkipWithoutBash();
        WorkflowLines().Should().NotContain(line => line.Contains("sharepoint", StringComparison.OrdinalIgnoreCase),
            "GitHub shows the variables of the workflow in the public logs");
        FakePlan(planExit: 0, planOutput: "No changes.", siteUrl: "https://empresa.sharepoint.example/sites/Projetos");

        var result = Run("plan");

        result.ExitCode.Should().Be(0, result.Errors);
        result.Output.Split('\n').Take(2).Should().Equal(
            "::add-mask::alertas@empresa.example", "::add-mask::https://empresa.sharepoint.example/sites/Projetos");
        Calls().Should().ContainInOrder(
            "az webapp config appsettings list --resource-group rg-fiscoprojects-mlr-dev --name app-fiscoprojects-mlr-dev --query [?name=='FileStorage__SharePoint__SiteUrl'].value | [0] --output tsv",
            "terraform sees the site https://empresa.sharepoint.example/sites/Projetos");

        File.Delete(Path.Combine(_fakes, "calls"));
        FakePlan(planExit: 0, planOutput: "No changes.");
        var withoutSite = Run("plan");
        withoutSite.ExitCode.Should().Be(0, withoutSite.Errors);
        Calls().Should().NotContain(call => call.StartsWith("terraform sees the site", StringComparison.Ordinal));
    }

    // Spec: publicacao, requirement "Caixa de envio fora dos logs" (ADR-0123).
    [Fact(DisplayName = "Caixa de envio mascarada")]
    public void Plan_ReadsTheMailboxFromTheApplication_AndMasksIt()
    {
        Shell.SkipWithoutBash();
        WorkflowLines().Should().NotContain(line => line.Contains("mailbox", StringComparison.OrdinalIgnoreCase),
            "GitHub shows the variables of the workflow in the public logs");
        FakePlan(planExit: 0, planOutput: "No changes.", mailbox: "fiscoprojects@empresa.example");

        var result = Run("plan");

        result.ExitCode.Should().Be(0, result.Errors);
        result.Output.Split('\n').Take(2).Should().Equal(
            "::add-mask::alertas@empresa.example", "::add-mask::fiscoprojects@empresa.example");
        Calls().Should().ContainInOrder(
            "az webapp config appsettings list --resource-group rg-fiscoprojects-mlr-dev --name app-fiscoprojects-mlr-dev --query [?name=='Email__Mailbox'].value | [0] --output tsv",
            "terraform sees the mailbox fiscoprojects@empresa.example");

        File.Delete(Path.Combine(_fakes, "calls"));
        FakePlan(planExit: 0, planOutput: "No changes.");
        var withoutMailbox = Run("plan");
        withoutMailbox.ExitCode.Should().Be(0, withoutMailbox.Errors);
        Calls().Should().NotContain(call => call.StartsWith("terraform sees the mailbox", StringComparison.Ordinal));
    }

    private void FakePlan(
        int planExit, string planOutput, string alertEmail = "alertas@empresa.example", string siteUrl = "", string mailbox = "")
    {
        Fake("az", $$"""
            echo "az $*" >> "$FAKES/calls"
            case "$*" in
              *"action-group"*) echo "{{alertEmail}}" ;;
              *"FileStorage__SharePoint__SiteUrl"*) echo "{{siteUrl}}" ;;
              *"Email__Mailbox"*) echo "{{mailbox}}" ;;
            esac
            """);
        File.WriteAllText(Path.Combine(_fakes, "plan-output"), planOutput + "\n");
        Fake("terraform", $$"""
            echo "terraform $*" >> "$FAKES/calls"
            if [ -n "${TF_VAR_alert_email:-}" ]; then echo "terraform sees the alert e-mail $TF_VAR_alert_email" >> "$FAKES/calls"; fi
            if [ -n "${TF_VAR_sharepoint_site_url:-}" ]; then echo "terraform sees the site $TF_VAR_sharepoint_site_url" >> "$FAKES/calls"; fi
            if [ -n "${TF_VAR_email_mailbox:-}" ]; then echo "terraform sees the mailbox $TF_VAR_email_mailbox" >> "$FAKES/calls"; fi
            case "$*" in
              "version -json") echo '{"terraform_version": "1.16.5"}' ;;
              *" plan "*) cat "$FAKES/plan-output"; exit {{planExit}} ;;
            esac
            """);
    }

    private Shell.Result Run(string script, string environment = "dev", Dictionary<string, string>? more = null)
    {
        var bin = Path.Combine(_fakes, "bin");
        var variables = new Dictionary<string, string>
        {
            ["PATH"] = $"{bin}:{Environment.GetEnvironmentVariable("PATH")}",
            ["FAKES"] = _fakes,
            ["ENVIRONMENT"] = environment,
            ["GITHUB_ACTIONS"] = "true",
            ["GITHUB_STEP_SUMMARY"] = Path.Combine(_fakes, "summary"),
        };
        foreach (var (name, value) in more ?? [])
        {
            variables[name] = value;
        }

        return Shell.Run(RepositoryFiles.PathOf(), variables, "bash", Path.Combine("scripts", "cd", script + ".sh"));
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
