# AGENTS.md

Instructions for coding agents (and people) working in this repo. Keep it short: only what you can't infer from the code. If something here goes stale, fix it in the same change.

## What this is

`xping` is an open-source (MIT), CI-native CLI. It detects when tests are weakened (assertions removed, timeouts loosened, tests skipped, class under test mocked) and backs findings with execution history stored in the customer's own repo. Source of truth for requirements: `docs/specs/xping-redesign-prespec.md`. Migration status: `docs/specs/migration-plan.md`.

Nothing is released yet. There is no compatibility contract: rename, reshape or delete APIs freely. No `[Obsolete]` shims. **Exception:** `schema/*.json` and the files they describe are a public contract. Old session files stay readable.

## Hard rules

- **No data leaves the customer environment.** No Xping service, no telemetry, no outbound calls except to the CI provider's own API. Never add an HTTP client for anything else.
- **Deterministic.** No LLM calls, anywhere.
- **No SDK dependency.** Never reference `Xping.Sdk.*`. The `[TestFingerprint]` attribute is matched by name, not by type.
- **Not Native AOT, not trimmed.** Roslyn relies on reflection.

## Commands

```sh
dotnet build                                          # warnings are errors
dotnet test                                           # Microsoft.Testing.Platform, all projects
dotnet test --project tests/Xping.Core.Tests          # one project
dotnet format --verify-no-changes                     # style check
dotnet run --project src/Xping.Cli -- --help          # run the CLI
dotnet publish src/Xping.Cli -c Release -r osx-arm64  # self-contained single-file binary
```

Before calling a change done: the build is clean and the affected test projects pass.

## Module boundaries

Enforced by `tests/Xping.Cli.IntegrationTests/ModuleBoundaryTests.cs`.

| Project | May reference |
|---|---|
| `Xping.Session` (netstandard2.0 + net10.0, published) | nothing |
| `Xping.Core` | Session |
| `Xping.Evidence`, `Xping.History`, `Xping.Analyzers.*`, `Xping.Reporters`, `Xping.Providers.Abstractions` | Core, Session |
| `Xping.Providers.GitHub` / `GitLab` / `AzureDevOps` | Providers.Abstractions |
| `Xping.Cli` | everything; nothing references it |

`Xping.Session` is consumed by the Xping SDK (netstandard2.0): no newer BCL APIs without a polyfill.

## Code style

Enforced by `.editorconfig` and `Directory.Build.props` (`AnalysisMode=All`, warnings as errors, nullable on). Beyond that:

- Every `.cs` file starts with:
  ```csharp
  // SPDX-License-Identifier: MIT
  // Copyright (c) Xping
  ```
- Package versions live only in `Directory.Packages.props`. A new dependency in a shipped project needs a reason in the PR.
- Public members need XML docs (the build enforces it).
- Comments explain *why*, not *what*.
- Suppress an analyzer only with a comment saying why.
- Unbuilt work is marked `TODO(spec)` with a pointer to what is missing.

## CLI contract

- Exit codes are defined once in `src/Xping.Core/ExitCodes.cs` and documented in `docs/reference/exit-codes.md`. A test keeps them in sync. Never reuse a code for a new meaning.
- Parse errors exit 64, not System.CommandLine's default 1 (1 means "blocked").
- `--format json` output is for agents: stable, documented, versioned.

## Tests

- xUnit v3 on Microsoft.Testing.Platform. Test projects mirror `src/` one to one.
- Name tests for the behaviour, as a sentence: `ABadCommandLineIsAUsageError`.
- No network, no real CI environment variables, no sleeps. Fixtures go in `samples/`.

## Workflow

- Branch off `main`. Conventional commits: `feat(cli): …`, `fix(session): …`, `docs: …`, `ci: …`.
- Code that diverges from a spec updates the spec in the same PR.
- Never commit secrets, `bin/`, `obj/`, `artifacts/` or `.xping/`.
