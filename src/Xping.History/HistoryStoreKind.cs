// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.History;

/// <summary>Where execution history is kept.</summary>
/// <remarks>
/// TODO(spec): stores. PRs read history; only the default branch writes it. One immutable file per
/// run, compacted baselines under <c>stats/</c> (see <c>schema/history.schema.json</c>).
/// </remarks>
public enum HistoryStoreKind
{
    /// <summary>An orphan branch or custom ref in the customer's repository. The default.</summary>
    GitBranch,

    /// <summary>CI artifacts of earlier pipeline runs.</summary>
    CiArtifact,

    /// <summary>A directory on the local file system.</summary>
    LocalFile,
}
