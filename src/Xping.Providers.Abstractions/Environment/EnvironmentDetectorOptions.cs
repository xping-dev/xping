// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.Environment;

/// <summary>
/// The settings environment detection reads. Replaces the SDK's <c>XpingConfiguration</c>, which
/// also carried Cloud settings the CLI must not have.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "TODO(spec): xping record builds it from .xping.yml; until then only tests do.")]
internal sealed record EnvironmentDetectorOptions
{
    /// <summary>The environment name recorded when none is configured.</summary>
    public const string DefaultEnvironment = "Default";

    /// <summary>The environment name to record, e.g. <c>Staging</c>. Blank means <see cref="DefaultEnvironment"/>.</summary>
    public string? Environment { get; init; }

    /// <summary>Whether to detect the pull/merge request the run belongs to.</summary>
    public bool EnablePullRequestDetection { get; init; } = true;

    /// <summary>Whether to record the local Git author. Off by default: it is personal data.</summary>
    public bool CollectLocalGitAuthor { get; init; }

    /// <summary>Whether to record the CI actor's email address. Off by default: it is personal data.</summary>
    public bool CollectCiActorEmail { get; init; }

    /// <summary>The default branch, overriding the one detected from the CI provider.</summary>
    public string? DefaultBranch { get; init; }

    /// <summary>The environment to record: the configured one, or <see cref="DefaultEnvironment"/>.</summary>
    public string ResolvedEnvironment =>
        string.IsNullOrWhiteSpace(Environment) ? DefaultEnvironment : Environment!.Trim();
}
