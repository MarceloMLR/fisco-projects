using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: integracao-continua, requirement "Verificações em todo pull request" (ADR-0061, ADR-0115). licenses.sh runs
// with a stand-in for dotnet, which lists the packages, and a folder of packages of its own.
public sealed class LicensesScriptTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("fisco-licenses-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact(DisplayName = "Pacote do NuGet com licença não permitida")]
    public void Licenses_FailOnAPackageOutsideTheAllowedList()
    {
        Shell.SkipWithoutBash();
        Package("Permissive.Package", "1.0.0", "<license type=\"expression\">MIT</license>");
        Package("Choice.Package", "2.0.0", "<license type=\"expression\">(MIT OR GPL-3.0-only)</license>");
        Package("Microsoft.Data.SqlClient.SNI.runtime", "6.0.3", "<license type=\"file\">LICENSE.txt</license>");
        Package("Copyleft.Package", "3.1.0", "<license type=\"expression\">GPL-3.0-only</license>");
        Package("Unknown.Package", "0.1.0", "<licenseUrl>https://example.com/license</licenseUrl>");

        var result = Licenses();

        result.ExitCode.Should().Be(1);
        result.Errors.Should().Contain("Copyleft.Package 3.1.0: license GPL-3.0-only is not allowed")
            .And.Contain("Unknown.Package 0.1.0: license without an SPDX expression is not allowed")
            .And.NotContain("Permissive.Package").And.NotContain("Choice.Package").And.NotContain("SNI.runtime");
    }

    [Fact(DisplayName = "Pacotes do NuGet com licenças permitidas")]
    public void Licenses_PassWhenEveryPackageIsAllowed()
    {
        Shell.SkipWithoutBash();
        Package("Permissive.Package", "1.0.0", "<license type=\"expression\">Apache-2.0</license>");

        var result = Licenses();

        result.ExitCode.Should().Be(0, result.Errors);
        result.Output.Should().Contain("NuGet licenses: all allowed.");
    }

    private readonly List<(string Id, string Version)> _packages = [];

    private void Package(string id, string version, string license)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_folder, "packages", id.ToLowerInvariant(), version)).FullName;
        File.WriteAllText(Path.Combine(folder, $"{id.ToLowerInvariant()}.nuspec"),
            $"<?xml version=\"1.0\"?>\n<package><metadata>\n<id>{id}</id>\n{license}\n</metadata></package>\n");
        _packages.Add((id, version));
    }

    private Shell.Result Licenses()
    {
        var bin = Directory.CreateDirectory(Path.Combine(_folder, "bin")).FullName;
        var listed = string.Join(',', _packages.Select(package => $"{{\"id\":\"{package.Id}\",\"resolvedVersion\":\"{package.Version}\"}}"));
        File.WriteAllText(Path.Combine(_folder, "list.json"), $"{{\"projects\":[{{\"frameworks\":[{{\"transitivePackages\":[{listed}]}}]}}]}}");
        var dotnet = Path.Combine(bin, "dotnet");
        File.WriteAllText(dotnet, $"#!/usr/bin/env bash\ncat \"{Path.Combine(_folder, "list.json")}\"\n");
        Shell.Run(bin, null, "chmod", "+x", dotnet);
        return Shell.Run(RepositoryFiles.PathOf(), new Dictionary<string, string>
        {
            ["PATH"] = $"{bin}:{Environment.GetEnvironmentVariable("PATH")}",
            ["NUGET_PACKAGES"] = Path.Combine(_folder, "packages"),
        }, "bash", Path.Combine("scripts", "ci", "licenses.sh"));
    }
}
