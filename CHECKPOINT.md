# Current checkpoint — notes4 complete / 0.5.0 preview published

Updated 2026-10-28 (America/Phoenix). Notes4 implementation, review follow-up, validation and publication are complete. Await user testing/feedback; do not continue optional review suggestions as new work.

- Release: https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.5.0-preview (published prerelease). Verified tag points to source8a611ce7bb552c7286bbebd4faa32c1f13fe6077. Remote code tree dbba0654d4f398761457a097b647b13a3779f90b; local implementation2a5a5e9f713f639903bd97058f3c762e475ee784, following checkpoint2228db2. Preserve both histories; no force-push.
- Installer artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.5.0.exe, 49,414,200 bytes. SHA25672B6C48F140D1F16D6DA71E2EE2173D5AE6BD69D956B3FFDD2078D997F012E95. Both GitHub asset digests match local files. Release proof: artifacts/installer/github-release-0.5.0.png. Adjacent checksum uploaded (111bytes; file digest2B0450FB5CED717E8CBC4F012FEF481A3AD9F3F377697FC1EFE0D7928DF7F9B0). Portable folder/ZIP under artifacts/release.
- 60 local checks pass, zero warnings/errors: artifacts/notes4-build.log. GitHub Windows workflow40647168571 and verify job122678569981 completed successfully, including build/tests, portable packaging and both artifact uploads. Exact final packaged version0.5.0.0 launched/responded/exited using isolated data and saved CleanShutdown=true: artifacts/notes4-portable-smoke-final/result.json.
- All six Notes4 areas implemented: aligned equal command row and spanning labeled squares; uniform whole-card+gap scaling below65%; joint pop-out play/time scaling; clickable badge/teal return arrow; independent shared/per-timer size/location memories reused every reopen; partial list descriptions with ellipsis.
- Reproduced pending resize loss before fix in artifacts/notes4-resize-regression-before.log. Fixed pre-suppression flush and native changes during suppression, preserving shared per-component ordering. Stronger tests assert layout/minimum transitions and callback pumping. Automatic out-of-drag correction does not overwrite saved memory. First stable opening initializes empty memory intentionally and is explained in help/README.
- Three Claude read-only code reviews: 20261027-193236-735,193922-192,235132-622. Final found no confirmed blocking/medium defect; test-validity refinements were applied. Planning review191726-559. Low hypotheses about DPI noise and in-drag automatic growth remain manual checks; final legal native-drag bounds are intentionally saved.
- No installer install/uninstall over the user's existing installation/AppId. Normal app data and installation remain untouched; test app process11860 closed. Full pointer/snap, fractional/mixed DPI, multi-monitor, physical audio and clean Windows11 remain unverified. Host is Windows10 22H2. Native-message tests are not real pointer testing.
- notes4.txt preserved. FEEDBACK4-IMPLEMENTATION-PLAN.md retains planning decisions; PROGRESS.md has implementation/review details. Whole list width shrinks below65% intentionally; hidden status/control preferences remain honored with context-menu access. User excluded cross-version compatibility work.

---
# Historical checkpoint — notes3 complete / 0.4.0 preview published

Updated 2026-10-27. All notes3 work and the user's zero-margin refinement are implemented, tested, reviewed and published. Await user feedback; no required work remains.

