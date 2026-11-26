# README note for xping-dev/sdk-dotnet

Paste at the top of `README.md` in xping-dev/sdk-dotnet, below the title.

---

> [!IMPORTANT]
> **The `xping` CLI has moved to [xping-dev/xping](https://github.com/xping-dev/xping).**
> Xping now checks pull requests for weakened tests and runs entirely in your CI, with history kept in your own repository. It needs no SDK and no account, and no data leaves your environment.
>
> This repository keeps the **optional** Xping SDK for .NET (NUnit, xUnit v2/v3, MSTest). The SDK records richer test evidence (retries, environment, local runs) that the CLI reads.
>
> - CLI and its issues: https://github.com/xping-dev/xping
> - Documentation: https://docs.xping.io
> - `Xping.Cli` 1.0.0-rc.x is deprecated; install `Xping.Cli` 2.x: `dotnet tool install -g Xping.Cli --prerelease`
> - The 2.x CLI does not connect to Xping Cloud and has no `xping login`.

---

TODO(decision): what to say about Xping Cloud itself (prespec §11.12: app.xping.io, pricing page, GitHub App). Add one line here once that is decided.

Also in that PR:
- **sdk-dotnet README "CLI" section:** replace it with a link to the new repository.
- **`nuspec/README.Cli.md`:** delete. The CLI package is no longer built from sdk-dotnet.
- **`src/Xping.Cli`, `tests/Xping.Cli.Tests`:** delete. The code continues in xping-dev/xping with its history. This is a separate PR, after `Xping.Cli` 2.0.0-alpha.1 is on nuget.org.
- **Docs deploy job** in sdk-dotnet `release.yml`: remove it at the docs.xping.io switch (migration plan §8).
