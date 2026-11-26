# Xping Redesign — Guide & Pre-Spec

> Status: pre-spec / direction document · Nov 2026
> Purpose: capture the decisions, architecture, and open questions for the Xping redesign so it can be turned into component specs.

---

## 1. Why redesign

### Feedback so far
- "We don't need another flaky detector." Flaky detection is a commodity — CI platforms, Datadog, Trunk, BuildPulse, Mergify and cheap/free tools already cover it.
- "AI agents handle failing tests well enough." Confirmed by our own pilot: when the cause is visible in the code, agents fix it without history.
- "Compliance makes a 3rd-party tool hard" + "our in-house solution is good enough." The objection is about **where data goes**, not features.
- Stryker showed a weak test that Xping scored high → we cannot back the claim "Xping tells you whether a green build can be trusted."
- Solo founder, no reach outside network, selling invite-only cloud SaaS to companies with compliance = hardest possible go-to-market.

### The real signal
Our pilot's slow-CI-runner scenario: **every agent without history loosened the test (250 ms → 2 s); every agent with Xping history changed nothing.** That is test weakening / reward hacking — a growing, non-commoditized problem that internal teams have little incentive to build tooling for (it means policing their own agents). Research supports it (ImpossibleBench, EvilGenie, SpecBench — see §12).

### New positioning (one line)
**Xping verifies that the safety net wasn't weakened.** It does not judge whether code is correct; it detects when tests were weakened and brings evidence (execution history) that reviewers and agents otherwise lack.

---

## 2. Core requirements

| # | Requirement |
|---|---|
| R1 | Open source (MIT core, consistent with existing Xping repos). |
| R2 | **No data leaves** the customer's environment. No Cloud/platform dependency. |
| R3 | Runs entirely in the customer's CI: **GitHub, GitLab, Azure DevOps**. |
| R4 | **No SDK required.** Inputs: JUnit XML (universal), TRX; Xping SDK session as optional richer input. |
| R5 | **Deterministic by default.** No LLM calls. Any AI feature is opt-in and uses the customer's own model endpoint. |
| R6 | **Approvable by security teams:** signed releases (Sigstore/cosign), SBOM (CycloneDX), SLSA provenance, pinned versions, no telemetry, least-privilege tokens. |
| R7 | **Useful without history, better with it.** Value on the first PR; history adds evidence-backed verdicts. |
| R8 | History stored in the customer's own repo (`xping-history`), written only from the default branch. |
| R9 | CLI output usable by coding agents (`--format json`, skills). |
| R10 | Check runtime budget: **< ~30 s** on a typical PR. |

---

## 3. Product: what Xping detects

### Rules (initial set)
| ID | Rule | Needs history? |
|---|---|---|
| XP001 | assertion-removed / weakened (count drop, broader matcher) | No (history improves) |
| XP002 | timeout-loosened | No for detection; **yes for verdict** ("p99 236 ms over 412 runs → slow runner, re-run") |
| XP003 | test-skipped / deleted | No; history verifies "flaky" claims ("0 failures in 380 runs — not flaky") |
| XP004 | sut-mocked (class under test replaced by mock) | No |
| XP005 | failure triage: flaky vs real regression (replaces planned `xping triage`) | Yes (or same-PR reruns) |
| XP0xx | fingerprint-duplicated / fingerprint-changed / test-missing-fingerprint | No |
| opt. | mutation-score drop on changed tests (Stryker `--since`) | No |

Rules are small, independent classes with stable IDs → auditable, individually suppressible, reusable in batch mode for the public-PR dataset study.

### Value layers
| Layer | What works | Role |
|---|---|---|
| Diff only (first PR) | XP001, XP003, XP004, XP002 detection | Gets Xping installed |
| + same-PR reruns | Flaky vs real for failing tests, no history needed | Week-one value |
| + backfill | History exists on day one | Removes cold start |
| + history | XP002 verdict, XP003 verification, XP005, fewer false positives | **The moat** — why teams keep it |

Honest note: diff-only Xping ≈ AGENTS.md + an LLM code review. **Verification + history is the product.**

---

## 4. Worked examples (customer problems)

Customer: .NET team of 12 on GitHub, ~30% of PRs opened by coding agents.

