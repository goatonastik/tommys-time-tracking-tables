# notes2 / v0.3.0 implementation — 2026-10-27

Implemented notes2.txt using FEEDBACK2-IMPLEMENTATION-PLAN.md, including the user's confirmed per-timer daily export rounding. Cross-version compatibility work is excluded.

- Header uses separate measured workspace/command rows, a two-row Add timer area, left-aligned Timer size/default pop-out sizing with chain link, and Settings-only app zoom. High zoom has an unscaled Reset app size escape.
- Grid and list presentations are distinct. Grid identity sits bottom-right; list fills the viewport with time, decimal rounded hours, identity and actions. Description hides before required content; minimum digit size and measured content prevent unreadable shrinking. Grid columns refresh after mode changes and scrolled lists. Individual resizing defaults off and retains saved dimensions.
- Fresh pop-outs use the default percentage; restoration uses saved geometry. Live pop-outs do not receive global transforms or resize when defaults/link change. Context menus and minimal styles remain.
- One Excel/CSV time table with centered headers, 45-unit description width, measured wrapped row heights, exact segment/day portions and original timestamps. Quarter-hour decimal hours appear once per timer/day. No subtotal rows or summary tabs. Full identifiers retained internally/comments; short IDs have collision handling. CSV includes milliseconds and escapes formulas.
- Bundled NAudio.WinMM 2.2.1 and NLayer 3.0.0 replace WPF MediaPlayer. PCM/float WAV and managed MP3 playback, verified local Windows sound choices, fallback chime, stable preview ownership and deferred custom-file import. Required MIT notices are included. Background-read stop/dispose is synchronized and device failures are handled.
- Icon frames regenerated with measured ink bounds and padding; 16–48px frames inspected.

Verification so far: build.ps1 passes 57 checks with zero warnings/errors; artifacts/notes2-build.log. Includes daily rounding, overnight/range allocation, duplicate names/short IDs, workbook formatting, WAV/MP3 decoding (local ignored silence fixture), threaded audio failure cleanup, narrow/list/grid renders, scrolled list→grid, and shown WPF pop-out minimum/default-size checks. MP3 fixture source: public blank-audio repository (not redistributed; no license assumed), artifacts/audio-fixtures/silence.mp3; optional local decoder check is not part of the committed test dependencies.

Claude first read-only code review: artifacts/claude-reviews/20261027-072607-530/review.md. Fixed custom sound reselection loss, import-on-Cancel copies, slider rebuilding/save flood, tooltip notice loss, inaccessible slider/date names, audio errors misclassified as save errors, and per-tick title/digit drawing allocations. Retained intentional fresh-pop-out defaults, fixed quarter-hour export rounding, requested Workspace style label, existing snooze deadline behavior and explicit Apply restarting alert configuration. Source documentation supersedes old export expectations.

An intermediate installer passed isolated installation, installed-file hashes (including decoder DLLs/licenses), bundled-runtime launch, accessibility inspection, clean shutdown, uninstall and data retention. The final package was rebuilt and rechecked after review fixes. Windows blocked pointer input with GetCursorPos access denied; no live click-through or audible playback claim. Actual machine remains Windows 10 22H2; mixed-DPI/multi-monitor, physical audio and clean Windows 11 remain manual checks.

Read-only follow-up review completed at artifacts/claude-reviews/20261027-073746-552/review.md. Fixed condition-based warning deduplication/recovery, keyboard-accessible announced notification dismissal, blank grid-row resizing, missing custom-file fallback, preview error visibility, preserved saved sound choices, and wrapped lazy tooltips. Final narrow audit artifacts/claude-reviews/20261027-074902-694/review.md found no confirmed blocking/medium issues. Applied its small wide-popout text clipping recommendation and verified the render plus actual minimum height after narrowing. All 57 checks still pass with zero warnings/errors. Fresh versus restored geometry and slider no-recreation/empty-row sizing are covered with shown WPF windows. Source committed locally as 29e7355 and synced to remote ce3350bb68d6d2eae00a8a49a0816a977ea9ae54; the small final caption fix follows in a new commit. Final distribution and CI verification are complete; see the publication record below.

