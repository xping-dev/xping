# xping CI/CD components

> Skeleton prepared in xping-dev/xping (`docs/specs/followups/gitlab-component/`). It has to live in a **gitlab.com** project (proposed `gitlab.com/xping-dev/xping-ci`), marked as a CI/CD Catalog resource. GitLab only lists Catalog components hosted on GitLab.

Runs [xping](https://github.com/xping-dev/xping) in GitLab CI. Each component runs the pinned `ghcr.io/xping-dev/xping` image. Behaviour lives in the CLI, never here.

| Component | Runs on | Purpose |
|---|---|---|
| `check` | merge request pipelines | find weakened tests in the MR |
| `record` | the default branch | append the run to history |
| `compact` | scheduled pipelines on the default branch | roll up and prune history |

```yaml
include:
  - component: gitlab.com/xping-dev/xping-ci/check@2.0.0-alpha.1
    inputs:
      version: 2.0.0-alpha.1
      results: 'TestResults/*.trx'
  - component: gitlab.com/xping-dev/xping-ci/record@2.0.0-alpha.1
    inputs:
      version: 2.0.0-alpha.1
```

## Tokens

Store a project access token in a masked, protected CI/CD variable `XPING_GITLAB_TOKEN` (or name another variable with `token_variable`).
- `check` needs scope `api`: `CI_JOB_TOKEN` cannot post merge request notes.
- `record` and `compact` need `write_repository`, to push the history branch.

Some GitLab versions let the job token push (*Settings → CI/CD → Job token permissions*). Check your instance (prespec §5).

Exclude the history branch from protected-branch rules and from pipeline triggers.

## Releasing the components

Tag the project with the same SemVer as the xping release (no `v` prefix, as the Catalog expects). `.gitlab-ci.yml` creates the release that publishes it to the Catalog.

TODO(spec):
- **Image signature:** verify it with cosign before running. GitLab pulls the job image before any script runs, so this needs a separate verify job or a digest pin.
- **Fork merge requests:** decide how they are handled.
