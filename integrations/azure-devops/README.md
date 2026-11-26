# Xping for Azure Pipelines

Marketplace extension skeleton. **Not implemented yet.** The task fails with exit code 69.

The task is a thin wrapper: it downloads a pinned, signature-verified `xping` binary and runs it. Behaviour lives in the CLI, never here.

```yaml
# Pull request pipeline
- task: Xping@0
  inputs:
    command: check
    version: 2.0.0-alpha.1
    results: '**/TestResults/*.trx'

# Default-branch pipeline (needs Contribute permission for the Build Service on the history ref)
- task: Xping@0
  inputs:
    command: record
    version: 2.0.0-alpha.1
  env:
    SYSTEM_ACCESSTOKEN: $(System.AccessToken)
```

## Packaging

```sh
npx tfx-cli extension create --manifest-globs vss-extension.json
```

TODO(spec): Marketplace publisher ID, YAML template, signature verification, release automation.
