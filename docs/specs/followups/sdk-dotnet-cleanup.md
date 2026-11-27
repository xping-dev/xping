# Cleanup in xping-dev/sdk-dotnet

The CLI now lives in xping-dev/xping. sdk-dotnet keeps only the SDK. No README banner: the old CLI has no users to redirect.

After `Xping.Cli` 2.0.0-alpha.1 is on nuget.org, in one PR in sdk-dotnet:
- **`src/Xping.Cli`, `tests/Xping.Cli.Tests`:** delete. The code continues in xping-dev/xping with its history.
- **`nuspec/README.Cli.md`** and any packing of `Xping.Cli` in `release.yml`: delete, so sdk-dotnet can no longer publish `Xping.Cli`.
- **README:** remove the "CLI commands" section and the CLI parts of "Two ways to run Xping"; link xping-dev/xping instead.
