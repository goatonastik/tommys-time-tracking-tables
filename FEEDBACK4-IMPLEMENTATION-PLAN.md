# notes4 implementation plan

Date: 2026-10-27. Baseline: published 0.4.0 preview, remote source c80d25a525a6f87a06eb16fd43ecf9947d21b9b1; local implementation 50949f0. Planning only. Preserve notes4.txt. This plan covers every paragraph, including the user's confirmation that remembered pop-out values apply whenever a window reopens. No cross-version compatibility project is requested.

## 1. Align the two-row toolbar and add labeled spanning squares

Current source: MainWindow.xaml places the four commands and two 40-DIP icon buttons together in the second row. The workspace/action row and command row have different right edges.

- Use one shared header layout: Add timer at the left; a central two-row group; two square buttons at its right spanning both rows.
- Central top row: Workspace style selector, New, Save, Rename, Duplicate, Delete. Bottom row: Export, Settings, Appearance, Backups, all equal width and height. The bottom group's right edge aligns exactly with the Delete button's right edge, and its left edge aligns with the workspace section.
- Right-side squares: Layout (grid/list icon plus action label such as Grid view / List view) and Timer size (growing-clock icon plus label). Each square's side equals the measured total height of the two central rows, including their inter-row gap. Both span the two rows. Keep Add timer aligned to that same full height.
- Use shared Grid columns/row measurements, not independent WrapPanels or width estimates. Keep labels readable; icons and labels both centered. Buttons retain tooltips and accessible names/states.
- At narrow widths or high application scale, preserve the central two-row block's alignment and move the two squares together to the next line when required. If the central block itself cannot fit, permit scrolling instead of clipping commands. Do not force the four equal commands into unequal widths or let only Delete wrap away from its row.
- Existing Export action and export table behavior remain. Interpret the opening fragment “I want export” in notes4 as referring to the Export/Settings/Appearance/Backups button group, since export already exists and no export behavior change is described.

## 2. Continue shrinking the entire timer below 65%

Current source: TimerCard clamps digits to at least 24 DIPs, play/stop to at least 32 DIPs, grid width to at least 150 and list width to at least 250. ConfigureWorkspace then clamps the requested card against those minima. This explains why the size slider stops visibly shrinking parts of a timer.

User's new requirement overrides the previous rule that rendered digits must never fall below 24 DIPs at every scale. Retain complete drawing bounds; allow intentionally smaller rendered text and controls below the threshold.

- Use 65% as a stable initial transition, matching the user's observation. No extra threshold setting is necessary.
- Separate logical layout size from final on-screen size. At requested scale s >= 0.65, keep the dimension-based layout behavior. At s < 0.65, arrange the entire timer at its 65% logical layout, then uniformly scale that complete visual by s / 0.65.
- Apply the uniform factor to the complete card: background/image, padding, rounded outline, time, play/stop, rounded total, name, description, status/badge, action buttons and resize grip. Do not keep the controls at their former physical minimums while only shrinking digits.
- Introduce a small presentation wrapper (e.g. TimerCardPresenter) to own this transform and report transformed desired size to TimerGridPanel. Keep TimerCard's logical measurement independent of its rendered dimensions. Do not multiply TimerScale twice or accidentally apply AppScale twice.
- The visual should be continuous at 65%; a 50% card is 50/65 of the 65% card in both dimensions, and a 25% card is 25/65. Grid column counts and row heights must use final transformed sizes so more cards fit as they shrink.
- In list view, uniformly scale the whole row below the threshold, including its width. Its logical layout is the 65% row using the available viewport; it becomes a smaller aligned row, not a row with selectively shrunk contents. Above the threshold, list rows continue to use the available width.
- Custom/manual card sizes and fit-to-content height still determine the logical card. Convert resize-drag deltas through the effective transform correctly before storing logical dimensions. Adjust the existing per-timer height/width clamps only where required for this conversion, without changing the meaning of saved dimensions.
- Context menus/tooltips remain ordinary readable UI outside the scaled scene. Preserve correct hit-testing, keyboard focus, hidden-control access and pop-out/return actions at every scale. The intentionally smaller click targets are part of the user's requested whole-card scaling.
- Keep the current 25–300% slider range. This workspace-card shrink policy does not promise that Windows native title bars/chrome can shrink proportionally. Default pop-out size and saved geometry follow section 5; actual pop-out controls follow section 3.

## 3. Scale pop-out play/stop together with the time

Current source: a vertical pop-out fixes target to 24, derives the play glyph from that target (and caps it at 48), but lets the separate readout Viewbox grow to fill the window. The button therefore stays small while the digits grow.

