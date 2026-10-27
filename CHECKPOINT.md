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

## Remaining finalization

1. Final Claude verdict is complete and accepted; no required code fixes remain.
2. Final installer build and isolated install/launch/uninstall/data-retention test completed. Logs in artifacts/installer-final-0.2.0. SHA256: 71FFF426BD6A33BD67EC642896C74D307D042EEB440695CA612F76CAF887764C.
3. Record final installer SHA256 and completed review/QA in PROGRESS and here. Commit source/docs locally with existing author Goatonastick. Preserve notes1.txt.
4. Sync changed source files using GitHub app with a lease; local and remote histories differ, never force-push. Remote main last checked d3ebafeb7143510b03ea29ebbcbf98c403d59fef, tree 4ca94c1513d37a244299780b10046ec96e97d8a6. New icon blob already uploaded: 761da28e0839f00198616c2f5d6a9346aaf774fa. functions store contains feedbackRemoteHead, feedbackBaseTree, feedbackIconBlob.
5. Publish a NEW v0.2.0-preview GitHub prerelease with installer and checksum; do not overwrite 0.1.0. GitHub connector supports source updates and reading releases but lacks release upload/write tools, so use the signed-in in-app browser for that step. Verify asset size/hash through connector after upload, keep release tab, capture proof image.

## Paths and constraints

- App/installer version is 0.2.0. Installer expected at artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.2.0.exe; checksum adjacent.
- Source at C:/TimersApp. sdk.ps1 selects .tools/dotnet/dotnet.exe; Inno Setup at .tools/inno/ISCC.exe.
- Actual test machine is Windows 10 22H2; Windows 11 target still needs clean-machine/live validation. Installer accepts both and remains unsigned. Close the app before installing/upgrading.
- Test installations use HKCU uninstall key {17AA17B2-C8E2-4A45-A338-C24D60DAED75}_is1. Guard against an existing user install before testing; only uninstall the exact verified test path. Never test with normal LocalAppData/TimersApp data.
- Local HEAD before feedback is fe190c9; working changes/new files are staged partly but not committed yet. Current published release is v0.1.0-preview.