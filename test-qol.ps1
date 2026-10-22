$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force -Path (Join-Path $root 'build') | Out-Null
$sources = @('Usage.cs','LineCursor.cs','Store.cs','Collector.cs','ReplayDatabase.cs','Convenience.cs') |
    ForEach-Object { Join-Path $root ('src\' + $_) } |
    Where-Object { Test-Path -LiteralPath $_ }
$sources += Join-Path $root 'tests\ConvenienceTests.cs'
& $compiler /nologo /target:exe /optimize+ /r:System.Web.Extensions.dll /r:System.Core.dll "/out:$root\build\ConvenienceTests.exe" $sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $root 'build\ConvenienceTests.exe')
exit $LASTEXITCODE
