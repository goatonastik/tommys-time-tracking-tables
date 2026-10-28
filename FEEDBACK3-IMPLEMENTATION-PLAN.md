# notes3 implementation plan — ready for Sol

Date: 2026-10-27. Baseline: published 0.3.0 preview. Scope: all seven paragraphs of notes3.txt. Planning only; do not alter application source or publish a release until implementation is requested. Preserve original feedback. No cross-version compatibility project is required.

## 1. Fix clipped time before changing density

Current evidence: TimerCard uses a 24-DIP minimum readout, but the Viewbox also has 10 DIPs of vertical margin. Horizontal MinHeight uses target + padding, omitting that margin and potentially the play button's measured height. Vertical layout combines hand-estimated optional heights with a star-sized time row, and splits the footer into two narrow columns. ClipToBounds masks overflow. Existing tests assert readout height, not that all drawn digits are inside the visible card. These are source-supported suspects, not a visually reproduced root cause yet.

- First reproduce list clipping at 100% and below, and grid clipping near 50%, with rendered and shown-window tests using isolated data.
- Replace independent height guesses with shared measurement of the actual composed content: readout ink/desired bounds, margins, play button, footer, rounded total, alerts, border and padding. Include full font/segment drawing extents, not just nominal font size.
- Keep a readable digit floor and grow the card when necessary; never solve clipping by cropping, hiding time or shrinking it beneath the floor. Long duration formats may require horizontal scrolling of the workspace.
- Invalidation must cover changing digit count, fonts, totals mode, alerts, title/description, style and viewport. Avoid measure/refresh feedback loops and card recreation on every tick.
- Verify that computed minimums correspond to actual arranged bounds after final layout. Check all digits and separators, not only ActualHeight.

## 2. Compact grid composition and useful space

User refinement during planning: margins should not exist by default; use timer space as efficiently as possible and minimize whitespace. This supersedes any implication below of retaining decorative card padding.

- Remove default internal outer padding, readout top/bottom margins, rounded-total margins and decorative footer gaps in workspace cards. Use zero as the default; retain only measured clearance required for complete drawn glyphs, non-overlapping controls, focus cues and usable click targets. Glyph ink extents are part of measurement, not decorative whitespace.
- Preserve explicit custom padding as an appearance option, but use a zero-padding default/reset style. Do not globally zero every WPF margin: settings forms, toolbar groups and intentionally customized styles have different requirements.
- Apply the same no-decorative-padding default to built-in time-only/pop-out styles (currently Padding=8), and remove the forced 4-DIP pop-out inset when padding is zero. Keep window chrome out of this calculation. Rounded card corners must not clip edge glyphs or hit targets: measure against the actual rounded clip or reduce default corner radius where necessary, rather than concealing the issue with a large inset.
- Pack time, rounded total, identity and action rows closely, with no spacer rows or arbitrary minimum vertical gaps. Remove the readout's current 5-DIP top and bottom margins rather than adding height merely to accommodate them.

- Introduce explicit list, regular-grid, compact-grid and pop-out layout modes (instead of one horizontal boolean serving all presentations).
- Compact grid: play + complete time; rounded total when enabled; title/description; one horizontal action row underneath. Give the footer full width instead of squeezing actions into half the card. Breakpoint comes from measured space, not a hardcoded percentage alone.
- Preserve existing title/description visibility preferences and context-menu access. Long descriptions may wrap or ellipsize with full text in tooltip/history; time must never be truncated. Keep active alert actions accessible and accounted for in sizing.
- Regular grid: reduce default height and unnecessary vertical gaps; make the time row content-sized or bounded instead of absorbing unlimited star space. Avoid allocating replacement empty space elsewhere.
- Add an explicit Fit card height to content setting, on by default. Measure height from the requested width, desired digit size and visible rows. With fit enabled, the saved manual CardHeight is retained but inactive; turning fit off restores manual height. Enabled individual per-timer dimensions remain explicit overrides, clamped to readable minimums. Avoid a magic zero-height sentinel or guessing whether a stored 250 was intentionally selected.
- Update persistence validation and the Settings height slider's current 220-DIP lower limit to permit compact manual sizes, with measured content minimums enforced at layout time. Empty grid rows use the same effective sizing policy. Decide final defaults from before/after renders; a default 100% workspace should fit more rows than 0.3.0 with full digits visible.
- Existing explicitly stored style padding remains editable; provide a clearly labeled compact/reset-default-style action so the user can apply the zero-padding appearance to a saved workspace without deleting timers. Do not silently rewrite all custom presets or user data during installation. This is current behavior configuration, not a compatibility migration project.
- Preserve separate pop-out/time-only behavior, native minimums, independent geometry and right-click controls. Do not impose the workspace footer on minimal pop-outs.

