# Verifying a release

Every Xping release is built by `.github/workflows/release.yml` from a tag `v<version>`, on GitHub-hosted runners. Each published file has:

- a **cosign keyless signature** (`<file>.sigstore.json`), signed by that workflow's identity and logged in the public Rekor transparency log;
- a **SLSA build provenance attestation** (GitHub artifact attestations) naming the repository, workflow, commit and runner;
- an entry in `SHA256SUMS`.

The release also ships **CycloneDX SBOMs** for the CLI (`xping-<version>.cdx.json`), the `Xping.Session` library (`xping-session-<version>.cdx.json`) and the container image (`xping-image-<version>.cdx.json`).

Below, `VERSION=2.0.0-alpha.1` stands for the version you downloaded.

## Binaries and NuGet packages

```sh
VERSION=2.0.0-alpha.1
FILE=xping-$VERSION-linux-x64.tar.gz

# 1. Provenance: built by this repository's release workflow.
gh attestation verify "$FILE" --repo xping-dev/xping

# 2. Signature: signed by the release workflow for this exact tag.
cosign verify-blob "$FILE" \
  --bundle "$FILE.sigstore.json" \
  --certificate-identity "https://github.com/xping-dev/xping/.github/workflows/release.yml@refs/tags/v$VERSION" \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com

# 3. Checksum.
sha256sum --check --ignore-missing SHA256SUMS
```

The same commands work for `Xping.Cli.<version>.nupkg` and `Xping.Session.<version>.nupkg`. NuGet also verifies its own repository signature on install.

## Container image

```sh
IMAGE=ghcr.io/xping-dev/xping:$VERSION

gh attestation verify "oci://$IMAGE" --repo xping-dev/xping

cosign verify "$IMAGE" \
  --certificate-identity "https://github.com/xping-dev/xping/.github/workflows/release.yml@refs/tags/v$VERSION" \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com

# The image SBOM is attached as a signed CycloneDX attestation.
cosign verify-attestation "$IMAGE" --type cyclonedx \
  --certificate-identity "https://github.com/xping-dev/xping/.github/workflows/release.yml@refs/tags/v$VERSION" \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
```

Pin the image by digest in CI (`ghcr.io/xping-dev/xping@sha256:…`), not by tag.

## What Xping does at runtime

No network calls except to your CI provider's API, with the token your CI job provides. No telemetry. No LLM calls. A test in the repository (`NoCloudDependencyTests`) fails the build if the CLI's dependency graph contains a telemetry client or the code names an Xping service endpoint.
