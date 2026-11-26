# Migration plan: sdk-dotnet + dashboard → xping-dev/xping

> Status: **Phase 5 prepared on `docs/followups` (§7, §8)** · 2026-11-26
> Source of truth: [`xping-redesign-prespec.md`](xping-redesign-prespec.md). (The task brief calls it `redesign-prespec.md`; the file in the repo is `xping-redesign-prespec.md`.)
> This file will be updated through Phases 2–5 to show what was actually done, including deviations.

## 0. Source snapshots

Both local checkouts are on feature branches. Extraction and copying use `origin/main` at these commits, not the working copies:

| Repo | Ref | SHA | Commits | License |
|---|---|---|---|---|
| `xping-dev/sdk-dotnet` | `main` | `0a1a6e570c5c392c6f403e58712175f2ce57af1b` (2026-11-23) | 256 | MIT (file headers say "© 2026 Xping.io. All Rights Reserved. License: [MIT]") |
| `xping-dev/dashboard` | `main` | `f250a6ef8913e335bbae66555e47f6a638123077` (2026-11-23) | 1527 | **No LICENSE file. Headers say "All Rights Reserved."** (see Q1) |

sdk-dotnet history before `df5314c` (2026-01-02) belongs to the old web-availability SDK (`src/Xping.Sdk`, `Session/Comparison`, …). The path lists below leave it out.

## 1. Inventory and decisions

Legend: **MOVE** = `git filter-repo` with history · **COPY** = normal commit naming source repo, path, SHA · **DROP** = not migrated.

### 1.1 sdk-dotnet

| Source path | What it does | Decision | Target | Reason |
|---|---|---|---|---|
| `src/Xping.Sdk.Core/Models/**` (incl. `Builders`, `Executions`, `Environments`, `PullRequests`, `Statistics`) | `TestSession`, `TestExecution`, `TestIdentity`, env/PR context, builders | MOVE | `src/Xping.Session/Models/` | Canonical session model. Builders are reused by the JUnit/TRX importers. |
| `src/Xping.Sdk.Core/Services/LocalStore/**` | `.xping/sessions/session-<ticks>-<id>-<tags>.json.gz` reader/writer, header scan, retention, path resolver | MOVE | `src/Xping.Session/Store/` | Session store. The SDK will write through this package later. |
| `src/Xping.Sdk.Core/Services/Serialization/**` (+ older `src/Xping.Sdk.Core/Serialization/**`) | JSON options and serializer for session files | MOVE | `src/Xping.Session/Serialization/` | Needed to keep the old format readable. |
| `src/Xping.Sdk.Core/Services/Identity/**` | `TestIdentityGenerator` (fingerprint/FQN identity) | MOVE | `src/Xping.Session/Identity/` | Stable test identity is part of the schema (prespec §5, §7). Reads the attribute **by type** today; cut to by-name (§3). |
| `src/Xping.Sdk.Core/Services/Diagnostics/**` | `RepositoryRoot`, `MessageNormaliser`, stack-frame helpers | MOVE | `src/Xping.Session/Diagnostics/` | `RepositoryRoot` is used by the store path resolver; `MessageNormaliser` by failure signatures. Stack-frame extraction is SDK-runtime, deleted in a follow-up commit if unused. |
| `src/Xping.Sdk.Core/Polyfills/**`, `src/Xping.Sdk.Shared/**` | netstandard2.0 polyfills, `ArgumentValidation`, `DurationFormatter`, `XpingVersion` | MOVE | `src/Shared/` (source-only, linked with `<Compile Include>`) | Same pattern as today. Not a project, so it adds no Xping package dependency. |
| `src/Xping.Sdk.Core/Services/Environment/**` (+ older `src/Xping.Sdk.Core/Environment/**`) | Detects OS, CPU, runner, CI provider from env vars | MOVE | `src/Xping.Providers.Abstractions/Environment/` | Feeds `xping record` runner baselines (prespec §5). Split per provider in a follow-up commit. |
| `src/Xping.Sdk.Core/Services/PullRequest/**` | GitHub / GitLab / ADO / Jenkins PR-context detectors, `GitRepositoryReader` | MOVE | `src/Xping.Providers.Abstractions/PullRequest/` → then per provider | "CI environment detection" row. **Jenkins detector dropped** (R3 lists three providers). |
| `src/Xping.Cli/` (Program, `Hosting/`, `Configuration/`, `Commands/`, `Reporting/`, `Services/`) | System.CommandLine host, global options, console IO, cancel handling, config loader, terminal glyphs/text | MOVE | `src/Xping.Cli/` | CLI plumbing. Old commands `report`, `repro`, `where`, `clear` are removed from the command tree in a follow-up commit (Q3). |
| `src/Xping.Cli/Report/Scoring/**` | Wilson, Fisher exact, Brunner–Munzel, Hodges–Lehmann, Kendall τ, Cochran–Armitage, BH-FDR, quantiles, robust dispersion | MOVE | `src/Xping.Core/Statistics/` | Pure, tested statistics for XP002 verdicts, XP005, baselines. |
| `src/Xping.Cli/Report/Signatures/**` | Failure signature (normalised message + site) | MOVE | `src/Xping.Core/Signatures/` | XP005 needs "same failure as on main". |
| `src/Xping.Cli/Report/{Providers,Indexes,Windowing,Model}/**`, `LatestRunAnalyzer`, `FindingCoordinator`, … | Local flakiness finding providers (duration, retry, time-sensitive, parallel-sensitive, vanished, failure-mode), session indexes, windows | MOVE (lands in `src/Xping.Cli/Report/`) | then `git mv` to `src/Xping.Core/Triage/` **or** delete (Q3) | Seed for XP005, but this is the old flaky product. |
| `src/Xping.Cli/Report/{Contract,Rendering}/**`, `CtaThrottle`, `ReproCommand`, `ShellDialect/Quote` | Report envelope (incl. `CloudDtos`), text/JSON renderers, repro commands | MOVE then delete, or DROP | — | Coupled to Cloud envelope and the old `report` UX. Q3. |
| `src/Xping.Cli/Auth/**`, `src/Xping.Cli/Cloud/**`, `src/Xping.Cli/Commands/Auth/**`, `Configuration/CloudUrl.cs` | OAuth/device login, OS credential stores (P/Invoke), Cloud API client, enricher, project resolver | **DROP** (removed from history in pass 2) | — | Hard constraint: no Cloud, no auth. |
| `tests/Xping.Cli.Tests/**` minus `Auth/`, `Cloud/`, `Commands/Auth/` | CLI + report tests, 140 golden files | MOVE | `tests/Xping.Cli.Tests/`; stats tests renamed to `tests/Xping.Core.Tests/Statistics/` | Goldens follow the Q3 decision. |
| `tests/Xping.Sdk.Core.Tests/{Models,LocalStore,Serialization,Builders,Identity,Services/Diagnostics}` | Session/store tests, incl. sample session JSON | MOVE | `tests/Xping.Session.Tests/…` | Keeps format-compat coverage. |
| `tests/Xping.Sdk.Core.Tests/{Environment,Services/Environment,Services/PullRequest}` | Detector tests | MOVE | `tests/Xping.Providers.Abstractions.Tests/…` | |
| `src/Xping.Sdk.Core/Attributes/XpingFingerprintAttribute.cs` | `[TestFingerprint]` attribute | **Stays in sdk-dotnet** | — | Matched by name (prespec §7). |
| `src/Xping.Sdk.Core/Services/{Upload,Collector,Retry,Statistics,Reporting}`, `Configuration/`, `Extensions/`, `XpingContext*`, adapters (`Xping.Sdk.NUnit/XUnit/MSTest/XUnit.V3`), `samples/`, `nuspec/`, `scripts/`, benchmarks | SDK runtime, uploader (`upload.xping.io`), DI, test-framework adapters | **Stays in sdk-dotnet / DROP** | — | SDK-only or Cloud. |
| `docs/**` (docfx, 54 files on main; 15 unmerged commits on `feat/docs-overhaul`) | docs.xping.io site | COPY in Phase 5 (Q6) | `docs/` | Mostly SDK + Cloud content today. |
| `.github/workflows/*` | CI, nightly, release, credential-store matrix | DROP (reference only) | — | Rewritten in Phase 4 to the new supply-chain spec. |
| `Directory.Build.props`, `AGENTS.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md` | Analyzer settings (AnalysisMode=All, warnings as errors), conventions | COPY (adapted) | repo root | Plus Central Package Management, which sdk-dotnet does not use yet. |

