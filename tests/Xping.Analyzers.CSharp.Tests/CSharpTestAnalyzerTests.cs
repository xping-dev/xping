// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Analyzers.CSharp.Tests;

// TODO(spec): rule fixtures once the Roslyn analyzer exists.
public sealed class CSharpTestAnalyzerTests
{
    [Fact]
    public void ReadsCSharpFiles() => Assert.Contains(".cs", CSharpTestAnalyzer.FileExtensions);
}
