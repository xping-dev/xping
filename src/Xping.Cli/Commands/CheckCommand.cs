// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping check</c>: analyse a pull/merge request, or the working tree with <c>--local</c>.</summary>
/// <remarks>TODO(spec): diff, analyzers, evidence import, history baselines, rules, policy, reporters.</remarks>
internal static class CheckCommand
{
    public static Command Create(CliContext context)
    {
        var results = CommonOptions.Results();
        var config = CommonOptions.Config();
        var historyRef = CommonOptions.HistoryRef();
        var baseRef = new Option<string>("--base")
        {
            Description = "Commit or ref to diff against. Default: detected from the CI provider.",
        };
        var local = new Option<bool>("--local")
        {
            Description = "Check the working tree on a developer or agent machine instead of a CI pull request.",
        };
        var format = new Option<string>("--format")
        {
            Description = "Output format.",
            DefaultValueFactory = _ => "text",
        };
        format.AcceptOnlyFromAmong("text", "json", "sarif");
        var output = new Option<string>("--output")
        {
            Description = "Write the report to this file instead of standard output.",
        };

        var command = new Command("check", "Check a pull/merge request (or the working tree with --local) for weakened tests.")
        {
            results, baseRef, local, format, output, config, historyRef,
        };
        command.SetAction(_ => NotImplemented.Report(context, "check"));
        return command;
    }
}
