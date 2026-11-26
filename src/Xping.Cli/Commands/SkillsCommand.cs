// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;

namespace Xping.Cli.Commands;

/// <summary><c>xping skills install</c>: install coding-agent skills that call <c>xping check --local</c>.</summary>
/// <remarks>TODO(spec): skill source (xping-dev/skills), target layout per agent.</remarks>
internal static class SkillsCommand
{
    public static Command Create(CliContext context)
    {
        var path = new Option<string>("--path")
        {
            Description = "Directory to install skills into. Default: the agent's skills folder in this repository.",
        };

        var install = new Command("install", "Install Xping agent skills into this repository.")
        {
            path, CommonOptions.DryRun(),
        };
        install.SetAction(_ => NotImplemented.Report(context, "skills install"));

        return new Command("skills", "Manage coding-agent skills.") { install };
    }
}
