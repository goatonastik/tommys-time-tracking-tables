# Development, verification and release runbook

## Resume safely

1. Read AGENTS and START-HERE. Inspect `git status`, recent commits, csproj version and current remote main/release before editing. Keep user changes.
2. Work on Windows with PowerShell and .NET 10 SDK. `sdk.ps1` prefers the ignored project-local SDK under `.tools`, otherwise an installed SDK. Use PowerShell 7 for the Claude script. Restore requires network when packages are missing.
3. Never use `%LOCALAPPDATA%\TimersApp` for tests, or install/uninstall over the user's normal installation. The existing installation has historically differed from release source; verify rather than assume its version.

For an isolated manual app launch in the same PowerShell process:

```powershell
$env:TIMERSAPP_DATA_DIR = Join-Path $PWD ('artifacts/manual-' + [guid]::NewGuid().ToString('N'))
# Launch the desired development/published executable from this process.
# Close it and inspect only the isolated files, then clear the override:
Remove-Item Env:TIMERSAPP_DATA_DIR
```

The portable executable uses normal app data unless explicitly overridden. Portable packaging alone does not isolate data.

## Change and verify

Run `./build.ps1`: it builds the custom tests executable in Release and runs it. Do not substitute `dotnet test` and assume the checks ran. Tests create isolated data under artifacts. For meaningful changes, reproduce the issue, add a focused regression where useful, run relevant verification, and inspect actual output. Layout renders appear under `artifacts/tests/`.

For substantive code changes, stage new source paths so the reviewer sees them, then use `./review-claude.ps1`. Read `review/README.md`. Claude receives a numbered tracked-source snapshot with action tools disabled. Do not enable bypass permissions, shell, MCP, edits or implementation delegation. If outbound access is denied, use permitted network execution while preserving read-only flags. Re-authenticate via Claude's own login on a new computer; never copy tokens into project files.

Check findings against source and meaningful tests; record false positives and uncertainty. Rerun review after material fixes. The wrapper uses an explicit allowlist: the new docs are not automatically included. Read them directly for documentation checks; do not claim Claude reviewed them unless its snapshot contains them.

## Package and publish an application update

1. Update source version deliberately and document changed behavior. Complete build/tests and Claude review.
2. Run `./publish.ps1` for a self-contained win-x64 directory and ZIP under `artifacts/release`. It rebuilds that output and includes required notices. Test the exact packaged executable with isolated data.
3. Install Inno Setup 6 if needed; run `./build-installer.ps1`. `-SkipPublish` is safe only when the current published folder was already verified. Installer/checksum output is under `artifacts/installer`.
4. Test installation on a disposable Windows 11 machine/VM, not the user's real installed copy. Verify upgrade/uninstall/data retention separately when authorized.
5. Review dependency advisories and scan exact artifacts when available. Record failures to reach feeds honestly. These checks are not certifications.
6. Preserve both local and remote Git histories. Local authentication may be unavailable; the GitHub integration has supported tree/commit/ref uploads. Base remote changes on the inspected remote head, upload only intended changed paths, and advance with a non-force update and expected-head check. Do not replace remote main with the unrelated local history. If another change arrives, inspect/reconcile first.
7. Create the release/tag from the verified remote source. Upload installer, portable ZIP and SHA-256 files. Verify downloaded asset size/digest against local output and inspect the tag/source and CI status. A source push alone does not publish an installer.
8. Update START-HERE, CHECKPOINT, PROGRESS and evidence index with source IDs, checks, asset hashes, review limits and remaining work.

`.github/workflows/windows-build.yml` builds/tests and packages portable artifacts on main/PR/manual dispatch. It does not sign installers or automatically publish a GitHub Release.

## Signing and Store: paused

Read STORE-RELEASE-PLAN and STORE-LISTING-DRAFT only when the user resumes this work. Development MSIX work is not a public certified Store submission. Microsoft identity verification/agreement and exact Store identity require the user's account. No trusted certificate is configured. Do not label an unsigned installer as signed, and do not promise SmartScreen reputation from ordinary tests or a self-signed certificate.

## Account-independent preservation

The handoff ZIP contains tracked source/docs and selected non-personal evidence, not an environment image. It excludes `.git`, `.tools`, compiled binaries, credentials, databases and user images/audio. A fresh machine needs SDK/dependencies, service access and sign-in again. Preserve a separate app-generated `.t4backup` for personal workspaces; preserve the full data folder/assets for full app migration. Do not put personal backups in GitHub by default.

After future releases refresh the handoff archive and its checksum. Keep private source/evidence private when moving it to another account.
