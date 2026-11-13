# Portable desktop audit and implementation

1. Audit native Windows dependencies and provider coverage; document supported schema boundaries.
2. Reuse the existing accounting, replay database and cursor core under .NET 8 with portable JSON and SQLite bindings. Add verified Gemini CLI and OpenCode adapters with synthetic accounting tests.
3. Build a shared native Avalonia dashboard: a compact navigation rail, overview, provider selection, and separate appearance/settings pages. Keep exact counts and the existing desktop widget sizes.
4. Package a self-contained Windows application and macOS app bundles for Apple Silicon and Intel. Verify on hosted macOS runners rather than claiming Windows tests prove Mac installation.
5. Preserve the existing pinned Windows application and its private history until a deliberate upgrade. Add import of legacy data by copying a consistent SQLite backup, never modifying the old cache.
6. Run accounting, native UI and package checks; publish only verified public artifacts. Record limitations honestly, including unsigned/notarized status.
