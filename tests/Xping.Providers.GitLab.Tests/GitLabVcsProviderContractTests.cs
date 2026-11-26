// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Copied from xping-dev/dashboard@f250a6e tests/Xping.Providers.Tests/VcsIntegration/GitLabVcsProviderContractTests.cs
// (relicensed under MIT; see docs/specs/migration-plan.md, Q1).

using System.Text.Json;
using AwesomeAssertions;
using NSubstitute;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

using Xping.Providers.Comments;
using Xping.Providers.GitLab;

namespace Xping.Providers.GitLab.Tests;

/// <summary>
/// The GitLab adapter against recorded GitLab responses (PR report spec §6.1, §6.3). The project is
/// resolved (id 42) unless a test is about the path form.
/// </summary>
public sealed class GitLabVcsProviderContractTests : IDisposable
{
    private const string Token = "glpat-project-token";
    private const string NotesPath = "/api/v4/projects/42/merge_requests/7/notes";
    private const string Marker = "<!-- xping:pr-report:01JABCDEFGHJKMNPQRSTVWXYZ0 -->";

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly List<Uri> _requestUris = [];
    private readonly GitLabVcsProvider _sut;
    private readonly VcsRepositoryRef _repo;
    private readonly VcsCredential _credential = new(Token);

    public GitLabVcsProviderContractTests()
    {
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(new RecordingHandler(_requestUris) { InnerHandler = new HttpClientHandler() }));
        _sut = new GitLabVcsProvider(_httpClientFactory);
        _repo = new VcsRepositoryRef(CiProvider.GitLab, _server.Url!, "a/b/c/d", "proj", "42");
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public void Dialect_IsGitLab() =>
        _sut.Dialect.Should().BeOfType<GitLabMarkdownDialect>();

    [Fact]
    public async Task ResolveRepositoryAsync_NestedNamespace_EscapesThePathAsOneSegmentAndReturnsTheId()
    {
        _server.Given(Request.Create().WithPath(new WildcardMatcher("/api/v4/projects/*")).UsingGet()
                .WithHeader("PRIVATE-TOKEN", Token))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"id":42,"path_with_namespace":"a/b/c/d/proj"}"""));

        VcsCallResult<VcsRepositoryRef> result =
            await _sut.ResolveRepositoryAsync(_repo with { ExternalRepositoryId = null }, _credential, default);

        result.Value!.ExternalRepositoryId.Should().Be("42");
        _requestUris.Single().AbsolutePath.Should().Be("/api/v4/projects/a%2Fb%2Fc%2Fd%2Fproj");
    }

    [Fact]
    public async Task ResolveRepositoryAsync_ProjectNotVisible_IsNotFound()
    {
        _server.Given(Request.Create().WithPath(new WildcardMatcher("/api/v4/projects/*")).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404).WithBody("""{"message":"404 Project Not Found"}"""));

        VcsCallResult<VcsRepositoryRef> result =
            await _sut.ResolveRepositoryAsync(_repo with { ExternalRepositoryId = null }, _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
    }

    [Fact]
    public async Task CreateCommentAsync_Created_ReturnsProjectAndNoteIdAsItsReference()
    {
        _server.Given(Request.Create().WithPath(NotesPath).UsingPost().WithHeader("PRIVATE-TOKEN", Token))
            .RespondWith(Response.Create().WithStatusCode(201).WithBody("""{"id":987,"project_id":42,"body":"…","system":false}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Json.Should().Be("""{"projectId":42,"noteId":987}""");
        JsonDocument.Parse(_server.LogEntries.Single().RequestMessage!.Body!).RootElement
            .GetProperty("body").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task CreateCommentAsync_Unresolved_AddressesTheProjectByEscapedPath()
    {
        _server.Given(Request.Create().WithPath(new WildcardMatcher("/api/v4/projects/*/merge_requests/7/notes")).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBody("""{"id":987,"project_id":42}"""));

        VcsCallResult<VcsCommentRef> result =
            await _sut.CreateCommentAsync(_repo with { ExternalRepositoryId = null }, 7, "hello", _credential, default);

        result.Value!.Json.Should().Be("""{"projectId":42,"noteId":987}""");
        _requestUris.Single().AbsolutePath.Should().Be("/api/v4/projects/a%2Fb%2Fc%2Fd%2Fproj/merge_requests/7/notes");
    }

