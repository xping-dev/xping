// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xping.Testing;

namespace Xping.Cli.IntegrationTests;

/// <summary>
/// No data leaves the customer environment: the CLI must not depend on the Xping SDK, ship a
/// telemetry client, or know a Xping Cloud endpoint. See AGENTS.md, "Hard rules".
/// </summary>
public sealed partial class NoCloudDependencyTests
{
    // Package IDs that must never be in the CLI's resolved graph, direct or transitive.
    private static readonly string[] DeniedPackagePrefixes =
    [
        "Xping.Sdk",
        "Microsoft.ApplicationInsights",
        "OpenTelemetry.Exporter",
        "Sentry",
        "Segment",
        "Mixpanel",
        "Datadog",
    ];

    // Hosts the old products called. docs.xping.io is documentation, not a data flow, and is allowed.
    [GeneratedRegex(
        @"(?<![\w.-])(?:(?!docs\.)[a-z0-9-]+\.)+xping\.io|xping\.io/start|\.azurewebsites\.net|applicationinsights\.azure\.com|dc\.services\.visualstudio\.com",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DeniedHost();

    [Theory]
    [InlineData("https://app.xping.io")]
    [InlineData("https://upload.xping.io/v1")]
    [InlineData("api.xping.io")]
    [InlineData("see https://xping.io/start")]
    [InlineData("xping-prod.azurewebsites.net")]
    public void TheHostPatternCatchesEveryKnownCloudHost(string text) => Assert.Matches(DeniedHost(), text);

    [Theory]
    [InlineData("https://docs.xping.io/cli/command-reference.html")]
    [InlineData("https://github.com/xping-dev/xping")]
    public void TheHostPatternAllowsDocumentationAndTheRepository(string text) => Assert.DoesNotMatch(DeniedHost(), text);

    [Fact]
    public void TheCliDependencyGraphHasNoSdkOrTelemetryPackage()
    {
        string assets = RepositoryPaths.Combine("src", "Xping.Cli", "obj", "project.assets.json");
        Assert.True(File.Exists(assets), $"{assets} is missing: restore the solution first.");

        using var document = JsonDocument.Parse(File.ReadAllText(assets));
        var libraries = document.RootElement.GetProperty("libraries").EnumerateObject()
            .Select(library => library.Name.Split('/')[0])
            .ToList();

        Assert.NotEmpty(libraries);
        var denied = libraries
            .Where(name => DeniedPackagePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Empty(denied);
    }

    [Fact]
    public void NoSourceFileNamesACloudHost()
    {
        var offenders = new List<string>();

        foreach (string file in Directory.EnumerateFiles(RepositoryPaths.Combine("src"), "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepositoryPaths.Root, file).Replace('\\', '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                continue;

            if (Path.GetExtension(file) is not (".cs" or ".csproj" or ".props" or ".targets" or ".json"))
                continue;

            foreach (Match match in DeniedHost().Matches(File.ReadAllText(file)))
                offenders.Add($"{relative}: {match.Value}");
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void NoShippedAssemblyIsTheSdkOrNamesACloudHost()
    {
        // The test output holds the built xping executable and everything it loads.
        string[] assemblies = Directory.GetFiles(AppContext.BaseDirectory, "*.dll");
        Assert.Contains(assemblies, path => Path.GetFileName(path) == "xping.dll");

        Assert.DoesNotContain(assemblies, path => Path.GetFileName(path).StartsWith("Xping.Sdk", StringComparison.OrdinalIgnoreCase));

        var offenders = new List<string>();
        foreach (string path in assemblies.Where(IsXpingAssembly))
        {
            byte[] bytes = File.ReadAllBytes(path);

            // Literals live in the #US heap and constants in the blob heap as UTF-16, at either byte
            // alignment; names and attributes are UTF-8.
            string[] texts =
            [
                Encoding.UTF8.GetString(bytes),
                Encoding.Unicode.GetString(bytes),
                Encoding.Unicode.GetString(bytes, 1, bytes.Length - 1),
            ];

            foreach (string text in texts)
            {
                foreach (Match match in DeniedHost().Matches(text))
                    offenders.Add($"{Path.GetFileName(path)}: {match.Value}");
            }
        }

        Assert.Empty(offenders);
    }

    private static bool IsXpingAssembly(string path)
    {
        string name = Path.GetFileName(path);
        return name == "xping.dll" ||
               (name.StartsWith("Xping.", StringComparison.Ordinal) && !name.Contains(".Tests", StringComparison.Ordinal) &&
                !name.Contains("IntegrationTests", StringComparison.Ordinal));
    }
}
