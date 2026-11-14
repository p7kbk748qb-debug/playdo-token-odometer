# Changelog

## Unreleased

- Fixed square backing corners on Windows desktop widgets by matching the native window region to the rounded panel. Dashboard windows restore their normal shape.

## 0.6.1 — 2026-11-13

- Fixed a font-dropdown crash when Avalonia recycles an empty item. Added native open, scroll, select and reopen regression checks for both font pickers.
- Corrected bundled Inter font resolution, keeping installed font previews and all ten presets.
- Redesigned the dashboard with provider totals, an actual seven-day usage chart, separate allowance windows/reset times, cache share, report freshness and expandable model breakdowns.
- Refined navigation, typography, controls and narrow-window layout without removing providers, widget modes, transparency, exports or settings.
- Added optional 220 ms interface/chart effects. Reduce motion overrides them; hidden windows stop refresh and animation timers.

## 0.6.0 — 2026-11-12

- Added self-contained native desktop packages for Windows x64, Apple Silicon Macs and Intel Macs.
- Rebuilt the dashboard with separate Overview, Providers, Appearance and Settings pages.
- Added Gemini CLI session and OpenCode SQLite usage sources, independently selectable with Codex and Claude Code.
- Added a broader installed-font picker with names rendered in their own typeface, live number previews and ten coordinated presets.
- Preserved the four layouts, optional translucency, count-up animation, reduced motion, provider/model CSV and Mini/Compact widget choices.
- Added consistent, non-destructive legacy Windows cache import and local single-writer protection.
- macOS bundles are ad-hoc signed and not notarized. Windows packages are unsigned. The runtime is included.

## 0.5.0 — 2026-11-12

- Added Tiny: a 300 × 68 DIP number-only widget, draggable by its background or number, with small hide and dashboard controls.
- Tiny supports themes, transparency, corner placement, saved layout restoration and the existing right-click actions. Open it from the dashboard, tray, window picker, Ctrl+Shift+T or --tiny.
- Added optional **Animate token increases** in Appearance. A short native count-up runs only for increases in the same view, provider and scope. Reduce motion overrides it; hidden windows stop the timer.
- Included the transparent taskbar gauge icon, with nine native sizes.

## 0.4.0 — 2026-11-08

- Choose Codex, Claude Code, or both in the token counter, model breakdown and CSV export.
- Claude Code imports local assistant usage, including cache reads and writes, and deduplicates repeated responses across files and restarts.
- Mini and Compact have no separate title bar. Dock them to a screen corner or drag them freely, and optionally let the desktop show through the panel.
- Codex allowance remains a separate local snapshot. Claude Code plan allowance is not inferred from tokens.

## 0.3.1 — 2026-10-25

- Corrected the packaged README's widget guide and screenshot links so they also work outside a source checkout. Widget behavior is unchanged from 0.3.0.

## 0.3.0 — 2026-10-25

- Mini and Compact have separate saved widget settings, accessible from either window without expanding the dashboard.
- Show 5-hour and weekly Codex usage bars, optional reset times, input/output totals, the most-used model and activity status.
- Small windows fit the enabled widgets. Missing, older and expired allowance reports are labeled; hover for the report time.
- Existing themes, preferences, token history and taskbar identity are preserved. No new background requests or dependencies.

See [small window settings](https://github.com/p7kbk748qb-debug/playdo-token-odometer/blob/main/docs/SMALL-WINDOWS.md). Download the portable ZIP and checksum from [v0.3.0](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/tag/v0.3.0).

## 0.2.0 — 2026-10-22

- Theme Studio with ten complete appearance presets, including Winter and Hazy, with coordinated typography and counter treatments.
- Dashboard, Compact and Mini layouts with saved appearance preferences.
- Separate interface and number fonts, restore-preset controls, copy total, aggregate CSV export and keyboard shortcuts.
- Approximate model usage rankings and retained local token accounting. Project tracking remains removed.
- Portable Windows x64 packaging with a strict public-file whitelist and SHA256 checksum.
- Version metadata, installation instructions, contribution guidance and MIT license.
- Public project documentation and issue templates for reproducible bug reports and feature proposals.

Download versioned assets and checksums from the [GitHub release page](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/tag/v0.2.0).

## Earlier builds

- Native WPF token odometer, daily activity and recorded account allowance.
- Tray operation, single-instance restore, Start menu shortcuts and optional startup.
- Incremental local usage reads with replay protection and SQLite aggregate checkpoints.
