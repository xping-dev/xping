// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Xping.Shared;

namespace Xping.Session.Models;

/// <summary>What wrote a session file.</summary>
/// <param name="Name">For example <c>xping-sdk-dotnet</c>, <c>xping-import-junit</c>.</param>
/// <param name="Version">The producer's version.</param>
public sealed record SessionProducer(string Name, string Version)
{
    /// <summary>The producer name of sessions recorded by the Xping .NET SDK.</summary>
    public const string SdkDotnet = "xping-sdk-dotnet";

    /// <summary>The default: this library, at its own version.</summary>
    public static SessionProducer ThisLibrary { get; } = new("xping-session", XpingVersion.Current);
}
