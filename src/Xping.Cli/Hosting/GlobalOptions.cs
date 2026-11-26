// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Cli.Hosting;

/// <summary>Options that apply to every command, filled in once the command line is parsed.</summary>
internal sealed class GlobalOptions
{
    /// <summary>Whether <c>--verbose</c> was given: diagnostics go to standard error.</summary>
    public bool Verbose { get; set; }
}
