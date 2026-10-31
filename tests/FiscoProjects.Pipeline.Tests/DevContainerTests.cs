using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: dados-de-demonstracao, requirement "Demonstração no Codespaces" (ADR-0113). The start of the codespace runs
// with stand-ins for docker, dotnet, curl, npm and gh, which write down how they were called.
public sealed class DevContainerTests : IDisposable
{
    private const string Codespace = "demo-codespace";

    private readonly string _fakes = Directory.CreateTempSubdirectory("fisco-devcontainer-").FullName;

    public void Dispose() => Directory.Delete(_fakes, recursive: true);

    [Fact(DisplayName = "Versões do Codespace")]
    public void Codespace_UsesThePinnedVersionsAndThePortOfTheApi()
    {
        using var configuration = JsonDocument.Parse(Repository.Read(".devcontainer", "devcontainer.json"));
        var root = configuration.RootElement;
        using var globalJson = JsonDocument.Parse(Repository.Read("global.json"));
        var sdk = Version.Parse(globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString()!);
        using var launchSettings = JsonDocument.Parse(Repository.Read("src", "FiscoProjects.Api", "Properties", "launchSettings.json"));
        var api = new Uri(launchSettings.RootElement.GetProperty("profiles").GetProperty("http").GetProperty("applicationUrl").GetString()!);

        root.GetProperty("image").GetString().Should().Be($"mcr.microsoft.com/devcontainers/dotnet:2-{sdk.Major}.{sdk.Minor}");
        root.GetProperty("features").GetProperty("ghcr.io/devcontainers/features/node:1").GetProperty("version").GetString()
            .Should().Be(Repository.Read(".nvmrc").Trim());
        root.GetProperty("forwardPorts").EnumerateArray().Select(port => port.GetInt32()).Should().Equal(api.Port);
        Repository.Read(".devcontainer", "start.sh").Should().Contain($"localhost:{api.Port}/health").And.Contain($"-{api.Port}.");
        root.GetProperty("postCreateCommand").GetString().Should().Be("bash .devcontainer/prepare.sh");
        root.GetProperty("postAttachCommand").GetString().Should().Be("bash .devcontainer/start.sh");
        foreach (var script in new[] { "prepare.sh", "start.sh" })
        {
            var commands = Repository.Read(".devcontainer", script).Split('\n').Where(line => line.Length > 0 && !line.StartsWith('#')).ToArray();
            commands[..2].Should().Equal("set -euo pipefail", "cd \"$(dirname \"$0\")/..\"");
        }

        // No secret besides the password of the database it makes itself (ADR-0134): no license key.
        foreach (var file in Directory.GetFiles(RepositoryFiles.PathOf(".devcontainer")))
        {
            File.ReadAllText(file).Should().NotContain("LicenseKey").And.NotContainEquivalentOf("syncfusion");
        }
    }

    [Fact(DisplayName = "Demonstração no Codespaces")]
    public void Start_BringsUpTheDatabaseTheApiAndTheDataAndSharesTheAddress()
    {
        Shell.SkipWithoutBash();

        var result = Start(apiAlreadyUp: false);

        result.ExitCode.Should().Be(0, result.Errors);
        Calls().Should().Equal(
            "docker compose up -d --wait",
            "dotnet run --project src/FiscoProjects.Api",
            "npm run demo:data (web)",
            $"gh codespace ports visibility 5175:public -c {Codespace}");
        result.Output.Should().Contain($"FiscoProjects no ar: https://{Codespace}-5175.app.github.dev")
            .And.Contain("O link já é público")
            .And.Contain("paulo.nogueira@empresa.example");
    }

    [Fact(DisplayName = "Codespace aberto de novo")]
    public void Start_WithTheApiUp_OnlyShowsTheAddress()
    {
        Shell.SkipWithoutBash();

        var result = Start(apiAlreadyUp: true);

        result.ExitCode.Should().Be(0, result.Errors);
        result.Output.Should().Contain($"A demonstração já está no ar: https://{Codespace}-5175.app.github.dev");
        Calls().Should().BeEmpty();
    }

    private Shell.Result Start(bool apiAlreadyUp)
    {
        var bin = Directory.CreateDirectory(Path.Combine(_fakes, "bin")).FullName;
        Fake(bin, "curl", "[ -f \"$FAKES/up\" ]");
        Fake(bin, "docker", "echo \"docker $*\" >> \"$FAKES/calls\"");
        Fake(bin, "dotnet", "echo \"dotnet $*\" >> \"$FAKES/calls\"; echo 'Now listening on: http://localhost:5175'; touch \"$FAKES/up\"; sleep 2");
        Fake(bin, "npm", "echo \"npm $* ($(basename \"$PWD\"))\" >> \"$FAKES/calls\"");
        Fake(bin, "gh", "echo \"gh $*\" >> \"$FAKES/calls\"");
        if (apiAlreadyUp)
        {
            File.WriteAllText(Path.Combine(_fakes, "up"), string.Empty);
        }

        return Shell.Run(RepositoryFiles.PathOf(), new Dictionary<string, string>
        {
            ["PATH"] = $"{bin}:{Environment.GetEnvironmentVariable("PATH")}",
            ["FAKES"] = _fakes,
            ["TMPDIR"] = _fakes,
            ["CODESPACE_NAME"] = Codespace,
            ["GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"] = "app.github.dev",
        }, "bash", Path.Combine(".devcontainer", "start.sh"));
    }

    private string[] Calls()
    {
        var calls = Path.Combine(_fakes, "calls");
        return File.Exists(calls) ? File.ReadAllLines(calls) : [];
    }

    private static void Fake(string bin, string name, string body)
    {
        var path = Path.Combine(bin, name);
        File.WriteAllText(path, $"#!/usr/bin/env bash\n{body}\n");
        Shell.Run(bin, null, "chmod", "+x", path);
    }
}
