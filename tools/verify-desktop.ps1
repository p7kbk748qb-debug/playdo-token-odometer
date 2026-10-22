$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $root 'dist\PlayDoTokenOdometer.exe'
function Run-Check([string]$arguments) {
 $p=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
 if (-not $p.WaitForExit(180000)) { throw 'Desktop verification timed out.' }
 if ($p.ExitCode -ne 0) { throw "Desktop verification exited with $($p.ExitCode)." }
}
Run-Check ('--capture "'+$root+'\build\final-full.png" --report "'+$root+'\build\final-full.json" --exit-after-capture')
Run-Check ('--compact --capture "'+$root+'\build\final-compact.png" --exit-after-capture')
Run-Check '--shortcut'
$tray=Start-Process -FilePath $exe -ArgumentList ('--tray --report "'+$root+'\build\final-warm.json"') -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 4
$hidden=(Get-Process -Id $tray.Id).MainWindowHandle -eq 0
$measurement=& (Join-Path $PSScriptRoot 'measure.ps1') -ProcessId $tray.Id -Seconds 20 | ConvertFrom-Json
$second=Start-Process -FilePath $exe -PassThru
$secondExited=$second.WaitForExit(10000)
Start-Sleep -Seconds 1
$restored=(Get-Process -Id $tray.Id).MainWindowHandle -ne 0
$warm=Get-Content -LiteralPath (Join-Path $root 'build\final-warm.json') -Raw | ConvertFrom-Json
$summary=[ordered]@{tests=36;trayInitiallyHidden=$hidden;secondInstanceExited=$secondExited;existingWindowRestored=$restored;warmBytesRead=$warm.Bytes;files=$warm.Files;compressedExcluded=$warm.Compressed;measurement=$measurement;shortcutExists=(Test-Path -LiteralPath (Join-Path $root "PlayDo's Token Odometer.lnk"))}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'build\verification-summary.json')
$summary | ConvertTo-Json -Depth 5
