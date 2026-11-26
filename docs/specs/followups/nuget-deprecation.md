# NuGet deprecation for the old CLI

Applies to **`Xping.Cli` 1.0.0-rc.1 … 1.0.0-rc.8** (built from xping-dev/sdk-dotnet). Apply after `Xping.Cli` 2.0.0-alpha.1 is published, so the alternative exists.

nuget.org → Manage package `Xping.Cli` → Deprecation → select every `1.0.0-rc.*` version:

| Field | Value |
|---|---|
| Reason | **Legacy** |
| Alternate package | `Xping.Cli`, version `2.0.0-alpha.1` (or "latest") |
| Custom message | see below |

Custom message (495 characters):

```text
The 1.0.0-rc builds of the xping CLI reported flaky tests from the Xping SDK's local store and Xping Cloud. Xping 2.x is a new CI-native tool that checks pull requests for weakened tests (removed assertions, loosened timeouts, skipped tests, mocked classes under test). It runs entirely in your CI and keeps history in your own repository. It needs no account and sends no data anywhere. Install: dotnet tool install -g Xping.Cli --prerelease. Source and docs: https://github.com/xping-dev/xping
```

Do **not** unlist the rc versions (Q4): people pinned to them keep restoring, and the deprecation warning tells them where to go.

Not deprecated: `Xping.Sdk.Core`, `Xping.Sdk.NUnit`, `Xping.Sdk.XUnit`, `Xping.Sdk.XUnit.V3`, `Xping.Sdk.MSTest`. The SDK continues; see `sdk-on-xping-session.md`.

The unlisted `Xping.Core` 1.0.0-rc1 (old web-testing SDK) stays unlisted. When this repository publishes `Xping.Core` it will be a new 2.x version.
