# Publishing a Windows release

Releases are portable ZIP files, not MSI installers. Publishing requires write access to the GitHub repository. Build and test before uploading anything.

1. Update `src/AssemblyInfo.cs`, the version in `package.ps1` and `tools/verify-package.ps1`, and the versioned artifact names in `.github/workflows/build.yml`. Add release notes to `CHANGELOG.md`. Update any versioned download links in the README and website when the asset filename changes.
2. On Windows 10/11 x64 with .NET Framework 4.8, close any app running from this checkout's `dist` folder and run:

   ```powershell
   .\package.ps1
   .\tools\verify-package.ps1
   ```

3. Check that Windows build and Project website workflows pass. Inspect the ZIP and its checksum. Do not add `data/`, diagnostic reports, real usage screenshots, authentication files or local notes to the repository or release.
4. Commit the reviewed source, create a version tag such as `v0.2.0`, and push the commit and tag. Create a GitHub release targeting that exact tag. Upload only `release/PlayDo-Token-Odometer-0.2.0-windows-x64.zip` and its `.zip.sha256` file. Mark stable releases as Latest so the download buttons resolve to them.
5. Download the public asset to a separate test folder, verify its SHA256, and confirm the extraction/run workflow. Check the README's download button and the project website.

Release notes should cover requirements, extraction, starting the app, taskbar pinning, new features, known limits and upgrade instructions. Describe the app as unsigned unless a real signing process has been used. Never invent a publisher signature, successful install, benchmark, review or rating.

The website is a static GitHub Pages deployment of `site/` plus public `assets/`. Its workflow does not publish runtime folders or the whole checkout. Configure Pages to use GitHub Actions. No API keys, analytics service or JavaScript build step is required.

The default download filename must match the published release asset exactly. GitHub's automatic Source code archives are useful to developers but do not contain the ready-to-run executable.
