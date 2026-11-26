// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Reporters.Tests;

// TODO(spec): golden output per reporter.
public sealed class ReportFormatTests
{
    [Theory]
    [InlineData(ReportFormat.Json)]
    [InlineData(ReportFormat.Sarif)]
    [InlineData(ReportFormat.PrComment)]
    [InlineData(ReportFormat.JobSummary)]
    public void TheFormatsThePrespecNamesAreDefined(ReportFormat format) => Assert.True(Enum.IsDefined(format));
}
