// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Core.Rules;

/// <summary>
/// Stable rule identifiers. IDs are never reused or renumbered: suppressions and policies refer to them.
/// </summary>
/// <remarks>TODO(spec): rule classes, findings model and policy evaluation.</remarks>
public static class RuleIds
{
    /// <summary>An assertion was removed or weakened.</summary>
    public const string AssertionRemoved = "XP001";

    /// <summary>A test timeout was loosened.</summary>
    public const string TimeoutLoosened = "XP002";

    /// <summary>A test was skipped or deleted.</summary>
    public const string TestSkipped = "XP003";

    /// <summary>The class under test was replaced by a mock.</summary>
    public const string SutMocked = "XP004";

    /// <summary>Failure triage: flaky versus real regression.</summary>
    public const string FailureTriage = "XP005";

    /// <summary>All rule IDs defined so far.</summary>
    public static IReadOnlyList<string> All { get; } =
        [AssertionRemoved, TimeoutLoosened, TestSkipped, SutMocked, FailureTriage];
}
