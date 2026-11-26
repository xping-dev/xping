// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/AzureDevOps/AzureDevOpsVcsProvider.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using Xping.Providers.Comments;

namespace Xping.Providers.AzureDevOps;

/// <summary>
/// Pull-request comments on Azure Repos (Azure DevOps Services and Server), as the first comment of a
/// thread the adapter opens with the configured status (<c>closed</c>, D17), so the comment never blocks
/// a policy that requires every thread resolved. The comment reference is
/// <c>{"repositoryId":"guid","threadId":148,"commentId":1}</c>.
/// </summary>
internal sealed class AzureDevOpsVcsProvider(
    IHttpClientFactory httpClientFactory,
    AzureDevOpsCommentOptions options,
    ILogger<AzureDevOpsVcsProvider> logger) : IVcsProvider
{
    private static readonly JsonSerializerOptions RefJson = new(JsonSerializerDefaults.Web);

    private static readonly AzureDevOpsMarkdownDialect AzureDevOpsDialect = new();

    public CiProvider Provider => CiProvider.AzureDevOps;

    public IMarkdownDialect Dialect => AzureDevOpsDialect;

    public async Task<VcsCallResult<VcsRepositoryRef>> ResolveRepositoryAsync(
        VcsRepositoryRef repo,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Resolving the Azure Repos repository";
        if (Address(repo, $"git/repositories/{Uri.EscapeDataString(repo.Name)}") is not { } uri)
            return VcsCallResult<VcsRepositoryRef>.Fail(InvalidOwner(operation, repo));

        return await SendAsync(HttpMethod.Get, uri, credential, null, operation, ct,
            async response =>
            {
                Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                return json.RootElement.TryGetProperty("id", out JsonElement id) &&
                       id.ValueKind == JsonValueKind.String && id.GetString() is { Length: > 0 } repositoryId
                    ? VcsCallResult<VcsRepositoryRef>.Success(repo with { ExternalRepositoryId = repositoryId })
                    : VcsCallResult<VcsRepositoryRef>.Fail(new VcsCallFailure(VcsFailureKind.Unexpected,
                        $"{operation}: Azure DevOps answered without a repository id", null, null));
            }).ConfigureAwait(false);
    }

    public async Task<VcsCallResult<VcsCommentRef>> CreateCommentAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        string body,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Creating the Azure Repos thread";
        if (repo.ExternalRepositoryId is not { } repositoryId)
            return VcsCallResult<VcsCommentRef>.Fail(Unresolved(operation));

        if (ThreadsUri(repo, repositoryId, pullRequestNumber, string.Empty) is not { } uri)
            return VcsCallResult<VcsCommentRef>.Fail(InvalidOwner(operation, repo));

        var thread = new
        {
            comments = new[] { new { parentCommentId = 0, content = body, commentType = "text" } },
            status = options.ThreadStatus
        };

        return await SendAsync(HttpMethod.Post, uri, credential, thread, operation, ct,
            async response =>
            {
                Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                return ThreadRef(json.RootElement, repositoryId) is { } created
                    ? VcsCallResult<VcsCommentRef>.Success(created)
                    : VcsCallResult<VcsCommentRef>.Fail(new VcsCallFailure(VcsFailureKind.Unexpected,
                        $"{operation}: Azure DevOps answered without a comment", null, null));
            }).ConfigureAwait(false);
    }

    public async Task<VcsCallResult> UpdateCommentAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        VcsCommentRef comment,
        string body,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Updating the Azure Repos comment";
        if (ParseRef(comment) is not { } thread)
            return VcsCallResult.Fail(InvalidRef(operation));

        if (CommentUri(repo, thread, pullRequestNumber) is not { } uri)
            return VcsCallResult.Fail(InvalidOwner(operation, repo));

        // Azure DevOps accepts an edit of a deleted comment and answers 200 with isDeleted; the comment
        // stays hidden, so that is the comment gone (D5).
        VcsCallResult<bool> result = await SendAsync(HttpMethod.Patch, uri, credential, new { content = body }, operation, ct,
            async response =>
            {
                Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                return IsDeleted(json.RootElement)
                    ? VcsCallResult<bool>.Fail(new VcsCallFailure(VcsFailureKind.NotFound,
                        $"{operation}: the comment is deleted", 200, null))
                    : VcsCallResult<bool>.Success(true);
            }).ConfigureAwait(false);

        return result.IsSuccess ? VcsCallResult.Success : VcsCallResult.Fail(result.Failure!);
    }

    public async Task<VcsCallResult<VcsCommentRef?>> FindCommentByMarkerAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        string marker,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Listing the Azure Repos threads";
        if (repo.ExternalRepositoryId is not { } repositoryId)
            return VcsCallResult<VcsCommentRef?>.Fail(Unresolved(operation));

        if (ThreadsUri(repo, repositoryId, pullRequestNumber, string.Empty) is not { } uri)
            return VcsCallResult<VcsCommentRef?>.Fail(InvalidOwner(operation, repo));

        // The threads list is not paged: one call returns every thread of the pull request.
        return await SendAsync(HttpMethod.Get, uri, credential, null, operation, ct,
            async response =>
            {
                Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                return VcsCallResult<VcsCommentRef?>.Success(FindMarker(json.RootElement, repositoryId, marker));
            }).ConfigureAwait(false);
    }

    public async Task<VcsCallResult> DeleteCommentAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        VcsCommentRef comment,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Deleting the Azure Repos comment";
        if (ParseRef(comment) is not { } thread)
            return VcsCallResult.Fail(InvalidRef(operation));

        if (CommentUri(repo, thread, pullRequestNumber) is not { } uri)
            return VcsCallResult.Fail(InvalidOwner(operation, repo));

        VcsCallResult<bool> result = await SendAsync(HttpMethod.Delete, uri, credential, null, operation, ct,
            _ => Task.FromResult(VcsCallResult<bool>.Success(true))).ConfigureAwait(false);

        return result.IsSuccess ? VcsCallResult.Success : VcsCallResult.Fail(result.Failure!);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068", Justification = "The success callback goes last so each call site ends in a lambda block.")]
    private async Task<VcsCallResult<T>> SendAsync<T>(
        HttpMethod method,
        Uri uri,
        VcsCredential credential,
        object? jsonBody,
        string operation,
        CancellationToken ct,
        Func<HttpResponseMessage, Task<VcsCallResult<T>>> onSuccess)
    {
        HttpClient client = httpClientFactory.CreateClient(AzureDevOpsApi.HttpClientName);

        try
        {
            using HttpRequestMessage request = AzureDevOpsApi.Request(method, uri, credential.Token, jsonBody);
            using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);

            // Azure DevOps rate-limits with 429 and Retry-After, which the shared mapping reads. Before
            // that it warns on successful calls; the call still went through.
            VcsCallFailure? failure = VcsHttpResponse.Map(response, operation) ?? SignInPage(response, operation);
            if (failure is null && IsThrottleWarning(response))
                logger.LogWarning("{Operation}: Azure DevOps reports the rate limit is used up", operation);

            return failure is null ? await onSuccess(response).ConfigureAwait(false) : VcsCallResult<T>.Fail(failure);
        }
        catch (Exception ex) when (VcsHttpResponse.MapException(ex, operation, ct) is { } transport)
        {
            return VcsCallResult<T>.Fail(transport);
        }
        catch (JsonException)
        {
            return VcsCallResult<T>.Fail(new VcsCallFailure(VcsFailureKind.Unexpected,
                $"{operation}: Azure DevOps answered with a body that is not JSON", null, null));
        }
    }

    /// <summary>
    /// Azure DevOps answers a revoked, expired or wrong PAT with 203 and its HTML sign-in page, or with
    /// a redirect to it that the handler follows; either is the token refused.
    /// </summary>
    private static VcsCallFailure? SignInPage(HttpResponseMessage response, string operation) =>
        response.StatusCode == System.Net.HttpStatusCode.NonAuthoritativeInformation ||
        response.Content.Headers.ContentType?.MediaType == "text/html"
            ? new VcsCallFailure(VcsFailureKind.Unauthorized,
                $"{operation} returned the Azure DevOps sign-in page (HTTP {(int)response.StatusCode})", (int)response.StatusCode, null)
            : null;

    private static bool IsThrottleWarning(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string>? values) &&
        values.Any(v => v.Trim() == "0");

    /// <summary><paramref name="path"/> under the project's API base, with the API version.</summary>
    private static Uri? Address(VcsRepositoryRef repo, string path) =>
        AzureDevOpsApi.BaseUri(repo.ServerUrl, repo.Owner) is { } baseUri
            ? new Uri(baseUri, path + (path.Contains('?', StringComparison.Ordinal) ? "&" : "?") + AzureDevOpsApi.ApiVersion)
            : null;

    private static Uri? ThreadsUri(VcsRepositoryRef repo, string repositoryId, int pullRequestId, string suffix) =>
        Address(repo,
            $"git/repositories/{Uri.EscapeDataString(repositoryId)}/pullRequests/{pullRequestId.ToString(CultureInfo.InvariantCulture)}/threads{suffix}");

    private static Uri? CommentUri(VcsRepositoryRef repo, AzureDevOpsThreadRef thread, int pullRequestId) =>
        ThreadsUri(repo, thread.RepositoryId, pullRequestId,
            $"/{Id(thread.ThreadId)}/comments/{Id(thread.CommentId)}");

    private static VcsCommentRef? FindMarker(JsonElement root, string repositoryId, string marker)
    {
        if (!root.TryGetProperty("value", out JsonElement threads) || threads.ValueKind != JsonValueKind.Array)
            return null;

        foreach (JsonElement thread in threads.EnumerateArray())
        {
            if (IsDeleted(thread) ||
                !thread.TryGetProperty("comments", out JsonElement comments) ||
                comments.ValueKind != JsonValueKind.Array ||
                comments.GetArrayLength() == 0)
                continue;

            JsonElement first = comments[0];
            if (!IsDeleted(first) &&
                first.TryGetProperty("content", out JsonElement content) &&
                content.ValueKind == JsonValueKind.String &&
                content.GetString()!.Contains(marker, StringComparison.Ordinal) &&
                ThreadRef(thread, repositoryId) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static bool IsDeleted(JsonElement element) =>
        element.TryGetProperty("isDeleted", out JsonElement deleted) && deleted.ValueKind == JsonValueKind.True;

    /// <summary>The reference to a thread's first comment, or null when the thread carries none.</summary>
    private static VcsCommentRef? ThreadRef(JsonElement thread, string repositoryId)
    {
        if (!thread.TryGetProperty("id", out JsonElement threadId) || threadId.ValueKind != JsonValueKind.Number ||
            !thread.TryGetProperty("comments", out JsonElement comments) || comments.ValueKind != JsonValueKind.Array ||
            comments.GetArrayLength() == 0 ||
            !comments[0].TryGetProperty("id", out JsonElement commentId) || commentId.ValueKind != JsonValueKind.Number)
            return null;

        return new VcsCommentRef(JsonSerializer.Serialize(
            new AzureDevOpsThreadRef(repositoryId, threadId.GetInt64(), commentId.GetInt64()), RefJson));
    }

    private static AzureDevOpsThreadRef? ParseRef(VcsCommentRef comment)
    {
        try
        {
            return JsonSerializer.Deserialize<AzureDevOpsThreadRef>(comment.Json, RefJson) is
            { RepositoryId.Length: > 0, ThreadId: > 0, CommentId: > 0 } thread
                ? thread
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Id(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static VcsCallFailure InvalidRef(string operation) =>
        new(VcsFailureKind.Unexpected, $"{operation}: the stored comment reference is not an Azure DevOps one", null, null);

    private static VcsCallFailure Unresolved(string operation) =>
        new(VcsFailureKind.Unexpected, $"{operation}: the repository id is not resolved", null, null);

    /// <summary>An owner that is not <c>{organization}/{project}</c> names no repository the API can reach.</summary>
    private static VcsCallFailure InvalidOwner(string operation, VcsRepositoryRef repo) =>
        new(VcsFailureKind.NotFound, $"{operation}: owner '{repo.Owner}' is not organization/project", null, null);

    private sealed record AzureDevOpsThreadRef(string RepositoryId, long ThreadId, long CommentId);
}
