# Install on macOS

1. Choose the **osx-arm64** download for an Apple Silicon Mac, or **osx-x64** for an Intel Mac.
2. Open the disk image and drag **PlayDo’s Token Odometer.app** to Applications.
3. Open the app and select the tools you use on the Providers page. No account key is required.

The application bundles its runtime. You do not need to install .NET. macOS 15 or later is the distribution target.

This community build is ad-hoc signed, not Apple notarized. macOS may prevent the first launch. Review the release/source, then use the per-app **Open Anyway** option in System Settings → Privacy & Security if you decide to run it. Do not disable Gatekeeper globally.

The app stores local aggregate usage and preferences in **~/Library/Application Support/PlayDo Token Odometer**. Closing the window keeps collection in the menu bar. Choose Exit in the menu-bar menu to stop it.

Codex, Claude Code and Gemini CLI default to their dot-folders in your home directory. OpenCode defaults to **~/.local/share/opencode** or **$XDG_DATA_HOME/opencode**. If you changed a tool’s location, choose its actual data folder on Providers. Some protected folders may require the access macOS normally asks for; the application does not require full-disk access to read the default user-owned folders.

Counts come from available local usage reports. Web-only conversations, missing archives and account credits are not inferred.
