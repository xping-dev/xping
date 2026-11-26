// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping init</c>: set Xping up in a repository.</summary>
/// <remarks>TODO(spec): CI steps, .xping.yml, AGENTS.md rules, fingerprint attribute, then <c>fingerprint add</c>.</remarks>
internal static class InitCommand
{
    public static Command Create(CliContext context)
    {
        var provider = new Option<string>("--provider")
        {
            Description = "CI provider to write pipeline steps for. Default: detected from the repository.",
        };
        provider.AcceptOnlyFromAmong("github", "gitlab", "azure-devops");
        var force = new Option<bool>("--force")
        {
            Description = "Overwrite files Xping created before.",
        };

        var command = new Command("init", "Add Xping CI steps, .xping.yml, AGENTS.md rules and test fingerprints to this repository.")
        {
            provider, CommonOptions.DryRun(), force,
        };
        command.SetAction(_ => NotImplemented.Report(context, "init"));
        return command;
    }
}
