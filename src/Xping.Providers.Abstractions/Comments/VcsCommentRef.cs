// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/VcsIntegration/VcsCommentRef.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>Provider-specific JSON naming a comment. Only the adapter that wrote it parses it.</summary>
internal sealed record VcsCommentRef(string Json);
