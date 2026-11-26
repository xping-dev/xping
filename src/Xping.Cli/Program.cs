// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using Xping.Cli.Hosting;

namespace Xping.Cli;

/// <summary>Entry point of the <c>xping</c> executable.</summary>
internal static class Program
{
    internal static int Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        var cancelKeys = new CancelKeyHandler(cancellation);

        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) => e.Cancel = cancelKeys.OnCancelKeyPress();

        Console.CancelKeyPress += OnCancelKeyPress;
        try
        {
            return XpingApp.Run(args, Console.Out, Console.Error, Console.In, Terminals.Detect(), cancelKeys);
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
    }
}
