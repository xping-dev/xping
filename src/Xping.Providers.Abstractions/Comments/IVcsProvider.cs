// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/VcsIntegration/IVcsProvider.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>
/// One provider's comment API. Every call returns a <see cref="VcsCallResult"/>; none throws for an
/// HTTP failure.
/// </summary>
internal interface IVcsProvider
{
    CiProvider Provider { get; }

    IMarkdownDialect Dialect { get; }

    /// <summary>
    /// Fills <see cref="VcsRepositoryRef.ExternalRepositoryId"/> for providers that address
    /// repositories by id. Called once per report; GitHub returns the input.
    /// </summary>
    Task<VcsCallResult<VcsRepositoryRef>> ResolveRepositoryAsync(VcsRepositoryRef repo, VcsCredential credential, CancellationToken ct);

    Task<VcsCallResult<VcsCommentRef>> CreateCommentAsync(VcsRepositoryRef repo, int pullRequestNumber, string body, VcsCredential credential, CancellationToken ct);

    Task<VcsCallResult> UpdateCommentAsync(VcsRepositoryRef repo, int pullRequestNumber, VcsCommentRef comment, string body, VcsCredential credential, CancellationToken ct);

    /// <summary>The first comment on the pull request whose body contains <paramref name="marker"/>; null value when none.</summary>
    Task<VcsCallResult<VcsCommentRef?>> FindCommentByMarkerAsync(VcsRepositoryRef repo, int pullRequestNumber, string marker, VcsCredential credential, CancellationToken ct);

    Task<VcsCallResult> DeleteCommentAsync(VcsRepositoryRef repo, int pullRequestNumber, VcsCommentRef comment, VcsCredential credential, CancellationToken ct);
}
