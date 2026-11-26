// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xping.Cli.Hosting;
using Xping.Core;

namespace Xping.Cli.Tests;

/// <summary>How the host behaves around any command: help, version, failures, Ctrl+C.</summary>
public sealed class ProgramTests
{
    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int code = XpingApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void NoArgumentsPrintsHelpAndIsAUsageError()
    {
        var (code, output, _) = Run();

        Assert.Equal(ExitCodes.UsageError, code);
        Assert.Contains(XpingApp.Description, output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("help")]
    public void HelpPrintsUsageAndSucceeds(string arg)
    {
        var (code, output, _) = Run(arg);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Contains(XpingApp.Description, output, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownVerbIsAUsageErrorOnStandardError()
    {
        var (code, output, error) = Run("frobnicate");

        Assert.Equal(ExitCodes.UsageError, code);
        Assert.Empty(output);
        Assert.Contains("Unrecognized command or argument", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AParseErrorPointsAtTheHelpOfTheCommandItHappenedIn()
    {
        var (_, _, error) = Run("check", "--no-such-option");

        Assert.Contains("Run `xping check --help` for usage.", error, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionPrintsCleanSemVer()
    {
        var (code, output, _) = Run("--version");
        string informational = typeof(XpingApp).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        Assert.Equal(ExitCodes.Success, code);
        Assert.Equal(informational.Split('+')[0], output.Trim());
    }

    [Fact]
    public void VersionIsNotAVerb()
    {
        var (code, _, error) = Run("version");

        Assert.Equal(ExitCodes.UsageError, code);
        Assert.Contains("Unrecognized command or argument", error, StringComparison.Ordinal);
    }

    [Fact]
    public void VerboseIsAcceptedOnEveryCommand()
    {
        var (code, _, _) = Run("fingerprint", "add", "--verbose");

        Assert.Equal(ExitCodes.NotImplemented, code);
    }

    // A failure while the services or the parser are built is still reported, not thrown.
    [Fact]
    public void AFailureWhileStartingUpIsAnInternalError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        int code = XpingApp.Run(
            ["check"],
            output,
            error,
            configureServices: services => services.AddSingleton<GlobalOptions>(
                _ => throw new InvalidOperationException("broken registration")));

        Assert.Equal(ExitCodes.InternalError, code);
        Assert.Equal("xping: internal error: broken registration" + Environment.NewLine, error.ToString());
    }

    [Fact]
    public void ASecretInAFailureMessageIsScrubbed()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        XpingApp.Run(
            ["check"],
            output,
            error,
            configureServices: services => services.AddSingleton<GlobalOptions>(
                _ => throw new InvalidOperationException("Authorization: Bearer abcdefghijkl")));

        Assert.DoesNotContain("abcdefghijkl", error.ToString(), StringComparison.Ordinal);
        Assert.Contains(Redaction.Placeholder, error.ToString(), StringComparison.Ordinal);
    }

    // Work torn down by Ctrl+C need not fail with OperationCanceledException.
    [Fact]
    public void AnyFailureAfterCtrlCIsACancel()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var output = new StringWriter();
        using var error = new StringWriter();

        int code = XpingApp.Run(
            ["check"],
            output,
            error,
            cancelKeys: new CancelKeyHandler(cancellation),
            configureServices: services => services.AddSingleton<GlobalOptions>(
                _ => throw new ObjectDisposedException("store")));

        Assert.Equal(ExitCodes.Cancelled, code);
        Assert.Equal("Cancelled." + Environment.NewLine, error.ToString());
    }
}
