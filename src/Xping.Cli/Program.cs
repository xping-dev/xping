// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Cli;

/// <summary>Entry point of the <c>xping</c> executable.</summary>
internal static class Program
{
    internal static int Main(string[] args) => XpingApp.Run(args, Console.Out, Console.Error);
}
