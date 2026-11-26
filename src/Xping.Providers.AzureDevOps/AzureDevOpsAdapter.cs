// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.AzureDevOps;

/// <summary>The AzureDevOps provider adapter.</summary>
/// <remarks>TODO(spec): detection, PR context and comments (Phase 3 migration).</remarks>
public sealed class AzureDevOpsAdapter : ICiProviderAdapter
{
    /// <inheritdoc />
    public CiProvider Provider => CiProvider.AzureDevOps;
}
