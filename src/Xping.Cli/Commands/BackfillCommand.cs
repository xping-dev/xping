// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping backfill</c>: import past test results from the CI provider's API.</summary>
/// <remarks>TODO(spec): GitHub artifacts, GitLab JUnit reports, Azure DevOps Test Results API.</remarks>
internal static class BackfillCommand
{
    public static Command Create(CliContext context)
    {
        var since = new Option<DateTimeOffset?>("--since")
        {
            Description = "Import runs that started on or after this date (ISO 8601).",
        };
        var maxRuns = new Option<int?>("--max-runs")
        {
            Description = "Import at most this many runs.",
        };

        var command = new Command("backfill", "Import past default-branch test results from the CI provider into history.")
        {
            since, maxRuns, CommonOptions.HistoryRef(), CommonOptions.DryRun(), CommonOptions.Config(),
        };
        command.SetAction(_ => NotImplemented.Report(context, "backfill"));
        return command;
    }
}
