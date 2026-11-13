# Desktop portability and provider audit

2026-11-12 · Version 0.6.0

## Platform audit

The original WPF application uses Windows-only UI classes, the Windows shell and winsqlite3.dll. It cannot become a macOS application by repackaging its executable.

The portable desktop application uses Avalonia 11.3.23 and a self-contained .NET 10 runtime. It links the same accounting ledger, replay store, cursor and built-in collectors as the Windows application. A compatibility layer reads the old checkpoint date/JSON format. Native SQLite resolves to the operating system library on each platform.

The release targets macOS 15 or later and Windows x64. .NET 8 reached end of support on November 10, 2026, so the distributable uses .NET 10 LTS. See the official [release lifecycle metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json) and [supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).

The new UI separates Overview, Providers, Appearance and Settings. Ten presets remain available, and installed font choices render their names in their own typeface. Windows and macOS can have different installed families. Missing fonts fall back.

## Verified local sources

| Program | Local source | Accounting boundary |
|---|---|---|
| Codex | sessions and archived_sessions JSONL | Existing cumulative-counter/reset/replay rules; account quota remains a separate snapshot |
| Claude Code | projects JSONL, including subagent folders | Assistant response IDs, monotonic snapshots, input plus cache read/write, output |
| Gemini CLI | tmp session JSON and append-only JSONL | Gemini messages with usage metadata; the reported total includes thought tokens; repeated message IDs deduplicate |
| OpenCode | opencode.db, read-only | Assistant usage in message or session_message tables; input, cache read/write, output and reasoning fields |

These adapters observe local formats, which may change between tool versions. Unknown OpenCode schemas display unsupported/unavailable status instead of inventing zero usage. Individual JSONL records above 1 MiB and whole Gemini JSON snapshots above 32 MiB are skipped. Codex compressed archives remain excluded. Web-only ChatGPT, Claude web, and tools without local token reports are not counted.

### Primary format references

- [Codex app server](https://learn.chatgpt.com/docs/app-server)
- [Claude Code usage](https://code.claude.com/docs/en/costs#using-the-usage-command)
- [Gemini CLI recording types](https://github.com/google-gemini/gemini-cli/blob/main/packages/core/src/services/chatRecordingTypes.ts) and [recording implementation](https://github.com/google-gemini/gemini-cli/blob/main/packages/core/src/services/chatRecordingService.ts)
- [OpenCode token fields](https://github.com/anomalyco/opencode/blob/dev/packages/schema/src/token-usage.ts) and [SQLite schema](https://github.com/anomalyco/opencode/blob/dev/packages/core/src/session/sql.ts)

## Data protection and resource controls

The portable version uses a separate data directory. Legacy import uses SQLite backup to copy consistent cache snapshots and never overwrites an existing target database. Local per-directory writer locking prevents duplicate application instances, including path aliases. A replacement source collector starts only after the previous worker has stopped.

Unselected providers stop collection. Gemini tails use file events with a periodic reconciliation, and OpenCode queries only when the database or WAL changes. UI refresh and count-up timers stop while hidden. Reports, prompts, account keys and browser sessions are never uploaded. Checkpoints contain aggregates, cursor metadata and hashed response identities; they do not contain conversation bodies.

## Release checks and limits

The shared accounting suite and provider-specific tests run on Windows, Apple Silicon macOS and Intel macOS. Native checks launch the extracted app, verify layout sizes/counters/font templates and export synthetic screenshots. macOS DMGs are mounted read-only and the app’s ad-hoc signature is verified before launch.

The macOS build is not Apple notarized, and the Windows build is unsigned. Runtime packaging means the portable release is larger than the old Framework-only executable. No current performance benchmark is claimed for the new UI. Passing hosted native checks does not prove every user environment or OS permission configuration.
