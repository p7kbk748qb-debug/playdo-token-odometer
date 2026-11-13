param([Parameter(Mandatory=$true)][ValidateSet('win-x64','osx-x64','osx-arm64')][string]$Runtime)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version='0.6.0'
$release=Join-Path $root 'release'
$stage=Join-Path $root ('build\portable-'+$Runtime+'-'+[Guid]::NewGuid().ToString('N'))
$publish=Join-Path $stage 'published'
New-Item -ItemType Directory -Path $release,$publish -Force | Out-Null
dotnet publish (Join-Path $PSScriptRoot 'Desktop\PlayDo.Odometer.Desktop.csproj') -c Release -r $Runtime --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o $publish
if($LASTEXITCODE){throw 'Portable publish failed'}
$public=@('LICENSE','README.md')
foreach($name in $public){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $publish}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MACOS-INSTALL.md'),(Join-Path $PSScriptRoot 'WINDOWS-INSTALL.md') -Destination $publish
if($Runtime -eq 'win-x64'){
 $archive=Join-Path $release "PlayDo-Token-Odometer-$version-$Runtime.zip"
 Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive -Force
}else{
 if(-not $IsMacOS){throw 'Build macOS app bundles on a macOS runner to retain executable permissions'}
 $bundle=Join-Path $stage "PlayDo's Token Odometer.app"
 $macos=Join-Path $bundle 'Contents/MacOS'
 $resources=Join-Path $bundle 'Contents/Resources'
 New-Item -ItemType Directory -Path $macos,$resources -Force | Out-Null
 Copy-Item -Path (Join-Path $publish '*') -Destination $macos -Recurse
 $plist=@"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>PlayDoTokenOdometer</string>
<key>CFBundleIdentifier</key><string>com.playdo.tokenodometer</string>
<key>CFBundleName</key><string>Token Odometer</string>
<key>CFBundleDisplayName</key><string>PlayDo's Token Odometer</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleVersion</key><string>$version</string>
<key>CFBundleShortVersionString</key><string>$version</string>
<key>NSHighResolutionCapable</key><true/>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>CFBundleIconFile</key><string>odometer.icns</string>
</dict></plist>
"@
 [IO.File]::WriteAllText((Join-Path $bundle 'Contents/Info.plist'),$plist)
 $iconset=Join-Path $stage 'odometer.iconset'
 New-Item -ItemType Directory -Path $iconset | Out-Null
 foreach($size in @(16,32,128,256,512)){
  & sips -z $size $size (Join-Path $root 'assets/app-icon.png') --out (Join-Path $iconset "icon_${size}x${size}.png") | Out-Null
  $retina=$size*2
  & sips -z $retina $retina (Join-Path $root 'assets/app-icon.png') --out (Join-Path $iconset "icon_${size}x${size}@2x.png") | Out-Null
 }
 & iconutil -c icns $iconset -o (Join-Path $resources 'odometer.icns')
 if($LASTEXITCODE){throw 'macOS icon conversion failed'}
 & chmod +x (Join-Path $macos 'PlayDoTokenOdometer')
 & codesign --force --deep --sign - $bundle
 if($LASTEXITCODE){throw 'Ad-hoc signing failed'}
 & codesign --verify --deep --strict $bundle
 if($LASTEXITCODE){throw 'Bundle signature verification failed'}
 $imageRoot=Join-Path $stage 'image'
 New-Item -ItemType Directory -Path $imageRoot | Out-Null
 Copy-Item -LiteralPath $bundle -Destination $imageRoot -Recurse
 & ln -s /Applications (Join-Path $imageRoot 'Applications')
 $archive=Join-Path $release "PlayDo-Token-Odometer-$version-$Runtime.dmg"
 & hdiutil create -volname "PlayDo Token Odometer" -srcfolder $imageRoot -ov -format UDZO $archive
 if($LASTEXITCODE){throw 'DMG creation failed'}
}
$hash=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($archive+'.sha256',$hash+'  '+[IO.Path]::GetFileName($archive)+"`n")
Write-Output "Packaged $archive"
