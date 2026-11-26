// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Analyzers.TreeSitter.Tests;

// TODO(spec): per-language query fixtures once languages are supported.
public sealed class TreeSitterTestAnalyzerTests
{
    [Fact]
    public void SupportsNoLanguageYet() => Assert.Empty(TreeSitterTestAnalyzer.FileExtensions);
}
