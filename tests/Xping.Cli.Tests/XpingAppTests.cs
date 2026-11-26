// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Xping.Core;

namespace Xping.Cli.Tests;

public sealed class XpingAppTests
{
    private static (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exitCode = XpingApp.Run(args, output, error);
        return (exitCode, output.ToString(), error.ToString());
    }

    [Theory]
    [InlineData("check")]
    [InlineData("check", "--local", "--format", "json")]
    [InlineData("check", "--results", "a.xml", "b.trx", "--base", "main")]
    [InlineData("record", "--results", "TestResults/*.trx", "--no-push")]
    [InlineData("compact", "--retain-days", "90")]
    [InlineData("backfill", "--since", "2026-11-01", "--max-runs", "50")]
    [InlineData("init", "--provider", "azure-devops", "--dry-run")]
    [InlineData("fingerprint", "add", "src", "tests", "--dry-run")]
    [InlineData("skills", "install", "--path", ".claude/skills")]
    public void EveryCommandParsesItsOptionsAndReportsNotImplemented(params string[] args)
    {
        var (exitCode, _, error) = Run(args);

        Assert.Equal(ExitCodes.NotImplemented, exitCode);
        Assert.Contains("not implemented", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("fingerprint")]
    [InlineData("skills")]
    [InlineData("check", "--format", "xml")]
    [InlineData("check", "--no-such-option")]
    [InlineData("compact", "--retain-days", "many")]
    [InlineData("init", "--provider", "jenkins")]
    [InlineData("frobnicate")]
    public void ABadCommandLineIsAUsageError(params string[] args)
    {
        var (exitCode, _, error) = Run(args);

        Assert.Equal(ExitCodes.UsageError, exitCode);
        Assert.Contains("--help` for usage.", error, StringComparison.Ordinal);
    }

    [Fact]
    public void HelpListsEveryCommand()
    {
        var (exitCode, output, _) = Run("--help");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.All(
            ["check", "record", "compact", "backfill", "init", "fingerprint", "skills"],
            command => Assert.Contains("  " + command + " ", output, StringComparison.Ordinal));
    }

    [Fact]
    public void VersionPrintsTheRepositoryVersion()
    {
        var (exitCode, output, _) = Run("--version");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.StartsWith("2.0.0-alpha.1", output, StringComparison.Ordinal);
    }
}
