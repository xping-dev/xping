// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/GitHub/GitHubApi.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Xping.Providers.Comments;

namespace Xping.Providers.GitHub;

/// <summary>What the GitHub adapter and credential provider share: the client, the base URL, the headers, the rate-limit shape.</summary>
internal static class GitHubApi
{
    public const string HttpClientName = "VcsGitHub";

    /// <summary>The server URL of github.com, as opposed to a GitHub Enterprise Server.</summary>
    public const string GitHubComServerUrl = "https://github.com";

    /// <summary><c>https://api.github.com/</c> for github.com, <c>{server}/api/v3/</c> for GitHub Enterprise Server.</summary>
    public static Uri BaseUri(string serverUrl) =>
        string.Equals(serverUrl, GitHubApi.GitHubComServerUrl, StringComparison.OrdinalIgnoreCase)
            ? new Uri("https://api.github.com/")
            : new Uri(serverUrl.TrimEnd('/') + "/api/v3/");

    public static HttpRequestMessage Request(HttpMethod method, Uri uri, string bearerToken, object? jsonBody = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        if (jsonBody is not null)
            request.Content = JsonContent(jsonBody);

        return request;
    }

    public static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

    /// <summary>
    /// GitHub's rate-limit shapes: 429, or 403 with <c>x-ratelimit-remaining: 0</c> or a body naming the
    /// secondary rate limit. Retry-after from <c>retry-after</c>, else <c>x-ratelimit-reset</c> − now.
    /// </summary>
    public static async Task<VcsCallFailure?> RateLimitAsync(
        HttpResponseMessage response,
        string operation,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        bool limited = response.StatusCode == HttpStatusCode.TooManyRequests;

        if (!limited && response.StatusCode == HttpStatusCode.Forbidden)
        {
            limited = Header(response, "x-ratelimit-remaining") == "0";
            if (!limited)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                limited = body.Contains("secondary rate limit", StringComparison.OrdinalIgnoreCase);
            }
        }

        if (!limited)
            return null;

        TimeSpan? retryAfter = VcsHttpResponse.RetryAfterHeader(response, now);
        if (retryAfter is null && long.TryParse(Header(response, "x-ratelimit-reset"), out long resetEpoch))
        {
            TimeSpan wait = DateTimeOffset.FromUnixTimeSeconds(resetEpoch) - now;
            retryAfter = wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return new VcsCallFailure(VcsFailureKind.RateLimited, $"{operation} was rate limited by GitHub",
            (int)response.StatusCode, retryAfter);
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? values.FirstOrDefault() : null;

    /// <summary>A path segment carrying user data, escaped on its own.</summary>
    public static string Segment(string value) => Uri.EscapeDataString(value);
}
