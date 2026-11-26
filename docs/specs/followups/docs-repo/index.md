# Xping

**Xping checks that the safety net wasn't weakened.**

Coding agents and people sometimes make a red build green by weakening tests: removing an assertion, raising a timeout, skipping a test as "flaky", mocking the class under test. Xping finds these changes in pull/merge requests and backs each finding with execution history kept in your own repository.

- Runs in your CI: GitHub Actions, GitLab CI, Azure Pipelines.
- No data leaves your environment. No telemetry. No LLM calls.
- No SDK required: reads JUnit XML and TRX.

[Get started](getting-started/index.md)
