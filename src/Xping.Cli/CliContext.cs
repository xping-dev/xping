// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Cli;

/// <summary>Where commands write. Tests pass their own writers.</summary>
internal sealed record CliContext(TextWriter Output, TextWriter Error);
