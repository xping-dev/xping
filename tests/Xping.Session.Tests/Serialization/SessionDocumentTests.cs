// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Reflection;
using System.Text.Json;
using Xping.Session.Models;
using Xping.Session.Models.Builders;
using Xping.Session.Models.Executions;
using Xping.Session.Serialization;
using Xping.Testing;

namespace Xping.Session.Tests.Serialization;

public sealed class SessionDocumentTests
{
    private static string LegacyFixture()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "Xping.Session.Tests.Serialization.TestData.afbba725-d460-4b9f-88c6-a39c040e85aa.json")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static TestSession Session() => new TestSessionBuilder()
        .WithSessionId(Guid.Parse("11111111-2222-3333-4444-555555555555"))
        .AddExecution(new TestExecutionBuilder().WithTestName("T").WithOutcome(TestOutcome.Passed).Build())
        .Build();

    [Fact]
    public void AnEarlySdkFileWithoutASchemaVersionStillReads()
    {
        TestSession session = SessionDocument.Read(LegacyFixture())!;

        Assert.Equal(Guid.Parse("dc6cdcdc-8567-439b-8a1d-d86fad27d05e"), session.SessionId);
        Assert.Equal(SessionSchema.CurrentVersion, session.SchemaVersion);
        Assert.NotEmpty(session.Executions);
    }

    [Fact]
    public void AnEarlySdkFileKeepsItsTestIdsAsFingerprints()
    {
        TestSession session = SessionDocument.Read(LegacyFixture())!;

        Assert.Equal(
            "d6638513377582655769dbfe8907ed8f4e3ee8aaea14b08e11c262d65f508717",
            session.Executions.First().Identity.TestFingerprint);
    }

    [Fact]
    public void ALegacySdkVersionBecomesTheProducer()
    {
        const string legacy = """
            {"assemblies":["A.Tests"],"sessionId":"11111111-2222-3333-4444-555555555555",
             "startedAt":"2026-11-01T10:00:00Z","executions":[],"sdkVersion":"1.0.0-rc.8"}
            """;

        TestSession session = SessionDocument.Read(legacy)!;

        Assert.Equal(new SessionProducer(SessionProducer.SdkDotnet, "1.0.0-rc.8"), session.Producer);
    }

    [Fact]
    public void ALegacyFileWithoutASdkVersionHasAnUnknownProducerVersion()
    {
        TestSession session = SessionDocument.Read(LegacyFixture())!;

        Assert.Equal(new SessionProducer(SessionProducer.SdkDotnet, "0.0.0"), session.Producer);
    }

    [Fact]
    public void AV1FileRoundTripsUnchanged()
    {
        TestSession original = Session();
        string json = JsonSerializer.Serialize(original, XpingSerializerOptions.FileOptions);

        TestSession read = SessionDocument.Read(json)!;

        Assert.Equal(original.SessionId, read.SessionId);
        Assert.Equal(original.Producer, read.Producer);
        Assert.Equal(SessionSchema.CurrentVersion, read.SchemaVersion);
    }

    [Fact]
    public void ANewerSchemaVersionIsRefusedNotGuessed()
    {
        string json = """{"schemaVersion":2,"sessionId":"11111111-2222-3333-4444-555555555555","executions":[]}""";

        var ex = Assert.Throws<UnsupportedSchemaVersionException>(() => SessionDocument.Read(json));
        Assert.Equal(2, ex.Version);
    }

    [Theory]
    [InlineData("""{"schemaVersion":"one"}""")]
    [InlineData("""{"schemaVersion":-1}""")]
    [InlineData("[]")]
    public void AMalformedDocumentIsAJsonError(string json) =>
        Assert.ThrowsAny<JsonException>(() => SessionDocument.Read(json));

    [Fact]
    public void AssembliesStaysTheFirstPropertyAndSchemaVersionFollows()
    {
        string json = JsonSerializer.Serialize(Session(), XpingSerializerOptions.FileOptions);

        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.EnumerateObject().Select(p => p.Name).Take(2).ToArray();

        Assert.Equal(["assemblies", "schemaVersion"], names);
    }

    [Fact]
    public void AWrittenSessionHasEveryPropertyTheSchemaRequires()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Combine("schema", "session.schema.json")));
        string json = JsonSerializer.Serialize(Session(), XpingSerializerOptions.FileOptions);
        using var document = JsonDocument.Parse(json);

        foreach (JsonElement required in schema.RootElement.GetProperty("required").EnumerateArray())
            Assert.True(document.RootElement.TryGetProperty(required.GetString()!, out _), $"missing {required}");
    }

    [Fact]
    public void TheSchemaFileDescribesEveryPropertyAWrittenSessionHas()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Combine("schema", "session.schema.json")));
        var declared = schema.RootElement.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet();
        string json = JsonSerializer.Serialize(Session(), XpingSerializerOptions.FileOptions);
        using var document = JsonDocument.Parse(json);

        Assert.All(document.RootElement.EnumerateObject(), property => Assert.Contains(property.Name, declared));
    }
}
