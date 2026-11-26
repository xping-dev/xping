// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Evidence;

/// <summary>Test evidence formats Xping reads.</summary>
/// <remarks>TODO(spec): one importer per format, each producing the session model.</remarks>
public enum EvidenceFormat
{
    /// <summary>JUnit XML, the universal format.</summary>
    JUnit,

    /// <summary>Visual Studio TRX.</summary>
    Trx,

    /// <summary>An Xping session file written by the Xping SDK.</summary>
    XpingSession,

    /// <summary>Stryker mutation report (JSON). Optional.</summary>
    StrykerJson,
}
