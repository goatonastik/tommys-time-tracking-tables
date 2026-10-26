$ErrorActionPreference = 'Stop'
# Prefer the workspace SDK, but support a fresh checkout with an installed .NET 10 SDK.
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.tools\cli'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.tools\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$sdk = Join-Path $PSScriptRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $sdk)) {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $command) { throw 'Install the .NET 10 SDK, then run this script again.' }
    $sdk = $command.Source
}
