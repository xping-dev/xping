// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;
using System.CommandLine.Invocation;
using Xping.Cli.Commands;
using Xping.Core;

namespace Xping.Cli;

/// <summary>Builds the command tree and runs one invocation.</summary>
internal static class XpingApp
{
    internal const string Description =
        "Xping detects when tests are weakened and backs findings with execution history kept in your own repository.";

    public static RootCommand BuildRootCommand(CliContext context)
    {
        var root = new RootCommand(Description);
        root.Subcommands.Add(CheckCommand.Create(context));
        root.Subcommands.Add(RecordCommand.Create(context));
        root.Subcommands.Add(CompactCommand.Create(context));
        root.Subcommands.Add(BackfillCommand.Create(context));
        root.Subcommands.Add(InitCommand.Create(context));
        root.Subcommands.Add(FingerprintCommand.Create(context));
        root.Subcommands.Add(SkillsCommand.Create(context));
        return root;
    }

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        var context = new CliContext(output, error);
        ParseResult parseResult = BuildRootCommand(context).Parse(args);

        // System.CommandLine exits 1 on a parse error, which is the "blocked" code here.
        if (parseResult.Action is ParseErrorAction)
        {
            foreach (var parseError in parseResult.Errors)
                error.WriteLine($"xping: {parseError.Message}");

            error.WriteLine("Run 'xping --help' for usage.");
            return ExitCodes.UsageError;
        }

        var invocation = new InvocationConfiguration
        {
            Output = output,
            Error = error,
            EnableDefaultExceptionHandler = false,
        };

        try
        {
            return parseResult.Invoke(invocation);
        }
        catch (Exception ex)
        {
            error.WriteLine($"xping: internal error: {ex.Message}");
            return ExitCodes.InternalError;
        }
    }
}
