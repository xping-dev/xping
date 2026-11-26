/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using Xping.Core.Triage;
using Xping.Core.Triage.Model;
using Xping.Session.Store;

namespace Xping.Core.Tests.Triage;

/// <summary>
/// The runner and project come from what the SDK recorded, and nothing it left out is filled in.
/// </summary>
public sealed class ReproTargetTests
{
    [Fact]
    public void TheRecordedRunnerAndProjectAreRead()
    {
        var session = TestSessionFactory.Session(
            0,
            [],
            customProperties: new Dictionary<string, string>
            {
                [LocalSessionProperties.Runner] = LocalSessionProperties.TestingPlatformRunner,
                [LocalSessionProperties.ProjectPath] = "tests/Shop.Tests/Shop.Tests.csproj"
            });

        Assert.Equal(
            new ReproTarget(LocalSessionProperties.TestingPlatformRunner, "tests/Shop.Tests/Shop.Tests.csproj"),
            ReproTarget.From(session));
    }

    [Fact]
    public void ASessionThatRecordedNeitherHasNoTarget()
    {
        Assert.Equal(ReproTarget.Unknown, ReproTarget.From(TestSessionFactory.Session(0, [])));
    }

    [Fact]
    public void AnEmptyWindowHasNoTarget()
    {
        Assert.Equal(ReproTarget.Unknown, ReproTarget.From(null));
    }

    [Fact]
    public void ABlankValueCountsAsNotRecorded()
    {
        var session = TestSessionFactory.Session(
            0,
            [],
            customProperties: new Dictionary<string, string>
            {
                [LocalSessionProperties.Runner] = LocalSessionProperties.VSTestRunner,
                [LocalSessionProperties.ProjectPath] = " "
            });

        Assert.Equal(new ReproTarget(LocalSessionProperties.VSTestRunner, null), ReproTarget.From(session));
    }
}
