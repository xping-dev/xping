# Security policy

## Reporting a vulnerability

Please **do not** open a public issue. Report it privately through GitHub: open the repository's **Security** tab and choose **Report a vulnerability**.

If you cannot use GitHub, email adrian@xping.io.

We acknowledge reports within 3 working days and keep you updated until a fix ships.

## Supported versions

Xping is pre-release. Only the latest published version receives fixes.

## What Xping promises

- No data leaves your environment: no Xping service, no telemetry. The only outbound calls go to your CI provider's API, with the token the CI job provides.
- No LLM calls.
- Releases are signed (Sigstore/cosign), ship a CycloneDX SBOM and carry SLSA provenance. (Planned for the first release.)

A breach of any of these is a security issue. Please report it as one.