## notes2 final distribution — completed

Published 0.3.0-preview at https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.3.0-preview, release ID 425601924. Tag points to final source 3f6aebcc55bbff0e208fa0832dbe9710133caa11 (local 1a975d1). Final GitHub Windows run 40525668158 passed build, 57 checks, packaging and artifact uploads. Three Claude read-only reviews completed; the final narrow audit reported no confirmed blocking/medium issues. Its remaining title-overlap suggestion was fixed and rendered at 640x160, and the shown-window minimum-height assertion after narrowing passed.

Exact final installer install/file hashes/runtime launch/clean shutdown/uninstall/data retention passed under artifacts/installer-verified-0.3.0, preserving the isolated test database. Installer size 49,410,103 bytes; SHA256 509F137511D10B768065A0568714309660AF4A4DA15824B9CCF6B6B08AA207EC, verified against the uploaded GitHub asset digest. Screenshot proof: artifacts/installer/github-release-0.3.0.png. No test installation or process remains. Prior releases remain available.

No required implementation or release steps remain. Await user feedback. Manual limitations recorded above still apply; optional broad asset garbage collection, deferred image imports, very large export performance and finer-than-millisecond CSV precision were not expanded into this feedback pass.

---
# Implementation handoff

## Current milestone

Functional foundation, reporting, customization, pop-outs, alerts and the Astra reliability review are implemented. A standalone Windows x64 preview is packaged. Live interaction and release QA remain. User prefers Astra Medium for correctness and Sol Medium for UI polish.

Brand: Tommy’s Time Tracking Tables, compact T⁴ mark. User suggested the full name during implementation. Detailed accepted requirements are in SPECIFICATION.md.

## Verified

- Release build: zero errors and zero warnings.
- 39 automated checks pass through tests/TimersApp.Tests.
- Latest rendered layout: artifacts/tests/workspace-preview.png. Inspected and fixed a foreground-style inheritance bug.
- XLSX fixtures generated and reopened to verify duration values and different daily/monthly rounding results.
- Running app starts and exposes expected controls through Windows accessibility inspection.
- Live Windows screenshots repeatedly timed out. Keyboard/interaction validation is incomplete; do not describe the UI as fully tested.
- Isolated UI test processes were stopped after checking exact executable identities. No production data has been created by tests.
- New appearance gallery: artifacts/tests/appearance-preview.png. Inspected regular fonts, seven-segment display, millisecond units and a 300x80 time-only card. Portable preset with an embedded image was exported/imported and its crop preserved.

## Implemented

- Core stopwatch and segment-history behavior, monotonic duration vs wall timestamps.
- Reset keeps earlier runs; remove archives timer; exported history includes archives.
- Default quarter-hour rounding, nearest minute/hour, exact/rounded/both.
- Basic dark workspace, main app/timer scale sliders, grid/list, columns and card dimensions.
- Basic pop-out shared-state windows; badge, focus, return, context menu, always-on-top.
- Editable names/descriptions with built-in ComboBox name completion; clear all name history.
- SQLite workspace snapshots; autosave and restore toggles; manual load/save; restart recovery choices; save/discard/remember exit dialog; timestamped backups (latest 20).
- All/date-range Excel report, selected/all timer selection, active snapshots, real calendar splits, daily/monthly rounding and partial-month labels. CSV is exact detail only.

## Added in customization phase (Sol)

