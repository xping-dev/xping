// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/PullRequestReports/Rendering/Dialects/GitLabMarkdownDialect.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using Xping.Providers.Comments;

namespace Xping.Providers.GitLab;

/// <summary>GitLab-flavoured Markdown: the same tables, <c>&lt;details&gt;</c> and <c>&lt;sub&gt;</c> as GitHub (A.6).</summary>
internal sealed class GitLabMarkdownDialect : GfmMarkdownDialect
{
    public override CiProvider Provider => CiProvider.GitLab;
}
