// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Analyzers.CSharp;

/// <summary>Reads static facts about C# tests from source.</summary>
/// <remarks>
/// TODO(spec): Roslyn syntax-tree analysis of changed test files (assertions, timeouts, skips,
/// mocks, <c>[TestFingerprint]</c> matched by attribute name). No MSBuild workspace load.
/// </remarks>
public static class CSharpTestAnalyzer
{
    /// <summary>File extensions this analyzer reads.</summary>
    public static IReadOnlyList<string> FileExtensions { get; } = [".cs"];
}
