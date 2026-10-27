# notes2.txt — implementation handoff

Prepared 2026-10-27 against the completed v0.2.0 source. Planning only: no application source, package, database, or release changed. Keep C#/WPF/.NET, SQLite and existing workspace lifecycle. This document supersedes notes1 design decisions only where notes2 changes them. Target next preview: 0.3.0 after verification.

## Verified baseline and diagnosis

- MainWindow.xaml nests command/scale WrapPanels; Export shares the workspace group. The logo occupies the left slot. Replace the header structure rather than tuning margins.
- Render constructs the same TimerCard for list and grid, gives list a fixed width, and honors saved per-card sizes in both modes. TimerCard does not receive list mode. Its single vertical composition and unconstrained Viewbox explain wasted list space and unreadably small time at low heights.
- TimerGridPanel measures children at infinity; columns are updated from viewport width around Render, with another update only on ScrollViewer SizeChanged. Recompute after mode/viewport changes, including scrolling changes, rather than relying on a window resize. Reproduce the reported stale layout before claiming the exact event-order cause.
- PopoutScale currently transforms content on every Refresh; it is not a starting-window-size preference. PopOut always reads stored geometry. Changing labels alone would not meet the request.
- AlertAudio uses WPF MediaPlayer. Replace this dependency to address the reported Windows Media Player requirement.
- ExcelExport currently writes day/month subtotals and four sheets. Reports.Build splits segments at midnight and clips to date ranges while preserving measured duration. Preserve that reliable allocation logic beneath a new export projection.
- generate-icon.ps1 draws text at negative offsets. Rebuild frames with measured ink bounds and safe padding; inspect actual 16/20/24/32 pixel rendering.

## 1. Header and sizing controls

Use an explicit two-row command grid, with Add timer on the far left spanning both rows. Remove the large T⁴ badge. Row one holds the labeled workspace selector and New/Save/Rename/Duplicate/Delete. Row two holds Export, Settings, Appearance and Backups. Give rows real measured heights; at narrow widths wrap each group without overlap or inaccessible commands.

Use the requested label “Workspace style” above the saved-workspace selector; this remains the existing named-workspace catalog, not a new style-only preset system. Make the selector shorter vertically and center its selected text with a proper content alignment/template, not offsets.

Place a left-aligned sizing strip below the command rows: Timer size, chain-link toggle, Default pop-out size, and grid/list toggle. Keep editable percentage/step controls and sliders. Move Application scale into Settings with a preview and a reachable Reset to 100% action. At the largest application scale, Settings and reset must remain reachable.

- Timer size changes workspace grid/list card dimensions, with readable minimum dimensions applied after scaling. Do not multiply app scale into detached windows.
- Default pop-out size scales the initial native window dimensions, not the contents of already-open windows. Remove its live content transform. Opening an already-open pop-out only focuses it.
- Proposed placement precedence: restoring windows on startup retains saved geometry; a fresh user pop-out uses the current default size and remembered usable location. Subsequent manual resizing stays local to that window. Use an explicit creation reason so restoring is not accidentally treated as a fresh opening. Clamp initial size/location to the monitor work area and WPF device-independent units.
- Link defaults off. When enabled, Timer size drives the default pop-out percentage; disable direct editing of the linked value. Preserve the independent pop-out value for unlinking. Changing the link or either slider never resizes an existing pop-out.
- Add Allow individual timer resizing in Settings, off for new and upgraded workspaces. Hide/disable resize handles when off; ignore but retain old per-timer dimensions so reenabling recovers them. This option does not disable normal resizing of detached windows.
- User clarification: cross-version compatibility is out of scope for this pass. Do not build older-version migration, downgrade support or compatibility branches. Take a consistent backup before changing stored settings, preserve current timer history, and keep current save/load and archive behavior internally consistent. Add the new fields/defaults directly; choose a schema bump only if the new format requires it, not as a separate compatibility project.

## 2. Distinct timer presentations and layout ownership

Introduce an explicit presentation mode (Grid, List, Popout, Preview) and a small shared timer command/readout component. Separate layout construction from the 100 ms value refresh. Keep a single timer engine and command implementation; do not duplicate start/stop or alert behavior across views. Extract sizing rules into testable helpers and let the container own grid/list geometry.

Grid: time dominates the center; name and description appear bottom-right as “Title — description”. Use inline runs so the description can wrap below the title line. Preserve title emphasis, visibility preferences, accessible full text and tooltips. No dangling dash for an empty/hidden description.

List: use measured columns in this order: play/stop plus time, rounded decimal-hour total, title/description, action buttons. Fill available horizontal space instead of using a fixed 580-unit card. Hide description first at narrow widths; reserve space for the full remaining title, current time format, total and buttons. Below that content-dependent minimum, allow horizontal scrolling rather than shrinking/clipping the required content. Handle long titles/formats deterministically; no global minimum-window-width explosion.

