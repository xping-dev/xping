// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e tests/Xping.Providers.Tests/VcsIntegration/AzureDevOpsVcsProviderContractTests.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xping.Providers.AzureDevOps;
using Xping.Providers.Comments;

namespace Xping.Providers.AzureDevOps.Tests;

/// <summary>
/// The Azure DevOps adapter against recorded Azure DevOps responses (PR report spec §6.1, §6.4). The
/// repository is resolved (its GUID) unless a test is about resolution.
/// </summary>
public sealed class AzureDevOpsVcsProviderContractTests : IDisposable
{
    private const string Pat = "ado-personal-access-token";
    private const string RepositoryId = "5febef5a-833d-4e14-b9c0-14cb638f91e6";
    private const string ThreadsPath = "/fabrikam/Shop/_apis/git/repositories/" + RepositoryId + "/pullRequests/7/threads";
    private const string Marker = "<!-- xping:pr-report:01JABCDEFGHJKMNPQRSTVWXYZ0 -->";

    private static readonly string BasicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(":" + Pat));

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger<AzureDevOpsVcsProvider> _logger = Substitute.For<ILogger<AzureDevOpsVcsProvider>>();
    private readonly AzureDevOpsCommentOptions _options = new();
    private readonly List<Uri> _requestUris = [];
    private readonly AzureDevOpsVcsProvider _sut;
    private readonly VcsRepositoryRef _repo;
    private readonly VcsCredential _credential = new(Pat);

    public AzureDevOpsVcsProviderContractTests()
    {
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(new RecordingHandler(_requestUris) { InnerHandler = new HttpClientHandler() }));
        _sut = new AzureDevOpsVcsProvider(_httpClientFactory, _options, _logger);
        _repo = new VcsRepositoryRef(CiProvider.AzureDevOps, _server.Url!, "fabrikam/Shop", "web", RepositoryId);
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public void Dialect_IsAzureDevOps() =>
        _sut.Dialect.Should().BeOfType<AzureDevOpsMarkdownDialect>();

    [Fact]
    public async Task ResolveRepositoryAsync_Services_ReturnsTheRepositoryGuid()
    {
        _server.Given(Request.Create().WithPath("/fabrikam/Shop/_apis/git/repositories/web").UsingGet()
                .WithHeader("Authorization", BasicAuth)
                .WithParam("api-version", "7.1"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody($$"""{"id":"{{RepositoryId}}","name":"web"}"""));

        VcsCallResult<VcsRepositoryRef> result =
            await _sut.ResolveRepositoryAsync(_repo with { ExternalRepositoryId = null }, _credential, default);

        result.Value!.ExternalRepositoryId.Should().Be(RepositoryId);
    }

    [Fact]
    public async Task ResolveRepositoryAsync_ServerWithCollectionAndSpaces_EscapesEachSegment()
    {
        _server.Given(Request.Create().WithPath(new WildcardMatcher("/tfs/*")).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody($$"""{"id":"{{RepositoryId}}"}"""));
        VcsRepositoryRef server = new(CiProvider.AzureDevOps, _server.Url + "/tfs", "Default Collection/Online Shop", "My Repo", null);

        VcsCallResult<VcsRepositoryRef> result = await _sut.ResolveRepositoryAsync(server, _credential, default);

        result.Value!.ExternalRepositoryId.Should().Be(RepositoryId);
        _requestUris.Single().AbsolutePath.Should()
            .Be("/tfs/Default%20Collection/Online%20Shop/_apis/git/repositories/My%20Repo");
        _requestUris.Single().Query.Should().Be("?api-version=7.1");
    }

    [Fact]
    public async Task ResolveRepositoryAsync_OwnerWithoutProject_IsNotFoundWithoutACall()
    {
        VcsCallResult<VcsRepositoryRef> result = await _sut.ResolveRepositoryAsync(
            _repo with { Owner = "fabrikam", ExternalRepositoryId = null }, _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveRepositoryAsync_RepositoryNotVisible_IsNotFound()
    {
        _server.Given(Request.Create().WithPath(new WildcardMatcher("/fabrikam/Shop/_apis/git/repositories/*")).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404).WithBody("""{"message":"TF401019"}"""));

        VcsCallResult<VcsRepositoryRef> result =
            await _sut.ResolveRepositoryAsync(_repo with { ExternalRepositoryId = null }, _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
    }

    [Fact]
    public async Task CreateCommentAsync_Created_OpensAClosedThreadAndReturnsItsReference()
    {
        _server.Given(Request.Create().WithPath(ThreadsPath).UsingPost()
                .WithHeader("Authorization", BasicAuth)
                .WithParam("api-version", "7.1"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"id":148,"status":"closed","comments":[{"id":1,"content":"hello"}]}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Value!.Json.Should().Be($$"""{"repositoryId":"{{RepositoryId}}","threadId":148,"commentId":1}""");
        JsonElement sent = JsonDocument.Parse(_server.LogEntries.Single().RequestMessage!.Body!).RootElement;
        sent.GetProperty("status").GetString().Should().Be("closed");
        JsonElement comment = sent.GetProperty("comments")[0];
        comment.GetProperty("parentCommentId").GetInt32().Should().Be(0);
        comment.GetProperty("content").GetString().Should().Be("hello");
        comment.GetProperty("commentType").GetString().Should().Be("text");
    }

    [Fact]
    public async Task CreateCommentAsync_ConfiguredThreadStatus_IsSent()
    {
        _options.ThreadStatus = "active";
        _server.Given(Request.Create().WithPath(ThreadsPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"id":148,"comments":[{"id":1}]}"""));

        await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        JsonDocument.Parse(_server.LogEntries.Single().RequestMessage!.Body!).RootElement
            .GetProperty("status").GetString().Should().Be("active");
    }

    [Fact]
    public async Task CreateCommentAsync_Unresolved_IsUnexpectedWithoutACall()
    {
        VcsCallResult<VcsCommentRef> result =
            await _sut.CreateCommentAsync(_repo with { ExternalRepositoryId = null }, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.Unexpected);
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCommentAsync_Ok_PatchesTheReferencedComment()
    {
        _server.Given(Request.Create().WithPath(ThreadsPath + "/148/comments/1").UsingPatch()
                .WithParam("api-version", "7.1"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"id":1}"""));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(148, 1), "edited", _credential, default);

        result.IsSuccess.Should().BeTrue();
        JsonDocument.Parse(_server.LogEntries.Single().RequestMessage!.Body!).RootElement
            .GetProperty("content").GetString().Should().Be("edited");
    }

    [Fact]
    public async Task UpdateCommentAsync_UsesTheRepositoryOfTheReference()
    {
        const string other = "0a6c5d3e-1111-2222-3333-444455556666";
        _server.Given(Request.Create().WithPath($"/fabrikam/Shop/_apis/git/repositories/{other}/pullRequests/7/threads/148/comments/1").UsingPatch())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(148, 1, other), "edited", _credential, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCommentAsync_CommentGone_IsNotFound()
    {
        _server.Given(Request.Create().WithPath(ThreadsPath + "/148/comments/1").UsingPatch())
            .RespondWith(Response.Create().WithStatusCode(404));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(148, 1), "edited", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
    }

    [Fact]
    public async Task UpdateCommentAsync_CommentDeletedByAUser_IsNotFound()
    {
        // Azure DevOps accepts the edit of a deleted comment (measured in P5.3) and keeps it hidden.
        _server.Given(Request.Create().WithPath(ThreadsPath + "/148/comments/1").UsingPatch())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"id":1,"content":"edited","isDeleted":true}"""));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(148, 1), "edited", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
    }

    [Theory]
    [InlineData(203)]
    [InlineData(200)]
    public async Task ResolveRepositoryAsync_SignInPage_IsUnauthorized(int status)
    {
        _server.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithHeader("Content-Type", "text/html; charset=utf-8")
                .WithBody("<html><body>Sign in</body></html>"));

        VcsCallResult<VcsRepositoryRef> result =
            await _sut.ResolveRepositoryAsync(_repo with { ExternalRepositoryId = null }, _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.Unauthorized);
    }

    [Theory]
    [InlineData("""{"name":"web"}""")]
    [InlineData("""{"id":42}""")]
    [InlineData("not json")]
    public async Task ResolveRepositoryAsync_NoRepositoryId_IsUnexpected(string body)
    {
        _server.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(body));

        VcsCallResult<VcsRepositoryRef> result =
            await _sut.ResolveRepositoryAsync(_repo with { ExternalRepositoryId = null }, _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.Unexpected);
    }

    [Fact]
    public async Task UpdateCommentAsync_GitLabReference_IsUnexpectedWithoutACall()
    {
        VcsCallResult result = await _sut.UpdateCommentAsync(
            _repo, 7, new VcsCommentRef("""{"projectId":42,"noteId":987}"""), "edited", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.Unexpected);
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task FindCommentByMarkerAsync_MarkerInAThread_FindsItsFirstComment()
    {
        string threads = JsonSerializer.Serialize(new
        {
            value = new object[]
            {
                new { id = 140, comments = new[] { new { id = 1, content = "LGTM", isDeleted = false } } },
                new { id = 148, comments = new[] { new { id = 1, content = $"report\n{Marker}", isDeleted = false } } }
            },
            count = 2
        });
        _server.Given(Request.Create().WithPath(ThreadsPath).UsingGet().WithParam("api-version", "7.1"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(threads));

        VcsCallResult<VcsCommentRef?> result = await _sut.FindCommentByMarkerAsync(_repo, 7, Marker, _credential, default);

        result.Value!.Json.Should().Be($$"""{"repositoryId":"{{RepositoryId}}","threadId":148,"commentId":1}""");
    }

    [Fact]
    public async Task FindCommentByMarkerAsync_DeletedCommentOrThreadOrReply_IsSkipped()
    {
        string threads = JsonSerializer.Serialize(new
        {
            value = new object[]
            {
                new { id = 140, isDeleted = false, comments = new[] { new { id = 1, content = Marker, isDeleted = true } } },
                new { id = 141, isDeleted = true, comments = new[] { new { id = 1, content = Marker, isDeleted = false } } },
                new
                {
                    id = 142, isDeleted = false, comments = new[]
                    {
                        new { id = 1, content = "question", isDeleted = false },
                        new { id = 2, content = $"quoting {Marker}", isDeleted = false }
                    }
                }
            }
        });
        _server.Given(Request.Create().WithPath(ThreadsPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(threads));

        VcsCallResult<VcsCommentRef?> result = await _sut.FindCommentByMarkerAsync(_repo, 7, Marker, _credential, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(401, VcsFailureKind.Unauthorized)]
    [InlineData(403, VcsFailureKind.Forbidden)]
    [InlineData(404, VcsFailureKind.NotFound)]
    [InlineData(400, VcsFailureKind.InvalidBody)]
    [InlineData(503, VcsFailureKind.Transient)]
    public async Task CreateCommentAsync_ErrorStatus_MapsToItsFailureKind(int status, VcsFailureKind expected)
    {
        _server.Given(Request.Create().WithPath(ThreadsPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody("""{"message":"nope"}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(expected);
        result.Failure.HttpStatus.Should().Be(status);
        result.Failure.Message.Should().NotContain("nope", "bodies are never put into a message");
    }

    [Fact]
    public async Task CreateCommentAsync_TooManyRequests_IsRateLimitedForTheRetryAfterSeconds()
    {
        _server.Given(Request.Create().WithPath(ThreadsPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "12"));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.RateLimited);
        result.Failure.RetryAfter.Should().Be(TimeSpan.FromSeconds(12));
    }

    [Fact]
    public async Task UpdateCommentAsync_SuccessWithRateLimitUsedUp_SucceedsAndWarns()
    {
        _server.Given(Request.Create().WithPath(ThreadsPath + "/148/comments/1").UsingPatch())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("X-RateLimit-Remaining", "0").WithBody("{}"));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(148, 1), "edited", _credential, default);

        result.IsSuccess.Should().BeTrue();
        _logger.ReceivedCalls().Should().Contain(call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning);
    }

    [Fact]
    public async Task DeleteCommentAsync_Ok_DeletesTheReferencedComment()
    {
        _server.Given(Request.Create().WithPath(ThreadsPath + "/148/comments/1").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(200));

        VcsCallResult result = await _sut.DeleteCommentAsync(_repo, 7, Ref(148, 1), _credential, default);

        result.IsSuccess.Should().BeTrue();
    }

    private static VcsCommentRef Ref(long threadId, long commentId, string repositoryId = RepositoryId) =>
        new($$"""{"repositoryId":"{{repositoryId}}","threadId":{{threadId}},"commentId":{{commentId}}}""");

    /// <summary>Records each request's URI as the client sent it; the mock decodes paths before matching.</summary>
    private sealed class RecordingHandler(List<Uri> uris) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            uris.Add(request.RequestUri!);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
