# Contributing

PlayDo's Token Odometer is a native Windows app written in C# and WPF. Changes should keep local usage private, avoid extra background work and remain usable in Dashboard, Compact and Mini layouts.

## Before you start

Check [existing issues](https://github.com/p7kbk748qb-debug/playdo-token-odometer/issues) for related work. Small fixes can go straight to a pull request. For a large change or new dependency, open a feature proposal explaining the user need and resource/privacy tradeoffs first.

## Development setup

Use Windows 10 or 11 x64, .NET Framework 4.8 and Windows PowerShell. The scripts use the Windows Framework C# compiler; no SDK, NuGet packages or font downloads are required.

```powershell
.\test.ps1
.\build.ps1
.\tools\verify-appearance.ps1
.\test-qol.ps1
```

The appearance checks render synthetic usage. For a complete portable build, close an app running from `dist` and run `./package.ps1`, then `./tools/verify-package.ps1` to extract and test the ZIP with synthetic records. Use `./package.ps1 -SkipBuild` only after building and verifying the same source yourself.

## Changes and tests

- Keep C# compatible with the compiler used by `build.ps1`; avoid adding package dependencies without discussing the need.
- Add focused synthetic fixtures for accounting, replay protection or persistence changes. Never use a real Codex session as a fixture.
- Check text contrast, keyboard access, reduced motion and all three window layouts after appearance changes. Prefer installed Windows fonts with a fallback; do not redistribute fonts.
- Preserve aggregate totals across upgrades. Model attribution is approximate, and cached input and reasoning counts are subsets.
- Keep background work event driven and bounded. Hidden windows should not redraw continuously.
- Project tracking has been removed; do not reintroduce working-directory collection as part of model attribution.

Include the reason for a change, user-visible behavior and verification in a pull request. Keep screenshots synthetic. Build, test and package artifacts stay outside source control. No minimum number of changes or tests is required: use focused verification appropriate to the change.

## Privacy when reporting problems

Never attach your `.codex` folder, authentication files, usage databases, settings, real diagnostic reports or live screenshots. Share a minimal synthetic example and app/Windows versions instead. See [SECURITY.md](SECURITY.md) for sensitive reports.

## License

The project uses the [MIT license](LICENSE). Submitted contributions should be compatible with it. Do not include third-party fonts or assets without a compatible license and attribution.
