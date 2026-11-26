/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using System.Reflection;
using Xping.Session.Models;
using Xping.Session.Models.Builders;
using Xping.Session.Models.Executions;

namespace Xping.Session.Tests.Models.Executions;

public sealed class TestExecutionTests
{
    [Fact]
    public void WithoutStackTrace_DropsOnlyTheTraceAndMarksItOmitted()
    {
        TestExecution execution = new TestExecutionBuilder()
            .WithTestName("Fails")
            .WithOutcome(TestOutcome.Failed)
            .WithIdentity(new TestIdentity { Assembly = "MyApp.Tests", FullyQualifiedName = "MyApp.Tests.Fails" })
            .WithException("System.InvalidOperationException", "boom", "   at MyApp.Tests.Fails()")
            .WithErrorMessageHash("message-hash")
            .WithStackTraceHash("trace-hash")
            .WithTimeoutBudget(TimeSpan.FromSeconds(5), TimeoutBudgetSource.Declared)
            .WithFailureSite(FailureSite.TestBody, "Fails")
            .Build();

        TestExecution copy = execution.WithoutStackTrace();

        Assert.Null(copy.StackTrace);
        Assert.True(copy.StackTraceOmitted);

        // Every other property is carried over, including ones added after this test was written.
        foreach (PropertyInfo property in typeof(TestExecution).GetProperties())
        {
            if (property.Name is nameof(TestExecution.StackTrace) or nameof(TestExecution.StackTraceOmitted))
                continue;

            Assert.Equal(property.GetValue(execution), property.GetValue(copy));
        }
    }

    // NUnit records a trace for Assert.Ignore and Assert.Pass; a skipped test is not a failure whose
    // trace was withheld.
    [Theory]
    [InlineData(TestOutcome.Skipped)]
    [InlineData(TestOutcome.Passed)]
    public void WithoutStackTrace_DropsATraceThatIsNotAFailuresWithoutMarkingIt(TestOutcome outcome)
    {
        TestExecution execution = new TestExecutionBuilder()
            .WithTestName("Ignored")
            .WithOutcome(outcome)
            .WithIdentity(new TestIdentity { Assembly = "MyApp.Tests", FullyQualifiedName = "MyApp.Tests.Ignored" })
            .WithException(null, "ignored", "   at NUnit.Framework.Assert.Ignore()")
            .Build();

        TestExecution copy = execution.WithoutStackTrace();

        Assert.Null(copy.StackTrace);
        Assert.False(copy.StackTraceOmitted);
    }

    [Fact]
    public void WithoutStackTrace_LeavesAnExecutionWithoutATraceAsItIs()
    {
        TestExecution execution = new TestExecutionBuilder()
            .WithTestName("Passes")
            .WithOutcome(TestOutcome.Passed)
            .WithIdentity(new TestIdentity { Assembly = "MyApp.Tests", FullyQualifiedName = "MyApp.Tests.Passes" })
            .Build();

        TestExecution copy = execution.WithoutStackTrace();

        Assert.Same(execution, copy);
        Assert.False(copy.StackTraceOmitted);
    }
}
