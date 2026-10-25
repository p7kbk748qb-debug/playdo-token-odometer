param([string]$Package)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Package) { $Package = Join-Path $root 'release\PlayDo-Token-Odometer-0.3.1-windows-x64.zip' }
$Package = (Resolve-Path -LiteralPath $Package).Path
$expectedHash = ((Get-Content -LiteralPath ($Package + '.sha256') -Raw).Trim() -split '\s+')[0]
$actualHash = (Get-FileHash -LiteralPath $Package -Algorithm SHA256).Hash
if ($actualHash -ne $expectedHash) { throw 'Package checksum does not match.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$expected = @('PlayDoTokenOdometer.exe','assets/app-icon.png','assets/app.ico','assets/cover.png','README.md','LICENSE','CHANGELOG.md','docs/INSTALL.md') | Sort-Object
$zip = [IO.Compression.ZipFile]::OpenRead($Package)
try {
    $entries = @($zip.Entries | Where-Object { $_.Name -ne '' } | ForEach-Object { $_.FullName.Replace('\','/') } | Sort-Object)
    if ($entries.Count -ne $expected.Count -or (Compare-Object $expected $entries)) { throw 'Unexpected portable package contents.' }
} finally { $zip.Dispose() }

$testRoot = Join-Path $root ('build\portable-check-' + [Guid]::NewGuid().ToString('N'))
$portable = Join-Path $testRoot 'dist'
$fixtureHome = Join-Path $testRoot 'codex-home'
New-Item -ItemType Directory -Path $portable,(Join-Path $fixtureHome 'sessions'),(Join-Path $testRoot 'src') -Force | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory($Package,$portable)
$at = [DateTime]::UtcNow.ToString('O')
$records = @(
    @{type='turn_context';timestamp=$at;payload=@{model='portable-fixture-model'}}
    @{type='event_msg';timestamp=$at;payload=@{type='token_count';info=@{
        total_token_usage=@{input_tokens=12345;cached_input_tokens=9000;output_tokens=678}
        last_token_usage=@{input_tokens=12345;cached_input_tokens=9000;output_tokens=678}
    }}}
)
$fixture = ($records | ForEach-Object { ConvertTo-Json -InputObject $_ -Depth 8 -Compress }) -join "`n"
[IO.File]::WriteAllText((Join-Path $fixtureHome 'sessions\fixture.jsonl'),$fixture + "`n",(New-Object Text.UTF8Encoding($false)))
$exe = Join-Path $portable 'PlayDoTokenOdometer.exe'
$report = Join-Path $testRoot 'snapshot.json'
$capture = Join-Path $testRoot 'portable.png'
$arguments = '--isolated --codex-home "' + $fixtureHome + '" --report "' + $report + '" --capture "' + $capture + '" --exit-after-capture'
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(30000)) {
    Stop-Process -Id $process.Id
    throw 'The isolated portable smoke test did not exit in 30 seconds.'
}
if ($process.ExitCode -ne 0) { throw "Portable app exited with code $($process.ExitCode)." }
$snapshot = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if ($snapshot.Recorded.Input -ne 12345 -or $snapshot.Recorded.Output -ne 678 -or $snapshot.Recorded.Cached -ne 9000) { throw 'Portable app did not import the synthetic usage correctly.' }
if ($snapshot.Models.Count -ne 1 -or $snapshot.Models[0].Label -ne 'portable-fixture-model') { throw 'Portable model breakdown is incorrect.' }
if ($snapshot.PSObject.Properties.Name -contains 'Projects') { throw 'Portable app includes project tracking.' }
if (-not (Test-Path -LiteralPath (Join-Path $portable 'data\live\usage-v3.db')) -or -not (Test-Path -LiteralPath $capture)) { throw 'Portable default data path or native capture failed.' }

$shortcutProcess = Start-Process -FilePath $exe -ArgumentList '--shortcut' -WindowStyle Hidden -PassThru
if (-not $shortcutProcess.WaitForExit(15000)) { Stop-Process -Id $shortcutProcess.Id; throw 'Portable shortcut creation timed out.' }
if ($shortcutProcess.ExitCode -ne 0) { throw 'Portable shortcut creation failed.' }
$shell = New-Object -ComObject WScript.Shell
$link = $null
try {
    $link = $shell.CreateShortcut((Join-Path $portable "PlayDo's Token Odometer.lnk"))
    if ($link.TargetPath -ne $exe -or $link.WorkingDirectory -ne $portable) { throw 'Portable shortcut points to the wrong executable.' }
} finally {
    if ($link) { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($link) | Out-Null }
    [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) | Out-Null
}
Write-Output 'PASS portable checksum and exact 8-file whitelist'
Write-Output 'PASS extracted app, default data path, synthetic usage, model breakdown and native render'
Write-Output 'PASS extracted app local launcher target and working directory'
Write-Output "Evidence: $testRoot"
