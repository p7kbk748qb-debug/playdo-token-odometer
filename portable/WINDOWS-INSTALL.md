# Install the portable desktop version on Windows

Extract the entire **win-x64** ZIP into a writable folder. The runtime is included. The application is unsigned; review the source and release before deciding whether to run it. New users can open **PlayDoTokenOdometer.exe** directly. Existing 0.5 users should import history **before the first launch**, as below.

Version 0.6 uses a new native desktop UI shared with macOS. Keep the 0.5 WPF application folder until you have verified your history in the new application. Do not overwrite an old application that is running.

## Keep existing Windows history

Exit the old app from its tray menu. Start the new executable once with:

    .\PlayDoTokenOdometer.exe --import-legacy "C:\path\to\old-odometer\data\live"

This uses SQLite’s backup API to import a separate consistent copy of the old caches. Existing target databases are not overwritten. Supported old appearance, provider and widget preferences are imported if no new preferences already exist.

If you already opened the new app, exit it and use a new data directory for the import, for example `--data "D:\ImportedOdometerData"` alongside `--import-legacy`. Use that same `--data` argument in your new shortcut afterward. The app will report when existing target caches were kept rather than silently replacing them.

The new data folder is **%LOCALAPPDATA%\PlayDo\TokenOdometer\Desktop**. You can use **--data "D:\OdometerData"** to choose a separate writable folder. Keep this folder private. The original cache is not modified.

After verifying the new app, unpin the previous executable and pin the new running app through Windows. Its executable path and multi-file runtime must remain together. Closing the window keeps tracking in the tray; Exit stops collection.
