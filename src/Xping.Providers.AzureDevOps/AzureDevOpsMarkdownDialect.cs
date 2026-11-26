// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/PullRequestReports/Rendering/Dialects/AzureDevOpsMarkdownDialect.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using Xping.Providers.Comments;

namespace Xping.Providers.AzureDevOps;

/// <summary>
/// Azure Repos Markdown: GFM tables, <c>&lt;details&gt;</c> and <c>&lt;sub&gt;</c> (probe 2026-11-01, PR comment
/// content spec §8). A newline is already a hard break, and a summary renders literally, so it stays plain text. A
/// comment holds at most 150,000 characters, well above the 60,000 budget.
/// </summary>
internal sealed class AzureDevOpsMarkdownDialect : GfmMarkdownDialect
{
    public override CiProvider Provider => CiProvider.AzureDevOps;

    public override string LineBreak() => "\n";
}