- Make play/stop and the time a single measured readout band with one fitting scale. Prefer a shared ReadoutBand component used by each layout, rather than deriving the play button font from the window's outer height.
- In pop-outs, compute the common scale from the available client space after measuring visible metadata/action rows. The play/stop symbol, button hit area and digits scale together, preserving their relative proportions in tall and wide windows.
- A joint Viewbox around the intrinsic band is a suitable implementation, provided nested transforms cannot independently resize the time again. Keep minimum native window calculation based on the intrinsic content and chrome, not on the already-scaled glyph's ActualHeight.
- A small vector triangle/square is appropriate for consistent symbol geometry. Retain green play and red stop, accessible names and tooltips. Hidden controls/time-only styles remove the button and its space.
- Respect Fit digits to window: when enlargement is disabled, the button must follow the actual digit scale, not grow independently.
- Preserve stable minimum dimensions, complete visible drawing, context menus and separate pop-out styles. Verify a tall-to-wide-to-narrow resize does not oscillate between presentation modes or repeatedly enlarge the window.

## 4. Return a pop-out from its main-window entry

Current source: the POPPED OUT text is a TextBlock; the outward-arrow button always calls PopOut, which focuses an existing window. Neither is the requested direct return action.

- Make the main-window Popped out badge an actual keyboard-accessible button that calls Return(timer). Separate it from the alert-status text so clicking an alert indicator cannot accidentally close the pop-out.
- Retain a reference to the main card's pop-out action button and update it when the timer detaches/returns. Detached state uses an inward arrow and the app's teal accent, with Return to workspace tooltip/accessibility name. Attached state uses the outward arrow and Pop out timer label.
- Clicking either the badge or the inward-arrow button closes that timer's existing pop-out and returns its presentation to the main window. It must not stop/reset time or create another timer/window. Native close and existing Return context-menu action use the same return path.
- Keep a separate Focus popped-out window context-menu command for users who want to bring it forward without returning it. The direct button now means return, as requested.
- Pop-out windows' own return button should also use the inward arrow/teal convention. Retain the popped-out border/indicator, including styles where only one of status or controls is visible. If both are hidden, context-menu access remains available.
- Update all representations immediately after opening/closing, including styles/presets applied while the window is detached.

## 5. Independent size and location memory

Confirmed by the user: when enabled, saved size/location apply on every reopening. The default-size slider is the fallback when no size has been saved or size memory is off. This intentionally replaces 0.3/0.4's rule that fresh openings always use the size slider.

Add a Pop-out memory section to Settings, using the existing draft/Apply/Cancel model:

- Save popped-out window location; subordinate Save per timer.
- Save popped-out window size; subordinate Save per timer.
- Defaults: both saving options on, both per-timer sub-options on. Sub-options are disabled while their parent is off, but their selections are retained.
- Policies belong to the current workspace's settings. Store shared remembered geometry separately on Workspace, outside the editable Settings draft: moving a pop-out while Settings is open must survive Apply/Cancel. “Shared” means all timers in this workspace use the last remembered value; it is not a new cross-workspace global preference.

Resolve each component independently when opening:

| Component | Saving disabled | Saving on, per timer | Saving on, shared |
| --- | --- | --- | --- |
| Location | Normal visible default placement | This timer's saved coordinates, else default placement | Shared saved coordinates, else default placement |
| Size | Current default pop-out size/linked timer size | This timer's saved dimensions, else current default | Shared saved dimensions, else current default |

