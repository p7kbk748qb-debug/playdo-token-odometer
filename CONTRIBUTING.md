# Contributing

PlayDo's Token Odometer is a C# desktop application for Windows and macOS, using Avalonia and a shared local accounting core. Changes should keep usage private, avoid extra background work and support Dashboard, Compact, Mini and Tiny layouts. The previous Windows WPF application remains in `src/` for compatibility and regression checks.

## Before you start

Check [existing issues](https://github.com/p7kbk748qb-debug/playdo-token-odometer/issues) for related work. Small fixes can go straight to a pull request. For a large change or new dependency, open a feature proposal explaining the user need and resource/privacy tradeoffs first.

## Development setup

Use .NET SDK 10 and PowerShell 7. Build native packages on their target operating system. macOS releases target version 15 or later.

```powershell
dotnet run --project portable/Tests/Odometer.Tests.csproj
dotnet run --project portable/ProviderTests/ProviderTests.csproj
dotnet run --project portable/Desktop/PlayDo.Odometer.Desktop.csproj
./portable/package.ps1 -Runtime win-x64
```

On macOS, package with `osx-arm64` or `osx-x64`. The portable workflow verifies extracted native packages, local accounting, provider formats, font previews and window layouts using synthetic records. See [the provider audit](docs/PORTABILITY-AUDIT.md) before adding a source.

The legacy Windows build uses .NET Framework 4.8 and Windows PowerShell:

```powershell
.\test.ps1
.\build.ps1
.\tools\verify-appearance.ps1
.\test-qol.ps1
```

The legacy appearance checks render synthetic usage. Legacy packaging uses `./package.ps1` and `./tools/verify-package.ps1`; these scripts build the previous WPF application, not the new portable desktop release.

## Changes and tests

- Shared `src/` files must remain compatible with the legacy compiler used by `build.ps1`. Keep portable-only code in `portable/`. Avoid dependencies without a clear need.
- Add focused synthetic fixtures for accounting, replay protection or persistence changes. Never use a real Codex session as a fixture.
- Check contrast, keyboard access, reduced motion and all four layouts after appearance changes. Preview font names in their own typeface, use safe fallbacks, and preserve third-party font licenses.
- Preserve aggregate totals across upgrades. Model attribution is approximate, and cached input and reasoning counts are subsets.
- Keep background work event driven and bounded. Hidden windows should not redraw continuously.
- Project tracking has been removed; do not reintroduce working-directory collection as part of model attribution.

Include the reason for a change, user-visible behavior and verification in a pull request. Keep screenshots synthetic. Build, test and package artifacts stay outside source control. No minimum number of changes or tests is required: use focused verification appropriate to the change.

## Privacy when reporting problems

Never attach provider data folders, authentication files, usage databases, settings, real diagnostic reports or live screenshots. Share a minimal synthetic example and app/OS versions instead. See [SECURITY.md](SECURITY.md) for sensitive reports.

## License

The project uses the [MIT license](LICENSE). Submitted contributions should be compatible with it. Do not include third-party fonts or assets without a compatible license and attribution.
