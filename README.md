# Xping

**Xping checks that the safety net wasn't weakened.**

Coding agents (and people) sometimes make a red build green by weakening the tests: deleting an assertion, raising a timeout, skipping a test as "flaky", mocking the class under test. Xping detects these changes in pull/merge requests and backs each finding with execution history kept in **your own repository**.

> Status: early development (`2.0.0-alpha`). The commands below parse their options but are not implemented yet. They exit with code 69.

## What it detects

| Rule | Finding |
|---|---|
| XP001 | Assertion removed or weakened |
| XP002 | Timeout loosened (with history: "p99 236 ms over 412 runs, the runner is slow, re-run") |
| XP003 | Test skipped or deleted (with history: "0 failures in 380 runs, not flaky") |
| XP004 | Class under test replaced by a mock |
| XP005 | Failure triage: flaky versus real regression |

## Principles

- **No data leaves your environment.** No Xping service, no telemetry. The only network calls go to your CI provider's API.
- **Deterministic.** No LLM calls.
- **No SDK required.** Reads JUnit XML and TRX. The Xping SDK session format is an optional, richer input.
- **Runs in your CI:** GitHub Actions, GitLab CI, Azure Pipelines.
- **History lives in your repo**, on a branch written only by the default-branch pipeline.

## Commands

| Command | Purpose |
|---|---|
| `xping check` | Analyse a pull/merge request, or the working tree with `--local` |
| `xping record` | Append a default-branch run to history |
| `xping compact` | Roll raw runs up into baselines and prune old ones |
| `xping backfill` | Import past test results from the CI provider's API |
| `xping init` | Add CI steps, `.xping.yml`, AGENTS.md rules and test fingerprints |
| `xping fingerprint add` | Give every test without one a stable fingerprint |
| `xping skills install` | Install coding-agent skills |

Exit codes: [docs/reference/exit-codes.md](docs/reference/exit-codes.md).

## Install

Not released yet. Planned: self-contained binaries for linux-x64, linux-arm64, win-x64 and osx-arm64, an OCI image at `ghcr.io/xping-dev/xping`, and a .NET tool:

```sh
dotnet tool install -g Xping.Cli --prerelease
```

## Build from source

```sh
dotnet build
dotnet test
dotnet run --project src/Xping.Cli -- --help
```

## Repository layout

| Path | What |
|---|---|
| `src/` | The CLI and its libraries (see [AGENTS.md](AGENTS.md) for module boundaries) |
| `tests/` | One test project per library, plus CLI integration tests |
| `schema/` | Versioned JSON schemas for session and history files |
| `integrations/` | Thin CI wrappers (Azure DevOps task) |
| `samples/` | Sample configuration and test reports |
| `docs/` | Specs and generated reference. Product docs live in a separate repository. |

## License

[MIT](LICENSE). "Xping" is a trademark, see [TRADEMARK.md](TRADEMARK.md).
