// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Microsoft.Extensions.DependencyInjection;
using Xping.Cli.Hosting;
using Xping.Core.Triage.Providers;

namespace Xping.Cli.Tests.Hosting;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void EveryRegisteredServiceCanBeBuilt()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddLogging()
            .AddXpingCliServices(TextWriter.Null, TextWriter.Null, TextReader.Null, Terminals.All(false))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Equal(6, provider.GetServices<IFindingProvider>().Count());
    }
}
