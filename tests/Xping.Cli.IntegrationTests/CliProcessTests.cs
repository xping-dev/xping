// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Diagnostics;
using Xping.Core;

namespace Xping.Cli.IntegrationTests;

/// <summary>Runs the built <c>xping</c> executable as a separate process.</summary>
public sealed class CliProcessTests
{
    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "xping.dll"));
        foreach (string arg in args)
            start.ArgumentList.Add(arg);

        using var process = Process.Start(start)!;
        var cancellation = TestContext.Current.CancellationToken;
        Task<string> output = process.StandardOutput.ReadToEndAsync(cancellation);
        Task<string> error = process.StandardError.ReadToEndAsync(cancellation);
        await process.WaitForExitAsync(cancellation);
        return (process.ExitCode, await output, await error);
    }

    [Fact]
    public async Task VersionExitsZero()
    {
        var (exitCode, output, _) = await RunAsync("--version");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.StartsWith("2.0.0", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HelpNamesTheExecutableXping()
    {
        var (exitCode, output, _) = await RunAsync("--help");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("xping [command] [options]", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AStubCommandExitsWithTheNotImplementedCode()
    {
        var (exitCode, _, error) = await RunAsync("check", "--local");

        Assert.Equal(ExitCodes.NotImplemented, exitCode);
        Assert.Contains("not implemented", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AParseErrorExitsWithTheUsageCode()
    {
        var (exitCode, _, _) = await RunAsync("check", "--no-such-option");

        Assert.Equal(ExitCodes.UsageError, exitCode);
    }
}
