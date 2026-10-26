. (Join-Path $PSScriptRoot 'sdk.ps1')
Push-Location $PSScriptRoot
try {
    & $sdk build 'tests\TimersApp.Tests\TimersApp.Tests.csproj' -c Release --configfile 'NuGet.Config'
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & $sdk 'tests\TimersApp.Tests\bin\Release\net10.0-windows\TimersApp.Tests.dll'
    if ($LASTEXITCODE -ne 0) { throw 'Verification failed.' }
} finally { Pop-Location }
