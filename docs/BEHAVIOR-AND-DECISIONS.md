# Behavior and user decisions

This records intentional behavior at the 0.5.3 baseline. Consult source, SPECIFICATION and original feedback if changing a contract.

- Multiple independently named count-up timers, optional descriptions and name suggestions. Only running segments contribute elapsed time; history exposes exact system timestamps and segment durations.
- Optional workspace setting **Only allow one timer to run at once**, off by default. Starting another stops the previous at the same captured time. Enabling it while several run retains the most recently started. Sleep/recovery must obey it too.
- Sleep time is excluded by default. Recovery offers choices; do not count interruption time unconditionally.
- Quarter-hour rounding is default, with configurable display rounding and exact/rounded/both display. Calendar dates group spreadsheet output. Elapsed months are excluded because their length is not fixed.
- Export rounds each timer's daily aggregate once, not each segment. Three five-minute segments in one day contribute fifteen minutes before rounding. The spreadsheet puts rounded daily hours on the first timer/day row and leaves subsequent cells blank. Filtering away that row can hide its total. No extra summary sheets/subtotal rows are currently emitted.
- Overnight portions keep original timestamps while identifying the allocation day. Test leap days and 23/25-hour daylight-saving days before changing date allocation.
- Named workspace selector and New/Save/Rename/Duplicate/Delete actions. Switching stops departing timers; duplicates are stopped and preserve history. Deleted workspaces are recoverable.
- Autosave/crash recovery is enabled by default. Closing normally offers Save/Discard/Cancel; Remember this decision starts unchecked. Discard means restore the workspace-entry state.
- Name-history collection can be disabled; this does not erase suggestions already collected. Existing suggestions can be removed.
- User prioritizes efficient space: minimal margins, thin list rows, minimal whitespace, readable complete digits. Defaults use zero decorative padding; custom styles retain intentional padding. List gap is independent of grid gap, and content-fit height is default.
- App scale and timer scale are independent. Current timer/default-pop-out size controls are under the growing-clock button; app scale is in Settings. Whole-card scaling below 65% is intentional.
- Pop-outs share timer state, show a return indicator in the main entry, support context menus even in minimal views, and can use separate styles. Clicking the badge or inward teal arrow returns the timer.
- Remembered pop-out size and location are independent; each can be per timer or shared in the workspace. Reuse enabled saved values every reopening. Default-size sliders apply when size memory is absent/off. The first stable opening initializes empty memory.
- Styles support seven-segment or regular text/fonts, custom unit layouts, title/description/total visibility, backgrounds and presets. Background fitting/anchors/crop are user choices; do not impose a single fit mode.
- Interval color/flashing/audio alerts are disabled by default; custom audio and snooze/dismiss are available. Manual stop cancels snooze. Sleep/resume can preserve snooze for a resumed timer; stopped timers must not replay alerts.
- Startup audio is disabled by default and app-wide. Settings Apply commits it; workspace Discard does not undo it. Missing files produce notification rather than startup failure.

## Collaboration preferences

The user has preferred Astra for planning and Sol Medium for implementation. This is a preference, not a requirement to stop work or a guarantee that those models exist in a future account. Claude provides independent read-only review through the project script; it must not implement changes or receive action tools. Verify reviewer claims before acting.

Signing and Store submission remain paused. The user previously wanted trusted signing plus Store distribution, but identity/account setup and certificates remain unresolved. Do not resume paid setup or claim certification based on build/tests/Defender alone.
