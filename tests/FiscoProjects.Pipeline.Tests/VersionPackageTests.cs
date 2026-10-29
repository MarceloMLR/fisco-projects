using System.IO.Compression;
using System.Text.RegularExpressions;
using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: publicacao, requirements "Pacote da versão" and "Publicação de uma versão à mão" (ADR-0127). The workflow is read
// as text; the scripts run with stand-ins for gh, az, dotnet and the web requests, which write down how they were called.
public sealed partial class VersionPackageTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("fisco-release-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Calls => Path.Combine(_folder, "calls");

    private string Artifacts => Path.Combine(_folder, "artifacts");

    [Fact(DisplayName = "Pacote anexado à versão")]
    public void Release_PackagesTheVersion_AndAttachesItToTheRelease()
    {
        var lines = File.ReadAllLines(RepositoryFiles.PathOf(".github", "workflows", "release.yml"));
        var workflow = string.Join('\n', lines);

        workflow.Should().Contain("on:\n  release:\n    types: [published]\n", "only when a version is published");
        workflow.Should().Contain("\npermissions:\n  contents: write\n\n", "it only attaches the package to the version")
            .And.NotContain("id-token").And.NotContain("azure/login").And.NotContain("secrets.");
        lines.Select(line => ScriptStep().Match(line)).Where(match => match.Success).Select(match => match.Groups["script"].Value)
            .Should().Equal(["publish", "release"], "the application is built and then packaged with the version");
        lines.Where(line => line.TrimStart().StartsWith("- uses:", StringComparison.Ordinal))
            .Should().AllSatisfy(line => PinnedAction().IsMatch(line).Should().BeTrue($"\"{line.Trim()}\" must be pinned to a commit"));
        lines.Where(line => line.TrimStart().StartsWith("run:", StringComparison.Ordinal))
            .Should().AllSatisfy(line => ScriptStep().IsMatch(line).Should().BeTrue("the logic lives in scripts/cd"));
        lines.Should().Contain("          persist-credentials: false");

        Shell.SkipWithoutBash();
        var result = Release("v1.2.3");

        result.ExitCode.Should().Be(0, result.Errors);
        var package = Path.Combine(Artifacts, "fiscoprojects-v1.2.3.zip");
        File.ReadAllLines(Calls).Should().Equal($"gh release upload v1.2.3 {package} --clobber --repo MarceloMLR/fisco-projects");
        using var zip = ZipFile.OpenRead(package);
        var entries = zip.Entries.Select(entry => entry.FullName).ToList();
        entries.Should().Contain(["app.zip", "VERSION", "infra/terraform/main.tf", "infra/terraform/variables.tf", "infra/terraform/.terraform.lock.hcl",
            "infra/terraform/modules/app/main.tf", "docs/instalacao.md", "docs/restauracao.md", "docs/custos-estimados.md"]);
        entries.Should().Contain(Directory.GetFiles(RepositoryFiles.PathOf("infra", "scripts")).Select(path => $"infra/scripts/{Path.GetFileName(path)}"));
        using (var version = new StreamReader(zip.GetEntry("VERSION")!.Open()))
        {
            version.ReadToEnd().Trim().Should().Be("v1.2.3");
        }

        File.ReadAllText(Path.Combine(_folder, "summary")).Should().Contain("fiscoprojects-v1.2.3.zip");
    }

    [Fact(DisplayName = "Pacote só de versão")]
    public void Release_OutsideTheTagOfAVersion_AttachesNothing()
    {
        Shell.SkipWithoutBash();

        foreach (var (type, name) in new[] { ("branch", "main"), ("tag", "v1.2"), ("tag", "release-1.2.3") })
        {
            var result = Release(name, type);

            result.ExitCode.Should().Be(1, $"{type} {name} is no version");
            result.Errors.Should().Contain("Publique a versão numa tag como v1.2.3");
        }

        File.Exists(Calls).Should().BeFalse("nothing reaches GitHub");
        Directory.GetFiles(Artifacts, "fiscoprojects-*").Should().BeEmpty();
    }

    [Fact(DisplayName = "Pacote sem os ambientes")]
    public void Release_TakesOnlyWhatIsInGit_WithoutTheEnvironmentsOfThisRepository()
    {
        Shell.SkipWithoutBash();

        Release("v2.0.0").ExitCode.Should().Be(0);

        using var zip = ZipFile.OpenRead(Path.Combine(Artifacts, "fiscoprojects-v2.0.0.zip"));
        zip.Entries.Select(entry => entry.FullName).Should().NotContain(entry =>
            entry.Contains("/environments/", StringComparison.Ordinal)
            || entry.EndsWith(".tfvars", StringComparison.Ordinal)
            || entry.Contains(".tfstate", StringComparison.Ordinal)
            || entry.Contains("/.terraform/", StringComparison.Ordinal),
            "the parameters of the environments, the local values and the state of this repository stay out");
    }

    [Fact(DisplayName = "Publicação à mão")]
    public void PublishVersion_MigratesWithTheAzureCli_ThenPublishesAndWaitsForHealth()
    {
        var result = PublishVersion();

        result.ExitCode.Should().Be(0, result.Output + result.Errors);
        var calls = File.ReadAllLines(Calls);
        calls.Should().HaveCount(7);
        calls[0].Should().Be("az webapp identity show --resource-group rg-fiscoprojects-cli-prod --name app-fiscoprojects-cli-prod --query principalId --output tsv");
        calls[1].Should().Be("az sql server firewall-rule create --resource-group rg-fiscoprojects-cli-prod --server sql-fiscoprojects-cli-prod --name publicacao-temporaria --start-ip-address 198.51.100.7 --end-ip-address 198.51.100.7 --output none");
        calls[2].Should().MatchRegex(@"^dotnet .+FiscoProjects\.Api\.dll migrate$");
        calls[3].Should().Be("environment Production | credential AzureCliCredential | identity app-fiscoprojects-cli-prod 7f3e2d1c-0b9a-4876-9543-210fedcba987");
        calls[4].Should().Be("az sql server firewall-rule delete --resource-group rg-fiscoprojects-cli-prod --server sql-fiscoprojects-cli-prod --name publicacao-temporaria --output none");
        calls[5].Should().MatchRegex(@"^az webapp deploy --resource-group rg-fiscoprojects-cli-prod --name app-fiscoprojects-cli-prod --src-path .+app\.zip --type zip --track-status false --output none$");
        calls[6].Should().Be("health https://app-fiscoprojects-cli-prod.azurewebsites.net/health");
        result.Output.Should().Contain("Versão v1.2.3 na instalação fiscoprojects-cli-prod");
    }

    [Fact(DisplayName = "Firewall aberto só nas migrações")]
    public void PublishVersion_WhenTheMigrationsFail_ClosesTheFirewall_AndPublishesNothing()
    {
        var result = PublishVersion(migrationsFail: true);

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("As migrações falharam em sql-fiscoprojects-cli-prod; nada foi publicado.");
        var calls = File.ReadAllLines(Calls);
        calls.Count(call => call.StartsWith("dotnet ", StringComparison.Ordinal)).Should().Be(3, "the rule may take a moment to hold");
        calls[^1].Should().StartWith("az sql server firewall-rule delete ", "the rule is removed even when the migrations fail");
        calls.Should().NotContain(call => call.StartsWith("az webapp deploy", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Publicação sem a identidade da aplicação")]
    public void PublishVersion_WithoutTheIdentityOfTheApplication_StopsBeforeTheDatabase()
    {
        var result = PublishVersion(withIdentity: false);

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("não tem identidade gerenciada: aplique antes o Terraform da versão");
        File.ReadAllLines(Calls).Should().ContainSingle().Which.Should().StartWith("az webapp identity show");
    }

    private Shell.Result Release(string name, string type = "tag")
    {
        Directory.CreateDirectory(Artifacts);
        File.WriteAllText(Path.Combine(Artifacts, "app.zip"), "the application");
        var bin = Directory.CreateDirectory(Path.Combine(_folder, "bin")).FullName;
        var gh = Path.Combine(bin, "gh");
        File.WriteAllText(gh, $"#!/usr/bin/env bash\necho \"gh $*\" >> \"{Calls}\"\n");
        Shell.Run(bin, null, "chmod", "+x", gh);

        return Shell.Run(RepositoryFiles.PathOf(), new Dictionary<string, string>
        {
            ["PATH"] = $"{bin}:{Environment.GetEnvironmentVariable("PATH")}",
            ["ARTIFACTS"] = Artifacts,
            ["GITHUB_REF_TYPE"] = type,
            ["GITHUB_REF_NAME"] = name,
            ["GITHUB_REPOSITORY"] = "MarceloMLR/fisco-projects",
            ["GITHUB_STEP_SUMMARY"] = Path.Combine(_folder, "summary"),
        }, "bash", Path.Combine("scripts", "cd", "release.sh"));
    }

    /// <summary>
    /// Runs Publish-Version.ps1 in the folder of a package of the installation "cli" in production, with stand-ins for
    /// az, dotnet, the address of this computer, /health and the waits, defined before the script runs.
    /// </summary>
    private Shell.Result PublishVersion(bool withIdentity = true, bool migrationsFail = false)
    {
        var powerShell = PowerShell();
        var package = Directory.CreateDirectory(Path.Combine(_folder, "package")).FullName;
        var scripts = Directory.CreateDirectory(Path.Combine(package, "infra", "scripts")).FullName;
        var terraform = Directory.CreateDirectory(Path.Combine(package, "infra", "terraform")).FullName;
        File.Copy(RepositoryFiles.PathOf("infra", "scripts", "Publish-Version.ps1"), Path.Combine(scripts, "Publish-Version.ps1"));
        File.WriteAllText(Path.Combine(terraform, "local.auto.tfvars"),
            "installation = \"cli\"\nenvironment  = \"prod\"\noperator_object_ids = [\"00000000-0000-0000-0000-000000000001\"]\n");
        File.WriteAllText(Path.Combine(package, "VERSION"), "v1.2.3\n");
        var application = Directory.CreateDirectory(Path.Combine(_folder, "application")).FullName;
        File.WriteAllText(Path.Combine(application, "FiscoProjects.Api.dll"), "the application");
        ZipFile.CreateFromDirectory(application, Path.Combine(package, "app.zip"));

        var identity = withIdentity ? "'7f3e2d1c-0b9a-4876-9543-210fedcba987'" : "$null";
        var migrated = migrationsFail ? 1 : 0;
        var wrapper = Path.Combine(_folder, "publish.ps1");
        File.WriteAllText(wrapper, $$"""
            $calls = '{{Calls}}'
            function global:az {
                if ("$args" -like 'account show*') { return 'Assinatura de teste' }
                Add-Content $calls "az $args"
                if ("$args" -like 'webapp identity show*') { return {{identity}} }
            }
            function global:dotnet {
                Add-Content $calls "dotnet $args"
                Add-Content $calls "environment $env:ASPNETCORE_ENVIRONMENT | credential $env:AZURE_TOKEN_CREDENTIALS | identity $env:Database__AppIdentity__Name $env:Database__AppIdentity__ObjectId"
                $global:LASTEXITCODE = {{migrated}}
            }
            function global:Invoke-RestMethod { '198.51.100.7' }
            function global:Invoke-WebRequest { param($Uri) Add-Content $calls "health $Uri"; [pscustomobject]@{ StatusCode = 200 } }
            function global:Start-Sleep { }
            try {
                & '{{Path.Combine(scripts, "Publish-Version.ps1")}}'
            }
            catch {
                Write-Output "$_"
                exit 1
            }
            """);

        return Shell.Run(_folder, null, powerShell, "-NoProfile", "-NonInteractive", "-File", wrapper);
    }

    /// <summary>PowerShell 7, as the GitHub runner and the computers of who operates have; the test is skipped without it.</summary>
    private static string PowerShell()
    {
        var name = OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh";
        var found = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => Path.Combine(folder, name))
            .FirstOrDefault(File.Exists);
        Assert.SkipWhen(found is null, "The scripts of who operates run with PowerShell 7 (pwsh).");
        return found!;
    }

    [GeneratedRegex(@"^\s+run: bash scripts/cd/(?<script>[a-z-]+)\.sh$")]
    private static partial Regex ScriptStep();

    [GeneratedRegex(@"^\s+- uses: [\w.-]+/[\w.-]+@[0-9a-f]{40} # v\d+(\.\d+)*$")]
    private static partial Regex PinnedAction();
}