Both: green triangle when stopped, red square when running, directly beside the leftmost time digit, centered and sized to the digit row. Remove Start/Resume text and RUNNING/STOPPED labels. Keep accessible names/tooltips, keyboard activation, and separate popped-out/alert indicators. Keep existing time-only pop-out style choices and right-click menus; hidden controls must remain accessible there.

Use minimum readable digit height (initial target 24 DIPs before app/DPI scaling) and measured padding/controls to derive card minimum height. At small grid heights, simplify optional content before shrinking digits; scrolling is preferable to unreadable digits. Validate long y:d:h:m:s:ms layouts, regular fonts, 7-segment, large padding and explicit style visibility. Do not rebuild layouts or recalculate stable geometry on every clock tick.

On list/grid switch, remeasure against the actual viewport after rebuilding cards; invalidate the panel when columns, spacing or dimensions change. Observe viewport-width changes, not just outer window SizeChanged; coalesce queued layout updates to avoid loops. Reset scroll to the top on mode switch. Auto-grid fits as many columns as readable card sizes allow; manual columns/rows still apply. “Put all timers in a grid” means immediate correct arrangement, with vertical scrolling if necessary, not shrinking every timer to fit one screen.

Rounded display: share one decimal-hours formatter between UI and export. Fifteen minutes becomes 0.25, thirty 0.50, one hour 1.00; label/tooltip identifies hours and rounding interval. Use numeric duration rounding first, then decimal formatting, midpoint upward consistent with current behavior. Keep existing 1/15/60-minute UI preferences and Exact/Rounded/Both; decimal formatting must not alter measured time. Rounded-only shows decimal hours as primary total; Both keeps normal time plus decimal hours. Preserve custom duration formats for the exact readout.

## 3. Export dialog and report projection

Extract export selection into a small dialog/options model rather than expanding MainWindow.Export_Click. Four exclusive modes:

| Mode | Visible filter | Meaning |
| --- | --- | --- |
| Export all | none | All recorded history for all timers, including archived |
| Only today’s | none | Time falling within today's local calendar day |
| Date range | inclusive start/end dates | Time falling within the selected local dates, all timers |
| Custom | timer selection | All history for selected timers, including selectable archived timers |

Default to Export all. Date fields appear only in Date range, timer selection only in Custom. Hidden stale filters must not affect the result. Require a valid ordered date range or at least one custom timer. Keep Include running snapshots. Remove Export all timers checkbox and obsolete daily/monthly basis and summary-display selectors. This interprets Custom literally as selected timers across all dates; combined timer/date filtering is not requested in notes2.

Write one visible time table (XLSX and equivalent CSV), with these columns in exactly this order:

1. Total time — numeric Excel duration formatted [h]:mm:ss (hours can exceed 24).
2. Daily total (rounded hours, nearest 15 min) — numeric decimal hours, format 0.00, populated once per timer per calendar day.
3. Timer name.
4. Timer description — width exactly 45 Excel units, wrapped.
5. Date started — actual recorded start date, format mmmm d, yyyy, e.g. January 31, 2026.
6. Actual start — recorded local date/time, preserving enough date/time precision to understand overnight segments.
7. Actual stop — recorded local date/time; blank for running snapshots.
8. Status — stopped/running snapshot, recovery/clock-adjustment qualifications and selected-range qualification when applicable.
9. Segment ID — first eight lowercase hexadecimal characters, display only.

No daily/monthly subtotal rows or daily/monthly tabs. Replace the old Export information sheet with concise worksheet comments on headers/status for time zone, measured-through time and partial-range explanation. Keep the requested first eight columns, appending Segment ID because notes explicitly asks to shorten it.

User confirmed: round each timer's daily total, not each segment. Retain Reports.Build daily portions: one detail row per segment per included local calendar day. Sum exact allocated ticks by (TimerId, Day), round that sum once to the nearest 15 minutes (midpoint upward), and write decimal hours only on the first detail row of that timer/day group. Leave other rounded cells blank, not zero. This keeps the rounded column summable without subtotal rows or sheets. Sort by allocation day, timer name/full ID, then portion start/full segment ID. Three 5-minute segments on one day contribute 0.25 hours once. Distinct timers and days are rounded separately, even if names match. Include a header comment and a status note on the populated row explaining that its rounded value covers all entries for that timer and day. CSV carries the same note and blanks. Explain that filtering individual segment rows may exclude the row holding the daily figure; rounded figures are daily groups, not segment allocations.

