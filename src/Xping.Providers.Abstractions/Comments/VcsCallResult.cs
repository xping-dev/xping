// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Core/Xping.Application/Core/Services/VcsIntegration/VcsCallResult.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

namespace Xping.Providers.Comments;

/// <summary>How a provider call failed; the publisher maps each kind to an outcome.</summary>
public enum VcsFailureKind
{
    /// <summary>401: the credential is no good.</summary>
    Unauthorized,

    /// <summary>403 that is not a rate limit: the credential is good, but refused this resource.</summary>
    Forbidden,

    /// <summary>404.</summary>
    NotFound,

    /// <summary>429, or a provider's own rate-limit shape.</summary>
    RateLimited,

    /// <summary>5xx, timeout, socket.</summary>
    Transient,

    /// <summary>400 or 422: the provider refused the body.</summary>
    InvalidBody,

    /// <summary>Anything else.</summary>
    Unexpected
}

internal sealed record VcsCallFailure(VcsFailureKind Kind, string Message, int? HttpStatus, TimeSpan? RetryAfter);

/// <summary>
/// The outcome of a provider call. A value, never an exception, and not <c>Result&lt;T&gt;</c>: the
/// publisher branches on the failure kind and the retry-after value, which an <c>Error</c> cannot carry.
/// </summary>
internal sealed record VcsCallResult<T>(T? Value, VcsCallFailure? Failure)
{
    public bool IsSuccess => Failure is null;

    public static VcsCallResult<T> Success(T value) => new(value, null);

    public static VcsCallResult<T> Fail(VcsCallFailure failure) => new(default, failure);
}

/// <inheritdoc cref="VcsCallResult{T}"/>
internal sealed record VcsCallResult(VcsCallFailure? Failure)
{
    public bool IsSuccess => Failure is null;

    public static VcsCallResult Success { get; } = new((VcsCallFailure?)null);

    public static VcsCallResult Fail(VcsCallFailure failure) => new(failure);
}
