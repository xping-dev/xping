// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Session.Models;

/// <summary>
/// Indicates the upload state of a session, enabling the cloud to distinguish
/// between the first batch, intermediate batches, and the final batch.
/// </summary>
public enum TestSessionState
{
    /// <summary>
    /// First upload in the batch. The cloud records the session and PR context
    /// but does not post a PR comment yet.
    /// </summary>
    Initial = 0,

    /// <summary>
    /// Intermediate upload. PR context is included for crash recovery,
    /// but the cloud does not post a PR comment.
    /// </summary>
    Partial = 1,

    /// <summary>
    /// Final upload. Session is complete, <c>QuickStatistics</c> are populated,
    /// and the cloud posts or updates the PR comment.
    /// </summary>
    Finalized = 2
}
