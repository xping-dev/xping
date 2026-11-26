// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/GitLab/GitLabVcsProvider.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Globalization;
using System.Text.Json;

using Xping.Providers.Comments;

namespace Xping.Providers.GitLab;

/// <summary>
/// Merge-request comments on GitLab.com and self-managed GitLab, through the notes API. The pull-request
/// number is the merge request's <c>iid</c>. The comment reference is <c>{"projectId":42,"noteId":987}</c>.
/// </summary>
internal sealed class GitLabVcsProvider(IHttpClientFactory httpClientFactory) : IVcsProvider
{
    private const int MarkerPages = 3;
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions RefJson = new(JsonSerializerDefaults.Web);

    private static readonly GitLabMarkdownDialect GitLabDialect = new();

    public CiProvider Provider => CiProvider.GitLab;

    public IMarkdownDialect Dialect => GitLabDialect;

    public async Task<VcsCallResult<VcsRepositoryRef>> ResolveRepositoryAsync(
        VcsRepositoryRef repo,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Resolving the GitLab project";
        Uri uri = new(GitLabApi.BaseUri(repo.ServerUrl), $"projects/{GitLabApi.PathSegment(repo)}");

        return await SendAsync(HttpMethod.Get, uri, credential, null, operation, ct,
            async response =>
            {
                long id = await ReadLongAsync(response, "id", ct).ConfigureAwait(false);
                return VcsCallResult<VcsRepositoryRef>.Success(
                    repo with { ExternalRepositoryId = id.ToString(CultureInfo.InvariantCulture) });
            }).ConfigureAwait(false);
    }

