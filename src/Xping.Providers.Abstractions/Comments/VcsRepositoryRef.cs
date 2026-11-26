// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/VcsIntegration/VcsRepositoryRef.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>
/// A repository on a provider, as a report names it. <see cref="ExternalRepositoryId"/> is the GitLab
/// project id or Azure DevOps repository GUID once resolved; null for GitHub.
/// </summary>
internal sealed record VcsRepositoryRef(CiProvider Provider, string ServerUrl, string Owner, string Name, string? ExternalRepositoryId);
