#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Focus = 'Review the current implementation against the agreed specification.',
    [string]$Model = 'sonnet',
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$output = Join-Path $root "artifacts\claude-reviews\$stamp"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$builder = [Text.StringBuilder]::new()
[void]$builder.AppendLine([IO.File]::ReadAllText((Join-Path $root 'review\claude-review-prompt.md')))
[void]$builder.AppendLine("`nReview focus: $Focus`n")
Push-Location $root
try {
    $tracked = @(git ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate tracked project files.' }
    # Explicit source selection excludes .tools, artifacts, credentials and binary assets.
    $files = @($tracked | Where-Object {
        $_ -in @('AGENTS.md','SPECIFICATION.md','README.md','PROGRESS.md','STORE-RELEASE-PLAN.md','STORE-LISTING-DRAFT.md','build.ps1','publish.ps1','sdk.ps1','build-installer.ps1','build-store-package.ps1','review-claude.ps1','review/README.md','review/claude-review-prompt.md') -or
        ($_ -match '^installer/' -and $_ -match '\.(iss|md|txt)$') -or
        ($_ -match '^(src|tests)/' -and $_ -match '\.(cs|xaml|csproj)$') -or
        $_ -eq '.github/workflows/windows-build.yml'
    })
    [void]$builder.AppendLine('Snapshot files are numbered from line 1. This snapshot includes current working-file changes, not only committed code.')
    foreach ($file in $files) {
        [void]$builder.AppendLine("`n--- FILE: $file ---")
        $lines = [IO.File]::ReadAllLines((Join-Path $root $file))
        for ($i = 0; $i -lt $lines.Length; $i++) { [void]$builder.AppendLine(('{0,5}: {1}' -f ($i + 1), $lines[$i])) }
    }
    $inputPath = Join-Path $output 'input.txt'
    [IO.File]::WriteAllText($inputPath, $builder.ToString(), [Text.UTF8Encoding]::new($false))
    $files | Set-Content -LiteralPath (Join-Path $output 'files.txt') -Encoding utf8
    Write-Host "Prepared $($files.Count) files for review: $inputPath"
    if ($PrepareOnly) { return }
    $claude = Join-Path $env:USERPROFILE '.local\bin\claude.exe'
    if (-not (Test-Path -LiteralPath $claude)) {
        $command = Get-Command claude -ErrorAction SilentlyContinue
        if ($null -eq $command) { throw 'Claude Code is not installed. See review/README.md.' }
        $claude = $command.Source
    }
    $emptyMcp = Join-Path $output 'mcp.json'
    [IO.File]::WriteAllText($emptyMcp, '{"mcpServers":{}}')
    $start = [Diagnostics.ProcessStartInfo]::new($claude)
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    $start.StandardOutputEncoding = [Text.Encoding]::UTF8
    # This workflow uses subscription authentication, never an inherited API key/provider.
    foreach ($name in @('ANTHROPIC_API_KEY','ANTHROPIC_AUTH_TOKEN','ANTHROPIC_BASE_URL','ANTHROPIC_CUSTOM_HEADERS','CLAUDE_CODE_USE_BEDROCK','CLAUDE_CODE_USE_VERTEX','CLAUDE_CODE_USE_FOUNDRY')) { [void]$start.Environment.Remove($name) }
    foreach ($argument in @('-p','--safe-mode','--model',$Model,'--tools','','--disallowedTools','mcp__*','--strict-mcp-config','--mcp-config',$emptyMcp,'--no-session-persistence','--output-format','text')) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $inputFailure = $null
        try { $process.StandardInput.Write($builder.ToString()) }
        catch { $inputFailure = $_ }
        finally { try { $process.StandardInput.Close() } catch { if ($null -eq $inputFailure) { $inputFailure = $_ } } }
        $process.WaitForExit()
        $result = $stdout.GetAwaiter().GetResult(); $errors = $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText((Join-Path $output 'diagnostics.txt'), $errors)
        if ($process.ExitCode -ne 0 -or $null -ne $inputFailure -or $result.TrimStart().StartsWith('API Error:', [StringComparison]::OrdinalIgnoreCase)) {
            [IO.File]::WriteAllText((Join-Path $output 'incomplete-review.txt'), $result)
            if (($result + $errors) -match 'ECONNREFUSED|Connection refused|ENOTFOUND|ETIMEDOUT|ECONNRESET|EAI_AGAIN|fetch failed') {
                throw "Claude review connection failed (exit $($process.ExitCode)). See $output. Check network access and proxy settings; if Claude auth status reports loggedIn, signing in again will not fix a blocked connection."
            }
            if ($null -ne $inputFailure) { throw "Claude review could not receive the source snapshot. See $output. Input failure: $inputFailure" }
            throw "Claude review failed (exit $($process.ExitCode)). See $output. Check Claude auth status and network access."
        }
        if ([string]::IsNullOrWhiteSpace($result)) { throw 'Claude returned no review.' }
        [IO.File]::WriteAllText((Join-Path $output 'review.md'), $result, [Text.UTF8Encoding]::new($false))
        Write-Host "Claude review saved: $(Join-Path $output 'review.md')"
    } finally { $process.Dispose() }
} finally { Pop-Location }
