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
- Main toolbar Timer size and Default pop-out size sliders/percentages, link off by default. Application scale is in Settings. Timer size changes logical dimensions down to 65%; below that, the entire card and its gap scale uniformly to the requested size.
- Individual workspace card resizing disabled by default, enabled in Settings. Pop-out windows remain independently resizable. Text and seven-segment displays; installed font selection.
- App and timer backgrounds independently support colors/images; fit, fill/crop, original, stretch, tile; corner/center/custom anchor with numeric positioning and visual zoom/pan/crop editor.
- Copy imported assets into application data storage.
- Named reusable style presets, import/export, inheritance from app defaults through preset and individual overrides.
- Name history with suggestions and clear/remove operations.

## Pop-outs
- Same timer state, no duplicate timing instance. Close returns timer to workspace.
- Main entry remains with a Popped out badge, status, live total, focus and return controls.
- Optional always on top; independent size/location memory enabled by default, each with a per-timer option enabled by default. Turning off per timer shares the latest value within the workspace. Saved values apply on every reopen; default size is fallback when memory is off or absent. Disabling memory retains previous values. Recover offscreen windows. Changing the default never resizes an open window.
- Separate pop-out style disabled by default. Time-only mode; independent title/description/total/control visibility.
- Every size supports right-click access to timer controls, history, information, presets/custom style, visibility, always-on-top and return.

## Alerts
- Disabled by default. Per-timer one-shot/repeating elapsed interval; color, flashing and/or sound; WAV/MP3 decoding and output bundled without WMP, local Windows sound choices, built-in fallback, volume and repetition.
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
- One XLSX sheet or equivalent CSV table for the current workspace. Export all, Only today, Date range or Custom selected timers; include archived timers and optional running snapshots.
- Ordered columns: exact duration, daily rounded decimal hours, timer name, description (45 units wide), original date started, actual start, actual stop, status, eight-character segment ID.
- One row per segment/calendar-day portion. Exact measured duration clips to selected local dates. Original timestamps remain unchanged; allocation days and partial segments are labeled. Running stop remains blank with measured-through time in status. Full IDs and offsets remain in workbook comments; CSV timestamps retain offsets.
- Sum exact ticks per timer/day, round once to nearest 15 minutes (midpoints upward), display decimal hours once on the first row of each timer/day group. Other rounded cells blank. No subtotal rows or daily/monthly/metadata sheets. Filtering detail rows can hide the row carrying the rounded daily total.
- Centered wrapped headers, fitted columns/rows, numeric durations [h]:mm:ss and decimal hours 0.00. CSV escapes formulas; output replacement is atomic.

## Implementation milestones
1. Core timer engine, segment history, persistence/recovery, functional WPF workspace.
2. Date-aware XLSX/CSV reports and rounding verification.
3. Styling, layout, scale sliders, asset editor and presets.
4. Pop-outs and interval alerts.
5. Windows accessibility, sleep/clock/multi-monitor checks, installer and release QA.

Model preference: Astra Medium for architecture and difficult correctness review; Sol Medium for routine UI implementation and polish. Tell user when a switch is worthwhile. No automatic model switching.

## Feedback revision: notes1.txt (0.2.0 preview)

- Named saved workspaces, default name `default`, with selector and New/Save/Rename/Duplicate/Delete icon actions. Only the selected workspace runs; switching offers save/discard/cancel when needed and deliberately stops departing timers. Deletions remain recoverable; duplication preserves history with stopped totals and new workspace identity.
- Responsive grouped toolbar beside T⁴, with long in-window heading/tagline removed. Percent inputs replace main scale sliders; +/- selects the next/previous 25% step. Independent pop-out scale defaults to 100%, with linking to timer scale off by default.
- Saved overlay replaces successful-save dialogs. Compact status bar is optional and off by default; failures remain visible independently. Consistent grid/list icons, smaller timer button height, resize grip at least twice the original size, larger icon artwork and responsive short/wide pop-outs.
- Two-column style settings beside a live preview, top preset actions, font entries with numeric samples, grouped font-size/padding/radius sliders, optional hidden unit labels (off), and validated custom formats using Y/D/H/M/S/MS. `H:M:S.MS` means hours:minutes:seconds.milliseconds; no calendar-month duration token.
- Workspace settings use layout sliders and draft-only Reset to defaults. Name-suggestion manager supports Remove all and stops collecting when Do not save name suggestions is selected; old suggestions remain until removed.
- Database-wide automatic backups default to 32, may be disabled independently of timer autosave, and may be deleted explicitly. Portable exports and recovery copies remain separate. Migration preserves a consistent database copy before assigning persistent workspace identity.
- Explicit preset save/delete and suggestion-management actions apply immediately, with explanatory text; cancelling a surrounding editor discards its draft only. Session Discard continues to restore entry-time settings and history, as originally agreed. Remembered exit decisions retain their existing semantics.

## Feedback revision: notes3.txt (0.4.0 preview)

- Fully visible timer drawing at small scales: measure actual glyph bounds, play control and composed rows. Default timer/pop-out styles have zero decorative padding and radius. Custom padding remains selectable; rounded clips require only geometrically necessary clearance.
- Content-fitted grid height defaults on; manual workspace height and explicit per-timer sizes remain selectable. Compact footer places title above a horizontal action row. Exact time remains readable, with context menu access at all sizes.
- Responsive two-column Settings, shorter selection fields and mint checkbox/slider accents. Settings changes remain a draft until Apply; application scale previews revert on Cancel.
- Top command rows share widths; square grid/list and growing-clock buttons. Size overlay contains timer/pop-out sliders and linking; live debounced updates preserve open pop-out geometry. Escape/outside click closes the panel.
- App-wide optional custom WAV/MP3 startup sound defaults disabled. One playback after launch/recovery, no replay on workspace switch or resume, separate from timer alerts. Preview, volume, managed import on Apply, missing-sound notification without fallback. Global startup preference and launch preferences commit atomically; workspace Discard does not undo an applied global preference. Portable workspace archives exclude it.

## Feedback revision: notes4.txt (0.5.0 preview)

- Equal Export/Settings/Appearance/Backups row aligns with Delete; labeled layout and size squares span both rows, with scrolling/reflow at narrow widths.
- Uniform whole-card shrinking below 65%, including controls/background and inter-card gaps. Lists show partial descriptions with ellipsis instead of hiding them.
- Pop-out time and play/stop fit together. Click the popped-out badge or teal inward arrow to return; focus remains available in the context menu.
- Independent per-timer/shared size/location memory reused on reopen, with normal geometry captured from user window movement and flushed before saving.


## Compact list follow-up (0.5.1 preview)
- List height fits measured content by default, independently of grid content-fit. Preserve font, time/totals, metadata and action buttons; expand for alerts/status as needed.
- List gap defaults to 4 DIPs independently of grid spacing. Settings provides a content-fit toggle, manual list row height (24–300 DIPs, constrained by actual content) and separate gap control. Existing saved manual heights remain selectable by turning content fit off.