- Mixed configurations must work: shared size with per-timer locations, or per-timer sizes with shared location. There is no precedence between components because they are resolved separately.
- Centralize resolution/capture in a PopoutPlacementPolicy helper rather than duplicating branches across PopOut, workspace restoration and settings.
- Represent absence of remembered size/location explicitly (nullable pairs or presence flags). WindowPlacement's existing default Width=390/Height=340 must not make every newly created timer appear to have a user-saved size. Preserve IsOpen and Topmost semantics separately from remembered geometry.
- Reuse one geometry value shape for timer-specific and shared memory; validate finite coordinates, finite positive dimensions and complete pairs. Apply native content minimums and current monitor visibility constraints after resolving the preferred geometry.
- Record only normal-window geometry after initialization. Opening, restoring, default placement and initial visibility correction must not overwrite the shared memory merely because several windows open in sequence. Later move/resize events capture stable actual bounds; coalesce saves through existing autosave/manual-save behavior.
- Initialize missing memory from the first stable opening only when its selected store is empty. Once shared memory exists, record subsequent genuine bounds changes in event order; an unchanged older window closing later must not overwrite a newer shared value. Track last observed bounds/pending capture per component, rather than unconditionally writing every window's bounds on close.
- Disabling an option stops using/updating that memory but does not erase it. Switching shared/per-timer modes retains both stores, so switching back recovers that mode's previous values. Applying policy changes does not move/resize currently open windows or copy one open window into all timers' memories. Subsequent movement/resizing uses the active policy.
- On return/close, capture the final normal geometry using the same policy. On workspace switch/exit, preserve requested open-state and complete save/discard semantics; do not capture into a replacement workspace after teardown starts.
- Flush pending geometry before preparing/saving the departing workspace or exit snapshot, not only in Closed handlers (the existing exit code saves before closing pop-outs). Associate each pop-out session with its owning workspace, and suppress event writes during teardown/replacement so old windows cannot update the new workspace's shared memory.
- Do not save minimized or maximized bounds over a normal size. Continue restoring offscreen windows to a visible monitor; keep mixed-DPI conversions consistent with WindowPlacementService.
- Remembered manual dimensions must not be silently truncated by the existing fixed 1800-by-1400 persistence/opening clamps when a monitor supports a larger window. Validate sane finite dimensions and apply the actual destination work-area constraint on opening; keep the default-size slider's own range separate from remembered manual geometry.
- Update size-panel help text: the default applies when size memory is off or no remembered size exists. Changing the slider never resizes an already-open window.
- Expose a concise Forget saved pop-out sizes/locations action only if needed to make fallback easy to reach; disabling memory already supplies the required fallback. Do not add extra management UI without a concrete usability need.

## 6. Keep list descriptions in the available space

Current source: showDescription requires the entire description to fit after hand-estimated widths (including 145 for buttons and 40 extra). Otherwise it removes the description completely, even when much of it could be shown. Title and description share one inline TextBlock; larger digits raise the estimated required width.

- Remove the all-or-nothing full-description-width gate. Measure the actual readout band, optional rounded-total and visible action buttons; let the metadata column consume the actual remaining space.
- Always include the description when Show description is enabled and text is present. Show the portion that fits, using character ellipsis for overflow and the existing full-text tooltip/history. Hide it only when disabled, absent, or no actual metadata space remains.
- Use a metadata presenter with title and description sharing the remaining width. Keep the title identifiable, but bound very long titles so they cannot unnecessarily consume all available description space. Omit the separator when either field is hidden/empty.
- Preserve a single-line list at ordinary row height. No forced horizontal scrolling solely to fit the complete description; essential time/controls may still establish the row's minimum size. Existing explicit title/description visibility settings continue to work.
- Recompute metadata allocation on viewport/scale/format/style changes and when time gains digits. Use actual measured widths and a bounded update path rather than rebuilding every timer on each tick.
- Coordinate with the whole-card scale wrapper: remaining width must be calculated in logical coordinates, and description visibility must not change solely because the scene is uniformly scaled below 65%.

## Implementation order and verification

1. Reproduce current symptoms with focused isolated renders: toolbar edges; 65/50/25% timer dimensions; large pop-out digits versus play symbol; list description with remaining space.
2. Introduce logical-card presentation wrapper and shared readout band; fix list metadata measurement. These changes share the same sizing responsibilities and should be designed together.
3. Add independent placement policy, explicit remembered geometry and Settings controls; update fresh-open and restore paths.
4. Implement stateful return badge/button and focus context action; build the shared two-row toolbar with labeled spanning squares.
5. Update relevant regression tests and docs; run build.ps1; have Claude review substantive code read-only and resolve confirmed findings. Verify rendered output and shown windows with isolated TIMERSAPP_DATA_DIR.
6. Target 0.5.0-preview only after validation. Build a fresh portable package/installer, smoke-test without touching the user's live app/data, sync source without force-push, verify Windows CI, publish installer/checksum and record release proof/checkpoint.

Acceptance checks:

