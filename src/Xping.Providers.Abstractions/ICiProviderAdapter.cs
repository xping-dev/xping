// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers;

/// <summary>One CI provider: environment detection, PR context and PR comments.</summary>
/// <remarks>
/// TODO(spec): environment/PR detection (from sdk-dotnet) and the comment client contract
/// (from dashboard) land here in Phase 3. Tokens come only from the CI job's environment.
/// </remarks>
public interface ICiProviderAdapter
{
    /// <summary>The provider this adapter handles.</summary>
    CiProvider Provider { get; }
}
