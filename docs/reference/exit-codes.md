# Exit codes

`xping` exits with one of these codes. CI scripts can rely on them. Source: `src/Xping.Core/ExitCodes.cs`; a test keeps this page and the code in sync.

| Code | Name | Meaning |
|---|---|---|
| 0 | `Success` | The command ran and nothing reached a `block` policy. |
| 1 | `Blocked` | At least one finding reached a `block` policy in `.xping.yml`. |
| 2 | `InputError` | An input (test results, history, configuration) is missing or unreadable. |
| 64 | `UsageError` | The command line could not be parsed: unknown option, bad value, missing subcommand. |
| 69 | `NotImplemented` | The command exists but is not implemented yet. |
| 70 | `InternalError` | Xping failed unexpectedly. Please report it. |
