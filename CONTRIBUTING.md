# Contributing

Thanks for your interest. Xping is in early development, and the architecture is still moving.

## Before you start

- Open an issue before a large change, so we can agree on the approach first.
- Contributions are accepted under the MIT license. TODO: a contributor agreement (CLA or DCO) is decided before outside contributions are merged.

## Build and test

```sh
dotnet build                          # warnings are errors
dotnet test                           # all test projects (Microsoft.Testing.Platform)
dotnet format --verify-no-changes     # style check, also run in CI
```

Requires the .NET SDK version in `global.json`.

## Pull requests

- One topic per PR, branched off `main`.
- Conventional commits: `feat(cli): …`, `fix(session): …`, `docs: …`, `chore: …`, `ci: …`.
- Every behaviour change has a test; every bug fix has a regression test that fails without the fix.
- Specs live in `docs/specs/`. If code has to diverge from a spec, update the spec in the same PR.
- Read [AGENTS.md](AGENTS.md): its rules apply to people too.

## Releasing

1. Set the version in `Directory.Build.props` (`VersionPrefix` / `VersionSuffix`) and merge.
2. Tag the merge commit: `git tag v2.0.0-alpha.1 && git push origin v2.0.0-alpha.1`.
3. `.github/workflows/release.yml` refuses a tag that does not match `Directory.Build.props`. It runs CI, builds the binaries, packages and image, signs and attests them, then publishes to GitHub Releases, NuGet and ghcr.io.

To rehearse without publishing: run the Release workflow manually (publish off). Pull requests that touch the release machinery rehearse it automatically. Users verify releases as described in [docs/reference/verifying-releases.md](docs/reference/verifying-releases.md).

