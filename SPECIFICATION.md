# Tommy’s Time Tracking Tables — agreed product specification

Windows 11 desktop application, C# / WPF / .NET 10. No account or server required.

## Timer behavior
- Independently add/remove/name/describe simultaneous count-up timers.
- Default total includes only running segments. Each start/resume opens a segment; stop closes it.
- History button shows exact system start/stop timestamps and exact duration for each segment.
- Reset archives the previous run; removing a timer retains exportable history by default.
- Preserve exact durations separately from rounded presentation. Default nearest 15 minutes; alternatives 1 minute / 1 hour. Exact, rounded, or both displays.
- Default HH:mm:ss. Configurable units years/days/hours/minutes/seconds/milliseconds, never elapsed months. Duration years use an explicitly labeled 365-day conversion; calendar reports use real calendar boundaries.

## Workspace and appearance
- Grid or list; manual columns, rows/automatic rows, cell width/height, spacing, list height, overflow scrolling.
- Main toolbar App scale and Timer scale sliders with percentage and reset. App scale scales the interface; window size remains independently adjustable.
- Timer cards and pop-outs independently resizable. Text and seven-segment displays; installed font selection.
- App and timer backgrounds independently support colors/images; fit, fill/crop, original, stretch, tile; corner/center/custom anchor with numeric positioning and visual zoom/pan/crop editor.
- Copy imported assets into application data storage.
- Named reusable style presets, import/export, inheritance from app defaults through preset and individual overrides.
- Name history with suggestions and clear/remove operations.

## Pop-outs
- Same timer state, no duplicate timing instance. Close returns timer to workspace.
- Main entry remains with a Popped out badge, status, live total, focus and return controls.
- Optional always on top; remember size/position and recover offscreen windows.
- Separate pop-out style disabled by default. Time-only mode; independent title/description/total/control visibility.
- Every size supports right-click access to timer controls, history, information, presets/custom style, visibility, always-on-top and return.

## Alerts
- Disabled by default. Per-timer one-shot/repeating elapsed interval; color, flashing and/or sound; custom sound, volume, repetition.
- Dismiss restores normal appearance; snooze temporarily restores appearance and silences sound then alerts later.
- Consolidate missed alerts after interruption.

## Persistence and exit
- Separate data file under %LocalAppData%/TimersApp; SQLite with transactions, backups, schema versions.
- Automatic save and restore on launch separately configurable, both on by default. Manual save/load.
- Exit dialog Save and exit / Discard changes and exit / Cancel; Remember this decision initially unchecked. Choice resettable in settings.
- Discard restores session-start saved workspace, even with autosave. Does not erase prior history.
- Distinguish deliberate exit, sleep, crash. Never silently count closed-app time. Recovery offers include interruption or resume from last saved elapsed value.
- Save settings, styles, windows, history and assets; disable autosave without deleting prior saved data.

## Exports
- XLSX primary, CSV optional; all history/date range and all/selected timers; optional active snapshot.
- Detail, daily totals, monthly totals, export information. Calendar month/day grouping, collapsible groups.
- Exact system start/stop and unique segment ID preserved; split allocation at local midnight and clip at selected range edges.
- Running segment stop remains blank, with measured-through field.
- Daily rounding: round each timer/day then sum. Monthly rounding: sum exact timer/month then round. Partial month labeled. Exact numeric durations retained; [h]:mm:ss format.

## Implementation milestones
1. Core timer engine, segment history, persistence/recovery, functional WPF workspace.
2. Date-aware XLSX/CSV reports and rounding verification.
3. Styling, layout, scale sliders, asset editor and presets.
4. Pop-outs and interval alerts.
5. Windows accessibility, sleep/clock/multi-monitor checks, installer and release QA.

Model preference: Astra Medium for architecture and difficult correctness review; Sol Medium for routine UI implementation and polish. Tell user when a switch is worthwhile. No automatic model switching.
