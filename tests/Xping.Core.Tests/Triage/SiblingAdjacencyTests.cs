/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using Xping.Core.Triage;
using Xping.Core.Triage.Model;

namespace Xping.Core.Tests.Triage;

/// <summary>
/// Two findings about one test are two findings, and the report has to say so.
/// </summary>
/// <remarks>
/// The same test used to appear at rank 4 and rank 5 with nothing connecting the two, and on a
/// longer report at ranks four apart. The ranking between distinct tests is untouched by all of
/// this: only findings that name a test already named move, and they move only as far as the last
/// finding naming it.
/// </remarks>
public sealed class SiblingAdjacencyTests
{
    /// <summary>
    /// Two clusters sharing nothing are left where the ranking put them.
    /// </summary>
    [Fact]
    public void TwoClustersAreNeverGroupedTogether()
    {
        IReadOnlyList<Finding> ordered = FindingOrder.WithSiblingsAdjacent(
        [
            Group("f_1", "fp-Alpha"),
            Test("f_2", "fp-Beta"),
            Group("f_3", "fp-Gamma")
        ]);

        Assert.Equal(["f_1", "f_2", "f_3"], ordered.Select(f => f.Id));
    }

    private static Finding Test(string id, string fingerprint) =>
        Build(id, new FindingSubject.SingleTest(Reference(fingerprint)));

    private static Finding Group(string id, params string[] fingerprints) =>
        Build(id, new FindingSubject.Group($"sig_{id}", [.. fingerprints.Select(Reference)]));

    private static Finding Build(string id, FindingSubject subject) =>
        new(
            id,
            FindingKind.Flaky,
            Severity.High,
            EvidenceLevel.Adequate,
            10,
            subject,
            new StubEvidence(),
            0.5);

    private static TestReference Reference(string fingerprint) =>
        new(fingerprint, $"MyApp.Tests.SampleTests.{fingerprint}", fingerprint, null, null, "MyApp.Tests");

    private sealed record StubEvidence : FindingEvidence;
}
