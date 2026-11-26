// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.Abstractions.Tests;

public sealed class CiProviderTests
{
    [Fact]
    public void NoneIsTheDefault() => Assert.Equal(CiProvider.None, default);

    [Fact]
    public void TheThreeSupportedProvidersAreDefined() =>
        Assert.Equal([CiProvider.None, CiProvider.GitHub, CiProvider.GitLab, CiProvider.AzureDevOps], Enum.GetValues<CiProvider>());
}
