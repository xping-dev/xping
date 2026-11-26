// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/GitLab/GitLabApi.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Net.Http.Headers;
using System.Text.Json;

using Xping.Providers.Comments;

namespace Xping.Providers.GitLab;

/// <summary>What the GitLab adapter needs to address the API: the client, the base URL, the headers, the project segment.</summary>
internal static class GitLabApi
{
    public const string HttpClientName = "VcsGitLab";

    /// <summary><c>{server}/api/v4/</c>; a server under a relative URL root already carries it.</summary>
    public static Uri BaseUri(string serverUrl) => new(serverUrl.TrimEnd('/') + "/api/v4/");

    public static HttpRequestMessage Request(HttpMethod method, Uri uri, string token, object? jsonBody = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("PRIVATE-TOKEN", token);

        if (jsonBody is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(jsonBody), System.Text.Encoding.UTF8, "application/json");

        return request;
    }

    /// <summary>
    /// The <c>{id}</c> of <c>/projects/{id}</c>: the numeric id once resolved, else the full path escaped
    /// as one segment (<c>group/sub/project</c> → <c>group%2Fsub%2Fproject</c>), which GitLab accepts too.
    /// </summary>
    public static string ProjectSegment(VcsRepositoryRef repo) =>
        repo.ExternalRepositoryId ?? PathSegment(repo);

    /// <summary>The full project path, escaped as one segment.</summary>
    public static string PathSegment(VcsRepositoryRef repo) =>
        Uri.EscapeDataString($"{repo.Owner}/{repo.Name}");
}
