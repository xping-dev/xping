# Plan: xping-dev/sdk-dotnet depends on the Xping.Session package

**Goal:** the SDK writes sessions through the published `Xping.Session` package. Then the session format is defined once, in xping-dev/xping, and the SDK and the CLI cannot drift apart. Today `Xping.Sdk.Core` still carries its own copy of the model and store (the code this repository extracted).

## What Xping.Session already provides (2.0.0-alpha.1, netstandard2.0)

- **Public:** the model (`TestSession`, `TestExecution`, `TestIdentity`, `EnvironmentInfo`, statistics, PR context), the builders, `SessionSchema`, `SessionProducer`, `SessionDocument` (reads v0 and v1), `XpingSerializerOptions`, `IXpingSerializer`, `ITestIdentityGenerator`.
- **Internal (exposed to this repository only through `InternalsVisibleTo`):** the store (`ILocalSessionStore`, `LocalSessionStore`, `JsonSessionStore`, retention, path resolution), `TestIdentityGenerator`, `XpingJsonSerializer`, `MessageNormaliser`, stack-frame helpers.

`InternalsVisibleTo` across repositories is not an option: the SDK needs a public surface.

## Steps

### In xping-dev/xping
1. **Decide the public writer API.** Recommendation: a small `SessionStore` facade (`Create(options, logger)`, `Write(TestSession)`, `ReadRecent(n)`), a public `TestIdentityGenerator`, and `XpingJsonSerializer` behind `IXpingSerializer` with a public factory. Store internals (file names, header scan, retention) stay internal.
2. **Make it a contract:**
   - add API compatibility checks (`Microsoft.CodeAnalysis.PublicApiAnalyzers` or package validation against the previous version) to `Xping.Session` only;
   - add a test that a session written through the facade validates against `schema/session.schema.json`.
3. **Producer:** let the writer pass `SessionProducer` (`xping-sdk-dotnet`, SDK version), so files say who wrote them. Today the default is `xping-session`.
4. **Publish** `Xping.Session` 2.0.0-alpha.N.

### In xping-dev/sdk-dotnet
5. **Reference the package:** add `PackageReference Xping.Session`. Delete the SDK's own copies:
   - `Models/**`
   - `Services/LocalStore/**`
   - `Services/Serialization/**`
   - `Services/Identity/**`
   - `Services/Diagnostics/{RepositoryRoot,MessageNormaliser}.cs`, if they became public in step 1; otherwise keep the SDK-runtime parts only.
6. **Map namespaces:** `Xping.Sdk.Core.Models.*` → `Xping.Session.Models.*`. Adapter packages (NUnit, xUnit, MSTest) follow, because they only build `TestExecution`s.
7. **Keep in the SDK:** `[TestFingerprint]` / `XpingFingerprintAttribute`. The CLI matches it by name, so its package location doesn't matter.
8. **Drop Cloud from the SDK** (a separate decision, but it simplifies 5): the uploader, `ApiKey`/`ProjectId`/`ApiEndpoint`, `XpingMode.Cloud`, `LocalSessionProperties.Mode/ProjectId`. Sessions written afterwards no longer carry the Cloud properties. The CLI already reads them as opaque.
9. **Check compatibility:**
   - add an SDK test that a session it writes is read back by `SessionDocument.Read` with `schemaVersion: 1`;
   - add an SDK test that rc.8 files still in a developer's `.xping/` keep reading. The CLI covers the latter already (`LegacySessionImporter`).

## Risks

| Risk | Handling |
|---|---|
| The two repositories release on different cadences | `Xping.Session` follows SemVer for its public API; the SDK pins a minimum version. Schema changes add a new `schemaVersion` and an importer; they never change v1 in place. |
| netstandard2.0 constraints leak into the CLI | Only `Xping.Session` is netstandard2.0. Rules, analyzers and providers stay net10.0. |
| A hot path regresses (the SDK records per test) | Keep the `LocalStorePerformanceTests` budgets in `Xping.Session.Tests`. Add an SDK benchmark before switching. |
| The SDK has a test framework that tests itself through the SDK (dogfooding) | Unchanged: it records through the package instead of project-internal code. |