- Release: https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.4.0-preview (ID 426141210, published prerelease). Tag points to remote source c80d25a525a6f87a06eb16fd43ecf9947d21b9b1. Local implementation commit 50949f052d4e588c87f48668a6bf1b312ae7a3f4; preserve both histories, never force-push.
- Installer: artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.4.0.exe, 49,404,920 bytes. SHA256 B20E921D6DF61F6334DA2AE72EC7DA06B02A01490A373E82B6B9A8704A918C49. GitHub uploaded digest matches local. Adjacent checksum uploaded. Proof: artifacts/installer/github-release-0.4.0.png. Portable folder/ZIP under artifacts/release.
- 59 local checks pass, zero warnings/errors (artifacts/notes3-build.log). GitHub Windows workflow 40610165937 completed successfully, including verification, packaging and artifacts. Final portable package version0.4.0.0 passed isolated launch/respond/clean-close in artifacts/notes3-portable-smoke-final/result.json.
- Three Claude read-only code reviews: 20261027-172607-204, 20261027-173126-794 and 20261027-173618-887. Confirmed findings were fixed/tested. Final narrow audit found no confirmed blocking/medium issues; its small startup-warning cleanup was applied and verified. Planning review 20261027-171424-679.
- Implemented compact measured timer layout, complete drawing bounds at small scales, zero default decorative padding/radius, content-fitted grid height, one horizontal action row, aligned toolbar, square size/layout buttons, size overlay, responsive two-column Settings, mint checkbox/slider controls and optional app-wide startup WAV/MP3 sound disabled by default.
- Saved workspace styles retain explicit padding; Settings > Use compact default timer style > Apply removes default timer/pop-out padding/radius while retaining fonts/colors/backgrounds and custom presets. Manual heights/individual sizes remain available. Startup global settings survive workspace Discard; launch preferences keep prior agreed session semantics.
- User-installed app PID29416 was running and remained untouched, as did normal data. No installer install/uninstall test this pass to avoid affecting that installation. All test app processes closed. Remaining manual QA: physical sound, pointer interaction, mixed DPI/multi-monitor, clean Windows11 and long-history responsiveness; host is Windows10 22H2.
- Full implementation and review details are at the top of PROGRESS.md. notes3.txt is preserved; FEEDBACK3-IMPLEMENTATION-PLAN.md retains the planning decisions. Do not continue changing source for optional review suggestions.
---
# Current checkpoint — notes2 complete / 0.3.0 preview published

Updated 2026-10-27. All requested notes2 work is implemented, tested, reviewed and published. Await user testing/feedback; do not keep changing source solely for optional review suggestions.

- Source: local implementation 29e7355, final title fix 1a975d1; remote final code 3f6aebcc55bbff0e208fa0832dbe9710133caa11. Local and remote histories differ; preserve both, no force-push.
- Published release: https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.3.0-preview (ID 425601924, prerelease, not draft). Tag points to the final code commit. Installer and checksum uploaded; GitHub digest matches local.
- Installer: artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.3.0.exe. SHA256 509F137511D10B768065A0568714309660AF4A4DA15824B9CCF6B6B08AA207EC. Proof: artifacts/installer/github-release-0.3.0.png. Portable folder/ZIP in artifacts/release.
- 57 local checks pass, zero warnings/errors: artifacts/notes2-build.log. Final GitHub Windows workflow 40525668158 succeeded, including packaging. Earlier implementation workflow 40525157497 also succeeded.
- Three read-only Claude reviews in artifacts/claude-reviews: 20261027-072607-530, 20261027-073746-552 and 20261027-074902-694. Final audit found no confirmed blocking/medium issues. Its small title-clipping suggestion and post-narrowing height assertion were applied and verified with render/shown-window checks.
- Exact final installer passed isolated install, application/audio/license file hashes, bundled-runtime launch, clean shutdown, uninstall and saved-data retention. Logs: artifacts/installer-verified-0.3.0. No test installation or app process remains; normal user data was never touched.
- Completed UI/layout/sizing, daily rounded export, bundled WAV/MP3 audio, Windows sound catalog and icon changes are summarized at the top of PROGRESS.md. The user excluded cross-version compatibility work.
- Remaining manual QA: click-through (Windows blocks simulated pointer input), physical audible playback/device removal, mixed DPI/multi-monitor, clean Windows 11 and long-history UI performance. Actual machine is Windows 10 22H2. Shown WPF geometry tests passed; do not equate them with live mouse testing. Local MP3 decoding used an ignored public silence fixture and is not a CI dependency.

---
# Current checkpoint — notes2 planning / ready for Sol

