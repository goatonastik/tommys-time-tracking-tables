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
- Optional installer, code signing and shortcuts. Current delivery is a portable x64 preview ZIP.
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