### 1.2 dashboard

**Disagreement with the brief:** the multi-provider PR comment code is **not** in `Xping.Analytics`. `Xping.Analytics` is an Azure Functions host (queue/timer functions, AI summary, DB-backed publisher). The reusable code sits in `src/Infrastructure/Xping.Providers/VcsIntegration` and `src/Core/Xping.Application/Core/Services/{VcsIntegration,PullRequestReports/Rendering}`.

| Source path | What it does | Decision | Target | Reason |
|---|---|---|---|---|
| `Application/Core/Services/VcsIntegration/{IVcsProvider,VcsCallResult,VcsCommentRef,VcsRepositoryRef,VcsCredential}.cs` | Provider contract: resolve repo, create/update/find-by-marker/delete comment; typed failures (401/403/404/429/transient) | COPY | `src/Xping.Providers.Abstractions/` | Clean contract. Replace `Domain.Enums.VcsProvider` and `VcsCredentialKind` with local enums. |
| `Application/.../PullRequestReports/Rendering/IMarkdownDialect.cs`, `Dialects/*` | Per-provider markdown (details/summary, escaping, body budget: GitHub 65 536, GitLab, ADO) | COPY | `Abstractions` (interface, GFM, plain) + one dialect per provider project | Body-size limits and escaping are tested knowledge. |
| `Providers/VcsIntegration/GitHub/{GitHubApi,GitHubVcsProvider}.cs` | GitHub REST issue comments | COPY | `src/Xping.Providers.GitHub/` | |
| `Providers/VcsIntegration/GitLab/{GitLabApi,GitLabVcsProvider}.cs` | GitLab MR notes | COPY | `src/Xping.Providers.GitLab/` | |
| `Providers/VcsIntegration/AzureDevOps/{AzureDevOpsApi,AzureDevOpsVcsProvider}.cs` | ADO PR threads | COPY | `src/Xping.Providers.AzureDevOps/` | Uses `IOptions<PrReportOptions>`; replaced with a constructor parameter. |
| `Providers/VcsIntegration/Http/VcsHttpResponse.cs` | HTTP status → `VcsCallFailure` mapping | COPY | `Abstractions/Http/` | |
| `Providers/VcsIntegration/Http/PublicOnlyConnect.cs` | SSRF guard that refuses private IPs | **DROP** | — | Multi-tenant SaaS concern. In the customer's CI it would **block** self-hosted GitLab / ADO Server on private networks. |
| `GitHubAppJwtFactory`, `GitHubAppOptions`, `GitHubCredentialProvider`, `AccessTokenCredentialProvider`, `VcsCredentialProvider`, `DataProtectionVcsSecretProtector`, `VcsProviderFactory` | GitHub App auth, DB-stored encrypted tokens | **DROP** | — | CLI uses the job token from env (`GITHUB_TOKEN`, `CI_JOB_TOKEN`/PAT, `SYSTEM_ACCESSTOKEN`). |
| `tests/Xping.Providers.Tests/VcsIntegration/{GitHub,GitLab,AzureDevOps}VcsProviderContractTests.cs`, `AzureDevOpsContentLimitProbeTests.cs` | Provider contract tests against fake HTTP | COPY | per-provider test projects | |
| `Application/.../PullRequestReports/Rendering/Internals/PullRequestReportRenderer.cs` (845 lines) + snapshot `.md` files | PR comment layout for flaky/failure reports | **DROP** (kept as a reference link in the PrComment reporter TODO) | — | Bound to DB entities (`Domain.Entities.*`), portal links, flaky verdicts. Wrong content for test-weakening findings. |
| `Application/.../PullRequestReports/Internals/FailureVerdictService.cs` | Flaky-vs-real verdict per failure | DROP (reference for XP005) | — | DB/repository-coupled. |
| `Analytics/Services/FlakyDetection/**`, `ConfidenceScoring/**` | Classifier-vote flaky detection, Bayesian pass-rate priors | **DROP** | — | Bound to `Domain.Entities` and repositories (51 + 22 usings). The decision record was marked superseded on main (#522). sdk-dotnet's `Report/Scoring` covers the statistics with fewer dependencies. A pure helper (`RunsTest`, `ChangePointTest`) can be copied later if XP005 needs it. |
| `Analytics/Services/AiSummary/**`, `Providers/AiSummary/**` (Anthropic client) | LLM failure summaries | **DROP** | — | R5: no LLM calls. |
| Everything else (Portal, DataGateway, DataCollector, Repositories, AppHost, infra, Auth, Billing, KeyVault, Storage, Communication) | SaaS | **DROP** | — | Cloud. |

### 1.3 Nothing to reuse (fresh stubs)

- **JUnit / TRX / Stryker importers:** none exist in either repo. `Xping.Evidence` starts empty.
- **History stores, rules XP001–XP005, Roslyn/tree-sitter analyzers, reporters other than PR comment:** none exist. Stubs with `TODO(spec)`.

## 2. `git filter-repo` extraction (sdk-dotnet)

Run on a fresh clone in a scratch directory. sdk-dotnet itself is not touched.

```bash
brew install git-filter-repo          # not installed on this machine yet

SRC_SHA=0a1a6e570c5c392c6f403e58712175f2ce57af1b
WORK=$(mktemp -d)/sdk-extract
git clone --no-tags --single-branch --branch main git@github.com:xping-dev/sdk-dotnet.git "$WORK"
cd "$WORK" && git reset --hard "$SRC_SHA"
```

**Pass 1: keep and rename** (`$WORK/../sdk-paths.txt`). As run: git-filter-repo a40bce548d2c treats an `old==>new` line as a rename only, so the file also lists every left-hand path as a plain include line (prepended; not repeated below).

```text
# --- Xping.Session ---
src/Xping.Sdk.Core/Models/==>src/Xping.Session/Models/
src/Xping.Sdk.Core/Services/LocalStore/==>src/Xping.Session/Store/
src/Xping.Sdk.Core/Services/Serialization/==>src/Xping.Session/Serialization/
src/Xping.Sdk.Core/Serialization/==>src/Xping.Session/Serialization/
src/Xping.Sdk.Core/Services/Identity/==>src/Xping.Session/Identity/
src/Xping.Sdk.Core/Services/Diagnostics/==>src/Xping.Session/Diagnostics/
src/Xping.Sdk.Core/Polyfills/==>src/Shared/Polyfills/
src/Xping.Sdk.Shared/==>src/Shared/
tests/Xping.Sdk.Core.Tests/Models/==>tests/Xping.Session.Tests/Models/
tests/Xping.Sdk.Core.Tests/LocalStore/==>tests/Xping.Session.Tests/Store/
tests/Xping.Sdk.Core.Tests/Serialization/==>tests/Xping.Session.Tests/Serialization/
tests/Xping.Sdk.Core.Tests/Builders/==>tests/Xping.Session.Tests/Builders/
tests/Xping.Sdk.Core.Tests/Identity/==>tests/Xping.Session.Tests/Identity/
tests/Xping.Sdk.Core.Tests/Services/Diagnostics/==>tests/Xping.Session.Tests/Diagnostics/
# --- CI environment / PR detection ---
src/Xping.Sdk.Core/Services/Environment/==>src/Xping.Providers.Abstractions/Environment/
src/Xping.Sdk.Core/Environment/==>src/Xping.Providers.Abstractions/Environment/
src/Xping.Sdk.Core/Services/PullRequest/==>src/Xping.Providers.Abstractions/PullRequest/
tests/Xping.Sdk.Core.Tests/Services/Environment/==>tests/Xping.Providers.Abstractions.Tests/Environment/
tests/Xping.Sdk.Core.Tests/Environment/==>tests/Xping.Providers.Abstractions.Tests/Environment/
tests/Xping.Sdk.Core.Tests/Services/PullRequest/==>tests/Xping.Providers.Abstractions.Tests/PullRequest/
# --- Xping.Core (statistics, signatures) ---
src/Xping.Cli/Report/Scoring/==>src/Xping.Core/Statistics/
src/Xping.Cli/Report/Signatures/==>src/Xping.Core/Signatures/
regex:^tests/Xping\.Cli\.Tests/Report/(BenjaminiHochberg|BrunnerMunzel|CochranArmitage|EvidenceLevelResolver|FalseDiscoveryRate|FisherExact|HodgesLehmann|KendallTau|NormalTail|Quantile|RobustDispersion|WilsonInterval)Tests\.cs$==>tests/Xping.Core.Tests/Statistics/\1Tests.cs
regex:^tests/Xping\.Cli\.Tests/Report/FailureSignatureTests\.cs$==>tests/Xping.Core.Tests/Signatures/FailureSignatureTests.cs
# --- Xping.Cli (same path) ---
src/Xping.Cli/
tests/Xping.Cli.Tests/
```

```bash
git filter-repo --force --paths-from-file ../sdk-paths.txt \
  --message-callback '
import re
return re.sub(rb"\(#(\d+)\)", rb"(xping-dev/sdk-dotnet#\1)", message) + b"\n\nMigrated-From: xping-dev/sdk-dotnet\n"'
```

The callback rewrites `(#388)` to `(xping-dev/sdk-dotnet#388)` so PR links don't point at the new repo's issues.

**Pass 2: purge Cloud/auth from history**

```bash
git filter-repo --force --invert-paths \
  --path src/Xping.Cli/Auth/ --path src/Xping.Cli/Cloud/ --path src/Xping.Cli/Commands/Auth/ \
  --path src/Xping.Cli/Configuration/CloudUrl.cs \
  --path tests/Xping.Cli.Tests/Auth/ --path tests/Xping.Cli.Tests/Cloud/ --path tests/Xping.Cli.Tests/Commands/Auth/ \
  --path src/Xping.Providers.Abstractions/PullRequest/Internals/JenkinsPullRequestDetector.cs \
  --path src/Xping.Cli/Report/Contract/ --path src/Xping.Cli/Report/Rendering/ \
  --path tests/Xping.Cli.Tests/Report/Goldens/
cp .git/filter-repo/commit-map ../sdk-dotnet-commit-map.txt
```

**Merge into the new repo** (on a branch, never directly on `main`):

```bash
cd /Users/adrian/Dev/xping/xping
git switch -c migrate/sdk-dotnet
git remote add sdk-extract "$WORK" && git fetch sdk-extract main
git merge --allow-unrelated-histories --no-ff sdk-extract/main \
  -m "chore(migrate): import session store, CLI plumbing and CI detection from xping-dev/sdk-dotnet@0a1a6e5"
git remote remove sdk-extract
mkdir -p docs/specs/provenance && cp "$WORK/../sdk-dotnet-commit-map.txt" docs/specs/provenance/
```

Follow-up commits (normal, `git log --follow` keeps history): split `Providers.Abstractions/{Environment,PullRequest}` per provider, rename namespaces `Xping.Sdk.Core.*` → `Xping.Session.*` / `Xping.Providers.*` / `Xping.Core.*`, delete leftover SDK-runtime files.

**Dashboard copies:** one commit per provider area, e.g.
`feat(providers): copy GitHub comment client from xping-dev/dashboard@f250a6e:src/Infrastructure/Xping.Providers/VcsIntegration/GitHub/`.

## 3. Constraint violations and how they get cut

| Migrated piece | Violating dependency | Cut |
|---|---|---|
| `Xping.Cli` | `ProjectReference` → `Xping.Sdk.Core` (the SDK package) | Replaced by refs to `Xping.Session`, `Xping.Core`, `Xping.Providers.*`. Test fails if `Xping.Sdk.*` appears in `project.assets.json`. |
| `Xping.Cli` `Program.cs`, `Hosting/{GlobalOptions,ServiceCollectionExtensions,VerboseLoggerProvider}`, `Configuration/CliConfiguration*` | `Auth.*`, `Cloud.*`, `CloudUrl` (`https://app.xping.io`), token redaction tied to auth | Remove registrations, `--cloud-url` / login options, config keys. Keep the rest. |
| `Commands/ReportCommand.cs` | Calls `CloudEnricher`; CTA link `https://xping.io/start` | Command removed or reworked (Q3). |
| `Report/Contract/CloudDtos.cs`, `Rendering/CloudText.cs`, `FlaggedInCloud…` | Cloud response shapes | Delete. |
| `Xping.Cli.csproj` | `AllowUnsafeBlocks` (credential-store P/Invoke) | Remove. |
| `Session/Store/*`, `Providers/Environment/EnvironmentDetector` | `XpingConfiguration` (holds `ApiKey`, `ProjectId`, `ApiEndpoint=https://upload.xping.io/v1`) | Replace with small options records (`SessionStoreOptions`, `EnvironmentDetectorOptions`). `LocalSessionProperties.ProjectId` stays a plain optional field (Q5). |
| `Session/Identity/TestIdentityGenerator` | `typeof(XpingFingerprintAttribute)` from the SDK | Match attribute by simple name `TestFingerprint`/`TestFingerprintAttribute`/`XpingFingerprint…` via reflection metadata or syntax. |
| `Session/Models/TestSession.SdkVersion` | Field name assumes the SDK wrote the file | Keep for reading old files. v1 schema adds `producer { name, version }` (Phase 3). |
| `Xping.Sdk.Core.csproj` package refs | `Microsoft.Extensions.Http(.Resilience)`, `System.Net.Http.Json`, Serilog | Not carried into `Xping.Session` (no HTTP). |
| Dashboard providers | `Xping.Domain`, `Xping.Application`, `IOptions<PrReportOptions>`, `PublicOnlyConnect`, GitHub App JWT, DataProtection | Local enums/records, ctor params, plain `HttpClient`, env-var tokens. |
| Any doc link to `docs.xping.io` | Not a data flow, but matches a host string | Allow-list `docs.xping.io` and `xping.io` in **help text only**; the guard test denies `app.xping.io`, `upload.xping.io`, `api.xping.io`, `*.azurewebsites.net`, `applicationinsights`, `Xping.Sdk`. |

Guard test (Phase 3): scans `Xping.Cli`'s resolved dependency graph (`project.assets.json`) for `Xping.Sdk*`, telemetry packages (`Microsoft.ApplicationInsights*`, `OpenTelemetry.Exporter*`, `Sentry*`), and scans all `src/**` sources plus the published assembly's string table for denied hosts.

## 4. Layout adjustments (proposed, need your OK)

1. **`Xping.Session` targets `netstandard2.0;net10.0`**, not only net10.0. The SDK (`Xping.Sdk.Core`) is netstandard2.0 and will depend on this package. Everything else is net10.0.
2. **Add `src/Shared/`**: source-only files linked into projects (polyfills, `ArgumentValidation`). Not a package.
3. **Dependency rule refinement.** The brief says "Core and Session depend on nothing Xping-specific" and "providers depend only on Abstractions + Core". The rules engine reads the session model and env detection produces `EnvironmentInfo`, so I propose:
   - `Session` → nothing.
   - `Core` → `Session`.
   - `Providers.Abstractions` → `Core`, `Session`. `Providers.*` → `Providers.Abstractions` only.
   - `Evidence`, `History`, `Analyzers.*`, `Reporters` → `Core` (+ `Session`).
   - `Cli` → everything.
   Enforced by an architecture test (ref graph from csproj files).

## 5. NuGet availability (checked 2026-11-26)

| ID | Status |
|---|---|
| `Xping.Cli` | **Owned by `Xping`** (us). `1.0.0-rc.1`…`rc.8` published, dotnet tool, 312 downloads. Reuse the ID (Q4). |
| `Xping.Core` | **Owned by `Xping`** (us). One unlisted `1.0.0-rc1` from the old web-testing SDK. Reusable. |
| `Xping.Sdk.Core` (+ adapters) | Owned by `Xping`, `1.0.0-rc.8`. Unchanged. |
| `Xping.Session`, `Xping.Analyzers.CSharp`, `Xping.Providers.GitHub`, `Xping.Providers.GitLab`, `Xping.Providers.AzureDevOps` | **Free** |
| (also checked) `Xping.Evidence`, `Xping.History`, `Xping.Reporters`, `Xping.Providers.Abstractions`, `Xping.Analyzers.TreeSitter` | Free |
| package ID `xping` | **Taken by a third party** (`xPing` 1.0.5, owner `MemphiZ`, a dotnet tool, 1 270 downloads). |
| tool **command** `xping` | NuGet doesn't reserve command names; we keep `ToolCommandName=xping` on `Xping.Cli`. It only clashes if a user installs both tools globally. |

Recommendation: apply for **ID prefix reservation `Xping.*`** on nuget.org now. It stops anyone from taking the free IDs and adds the verified badge.

## 6. Decisions (2026-11-26)

| # | Decision |
|---|---|
| Q1 | Relicensing the files copied from dashboard under MIT is approved. Migrated headers become `// SPDX-License-Identifier: MIT`. |
| Q2 | Copy the provider clients and dialects only; the 845-line renderer is not copied. |
| Q3 | Option **a**: the engine moves to `Xping.Core/Triage/`; `Contract/`, `Rendering/`, goldens and the `report`/`repro`/`where`/`clear` commands are purged in pass 2. |
| Q4 | `Xping.Cli` continues at **`2.0.0-alpha.1`**. rc.1–rc.8 get a NuGet deprecation message pointing at 2.x; they are not unlisted. |
| Q5 | `ProjectId` is read from old files as an optional field and dropped from the v1 writer. |
| Q6 | Product docs live in a **separate public docs repo** (proposed `xping-dev/docs`), not here and not in `system-docs` (private, holds internal dashboard plans). Reference material generated from code stays in this repo and the docs build pulls it in: command reference (from `--help`), `schema/*.json`, rule pages (XP001…), `.xping.yml` reference. `docs/specs/` stays here. Phase 5 becomes: prepare the docs repo skeleton under `docs/specs/followups/docs-repo/` plus the DNS steps; this repo's `docs/` keeps only specs and generated reference. |
| Q7 | Jenkins PR detector is dropped. |
| Q8 | `git-filter-repo` is installed via Homebrew. |

## 6a. Original questions

1. **Dashboard license.** The dashboard has no LICENSE and headers say "All Rights Reserved". Copying the provider code into an MIT repo relicenses it. You own it, but please confirm. I'd also change all migrated headers to `// SPDX-License-Identifier: MIT` + `Copyright (c) Xping`.
2. **PR comment source.** The brief points at `Xping.Analytics`. The code is actually in dashboard `Xping.Providers/VcsIntegration` + `Xping.Application/.../Rendering/Dialects`, and I'd **not** copy the 845-line renderer (§1.2). OK?
3. **Old `report` / `repro` / `where` / `clear` commands and their engine** (~60 source files, 57 test classes, 140 goldens). Options:
   a. **Keep the engine, drop the UX (recommended):** move finding providers + indexes + windowing to `Xping.Core/Triage/` as the XP005 seed; purge `Contract/`, `Rendering/`, goldens and the four commands from the extraction.
   b. Keep everything as a hidden `xping report --local` until XP005 exists.
   c. Drop all of it except `Scoring` and `Signatures`.
4. **`Xping.Cli` version line.** `1.0.0-rc.8` is already on nuget.org. Either deprecate + unlist rc.1–rc.8 and restart at `0.1.0`, or continue at `2.0.0-alpha.1`. I'd deprecate/unlist and restart at `0.1.0`. That also makes the "deprecation message for the old CLI package" a per-version deprecation, not a package rename.
5. **`ProjectId` / `LocalSessionProperties`** in old session files: keep as an opaque optional field when reading, and drop it from the v1 writer? (I'd say yes.)
6. **Docs.** docs.xping.io today is mostly SDK + Cloud content. The SDK keeps living in sdk-dotnet. Should `docs/` here (a) host the whole site with an SDK section, or (b) host CLI docs only while SDK docs stay in sdk-dotnet? And copy from `main` or from the unmerged `feat/docs-overhaul` (15 commits ahead)?
7. **Jenkins PR detector:** drop (outside R3)? I'd drop it.
8. **Install `git-filter-repo`** via Homebrew on this machine: OK?

## 7. Deviations log

### Phase 2: scaffold (branch `chore/scaffold`)

Done: build config, 12 `src/` projects, 13 test projects, CLI command stubs, schema drafts, samples, Azure DevOps task skeleton, `ci.yml`, root docs. `dotnet build` (warnings as errors), `dotnet test` (61 tests) and `dotnet format --verify-no-changes` pass locally. `dotnet pack` produces `Xping.Cli` and `Xping.Session`. The tool installs and runs. `dotnet publish -r osx-arm64` produces one 34 MB `xping` file.

| Deviation | Why |
|---|---|
| `release.yml` not created yet | Phase 4 defines it in full (SBOM, cosign, attestations). |
| `docs/` holds only `specs/` and `reference/` | Q6: product docs go to a separate repo. |
| `src/Shared/` not created yet | It arrives with the Phase 3 extraction. |
| System.CommandLine **2.0.15**, not 3.0.0 | The migrated CLI plumbing is written against 2.0. Upgrade separately. |
| Tests use **xUnit v3 on Microsoft.Testing.Platform** (`global.json` → `test.runner`) | xunit.v3 4.x requires MTP on the .NET 10 SDK. sdk-dotnet's tests are xUnit v2 + VSTest (+ Moq in Core tests), so Phase 3 ports them: mostly namespace and `ValueTask` changes. |
| One version for every package (`2.0.0-alpha.1`), including the new `Xping.Session` | Keeps a single repository version. Only `Xping.Session` and `Xping.Cli` are packable for now. |
| New exit-code table: 0, 1 blocked, 2 input error, 64 usage, 69 not implemented, 70 internal | The old CLI's 2 (insufficient data), 3 (not found) and 4 (ambiguous) belonged to `report`, which is dropped (Q3). |
| Per-project tests are placeholders (marked `TODO(spec)`) | The stubs carry no behaviour yet. The CLI, exit-code, schema and module-boundary tests are real. |
| `.xping.yml` sample and both schemas are drafts | The formats are not specified yet. |

Open items: the Marketplace publisher ID (`integrations/azure-devops/vss-extension.json`), GitHub private vulnerability reporting must be enabled for `SECURITY.md` to work. The ADO `index.js` placeholder runs under Node and exits 69.

### Phase 3: migrate (branch `migrate/sdk-dotnet`, stacked on `chore/scaffold`)

Done:
- **Extraction** as in §2 (both passes), 131 commits merged with `--allow-unrelated-histories`. Commit maps (original → pass 1 → pass 2) are in `docs/specs/provenance/`. Imported messages carry `Migrated-From: xping-dev/sdk-dotnet` and link PRs as `xping-dev/sdk-dotnet#N`. Commits before 2026-01-02 come along where the kept paths existed in the old web-testing SDK.
- **Dashboard copies:** four commits, each naming `xping-dev/dashboard@f250a6e` and the files. Headers say so too.
- **Cuts** (§3): no `Xping.Sdk.*` reference, no Cloud configuration, auth, uploader, `ApiOptions`, `Mode`/`ProjectId`/`IsConnected`, Polly, `PublicOnlyConnect` or GitHub App auth. `NoCloudDependencyTests` enforces it on the package graph, the sources and the shipped assemblies. It was checked by planting a Cloud URL, which made it fail.
- **Session format:** `schemaVersion` + `producer` in model and schema. `SessionDocument` reads every version: v0 goes through `LegacySessionImporter`, newer versions are refused. Covered with the embedded early-SDK file and an rc.8-style file.
- **Headers:** every `.cs` file has the SPDX MIT header.
- **Result:** build has 0 warnings (Debug and Release), 2117 tests pass, the format check is clean.

| Deviation | Why |
|---|---|
| `Xping.Session` is **netstandard2.0 only** (not + net10.0) | The migrated code is netstandard2.0 code; a net10.0 build demands APIs (span overloads, `ThrowIfNull`, `HashData`) netstandard2.0 lacks. The CLI consumes netstandard2.0 fine. |
| Environment and PR detection stay in `Providers.Abstractions`, not split per provider | `EnvironmentDetector` calls the concrete GitHub/GitLab/ADO detectors directly. Splitting needs the provider adapter design (TODO(spec)), not a file move. |
| The comment clients use `IHttpClientFactory`; nothing registers the named clients yet | Registration belongs to the PrComment reporter (TODO(spec)). |
| `Cli/Configuration` deleted rather than migrated | It held only `CloudUrl`, `ApiKey` and `ProjectId`. `.xping.yml` is a Core TODO(spec). |
| `TestNames` (from `Contract/SubjectNames.cs`) and `Redaction` (from `Auth/Redaction.cs`) are **copied**, not moved with history | Their folders were purged from history in pass 2; one file each was still needed. Their headers name the source. |
| Tests dropped with the code they tested | The report renderers, envelope and goldens; the `report`/`where`/`clear` commands and CTA throttle; SDK orchestrator, DI and attribute tests; `XpingModeValidation`; GitHub App credential tests; the live ADO content-limit probe. The triage provider tests that asserted through the envelope JSON (headlines, byte-identical output, sibling ordering) are removed too. Determinism of the triage engine has no test now: re-add one when XP005 defines its output. |
| `--version` prints clean SemVer (`2.0.0-alpha.1`), not `+sha` | Kept from the old CLI. |
| New exit code **130 Cancelled** | Kept from the old CLI (it was an auth-only code there). |
| Test packages: Moq, NSubstitute, WireMock.Net, AwesomeAssertions, TimeProvider.Testing | Used by the migrated tests. AwesomeAssertions (Apache-2.0) replaces FluentAssertions, whose v8+ is commercial. |
| `xUnit1051` and `CA1859` suppressed in tests | The migrated tests predate xUnit v3's cancellation token and call internals through interfaces on purpose. |
| `CA1812` off for `src/Xping.Core/Triage/**` | Nothing composes the triage engine until XP005. TODO(spec) in `.editorconfig`. |
| `LocalStorePerformanceTests` (`Category=Performance`) do not run on the Windows CI job | Their wall-clock budgets were calibrated on ubuntu (sdk-dotnet's CI was ubuntu-only); a read took 5129 ms against 5000 ms on windows-latest. The budgets are unchanged and enforced on ubuntu. |
| Calibration (Monte Carlo) tests run in every CI build | The whole Core suite takes ~20 s; sdk-dotnet ran them nightly only. |

Follow-ups:
- Many migrated comments still explain choices in terms of Xping Cloud. They are harmless, but should be reworded as the code is touched.
- `ReportGlyphs` / `TerminalText` are kept for the future text reporter and are unused today.

### Phase 4: CI, release, supply chain (branch `ci/release`)

- `ci.yml`: build, test and format check on PRs and `main`, ubuntu + windows. It is also callable (`workflow_call`), so the release runs the same checks first.
- `release.yml` on tag `v*`:
  - single-file binaries for linux-x64, linux-arm64, win-x64 (cross-published on ubuntu) and osx-arm64 (on macOS, so the SDK signs the apphost);
  - NuGet packages `Xping.Cli` (dotnet tool) and `Xping.Session`;
  - OCI image `ghcr.io/xping-dev/xping` for amd64 + arm64, built from the same binaries;
  - CycloneDX SBOMs for the CLI, the library (CycloneDX .NET tool, pinned in `dotnet-tools.json`) and the image (syft);
  - cosign keyless signatures (`*.sigstore.json`) for every file and for the image, with the image SBOM as a signed attestation;
  - SLSA build provenance through `actions/attest-build-provenance` for every file (via `SHA256SUMS`) and for the image;
  - a GitHub Release, NuGet trusted publishing (OIDC, no API key secret), and a push to ghcr.io with `GITHUB_TOKEN`.
- Least privilege: the workflow starts with `permissions: {}` and each job asks only for what it uses. Actions are pinned by commit SHA. Checkouts don't persist credentials. Dependabot keeps actions, NuGet, the base image digest and the SDK current. `actionlint` and `zizmor --persona=auditor` are clean, apart from one stylistic hint, the new self-repository `uses:` syntax, which was not adopted.
- Dry runs: `workflow_dispatch` (publish off by default) and pull requests that touch the release machinery build everything and upload it as workflow artifacts. Nothing is signed, attested or published in a dry run. Publishing from `workflow_dispatch` is refused unless it runs on the tag `v<version>`, and every run refuses a version that differs from `Directory.Build.props`.
- `docs/reference/verifying-releases.md` documents `gh attestation verify` and `cosign verify(-blob)` for users.

| Deviation | Why |
|---|---|
| Signing and attestation are not exercised by dry runs | Both write to public logs (Rekor, the attestation store) and would leave entries for builds that were never released. The first tag exercises them. |
| Image base: `mcr.microsoft.com/dotnet/runtime-deps:10.0` (Ubuntu 24.04) + `git` | `xping check` runs `git diff`. The chiseled images have no package manager and no git. 376 MB uncompressed; a slimmer base can come later. |
| The container builds arm64 under QEMU | Only `apt-get install git` runs emulated. The binary itself is built natively in the binaries job. |

One-time setup before the first tag `v2.0.0-alpha.1`:
1. **nuget.org trusted publishing:** add a policy for owner `Xping`, repository `xping-dev/xping`, workflow `release.yml`, environment `release`. Set the repository variable `NUGET_USER` to the nuget.org profile name that owns the packages.
2. **Old CLI versions:** mark `Xping.Cli` 1.0.0-rc.1…rc.8 as deprecated, with 2.0.0-alpha.1 as the alternative (Q4). Text: `docs/specs/followups/` (Phase 5).
3. **ghcr.io:** after the first push, make the `xping` package public and link it to the repository.
4. **Repository settings:** enable private vulnerability reporting (SECURITY.md). Optionally add required reviewers to the `release` environment.
5. **nuget.org:** request the `Xping.*` ID prefix reservation (§5).

### Phase 5: docs and cross-repo follow-ups (branch `docs/followups`, prepared, not executed)

Drafts under `docs/specs/followups/` (index: `followups/README.md`):

| Draft | State |
|---|---|
| `docs-repo/`: skeleton for public `xping-dev/docs` | docfx 2.81.0 site with sections (getting started, CI setup, rules, reference, SDK), `scripts/import-reference.sh <tag>`, which imports `docs/reference/*.md` and `schema/*.json` from this repo at the tag in `xping-version.txt`, and a Pages workflow with pinned actions. Built locally against `main`: 0 warnings, 0 errors. |
| `sdk-dotnet-readme-note.md` | README banner pointing to this repo, plus what to delete in sdk-dotnet afterwards. Wording about Xping Cloud itself is a TODO(decision), prespec §11.12. |
| `nuget-deprecation.md` | Deprecation of `Xping.Cli` 1.0.0-rc.1…rc.8: reason Legacy, alternative 2.0.0-alpha.1, a 495-character message. Not unlisted (Q4). |
| `sdk-on-xping-session.md` | The steps: a public writer API in `Xping.Session` with API-compat checks, then sdk-dotnet replaces its model/store copies with the package. Risks included. |
| `action/`: skeleton for `xping-dev/action` | Composite `action.yml` at the root, `command: check \| record \| compact`, exact `version`. It downloads the release binary for the runner, checks it against `SHA256SUMS`, runs `gh attestation verify --signer-workflow` and then the CLI. Inputs reach the scripts only through env. A smoke workflow covers 4 OSes. Scripts are shellcheck-clean, the workflow actionlint-clean. |
| `gitlab-component/`: skeleton for a gitlab.com Catalog project | Three components (`check`, `record`, `compact`; GitLab rules cannot branch on an input), each running the pinned image. Self-test and Catalog release in `.gitlab-ci.yml`. YAML parses. Not linted with GitLab's CI lint (needs a gitlab.com project). |

| Deviation | Why |
|---|---|
| The docs site moves to `xping-dev/docs`, not into this repo's `docs/` | Q6. This repo keeps specs and the reference pages generated next to the code. |
| The SDK docs are not copied yet | `feat/docs-overhaul` (15 commits) is unmerged in sdk-dotnet. Merge it there first, then move the pages (§8, step 2). |
| GitLab ships three components, not one with `command` | GitLab `rules:` cannot test an input value, and each command needs different rules. |
| The action verifies provenance with `gh attestation verify`, not cosign | `gh` is preinstalled on GitHub runners and verifies the same Sigstore-backed attestation, with no extra download. cosign bundles stay available for users (`docs/reference/verifying-releases.md`). |

## 8. docs.xping.io switch steps

Today: `docs.xping.io` is a DNS CNAME to `xping-dev.github.io`. GitHub Pages of **xping-dev/sdk-dotnet** claims the custom domain and is deployed by the `docs` job in sdk-dotnet's `release.yml` (docfx). GitHub routes a custom domain to the one repository in the organisation that claims it, so the move changes **repository settings only; DNS stays as it is**.

1. **Verify the domain** for the organisation (Org settings → Pages → Verified domains → `xping.io`), if it isn't verified yet. This prevents a takeover during the gap in step 5.
2. **Prepare the content in sdk-dotnet first.** Merge `feat/docs-overhaul`. Then list the pages to move. Sections on `main` today:
   - getting-started (5), guides (13), configuration (2), troubleshooting (2), performance (2), cli (1), known-limitations, versioning;
   - `internals/implementation-specs` (old CLI specs: do not move).
3. **Create `xping-dev/docs`** (public, MIT) from `docs/specs/followups/docs-repo/`:
   - move the SDK pages to `sdk/`, in one commit naming `xping-dev/sdk-dotnet@<sha> docs/`;
   - drop the Cloud, old `report` CLI and `internals/` pages.
4. **Keep old URLs working.** Code and NuGet readmes of released SDK packages link these pages; each gets a redirect page (docfx `redirect_url` metadata) at its old path, pointing at the new one:
   - `/index.html`, `/getting-started/quickstart-{nunit,xunit,mstest}.html`, `/getting-started/local-first.html`;
   - `/configuration/configuration-reference.html`, `/configuration/local-store.html`;
   - `/troubleshooting/known-limitations.html`;
   - `/cli/command-reference.html`, `/cli/reading-the-report.html`: these two point at the 2.x CLI overview, noting that the 1.0.0-rc `report` command is gone.
5. **Switch**, in this order and in one sitting (the gap is minutes):
   1. In xping-dev/docs: Settings → Pages → Source: GitHub Actions. Run the `Docs` workflow once; the site is at `https://xping-dev.github.io/docs/`. Check it.
   2. In xping-dev/sdk-dotnet: Settings → Pages → Custom domain → remove `docs.xping.io`.
   3. In xping-dev/docs: Settings → Pages → Custom domain → `docs.xping.io` → Save. Wait for the DNS check and the certificate, then tick *Enforce HTTPS*.
6. **Clean up sdk-dotnet:** remove the `docs` job from `release.yml` and the `docs/` site sources, keeping a README link to docs.xping.io (see `followups/sdk-dotnet-readme-note.md`). Unpublish its Pages site.
7. **Check:**
   - `curl -sI https://docs.xping.io/` returns 200 from GitHub with a valid certificate;
   - each redirect path in step 4 lands on its new page;
   - search works.
8. **Rollback:** remove the custom domain from xping-dev/docs and re-add it to xping-dev/sdk-dotnet, whose Pages deployment is untouched until step 6.
