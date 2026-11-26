// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary>Options several commands share. Each call returns a new instance, because an option belongs to one command.</summary>
internal static class CommonOptions
{
    public const string DefaultHistoryRef = "xping-history";

    public static Option<string> Config() => new("--config")
    {
        Description = "Path to the Xping configuration file.",
        DefaultValueFactory = _ => ".xping.yml",
    };

    public static Option<string[]> Results() => new("--results")
    {
        Description = "Test result files or glob patterns: JUnit XML, TRX or Xping session files. Repeatable.",
        AllowMultipleArgumentsPerToken = true,
    };

    public static Option<string> HistoryRef() => new("--history-ref")
    {
        Description = "Branch or ref that holds execution history.",
        DefaultValueFactory = _ => DefaultHistoryRef,
    };

    public static Option<bool> DryRun() => new("--dry-run")
    {
        Description = "Show what would change without writing anything.",
    };
}
