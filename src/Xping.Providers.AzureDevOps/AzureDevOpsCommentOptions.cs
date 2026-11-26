// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.AzureDevOps;

/// <summary>How Xping's pull request thread is created on Azure DevOps.</summary>
/// <remarks>Replaces the dashboard's <c>PrReportOptions.AzureDevOps</c> section.</remarks>
internal sealed class AzureDevOpsCommentOptions
{
    /// <summary>Status of the thread the comment opens.</summary>
    public string ThreadStatus { get; set; } = "closed";
}
