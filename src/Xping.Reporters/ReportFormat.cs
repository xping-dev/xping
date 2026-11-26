// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Reporters;

/// <summary>Output formats. The process exit code is reported in every format.</summary>
/// <remarks>TODO(spec): one reporter per format.</remarks>
public enum ReportFormat
{
    /// <summary>Human-readable terminal output.</summary>
    Text,

    /// <summary>Machine-readable JSON for coding agents and scripts.</summary>
    Json,

    /// <summary>SARIF 2.1.0.</summary>
    Sarif,

    /// <summary>A pull/merge request comment, posted through the CI provider.</summary>
    PrComment,

    /// <summary>The CI job summary (GitHub step summary, Azure Pipelines summary tab).</summary>
    JobSummary,
}
