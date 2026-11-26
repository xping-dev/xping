/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using Xping.Session.Models;
using Xping.Session.Models.Builders;
using Xping.Session.Models.Executions;
using Xping.Shared;

namespace Xping.Session.Tests.Models;

public sealed class TestSessionTests
{
    // ---------------------------------------------------------------------------
    // SdkVersion — parameterless constructor (JSON deserialization path)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_DefaultParameterless_SdkVersionMatchesXpingSdkVersionCurrent()
    {
        var session = new TestSession();

        Assert.Equal(XpingVersion.Current, session.SdkVersion);
    }

    // ---------------------------------------------------------------------------
    // SdkVersion — builder-constructed session (normal creation path)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Build_WithExecution_SdkVersionMatchesXpingSdkVersionCurrent()
    {
        var session = new TestSessionBuilder()
            .AddExecution(
                new TestExecutionBuilder()
                    .WithTestName("SampleTest")
                    .WithOutcome(TestOutcome.Passed)
                    .Build())
            .Build();

        Assert.Equal(XpingVersion.Current, session.SdkVersion);
    }
}
