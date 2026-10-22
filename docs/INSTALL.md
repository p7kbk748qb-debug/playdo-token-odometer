# Install PlayDo's Token Odometer

## Requirements

Windows 10 or 11 x64, .NET Framework 4.8 and a local Codex installation that writes usage records. The app needs no API key, administrator rights, Node server or font downloads. Model and token totals depend on the local records available on this PC.

## Portable package

1. Download [PlayDo-Token-Odometer-0.2.0-windows-x64.zip](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/latest/download/PlayDo-Token-Odometer-0.2.0-windows-x64.zip) and its `.sha256` file from the [official project releases](https://github.com/p7kbk748qb-debug/playdo-token-odometer/releases/latest).
2. Optionally verify the download in PowerShell:

   ```powershell
   Get-FileHash .\PlayDo-Token-Odometer-0.2.0-windows-x64.zip -Algorithm SHA256
   Get-Content .\PlayDo-Token-Odometer-0.2.0-windows-x64.zip.sha256
   ```

   The hash values must match. A checksum does not authenticate a distributor; get both files from the intended maintainer.
3. Extract the entire ZIP into a writable folder you control, for example a folder under Documents. Do not run the app directly inside the ZIP or place it in a protected system directory.
4. Double-click `PlayDoTokenOdometer.exe`. Keep the `assets` folder beside it. This portable executable is unsigned. Windows may show an unknown-publisher or SmartScreen notice; review the source and release provenance before deciding whether to run it.

The portable app creates `data/live` beside its executable. It contains local aggregates and settings. The initial import can take longer than later launches; live updates follow complete Codex usage records.

## Custom Codex location

By default, the app reads the `CODEX_HOME` environment variable, or `%USERPROFILE%\.codex` when it is not set. If your Codex records live elsewhere, launch the app with an explicit folder:

```powershell
.\PlayDoTokenOdometer.exe --codex-home "D:\MyCodexHome"
```

Exit the existing instance before changing the source folder. The folder must be a Codex home containing its session records, not an individual JSONL file. This option changes the source for that launch; repeat it in a shortcut's target arguments if you want to keep using it. Use `--data "D:\OdometerData"` for a separate writable cache/settings location when needed.

## Start menu and taskbar

Run this once from the extracted folder to create or repair local and Start menu shortcuts:

```powershell
.\PlayDoTokenOdometer.exe --install-shortcuts
```

Open the app, right-click its taskbar icon and choose **Pin to taskbar**. Windows controls the final pin action. If you move the extracted folder, repair the shortcuts and pin the new location.

Closing the window keeps tracking in the tray. Double-click the tray icon or reopen the taskbar shortcut to restore it. Choose **Exit** in the tray menu to stop the app completely. **Start with Windows** is an optional setting and is off by default.

## Upgrade and remove

For an upgrade, exit the old app first. Keep a local backup of `data/live`, then replace only the executable and assets with the new release in the same folder. Keep personal databases and settings out of public bug reports and repositories.

To remove the app, disable **Start with Windows**, exit through the tray menu, unpin it, remove its Start menu shortcut and delete its portable folder when you no longer need the local usage history. Shortcuts point to the portable folder; moving or removing that folder does not remove shortcuts automatically.

## Build your own package

From a source checkout, use Windows PowerShell:

```powershell
.\package.ps1
```

This builds and checks the app, then creates a ZIP and checksum in `release/`. It includes only the executable, three branded assets, README, license, changelog and this guide. Local data, settings, diagnostic reports, screenshots and test executables are excluded. No release is uploaded automatically.
