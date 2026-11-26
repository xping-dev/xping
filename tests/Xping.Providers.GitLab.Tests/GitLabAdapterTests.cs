// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.GitLab.Tests;

// TODO(spec): detection, PR context and comment contract tests (Phase 3 migration).
public sealed class GitLabAdapterTests
{
    [Fact]
    public void HandlesItsOwnProvider() => Assert.Equal(CiProvider.GitLab, new GitLabAdapter().Provider);
}
