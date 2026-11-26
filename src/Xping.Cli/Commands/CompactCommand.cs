// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping compact</c>: roll raw runs up into <c>stats/</c> and prune old ones.</summary>
/// <remarks>TODO(spec): baselines per environment, retention default.</remarks>
internal static class CompactCommand
{
    public static Command Create(CliContext context)
    {
        var retainDays = new Option<int?>("--retain-days")
        {
            Description = "Delete raw run files older than this many days after rolling them up. Default: from .xping.yml.",
        };

        var command = new Command("compact", "Roll raw history runs up into baselines and prune old runs.")
        {
            CommonOptions.HistoryRef(), retainDays, CommonOptions.DryRun(), CommonOptions.Config(),
        };
        command.SetAction(_ => NotImplemented.Report(context, "compact"));
        return command;
    }
}
