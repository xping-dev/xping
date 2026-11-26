# Schemas

Public, versioned data contracts. Teams query these files directly, so a change to them is a contract change.

| File | `$id` | Status |
|---|---|---|
| [`session.schema.json`](session.schema.json) | `urn:xping:schema:session:v1` | Draft v1 |
| [`history.schema.json`](history.schema.json) | `urn:xping:schema:history:v1` | Draft v1 |

Rules:

- Every document carries `schemaVersion`. Readers reject versions newer than they know and ignore unknown properties.
- A breaking change adds a new major version and a new file. The old schema stays here, and Xping keeps an importer for it.
- Session files written before v1 (by `Xping.Sdk.Core` 1.0.0-rc.x) have no `schemaVersion`. Xping reads them as version 0 through the legacy importer.