Updated 2026-10-27. The user requested a plan for notes2.txt before switching from Astra to Sol. Planning is complete in FEEDBACK2-IMPLEMENTATION-PLAN.md; no application source, database, package or release has changed. notes2.txt remains untouched.

- Reviewed current header, timer card/grid, scale/pop-out lifecycle, settings persistence, report/export, audio and icon generator against every notes2 item.
- Structural direction: explicit grid/list/pop-out presentation modes; dimension-based sizing and readable minimums; initial-only pop-out sizing; simplified export selection/table; NAudio + managed NLayer MP3 playback; local Windows sound choices and built-in fallback.
- User explicitly excluded compatibility with other versions. Do not build cross-version migration/downgrade support. Preserve current data and take a backup before stored-data changes.
- Claude reviewed the source and proposed plan read-only. Completed review: artifacts/claude-reviews/20261027-041054-268/review.md. Confirmed refinements and intentional decisions are recorded at the end of the plan. First review attempt failed ECONNREFUSED; retry succeeded.
- User confirmed: round each timer's daily total. The plan retains segment/day detail and places rounded daily hours once on the first row of each timer/day group; other rounded cells are blank. No subtotal rows or tabs. Exact times and original timestamps remain; overnight portions are labeled with allocation dates. No planning questions remain.
- Baseline remains published v0.2.0-preview, prior 51 checks passed. No new implementation/tests run during this planning pass. Target next preview 0.3.0 after implementation and validation.
- Next: after the user switches to Sol, read AGENTS.md, notes2.txt and FEEDBACK2-IMPLEMENTATION-PLAN.md and follow the implementation order. Use Claude for read-only code review and isolated data for tests.

---
# Current checkpoint — notes1 feedback / 0.2.0 preview

Updated 2026-10-27. The notes1 implementation is complete in the working tree, with final read-only review and local installer smoke verification completed. Read AGENTS.md for the Claude read-only requirement and GitHub history rules. The original feedback and architectural plan are retained in notes1.txt and FEEDBACK-IMPLEMENTATION-PLAN.md.

## Implemented

- Named workspace SQLite catalog with transactional transitions, consistent pre-migration backup, stable IDs, recoverable deletions, import/replace choices, and full-catalog database recovery. Only the selected workspace runs. Save/discard/cancel and entry baselines are handled on switches; deliberately stopped segments and alert counters remain correct.
- Global automatic-backup policy: default 32, disable switch, delete-all action; distinct from autosave, portable exports and recovery slots. Preserve newer launch preferences during migration.
- Responsive grouped toolbar beside T4, named-workspace selector/actions, vector icons, editable scale percentages with 25% steps, independent/linkable pop-out scale, Saved overlay and optional compact status bar.
- Larger resize grip, shorter timer controls, width/height-aware pop-out arrangements, tighter readout bounds, stable digit measurement and reuse of clipping/background work. Improved multi-size icon plus generate-icon.ps1.
- Two-column responsive style editor, live preview, numeric font samples, top preset actions, dimension sliders, label visibility and validated custom layouts. H:M:S.MS produces hours:minutes:seconds.milliseconds.
- Workspace layout sliders and draft Reset defaults; suggestion Remove all and disable-new-name collection. Explicit preset and suggestion operations disclose immediate persistence.

## Verified

- Full build and 51 checks pass with zero warnings/errors (artifacts/feedback-build.log). Dependency advisories refreshed successfully using network access.
- Added migration/preference, transaction rollback, catalog recovery/deletion, backup policy, format and alert-state tests. WPF tests also cover fresh Discard identity, restore-off scratch sessions, scratch edits, post-switch baselines, stopped recovery slots, compact/wide pop-outs, scale linking, percentage stepping and layout reuse.
- Rendered workspace, appearance and style-editor images under artifacts/tests were inspected.
- Isolated native app launched and exposed controls through accessibility. Windows blocked simulated mouse input (GetCursorPos Access denied), so do not claim full live interaction, DPI/multi-monitor, real sleep/audio or long-history performance verification.
- An earlier 0.2.0 installer build passed isolated install/file hashes/bundled-runtime launch/uninstall/data retention. Rebuild and recheck the final installer after the last source fixes before distribution.

