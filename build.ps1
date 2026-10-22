$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
foreach ($folder in @('build','dist','assets','dist\assets')) { New-Item -ItemType Directory -Force -Path (Join-Path $root $folder) | Out-Null }
& $compiler /nologo /target:exe /r:System.Drawing.dll "/out:$root\build\BrandAssets.exe" (Join-Path $root 'tools\BrandAssets.cs')
if ($LASTEXITCODE -ne 0) { throw 'Brand asset build failed.' }
& (Join-Path $root 'build\BrandAssets.exe') (Join-Path $root 'assets')
if ($LASTEXITCODE -ne 0) { throw 'Brand asset generation failed.' }
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.Windows.Forms.dll','System.Drawing.dll',"$framework\WPF\WindowsBase.dll","$framework\WPF\PresentationCore.dll","$framework\WPF\PresentationFramework.dll") | ForEach-Object { '/r:' + $_ }
$sources = (Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs').FullName
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$root\assets\app.ico" "/win32manifest:$root\app.manifest" "/resource:$root\src\MainWindow.xaml,MainWindow.xaml" "/out:$root\dist\PlayDoTokenOdometer.exe" $references $sources
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
Copy-Item -LiteralPath (Join-Path $root 'assets\app-icon.png'),(Join-Path $root 'assets\app.ico'),(Join-Path $root 'assets\cover.png') -Destination (Join-Path $root 'dist\assets') -Force
Write-Output 'Built dist\PlayDoTokenOdometer.exe and branded PNG/ICO assets.'
