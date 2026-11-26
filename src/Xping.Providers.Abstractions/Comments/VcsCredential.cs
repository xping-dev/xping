// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/VcsIntegration/VcsCredential.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>The CI job's token for the provider API (GITHUB_TOKEN, CI_JOB_TOKEN or a project token, SYSTEM_ACCESSTOKEN).</summary>
/// <remarks>
/// The dashboard's version also carried a kind (GitHub App or access token) and an expiry for tokens it
/// stored. The CLI only ever holds the job's own token for the length of one run.
/// </remarks>
internal sealed record VcsCredential(string Token)
{
    // The token never reaches a log line through a record ToString.
    public override string ToString() => "VcsCredential { Token = [redacted] }";
}
