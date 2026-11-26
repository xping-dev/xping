// SPDX-License-Identifier: MIT
// Copyright (c) Xping
//
// Azure Pipelines task: a thin wrapper around the xping binary. No logic lives here.
//
// TODO(spec):
//   1. Download the pinned xping release for the agent's OS/arch from GitHub Releases.
//   2. Verify its SHA-256 and cosign signature before running it.
//   3. Run `xping <command> --results <globs> <arguments>` with SYSTEM_ACCESSTOKEN in the
//      environment (record needs Contribute on the history ref).
//   4. Map the exit code: 0 succeeded, 1 failed (blocked), other codes failed with a message.

'use strict';

const NOT_IMPLEMENTED = 69; // docs/reference/exit-codes.md

console.log('##vso[task.logissue type=error]The Xping task is not implemented yet.');
console.log('##vso[task.complete result=Failed;]Not implemented');
process.exitCode = NOT_IMPLEMENTED;
