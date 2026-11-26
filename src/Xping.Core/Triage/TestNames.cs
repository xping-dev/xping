// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/sdk-dotnet@0a1a6e5 src/Xping.Cli/Report/Contract/SubjectNames.cs
// (ShortName only; the rest of Contract/ was the old report envelope and is not migrated).

namespace Xping.Core.Triage;

/// <summary>Short, human-readable test names.</summary>
internal static class TestNames
{
    private const string Unnamed = "(unnamed)";

    /// <summary>
    /// The last two segments of the fully qualified name (<c>Class.Method</c>), plus the arguments of
    /// a parameterized case taken from the name or, failing that, from the display name.
    /// </summary>
    public static string ShortName(string? fullyQualifiedName, string? displayName)
    {
        if (fullyQualifiedName is not { Length: > 0 } qualified)
            return displayName is { Length: > 0 } recorded ? recorded : Unnamed;

        int paren = qualified.IndexOf('(', StringComparison.Ordinal);
        string name = paren < 0 ? qualified : qualified[..paren];
        string arguments = paren < 0 ? string.Empty : qualified[paren..];

        if (arguments.Length == 0 && displayName is { Length: > 0 } display)
            arguments = ArgumentsOf(display, LastSegment(name), qualified);

        return LastTwoSegments(name) + arguments;
    }

    private static string ArgumentsOf(string displayName, string method, string qualified)
    {
        int paren = displayName.IndexOf('(', StringComparison.Ordinal);
        if (paren < 0)
            return string.Empty;

        string prefix = displayName[..paren].TrimEnd();

        return string.Equals(prefix, method, StringComparison.Ordinal) ||
               string.Equals(prefix, qualified, StringComparison.Ordinal)
            ? displayName[paren..]
            : string.Empty;
    }

    private static string LastSegment(string name)
    {
        int last = name.LastIndexOf('.');
        return last < 0 ? name : name[(last + 1)..];
    }

    private static string LastTwoSegments(string name)
    {
        int last = name.LastIndexOf('.');
        if (last <= 0)
            return name;

        int previous = name.LastIndexOf('.', last - 1);
        return previous < 0 ? name : name[(previous + 1)..];
    }
}
