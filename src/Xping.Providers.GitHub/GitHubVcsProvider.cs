// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/GitHub/GitHubVcsProvider.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Globalization;
using System.Text.Json;

using Xping.Providers.Comments;

namespace Xping.Providers.GitHub;

/// <summary>
/// Pull-request comments on GitHub and GitHub Enterprise Server, through the issue comments API.
/// The comment reference is <c>{"commentId":123}</c>.
/// </summary>
internal sealed class GitHubVcsProvider(
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider) : IVcsProvider
{
    private const int MarkerPages = 3;

    private static readonly JsonSerializerOptions RefJson = new(JsonSerializerDefaults.Web);

    private static readonly GitHubMarkdownDialect GitHubDialect = new();

    public CiProvider Provider => CiProvider.GitHub;

    public IMarkdownDialect Dialect => GitHubDialect;

    public Task<VcsCallResult<VcsRepositoryRef>> ResolveRepositoryAsync(
        VcsRepositoryRef repo,
        VcsCredential credential,
        CancellationToken ct) =>
        Task.FromResult(VcsCallResult<VcsRepositoryRef>.Success(repo));

    public async Task<VcsCallResult<VcsCommentRef>> CreateCommentAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        string body,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Creating the GitHub comment";
        Uri uri = RepoUri(repo, $"issues/{pullRequestNumber.ToString(CultureInfo.InvariantCulture)}/comments");

        return await SendAsync(HttpMethod.Post, uri, credential, new { body }, operation, ct,
            async response =>
            {
                long id = await ReadIdAsync(response, ct).ConfigureAwait(false);
                return VcsCallResult<VcsCommentRef>.Success(ToRef(id));
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
        const string operation = "Updating the GitHub comment";
        if (ParseRef(comment) is not { } commentId)
            return VcsCallResult.Fail(InvalidRef(operation));

        Uri uri = RepoUri(repo, $"issues/comments/{commentId.ToString(CultureInfo.InvariantCulture)}");

        VcsCallResult<bool> result = await SendAsync(HttpMethod.Patch, uri, credential, new { body }, operation, ct,
            _ => Task.FromResult(VcsCallResult<bool>.Success(true))).ConfigureAwait(false);

        return result.IsSuccess ? VcsCallResult.Success : VcsCallResult.Fail(result.Failure!);
    }

    public async Task<VcsCallResult<VcsCommentRef?>> FindCommentByMarkerAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        string marker,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Listing the GitHub comments";

        for (int page = 1; page <= MarkerPages; page++)
        {
            Uri uri = RepoUri(repo,
                $"issues/{pullRequestNumber.ToString(CultureInfo.InvariantCulture)}/comments?per_page=100&page={page.ToString(CultureInfo.InvariantCulture)}");

            VcsCallResult<PageScan> scan = await SendAsync(HttpMethod.Get, uri, credential, null, operation, ct,
                async response => VcsCallResult<PageScan>.Success(await ScanPageAsync(response, marker, ct).ConfigureAwait(false))).ConfigureAwait(false);

            if (!scan.IsSuccess)
                return VcsCallResult<VcsCommentRef?>.Fail(scan.Failure!);

            if (scan.Value!.FoundId is { } id)
                return VcsCallResult<VcsCommentRef?>.Success(ToRef(id));

            if (scan.Value.Count < 100)
                break;
        }

        return VcsCallResult<VcsCommentRef?>.Success(null);
    }

    public async Task<VcsCallResult> DeleteCommentAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        VcsCommentRef comment,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Deleting the GitHub comment";
        if (ParseRef(comment) is not { } commentId)
            return VcsCallResult.Fail(InvalidRef(operation));

        Uri uri = RepoUri(repo, $"issues/comments/{commentId.ToString(CultureInfo.InvariantCulture)}");

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
        HttpClient client = httpClientFactory.CreateClient(GitHubApi.HttpClientName);

        try
        {
            using HttpRequestMessage request = GitHubApi.Request(method, uri, credential.Token, jsonBody);
            using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);

            VcsCallFailure? failure = VcsHttpResponse.Map(response, operation,
                await GitHubApi.RateLimitAsync(response, operation, timeProvider.GetUtcNow(), ct).ConfigureAwait(false));

            return failure is null ? await onSuccess(response).ConfigureAwait(false) : VcsCallResult<T>.Fail(failure);
        }
        catch (Exception ex) when (VcsHttpResponse.MapException(ex, operation, ct) is { } transport)
        {
            return VcsCallResult<T>.Fail(transport);
        }
    }

    private static Uri RepoUri(VcsRepositoryRef repo, string relative) =>
        new(GitHubApi.BaseUri(repo.ServerUrl),
            $"repos/{GitHubApi.Segment(repo.Owner)}/{GitHubApi.Segment(repo.Name)}/{relative}");

    private static async Task<long> ReadIdAsync(HttpResponseMessage response, CancellationToken ct)
    {
        Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
        using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        return json.RootElement.GetProperty("id").GetInt64();
    }

    private static async Task<PageScan> ScanPageAsync(HttpResponseMessage response, string marker, CancellationToken ct)
    {
        Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
        using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        int count = 0;
        foreach (JsonElement comment in json.RootElement.EnumerateArray())
        {
            count++;
            if (comment.TryGetProperty("body", out JsonElement body) &&
                body.ValueKind == JsonValueKind.String &&
                body.GetString()!.Contains(marker, StringComparison.Ordinal))
            {
                return new PageScan(count, comment.GetProperty("id").GetInt64());
            }
        }

        return new PageScan(count, null);
    }

    private static VcsCommentRef ToRef(long commentId) =>
        new(JsonSerializer.Serialize(new GitHubCommentRef(commentId), RefJson));

    private static long? ParseRef(VcsCommentRef comment)
    {
        try
        {
            return JsonSerializer.Deserialize<GitHubCommentRef>(comment.Json, RefJson)?.CommentId;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static VcsCallFailure InvalidRef(string operation) =>
        new(VcsFailureKind.Unexpected, $"{operation}: the stored comment reference is not a GitHub one", null, null);

    private sealed record GitHubCommentRef(long CommentId);

    private sealed record PageScan(int Count, long? FoundId);
}
