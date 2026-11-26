// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/PullRequestReports/Rendering/IMarkdownDialect.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>The emoji a report uses. Rendered as Unicode on every dialect.</summary>
internal enum ReportEmoji
{
    Passed,
    Failed,
    Warning,
    Running,
    Unclear
}

/// <summary>
/// A provider's Markdown. The renderer decides what to say; the dialect decides the syntax.
/// </summary>
internal interface IMarkdownDialect
{
    CiProvider Provider { get; }

    /// <summary>The longest body the renderer produces, in characters.</summary>
    int BodyBudget { get; }

    /// <summary>
    /// A collapsible block, or a bold summary followed by the body where there is none. The summary is plain
    /// text; the body is wrapped in blank lines.
    /// </summary>
    string Details(string summary, string body);

    /// <summary>A hard line break inside a paragraph, with the newline after it.</summary>
    string LineBreak();

    /// <summary>Text safe inside a table cell: <c>|</c> escaped, newlines flattened.</summary>
    string EscapeCell(string text);

    string Bold(string text);

    string InlineCode(string text);

    /// <summary>A link whose text has every Markdown punctuation character escaped.</summary>
    string Link(string text, string url);

    /// <summary>Small print, as <c>&lt;sub&gt;</c> or the dialect's nearest equivalent.</summary>
    string Small(string text);

    string Emoji(ReportEmoji e);
}
