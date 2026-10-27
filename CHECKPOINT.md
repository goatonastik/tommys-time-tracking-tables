# Current checkpoint — notes2 source complete, final distribution pending

Updated 2026-10-27. Sol implemented notes2 and the confirmed daily rounding. Application version 0.3.0. Read AGENTS.md and the top of PROGRESS.md. Normal user data was never used for tests.

- All 57 checks pass, zero warnings/errors: artifacts/notes2-build.log. Includes background audio failure cleanup, local managed MP3 decoding, shown-window pop-out min height/width, default size independence, fresh/restored geometry, slider no-recreation/empty rows, notice lifecycle, scrolled list-to-grid and exports.
- Claude read-only reviews completed: 20261027-072607-530, 20261027-073746-552, and final 20261027-074902-694 under artifacts/claude-reviews. Final audit found no confirmed blocking/medium issues. Its small title-clipping recommendation and post-narrowing minimum-height assertion are implemented and verified. No reviewer is running.
- Source committed locally as 29e7355, remote source commit ce3350bb68d6d2eae00a8a49a0816a977ea9ae54. Small final caption/test fix and documentation need their next commit. No force-push; local/remote histories differ.
- Final/intermediate 0.3.0 installers passed isolated install/file hashes/launch/clean shutdown/uninstall/data retention. A final rebuild is needed for the one-line title clipping fix. No test installation/process remains.
- Pending: rebuild and smoke final installer, commit/sync final fixes, verify Windows CI, publish v0.3.0-preview installer/checksum and record proof. Prior release remains available.
- User excludes cross-version compatibility work. Fresh user pop-outs use current defaults; startup/recovery uses saved geometry. Rounded export hours appear once per timer/day, no subtotal rows/tabs.
- Pointer automation fails GetCursorPos access denied. Do not claim click-through, audible output, mixed DPI or clean Windows 11 validation.

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
