// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text.Json;
using Xping.Testing;

namespace Xping.Session.Tests;

public sealed class SessionSchemaTests
{
    [Fact]
    public void TheLibraryWritesTheVersionTheSchemaFileDeclares()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Combine("schema", "session.schema.json")));

        int declared = schema.RootElement
            .GetProperty("properties").GetProperty("schemaVersion").GetProperty("const").GetInt32();

        Assert.Equal(SessionSchema.CurrentVersion, declared);
    }

    [Fact]
    public void SchemaVersionIsRequired()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Combine("schema", "session.schema.json")));

        var required = schema.RootElement.GetProperty("required").EnumerateArray().Select(e => e.GetString());

        Assert.Contains("schemaVersion", required);
    }

    [Fact]
    public void TheLegacyVersionIsOlderThanTheCurrentOne() =>
        Assert.True(SessionSchema.LegacyVersion < SessionSchema.CurrentVersion);
}
