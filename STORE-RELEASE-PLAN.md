# Microsoft Store and trusted distribution

Updated 2026-10-28. The 0.5.1 GitHub preview is published. The Store path uses an MSIX package for the existing WPF/.NET application. Microsoft signs Store-distributed MSIX packages after certification and delivers updates. The real Store submission package is unsigned until Microsoft signs it and must not be sideloaded. The existing GitHub Inno Setup EXE is a separate distribution channel and remains unsigned until a trusted signing service or certificate is configured.

## Completed preparation

- Added `build-store-package.ps1` to create an x64 MSIX from the verified self-contained publish folder, with the existing T4 icon as its package artwork. For a real submission, pass the exact Identity name, Publisher distinguished name, Publisher display name and app Display name from the reserved Partner Center app. The builder rejects the development identity in real-submission mode.
- Built a development-only MSIX from a clean self-contained publish. MakeAppx accepted the full-trust desktop manifest. Archive CRC verification passed for 443 entries; executable, license and .NET runtime notice hashes matched the publish, and x64 architecture and three icon assets were checked (`artifacts/store/verification.json`). The notice files are now copied deliberately into each clean publish. The Store requires a nonzero package major version, so app 0.5.1 maps to package 1.5.1.0, with the fourth field reserved as zero. This package is unsigned and must not be distributed or uploaded to the Store.
- The 0.5.1 app itself passed 61 local checks and a GitHub Windows build. The isolated portable copy launched and closed cleanly. These checks do not replace Store certification or clean-machine testing.
- An online NuGet advisory check found no currently reported vulnerable direct or transitive packages for the desktop project (`artifacts/store-nuget-audit.log`). This is a time-specific check, not a security guarantee.

## Required before submission

1. The owner signs in or creates a Windows developer account at [Microsoft Store Developer Platform](https://storedeveloper.microsoft.com/), completes identity verification and accepts the developer agreement. Microsoft's [current account page](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account) describes a fee-free onboarding flow; check again when enrolling. Reserve “Tommy's Time Tracking Tables” if available. Copy its exact Identity name, Publisher distinguished name, Publisher display name and Display name from Partner Center; the package builder requires all four. The reserved display name may use a different apostrophe than the current draft.
2. Rebuild the MSIX with those exact values and inspect its manifest and contents. On an isolated clean Windows 11 VM, sign a development-identity package with a disposable test certificate trusted only in that VM, then validate install, launch, upgrade, export, pop-outs, data path and uninstall. Do not install the unsigned rehearsal package on the user's normal PC. The Store and existing Inno installations can have different AppData behavior; test side-by-side use and migration through the existing `.t4backup` export/import. Advise users to export a portable backup before uninstall until actual Store data retention is verified. Never overwrite the user's current install or data.
3. Prepare Store screenshots, accurate description, age rating, support details and a privacy statement describing local timer data and user-triggered exports. Review `STORE-LISTING-DRAFT.md` with the owner. The manifest declares `runFullTrust` because this is a WPF desktop app using local windows and file dialogs; explain that in certification notes. Run the Windows App Certification Kit in the isolated test environment. Avoid claiming accessibility or security capabilities beyond what has been verified.
4. Upload the package to Partner Center, resolve validation/certification findings, and submit the listing. Publication follows Microsoft's certification decision.

## Signing the separate GitHub installer

Use a trusted code-signing identity for every non-Store release, preferably [Azure Artifact Signing](https://learn.microsoft.com/en-us/azure/artifact-signing/overview) if the account is eligible, or a CA-issued certificate. The owner must complete identity validation and any subscription or purchase. Sign the application PE files and the final installer, timestamp signatures, then verify them before release. Keep the signing identity and key/service access controlled and consistent across releases. Signing improves publisher identification but does not guarantee that a new direct download avoids SmartScreen warnings; Microsoft says reputation still builds over time. The Store MSIX path avoids SmartScreen download warnings for Store installs.

For an eligible individual, [Microsoft's current Basic tier page](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-change-sku) lists $9.99 per month; a paid Azure subscription and identity validation are also required. Check pricing at setup time. The certificate may show the validated legal name even if the Store publisher display name is Goatonastick. Confirm cost and displayed name with the owner before creating a billable service. The bundled .NET runtime must be rebuilt and redistributed on a regular update cadence; the NuGet audit alone does not cover future runtime fixes.

## References

- [Package a .NET WPF app with MSIX](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/dotnet/package-app)
- [Manual MSIX manifest and package identity](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion)
- [Microsoft Store developer account](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account)
- [Store submission and signing](https://learn.microsoft.com/en-us/windows/apps/publish/get-started)
- [SmartScreen reputation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
