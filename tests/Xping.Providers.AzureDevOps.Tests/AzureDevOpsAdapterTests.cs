// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.AzureDevOps.Tests;

// TODO(spec): detection, PR context and comment contract tests (Phase 3 migration).
public sealed class AzureDevOpsAdapterTests
{
    [Fact]
    public void HandlesItsOwnProvider() => Assert.Equal(CiProvider.AzureDevOps, new AzureDevOpsAdapter().Provider);
}
