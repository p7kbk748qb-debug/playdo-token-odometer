# PlayDo’s Token Odometer

## Purpose
A local desktop companion that shows observed AI coding token usage without credentials or paid services. Users select the tools they use and keep an optional tiny counter on their desktop.

## Platform
Windows and macOS desktop. Existing WPF Windows releases remain available. The portable desktop update must keep actual token accounting separate from subscription credits and quota.

## Stack
The existing C# accounting engine is authoritative. A shared native desktop surface will use Avalonia and .NET, retaining incremental reads and local SQLite. This framework choice is an implementation decision to avoid maintaining two accounting engines.

## Requirements
Codex and Claude Code history and preferences must remain recoverable. Provider support must be based on verified local schemas. Unknown providers or inaccessible files must not look like zero usage. No credential collection, cloud sync or analytics.

## Design direction
The owner selected a clean instrument panel: charcoal, crisp type, cyan and amber accents. Preserve the transparent gauge icon, exact totals, Tiny/Mini/Compact, provider choice, reduced motion and optional animation.
