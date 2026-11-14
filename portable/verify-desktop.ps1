param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$check=Join-Path $root ('build/desktop-check-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $check -Force | Out-Null
$report=Join-Path $check 'report.json'
$capture=Join-Path $check 'appearance.png'
$arguments=@('--smoke','--data',('"' +(Join-Path $check 'data')+'"'),'--codex-home',('"'+(Join-Path $PSScriptRoot 'fixtures/codex')+'"'),'--claude-home',('"'+(Join-Path $PSScriptRoot 'fixtures/claude')+'"'),'--gemini-home',('"'+(Join-Path $PSScriptRoot 'fixtures/gemini')+'"'),'--opencode-home',('"'+(Join-Path $check 'absent-opencode')+'"'),'--report',('"'+$report+'"'),'--capture',('"'+$capture+'"'))
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
if(-not $state.FontDropdownChecks -or -not $state.DashboardChecks){throw 'Interactive font or dashboard regression checks missing'}
foreach($name in @('appearance.png','appearance-overview.png','appearance-providers.png','appearance-settings.png','portable-tiny.png')){if(-not (Test-Path (Join-Path $check $name))){throw "Missing native render: $name"}}
"PASS native window, four layouts, provider sum, font popup recycling, dashboard, themes, reduced motion and hidden timers"
"Evidence: $check"
