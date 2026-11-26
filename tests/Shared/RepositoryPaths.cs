// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Testing;

/// <summary>Locates files in the repository from a test's output directory.</summary>
internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string Combine(params string[] parts) => Path.Combine([Root, .. parts]);

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Xping.slnx")))
                return dir.FullName;
        }

        throw new InvalidOperationException("Xping.slnx not found above " + AppContext.BaseDirectory);
    }
}
