# xping-dev/action

> Skeleton prepared in xping-dev/xping (`docs/specs/followups/action/`). `action.yml` sits at the repository root.

Runs [xping](https://github.com/xping-dev/xping) in GitHub Actions. The action is a thin wrapper:
1. It downloads the exact xping release you pin.
2. It checks the download against `SHA256SUMS` and verifies its build provenance (`gh attestation verify`).
3. It runs the command.

The CLI does all the work. Pin this action by commit SHA.

## Pull requests: `check`

```yaml
on: pull_request
permissions:
  contents: read
  pull-requests: write   # the PR comment
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@<sha>
        with:
          fetch-depth: 0   # TODO(spec): merge-base depth instead of full history
      - run: dotnet test --logger trx
      - uses: xping-dev/action@<sha> # v2.0.0-alpha.1
        with:
          version: 2.0.0-alpha.1
          results: '**/TestResults/*.trx'
```

Fork PRs get a read-only token, so `check` cannot comment on them. TODO(spec): a `workflow_run` job that posts the comment from a trusted context (never `pull_request_target`).

## Default branch: `record`

```yaml
on:
  push:
    branches: [main]
permissions:
  contents: write   # push to the history branch; this job only
jobs:
  record:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@<sha>
      - run: dotnet test --logger trx
      - uses: xping-dev/action@<sha> # v2.0.0-alpha.1
        with:
          command: record
          version: 2.0.0-alpha.1
          results: '**/TestResults/*.trx'
```

Exclude the history branch (`xping-history`) from branch protection and from CI triggers.

## Inputs

| Input | Default | |
|---|---|---|
| `command` | `check` | `check`, `record` or `compact` |
| `version` | required | exact xping version |
| `results` | | result files or globs, one per line |
| `args` | | extra CLI arguments |
| `token` | `github.token` | GitHub API token |

The smoke test in `.github/workflows/test.yml` needs the first xping release (`v2.0.0-alpha.1`) to exist.

Output `exit-code`: see [exit codes](https://github.com/xping-dev/xping/blob/main/docs/reference/exit-codes.md).
