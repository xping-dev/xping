// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Xping.Core.Triage;
using Xping.Core.Triage.Model;
using Xping.Core.Triage.Providers;
using Xping.Session.Models.Executions;

namespace Xping.Core.Tests.Triage;

/// <summary>
/// Pins the names a subject is presented under.
/// </summary>
/// <remarks>
/// The adapter shapes here are recorded facts, not invented ones: the three frameworks build a
/// display name differently and one of them puts the argument list in the qualified name. A rule
/// tested against one of them is a rule that silently loses the other two.
/// </remarks>
public sealed class SubjectNamesTests
{
    /// <summary>
    /// The namespace goes and the class stays. A method name alone is ambiguous across a suite.
    /// </summary>
    [Fact]
    public void AQualifiedNameKeepsItsClassAndDropsItsNamespace() =>
        Assert.Equal(
            "SampleTests.Add",
            TestNames.ShortName("SampleApp.XUnit.SampleTests.Add", "Add"));

    /// <summary>
    /// A nested class arrives already encoded, and the encoding is what a filter accepts.
    /// </summary>
    [Fact]
    public void ANestedClassKeepsTheEncodingItsQualifiedNameCarries() =>
        Assert.Equal(
            "Outer+Inner.Add",
            TestNames.ShortName("SampleApp.XUnit.Outer+Inner.Add", "Add"));

    /// <summary>
    /// A qualified name shorter than two segments is kept whole rather than padded.
    /// </summary>
    [Fact]
    public void AQualifiedNameWithNoNamespaceIsKeptWhole() =>
        Assert.Equal("Add", TestNames.ShortName("Add", "Add"));

    /// <summary>
    /// NUnit records <c>test.FullName</c>, which carries the arguments already.
    /// </summary>
    /// <remarks>
    /// The decimal point is the whole reason the paren is split before the dots. Split on <c>.</c>
    /// first and the last two segments of this name are <c>5, 2)</c>, which loses the class and
    /// half the argument list at once.
    /// </remarks>
    [Fact]
    public void AQualifiedNameCarryingItsOwnArgumentsIsSplitAtTheParenFirst() =>
        Assert.Equal(
            "SampleTests.Add(1.5, 2)",
            TestNames.ShortName("SampleApp.NUnit.SampleTests.Add(1.5, 2)", "Add(1.5,2)"));

    /// <summary>
    /// xUnit prefixes a parameterised display name with the whole qualified name.
    /// </summary>
    [Fact]
    public void AnArgumentListPrefixedWithTheWholeQualifiedNameIsAppended() =>
        Assert.Equal(
            "SampleTests.Add(a: 1, b: 2)",
            TestNames.ShortName(
                "SampleApp.XUnit.SampleTests.Add", "SampleApp.XUnit.SampleTests.Add(a: 1, b: 2)"));

    /// <summary>
    /// NUnit's display name is the method segment alone.
    /// </summary>
    [Fact]
    public void AnArgumentListPrefixedWithTheMethodSegmentIsAppended() =>
        Assert.Equal(
            "SampleTests.Add(1,2)",
            TestNames.ShortName("SampleApp.NUnit.SampleTests.Add", "Add(1,2)"));

    /// <summary>
    /// MSTest writes a space in front of the parenthesis.
    /// </summary>
    /// <remarks>
    /// The space belongs to the prefix and not to the argument list, so it is trimmed before the
    /// comparison and does not reach the name. A rule matching the prefix untrimmed would have
    /// failed on MSTest alone.
    /// </remarks>
    [Fact]
    public void ASpaceBeforeTheArgumentListDoesNotStopItBeingRecognised() =>
        Assert.Equal(
            "SampleTests.Add(1,2)",
            TestNames.ShortName("SampleApp.MSTest.SampleTests.Add", "Add (1,2)"));

    /// <summary>
    /// Arguments are appended as the adapter wrote them, never normalised into one spelling.
    /// </summary>
    /// <remarks>
    /// The three runners render them differently — named in xUnit, positional in the other two — and
    /// a spelling invented here is one no runner emits and no <c>--filter</c> accepts.
    /// </remarks>
    [Fact]
    public void AnArgumentListIsAppendedExactlyAsTheAdapterWroteIt() =>
        Assert.Equal(
            "SampleTests.Add(a: \"x, y\", b: null)",
            TestNames.ShortName(
                "SampleApp.XUnit.SampleTests.Add", "Add(a: \"x, y\", b: null)"));

    /// <summary>
    /// Prose on a parameterised case names no method, so its arguments are not the method's.
    /// </summary>
    [Fact]
    public void ProseCarryingAnArgumentListContributesNothing() =>
        Assert.Equal(
            "SampleTests.Add",
            TestNames.ShortName(
                "SampleApp.XUnit.SampleTests.Add", "adds two numbers(a: 1, b: 2)"));

    /// <summary>
    /// Prose alone is not appended either. It is not an identity and there is a name already.
    /// </summary>
    [Fact]
    public void ProseAloneDoesNotReachTheName() =>
        Assert.Equal(
            "SampleTests.Add",
            TestNames.ShortName("SampleApp.XUnit.SampleTests.Add", "adds two numbers"));

    /// <summary>
    /// With no qualified name the display name is all there is, and it is better than nothing.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutAQualifiedNameTheDisplayNameStands(string? qualified) =>
        Assert.Equal("adds two numbers", TestNames.ShortName(qualified, "adds two numbers"));

    /// <summary>
    /// An unnamed test says so rather than rendering an empty line where a name belongs.
    /// </summary>
    [Fact]
    public void WithNeitherNameTheSubjectSaysItIsUnnamed() =>
        Assert.Equal("(unnamed)", TestNames.ShortName(null, null));

    private static BrokenFixtureEvidence Fixture(string? member, FailureSite site) =>
        new(
            site.ToString(),
            member,
            Signature("System.InvalidOperationException"),
            3,
            [new ClusterMember("fp", "MyApp.Tests.A", 4, 3, 20)],
            12, 3, 20, 3,
            new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            "a3f9c2e",
            []);

    private static SharedFailureEvidence Shared(string? exceptionType) =>
        new(
            Signature(exceptionType),
            3,
            [new ClusterMember("fp", "MyApp.Tests.A", 4, 3, 20)],
            12, 3, 20, 3,
            new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            "a3f9c2e",
            []);

    private static SignatureView Signature(string? exceptionType) =>
        new(
            "abc123",
            exceptionType,
            "expected <num> but was <num>",
            [],
            Degraded: false,
            Unavailable: false,
            TimedOut: false,
            Occurrences: 12,
            FirstSeenAt: new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
            FirstSeenSha: "a3f9c2e",
            FirstSeenSessionsAgo: 4,
            FirstSeenInLatestSession: false,
            FirstSeenAfterFirstRun: true,
            ErrorMessageHash: null);
}
