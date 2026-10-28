[CmdletBinding()]
param(
    [string]$IdentityName,
    [string]$Publisher,
    [string]$PublisherDisplayName,
    [string]$DisplayName,
    [switch]$DevelopmentIdentity
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if ($DevelopmentIdentity) {
        if ($IdentityName -or $Publisher -or $PublisherDisplayName -or $DisplayName) { throw 'Do not mix -DevelopmentIdentity with Partner Center identity values.' }
        $IdentityName = 'Goatonastick.TommysTimeTrackingTables.Dev'
        $Publisher = 'CN=Goatonastick Developer Test'
        $PublisherDisplayName = 'Goatonastick Developer Test'
        $DisplayName = "Tommy's Time Tracking Tables (dev)"
    } elseif (-not $IdentityName -or -not $Publisher -or -not $PublisherDisplayName -or -not $DisplayName) {
        throw 'Provide the exact Identity name, Publisher, Publisher display name and Display name from Partner Center.'
    }
    if ($IdentityName -notmatch '^[A-Za-z0-9][A-Za-z0-9.-]{2,49}$' -or $IdentityName.EndsWith('.')) { throw 'Invalid MSIX Identity name.' }
    if ($Publisher -notmatch '^CN=(?:"[^"]+"|[^,]+)(?:,|$)') { throw 'Publisher must be the Partner Center distinguished name.' }
    if (-not $DevelopmentIdentity -and ($IdentityName.EndsWith('.Dev',[StringComparison]::OrdinalIgnoreCase) -or $Publisher -match 'Developer Test' -or $PublisherDisplayName -match 'Developer Test' -or $DisplayName -match '\(dev\)')) {
        throw 'Development identity cannot be used for a Store package.'
    }

    $published = Join-Path $PSScriptRoot 'artifacts\release\TommysTimeTrackingTables'
    foreach ($file in @('TimersApp.exe','TimersApp.dll','coreclr.dll','PresentationFramework.dll','README.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $published $file))) {
            throw "Published file missing: $file. Run publish.ps1 first."
        }
    }
    $project = [xml][IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src\TimersApp.Desktop\TimersApp.Desktop.csproj'))
    $versionText = $project.Project.PropertyGroup | ForEach-Object { $_.Version } |
        Where-Object { $_ } | Select-Object -First 1
    if (-not $versionText) { throw 'Desktop project version missing.' }
    $version = [version]$versionText.Trim()
    # Store package versions require a nonzero first field. Offset the app major
    # version consistently so preview 0.5.1 becomes package 1.5.1.0.
    if ($version.Major -ge 65535 -or $version.Minor -gt 65535 -or $version.Build -lt 0 -or $version.Build -gt 65535) {
        throw 'App version exceeds Store package version limits.'
    }
    $packageVersion = '{0}.{1}.{2}.0' -f ($version.Major+1),$version.Minor,$version.Build
    $fileVersion = (Get-Item -LiteralPath (Join-Path $published 'TimersApp.exe')).VersionInfo.FileVersion
    if ($fileVersion -ne ('{0}.{1}.{2}.0' -f $version.Major,$version.Minor,$version.Build)) {
        throw "Published executable version $fileVersion does not match project version $versionText. Run publish.ps1 first."
    }
    $sdkRoot = 'C:\Program Files (x86)\Windows Kits\10\bin'
    $makeappx = Get-ChildItem -LiteralPath $sdkRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^10\.0\.\d+\.0$' } |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName 'x64\makeappx.exe' } |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $makeappx) { throw 'Windows SDK MakeAppx.exe is required.' }

    $storeDir = Join-Path $PSScriptRoot 'artifacts\store'
    New-Item -ItemType Directory -Force -Path $storeDir | Out-Null
    $stage = Join-Path $storeDir ('stage-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    $publishedPrefix = [IO.Path]::GetFullPath($published).TrimEnd('\') + '\'
    foreach ($item in Get-ChildItem -LiteralPath $published -File -Recurse -Force) {
        if ($item.Extension -eq '.pdb') { continue }
        $relative = $item.FullName.Substring($publishedPrefix.Length)
        $destination = Join-Path $stage $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
        Copy-Item -LiteralPath $item.FullName -Destination $destination
    }

    # The existing T4 icon is the single source for the package's square assets.
    Add-Type -AssemblyName System.Drawing
    $iconBytes = [IO.File]::ReadAllBytes((Join-Path $PSScriptRoot 'src\TimersApp.Desktop\Resources\T4.ico'))
    $frames = [BitConverter]::ToUInt16($iconBytes,4)
    $frameOffset = -1; $frameLength = 0
    for ($i=0; $i -lt $frames; $i++) {
        $entry = 6 + 16*$i
        if ($iconBytes[$entry] -eq 0 -and $iconBytes[$entry+1] -eq 0) {
            $frameLength = [int][BitConverter]::ToUInt32($iconBytes,$entry+8)
            $frameOffset = [int][BitConverter]::ToUInt32($iconBytes,$entry+12)
            break
        }
    }
    if ($frameOffset -lt 0) { throw 'T4.ico has no 256-pixel frame.' }
    $stream = [IO.MemoryStream]::new($iconBytes,$frameOffset,$frameLength)
    try {
        $source = [Drawing.Bitmap]::new($stream)
        try {
            $assets = Join-Path $stage 'StoreAssets'
            New-Item -ItemType Directory -Path $assets | Out-Null
            foreach ($size in @(44,50,150)) {
                $image = [Drawing.Bitmap]::new($size,$size)
                $canvas = [Drawing.Graphics]::FromImage($image)
                try {
                    $canvas.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $canvas.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $canvas.DrawImage($source,0,0,$size,$size)
                    $image.Save((Join-Path $assets "T4-$size.png"),[Drawing.Imaging.ImageFormat]::Png)
                } finally { $canvas.Dispose(); $image.Dispose() }
            }
        } finally { $source.Dispose() }
    } finally { $stream.Dispose() }

    $safeName = [Security.SecurityElement]::Escape($IdentityName)
    $safePublisher = [Security.SecurityElement]::Escape($Publisher)
    $safeDisplay = [Security.SecurityElement]::Escape($PublisherDisplayName)
    $safeAppDisplay = [Security.SecurityElement]::Escape($DisplayName)
    $manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Identity Name="$safeName" Version="$packageVersion" Publisher="$safePublisher" ProcessorArchitecture="x64" />
  <Properties>
    <DisplayName>$safeAppDisplay</DisplayName>
    <PublisherDisplayName>$safeDisplay</PublisherDisplayName>
    <Description>Multiple customizable time trackers with local history and spreadsheet export.</Description>
    <Logo>StoreAssets\T4-50.png</Logo>
  </Properties>
  <Resources><Resource Language="en-us" /></Resources>
  <Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.22000.0" MaxVersionTested="10.0.26100.0" /></Dependencies>
  <Capabilities><rescap:Capability Name="runFullTrust" /></Capabilities>
  <Applications>
    <Application Id="TommysTimeTrackingTables" Executable="TimersApp.exe"
                 uap10:RuntimeBehavior="packagedClassicApp" uap10:TrustLevel="mediumIL">
      <uap:VisualElements DisplayName="$safeAppDisplay"
                          Description="Multiple customizable time trackers with spreadsheet export"
                          Square150x150Logo="StoreAssets\T4-150.png" Square44x44Logo="StoreAssets\T4-44.png"
                          BackgroundColor="#53D9B0" />
    </Application>
  </Applications>
</Package>
"@
    $manifestPath = Join-Path $stage 'AppxManifest.xml'
    [IO.File]::WriteAllText($manifestPath,$manifest,[Text.UTF8Encoding]::new($false))
    $suffix = if ($DevelopmentIdentity) { '-dev' } else { '-store' }
    $packagePath = Join-Path $storeDir ("Tommys-Time-Tracking-Tables-$packageVersion$suffix.msix")
    & $makeappx pack /d $stage /p $packagePath /o /h SHA256
    if ($LASTEXITCODE -ne 0) { throw 'MSIX packaging failed.' }
    $hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    "$hash  $([IO.Path]::GetFileName($packagePath))" | Set-Content -LiteralPath "$packagePath.sha256" -Encoding ascii
    Write-Host "MSIX ready: $packagePath"
    Write-Host "SHA256: $hash"
    if ($DevelopmentIdentity) { Write-Host 'Development identity only: this unsigned package is not Store-submittable.' }
    else { Write-Host 'Unsigned Store submission package: upload only through Partner Center; do not sideload.' }
} finally {
    try {
        if ($stage -and (Test-Path -LiteralPath $stage)) {
            $storePrefix = [IO.Path]::GetFullPath($storeDir).TrimEnd('\') + '\'
            $stagePath = [IO.Path]::GetFullPath($stage)
            if (-not $stagePath.StartsWith($storePrefix,[StringComparison]::OrdinalIgnoreCase)) {
                throw 'Refusing to remove a staging path outside artifacts/store.'
            }
            Remove-Item -LiteralPath $stagePath -Recurse -Force
        }
    } catch { Write-Warning "Could not clean MSIX staging folder: $($_.Exception.Message)" }
    finally { Pop-Location }
}
