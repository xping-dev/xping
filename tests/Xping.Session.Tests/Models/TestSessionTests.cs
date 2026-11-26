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
    // Schema version and producer — parameterless constructor (JSON deserialization path)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_DefaultParameterless_WritesTheCurrentSchemaAsThisLibrary()
    {
        var session = new TestSession();

        Assert.Equal(SessionSchema.CurrentVersion, session.SchemaVersion);
        Assert.Equal(new SessionProducer("xping-session", XpingVersion.Current), session.Producer);
    }

    // ---------------------------------------------------------------------------
    // Schema version and producer — builder-constructed session (normal creation path)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Build_WithExecution_WritesTheCurrentSchemaAsThisLibrary()
    {
        var session = new TestSessionBuilder()
            .AddExecution(
                new TestExecutionBuilder()
                    .WithTestName("SampleTest")
                    .WithOutcome(TestOutcome.Passed)
                    .Build())
            .Build();

        Assert.Equal(SessionSchema.CurrentVersion, session.SchemaVersion);
        Assert.Equal(new SessionProducer("xping-session", XpingVersion.Current), session.Producer);
    }
}
