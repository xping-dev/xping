// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Analyzers.TreeSitter;

/// <summary>Reads static facts about tests in languages other than C#.</summary>
/// <remarks>TODO(spec): per-language tree-sitter queries; evaluate TreeSitter.DotNet.</remarks>
public static class TreeSitterTestAnalyzer
{
    /// <summary>File extensions this analyzer reads. Empty until a language is supported.</summary>
    public static IReadOnlyList<string> FileExtensions { get; } = [];
}