**Problem 1 — "The agent loosened the test to get green."**
Slow runner → `OrderService_ProcessesBatch_Under250ms` fails → agent raises timeout to 2 s → green.
Xping: `XP002` — timeout raised 8×; history p99 236 ms over 412 runs; runner 1.7× slower than baseline → re-run, don't change the test.

**Problem 2 — "The refactor PR that quietly hollowed out the tests."**
40-file agent refactor; 3 assertions removed, a test skipped "as flaky", `FeeCalculator` mocked in its own test. All green.
Xping: BLOCKED — `XP001`, `XP004`; `XP003` notes 0 failures in 380 runs (not flaky by evidence); optional mutation score 78% → 31%.

**Problem 3 — "Is this failure flaky, or did I break it?"**
Xping: `XP005` — one test fails 6.1% on main and is unrelated to the diff (re-run); another has 0 failures in 377 runs and covers a changed file (investigate). Same verdict available locally to the agent via `xping check --local --format json`.

### Who gets what
| Who | Problem | Xping gives |
|---|---|---|
| Engineering lead | Can't review every agent PR; green CI no longer means much | Every PR checked for test weakening; blocking policy; audit trail (paid) |
| Developer | Mine or flaky? Is the agent's test change legit? | Evidence-backed verdict in the PR |
| Coding agent | Evidence isn't in the repo → guesses and loosens tests | History via skill / JSON |
| Security / compliance | Can't approve a new vendor | Auditable OSS, runs in own CI, data in own repo |

---

## 5. Architecture

```
╔═══════════════ DEVELOPER / CODING-AGENT MACHINE (optional) ═══════════════╗
║  dotnet test ──► [Xping SDK, optional] ──► local session store (.xping/)   ║
║  agent skill ──► xping check --local --format json                         ║
║  Never leaves the machine.                                                 ║
╚════════════════════════════════════════════════════════════════════════════╝

╔══════════ PR / MR PIPELINE (GitHub Actions | GitLab CI | Azure Pipelines) ═╗
║ 1. checkout (merge-base depth) + fetch xping-history --depth=1  (READ ONLY)║
║ 2. run tests ──► JUnit | TRX | .xping/session                              ║
║ 3. xping check                                                             ║
║   Provider (env) ─► PR id, base SHA, token, runner info                    ║
║   CODE:    git diff ─► Analyzers (Roslyn C# | tree-sitter) ─► static facts ║
║   RESULTS: Importers (JUnit/TRX/SDK) ─► SESSION MODEL; [Stryker JSON]      ║
║   HISTORY: HistoryStore ─► per-test baselines                              ║
║            ▼                                                               ║
║   RULES ENGINE (XP001..) ─► Findings ─► Policy (.xping.yml warn|block)     ║
║            ▼                                                               ║
║   Reporters: PR/MR comment | job summary | JSON | SARIF | exit code        ║
╚════════════════════════════════════════════════════════════════════════════╝

╔═════════ DEFAULT-BRANCH PIPELINE (after merge) + SCHEDULED JOB ═══════════╗
║ run tests ─► xping record ─► session model ─► append immutable run file    ║
║            ─► push xping-history (pull --rebase + retry on reject)         ║
║ scheduled: xping compact ─► stats/ rollup, prune raw > N days              ║
╚════════════════════════════════════════════════════════════════════════════╝

xping-history (orphan branch or custom ref, in customer repo)
├── meta.json                              schema version, retention
├── runs/YYYY/MM/<sha>-<runid>.json.gz     one immutable file per run
└── stats/tests.json.gz                    compacted baselines (what PRs read)
```

### CLI module layout
```
xping (CLI)
├── Core        rules engine, findings model, policy, config (.xping.yml)
├── Analyzers   CSharp (Roslyn) | TreeSitter (per-language queries)
├── Evidence    TestResults (JUnit/TRX/Session) | Mutation (Stryker) | History
├── Stores      GitBranch | CiArtifact | LocalFile
├── Providers   GitHub | GitLab | AzureDevOps (detected from env vars)
└── Reporters   PrComment | JobSummary | Json | Sarif | ExitCode
```

### Commands
| Command | Purpose |
|---|---|
| `xping init` | Add CI steps, `.xping.yml`, AGENTS.md rules, fingerprint attribute; run `fingerprint add` |
| `xping check` | PR/MR analysis (CI) or `--local` (dev/agent machine) |
| `xping record` | Append default-branch run to history |
| `xping compact` | Roll up raw runs into `stats/`, prune |
| `xping backfill` | Import past test results from the customer's CI API |
| `xping fingerprint add` | Codemod: add fingerprints to tests without one |
| `xping skills install` | Install agent skills |

