# Corner widget and Claude Code implementation plan

Use Superpowers executing-plans inline. Keep the existing WPF stack and Codex database.

1. Add a Claude Code reader to the existing incremental collector. Scan only local project JSONL, count assistant usage by provider response ID, and store aggregates, cursor offsets and deduplication metadata in a separate checkpoint. Count uncached, cache-read and cache-write input once. Exercise repeated chunks, replay and restart with synthetic records.
2. Add Codex / Claude Code source preferences. Combine selected snapshots across dashboard, small views, chart, model rows and CSV. Keep Codex allowance explicitly labeled and hidden in Claude-only mode. No credentials, polling, or invented Claude allowance.
3. Refine Mini and Compact as corner widgets. Remove the separate title bar, keep drag, dashboard, settings and hide actions available, add an optional translucent surface, and place the widget inside the monitor's work area. Keep theme identity and independent widget options. Verify real WPF layouts and state restoration.
4. Update public usage and upgrade docs, build and verify the Windows ZIP, then push the public checkout and publish the release. Keep personal records and installation out of the package.
