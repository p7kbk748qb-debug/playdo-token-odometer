param([switch]$SettingsOnly,[string]$ExecutablePath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $ExecutablePath){$ExecutablePath=Join-Path $root 'dist\PlayDoTokenOdometer.exe'}
$ExecutablePath=(Resolve-Path -LiteralPath $ExecutablePath).Path
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$refs=@('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll',"$framework\WPF\WindowsBase.dll","$framework\WPF\PresentationCore.dll","$framework\WPF\PresentationFramework.dll",$ExecutablePath) | ForEach-Object {'/r:'+$_}
& $compiler /nologo /target:exe /platform:x64 /optimize+ "/out:$root\build\AppearanceTests.exe" $refs (Join-Path $root 'tests\AppearanceTests.cs')
if($LASTEXITCODE -ne 0){throw 'Appearance check compilation failed.'}
$checkArgs=@($root,'--all',$ExecutablePath)
if($SettingsOnly){$checkArgs[1]='--settings-only'}
& (Join-Path $root 'build\AppearanceTests.exe') $checkArgs
if($LASTEXITCODE -ne 0){throw 'Appearance verification failed.'}