- Serializable TimerStyle, BackgroundStyle, StylePreset and WindowPlacement models; default/preset/custom inheritance. Separate pop-out style is off by default, with explicit individual overrides.
- Installed fonts, manual size or auto-fit, vector seven-segment digits, optional y/d/h/min/s/ms units with 365-day duration years. Title/description/status/buttons/total visibility. Small cards hide overflow controls; context menus retain all commands.
- Live-preview style editor; create/edit/rename/duplicate/remove/import/export presets. Removing a preset copies its style onto affected timers so their appearance is preserved. .t4style ZIP files embed image assets.
- App/timer backgrounds with managed image copies, fit/fill/original/stretch/tile, anchor presets and numeric custom anchors, opacity, zoom, draggable pan and crop region selection.
- Auto-fit or fixed columns, minimum manual rows, spacing, list-row height, draggable individual card sizes and reset-to-default sizes. TimerGridPanel supports variable card dimensions with scrolling.
- Pop-out geometry/always-on-top/open-state persistence and virtual-screen boundary fallback. Primary display entries retain live total and Popped out badge/accent border. Pop-outs render directly at their window size without a scroll pane; time-only mode and all hidden commands remain available by right click.
- Interval alerts: exact running-time threshold, one-shot/repeat, digit color/flashing, built-in generated WAV or imported WAV/MP3/WMA/M4A, volume, repeated sound, dismiss and snooze. Dismiss/snooze buttons appear on full cards during an alert; context commands work at any size. Missed intervals consolidate to one alert.
- Individual name-suggestion removal.
- Fixed settings-application recursion that could overwrite a restored timer scale while applying app scale.

## Completed reliability review (Astra, 2026-10-26)

- Fixed checkpoint recovery gaps, discard/session-baseline behavior, atomic launch preferences and autosave scheduling after wall-clock changes.
- Added default sleep exclusion, power-event handling and crash recovery while sleep-paused; tested simulated sleep gaps.
- Added schema validation/migration, damaged-database preservation/recovery, backup browser and portable workspace backups with assets.
- Added actual monitor working-area clamping for main/pop-out windows, including monitor-layout changes. Real mixed-DPI hardware still needs validation.
- Added selected-timer exports, exact/rounded summary choices, daily/monthly subtotal rows, original timestamp offsets and clock-adjustment annotations.
- Fixed empty exports, resize scaling, inactive-alert snooze, tiled-image anchoring and replacement-workspace autosave races.
- Updated SQLite native bundle to 2.1.14 and added app icon.
- Release build and all 39 automated checks passed. Offscreen workspace rendering inspected.
- Standalone x64 publish succeeded with bundled .NET and Windows Desktop 10.0.14 runtimes. The executable launched with DOTNET_ROOT pointed at a nonexistent external runtime.
- Live accessibility inspection confirmed main buttons, both scale sliders and empty state. Screenshot capture timed out; simulated mouse input failed with GetCursorPos Access is denied (0x80070005). No full live interaction claim is warranted. Both isolated test instances were stopped after fresh executable identity checks.

## Additional Astra review (2026-10-26)

- Fixed decimal overflow during multi-year report allocation using exact integer arithmetic. Four-year segments preserve every tick, including date-range clips.
- Fixed final supported calendar month export (December 9999).
- CSV now writes to a temporary file before replacing the destination, matching XLSX protection. Regression test locks an existing report, verifies preservation/cleanup, then confirms successful replacement and escaped formula-like text.
- Compact timer cards are keyboard focusable and expose their options through Shift+F10 or the menu key. Custom timer/readout automation peers provide current names, states and times; icon buttons, scale sliders and standard dialog fields have accessible labels.
- All 35 checks pass, with zero build warnings/errors. Accessibility tests inspect offscreen peers and labels; actual screen-reader and keyboard operation still need live testing.
## Remaining release QA

- Live add/start/stop/history/export, save/discard/remember and recovery dialog workflows.
- Preset editing/import/export, image crop/pan, audio playback/snooze, native pop-out sizing and minimal context menus.
- Keyboard/screen-reader checks, high DPI and real multi-monitor configurations; real Windows sleep/resume.
- Long-history performance and clean Windows testing. Fifty simultaneous timers pass deterministic checks.
- Code signing remains pending. Delivery includes a portable x64 preview ZIP and a per-user installer with shortcuts (see installer verification below).
- CSV remains exact detail only. XLSX is the complete grouped and rounded report format.
## Commands

Build/test: `./build.ps1`.

Launch: `Start Tommy's Time Tracking Tables.cmd`.

Project-local SDK: `.tools/dotnet/dotnet.exe`; CLI home `.tools/cli`; NuGet packages `.tools/packages`; NuGet.Config uses nuget.org.

Keep generated artifacts and .tools out of version control. There was no existing repository content or AGENTS.md in the workspace when work began.



## GitHub repository

