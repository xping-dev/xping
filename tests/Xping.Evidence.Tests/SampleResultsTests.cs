// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Xml.Linq;
using Xping.Testing;

namespace Xping.Evidence.Tests;

// TODO(spec): replace with importer tests once the JUnit and TRX importers exist.
public sealed class SampleResultsTests
{
    [Theory]
    [InlineData("basic.junit.xml", "testsuites")]
    [InlineData("basic.trx", "TestRun")]
    public void TheSampleReportsAreWellFormed(string file, string root)
    {
        var document = XDocument.Load(RepositoryPaths.Combine("samples", "results", file));

        Assert.Equal(root, document.Root!.Name.LocalName);
    }

    [Fact]
    public void JUnitAndTrxAreKnownFormats()
    {
        Assert.True(Enum.IsDefined(EvidenceFormat.JUnit));
        Assert.True(Enum.IsDefined(EvidenceFormat.Trx));
    }
}
