# Tommy’s Time Tracking Tables

A Windows 11 desktop stopwatch workspace built with C#, WPF, and .NET 10.

## Run on Windows 11 (64-bit Intel/AMD)

Download the installer from [GitHub Releases](https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.3.0-preview) and run **Tommys-Time-Tracking-Tables-Setup-0.3.0.exe** to install this preview for your Windows account. Local builds place it under `artifacts/installer`. Setup adds a Start menu shortcut and offers an optional desktop shortcut. The runtime is included, and administrator access is not required. Save and close the app before installing again. Uninstalling preserves saved data. This preview is unsigned, so Windows may show an unknown-publisher warning.

The installer also accepts Windows 10 22H2, the current test machine. Windows 11 remains the intended target; clean Windows 11 testing is still pending.

Alternatively, use the portable package:

Extract **Tommys-Time-Tracking-Tables-win-x64.zip**, open its **TommysTimeTrackingTables** folder, and double-click **TimersApp.exe**. Keep the extracted files together. The .NET runtime is included; no SDK or Excel installation is needed. This portable build does not install shortcuts or register file associations.

From the source workspace, **Start Tommy's Time Tracking Tables.cmd** opens the standalone build when present, with a development-build fallback. This is a preview pending live interaction and clean-Windows validation. See `SPECIFICATION.md` for the agreed requirements and `PROGRESS.md` in the source workspace for verification status.

## Available now

- Named workspaces with a selector, New/Save/Rename/Duplicate/Delete actions and recovery of deleted workspaces. Switching stops timers in the workspace being left; only the selected workspace runs. Duplicate copies are stopped and retain recorded history.
- Independent named timers with optional descriptions and name suggestions.
- Start/stop/resume; exact running totals; rounded or combined totals.
- Segment history with full system timestamps; reset and archive retain historical records.
- Timer size and Default pop-out size sliders/percentages with optional linking. Application scale is in Settings; open pop-out windows keep their size.
- Pop-out windows with shared timer state, main-window badge, context menus and always-on-top.
- Two-column style editor with live preview, font samples, preset actions, dimension sliders, vector seven-segment digits and custom number layouts. Use `H:M:S.MS` for `00:00:00.000`; hide-unit-labels is off by default.
- Separate pop-out styles, size/position persistence and visibility options; hidden buttons remain available by right click.
- Image backgrounds with fit/fill/original/stretch/tile, anchors, zoom/pan/crop; portable preset import/export with embedded images.
- Responsive grid cards and horizontal list rows, automatic/fixed columns, spacing and height settings. Individual resizing is off by default and can be enabled in Settings.
- Interval color/flashing/audio alerts, WAV/MP3 playback with bundled open-source decoders, local Windows sound choices, custom sounds, dismiss and snooze (alerts disabled by default). No Windows Media Player installation is needed.
- SQLite autosave, a Saved overlay, an optional compact status bar (off by default), recovery choices and save/discard/cancel exit dialog with optional remembered decision.
- Sleep/resume handling with sleep time excluded by default; backup browser, damaged-database recovery and portable backups containing image/audio assets.
- One Excel/CSV time table; Export all, Today, Date range or Custom timer selection. Exact segment/day details and original timestamps, with each timer's daily total rounded once to quarter-hour decimal hours.

## Data

Default location: `%LocalAppData%/TimersApp/workspace.db`. Save/stop/start operations persist immediately when autosave is enabled. Running snapshots are saved every five seconds, so a crash can lose up to the latest checkpoint interval if interruption time is excluded. Timestamped backups are made hourly during autosave, on manual save and on clean exit; the latest 32 are normally retained (locked older files are retried on the next backup). **Backups** configures the database-wide backup limit, disables automatic snapshots, deletes snapshot files, recovers deleted workspaces, or exports/imports a portable `.t4backup` containing one workspace and its referenced images/sounds. Disabling snapshots does not disable timer autosave or explicit portable exports. Database snapshots contain all saved workspaces.

The first upgrade preserves a consistent pre-migration database as `before-workspaces-*.db` and migrates existing content to `default`. This schema is newer than v0.1.0 can read; retain that preserved database if you need to roll back. Newer saved work cannot be opened in the old preview.

Timer-name suggestions can be cleared together or individually. **Do not save name suggestions** stops collecting new names; existing suggestions remain until removed. Workspace settings edit a draft; Reset to defaults does not erase timers or history.

On exit, Save keeps current changes; Discard restores the workspace from when you entered this workspace. Remember this decision is initially unchecked. Reset or archive preserves recorded history. Reports include segment/day details, with rounded daily hours on the first row for each timer/day; the other rounded cells are blank. Overnight portions retain original timestamps and identify allocation days. There are no subtotal rows or extra summary sheets. Filtering detail rows may hide the row carrying the rounded daily total.

Development tests use `TIMERSAPP_DATA_DIR` to isolate their files under `artifacts/`. That environment variable is not required for normal use.

## Build and verification

Run `./build.ps1` in PowerShell to build and run the 57-check verification suite. Run `./publish.ps1` to produce the self-contained Windows x64 folder and ZIP under `artifacts/release`. Both prefer the project-local .NET 10 SDK and otherwise use an installed .NET 10 SDK. A fresh checkout needs Windows and the .NET 10 SDK; generated files stay under ignored .tools and artifacts folders. Network access is needed when restoring missing packages.

The suite covers independent timers, stopped/sleep gaps, duplicate starts, clock changes, reset/history preservation, name snapshots, rounding boundaries, crash recovery, date clipping, leap days, 23/25-hour daylight-saving days, active snapshots, atomic preferences, schema migration, damaged-database recovery, Excel totals/offsets, style inheritance/persistence, alerts/snooze, portable presets/backups, 50 active timers and offscreen WPF layout renders. The standalone app launches and exposes its controls through Windows accessibility. Accessible timer/readout names and dialog labels are checked offscreen; focus a timer and press Shift+F10 or the menu key to reach hidden controls. Live pointer interaction, audible playback, screen readers, mixed-DPI monitors and a clean Windows installation remain unverified.

## Structure

To build the installer, install Inno Setup 6 and run `./build-installer.ps1`. It publishes the app, compiles Setup, and writes a SHA-256 checksum under `artifacts/installer`. Use `-SkipPublish` only with an already verified current publish folder. The build looks for Inno Setup in `.tools/inno`, standard installation folders, and PATH.

- `src/TimersApp.Core`: serializable records, timing engine and calendar allocation; no WPF dependencies.
- `src/TimersApp.Desktop`: WPF views, dialogs, SQLite storage and workbook writer.
- `tests/TimersApp.Tests`: deterministic engine/integration checks and rendered preview.

The presentation layer uses event-driven views and isolated draft editors. Timer correctness, style resolution, duration formatting and alert scheduling remain in Core. Appearance and interval settings are stored alongside the workspace and obey the same save/discard behavior.


## Automated Windows builds

The GitHub Actions workflow builds and tests on Windows, then uploads the portable ZIP and rendered previews as workflow artifacts. It runs on pushes to main, pull requests and manual requests. The initial GitHub Windows build passed, including tests, packaging and artifact upload.