## Claude reviews (all read-only, tools disabled)

- artifacts/claude-reviews/20261027-021632-675/review.md: fixed restore-off collision, recovery identity, launch preference carry-over, deliberate classification, deleted snapshot selection.
- artifacts/claude-reviews/20261027-022508-574/review.md: fixed spent alert counter resets, raw running recovery slots, scratch handling, picker display/selection and font preservation. Kept intentional Discard-to-entry settings semantics. Disclosed immediate preset/suggestion actions.
- artifacts/claude-reviews/20261027-023444-006/review.md: no blocking/data-loss defects; fixed its two follow-ups (pre-recovery baseline classification and newer explicit migration preferences), with regression tests.
- Final narrow follow-up completed: artifacts/claude-reviews/20261027-024122-176/review.md. No new blocking or medium findings. Optional low-priority hardening is recorded in PROGRESS.md.

## Completed finalization

- All 51 local checks passed with zero warnings/errors. Final Claude follow-up found no blocking/medium findings; optional hardening and manual QA remain in PROGRESS.md.
- Final reviewed installer passed isolated install, file hashes, bundled-runtime launch, uninstall and data retention. SHA256: 71FFF426BD6A33BD67EC642896C74D307D042EEB440695CA612F76CAF887764C.
- Implementation committed locally as 601c26f. Source synced to GitHub commit 87b8f8486b9b9e2384da526f9d76aa156fcf9890 without force-pushing. GitHub Windows workflow 40483209578 completed successfully.
- Published https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.2.0-preview (release ID 425266077). Installer and checksum uploaded; GitHub digest verified against local file. 0.1.0 remains available separately. Release proof: artifacts/installer/github-release-0.2.0.png.
- No required implementation/release steps remain for notes1. Await user testing/feedback. Actual pointer/dialog flows, high-DPI/multi-monitor, real sleep/audio and long-history performance remain manual QA; do not claim they passed. Source code should not be changed merely to clear optional review suggestions without a concrete need.
## Paths and constraints

- App/installer version is 0.2.0. Installer expected at artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.2.0.exe; checksum adjacent.
- Source at C:/TimersApp. sdk.ps1 selects .tools/dotnet/dotnet.exe; Inno Setup at .tools/inno/ISCC.exe.
- Actual test machine is Windows 10 22H2; Windows 11 target still needs clean-machine/live validation. Installer accepts both and remains unsigned. Close the app before installing/upgrading.
- Test installations use HKCU uninstall key {17AA17B2-C8E2-4A45-A338-C24D60DAED75}_is1. Guard against an existing user install before testing; only uninstall the exact verified test path. Never test with normal LocalAppData/TimersApp data.
- Local and remote histories remain distinct. The implementation commit is recorded above; a subsequent documentation-only commit records publication. Use the GitHub integration and leases for source synchronization; never force-push.
# Current checkpoint — compact list 0.5.1 preview published