Exact duration remains each row's selected daily portion; summing these rows preserves measured running time. A segment crossing midnight appears on each applicable day with the same segment ID and original start/stop timestamps. Date started remains its actual original start date; Status identifies the allocation date and partial-day/selected-range portions, so the row is not mistaken for a duplicate full segment. Keep Actual start/stop untouched and stop blank for running snapshots. Captured segment name/description stays historical. Snapshot active timers once before constructing the report. Today/range filtering selects included local days before daily aggregation. Quarter-hour export rounding is fixed at 15 minutes; existing app rounding preferences remain independent.

Keep full GUIDs internally and in backups. Eight-character labels are not unique keys: detect prefix collisions in a workbook, keep eight characters in the visible column, and include the full ID in a cell comment for disambiguation. CSV has no comments; status must flag a short-ID collision and provide the full ID there when necessary.

Center and wrap header text. Fit column widths to header/data (not only the first 200 rows), excluding the fixed description column; apply sensible limits for pathological text and wrap it. Fit row heights after widths/wrapping, including multiline descriptions. Interpret “resize rows” as both column fitting and row-height fitting. Do not rely on Excel opening the file to fix clipped text. Verify the generated workbook with long descriptions and headers. Preserve atomic output replacement and CSV formula-injection protection; data cells remain numeric where appropriate in XLSX.

## 4. Audio replacement and sound choices

Use an MIT-licensed NAudio output backend with NLayer managed MP3 decoding, shipped in the app. WAV decoding must handle supported PCM/IEEE-float WAV without WMP; MP3 must explicitly select NLayer, not NAudio's default Windows codec path. Select and pin a mutually compatible stable package set during implementation: current upstream has API differences across NAudio major versions, so do not blindly copy older WaveOutEvent snippets.

Official references checked during planning:
- https://github.com/naudio/NAudio (MIT license and playback backends)
- https://github.com/naudio/NLayer (managed MIT MP3 decoder and integration)
- https://github.com/naudio/NAudio/blob/main/Docs/ConvertMp3ToWav.md (explicit managed decoder)

Keep AlertAudio as the lifecycle boundary with an injectable output factory for tests. Maintain independent playback per timer, volume, repeat, stop, dismiss/snooze and disposal. Guard late callbacks after Stop/Dispose, marshal errors to the UI thread, and avoid leaking test sounds on dialog close or cancellation. Repeated failures must not generate a message every tick.

Provide a sound dropdown with generated built-in chime(s), discovered local Windows notification/error WAV files, and Custom WAV/MP3. Discover from the machine's Windows Media folder; do not assume Windows 11 filenames or bundle Microsoft's audio. Existing built-in generated sound guarantees a fallback when the folder/sound is absent. Store a system-sound reference separately from imported managed assets, with fallback on another machine. Archive/import handling must not silently bundle OS sounds as app-owned files. Include third-party license notices in installer and portable package.

Limit new custom imports to WAV/MP3, validated with the decoder before accepting. For any previously selected WMA/M4A file, show a clear unsupported-format message and allow replacement with WAV/MP3; do not build a legacy playback path. Custom files continue to be copied into managed assets. No external media-player installation or codec download.

## 5. Icon

Regenerate every ICO frame using measured T and superscript 4 ink bounds with explicit safe margins, replacing negative offsets. Inspect the tiny frames at native size and enlarged for clipping, and the titlebar/taskbar/installer shortcut. Set icon consistently on main and detached windows. Keep the T⁴ brand in titlebar/icon even though the large header badge is removed.

## Implementation order and completion gates for Sol

1. Capture the v0.2.0 baseline and add isolated regressions reproducing layout-switch, export and sizing behavior. Read AGENTS.md and this plan; preserve notes2.txt.
2. Add sizing/presentation/export/sound-selection models and current-format persistence. Cross-version compatibility is out of scope. Keep catalog/backup/exit semantics intact.
3. Implement header/settings and explicit grid/list layouts, controls, readable minimums and initial-only pop-out sizing. Update style preview and existing tests that intentionally reflect superseded behavior.
4. Implement shared decimal formatter, four-mode export UI and new report projection/writers. Test workbook structure, values, formats and heights, plus CSV.
5. Replace audio, add sound catalog, legacy handling and notices; regenerate icon.
6. Run build.ps1 and all existing relevant checks plus new meaningful regression coverage. Render normal/narrow/high-scale header, grid/list, long-title list, compact/normal pop-outs, export dialog states and icon frames. Test scroll-down list→grid without resizing, link changes with already-open pop-outs, and resize toggle persistence.
7. Read-only Claude code review using review-claude.ps1, resolve confirmed substantive findings, rerun affected checks. Test audio with real WAV/MP3 and no WMP installed, concurrent alerts, repeated preview, snooze/dismiss, missing devices/files, and app closure. Automated decoder tests are not proof of audible playback; report inaccessible live tests honestly.
8. Version 0.3.0, build portable/installer, isolate install/upgrade/data-retention checks, then publish through the established GitHub path with checksum after validation. Preserve local/remote history; no force-push. Never launch tests against the user's normal database.

