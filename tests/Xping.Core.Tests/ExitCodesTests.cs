// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Reflection;
using System.Text.RegularExpressions;
using Xping.Testing;

namespace Xping.Core.Tests;

public sealed partial class ExitCodesTests
{
    private static Dictionary<string, int> Declared() =>
        typeof(ExitCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .ToDictionary(f => f.Name, f => (int)f.GetRawConstantValue()!);

    [Fact]
    public void EveryExitCodeIsDistinct()
    {
        var codes = Declared().Values.ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void TheReferencePageListsExactlyTheDeclaredCodes()
    {
        string page = File.ReadAllText(RepositoryPaths.Combine("docs", "reference", "exit-codes.md"));

        var documented = RowPattern().Matches(page)
            .ToDictionary(m => m.Groups["name"].Value, m => int.Parse(m.Groups["code"].Value, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(Declared().OrderBy(p => p.Key), documented.OrderBy(p => p.Key));
    }

    [GeneratedRegex(@"^\| (?<code>\d+) \| `(?<name>\w+)` \|", RegexOptions.Multiline)]
    private static partial Regex RowPattern();
}