Updated 2026-10-28. User requested thinner list rows with all existing features. Implemented content-fitted list height by default, independent 4-DIP list spacing, manual height/gap controls and alert expansion. Existing saved manual heights can be restored by disabling list content fit. Claude read-only review `artifacts/claude-reviews/20261028-002726-115/review.md` found no confirmed clipping/data-loss defects; its confirmed misleading tooltip and unclear manual minimum were corrected. Final local build `artifacts/compact-list-build-final.log`: 61 passed, zero warnings/errors. Final installer `artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.5.1.exe` (49,408,162 bytes), SHA256 B1D9D3D76439403AF3A1993A61C3A534DA332B7DC64D2D564672E4A1E5EB49AB. Checksum-file digest D883BA06E9C9ABE02F7BBD021564025D9143C833FAB275A767773F7D8A8A1C19. Exact packaged app launched, responded and closed successfully with isolated data; database CleanShutdown=true (`artifacts/compact-list-portable-smoke-final/result.json`). No normal installation/data was touched. Local implementation commit 580cf25; remote release source 125363b2ea9938e19364ab2f03a2775f941795ad; preserve both histories, no force-push. GitHub Windows workflow 40651115998 / verify job 122691145467 succeeded. Published https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.5.1-preview as prerelease; tag points to the release source. Both uploaded asset digests match local files. Proof: `artifacts/installer/github-release-0.5.1.png`. Manual Windows 11, physical pointer/audio and mixed-DPI checks remain open. User also requested trusted signing and Microsoft Store distribution; begin a separate Store MSIX package and account preparation, with any certificate or account validation handled through the user's own identity.

---

# Current checkpoint — Store and signing preparation after 0.5.1

Signing/installer follow-up, 2026-10-28: The published 0.5.1 setup still has Authenticode status `NotSigned`; its SHA256 matches the published release record (`B1D9D3D76439403AF3A1993A61C3A534DA332B7DC64D2D564672E4A1E5EB49AB`). No usable code-signing certificate was found in the current-user or local-machine personal certificate stores. The normal machine has an existing 0.4.0 installation with the production AppId, so it was not replaced or uninstalled. A temporary Inno build using a distinct test AppId installed into `artifacts/installer-smoke-0.5.1/program`; all 437 published payload files matched the installed copies by SHA256. The app version 0.5.1.0 launched and responded with isolated `TIMERSAPP_DATA_DIR`. Automated close met the expected save/discard prompt, so the test process was stopped; no clean-close result is claimed. The test build uninstalled successfully, its registry key and executable were removed, and its isolated data remained. The first production-ID install attempt in the restricted execution environment failed resolving the Local AppData shell folder before copying any files; a normal-access retry was aborted when the existing 0.4.0 installation was detected. The exact GitHub setup itself has not been installed on this machine. A user choice is pending for a trusted signing identity (Azure Artifact Signing or an existing trusted certificate); signing cannot be completed with a self-signed certificate if the goal is Windows publisher trust. Do not replace the published release asset with a differently hashed file without publishing and verifying a new signed release.

Updated 2026-10-28. The user requested a Microsoft Store release and trusted signing for user confidence. GitHub 0.5.1 preview is complete below. Added `build-store-package.ps1`, `STORE-RELEASE-PLAN.md` and `STORE-LISTING-DRAFT.md`; a development-only, unsigned Windows 11 x64 MSIX with package version 1.5.1.0 was built from a freshly cleared self-contained publish folder. MakeAppx accepted it; `artifacts/store/verification.json` reports 443 CRC-valid entries, matching core app binaries, bundled license and .NET runtime notices, expected manifest identity/version/OS floor/assets and no PDBs. Package SHA256 681A49801B046FD7B43BA58D142DFA7703738F0F1CAE9B1A60F4763807D09365. Online NuGet advisory check found no currently reported vulnerable packages (`artifacts/store-nuget-audit.log`). The package is not Store-submittable or install-tested. The user must sign in/enroll/verify a Partner Center developer account, accept Microsoft's agreement, reserve the title and provide the exact four package identity/display values. A request is pending. Real Store MSIX signing is handled by Microsoft after certification; separate GitHub EXE signing requires a trusted certificate or billable Artifact Signing service. No certificate/account setup or payment was performed. Store data isolation/uninstall and Inno-to-Store migration require a clean Windows 11 VM test, as does WACK. No normal user app data or installation was touched. Claude read-only reviews `artifacts/claude-reviews/20261028-004929-954/review.md`, `005420-206/review.md` and `005829-388/review.md` found no final blocking issue; their confirmed low issues were addressed in the clean publish script and builder. A fourth, post-fix review attempt (`010452-671`) failed with ECONNREFUSED and produced no review.

---