- Created private repository https://github.com/goatonastik/tommys-time-tracking-tables and uploaded all 44 tracked files through the GitHub integration. Local origin points to this repository. The initial local commit is preserved separately; GitHub has its own initial README commit and upload history. Local Git authentication is not configured; use the connected integration for subsequent uploads until Git is authenticated.
- Added fresh-checkout SDK discovery, .NET 10 version selection, line-ending rules, private-data exclusions and a Windows build/test/package workflow.
- All 35 checks and standalone publishing pass locally. GitHub Windows workflow 40398395644 completed successfully, including tests, packaging and artifact upload.

## Claude review integration (2026-10-26)

- Claude Code 2.1.308 installed and authenticated through the user's Claude Pro subscription. The review script defaults to Sonnet, supplies a numbered source snapshot, disables tools/customizations and saves output under ignored artifacts/claude-reviews. The reusable setup is also on GitHub.
- First full static review completed successfully: artifacts/claude-reviews/20261026-155655-481/review.md. A minimal connection check also returned READY. No application code was changed by Claude.
- Independently confirmed findings to address: backup exceptions during ordinary Save are mislabeled as primary save failures; main cards lack a return-from-pop-out action; history durations omit milliseconds; pop-out window titles do not follow timer renames.
- Lifecycle suggestions need regression coverage before changes: restore-off uses a single previous-workspace slot, and Load saved does not preserve the displaced workspace, although its dialog explicitly warns that unsaved changes are replaced. Preserve accepted session-start Discard semantics.
- Do not accept every review assertion: backup failures on exit are already caught separately, and active blank-stop exports already have tests. The proposed sleep-gap heuristic must distinguish wall-clock adjustments from sleep before adoption. Accessibility/performance and real sleep/audio/multi-monitor concerns remain partly unverified.
- Subsequent substantive changes should use this independent review workflow, with findings checked against the code before implementation. Raw review snapshots remain local, outside Git.

## Claude-reviewed fixes (2026-10-26)

- Added AGENTS.md to preserve the user's requirement: use Claude for substantive reviews and keep it read-only.
- Primary-save and secondary-backup outcomes are separated in PersistenceResult, including preservation before Load saved. Failed backups cannot turn a durable save into a primary-save failure or block a safely preserved restore.
- Backup targets use nonpooled connections, close before rotation, and become visible only after a completed copy is moved from .partial to .db. Locked old files are retried during later rotation rather than invalidating the new backup.
- Load saved preserves the displaced workspace first; dialogs explicitly describe the single most-recent before-restore slot.
- Main timer menus offer Return to workspace; pop-out titles track renamed timers. Segment-history durations include milliseconds. Add/Edit timer names have an explicit accessible label.
- 39 checks pass with zero warnings/errors. New checks cover save/backup outcome separation, preservation despite backup failure, locked-old-backup retention, millisecond formatting and pop-out title changes.
- Claude reviewed the first batch (artifacts/claude-reviews/20261026-172902-742/review.md). Its secondary-backup and incomplete-file findings prompted the follow-up fixes above. Single-slot recovery behavior is documented; complete lifecycle UI testing and restore-off history retention remain follow-up work.
- Final read-only follow-up: artifacts/claude-reviews/20261026-205932-592/review.md reported no new confirmed defects in these fixes. Remaining checks: interrupted-backup SQLite sidecars and corrupt optional slots in Load. Backup warnings intentionally persist until a successful backup; manual Save retries immediately.

## Preview installer (2026-10-26)

- Added an Inno Setup 6 installer, reproducible build script and SHA-256 output. Per-user install includes the runtime, Start menu shortcut, optional desktop shortcut and Windows uninstall registration. User data is separate and preserved on removal.
- Built with Inno Setup 6.7.3. Verified installation into an isolated folder, installed-file hashes, a responsive app window with no external runtime, and successful uninstall with test data retained. The actual test PC reports Windows 10 22H2; Setup accepts that version and Windows 11. Clean Windows 11 and interactive wizard testing remain pending.
- Claude's read-only reviews: artifacts/claude-reviews/20261026-232352-221/review.md and 20261026-232604-360/review.md. No blocking defects. Setup never automatically closes the app; its information page asks users to save and close it first. Enforced running-app upgrade blocking and obsolete-file cleanup remain future installer improvements; do not reinstall this preview while it is running.
- Review snapshots now include installer sources. New files must still be staged before review, as documented in AGENTS.md. Application code is unchanged; its latest verification remains 39 passing checks.
- Installer is unsigned. Output: artifacts/installer/Tommys-Time-Tracking-Tables-Setup-0.1.0.exe.

