// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/Http/VcsHttpResponse.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Net;

namespace Xping.Providers.Comments;

/// <summary>
/// The status-to-failure mapping every adapter shares. Bodies are never put into a message: they can
/// carry repository content.
/// </summary>
internal static class VcsHttpResponse
{
    /// <summary>
    /// Null for a success status; otherwise the failure, with <paramref name="rateLimit"/> consulted
    /// first so a provider's own rate-limit shape (GitHub's 403) wins over the generic mapping.
    /// </summary>
    public static VcsCallFailure? Map(HttpResponseMessage response, string operation, VcsCallFailure? rateLimit = null)
    {
        if (response.IsSuccessStatusCode)
            return null;

        if (rateLimit is not null)
            return rateLimit;

        int status = (int)response.StatusCode;
        string message = $"{operation} returned HTTP {status}";

        VcsFailureKind kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => VcsFailureKind.Unauthorized,
            HttpStatusCode.Forbidden => VcsFailureKind.Forbidden,
            HttpStatusCode.NotFound => VcsFailureKind.NotFound,
            HttpStatusCode.TooManyRequests => VcsFailureKind.RateLimited,
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => VcsFailureKind.InvalidBody,
            HttpStatusCode.RequestTimeout => VcsFailureKind.Transient,
            _ when status >= 500 => VcsFailureKind.Transient,
            _ => VcsFailureKind.Unexpected
        };

        TimeSpan? retryAfter = kind == VcsFailureKind.RateLimited ? RetryAfterHeader(response) : null;
        return new VcsCallFailure(kind, message, status, retryAfter);
    }

    /// <summary>A transport failure: socket, DNS, timeout. Null for anything else, which the caller rethrows.</summary>
    public static VcsCallFailure? MapException(Exception exception, string operation, CancellationToken callerToken) =>
        exception switch
        {
            OperationCanceledException when callerToken.IsCancellationRequested => null,
            HttpRequestException or TaskCanceledException =>
                new VcsCallFailure(VcsFailureKind.Transient, $"{operation} failed: {exception.GetType().Name}", null, null),
            _ => null
        };

    /// <summary>The <c>Retry-After</c> header as a delay, whether given in seconds or as a date.</summary>
    public static TimeSpan? RetryAfterHeader(HttpResponseMessage response, DateTimeOffset? now = null)
    {
        if (response.Headers.RetryAfter is not { } retryAfter)
            return null;

        if (retryAfter.Delta is { } delta)
            return delta;

        if (retryAfter.Date is { } date)
        {
            TimeSpan wait = date - (now ?? DateTimeOffset.UtcNow);
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }
}
