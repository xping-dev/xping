// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping record</c>: append a default-branch run to history.</summary>
/// <remarks>TODO(spec): one immutable run file per run, CI environment capture, pull --rebase + retry on push.</remarks>
internal static class RecordCommand
{
    public static Command Create(CliContext context)
    {
        var noPush = new Option<bool>("--no-push")
        {
            Description = "Write the run file to the history ref locally but do not push it.",
        };

        var command = new Command("record", "Append this default-branch run to execution history.")
        {
            CommonOptions.Results(), CommonOptions.HistoryRef(), noPush, CommonOptions.Config(),
        };
        command.SetAction(_ => NotImplemented.Report(context, "record"));
        return command;
    }
}
