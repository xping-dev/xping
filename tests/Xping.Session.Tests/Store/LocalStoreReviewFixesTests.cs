/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xping.Session.Models;
using Xping.Session.Models.Builders;
using Xping.Session.Models.Executions;
using Xping.Session.Store;
using Xping.Session.Store.Internals;

namespace Xping.Session.Tests.Store;

public sealed class LocalStoreReviewFixesTests : IDisposable
{
    private readonly string _root;

    public LocalStoreReviewFixesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "xping-review-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static TestSession BuildSession(
        string name = "T", IDictionary<string, string>? customProperties = null) =>
        new TestSessionBuilder()
            .WithSessionId(Guid.NewGuid())
            .WithStartedAt(DateTime.UtcNow)
            .WithEnvironmentInfo(new EnvironmentInfoBuilder()
                .AddCustomProperties(customProperties ?? new Dictionary<string, string>())
                .Build())
            .AddExecutions(
            [
                new TestExecutionBuilder()
                    .WithIdentity(new TestIdentityBuilder()
                        .WithTestFingerprint("fp")
                        .WithAssembly("MyApp.Tests")
                        .WithDisplayName(name)
                        .Build())
                    .WithTestName(name)
                    .WithOutcome(TestOutcome.Passed)
                    .Build()
            ])
            .WithSessionState(TestSessionState.Finalized)
            .Build();

    private JsonSessionStore CreateStore() =>
        new(new LocalStoreOptions(), NullLogger.Instance, storePath: _root);

    [Fact]
    public void AMalformedSessionDoesNotBreakDelete()
    {
        // Arrange — a scoped delete has to read each file to learn its assembly, so a corrupt file
        // must cost that file alone rather than the whole operation.
        JsonSessionStore store = CreateStore();
        store.Write(BuildSession("good"));

        string path = Path.Combine(
            LocalStorePathResolver.GetSessionsDirectory(_root),
            "session-0638999999999999998-deadbee0.json.gz");

        using (var file = new FileStream(path, FileMode.Create))
        using (var gzip = new GZipStream(file, CompressionLevel.Fastest))
        using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
        {
            writer.WriteLine("{ not json either");
        }

        // Act
        store.Delete("Nonexistent.Tests");

        // Assert — the readable session was out of scope, so it survives.
        Assert.Single(store.ReadRecent(10).Sessions);
    }

    [Fact]
    public void ResolveFallsBackToTheProfileWhenTheRepositoryRootIsNotWritable()
    {
        // The documented fallback covers "the root is not writable", but resolution never checked,
        // so a read-only checkout silently lost every run instead of falling back.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // chmod semantics differ; the behaviour is verified on Unix CI.

        // Arrange — a repository root that cannot be written to.
        string repo = Path.Combine(_root, "readonly-repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        string start = Path.Combine(repo, "tests", "bin");
        Directory.CreateDirectory(start);

        File.SetUnixFileMode(
            repo,
            UnixFileMode.UserRead | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

        try
        {
            // Act
            string? resolved = LocalStorePathResolver.Resolve(start);

            // Assert — not the unwritable repository root.
            Assert.NotNull(resolved);
            Assert.NotEqual(
                Path.Combine(repo, LocalStorePathResolver.StoreDirectoryName), resolved);
        }
        finally
        {
            File.SetUnixFileMode(
                repo,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public void ResolveStillPrefersAWritableRepositoryRoot()
    {
        string repo = Path.Combine(_root, "writable-repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        string start = Path.Combine(repo, "tests", "bin");
        Directory.CreateDirectory(start);

        Assert.Equal(
            Path.Combine(repo, LocalStorePathResolver.StoreDirectoryName),
            LocalStorePathResolver.Resolve(start));
    }


}
