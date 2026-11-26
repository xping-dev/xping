// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Session;

/// <summary>
/// Facts about the canonical session schema (<c>schema/session.schema.json</c>).
/// </summary>
/// <remarks>
/// TODO(spec): the session model, serializer and local store move here from xping-dev/sdk-dotnet
/// in Phase 3 of docs/specs/migration-plan.md. Files written before v1 carry no
/// <c>schemaVersion</c>; they are read as version 0 through an importer, never rejected.
/// </remarks>
public static class SessionSchema
{
    /// <summary>The schema version this library writes.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The version assigned to session files that predate <c>schemaVersion</c>.</summary>
    public const int LegacyVersion = 0;
}
