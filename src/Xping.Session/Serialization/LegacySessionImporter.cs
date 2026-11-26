// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text.Json.Nodes;
using Xping.Session.Models;

namespace Xping.Session.Serialization;

/// <summary>
/// Upgrades a pre-v1 session document (written by Xping.Sdk.Core 1.0.0-rc.x, no
/// <c>schemaVersion</c>) to schema v1 in place.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>sdkVersion</c> becomes <c>producer: { name: "xping-sdk-dotnet", version }</c>.</item>
/// <item>Early SDK builds called the test fingerprint <c>identity.testId</c>; it becomes
/// <c>identity.testFingerprint</c>.</item>
/// <item>Everything else is already v1-shaped. Cloud-only custom properties (<c>Xping.Mode</c>,
/// <c>Xping.ProjectId</c>) are kept as the opaque strings they are.</item>
/// </list>
/// </remarks>
internal static class LegacySessionImporter
{
    private const string UnknownVersion = "0.0.0";

    public static void Upgrade(JsonObject document)
    {
        string version = Take(document, "sdkVersion") ?? UnknownVersion;
        document["schemaVersion"] = SessionSchema.CurrentVersion;
        document["producer"] = new JsonObject
        {
            ["name"] = SessionProducer.SdkDotnet,
            ["version"] = version,
        };

        if (Get(document, "executions") is not JsonArray executions)
            return;

        foreach (JsonNode? execution in executions)
        {
            if (execution is JsonObject run && Get(run, "identity") is JsonObject identity &&
                Get(identity, "testFingerprint") is null && Take(identity, "testId") is { } fingerprint)
            {
                identity["testFingerprint"] = fingerprint;
            }
        }
    }

    // The SDK reads case-insensitively, so a hand-edited file may differ in case.
    private static JsonNode? Get(JsonObject node, string name)
    {
        foreach (var property in node)
        {
            if (string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }

    private static string? Take(JsonObject node, string name)
    {
        string? key = node.Select(p => p.Key).FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
        if (key is null)
            return null;

        string? value = node[key] is JsonValue v && v.TryGetValue(out string? text) ? text : null;
        node.Remove(key);
        return value;
    }
}
