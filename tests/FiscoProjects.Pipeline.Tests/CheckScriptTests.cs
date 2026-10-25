using System.Text.Json;
using System.Xml.Linq;
using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: integracao-continua.
public sealed class CheckScriptTests
{
    /// <summary>The scripts of the verification (scripts/ci) and of the publication (scripts/cd), as "ci/build".</summary>
    public static TheoryData<string> Scripts() =>
        [.. new[] { "ci", "cd" }
            .SelectMany(folder => Directory.GetFiles(RepositoryFiles.PathOf("scripts", folder), "*.sh")
                .Select(path => $"{folder}/{Path.GetFileNameWithoutExtension(path)}"))
            .Order()];

    [Theory(DisplayName = "Comando que falha reprova a verificação")]
    [MemberData(nameof(Scripts))]
    public void Script_StopsAtTheFirstFailingCommand(string script)
    {
        FirstCommands(script)[0].Should().Be("set -euo pipefail");
    }

    [Theory(DisplayName = "Script chamado de outra pasta")]
    [MemberData(nameof(Scripts))]
    public void Script_WorksFromTheRepositoryRoot(string script)
    {
        FirstCommands(script)[1].Should().Be("cd \"$(dirname \"$0\")/../..\"");
    }

    [Fact(DisplayName = "Pacote com vulnerabilidade conhecida")]
    public void Build_FailsOnVulnerablePackages()
    {
        var properties = XDocument.Parse(Repository.Read("Directory.Build.props")).Descendants().ToDictionary(e => e.Name.LocalName, e => e.Value);

        properties.Should().Contain("TreatWarningsAsErrors", "true")
            .And.Contain("NuGetAudit", "true")
            .And.Contain("NuGetAuditMode", "all")
            .And.Contain("NuGetAuditLevel", "low");
        Repository.Read("scripts", "ci", "build.sh").Should().Contain("dotnet build FiscoProjects.slnx -warnaserror");
    }

    [Fact(DisplayName = "Pacote do frontend com vulnerabilidade conhecida")]
    public void Frontend_FailsOnVulnerablePackagesThatReachTheBrowser()
    {
        FrontendCommands().Should().Contain("npm audit --omit=dev");
    }

    [Fact(DisplayName = "Dependência do frontend com licença não permitida")]
    public void Frontend_FailsOnLicensesOutsideTheAllowedList()
    {
        FrontendCommands().Should().Contain("npm run licenses");

        using var package = JsonDocument.Parse(Repository.Read("web", "package.json"));
        var command = package.RootElement.GetProperty("scripts").GetProperty("licenses").GetString();
        command.Should().StartWith("license-checker-rseidelsohn ").And.Contain("--onlyAllow ");
        AllowedLicenses(command!).Should().NotContain(license => license.Contains("GPL", StringComparison.Ordinal)
            || license.StartsWith("SSPL", StringComparison.Ordinal) || license.StartsWith("BUSL", StringComparison.Ordinal));
    }

    private static string[] FrontendCommands() =>
        [.. File.ReadAllLines(Path.Combine(Repository.ScriptsFolder, "frontend.sh")).Select(line => line.Trim())];

    private static string[] AllowedLicenses(string command) =>
        command.Split("--onlyAllow ")[1].Split(' ')[0].Trim('"').Split(';');

    private static string[] FirstCommands(string script) =>
        [.. File.ReadAllLines(RepositoryFiles.PathOf(["scripts", .. script.Split('/')]) + ".sh")
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Take(2)];
}
