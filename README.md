# PlayDo's Token Odometer — Codex token usage tracker for Windows

[![Windows 10 / 11 x64](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4)](https://github.com/p7kbk748qb-debug/playdo-token-odometer#download-and-install)
[![Latest release](https://img.shields.io/github/v/release/p7kbk748qb-debug/playdo-token-odometer)](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/latest)
[![MIT license](https://img.shields.io/badge/license-MIT-blue)](https://github.com/p7kbk748qb-debug/playdo-token-odometer/blob/main/LICENSE)

**See your Codex token usage, discover which models you use most, and keep a small live counter on your Windows desktop.** PlayDo's Token Odometer is a free, open-source tray app with ten visual themes, compact layouts and CSV export. It reads local Codex usage records without an API key, account sign-in or background AI calls.

**[Download for Windows x64](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/latest/download/PlayDo-Token-Odometer-0.2.0-windows-x64.zip)** · [Release notes and SHA256 checksum](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/latest) · [Project website](https://p7kbk748qb-debug.github.io/playdo-token-odometer/)

![Winter theme: native Windows Codex usage dashboard with token odometer and model breakdown](https://raw.githubusercontent.com/p7kbk748qb-debug/playdo-token-odometer/main/assets/screenshots/winter.png)

*Screenshots use synthetic example data.*

## Download and install

Requires **Windows 10 or 11 x64**, **.NET Framework 4.8**, and local Codex usage records. This is a portable app; no MSI installer or administrator rights are required.

1. Download the [Windows ZIP](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/latest/download/PlayDo-Token-Odometer-0.2.0-windows-x64.zip).
2. Extract the entire ZIP into a writable folder you want to keep. Keep `assets` beside the executable.
3. Open **PlayDoTokenOdometer.exe**. The first launch indexes available local history; later launches resume incremental tracking.

The executable is **unsigned**. Windows may show an unknown-publisher or SmartScreen notice; review the source and release provenance before deciding whether to run it. See the [installation guide](https://github.com/p7kbk748qb-debug/playdo-token-odometer/blob/main/docs/INSTALL.md) for checksums, upgrades and removal. In a source checkout, build first and open `dist/PlayDoTokenOdometer.exe`.

- **Pin:** open the app, right-click its taskbar icon, choose **Pin to taskbar**. Run the executable with `--install-shortcuts` to create or repair the Start menu and local launchers, including their matching Windows app identity. From Start, right-click **PlayDo's Token Odometer** and choose **More → Pin to taskbar**. Windows owns the final pin action.
- **Background:** close the window; the tray icon keeps tracking. Double-click the tray icon or open the taskbar shortcut to restore it.
- **Layouts:** select Compact (440 × 260) or Mini (360 × 174); select Expand to return. **Style** opens appearance controls from either small view, and Done takes you back.
- **Appearance:** open **Appearance & settings** for the preset gallery. Each preset supplies coordinated interface and number fonts, counter styling, panel geometry and static materials. Fine-tune fonts or counters, then use Restore preset styling to return to that preset's defaults. Changes preview immediately and persist across launches. Missing fonts fall back safely.
- **Copy and export:** copy the current period's total or export its aggregate model breakdown to CSV. Exports contain scope, model and token counts; no session IDs, working folders or conversation text.
- **Startup:** enable **Start with Windows** in Settings if wanted. It is off by default.
- **Exit:** choose Exit in the tray menu or Exit app in Settings.
- **Pause:** pauses reads; Resume catches up on records written during the pause.

The release is portable. Keep its executable and `assets` together in a writable folder. An extracted release stores aggregate usage in `data/live` beside the executable. The development checkout stores it in the project's `data/live` folder. To uninstall, first disable Start with Windows, exit the app, then remove its folder and unpin its shortcut.

## Ten complete styles

Open **Appearance & settings** or press **Ctrl+,** to use Style Studio. Each preset changes interface and number fonts, counter styling, panel shapes and static materials.

![Style Studio showing ten coordinated themes for the token usage monitor](https://raw.githubusercontent.com/p7kbk748qb-debug/playdo-token-odometer/main/assets/screenshots/style-studio.png)

| Preset | Character |
| --- | --- |
| Instrument | Ink blue, amber and cyan; mechanical counter |
| Graphite | Neutral charcoal, silver accents; clean numbers |
| Arctic | Light steel and navy; crisp outlined panels |
| Terminal | Dark console, green phosphor and monospace text |
| Winter | Midnight frost, silver typography and crystalline detail |
| Hazy | Smoky lavender, warm light and softer serif typography |
| Aurora | Deep teal and plum with angular ribbons |
| Blueprint | Technical navy, cyan grid and drafting typography |
| Sunset | Warm charcoal, apricot accents and rounded plates |
| Paper | Ivory ledger, graphite ink and serif typography |

Presets use fonts already installed in Windows. Static brushes and patterns require no continuous animation or downloaded assets.

## Keyboard shortcuts

These work while the odometer window has focus.

| Shortcut | Action |
| --- | --- |
| Ctrl+1 / Ctrl+2 / Ctrl+3 | Today / 7 days / Recorded |
| Ctrl+Shift+C | Copy current token total |
| Ctrl+E | Export current scope as CSV |
| Ctrl+, | Open appearance and settings |
| Ctrl+M | Switch Compact / Dashboard |
| Ctrl+Shift+M | Open Mini |
| Escape | Close appearance and return to the previous view |

## What the numbers mean

**Tokens** are observations from local Codex `token_count` records. Input + output makes the total. Cached input is already included in input; reasoning tokens are already included in output. These can be large numbers because context is processed repeatedly during work.

**Today** uses the PC's local calendar date. **7 days** includes today plus the previous six dates. **Recorded** covers usage imported from available uncompressed local files. Initial indexing is shown explicitly and runs in a background worker. Later updates usually appear within about two seconds of a complete usage record, subject to Windows scheduling. Codex reports usage in batches; this is not a per-token stream.

The first record contributes its reported last-call counters, not an unexplained cumulative balance. Following records contribute cumulative increases. Repeated snapshots and exact fork replays are suppressed; a counter reset uses last-call usage. Incomplete historical logs, undisclosed fork baselines and missing records can limit coverage. This is a personal activity monitor, not an account billing statement.

**Models in use** ranks approximate tokens and each model's share of the chosen Today, 7 days or Recorded scope. Attribution follows the model named in each local turn's metadata, including switches within a chat. **Unknown** retains usage whose model was not recorded. Show all reveals additional models beyond the top eight.

The model breakdown reconciles to the global scope total. The app does not track projects or working folders.

The first launch of this upgrade indexes local metadata once into `data/live/usage-v3.db`. Previous `usage-v2.db` files remain intact and are read only to preserve older observations. If original records are no longer readable, their tokens remain Unknown. Returning old logs can fill in attribution without adding their usage twice; new usage continues to increase normally. Later launches resume incremental reads.

**Account allowance** is the latest quota snapshot Codex wrote locally. The app displays remaining percentage, window duration, reset and reported age. Snapshots older than 15 minutes are labeled older. After a reset passes, the app waits for a new report instead of inventing a renewed allowance. Cloud or other-device usage appears only when a local Codex quota snapshot reflects it. Token totals do not directly convert into subscription percentage or dollar charges.

Compressed `.jsonl.zst` archives are excluded and counted in the footer. Remote sessions without local records are not counted. A zero is a measured scope total; absent quota is shown as unknown.

## Low overhead and privacy

- Native C# / WPF; no browser engine, Node server, cloud database or AI calls.
- Windows file notifications coalesce updates. A 60-second reconciliation checks metadata to recover missed notifications.
- Complete-line byte cursors avoid re-reading unchanged logs. Large unrelated lines have a 256 KiB accumulation cap.
- Windows' built-in SQLite stores hashed replay identities on disk with a 2 MiB page-cache target. Aggregate checkpoint and replay identities commit in one transaction.
- No continuous frame/render loop. Hidden/minimized windows stop UI refresh timers and avoid collector-driven redraws.
- Reads the user's `CODEX_HOME` or defaults to `%USERPROFILE%\.codex`. No authentication keys are read and no network requests are made.
- Local cache contains dates, aggregate counts, session IDs, model names, source-file positions, quota snapshots and replay hashes. Working-directory/project metadata is ignored. Prompt text and conversation bodies are not persisted. Model metadata stays on this PC.
- Actual CPU and memory vary with Windows, DPI, history size and active workloads. Theme materials are static and UI refresh stops while hidden.

One 15-second tray sample during active Codex work measured approximately **84 MB working set and 0.36% of total machine CPU** on a 12-logical-processor PC. This is an observation, not a performance guarantee.

## FAQ

### Does it work with a ChatGPT Codex subscription?

It can read usage records written locally by Codex, without an API key. It does not access your account billing or infer your subscription cost. It is an independent community project, not an official OpenAI product.

### Why does my total differ from another usage dashboard?

This app counts available local records using the replay and reset rules described above. Remote sessions without local records, missing historical files and compressed `.jsonl.zst` archives are not counted. Recorded usage is not complete account-wide history. The displayed allowance is a separate Codex snapshot, not a conversion from tokens.

### Does it upload my conversations?

No. The application makes no network requests and does not persist conversation bodies. CSV export contains only scope, model and aggregate token counts. Keep local cache files and diagnostic reports private.

## Build and test

Requires Windows 10/11 x64, installed .NET Framework 4.8, and Windows PowerShell. No SDK, NuGet or package downloads.

```powershell
.\test.ps1
.\build.ps1
.\tools\verify-appearance.ps1
.\test-qol.ps1
.\package.ps1 -SkipBuild
.\tools\verify-package.ps1
```

Tests exercise cumulative accounting, cached subsets, replay/reset protection, day scoping, 64-bit counters, split/oversized/replaced lines, live appends, pause/catch-up, transactional cache restart and corrupt-cache recovery.

`tools/verify-appearance.ps1` exercises the preset gallery, preference persistence, typography, contrast, small layouts, shortcuts, model scopes, absence of project controls and preserved-history display using synthetic records; its screenshots stay in ignored `build/`. `test-qol.ps1` checks scope selection, 64-bit totals, CSV escaping and spreadsheet formula protection. `package.ps1` can also build and run all checks itself before creating the portable ZIP and SHA256 file in `release/`.

The build generates `assets/app-icon.png`, multi-size `assets/app.ico`, and `assets/cover.png` from authored vector geometry in `tools/BrandAssets.cs`. No external branding asset is required. The executable is a personal unsigned build.

Useful command-line options:

```text
--tray                     Start hidden in the tray
--compact                  Open compact view
--mini                     Open the smallest view
--appearance               Open the preset gallery and settings
--view <Dashboard|Compact|Mini>  Select and save a layout
--shortcut                 Create the branded local launcher shortcut
--install-shortcuts        Create/repair local and Start menu launchers
--exit                     Ask the existing instance to exit
--codex-home <folder>       Use another local Codex home
--data <folder>             Override aggregate/settings directory
--isolated                 Separate test instance identity
--capture <png>             Save the app's rendered UI after indexing
--report <json>             Save a local diagnostic snapshot (contains usage)
--exit-after-capture        Exit after capture/report
```

Keep `data/`, diagnostics and live screenshots out of GitHub; the ignore rules already exclude them. The repository ships source, docs and branding, not personal usage.

Read the [contribution guide](https://github.com/p7kbk748qb-debug/playdo-token-odometer/blob/main/CONTRIBUTING.md) to help with the code, and the [security policy](https://github.com/p7kbk748qb-debug/playdo-token-odometer/security/policy) before reporting sensitive issues. Portable packages include the installation guide, license and changelog.

Protocol reference: [official OpenAI Codex app-server documentation](https://learn.chatgpt.com/docs/app-server). The app uses observed local record schemas, which may require updates if Codex changes them.

## Help the project grow

If this Codex usage monitor helps you, a **GitHub star** makes it easier for others to find. Useful [bug reports](https://github.com/p7kbk748qb-debug/playdo-token-odometer/issues/new?template=bug_report.yml), [feature ideas](https://github.com/p7kbk748qb-debug/playdo-token-odometer/issues/new?template=feature_request.yml), synthetic screenshots and contributions are welcome.

Released under the [MIT license](https://github.com/p7kbk748qb-debug/playdo-token-odometer/blob/main/LICENSE). “Codex” and OpenAI marks belong to their respective owners.
