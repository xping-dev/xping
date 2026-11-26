// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text.Json;
using Xping.Testing;

namespace Xping.History.Tests;

public sealed class HistorySchemaTests
{
    [Theory]
    [InlineData("meta")]
    [InlineData("run")]
    [InlineData("stats")]
    public void EveryHistoryFileKindRequiresSchemaVersion(string kind)
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Combine("schema", "history.schema.json")));

        var required = schema.RootElement.GetProperty("$defs").GetProperty(kind)
            .GetProperty("required").EnumerateArray().Select(e => e.GetString());

        Assert.Contains("schemaVersion", required);
    }

    [Fact]
    public void TheGitBranchStoreIsTheFirstKind() => Assert.Equal(HistoryStoreKind.GitBranch, default);
}
