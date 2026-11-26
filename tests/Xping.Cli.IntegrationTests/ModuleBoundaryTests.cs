// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Xml.Linq;
using Xping.Testing;

namespace Xping.Cli.IntegrationTests;

/// <summary>
/// Project references follow the module boundaries in docs/specs/migration-plan.md (section 4).
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] OnCore = ["Xping.Core", "Xping.Session"];

    private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.Ordinal)
    {
        ["Xping.Session"] = [],
        ["Xping.Core"] = ["Xping.Session"],
        ["Xping.Evidence"] = OnCore,
        ["Xping.History"] = OnCore,
        ["Xping.Analyzers.CSharp"] = OnCore,
        ["Xping.Analyzers.TreeSitter"] = OnCore,
        ["Xping.Reporters"] = OnCore,
        ["Xping.Providers.Abstractions"] = OnCore,
        ["Xping.Providers.GitHub"] = ["Xping.Providers.Abstractions"],
        ["Xping.Providers.GitLab"] = ["Xping.Providers.Abstractions"],
        ["Xping.Providers.AzureDevOps"] = ["Xping.Providers.Abstractions"],
    };

    public static TheoryData<string> SourceProjects() =>
        new(Directory.GetDirectories(RepositoryPaths.Combine("src")).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(SourceProjects))]
    public void ProjectReferencesStayInsideTheModuleBoundary(string project)
    {
        if (project == "Xping.Cli")
            return; // The CLI composes everything.

        Assert.True(Allowed.TryGetValue(project, out string[]? allowed), $"{project} has no entry in the boundary table. Add one.");

        var references = References(project);

        Assert.All(references, reference => Assert.Contains(reference, allowed));
    }

    [Fact]
    public void NothingReferencesTheCli()
    {
        var offenders = Directory.GetDirectories(RepositoryPaths.Combine("src"))
            .Select(Path.GetFileName).OfType<string>()
            .Where(project => References(project).Contains("Xping.Cli"));

        Assert.Empty(offenders);
    }

    private static List<string> References(string project)
    {
        var csproj = XDocument.Load(RepositoryPaths.Combine("src", project, project + ".csproj"));

        return csproj.Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value.Replace('\\', '/')))
            .ToList();
    }
}