## notes1 feedback implementation (2026-10-27)

- Implemented named workspace catalog, transactional save/switch/delete, stable legacy migration with pre-migration database preservation, deleted-workspace recovery, and complete-catalog snapshot recovery. Session entry baselines now follow workspace selection; scratch workspaces do not accumulate automatically. Restore/import explicitly chooses a new workspace or replacement while preserving catalog identity.
- Added responsive grouped header, editable scale percentages and 25% steps, independent/linkable pop-out scale, Saved overlay, optional compact status bar, vector action/layout icons, larger resize grips and shorter timer buttons. Removed in-window heading/tagline. Pop-outs use width and height, with short/wide side arrangements and tighter readout bounds. Cached repeated clipping/background/readout work; digit widths remain stable as values tick.
- Rebuilt style editor with two settings columns, font samples, top preset actions, sliders, hidden-unit-label and custom-layout controls, responsive fallback and draft isolation. Added layout sliders/reset, suggestion Remove all/disable, global backup disable/count/delete and updated multi-size ICO with reproducible generation script.
- 51 checks pass with zero warnings/errors. Added persistence rollback, legacy migration, whole-catalog recovery, retention/default/disable/deletion scope, custom-format validation, suggestion preferences, stopped-copy/alert classification and launch preference checks. WPF integration additionally covers first-session Discard identity, repeated restore-off launches without catalog growth, post-switch Discard baseline, preserved recovery segments, wide-pop-out controls, scale independence/linking and percentage step event count. Editor and workspace renders are in artifacts/tests.
- Read-only Claude review artifacts: 20261027-021632-675 and 20261027-022508-574. Confirmed findings fixed include restore-off name collisions, stale recovery identity, launch preference carry-over on switching, deliberate stop classification, alert counter resets, running recovery slots, empty scratch accumulation, backup picker labels/defaults and unavailable font preservation. Preset/suggestion actions now explicitly disclose immediate persistence. The suggestion to retain changed settings after session Discard was not adopted because Discard restoring entry-time settings is the existing user-agreed behavior.
- Native test app launched with isolated data and exposed the redesigned controls through accessibility. Simulated mouse input remains blocked by Windows GetCursorPos Access denied (0x80070005); no live end-to-end interaction claim. Actual high-DPI/multi-monitor, audio, sleep and long-history performance remain manual QA. A final focused review and installer verification are pending at this entry.

- Final focused review 20261027-023444-006 found no blocking/data-loss defects. Its two follow-ups were fixed: switch-Discard now shares ExitWorkspace recovery classification, and legacy migration preserves explicitly saved launch preferences that are newer than current. New regression tests cover both. Empty scratch settings/preset edits are now recognized and persisted normally, while untouched scratch sessions still do not accumulate. Unchanged digit updates and card clipping also have layout-stability assertions.

- Final narrow Claude review 20261027-024122-176 reported no new blocking or medium findings. Optional follow-ups: use the same slim preferences payload on scratch exit, and tolerate a corrupt legacy preferences slot during migration. Both remain documented; no unverified live-flow claims.
- Final v0.2.0 installer passed isolated installation, file-hash checks, responsive launch without an external runtime, uninstall and saved-data retention. Logs: artifacts/installer-final-0.2.0. Installer SHA256: 71FFF426BD6A33BD67EC642896C74D307D042EEB440695CA612F76CAF887764C.

- Published v0.2.0-preview to GitHub Releases, ID 425266077, with installer/checksum; uploaded digest matches the final local installer. Source commit 87b8f8486b9b9e2384da526f9d76aa156fcf9890. GitHub Windows workflow 40483209578 passed. Local implementation commit 601c26f; both histories preserved.
