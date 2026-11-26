// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers;

/// <summary>Supported CI providers.</summary>
public enum CiProvider
{
    /// <summary>Not running in a known CI provider (developer or agent machine).</summary>
    None,

    /// <summary>GitHub Actions.</summary>
    GitHub,

    /// <summary>GitLab CI.</summary>
    GitLab,

    /// <summary>Azure Pipelines.</summary>
    AzureDevOps,
}
