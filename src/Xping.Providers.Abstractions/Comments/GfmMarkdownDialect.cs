// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/PullRequestReports/Rendering/Dialects/GfmMarkdownDialect.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>
/// GitHub-flavoured Markdown: tables, <c>&lt;details&gt;</c>, <c>&lt;sub&gt;</c>. GitHub and GitLab render it
/// alike (PR comment content spec §8.2); a provider that differs overrides it.
/// </summary>
internal abstract class GfmMarkdownDialect : IMarkdownDialect
{
    public abstract CiProvider Provider { get; }

    public int BodyBudget => 60_000;

    public string Details(string summary, string body) =>
        $"<details><summary>{summary}</summary>\n\n{body}\n\n</details>";

    public virtual string LineBreak() => "<br>\n";

    public string EscapeCell(string text) =>
        text.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');

    public string Bold(string text) => $"**{text}**";

    public string InlineCode(string text) =>
        text.Contains('`', StringComparison.Ordinal) ? $"`` {text} ``" : $"`{text}`";

    public string Link(string text, string url) => $"[{EscapeLinkText(text)}]({url})";

    public string Small(string text) => $"<sub>{text}</sub>";

    public string Emoji(ReportEmoji e) => e switch
    {
        ReportEmoji.Passed => "✅",
        ReportEmoji.Failed => "❌",
        ReportEmoji.Warning => "⚠️",
        ReportEmoji.Running => "⏳",
        ReportEmoji.Unclear => "❓",
        _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
    };

    /// <summary>
    /// Backslash before each character that could start Markdown or HTML in link text (spec §5 rule 5), so a
    /// theory argument such as <c>&lt;null&gt;</c> or <c>a_b*</c> survives on every provider.
    /// </summary>
    private static string EscapeLinkText(string text)
    {
        System.Text.StringBuilder escaped = new(text.Length + 8);
        foreach (char c in text)
        {
            if (c is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '~' or '|')
                escaped.Append('\\');
            escaped.Append(c);
        }

        return escaped.ToString();
    }
}