## 3. Two-column Settings

- Extract the current long Settings_Click construction into a dedicated settings dialog with a draft model and grouped sections.
- Normal-size window: two balanced columns, roughly 760–840 DIPs overall, constrained to available work area. Use approximately 200–240-DIP fields for Exit decision, Round to nearest, Total display, Grid columns and Minimum grid rows rather than stretching them across the window.
- Group session/saving/application scale/startup sound on one side and totals/layout/dimensions on the other; use section headings and consistent label/control spacing.
- Reflow to one column on narrow work areas; scroll the body with Apply/Cancel/Reset reachable. Do not squeeze labels or numeric fields to achieve a square outline.
- Preserve existing validation, live app-scale preview and Cancel rollback, reset-as-draft behavior, and the explicit immediate-persistence note for name-suggestion management.

## 4. Consistent accent controls

- Define a shared accent brush using the existing Add timer color #53D9B0 and reference it from that button, checkbox indicators and slider thumbs.
- Style the indicator/knob, keeping text readable. Cover checked/unchecked/indeterminate, hover, pressed, keyboard focus and disabled states; preserve keyboard, mouse, dragging, click-track and automation behavior.
- Apply consistently to scale controls, settings, appearance/style editors and alert-volume sliders. Preserve high-contrast usability; do not rely on color alone to indicate checked state.

## 5. App-wide optional startup sound

Assumption: this is an application preference, independent of which workspace is selected. Disabled by default. It plays once after successful startup and recovery prompts, never on workspace switches, timer starts, wake-from-sleep, opening pop-outs or saving settings.

- Add a small StartupSoundSettings record stored in the existing app_options table (like backup policy), including Enabled, managed custom path/display name and volume. No timer or elapsed-time data changes.
- Settings includes enable switch, Choose WAV/MP3, filename, volume, Test and Stop preview. Require a valid selected sound to enable; selection and preview do not enable startup playback automatically.
- Reuse managed WAV/MP3 decoding and output ownership. Extract a neutral one-shot playback entry point so startup does not need a fake timer or alert state. Give startup and preview separate identities; neither interrupts a timer alert.
- Validate on selection; copy into managed assets only on Apply. Cancel leaves preferences and managed assets unchanged. Keep imported file if the original file is moved/deleted. Stop preview on dialog close and dispose all playback on shutdown.
- Missing/corrupt sound or unavailable output must not block launch: skip sound with one nonmodal, dismissible notification. Do not silently replace a chosen startup sound with an alert chime.
- App-wide preference remains outside portable single-workspace exports, as backup policy already does. Full-database backups retain its reference, with the same existing local asset-storage limitations. Do not claim single-workspace archives transport this new global option.
- Persist global sound and launch preferences safely before replacing in-memory settings; handle write failure without partially presenting successful Apply. Prefer one store transaction for the dialog's persistent preference changes.

## 6. Compact, aligned toolbar and size panel

- Remove the permanently visible Timer size/Default pop-out size row. Add a square size button at the right of the top command group, using a vector clock with a larger ghosted clock and growth arrow.
- Button opens a keyboard-accessible anchored size panel containing the existing sliders, editable percentages, 25% step controls and link toggle. Live updates retain existing debounced saving. Escape/outside-click closes; focus returns to the invoker. Constrain panel to the working area and prevent duplicate instances.
- Preserve link/unlink semantics and default-pop-out-size semantics: future user-created pop-outs use the default; existing windows are not resized and restored windows retain saved geometry.
- Make grid/list toggle an equally sized square icon button; tooltip and accessible name say the action (Switch to grid/list). Keep current layout discoverable.
- Use shared column sizing: workspace selector/actions above; Export, Settings, Appearance and Backups evenly filling that group's width below. Place matching layout and size squares at the right, with common heights, baselines and gaps. Add timer continues to span the group height.
- On narrow windows wrap/reflow whole groups; do not cause overlap or off-screen controls. Keep the high-app-scale reset escape.
- Move scale wiring out of assumptions that XAML slider fields always exist. Refresh an open panel on settings changes and close it on workspace replacement/exit; flush pending scale saves through existing transition rules.

## Implementation order and acceptance

