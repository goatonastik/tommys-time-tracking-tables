. (Join-Path $PSScriptRoot 'sdk.ps1')
$output = Join-Path $PSScriptRoot 'artifacts\release\TommysTimeTrackingTables'
Push-Location $PSScriptRoot
try {
    $releaseRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'artifacts\release'))
    $outputPath = [IO.Path]::GetFullPath($output)
    $expected = [IO.Path]::Combine($releaseRoot,'TommysTimeTrackingTables')
    if (-not [string]::Equals($outputPath,$expected,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to clear an unexpected publish path.'
    }
    $zipPath = [IO.Path]::GetFullPath((Join-Path $releaseRoot 'Tommys-Time-Tracking-Tables-win-x64.zip'))
    $expectedZip = [IO.Path]::Combine($releaseRoot,'Tommys-Time-Tracking-Tables-win-x64.zip')
    if (-not [string]::Equals($zipPath,$expectedZip,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to clear an unexpected package path.'
    }
    if (Test-Path -LiteralPath $outputPath) {
        $existing = Get-Item -LiteralPath $outputPath -Force
        if ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing to clear a linked publish folder.' }
        Remove-Item -LiteralPath $outputPath -Recurse -Force
    }
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    & $sdk publish 'src\TimersApp.Desktop\TimersApp.Desktop.csproj' -c Release -r win-x64 --self-contained true -o $output --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item -LiteralPath 'README.md' -Destination $output -Force
    Copy-Item -LiteralPath 'SPECIFICATION.md' -Destination $output -Force
    $sdkRoot = Split-Path -Parent $sdk
    foreach ($notice in @('LICENSE.txt','ThirdPartyNotices.txt')) {
        $source = Join-Path $sdkRoot $notice
        if (-not (Test-Path -LiteralPath $source)) { throw "Bundled .NET notice missing: $source" }
        Copy-Item -LiteralPath $source -Destination $output -Force
    }
    $notices = Join-Path $output 'ThirdPartyLicenses'
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    Copy-Item -LiteralPath 'ThirdPartyLicenses/NAudio.txt','ThirdPartyLicenses/NLayer.txt' -Destination $notices -Force
    $assets = Get-Content 'src\TimersApp.Desktop\obj\project.assets.json' -Raw | ConvertFrom-Json
    foreach ($library in $assets.libraries.PSObject.Properties) {
        if ($library.Value.type -ne 'package') { continue }
        $package = Join-Path $env:NUGET_PACKAGES $library.Value.path
        $destination = Join-Path $notices ($library.Name.Replace('/', '-'))
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object { $_.Name -match '^(license|copying|notice|third.party)' -or $_.Extension -eq '.nuspec' } | Copy-Item -Destination $destination -Force
    }
    Compress-Archive -LiteralPath $output -DestinationPath $zipPath
} finally { Pop-Location }
