# Security and privacy

The odometer reads selected local Codex, Claude Code, Gemini CLI and OpenCode usage records and stores local aggregates. It does not require an API key or a cloud service. Personal usage and model metadata can still be sensitive.

## Reporting a vulnerability

Please report security issues privately to the maintainer. If available, use [GitHub private vulnerability reporting](https://github.com/p7kbk748qb-debug/playdo-token-odometer/security/advisories/new) under **Security → Report a vulnerability**. Otherwise, ask for a private reporting channel in a public issue without disclosing sensitive details. Do not post credentials, personal usage data or an exploit involving someone else's files in a public issue.

Include the app version, operating system, a synthetic reproduction and the expected impact. Do not upload raw provider logs, authentication files, provider data directories, usage databases, settings, live screenshots or diagnostic snapshots. Redact identifying paths and session identifiers.

## Distribution

Windows packages are unsigned. Mac bundles are ad-hoc signed, not Apple notarized. Obtain packages from [the project releases](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases), compare the published SHA256 checksum, and keep the executable and its assets together in a folder you control. A checksum detects a changed download; it does not authenticate an unknown distributor.

The application uses per-user data directories. Portable packaging includes published binaries and an explicit list of public documentation, resources and notices; it excludes local usage, settings and diagnostics. Legacy Start with Windows remains optional in the previous WPF version.

Only maintainers should publish releases. A locally generated ZIP or CI artifact is not a signed or publicly published release.

## Supported versions

Security fixes target the latest release. Use the latest version when reporting an issue, or explain why the issue also affects it. There is no guaranteed response time or security support period.
