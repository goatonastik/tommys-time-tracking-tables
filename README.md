# Tommy’s Time Tracking Tables

A Windows 11 desktop stopwatch workspace built with C#, WPF, and .NET 10.

## Run on Windows 11 (64-bit Intel/AMD)

Extract **Tommys-Time-Tracking-Tables-win-x64.zip**, open its **TommysTimeTrackingTables** folder, and double-click **TimersApp.exe**. Keep the extracted files together. The .NET runtime is included; no SDK or Excel installation is needed. This portable build does not install shortcuts or register file associations.

From the source workspace, **Start Tommy's Time Tracking Tables.cmd** opens the standalone build when present, with a development-build fallback. This is a preview pending live interaction and clean-Windows validation. See `SPECIFICATION.md` for the agreed requirements and `PROGRESS.md` in the source workspace for verification status.

## Available now

- Independent named timers with optional descriptions and name suggestions.
- Start/stop/resume; exact running totals; rounded or combined totals.
- Segment history with full system timestamps; reset and archive retain historical records.
- App and timer scale sliders, grid/list switch, column count and card size settings.
- Pop-out windows with shared timer state, main-window badge, context menus and always-on-top.
- Style presets with live preview, installed fonts, vector seven-segment digits, configurable duration units and minimal time-only displays.
- Separate pop-out styles, size/position persistence and visibility options; hidden buttons remain available by right click.
- Image backgrounds with fit/fill/original/stretch/tile, anchors, zoom/pan/crop; portable preset import/export with embedded images.
- Automatic/fixed grid columns, minimum rows, spacing, list height and individual resize handles.
- Interval color/flashing/audio alerts, custom sounds, dismiss and snooze (disabled by default).
- SQLite autosave, manual save/load, separate restore preference, recovery choices, save/discard/cancel exit dialog with optional remembered decision.
- Sleep/resume handling with sleep time excluded by default; backup browser, damaged-database recovery and portable backups containing image/audio assets.
- Excel detail/daily/monthly/metadata sheets, all-history/date-range filters, daily/monthly rounding and CSV detail.

## Data

Default location: `%LocalAppData%/TimersApp/workspace.db`. Save/stop/start operations persist immediately when autosave is enabled. Running snapshots are saved every five seconds, so a crash can lose up to the latest checkpoint interval if interruption time is excluded. Timestamped backups are made hourly during autosave, on manual save and on clean exit; the latest 20 are normally retained (locked older files are retried on the next backup). **Backups…** can restore snapshots or export/import a portable `.t4backup` containing the workspace and referenced images/sounds.

On exit, Save keeps current changes; Discard restores the workspace from the start of this session. Remember this decision is initially unchecked. Reset or archive preserves recorded history. Reports may include archived timers and can filter selected timers or dates. XLSX includes calendar groups and summaries; CSV contains exact session detail only.

Development tests use `TIMERSAPP_DATA_DIR` to isolate their files under `artifacts/`. That environment variable is not required for normal use.

## Build and verification

Run `./build.ps1` in PowerShell to build and run the 39-check verification suite. Run `./publish.ps1` to produce the self-contained Windows x64 folder and ZIP under `artifacts/release`. Both prefer the project-local .NET 10 SDK and otherwise use an installed .NET 10 SDK. A fresh checkout needs Windows and the .NET 10 SDK; generated files stay under ignored .tools and artifacts folders. Network access is needed when restoring missing packages.

The suite covers independent timers, stopped/sleep gaps, duplicate starts, clock changes, reset/history preservation, name snapshots, rounding boundaries, crash recovery, date clipping, leap days, 23/25-hour daylight-saving days, active snapshots, atomic preferences, schema migration, damaged-database recovery, Excel totals/offsets, style inheritance/persistence, alerts/snooze, portable presets/backups, 50 active timers and offscreen WPF layout renders. The standalone app launches and exposes its controls through Windows accessibility. Accessible timer/readout names and dialog labels are checked offscreen; focus a timer and press Shift+F10 or the menu key to reach hidden controls. Live pointer interaction, audible playback, screen readers, mixed-DPI monitors and a clean Windows installation remain unverified.

## Structure

- `src/TimersApp.Core`: serializable records, timing engine and calendar allocation; no WPF dependencies.
- `src/TimersApp.Desktop`: WPF views, dialogs, SQLite storage and workbook writer.
- `tests/TimersApp.Tests`: deterministic engine/integration checks and rendered preview.

The presentation layer uses event-driven views and isolated draft editors. Timer correctness, style resolution, duration formatting and alert scheduling remain in Core. Appearance and interval settings are stored alongside the workspace and obey the same save/discard behavior.


## Automated Windows builds

The GitHub Actions workflow builds and tests on Windows, then uploads the portable ZIP and rendered previews as workflow artifacts. It runs on pushes to main, pull requests and manual requests. The initial GitHub Windows build passed, including tests, packaging and artifact upload.