1. Reproduce clipping; add actual visible-bounds checks; implement measured sizing and grid composition together.
2. Compact toolbar and size panel, preserving scale/transition behavior.
3. Dedicated responsive Settings and shared accent resources.
4. Startup sound preference, managed import and one-shot playback integration.
5. Run build.ps1 with isolated TIMERSAPP_DATA_DIR; use Claude read-only through review-claude.ps1 for substantive changes, fix confirmed findings, then final regression and visual review.
6. Update documentation/checkpoint. Target 0.4.0-preview packaging after implementation/validation; rebuild and smoke-test installer before publishing, never reuse a stale binary. Preserve GitHub history and prior releases.

Validation matrix:
- Grid/list at 25%, 50%, 75%, 100%, 125%, 150%, including observed failure ranges; minimum and large card settings. Text and seven-segment; exact/rounded/both; H:M:S and longest supported format; increasing hour/day digits; several representative font ascenders/descenders.
- Check transformed time drawing bounds against all ancestor clips after layout, with a small pixel tolerance; render and inspect examples. Include bottom segments and punctuation, play button, alerts, popped-out marker and wrapped footer. Retain digit floor checks as a secondary assertion.
- 480-DIP and normal desktop widths; 100/125/150/200% DPI where available; high app scale; list/grid switching; auto/manual columns; individual sizes on/off; no layout oscillation.
- Settings two/one-column layouts, Apply/Cancel/Reset, long labels and accessible keyboard order; checkbox and slider focus/disabled states.
- Size panel open/close, percentages, steps, link/unlink, workspace transition, debounce and no-resize of open pop-outs.
- Startup disabled/enabled, once-only launch, restore-off session, no replay on workspace switch/resume, cancelled selection, retained copied asset, malformed/missing file, device failure, preview cleanup and concurrent timer alert. Fake audio for deterministic checks; real audible playback separately.
- Regression: timer history and daily rounded exports unchanged; all existing checks pass or are updated only where requested layout behavior intentionally changed.

No blocking user questions. Existing limitations remain: simulated native pointer input is blocked, and this host is Windows 10; do not report clean Windows 11, physical audio or mixed-DPI manual testing as completed without performing it.

## Read-only Claude planning review and resolved details

Review completed: artifacts/claude-reviews/20261027-171424-679/review.md. Source review only; no tests or implementation run. Claude received the original plan before the user's subsequent zero-margin refinement; its suggestion to merely cap padding is superseded by that refinement.

- Confirmed omitted play-button height and actual-height feedback risk. Size the play button from requested dimensions/digit target, not the resulting ActualHeight. Choose workspace layout mode from requested width and measured content, with stable thresholds; one sizing owner writes minimums. Pop-outs still respond to available client geometry but must settle after constraints. Add a bounded-layout-pass assertion.
- Confirmed saved CardHeight and 220-DIP clamp undermine density. Fit-to-content policy above addresses this without the suggested migration that guesses whether old default values were intentional.
- Use an in-window overlay for the size panel, with explicit app-scale handling and working-area constraints. It must remain usable at 150–300% app scale. Update tests that previously found persistent XAML ScaleInput fields; initialize panel from current settings on every open. Use a dedicated link ToggleButton with accessible state so global checkbox styling does not add redundant visual chrome.
- Fresh 25% pop-outs and restored small windows must grow only to the complete-content minimum. Minimum size wins over requested geometry; verify full visibility and stable layout. Zero decorative padding does not remove necessary native chrome or full glyph bounds.
- Define audio ownership: timer alerts, settings previews and startup one-shot are separate. Workspace replacement stops timer alerts and closes transient UI; it does not restart startup sound. Suspend and application shutdown stop all playback; startup never replays on resume. Preview stops on dialog close/selection change and explicit Stop.
- Startup section explicitly says Applies to all workspaces. It saves only on Apply, not during selection. Cancel preserves it; workspace Discard does not roll back an applied global option. Workspace Reset to defaults does not alter global startup sound; provide its own disable/reset control. Add a store transaction API for launch-preference snapshot and startup app_options writes so failure does not leave half-applied persistent preferences.
- Accent checkbox uses a dark check on mint, a visible unchecked outline and focus cue. Add system-color resources/triggers for these newly styled controls in high-contrast mode; do not claim whole-app high-contrast compliance.
- Implement toolbar alignment with shared Grid/UniformGrid sizing, replacing independent WrapPanels for the paired rows; explicit narrow-width reflow remains required.
- Bounds assertions need rendered evidence: use deterministic digit/segment fixtures and transformed drawing extents, and inspect bottom segments/punctuation in screenshots. A minimum-height assertion alone remains insufficient.

Planning complete. Next step: switch to Sol and implement this plan. Application source, database and release remain unchanged.
