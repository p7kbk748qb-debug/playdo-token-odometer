$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force -Path (Join-Path $root 'build') | Out-Null
$sources = @((Join-Path $root 'src\Usage.cs'), (Join-Path $root 'src\LineCursor.cs'))
foreach ($name in @('Store.cs','Collector.cs','ReplayDatabase.cs')) { $file = Join-Path $root ('src\' + $name); if (Test-Path -LiteralPath $file) { $sources += $file } }
$sources += Join-Path $root 'tests\Tests.cs'
& $compiler /nologo /target:exe /optimize+ /r:System.Web.Extensions.dll /r:System.Core.dll "/out:$root\build\Tests.exe" $sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $root 'build\Tests.exe')
exit $LASTEXITCODE
