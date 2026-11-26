/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using Xping.Session.Models;

namespace Xping.Session.Store;

/// <summary>
/// Environment properties the local store adds to the sessions it writes.
/// </summary>
/// <remarks>
/// These describe how a session was recorded rather than what it recorded, so they travel on
/// <see cref="Xping.Session.Models.Environments.EnvironmentInfo.CustomProperties"/> beside the Git
/// metadata rather than as fields on <see cref="TestSession"/> itself. Files written by
/// Xping.Sdk.Core 1.0.0-rc.x may also carry <c>Xping.Mode</c> and <c>Xping.ProjectId</c>; they are
/// Cloud-only and read as opaque properties.
/// </remarks>
internal static class LocalSessionProperties
{
    /// <summary>
    /// The property key holding the test runner the session ran on: <see cref="VSTestRunner"/> or
    /// <see cref="TestingPlatformRunner"/>. Absent when the SDK could not tell.
    /// </summary>
    /// <remarks>
    /// <c>dotnet test</c> takes different arguments on each runner, so a command the CLI prints to run
    /// a test again depends on it. Left out rather than guessed: a wrong runner prints a command that
    /// fails, where a missing one prints none. <see cref="TestingPlatformRunner"/> covers both ways
    /// to start a Microsoft.Testing.Platform app, <c>dotnet test</c> and the test executable itself;
    /// they take the same filter arguments.
    /// </remarks>
    public const string Runner = "Xping.Runner";

    /// <summary>
    /// The property key holding the test framework whose adapter recorded the session:
    /// <see cref="NUnitFramework"/>, <see cref="MSTestFramework"/>, <see cref="XUnitFramework"/> or
    /// <see cref="XUnitV3Framework"/>.
    /// </summary>
    public const string TestFramework = "Xping.TestFramework";

    /// <summary>
    /// The property key holding the path of the test project, relative to the repository root and with
    /// <c>/</c> separators, e.g. <c>tests/Shop.Tests/Shop.Tests.csproj</c>.
    /// </summary>
    /// <remarks>
    /// Stamped into the test assembly at build time by the <c>Xping.Sdk.Core.targets</c> file the
    /// package ships. Absent when the project was not built in a repository, was built with
    /// <c>GenerateAssemblyInfo=false</c>, or when the session's test assemblies name different
    /// projects.
    /// </remarks>
    public const string ProjectPath = "Xping.ProjectPath";

    /// <summary>The <see cref="Runner"/> value for a run in a vstest test host.</summary>
    public const string VSTestRunner = "VSTest";

    /// <summary>The <see cref="Runner"/> value for a run on Microsoft.Testing.Platform.</summary>
    public const string TestingPlatformRunner = "MTP";

    /// <summary>The <see cref="TestFramework"/> value for the NUnit adapter.</summary>
    public const string NUnitFramework = "NUnit";

    /// <summary>The <see cref="TestFramework"/> value for the MSTest adapter.</summary>
    public const string MSTestFramework = "MSTest";

    /// <summary>The <see cref="TestFramework"/> value for the xUnit v2 adapter.</summary>
    public const string XUnitFramework = "xUnit";

    /// <summary>The <see cref="TestFramework"/> value for the xUnit v3 adapter.</summary>
    /// <remarks>
    /// Separate from <see cref="XUnitFramework"/>: only xUnit v3 runs on Microsoft.Testing.Platform,
    /// and the two adapters derive a theory row's identity differently.
    /// </remarks>
    public const string XUnitV3Framework = "xUnit.v3";
}