    [Fact]
    public async Task UpdateCommentAsync_Ok_PutsTheNoteOfTheReferencedProject()
    {
        _server.Given(Request.Create().WithPath("/api/v4/projects/43/merge_requests/7/notes/987").UsingPut())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(43, 987), "edited", _credential, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCommentAsync_NoteGone_IsNotFound()
    {
        _server.Given(Request.Create().WithPath(NotesPath + "/987").UsingPut())
            .RespondWith(Response.Create().WithStatusCode(404));

        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, Ref(42, 987), "edited", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.NotFound);
    }

    [Fact]
    public async Task UpdateCommentAsync_GitHubReference_IsUnexpectedWithoutACall()
    {
        VcsCallResult result = await _sut.UpdateCommentAsync(_repo, 7, new VcsCommentRef("""{"commentId":5}"""), "edited", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.Unexpected);
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task FindCommentByMarkerAsync_MarkerOnTheSecondPage_FindsIt()
    {
        string firstPage = JsonSerializer.Serialize(Enumerable.Range(1, 100).Select(i => new { id = i, body = "other", system = false, project_id = 42 }));
        string secondPage = JsonSerializer.Serialize(new[]
        {
            new { id = 900, body = "x", system = false, project_id = 42 },
            new { id = 901, body = $"report\n{Marker}", system = false, project_id = 42 }
        });

        _server.Given(Request.Create().WithPath(NotesPath).UsingGet().WithParam("page", "1").WithParam("sort", "asc"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(firstPage));
        _server.Given(Request.Create().WithPath(NotesPath).UsingGet().WithParam("page", "2"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(secondPage));

        VcsCallResult<VcsCommentRef?> result = await _sut.FindCommentByMarkerAsync(_repo, 7, Marker, _credential, default);

        result.Value!.Json.Should().Be("""{"projectId":42,"noteId":901}""");
    }

    [Fact]
    public async Task FindCommentByMarkerAsync_SystemNoteQuotingTheMarker_IsSkipped()
    {
        string page = JsonSerializer.Serialize(new[]
        {
            new { id = 1, body = $"changed this line {Marker}", system = true, project_id = 42 }
        });
        _server.Given(Request.Create().WithPath(NotesPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(page));

        VcsCallResult<VcsCommentRef?> result = await _sut.FindCommentByMarkerAsync(_repo, 7, Marker, _credential, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        _server.LogEntries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(401, VcsFailureKind.Unauthorized)]
    [InlineData(403, VcsFailureKind.Forbidden)]
    [InlineData(404, VcsFailureKind.NotFound)]
    [InlineData(400, VcsFailureKind.InvalidBody)]
    [InlineData(422, VcsFailureKind.InvalidBody)]
    [InlineData(502, VcsFailureKind.Transient)]
    public async Task CreateCommentAsync_ErrorStatus_MapsToItsFailureKind(int status, VcsFailureKind expected)
    {
        _server.Given(Request.Create().WithPath(NotesPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody("""{"message":"nope"}"""));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(expected);
        result.Failure.HttpStatus.Should().Be(status);
        result.Failure.Message.Should().NotContain("nope", "bodies are never put into a message");
    }

    [Fact]
    public async Task CreateCommentAsync_TooManyRequests_IsRateLimitedForTheRetryAfterSeconds()
    {
        _server.Given(Request.Create().WithPath(NotesPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "30"));

        VcsCallResult<VcsCommentRef> result = await _sut.CreateCommentAsync(_repo, 7, "hello", _credential, default);

        result.Failure!.Kind.Should().Be(VcsFailureKind.RateLimited);
        result.Failure.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task DeleteCommentAsync_NoContent_Succeeds()
    {
        _server.Given(Request.Create().WithPath(NotesPath + "/987").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(204));

        VcsCallResult result = await _sut.DeleteCommentAsync(_repo, 7, Ref(42, 987), _credential, default);

        result.IsSuccess.Should().BeTrue();
    }

    private static VcsCommentRef Ref(long projectId, long noteId) =>
        new($$"""{"projectId":{{projectId}},"noteId":{{noteId}}}""");

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