### Key design rules
- **PRs read history; only the default branch writes it.** Fork PRs have no push rights; unmerged data pollutes baselines.
- **One immutable file per run** → concurrent writers never conflict; rebase + retry always succeeds.
- PRs fetch `--depth=1`; `stats/` carries the aggregated history.
- **The session schema is the canonical data model.** JUnit/TRX importers convert into it; history stores it; rules read it. Version it and define stable test identity before the first file is written anywhere.
- **History format is a public, versioned contract** — teams will query it; that's part of the compliance story.
- `xping record` captures environment from CI env vars (runner type, OS, CPU count, runner name) — reports don't contain it, and runner baselines are needed for the slow-runner verdict.
- Baselines are kept **per environment** (OS/matrix), never mixed.

### Push permissions per provider
- **GitHub:** `contents: write` on the default-branch job only.
- **Azure DevOps:** `System.AccessToken` + Contribute permission for the Build Service on the history ref.
- **GitLab:** project access token or deploy key (newer versions may allow job-token push via a setting — verify per customer version).
- Exclude `xping-history` from branch protection rules and from CI triggers.

---

## 6. Inputs: SDK, JUnit, TRX, session

| Input | Gives | Missing |
|---|---|---|
| JUnit XML | Universal, zero setup | Retries (inconsistent), stable IDs for parameterized tests, assertion data, local runs |
| TRX | Better .NET detail (outcomes, durations, IDs, separate FQN) | Assertion data, local runs |
| Xping SDK session | All above + retries/attempts, environment, assertion counts, local runs | Needs a NuGet package |

- No SDK: `xping check` with JUnit/TRX + diff = the core product.
- With SDK: richer evidence and `--local` mode (the "agent loosened it locally while CI stayed green" differentiator). Local data never leaves the machine.
- The old plan of uploading the session store as a CI artifact for `xping triage` is superseded by the history branch + XP005.

---

## 7. Test identity: `[TestFingerprint("...")]`

### Reading it
- Read **from source with Roslyn syntax trees** — no compilation, milliseconds per file, works identically in CI and locally.
- Join source ↔ results on the fully qualified name (namespace.class.method); history is keyed by **fingerprint**.
- Fallback read path: compiled test DLL via `System.Reflection.Metadata` (parses metadata without loading; build output already exists in CI and locally).
- Match the attribute **by name, not type** → no package reference required. `xping init` can generate a 3-line attribute class, or ship a source-only package. Existing SDK attribute keeps working.

### Edge cases
| Case | Problem | Handling |
|---|---|---|
| `DisplayName` / `TestName` | Report shows display name | Use TRX FQN, or assembly-metadata fallback |
| Inherited test methods | One fingerprint, many concrete classes | Key = fingerprint + concrete class |
| Parameterized tests | One fingerprint, many cases | Key = fingerprint + case ID (from args) |
| Partial / nested classes | Name building is fiddly | Handle in the name builder, cover with tests |
| Tests without fingerprint | No stable ID | Fall back to FQN; warn |

### Process
- Fingerprints are **created deterministically** by `xping fingerprint add` (init, pre-commit hook, or CI suggestion) — not by agents.
- AGENTS.md only says: never change or copy a fingerprint.
- Xping enforces it: duplicate fingerprint (needs a repo-wide registry), fingerprint changed while body mostly unchanged, missing fingerprint.
- **Bonus:** same fingerprint under a new name/file in the diff = rename/move with no heuristics → XP001 compares old vs new version of the same test → fewer false positives.
- Other languages via tree-sitter: Java annotations, Python decorators, comment marker (`// xping:fp …`) for JS/TS.

---

## 8. AGENTS.md: complement, not competitor

- Prompt rules work well on simple tasks (ImpossibleBench: GPT-5 cheating 92% → 1% on LiveCodeBench with an appropriate prompt) but agents still cheat frequently on realistic repo tasks (Impossible-SWE-bench) despite careful prompting.
- Gaps: no notion of when a test change is legitimate; missing facts (the slow runner is only in history); not enforced; covers only agents that read it; reviewer can't verify it was followed.
- **Telling the agent ≠ checking the result.** Xping ships both:
  - `xping init` writes AGENTS.md rules (e.g. "don't change assertions or timeouts to make tests pass; if a test seems wrong, run `xping check --local` and report").
  - The skill gives agents the evidence; the CI check verifies.
