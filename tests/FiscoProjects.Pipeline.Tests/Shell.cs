using System.Diagnostics;

namespace FiscoProjects.Pipeline.Tests;

/// <summary>
/// Runs commands with bash and git, as the verification does on Linux. On Windows, the bash on the path may be the one
/// of WSL, so the tests that need it are skipped there and run in the CI.
/// </summary>
internal static class Shell
{
    public sealed record Result(int ExitCode, string Output, string Errors);

    public static void SkipWithoutBash() =>
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "The scripts of the verification run with bash on Linux.");

    /// <summary>Runs a program in a folder, with these variables added to the environment.</summary>
    public static Result Run(string folder, IReadOnlyDictionary<string, string>? environment, string program, params string[] arguments)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // The tests never reach GitHub: no variable of the runner leaks into the scripts.
        foreach (var name in new[] { "GITHUB_OUTPUT", "GH_TOKEN", "GITHUB_TOKEN", "PR_NUMBER", "REPOSITORY", "HEAD_SHA", "CHANGES_BASE" })
        {
            start.Environment.Remove(name);
        }

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[name] = value;
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"{program} did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return new Result(process.ExitCode, output.GetAwaiter().GetResult(), errors.GetAwaiter().GetResult());
    }
}
