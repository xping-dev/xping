// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.GitHub;

/// <summary>The GitHub provider adapter.</summary>
/// <remarks>TODO(spec): detection, PR context and comments (Phase 3 migration).</remarks>
public sealed class GitHubAdapter : ICiProviderAdapter
{
    /// <inheritdoc />
    public CiProvider Provider => CiProvider.GitHub;
}
