// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.GitHub.Tests;

// TODO(spec): detection, PR context and comment contract tests (Phase 3 migration).
public sealed class GitHubAdapterTests
{
    [Fact]
    public void HandlesItsOwnProvider() => Assert.Equal(CiProvider.GitHub, new GitHubAdapter().Provider);
}
