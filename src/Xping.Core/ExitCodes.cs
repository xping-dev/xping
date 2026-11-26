// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Core;

/// <summary>
/// Process exit codes of the <c>xping</c> command. This is a public contract: CI scripts branch
/// on these values. Documented in <c>docs/reference/exit-codes.md</c>.
/// </summary>
public static class ExitCodes
{
    /// <summary>The command ran and nothing reached the blocking policy level.</summary>
    public const int Success = 0;

    /// <summary>At least one finding reached a <c>block</c> policy in <c>.xping.yml</c>.</summary>
    public const int Blocked = 1;

    /// <summary>An input (test results, history, configuration) is missing or unreadable.</summary>
    public const int InputError = 2;

    /// <summary>The command line could not be parsed (unknown option, missing argument). Same value as <c>EX_USAGE</c>.</summary>
    public const int UsageError = 64;

    /// <summary>The command exists but is not implemented yet. Same value as <c>EX_UNAVAILABLE</c>.</summary>
    public const int NotImplemented = 69;

    /// <summary>Xping failed unexpectedly. Same value as <c>EX_SOFTWARE</c>.</summary>
    public const int InternalError = 70;
}