No implementation started in this planning pass. Existing 51 passing checks are the prior baseline, not evidence for the new behavior. The handoff includes the confirmed daily rounding decision; no planning questions remain.

## Read-only Claude review and final refinements

Completed review: artifacts/claude-reviews/20261027-041054-268/review.md. The first attempt failed with a connection-refused error; the network-enabled retry completed with tools disabled. Findings were checked against source. These refinements take precedence over less-specific wording above.

- Dimension scaling: replace workspace-card LayoutTransform with explicit requested dimensions and measured minimums. Keep Timer size at 25–300%, but show “Minimum readable size reached” when the effective dimension is clamped. Test actual digit height at 25%, not just the card's requested Height. App scale is a separate UI zoom.
- List layout must receive finite viewport width explicitly, converted to the container's coordinate space. Its width is max(viewport width, measured required content width). Remove the fixed 580-width assumption. Start the compact list-row default at 72 DIPs, with a 48–300 requested range and a content-dependent effective minimum; update Settings sliders and Decode clamps together. Settings may request a smaller height than needed; the readable content floor wins.
- Grid layout must ignore retained individual Width/Height for both card sizing and column calculation when individual resizing is off. Switching that setting updates the grip and geometry immediately. Keep the existing explicit destructive reset action, but label it “Forget saved individual timer sizes” so disabling resizing itself never clears them.
- Pop-out creation reasons must distinguish user opening, startup restore and workspace/recovery restore. The selected rule remains fresh user openings use the current default; startup/recovery restore uses saved geometry. Claude suggested retaining manually resized sizes on every reopen, but that would weaken the requested default-starting-size behavior. Do not add another persistence flag for that alternative. Remove both the live transform and scale-divided breakpoint calculations.
- Play/stop glyph size follows the measured digit band, with a minimum 32-DIP hit target; do not scale away the hit target inside a Viewbox. Hidden-controls/time-only styles continue to hide it. ShowStatus governs alert/popped-out indicators, not the removed running/stopped label; update the style editor wording. Grid: time/control band in the main area, rounded total directly below, auxiliary actions bottom-left, title/description bottom-right, alert actions in a measured temporary row. Detached windows use content-dependent minimum geometry; workspace scrolling cannot solve pop-out clipping.
- Header's two command rows are Auto-sized. Internal wrapping increases those row heights; Add spans their combined height. Sizing strip is a separate row. Keep the header vertically reachable at high app zoom with scrolling if needed. Previewing Application scale in Settings must restore the old value on Cancel; Reset to 100% participates in the same draft/Apply behavior.
- Export all means the current workspace. Say so in the dialog, include a filename-safe workspace name in the default filename, and put it in the header metadata. Merge portions by (TimerId, SegmentId), never by shortened IDs. CSV exact time uses invariant HH:MM:SS with total hours, rounded hours uses invariant 0.00, and timestamps retain date plus offset. XLSX remains numeric. Same-name timers may be visually indistinguishable in the requested columns; full IDs remain in metadata/comments.
- User confirmed daily rounding after the Claude review: use the revised daily-portion projection above. Keep portion identity as (TimerId, SegmentId, Day), superseding the earlier merge-by-segment instruction, and rounding groups as (TimerId, Day). Test three 5-minute segments producing one 0.25 value, distinct same-name timers, overnight segments, multiple days, selected ranges, running snapshots, and no duplicated rounded values. No daily subtotal rows or tabs are reintroduced.
- Validate discoverable Windows sounds with the actual supported decoder; exclude/mark unplayable compressed WAVs. Reuse one preview playback ID, stopping it on replacement and dialog close. Define repeat at the decoded-stream boundary, and ensure explicit Stop never restarts it through a late completion callback. Verify dependency notices are actually copied into the published output.
- Cross-version compatibility remains excluded per user clarification. Do not add a legacy archive migration project in response to Claude's compatibility concern. For current-format imports, unsupported sound assets must produce a clear validation result without partial workspace replacement. No new WMA/M4A playback support.
- Update SPECIFICATION.md and README to replace superseded export/layout/scale claims. Rewrite old workbook tests that require removed tabs rather than retaining obsolete behavior. Add stable AutomationIds/test helpers instead of relying on visual-child indexes. Version locations include csproj, installer .iss, build-installer.ps1 defaults and documentation links.
- Claude did not receive generate-icon.ps1 in its snapshot. The negative-offset diagnosis was independently verified directly from that file during this planning pass. No live UI/audio or new-code tests were run; this is a planning review.
