// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text.Json;
using System.Text.Json.Nodes;
using Xping.Session.Models;

namespace Xping.Session.Serialization;

/// <summary>
/// Reads a session document of any known schema version into the current model.
/// </summary>
/// <remarks>
/// A document without <c>schemaVersion</c> is the pre-v1 format and goes through
/// <see cref="LegacySessionImporter"/>. A version newer than <see cref="SessionSchema.CurrentVersion"/>
/// is refused rather than guessed at.
/// </remarks>
public static class SessionDocument
{
    /// <summary>Reads <paramref name="json"/> into a session.</summary>
    /// <exception cref="UnsupportedSchemaVersionException">The document is newer than this library.</exception>
    /// <exception cref="JsonException">The document is not a session.</exception>
    public static TestSession? Read(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject document)
            throw new JsonException("A session document must be a JSON object.");

        int version = VersionOf(document);
        if (version > SessionSchema.CurrentVersion)
            throw new UnsupportedSchemaVersionException(version);

        if (version == SessionSchema.LegacyVersion)
            LegacySessionImporter.Upgrade(document);

        return document.Deserialize<TestSession>(XpingSerializerOptions.FileOptions);
    }

    private static int VersionOf(JsonObject document)
    {
        foreach (var property in document)
        {
            if (!string.Equals(property.Key, "schemaVersion", StringComparison.OrdinalIgnoreCase))
                continue;

            return property.Value is JsonValue value && value.TryGetValue(out int version) && version >= 0
                ? version
                : throw new JsonException("schemaVersion must be a non-negative integer.");
        }

        return SessionSchema.LegacyVersion;
    }
}

/// <summary>A session document declares a schema version newer than this library knows.</summary>
public sealed class UnsupportedSchemaVersionException : Exception
{
    /// <summary>Creates the exception.</summary>
    public UnsupportedSchemaVersionException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    public UnsupportedSchemaVersionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    public UnsupportedSchemaVersionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception for <paramref name="version"/>.</summary>
    public UnsupportedSchemaVersionException(int version)
        : base($"Session schema version {version} is newer than {SessionSchema.CurrentVersion}, the newest this version of Xping reads. Upgrade Xping.")
    {
        Version = version;
    }

    /// <summary>The version the document declared.</summary>
    public int Version { get; }
}
