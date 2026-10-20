namespace FiscoProjects.Pipeline.Tests;

// Spec: integracao-continua, requirement "Commit que só muda documentação" (ADR-0090). Each test builds a Git
// repository with the script, a main and a pull request, and a fake gh that answers which commits passed the
// verification.
public sealed class ChangeScriptTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("fisco-changes-").FullName;

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_folder, recursive: true);
    }

    [Fact(DisplayName = "Archive só com documentação")]
    public void DocumentationAfterAVerifiedCommit_RunsOnlyTheSpecs()
    {
        Shell.SkipWithoutBash();
        var repository = new SampleRepository(_folder);
        var code = repository.Commit("src/Projects/Schedule.cs");
        var archive = repository.Commit("openspec/changes/archive/x/tasks.md", "openspec/specs/x/spec.md", "docs/roadmap.md");

        repository.Changes(verified: [code], head: archive).Should().Be("code=false");
        // Against main, the pull request still has code.
        repository.Changes().Should().Be("code=true");
    }

    [Theory(DisplayName = "Código depois de um commit aprovado")]
    [InlineData("docs/adr/0090-x.md", false)]
    [InlineData("openspec/specs/x/spec.md", false)]
    [InlineData(".claude/commands/opsx/apply.md", false)]
    [InlineData("CLAUDE.md", false)]
    [InlineData("src/Modules/README.md", false)]
    [InlineData("web/README.md", true)]
    [InlineData("web/src/api/openapi.json", true)]
    [InlineData("src/Modules/Projects/Schedule.cs", true)]
    [InlineData("tests/Pipeline/ChangeScriptTests.cs", true)]
    [InlineData(".github/workflows/pull-request.yml", true)]
    [InlineData("scripts/ci/build.sh", true)]
    [InlineData("global.json", true)]
    public void AnythingButDocumentation_RunsEverything(string file, bool code)
    {
        Shell.SkipWithoutBash();
        var repository = new SampleRepository(_folder);
        var verified = repository.Commit("src/Projects/Schedule.cs");
        var head = repository.Commit("docs/roadmap.md", file);

        repository.Changes(verified: [verified], head: head).Should().Be(code ? "code=true" : "code=false");
    }

    [Fact(DisplayName = "Commit aprovado anterior a uma falha")]
    public void DocumentationAfterAFailedCommit_ComparesWithTheLastPassedOne()
    {
        Shell.SkipWithoutBash();
        var repository = new SampleRepository(_folder);
        var passed = repository.Commit("src/Projects/Schedule.cs");
        repository.Commit("src/Projects/ScheduleFigures.cs");
        var documentation = repository.Commit("docs/roadmap.md");

        // The commit in between failed: the code it changed runs again.
        repository.Changes(verified: [passed], head: documentation).Should().Be("code=true");
        // With no commit passed, the base is main.
        repository.Changes(verified: [], head: documentation).Should().Be("code=true");
        // By hand, the base may be given.
        repository.Changes(environment: new Dictionary<string, string> { ["CHANGES_BASE"] = "HEAD~1" }).Should().Be("code=false");
    }

    [Fact(DisplayName = "Base fora do histórico")]
    public void BaseOutOfTheHistory_RunsEverything()
    {
        Shell.SkipWithoutBash();
        var repository = new SampleRepository(_folder);
        repository.Commit("docs/roadmap.md");

        repository.Changes(environment: new Dictionary<string, string> { ["CHANGES_BASE"] = new string('0', 40) })
            .Should().Be("code=true");
    }

    /// <summary>
    /// A repository with the script of the verification, a main with one commit, a branch for the pull request, and
    /// a fake gh on the path.
    /// </summary>
    private sealed class SampleRepository
    {
        private readonly string _root;
        private readonly string _fakeGh;
        private int _files;

        public SampleRepository(string folder)
        {
            _root = Path.Combine(folder, "repository");
            _fakeGh = Path.Combine(folder, "gh");
            Directory.CreateDirectory(Path.Combine(_root, "scripts", "ci"));
            Directory.CreateDirectory(Path.Combine(_fakeGh, "bin"));
            File.Copy(Path.Combine(Repository.ScriptsFolder, "changes.sh"), Path.Combine(_root, "scripts", "ci", "changes.sh"));
            File.WriteAllText(Path.Combine(_fakeGh, "bin", "gh"), FakeGh);
            Git("init", "--quiet", "--initial-branch=main");
            Git("config", "user.email", "verificacao@empresa.example");
            Git("config", "user.name", "Verificação");
            Git("config", "commit.gpgsign", "false");
            Shell.Run(_fakeGh, null, "chmod", "+x", Path.Combine(_fakeGh, "bin", "gh"));
            Commit("src/Projects/Project.cs", "docs/discovery.md", "web/src/main.tsx");
            Git("update-ref", "refs/remotes/origin/main", "HEAD");
            Git("switch", "--quiet", "-c", "feat/x");
        }

        /// <summary>Changes these files in a new commit, and gives its hash.</summary>
        public string Commit(params string[] files)
        {
            foreach (var file in files)
            {
                var path = Path.Combine(_root, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, $"change {++_files}\n");
            }

            Git("add", "--all");
            Git("commit", "--quiet", "-m", $"commit {_files}");
            return Git("rev-parse", "HEAD").Trim();
        }

        /// <summary>
        /// The line the script prints: on GitHub, with these commits of the pull request passed; outside it, with
        /// these variables.
        /// </summary>
        public string Changes(IReadOnlyCollection<string>? verified = null, string? head = null, IReadOnlyDictionary<string, string>? environment = null)
        {
            var variables = new Dictionary<string, string>(environment ?? new Dictionary<string, string>());
            if (verified is not null)
            {
                var commits = Git("log", "--reverse", "--format=%H", "origin/main..HEAD");
                File.WriteAllText(Path.Combine(_fakeGh, "commits"), commits);
                File.WriteAllText(Path.Combine(_fakeGh, "passed"), string.Join('\n', verified) + "\n");
                variables["PATH"] = $"{Path.Combine(_fakeGh, "bin")}:{Environment.GetEnvironmentVariable("PATH")}";
                variables["FAKE_GH"] = _fakeGh;
                variables["PR_NUMBER"] = "31";
                variables["REPOSITORY"] = "fisco/projects";
                variables["HEAD_SHA"] = head ?? string.Empty;
            }

            var result = Shell.Run(_root, variables, "bash", "scripts/ci/changes.sh");
            result.ExitCode.Should().Be(0, result.Errors);
            return result.Output.Trim();
        }

        private string Git(params string[] arguments)
        {
            var result = Shell.Run(_root, null, "git", arguments);
            result.ExitCode.Should().Be(0, result.Errors);
            return result.Output;
        }

        /// <summary>Answers the commits of the pull request, and whether a commit passed the verification.</summary>
        private const string FakeGh = """
            #!/usr/bin/env bash
            case "$2" in
              */pulls/*/commits*) cat "$FAKE_GH/commits" ;;
              */check-runs*)
                sha=${2#*/commits/}
                sha=${sha%%/*}
                if grep -qx "$sha" "$FAKE_GH/passed"; then echo 1; else echo 0; fi
                ;;
              *) exit 1 ;;
            esac
            """;
    }
}
