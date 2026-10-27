[CmdletBinding()]
param([switch]$SkipPublish)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not $SkipPublish) { & (Join-Path $PSScriptRoot 'publish.ps1') }
    $published = Join-Path $PSScriptRoot 'artifacts\release\TommysTimeTrackingTables'
    foreach ($file in @('TimersApp.exe','TimersApp.dll','coreclr.dll','PresentationFramework.dll','README.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $published $file))) { throw "Missing published file: $file. Run publish.ps1 first." }
    }
    $candidates = @(
        (Join-Path $PSScriptRoot '.tools\inno\ISCC.exe'),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $compiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $compiler) {
        $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($command) { $compiler = $command.Source }
    }
    if (-not $compiler) { throw 'Install Inno Setup 6 or place its compiler under .tools/inno, then run this script again.' }
    & $compiler 'installer\TimersApp.iss'
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    $setup = Join-Path $PSScriptRoot 'artifacts\installer\Tommys-Time-Tracking-Tables-Setup-0.3.0.exe'
    if (-not (Test-Path -LiteralPath $setup)) { throw 'Installer output missing.' }
    $hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
    "$hash  $([IO.Path]::GetFileName($setup))" | Set-Content -LiteralPath "$setup.sha256" -Encoding ascii
    Write-Host "Installer ready: $setup"
} finally { Pop-Location }
