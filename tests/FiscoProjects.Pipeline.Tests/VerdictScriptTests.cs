namespace FiscoProjects.Pipeline.Tests;

// Spec: integracao-continua, requirements "Verificações em todo pull request" and "Commit que só muda documentação"
// (ADR-0090): the job verify passes only when the parts that had to run passed.
public sealed class VerdictScriptTests
{
    private static Shell.Result Verdict(string code, string backend, string format, string frontend, string specs = "success", string changes = "success") =>
        Shell.Run(Repository.ScriptsFolder, new Dictionary<string, string>
        {
            ["CODE"] = code,
            ["CHANGES"] = changes,
            ["BACKEND"] = backend,
            ["FORMAT"] = format,
            ["FRONTEND"] = frontend,
            ["SPECS"] = specs,
        }, "bash", Path.Combine(Repository.ScriptsFolder, "verdict.sh"));

    [Fact(DisplayName = "Todas as verificações no pull request")]
    public void Verify_PassesOnlyWhenEveryPartPassed()
    {
        Shell.SkipWithoutBash();

        Verdict("true", "success", "success", "success").ExitCode.Should().Be(0);
        Verdict("true", "failure", "success", "success").ExitCode.Should().Be(1);
        Verdict("true", "success", "success", "cancelled").ExitCode.Should().Be(1);
        Verdict("true", "success", "success", "success", specs: "failure").ExitCode.Should().Be(1);
        // Without the answer of changes, nothing may be taken for granted.
        Verdict(string.Empty, "skipped", "skipped", "skipped", changes: "failure").ExitCode.Should().Be(1);
    }

    [Fact(DisplayName = "Parte pulada sem motivo")]
    public void Verify_FailsWhenACodePartWasSkippedWithCode()
    {
        Shell.SkipWithoutBash();

        var skipped = Verdict("true", "skipped", "success", "success");

        skipped.ExitCode.Should().Be(1);
        skipped.Errors.Should().Contain("backend: skipped, but it must be success");
        // With documentation only, the code parts may be skipped, but none may fail; the specs always run.
        Verdict("false", "skipped", "skipped", "skipped").ExitCode.Should().Be(0);
        Verdict("false", "skipped", "failure", "skipped").ExitCode.Should().Be(1);
        Verdict("false", "skipped", "skipped", "skipped", specs: "skipped").ExitCode.Should().Be(1);
    }
}
