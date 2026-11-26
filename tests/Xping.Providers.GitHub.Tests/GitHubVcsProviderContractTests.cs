// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e tests/Xping.Providers.Tests/VcsIntegration/GitHubVcsProviderContractTests.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

using Xping.Providers.Comments;
using Xping.Providers.GitHub;

namespace Xping.Providers.GitHub.Tests;

/// <summary>
/// The GitHub adapter against recorded GitHub responses (PR report spec §6.1, §6.2). The server URL
/// is the mock's, so the adapter takes the GitHub Enterprise Server path, <c>{server}/api/v3/</c>.
/// </summary>
public sealed class GitHubVcsProviderContractTests : IDisposable
{
    private const string Token = "ghs_installation_token";

    // The mock decodes the request path, so it matches the owner unescaped.
    private const string RepoPath = "/api/v3/repos/acme corp/api/";
    private const string Marker = "<!-- xping:pr-report:01JABCDEFGHJKMNPQRSTVWXYZ0 -->";

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly GitHubVcsProvider _sut;
    private readonly VcsRepositoryRef _repo;
    private readonly VcsCredential _credential = new(Token);

    public GitHubVcsProviderContractTests()
    {
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        _sut = new GitHubVcsProvider(_httpClientFactory, _time);
        _repo = new VcsRepositoryRef(CiProvider.GitHub, _server.Url!, "acme corp", "api", null);
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public async Task CreateCommentAsync_Created_ReturnsTheCommentIdAsItsReference()
    {
        _server.Given(Request.Create().WithPath(RepoPath + "issues/7/comments").UsingPost()
                .WithHeader("Authorization", $"Bearer {Token}")
                .WithHeader("X-GitHub-Api-Version", "2022-11-28"))
            .RespondWith(Response.Create().WithStatusCode(201).WithBody("""{"id":123456789,"body":"…"}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.IsSuccess.Should().BeTrue();
        CommentId(result.Value!).Should().Be(123456789);
        JsonDocument.Parse(_server.LogEntries.Single().RequestMessage!.Body!).RootElement
            .GetProperty("body").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task UpdateCommentAsync_Ok_PatchesTheComment()
    {
        _server.Given(Request.Create().WithPath(RepoPath + "issues/comments/55").UsingPatch())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(55), "edited", _credential, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCommentAsync_CommentGone_IsNotFound()
    {
        _server.Given(Request.Create().WithPath(RepoPath + "issues/comments/55").UsingPatch())
            .RespondWith(Response.Create().WithStatusCode(404));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(55), "edited", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
    }

    [Fact]
    public async Task FindCommentByMarkerAsync_MarkerOnTheSecondPage_FindsIt()
    {
        string firstPage = JsonSerializer.Serialize(Enumerable.Range(1, 100).Select(i => new { id = i, body = "other" }));
        string secondPage = JsonSerializer.Serialize(new[] { new { id = 900, body = "x" }, new { id = 901, body = $"report\n{Marker}" } });

        _server.Given(Request.Create().WithPath(RepoPath + "issues/7/comments").UsingGet().WithParam("page", "1"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(firstPage));
        _server.Given(Request.Create().WithPath(RepoPath + "issues/7/comments").UsingGet().WithParam("page", "2"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(secondPage));

        VcsCallResult<VcsCommentRef?> result = await _sut.FindCommentByMarkerAsync(_repo, 7, Marker, _credential, default);

        CommentId(result.Value!).Should().Be(901);
    }

    [Fact]
    public async Task FindCommentByMarkerAsync_NoMarkerOnAShortPage_StopsAndFindsNothing()
    {
        _server.Given(Request.Create().WithPath(RepoPath + "issues/7/comments").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""[{"id":1,"body":"other"}]"""));

        VcsCallResult<VcsCommentRef?> result = await _sut.FindCommentByMarkerAsync(_repo, 7, Marker, _credential, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        _server.LogEntries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(401, VcsFailureKind.Unauthorized)]
    [InlineData(403, VcsFailureKind.Forbidden)]
    [InlineData(404, VcsFailureKind.NotFound)]
    [InlineData(422, VcsFailureKind.InvalidBody)]
    [InlineData(400, VcsFailureKind.InvalidBody)]
    [InlineData(503, VcsFailureKind.Transient)]
    [InlineData(409, VcsFailureKind.Unexpected)]
    public async Task CreateCommentAsync_ErrorStatus_MapsToItsFailureKind(int status, VcsFailureKind expected)
    {
        GivenCreateResponds(Response.Create().WithStatusCode(status).WithBody("""{"message":"nope"}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(expected);
        result.Failure.HttpStatus.Should().Be(status);
        result.Failure.Message.Should().NotContain("nope", "bodies are never put into a message");
    }

    [Fact]
    public async Task CreateCommentAsync_TooManyRequests_IsRateLimitedForTheRetryAfterSeconds()
    {
        GivenCreateResponds(Response.Create().WithStatusCode(429).WithHeader("retry-after", "7"));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.RateLimited);
        result.Failure.RetryAfter.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task CreateCommentAsync_ForbiddenWithNoRemainingQuota_IsRateLimitedUntilTheReset()
    {
        long reset = _time.GetUtcNow().AddSeconds(90).ToUnixTimeSeconds();
        GivenCreateResponds(Response.Create().WithStatusCode(403)
            .WithHeader("x-ratelimit-remaining", "0")
            .WithHeader("x-ratelimit-reset", reset.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.RateLimited);
        result.Failure.RetryAfter.Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public async Task CreateCommentAsync_SecondaryRateLimit_IsRateLimitedNotUnauthorized()
    {
        GivenCreateResponds(Response.Create().WithStatusCode(403)
            .WithBody("""{"message":"You have exceeded a secondary rate limit."}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.RateLimited);
        result.Failure.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCommentAsync_NoContent_Succeeds()
    {
        _server.Given(Request.Create().WithPath(RepoPath + "issues/comments/55").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(204));

        VcsCallResult result = await _sut.DeleteCommentAsync(_repo, 7, Ref(55), _credential, default);

        result.IsSuccess.Should().BeTrue();
    }

    private void GivenCreateResponds(IResponseBuilder response) =>
        _server.Given(Request.Create().WithPath(RepoPath + "issues/7/comments").UsingPost())
            .RespondWith(response);

    private static VcsCommentRef Ref(long id) => new($$"""{"commentId":{{id}}}""");

    private static long CommentId(VcsCommentRef comment) =>
        JsonDocument.Parse(comment.Json).RootElement.GetProperty("commentId").GetInt64();

#pragma warning disable CS0618 // ISystemClock is what MemoryCacheOptions still takes.
#pragma warning restore CS0618
}
