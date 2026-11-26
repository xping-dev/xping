// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/PullRequestReports/Rendering/Dialects/GitHubMarkdownDialect.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using Xping.Providers.Comments;

namespace Xping.Providers.GitHub;

/// <summary>GitHub-flavoured Markdown, as GitHub renders it.</summary>
internal sealed class GitHubMarkdownDialect : GfmMarkdownDialect
{
    public override CiProvider Provider => CiProvider.GitHub;
}
