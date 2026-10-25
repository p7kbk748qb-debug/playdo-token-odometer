param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$version = '0.3.1'

function Invoke-ReleaseCheck([string]$RelativePath) {
    & (Join-Path $root $RelativePath)
    if ($LASTEXITCODE -ne 0) { throw "Release check failed: $RelativePath" }
}

if (-not $SkipBuild) {
    Invoke-ReleaseCheck 'build.ps1'
    Invoke-ReleaseCheck 'test.ps1'
    Invoke-ReleaseCheck 'tools\verify-appearance.ps1'
    if (Test-Path -LiteralPath (Join-Path $root 'test-qol.ps1')) {
        Invoke-ReleaseCheck 'test-qol.ps1'
    }
}

# Copy individual public files only. Never package the dist, data or checkout tree.
$files = [ordered]@{
    'PlayDoTokenOdometer.exe' = 'dist\PlayDoTokenOdometer.exe'
    'assets/app-icon.png' = 'dist\assets\app-icon.png'
    'assets/app.ico' = 'dist\assets\app.ico'
    'assets/cover.png' = 'dist\assets\cover.png'
    'README.md' = 'README.md'
    'LICENSE' = 'LICENSE'
    'CHANGELOG.md' = 'CHANGELOG.md'
    'docs/INSTALL.md' = 'docs\INSTALL.md'
}

foreach ($relative in $files.Values) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        throw "Required release file is missing: $relative"
    }
}
$binary = Join-Path $root 'dist\PlayDoTokenOdometer.exe'
$metadata = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($binary)
if ($metadata.FileVersion -ne "$version.0") {
    throw "Expected binary version $version.0, found '$($metadata.FileVersion)'. Run build.ps1 first."
}

$staging = Join-Path $root ('build\package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging -Force | Out-Null
foreach ($entry in $files.GetEnumerator()) {
    $destination = Join-Path $staging $entry.Key.Replace('/', [IO.Path]::DirectorySeparatorChar)
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root $entry.Value) -Destination $destination
}

$releaseDir = Join-Path $root 'release'
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
$archiveName = "PlayDo-Token-Odometer-$version-windows-x64.zip"
$archive = Join-Path $releaseDir $archiveName
$temporaryArchive = Join-Path $releaseDir ([Guid]::NewGuid().ToString('N') + '.zip')
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $temporaryArchive -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
try {
    $expected = @($files.Keys | Sort-Object)
    $actual = @($zip.Entries | Where-Object { $_.Name -ne '' } | ForEach-Object { $_.FullName.Replace('\', '/') } | Sort-Object)
    if ($actual.Count -ne $expected.Count -or (Compare-Object $expected $actual)) {
        throw 'Release archive did not match the explicit public-file whitelist.'
    }
} finally {
    $zip.Dispose()
}

Move-Item -LiteralPath $temporaryArchive -Destination $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = $archive + '.sha256'
Set-Content -LiteralPath $checksum -Value ($hash + ' *' + $archiveName) -Encoding ASCII
Write-Output "Packaged $archive"
Write-Output "SHA256 $checksum"
Write-Output "Verified $($files.Count) public files. Personal usage, settings and diagnostics are excluded."
