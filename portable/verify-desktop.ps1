param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$check=Join-Path $root ('build/desktop-check-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $check -Force | Out-Null
$fixtureRoot=Join-Path $check 'fixtures'
& (Join-Path $PSScriptRoot 'New-DesktopFixtures.ps1') -Destination $fixtureRoot
$report=Join-Path $check 'report.json'
$capture=Join-Path $check 'appearance.png'
# Start with a pre-studio settings file and explicit synthetic provider selections.
$data=Join-Path $check 'data'
New-Item -ItemType Directory -Path $data | Out-Null
@{
 Theme='Instrument'; Font='Inter'; NumberFont='Inter'; Layout='Dashboard'; Scope='Recorded'
 Transparent=$true; Corner='Free'; Animate=$false; Effects=$false; ReduceMotion=$true
 ShowAllowance=$false; Paused=$false; Topmost=$false
 Enabled=@{Codex=$true;'Claude Code'=$true;'Gemini CLI'=$true;OpenCode=$false}
 Mini=@{ShowPrimary=$false;ShowWeekly=$true;ShowStatus=$false}
 Compact=@{ShowModel=$true;ShowReset=$true;ShowBreakdown=$false}
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $data 'preferences.json')
$arguments=@('--smoke','--verify-legacy-settings','--data',('"' +(Join-Path $check 'data')+'"'),'--codex-home',('"'+(Join-Path $fixtureRoot 'codex')+'"'),'--claude-home',('"'+(Join-Path $fixtureRoot 'claude')+'"'),'--gemini-home',('"'+(Join-Path $fixtureRoot 'gemini')+'"'),'--opencode-home',('"'+(Join-Path $check 'absent-opencode')+'"'),'--report',('"'+$report+'"'),'--capture',('"'+$capture+'"'))
$p=Start-Process -FilePath $Executable -ArgumentList $arguments -PassThru -RedirectStandardError (Join-Path $check 'stderr.txt') -RedirectStandardOutput (Join-Path $check 'stdout.txt')
if(-not $p.WaitForExit(45000)){
 Stop-Process -Id $p.Id
 throw 'Native desktop check timed out'
}
if($p.ExitCode -ne 0 -or -not (Test-Path $report)){
 Get-Content (Join-Path $check 'stderr.txt')
 throw 'Native desktop check failed'
}
$state=Get-Content $report -Raw | ConvertFrom-Json
if($state.Total -ne 397766182 -or $state.Fonts -lt 1 -or $state.Providers -ne 4 -or -not $state.WindowReady){throw 'Native desktop report did not match synthetic inputs'}
if(-not $state.AppearanceStudioChecks){throw 'Appearance Studio checks missing'}
if(-not $state.WidgetBackgroundChecks){throw 'Widget background mode checks missing'}
if(-not $state.WindowTransparencyChecks){throw 'Window-layer transparency checks missing'}
if(-not $state.FontDropdownChecks -or -not $state.DashboardChecks){throw 'Interactive font or dashboard regression checks missing'}
foreach($name in @('appearance.png','appearance-overview.png','appearance-studio-look.png','appearance-studio-ink.png','appearance-providers.png','appearance-settings.png','portable-tiny.png','window-transparent.png','widget-glass.png')){if(-not (Test-Path (Join-Path $check $name))){throw "Missing native render: $name"}}
"PASS native window, four layouts, provider sum, font popup recycling, dashboard, saved looks, counter ink, legacy settings, reduced motion and hidden timers"
"Evidence: $check"