- At normal width: Export/Settings/Appearance/Backups have equal outer bounds; their group ends at Delete's right edge. Layout/Timer size are labeled squares whose height equals the two-row central group. At 480-DIP width/high app scale, every command remains reachable and no controls overlap.
- Grid and list at 65%, 64%, 50% and 25%: rendered card width/height and visible controls scale proportionally below the threshold; crossing the threshold is continuous. Grid packing uses transformed dimensions. Verify hit-testing/focus/context menu and individual drag conversion. Replace unconditional 24-DIP rendered-floor assertions below 65% with proportional-size plus no-clipping assertions.
- Cover text and seven-segment, three fonts, short/long/custom millisecond formats, exact/rounded/both totals, wrapped metadata, alerts, popped-out badge, custom padding/radius, list/grid switching and app-scale composition. Keep intrinsic drawing-bound checks before the outer scale and transformed visible-bound checks afterward.
- Pop-outs: play and stop visible-symbol bounds grow with digits at multiple client sizes; test wide/tall/narrow, controls hidden, time-only, FitDigitsToWindow off, native minimums and stable layout passes.
- Return via main badge, main icon, pop-out icon, context menu and native close preserves running total/history and closes exactly one window. Focus remains a distinct context action. Badge/icon update without leaving stale detached state.
- Size/location policy matrix: each component off/per-timer/shared, mixed combinations, two timers with different geometry, first open/no saved data, close/reopen, switch/restart restore, defaults/linking, toggling policy without erasing stores, disabled memory, minimized/maximized windows and absent-monitor fallback. Opening several restored windows must not clobber shared memory.
- List metadata: at larger timer scales and adequate viewport, description remains visible; with partial room it ellipsizes rather than disappears; empty/hidden fields have no stray separator. Test long title, long description, rounded totals, control visibility and time gaining digits.
- Existing timer correctness, daily rounded export, autosave/discard, startup sound and asset tests continue to pass. Do not weaken tests merely to accommodate unintended clipping or lost settings.

Manual limitations remain: native pointer automation was blocked on this PC, and the host is Windows 10 22H2. Do not claim clean Windows 11, mixed-DPI/multi-monitor or physical audio checks without performing them. No application source, data, installer or release changes are part of this planning pass.

## Read-only Claude planning review — 2026-10-27

Completed static review: artifacts/claude-reviews/20261027-191726-559/review.md. Findings were compared with source; rendering hypotheses remain checks for implementation, not verified failures. Incorporate these refinements:

- Keep shared geometry on Workspace rather than AppSettings, as specified above. Settings currently replaces its draft wholesale, while unowned pop-outs can move during the dialog. Reset to defaults resets policies without deleting either geometry store. Add a regression for moving a pop-out while Settings is open and then applying.
- Centralize logical scale L=max(s,0.65) and presentation factor f=min(1,s/0.65). Audit every TimerScale consumer, including empty-row sizing and column calculation. Replace all Cards.Children casts with presenter-aware access together. Style previews and native pop-out cards bypass the workspace presenter. A Decorator with explicit measure/arrange and a visual transform is preferred; verify complete transformed bounds.
- Scale the configured inter-card gap by f below 65% too, keeping its saved value unchanged. This prevents fixed gaps dominating tiny cards. Zero gap remains zero. Application scale still applies once to the whole workspace.
- Verify Thumb drag-delta coordinate units before adding conversion: WPF may already report local logical deltas. Do not divide by the outer factor twice. Test the stored size resulting from a known drag at multiple scales; document any pointer-testing limitation.
- Derive native minimums from explicit intrinsic content, never the enlarged Viewbox's actual size. Both wide and tall pop-outs use joint readout/button scaling. Test layout settling, scrollbar appearance and wide/tall transition boundaries; add hysteresis or stable viewport allocation if needed to prevent oscillation.
- Shared location intentionally means the same saved coordinates, including overlapping windows. Do not silently cascade away from the user's saved location. Per-timer memory remains the default for distinct positions. Suppress initialization/visibility-correction writes, detach handlers on close, and associate pending writes with the owning workspace. Do not adopt the review's unconditional maximized-close capture: retain the last stable normal bounds instead.
- Remove full-title width from list minimum sizing; title and description use measured remaining space with ellipsis. Re-measure only when relevant layout inputs change. Explicitly enable a horizontal-scroll fallback for the central toolbar block and remove the current four-to-two command-column fallback, which would break its two-row geometry.
- Use the application Accent resource for the teal return action. Retain a button reference and separate the badge from alert state. Update the obsolete minimum-readable-size tooltip below 65%. If a forget-pop-out-memory action is added, distinguish it clearly from the existing forget-individual-card-sizes action.
- Rewrite the superseded fresh-open-size and rendered-24-DIP tests to assert the new requirements, and update SPECIFICATION.md, README and size-panel help. Preserve intrinsic drawing-bound tests. Add shared-memory close-order, pre-save flush and large-monitor-size round-trip tests.

Planning is complete. Next step is Sol implementation in the order above. No application code or release was changed, and no runtime validation was performed in this planning pass. Cross-version compatibility work remains excluded by the user's instruction.
