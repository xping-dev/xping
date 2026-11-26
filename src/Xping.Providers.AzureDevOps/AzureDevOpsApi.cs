// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e src/Infrastructure/Xping.Providers/VcsIntegration/AzureDevOps/AzureDevOpsApi.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Xping.Providers.Comments;

namespace Xping.Providers.AzureDevOps;

/// <summary>What the Azure DevOps adapter needs to address the API: the client, the base URL, the headers.</summary>
internal static class AzureDevOpsApi
{
    public const string HttpClientName = "VcsAzureDevOps";

    public const string ApiVersion = "api-version=7.1";

    /// <summary>
    /// <c>{server}/{organization}/{project}/_apis/</c>, or null when <paramref name="owner"/> is not
    /// <c>{organization or collection}/{project}</c>. The owner splits at its first <c>/</c>: a project
    /// name cannot hold one. On Server the server URL stops before the collection, which the owner names.
    /// </summary>
    public static Uri? BaseUri(string serverUrl, string owner)
    {
        int slash = owner.IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0 || slash == owner.Length - 1)
            return null;

        string organization = Uri.EscapeDataString(owner[..slash]);
        string project = Uri.EscapeDataString(owner[(slash + 1)..]);
        return new Uri($"{serverUrl.TrimEnd('/')}/{organization}/{project}/_apis/");
    }

    public static HttpRequestMessage Request(HttpMethod method, Uri uri, string pat, object? jsonBody = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(":" + pat)));

        if (jsonBody is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

        return request;
    }
}
