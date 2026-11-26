# xping-dev/docs (skeleton)

> Draft for a new public repository `xping-dev/docs`, prepared in xping-dev/xping
> (`docs/specs/followups/docs-repo/`). Copy this folder to the root of the new repository.

Source of https://docs.xping.io: product documentation for the Xping CLI, its CI integrations
(GitHub Action, GitLab component, Azure DevOps task) and the Xping SDK.

## Build

```sh
dotnet tool restore
./scripts/import-reference.sh "$(cat xping-version.txt)"   # reference pages + schemas from xping-dev/xping
dotnet docfx docfx.json --serve                # http://localhost:8080
```

## What lives where

| Content | Owner | How it gets here |
|---|---|---|
| Guides, CI setup, concepts | this repo | written here |
| Exit codes, release verification, command reference, rule pages, `.xping.yml` reference | `xping-dev/xping` (`docs/reference/`) | `scripts/import-reference.sh <tag>`, never edited here |
| `session.schema.json`, `history.schema.json` | `xping-dev/xping` (`schema/`) | same script, published under `/schema/` |
| SDK guides | moved from `xping-dev/sdk-dotnet` (`docs/`) | one-time move, see the migration plan in xping-dev/xping |

Pages generated from code are imported at the release tag in `xping-version.txt`, so the site always describes a released version. Bump the file to publish docs for a new release.