- **Experiment:** extend the pilot to 4 conditions on real repos — no guidance / AGENTS.md only / Xping only / AGENTS.md + Xping. Either a cheap negative result or the strongest blog post and investor slide.

---

## 9. Technology stack

| Concern | Choice | Notes |
|---|---|---|
| Core | .NET 10, C# | Founder strength; reuse SDK/CLI code |
| Packaging | Self-contained single-file binary (linux-x64/arm64, win-x64, osx-arm64) + OCI image on ghcr.io | **Not Native AOT** — Roslyn relies on reflection; ~60–80 MB, <1 s start is fine in CI |
| C# analysis | Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Semantic understanding where needed |
| Other languages | tree-sitter (evaluate `TreeSitter.DotNet`) | Per-language rule queries |
| Diff | `git` CLI (`git diff --merge-base`) | Shallow-clone friendly, no native libs |
| Test results | JUnit, TRX, Xping session | |
| Mutation | Stryker.NET `--since`, read JSON report | Optional; changed tests only |
| History | GitBranch (default durable) / CiArtifact / LocalFile | SQLite or JSONL + compaction — choose per store |
| Output | PR comment + job summary (primary), JSON, SARIF (secondary), exit code | SARIF display is gated: GitHub Code Security for private repos, GitLab Ultimate |
| PR comments | Reuse multi-provider code from Xping.Analytics | GitHub REST, GitLab Notes, ADO PR Threads |
| Distribution | GitHub Action (composite, pinned by digest), GitLab CI/CD Catalog component, ADO Marketplace task + YAML template, `dotnet tool` | One binary, thin adapters, no logic in wrappers |
| Agents | `--format json` + `xping-dev/skills`; MCP later if at all | |
| Supply chain | cosign, CycloneDX SBOM, SLSA provenance | Answers compliance |
| License | MIT | .NET ecosystem norm (runtime, Roslyn), consistent with existing repos; protect the "Xping" name via trademark registration + policy. Switch to Apache 2.0 only if an enterprise adopter requires the patent grant |

Why not Go/Rust: they're the norm for OSS CI tools and Rust has the best tree-sitter story, but switching costs months and loses Roslyn (the C# moat). Users run a binary/container — implementation language doesn't affect adoption. Revisit only if early adopters are mostly non-.NET.

---

## 10. Monetization: open core

**Do not** close the CLI or make it paid "after version X":
- Released MIT versions stay forkable forever.
- License-change backlash is well known (Terraform → OpenTofu, Redis → Valkey); a vague "free until X" deters careful adopters.
- It charges for the adoption engine. And with zero users there is no revenue to protect.