    public async Task<VcsCallResult<VcsCommentRef>> CreateCommentAsync(
        VcsRepositoryRef repo,
        int pullRequestNumber,
        string body,
        VcsCredential credential,
        CancellationToken ct)
    {
        const string operation = "Creating the GitLab note";
        Uri uri = NotesUri(repo, GitLabApi.ProjectSegment(repo), pullRequestNumber, string.Empty);

        return await SendAsync(HttpMethod.Post, uri, credential, new { body }, operation, ct,
            async response =>
            {
                Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                return NoteRef(json.RootElement, repo) is { } created
                    ? VcsCallResult<VcsCommentRef>.Success(created)
                    : VcsCallResult<VcsCommentRef>.Fail(new VcsCallFailure(VcsFailureKind.Unexpected,
                        $"{operation}: GitLab answered without a project id", null, null));
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
        const string operation = "Updating the GitLab note";
        if (ParseRef(comment) is not { } note)
            return VcsCallResult.Fail(InvalidRef(operation));

        Uri uri = NotesUri(repo, Id(note.ProjectId), pullRequestNumber, "/" + Id(note.NoteId));

        VcsCallResult<bool> result = await SendAsync(HttpMethod.Put, uri, credential, new { body }, operation, ct,
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
        const string operation = "Listing the GitLab notes";

        // Oldest first: the report note is posted with the first run, so it sits near the start, while
        // pushes keep adding system notes at the end of a busy merge request.
        for (int page = 1; page <= MarkerPages; page++)
        {
            Uri uri = NotesUri(repo, GitLabApi.ProjectSegment(repo), pullRequestNumber,
                $"?per_page={PageSize}&order_by=created_at&sort=asc&page={page.ToString(CultureInfo.InvariantCulture)}");

            VcsCallResult<PageScan> scan = await SendAsync(HttpMethod.Get, uri, credential, null, operation, ct,
                async response => VcsCallResult<PageScan>.Success(await ScanPageAsync(response, repo, marker, ct).ConfigureAwait(false))).ConfigureAwait(false);

            if (!scan.IsSuccess)
                return VcsCallResult<VcsCommentRef?>.Fail(scan.Failure!);

            if (scan.Value!.Found is { } found)
                return VcsCallResult<VcsCommentRef?>.Success(found);

            if (scan.Value.Count < PageSize)
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
        const string operation = "Deleting the GitLab note";
        if (ParseRef(comment) is not { } note)
            return VcsCallResult.Fail(InvalidRef(operation));

        Uri uri = NotesUri(repo, Id(note.ProjectId), pullRequestNumber, "/" + Id(note.NoteId));

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
        HttpClient client = httpClientFactory.CreateClient(GitLabApi.HttpClientName);

        try
        {
            using HttpRequestMessage request = GitLabApi.Request(method, uri, credential.Token, jsonBody);
            using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);

            // GitLab's only rate-limit shape is 429 with Retry-After, which the shared mapping reads.
            VcsCallFailure? failure = VcsHttpResponse.Map(response, operation);

            return failure is null ? await onSuccess(response).ConfigureAwait(false) : VcsCallResult<T>.Fail(failure);
        }
        catch (Exception ex) when (VcsHttpResponse.MapException(ex, operation, ct) is { } transport)
        {
            return VcsCallResult<T>.Fail(transport);
        }
    }

    private static Uri NotesUri(VcsRepositoryRef repo, string projectSegment, int mergeRequestIid, string suffix) =>
        new(GitLabApi.BaseUri(repo.ServerUrl),
            $"projects/{projectSegment}/merge_requests/{mergeRequestIid.ToString(CultureInfo.InvariantCulture)}/notes{suffix}");

    private static async Task<long> ReadLongAsync(HttpResponseMessage response, string property, CancellationToken ct)
    {
        Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
        using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        return json.RootElement.GetProperty(property).GetInt64();
    }

    private static async Task<PageScan> ScanPageAsync(
        HttpResponseMessage response,
        VcsRepositoryRef repo,
        string marker,
        CancellationToken ct)
    {
        Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
        using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        int count = 0;
        foreach (JsonElement note in json.RootElement.EnumerateArray())
        {
            count++;

            // System notes ("added 1 commit", "changed the description") are GitLab's, never ours.
            if (note.TryGetProperty("system", out JsonElement system) && system.ValueKind == JsonValueKind.True)
                continue;

            if (note.TryGetProperty("body", out JsonElement body) &&
                body.ValueKind == JsonValueKind.String &&
                body.GetString()!.Contains(marker, StringComparison.Ordinal) &&
                NoteRef(note, repo) is { } found)
            {
                return new PageScan(count, found);
            }
        }

        return new PageScan(count, null);
    }

    /// <summary>
    /// The reference to a note: its <c>project_id</c> when GitLab sends one, else the resolved project id
    /// of <paramref name="repo"/>. Null when neither is known.
    /// </summary>
    private static VcsCommentRef? NoteRef(JsonElement note, VcsRepositoryRef repo)
    {
        long noteId = note.GetProperty("id").GetInt64();

        long? projectId = note.TryGetProperty("project_id", out JsonElement project) && project.ValueKind == JsonValueKind.Number
            ? project.GetInt64()
            : long.TryParse(repo.ExternalRepositoryId, NumberStyles.None, CultureInfo.InvariantCulture, out long resolved)
                ? resolved
                : null;

        return projectId is { } id
            ? new VcsCommentRef(JsonSerializer.Serialize(new GitLabNoteRef(id, noteId), RefJson))
            : null;
    }

    private static GitLabNoteRef? ParseRef(VcsCommentRef comment)
    {
        try
        {
            return JsonSerializer.Deserialize<GitLabNoteRef>(comment.Json, RefJson) is { ProjectId: > 0, NoteId: > 0 } note
                ? note
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Id(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static VcsCallFailure InvalidRef(string operation) =>
        new(VcsFailureKind.Unexpected, $"{operation}: the stored comment reference is not a GitLab one", null, null);

    private sealed record GitLabNoteRef(long ProjectId, long NoteId);

    private sealed record PageScan(int Count, VcsCommentRef? Found);
}
