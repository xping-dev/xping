// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Xping.Core;

namespace Xping.Cli.Commands;

/// <summary>The action of every command that is parsed but not built yet.</summary>
internal static class NotImplemented
{
    public static int Report(CliContext context, string command)
    {
        context.Error.WriteLine(
            $"xping {command}: not implemented yet. Exit code {ExitCodes.NotImplemented} means the command exists but does nothing so far.");
        return ExitCodes.NotImplemented;
    }
}
