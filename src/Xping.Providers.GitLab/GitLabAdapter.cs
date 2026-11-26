// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.GitLab;

/// <summary>The GitLab provider adapter.</summary>
/// <remarks>TODO(spec): detection, PR context and comments (Phase 3 migration).</remarks>
public sealed class GitLabAdapter : ICiProviderAdapter
{
    /// <inheritdoc />
    public CiProvider Provider => CiProvider.GitLab;
}
