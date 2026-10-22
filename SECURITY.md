# Security and privacy

The odometer reads local Codex usage records and stores local aggregates. It does not require an API key or a cloud service. Personal usage and model metadata can still be sensitive.

## Reporting a vulnerability

Please report security issues privately to the maintainer. If available, use [GitHub private vulnerability reporting](https://github.com/p7kbk748qb-debug/playdo-token-odometer/security/advisories/new) under **Security → Report a vulnerability**. Otherwise, ask for a private reporting channel in a public issue without disclosing sensitive details. Do not post credentials, personal usage data or an exploit involving someone else's files in a public issue.

Include the app version, Windows version, a synthetic reproduction and the expected impact. Do not upload raw Codex logs, authentication files, a `.codex` directory, `data/` databases, settings, live screenshots or diagnostic snapshots. Redact identifying paths and session identifiers.

## Distribution

Portable packages are unsigned unless a release explicitly says otherwise. Obtain them from [the project releases](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases), compare the published SHA256 checksum, and keep the executable and its assets in a folder you control. A checksum detects a changed download; it does not authenticate an unknown distributor.

Builds require no administrator rights. Start with Windows is optional. Packaging includes an explicit public-file whitelist and excludes local usage, settings and diagnostics.

Only maintainers should publish releases. A locally generated ZIP or CI artifact is not a signed or publicly published release.

## Supported versions

Security fixes target the latest release. Use the latest version when reporting an issue, or explain why the issue also affects it. There is no guaranteed response time or security support period.
