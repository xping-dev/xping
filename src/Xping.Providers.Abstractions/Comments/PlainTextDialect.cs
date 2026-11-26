// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/PullRequestReports/Rendering/Dialects/PlainTextDialect.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>
/// Text with no markup, for lines the Portal shows outside a comment (the report page's headline).
/// Not a provider's dialect: nothing publishes in it.
/// </summary>
internal sealed class PlainTextDialect : IMarkdownDialect
{
    public static PlainTextDialect Instance { get; } = new();

    public CiProvider Provider => CiProvider.None;
    public int BodyBudget => int.MaxValue;
    public string Details(string summary, string body) => $"{summary}\n\n{body}";
    public string LineBreak() => "\n";
    public string EscapeCell(string text) => text;
    public string Bold(string text) => text;
    public string InlineCode(string text) => text;
    public string Link(string text, string url) => text;
    public string Small(string text) => text;
    public string Emoji(ReportEmoji e) => string.Empty;
}
