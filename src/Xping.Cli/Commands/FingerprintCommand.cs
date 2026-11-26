// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping fingerprint add</c>: give every test without one a stable fingerprint.</summary>
/// <remarks>TODO(spec): deterministic codemod; attribute matched by name, no SDK reference.</remarks>
internal static class FingerprintCommand
{
    public static Command Create(CliContext context)
    {
        var paths = new Argument<string[]>("paths")
        {
            Description = "Files or directories to scan. Default: the current directory.",
            Arity = ArgumentArity.ZeroOrMore,
        };

        var add = new Command("add", "Add a [TestFingerprint] to every test that has none.")
        {
            paths, CommonOptions.DryRun(),
        };
        add.SetAction(_ => NotImplemented.Report(context, "fingerprint add"));

        return new Command("fingerprint", "Manage stable test fingerprints.") { add };
    }
}
