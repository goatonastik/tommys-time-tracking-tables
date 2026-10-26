. (Join-Path $PSScriptRoot 'sdk.ps1')
$output = Join-Path $PSScriptRoot 'artifacts\release\TommysTimeTrackingTables'
Push-Location $PSScriptRoot
try {
    & $sdk publish 'src\TimersApp.Desktop\TimersApp.Desktop.csproj' -c Release -r win-x64 --self-contained true -o $output --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item -LiteralPath 'README.md' -Destination $output -Force
    Copy-Item -LiteralPath 'SPECIFICATION.md' -Destination $output -Force
    $notices = Join-Path $output 'ThirdPartyLicenses'
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    $assets = Get-Content 'src\TimersApp.Desktop\obj\project.assets.json' -Raw | ConvertFrom-Json
    foreach ($library in $assets.libraries.PSObject.Properties) {
        if ($library.Value.type -ne 'package') { continue }
        $package = Join-Path $env:NUGET_PACKAGES $library.Value.path
        $destination = Join-Path $notices ($library.Name.Replace('/', '-'))
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object { $_.Name -match '^(license|copying|notice|third.party)' -or $_.Extension -eq '.nuspec' } | Copy-Item -Destination $destination -Force
    }
    Compress-Archive -LiteralPath $output -DestinationPath 'artifacts\release\Tommys-Time-Tracking-Tables-win-x64.zip' -Force
} finally { Pop-Location }
