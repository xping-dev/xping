/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using Xping.Session.Models.Builders;
using Xping.Session.Models.Executions;
using Xping.Session.Serialization;

namespace Xping.Session.Tests.Serialization;

public sealed class XpingSerializerOptionsTests
{
    [Fact]
    public void FileOptions_ShouldBeCaseInsensitive()
    {
        Assert.True(XpingSerializerOptions.FileOptions.PropertyNameCaseInsensitive);
    }

    [Fact]
    public void FileOptions_ShouldSerializeEnumsAsStrings()
    {
        // TestOutcome is an enum — with JsonStringEnumConverter it should serialize as a string
        var obj = new { Outcome = TestOutcome.Passed };
        var json = JsonSerializer.Serialize(obj, XpingSerializerOptions.FileOptions);
        Assert.Contains("\"passed\"", json, StringComparison.Ordinal); // camelCase enum + string form
    }

    /// <summary>
    /// The wire form of an outcome is its camelCase name, so the name — not the numeric value — is
    /// what every stored session is pinned to.
    /// </summary>
    [Fact]
    public void FileOptions_TimeoutOutcome_RoundTripsAsTheStringTimeout()
    {
        var json = JsonSerializer.Serialize(
            new { Outcome = TestOutcome.Timeout }, XpingSerializerOptions.FileOptions);

        Assert.Contains("\"timeout\"", json, StringComparison.Ordinal);

        var roundTripped = JsonSerializer.Deserialize<TestOutcome>(
            "\"timeout\"", XpingSerializerOptions.FileOptions);

        Assert.Equal(TestOutcome.Timeout, roundTripped);
    }

    [Fact]
    public void FileOptions_TestExecutionWithTimeoutBudget_RoundTripsBudgetAndSource()
    {
        var execution = new TestExecutionBuilder()
            .WithTestName("HangsForever")
            .WithOutcome(TestOutcome.Timeout)
            .WithDuration(TimeSpan.FromMilliseconds(5000))
            .WithTimeoutBudget(TimeSpan.FromMilliseconds(5000), TimeoutBudgetSource.Declared)
            .Build();

        var json = JsonSerializer.Serialize(execution, XpingSerializerOptions.FileOptions);
        var roundTripped = JsonSerializer.Deserialize<TestExecution>(json, XpingSerializerOptions.FileOptions);

        Assert.Equal(TestOutcome.Timeout, roundTripped!.Outcome);
        Assert.Equal(TimeSpan.FromMilliseconds(5000), roundTripped.TimeoutBudget);
        Assert.Equal(TimeoutBudgetSource.Declared, roundTripped.TimeoutBudgetSource);
    }

    // ---------------------------------------------------------------------------
    // FileOptions
    // ---------------------------------------------------------------------------

    [Fact]
    public void FileOptions_ShouldUseCamelCaseNaming()
    {
        Assert.Equal(JsonNamingPolicy.CamelCase, XpingSerializerOptions.FileOptions.PropertyNamingPolicy);
    }

    [Fact]
    public void FileOptions_ShouldNotWriteIndented()
    {
        Assert.False(XpingSerializerOptions.FileOptions.WriteIndented);
    }

    [Fact]
    public void FileOptions_ShouldIgnoreNullWhenWriting()
    {
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, XpingSerializerOptions.FileOptions.DefaultIgnoreCondition);
    }

    [Fact]
    public void FileOptions_ShouldBeSameInstance_OnMultipleAccesses()
    {
        Assert.Same(XpingSerializerOptions.FileOptions, XpingSerializerOptions.FileOptions);
    }

    // ---------------------------------------------------------------------------
    // TestOptions
    // ---------------------------------------------------------------------------

    [Fact]
    public void TestOptions_ShouldWriteIndented()
    {
        Assert.True(XpingSerializerOptions.TestOptions.WriteIndented);
    }

    [Fact]
    public void TestOptions_ShouldUseCamelCaseNaming()
    {
        Assert.Equal(JsonNamingPolicy.CamelCase, XpingSerializerOptions.TestOptions.PropertyNamingPolicy);
    }

    [Fact]
    public void TestOptions_ShouldBeSameInstance_OnMultipleAccesses()
    {
        Assert.Same(XpingSerializerOptions.TestOptions, XpingSerializerOptions.TestOptions);
    }

    // ---------------------------------------------------------------------------
    // Null-suppression verification (integration)
    // ---------------------------------------------------------------------------

    [Fact]
    public void FileOptions_ShouldOmitNullProperties_InSerializedOutput()
    {
        var obj = new { Name = "test", Optional = (string?)null };
        var json = JsonSerializer.Serialize(obj, XpingSerializerOptions.FileOptions);
        Assert.DoesNotContain("optional", json, StringComparison.Ordinal);
        Assert.Contains("\"name\"", json, StringComparison.Ordinal);
    }
}
