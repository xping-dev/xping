// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xping.Cli.Commands;
using Xping.Cli.Hosting;
using Xping.Core;

namespace Xping.Cli;

/// <summary>Builds the command tree and runs one invocation.</summary>
internal static class XpingApp
{
    internal const string Description =
        "Xping detects when tests are weakened and backs findings with execution history kept in your own repository.";

    public static RootCommand BuildRootCommand(CliContext context, Option<bool> verbose)
    {
        var root = new RootCommand(Description) { verbose };
        root.Subcommands.Add(CheckCommand.Create(context));
        root.Subcommands.Add(RecordCommand.Create(context));
        root.Subcommands.Add(CompactCommand.Create(context));
        root.Subcommands.Add(BackfillCommand.Create(context));
        root.Subcommands.Add(InitCommand.Create(context));
        root.Subcommands.Add(FingerprintCommand.Create(context));
        root.Subcommands.Add(SkillsCommand.Create(context));

        // The built-in action prints the informational version, build metadata included
        // (2.0.0-alpha.1+<sha>). Scripts compare versions, so print the SemVer alone.
        foreach (Option option in root.Options)
        {
            if (option is VersionOption versionOption)
                versionOption.Action = new PrintVersionAction(context.Output);
        }

        return root;
    }

    /// <summary>The CLI version without build metadata, e.g. <c>2.0.0-alpha.1</c>.</summary>
    internal static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        string? informational = typeof(XpingApp).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
            return "0.0.0";

        int plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? informational[..plus] : informational;
    }

    private sealed class PrintVersionAction(TextWriter output) : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            output.WriteLine(Version);
            return ExitCodes.Success;
        }
    }

    public static int Run(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        TextReader? input = null,
        Terminals? terminals = null,
        CancelKeyHandler? cancelKeys = null,
        Action<IServiceCollection>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(error);
        CancellationToken cancellationToken = cancelKeys?.Token ?? CancellationToken.None;
        GlobalOptions? options = null;

        // Building the services and the parser is inside the try: a crash while starting is still a crash.
        try
        {
            using ServiceProvider services = BuildServices(
                output, error, input ?? TextReader.Null, terminals ?? Terminals.All(false), configureServices);
            var context = new CliContext(output, error);
            var verbose = new Option<bool>("--verbose")
            {
                Description = "Write diagnostics to standard error.",
                Recursive = true,
            };

            // No arguments, or the bare word `help`: show help. No arguments is still a usage error.
            bool noArgs = args.Count == 0;
            IReadOnlyList<string> effectiveArgs = noArgs || args[0] == "help" ? ["--help"] : args;
            ParseResult parseResult = BuildRootCommand(context, verbose).Parse(effectiveArgs);

            // System.CommandLine exits 1 on a parse error, which is the "blocked" code here.
            if (parseResult.Action is ParseErrorAction)
            {
                // Scrubbed: the parser quotes what it could not place, and an argument can be a secret.
                foreach (ParseError parseError in parseResult.Errors)
                    error.WriteLine($"xping: {Redaction.Scrub(parseError.Message)}");

                error.WriteLine(CommandPath(parseResult.CommandResult) is { } path
                    ? $"Run `xping {path} --help` for usage."
                    : "Run `xping --help` for usage.");
                return ExitCodes.UsageError;
            }

            options = services.GetRequiredService<GlobalOptions>();
            options.Verbose = parseResult.GetValue(verbose);

            var invocation = new InvocationConfiguration
            {
                Output = output,
                Error = error,
                // Main owns Ctrl+C; the parser's own handler would race it for the same signal.
                ProcessTerminationTimeout = null,
                // Escaping exceptions are reported below, scrubbed. The default handler prints the raw
                // message and stack trace, which is where a token in an exception message would leak.
                EnableDefaultExceptionHandler = false,
            };

            // Blocking is safe: a console app has no synchronization context to deadlock on.
            int exitCode = parseResult.InvokeAsync(invocation, cancellationToken).GetAwaiter().GetResult();
            return noArgs ? ExitCodes.UsageError : exitCode;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Not only OperationCanceledException: work torn down by Ctrl+C can fail some other way,
            // and the user still asked it to stop.
            error.WriteLine("Cancelled.");
            return ExitCodes.Cancelled;
        }
        catch (Exception ex)
        {
            error.WriteLine($"xping: internal error: {Redaction.Scrub(ex.Message)}");
            if (options?.Verbose == true)
                error.WriteLine(Redaction.Scrub(ex.ToString()));
            return ExitCodes.InternalError;
        }
    }

    private static ServiceProvider BuildServices(
        TextWriter output,
        TextWriter error,
        TextReader input,
        Terminals terminals,
        Action<IServiceCollection>? configureServices)
    {
        var services = new ServiceCollection();

        // No console logger: it writes straight to System.Console, around the output/error seam and
        // into --format json output. The one provider is silent unless --verbose was given.
        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton<ILoggerProvider, VerboseLoggerProvider>();
        services.AddXpingCliServices(output, error, input, terminals);
        configureServices?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static string? CommandPath(CommandResult result)
    {
        var names = new List<string>();
        for (SymbolResult? current = result; current is CommandResult { Command: not RootCommand } command; current = command.Parent)
            names.Insert(0, command.Command.Name);
        return names.Count == 0 ? null : string.Join(' ', names);
    }
}