### The line is the organization
| Free OSS (MIT) | Paid |
|---|---|
| `check`, `record`, `compact`, `backfill` | Org-wide policy across repos |
| Core rules XP001–XP005 | Self-hosted dashboard aggregating `xping-history` across repos |
| JUnit/TRX/SDK importers | Audit & compliance reports |
| GitHub/GitLab/ADO adapters | Advanced rule packs (mutation-backed scoring, deep semantic C#, more languages) |
| History branch, agent skills | Enterprise packaging: LTS, air-gapped images, SLA, support |
| | Setup/audit as a paid service |

- Paid features unlocked by a **signed license file validated offline** — no phone-home, keeps R2.
- Now: keep MIT for the core; register the "Xping" trademark and publish a short trademark policy (MIT says nothing about names); add a CLA or clear contribution policy before outside contributions (MIT has no contribution clause); keep paid code in a separate private repo from day one.
- Middle option for paid source-available parts: Functional Source License, MIT-future variant (FSL-1.1-MIT, converts to MIT after 2 years) — not OSI open source, so never for the core.
- Build no paid features until ~10 teams run the free CLI.

---

## 11. Risks and things not to miss

### Product
1. **False positives kill CI checks.** Default `warn`, opt-in `block`; inline suppressions with reason (`// xping-ignore XP001: requirement changed, see #123`); **baseline mode** (report only new findings); measure precision on the public-PR dataset before launch.
2. **Legitimate test changes.** Moves/renames/splits/merges must not read as "assertions removed" (fingerprints + Roslyn move detection). Treat "test and production changed together" differently from "only the test changed".
3. **PR authorship.** Detect agent PRs (Co-authored-by trailers, bot accounts, branch prefixes, commit metadata); allow different policy for agent vs human PRs.
4. **Test identity across renames** — solved by fingerprints (§7).

### Technical
5. **Roslyn performance — biggest technical risk.** Full MSBuild workspace load can take minutes. Options: syntax-level analysis + heuristics; semantic model only for changed files using reference assemblies from existing build output; reading the binlog. Prototype early.
6. **Fork PRs & security.** Comments need write tokens; fork PRs get read-only. Avoid `pull_request_target` ("pwn requests"). Safe design: check runs read-only and uploads findings as an artifact; a trusted job (`workflow_run` on GitHub) posts the comment. Design per provider.
7. **History branch side effects:** every `git clone` downloads it → consider a custom ref (`refs/xping/history`, not fetched by default; verify all three providers accept it); pushes can trigger CI (exclude); anyone with push can poison baselines → restrict writes to the CI identity, treat history as evidence not ground truth.
8. **Monorepos, sharding, matrix builds:** merge results from parallel jobs before checking; baselines per environment.
9. **History requirements:** backfill (ADO Test Results API, GitLab JUnit reports per pipeline, GitHub artifacts ~90-day default retention); confidence on every history verdict ("12 runs: low confidence" vs "412 runs"), fall back to diff-only wording below threshold; default-branch only.

### Business
10. **Measuring success without telemetry:** GitHub "Used by", Marketplace installs, image/`dotnet tool` downloads, opt-in anonymous findings counts, feedback link in every PR comment. Also investor evidence.
11. **Competitive scan first** (half a day): test-smell tools (e.g. tsDetect), Stryker dashboard/diff mode, whether CodeRabbit / Copilot code review already flag test weakening.
12. **Wind down the old product:** decide on app.xping.io, the $99 pilot on the pricing page, the GitHub App, Azure costs, xping.io messaging. Freeze Cloud rather than maintain two products.
13. **Public demo repo** reproducing the three scenarios — docs, test fixture, sales material (investor call), and the AGENTS.md vs Xping experiment setup.

---

## 12. Plan and order of work

1. **Update the survey before 1 Dec:** add incident questions ("Have you caught an agent loosening, skipping, or deleting a test to get green? How did you catch it?").
2. **Competitive scan.**
3. **Session schema v1** + stable test identity (fingerprint design).
4. **Diff-rule engine in batch mode** (Roslyn prototype incl. performance spike).
5. **Public-PR dataset study:** run the engine over thousands of public agent-authored PRs → publish "X% of agent PRs that touch tests weaken them". Gives precision data, content, leads, investor evidence.
6. **4-condition agent experiment** (no guidance / AGENTS.md / Xping / both) on real repos.
7. Only then: CI adapters (GitHub → ADO → GitLab or by demand), `xping-history`, `record`/`compact`/`backfill`, reporters, supply-chain hardening, `xping init`.
8. Paid layer only after ~10 active teams.

### Kill criteria (by end of Feb 2027)
- A measurable rate of test weakening in public agent PRs, **and**
- 5+ teams running the check in CI for free, **and**
- at least 2 say they'd be upset if it went away.

If not met: the problem isn't there — move on without spending another year.

---

## 13. References
- ImpossibleBench (Zhong, Raghunathan, Carlini) — https://arxiv.org/abs/2510.20270
- EvilGenie: A Reward Hacking Benchmark — https://arxiv.org/abs/2511.21654
- SpecBench: Measuring Reward Hacking in Long-Horizon Coding Agents — https://arxiv.org/abs/2605.21384
- Harness: 2026 flaky-test tooling landscape — https://www.harness.io/blog/flaky-test-tooling-landscape-2026
- Datadog Bits AI + Test Optimization — https://www.datadoghq.com/blog/bits-ai-test-optimization/
- boldstart: competing against internally developed tools — https://boldstart.vc/devtoolkit/
- GitLab SARIF reports (Ultimate) — https://docs.gitlab.com/user/application_security/detect/sarif/
- TreeSitter.DotNet — https://www.nuget.org/packages/TreeSitter.DotNet/
