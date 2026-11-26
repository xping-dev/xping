// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Microsoft.Extensions.DependencyInjection;
using Xping.Cli.Services;
using Xping.Core.Triage;
using Xping.Core.Triage.Providers;
using Xping.Core.Triage.Windowing;
using Xping.Providers.Environment;

namespace Xping.Cli.Hosting;

/// <summary>The CLI's service registrations.</summary>
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddXpingCliServices(
        this IServiceCollection services,
        TextWriter output,
        TextWriter error,
        TextReader input,
        Terminals terminals)
    {
        services.AddSingleton(new ConsoleIO(output, error, input, terminals));
        services.AddSingleton<GlobalOptions>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEnvironmentVariableProvider, ProcessEnvironment>();
        services.AddSingleton<ILocalSessionStoreFactory, LocalSessionStoreFactory>();
        services.AddXpingLocalAnalysis();
        return services;
    }

    // TODO(spec): XP005 consumes these. Registered now so the triage engine stays composable.
    private static IServiceCollection AddXpingLocalAnalysis(this IServiceCollection services)
    {
        services.AddSingleton<IWindowResolver, WindowResolver>();
        services.AddSingleton<FindingCoordinator>();
        services.AddSingleton<IFindingProvider, FailureModeProvider>();
        services.AddSingleton<IFindingProvider, RetryProvider>();
        services.AddSingleton<IFindingProvider, DurationProvider>();
        services.AddSingleton<IFindingProvider, VanishedProvider>();
        services.AddSingleton<IFindingProvider, ParallelSensitiveProvider>();
        services.AddSingleton<IFindingProvider, TimeSensitiveProvider>();
        return services;
    }
}
