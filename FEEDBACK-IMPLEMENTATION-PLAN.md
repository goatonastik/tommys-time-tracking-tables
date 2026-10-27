# notes1.txt — reviewed implementation plan

Reviewed 2026-10-27 against the source and partial draft. This is the handoff from Astra to Sol; no application implementation was changed during this planning pass. The original notes remain authoritative. The user confirmed the saved-items dropdown means saved workspaces.

## Baseline and scope

The published v0.1.0 preview is the working baseline. Four source files contain preliminary, uncommitted changes described in CHECKPOINT.md. They compile, but have no new regression coverage or Claude review. Keep useful changes, revise incomplete storage behavior, and do not treat compilation as functional verification. The UI is still the original UI.

Keep C#/WPF/.NET and SQLite. This feedback needs focused components and lifecycle boundaries, not a framework replacement. Implement as a v0.2.0 preview after validation. Test only against isolated data until migration is verified.

## Structural decisions

### 1. Named workspaces and data ownership

Introduce a workspace repository boundary and a workspace-session controller, separating persistence and switching from MainWindow event handlers. MainWindow remains responsible for windows and user interaction. The session controller coordinates the engine, entry baseline, save/discard choice and selected workspace. Avoid a broad rewrite of unrelated timer/report code.

- Workspace identity is a GUID; display name is not a filename or database key. Names are trimmed, nonempty, and unique ignoring case among live workspaces. Bound name length to a reasonable UI limit (100 characters). Existing content becomes `default` once during migration, retaining all timers/history/settings/presets.
- Maintain a small catalog with ID/name/update time/deleted state and payloads keyed by ID. A dedicated `workspaces` SQLite table is recommended; the existing `snapshots` table can continue holding recovery and compatibility slots. List catalog metadata without parsing every timer history. Keep one authoritative payload per workspace; any `current` compatibility projection must be updated in the same transaction, never become a competing source of truth.
- Persist the active-workspace ID alongside launch preferences. Create/rename/save/switch/delete and their required catalog/current/preference updates must be transactional. A failed commit leaves the old in-memory session selected and running; do not close pop-outs or discard engine anchors first.
- Separate database-wide backup policy (disable backups, maximum count) from workspace appearance/settings. A database backup contains all named workspaces, so its policy must not unexpectedly change when selecting another workspace. Introduce a small storage-policy record rather than duplicating that policy in every workspace. Preserve current autosave/restore/exit semantics, explicitly projecting the active workspace's launch preferences as needed.
- New: a named empty workspace with default settings. Duplicate: a deep copy of the current checkpoint, including timer history, settings, suggestions and presets, assigned a new workspace ID/name; running segments in the copy close at that checkpoint and pop-outs start closed. Referenced managed image/audio files can be shared because imports are immutable; deletion must not remove assets still used elsewhere.
- Switching policy proposed for this iteration: only the selected workspace runs. When leaving running timers, explain that switching will stop them and offer Save and switch / Cancel. With unsaved edits and autosave off, offer Save / Discard / Cancel; discard uses this workspace's entry baseline. Do not stop timers merely because the dropdown opened. Use staged snapshots for persistence so failure cannot lose elapsed time or leave the live engine stopped. This policy must be stated to the user, not silently inferred as a previously agreed requirement.
- After a successful switch, establish an entry baseline for the newly entered workspace. Existing manual-save and session-discard behavior remains consistent: discard means changes since entry to that workspace, not an unrelated app-start workspace. Do not reset that baseline during a same-workspace recovery restore unless the user explicitly begins a new session.
- Rename changes metadata and the visible name without changing timer history. Delete confirms the workspace name, stops its timers on successful completion, hides it from the catalog and retains a recoverable copy accessible from Backups. Select another saved workspace, or create a fresh `default` if none remain. Ensure exit/discard cannot resurrect the deleted catalog entry.
- Import portable backup should offer import as a new workspace (new ID, collision-safe name) and explicitly replace current. Replacing current keeps the current catalog ID/name and saves its displaced payload first. Restore a database snapshot through a workspace selector so named workspaces in that snapshot are accessible; do not silently restore only the formerly active payload. Disaster recovery of the entire database must preserve the whole catalog, not just `ReadDatabase(...current)` as today.
- With Restore on launch disabled, open a fresh unsaved workspace while retaining the saved catalog. Do not overwrite `default`, produce unbounded blank saved workspaces, or replace a single previous-workspace slot repeatedly.
- Migration runs once transactionally, assigning stable IDs. Preserve the pre-migration database using SQLite backup or another consistent copy, independently of routine backup policy. Unsupported future schema versions must produce an actionable error without automatic destructive recovery. Old v0.1.0 cannot read schema 3; verify migration before normal launch and document rollback using the preserved database.

### 2. Responsive shell and reusable controls

Build a `ScaleInput` control (percent text, minus, plus), a reusable labeled slider with numeric feedback, a small vector icon library, and a notification overlay. Use one style source for consistent focus, tooltips and sizing.

- Percentage values commit on Enter or focus loss; Escape restores the last committed value. Invalid/nonfinite values show an inline message and retain the last valid scale. Proposed supported range: 25–300%, consistent across controls and serialization. Minus/plus move to the immediately lower/higher multiple of 25 (110% → 100% or 125%; 100% → 75% or 125%). One user action triggers one settings/layout update. Distinguish focus-loss commit from subsequent step handling to prevent double saves/reflows.
- Keep T⁴ on the left. Place command and scale/layout groups to its right; the scale group wraps beneath commands when insufficient space remains. Groups may wrap internally on very small windows. Do not put fixed-width groups into an unconstrained horizontal layout that simply clips. At high app scale, keep settings and scale controls reachable through wrapping/scrolling.
- App scale affects main-window UI density; timer scale affects workspace cards; pop-out scale is independent and defaults to 100%. Link pop-out scale to timer scale defaults off. Preserve the independent value while linked so unlinking restores it. Main-window scale must never multiply into detached windows. Pop-out scale affects its content density, not repeatedly resizes its native window; user window geometry remains under user control. With fit-to-space enabled, readout fitting can take priority over nominal font size, while scale controls padding/controls/detail density.

### 3. Timer presentation and resize behavior

Split TimerCard updates into style/content changes, size-dependent layout and frequent elapsed/alert updates. Cache stable geometry and background parameters, and avoid invalidation when values are unchanged. Visuals.Brush and ImageBackdrop already cache brushes/images; do not claim those are missing. TimeReadout currently invalidates measure whenever the displayed string changes, and paints even when unchanged. Measure stable digit bounds where appropriate, invalidate only when actual bounds or style change, and continue updating milliseconds/flash correctly.

Use a small layout policy based on available width AND height after chrome and scaled padding, measured content and requested visibility. Three arrangements are useful: normal vertical, short/wide with information and controls beside the readout, and compact time-first. Determine the arrangement without content visibility changing the input size; use stable breakpoints or hysteresis to avoid oscillation. Never show content explicitly disabled by the style. All omitted actions remain available by right click and keyboard context menu.

Trim excess top/bottom readout bounds and compact padding. Preserve glyph aspect ratio, since a very narrow window cannot simultaneously fit a long number and fill all height. Time-only mode should devote available space to the readout, not reserve hidden heading/footer rows. Widening a short window should restore requested title, description and controls when they fit.

Profile and reproduce the jitter before declaring a fix. Candidate paths: Scale_Changed runs continuously during slider dragging, SizeChanged calls full Refresh, Refresh recreates clipping geometry, ImageBackdrop.Update always invalidates, and auto columns use ActualWidth plus auto scrollbars. Use viewport width, avoid unnecessary column changes, and prevent scrollbar/layout feedback. Coalesce resize work per render frame; do not defer accurate timer accounting or make dragging unresponsive.

### 4. Editor design

Create a dedicated style-editor layout instead of forcing the existing single StackPanel FormDialog into two columns. Two narrower settings columns plus a preview column; target approximately 1100–1200 logical pixels, bounded to the work area. Stack/reflow and allow vertical scrolling on small/high-DPI screens. One-page presentation is the goal on a normal desktop, not a reason to clip controls.

Keep a draft model until Apply. Preview changes immediately. Save as preset is an explicit persistence action, separately from applying the draft to a timer. Put preset selector, Load, Save as preset and Delete at the top. Deleting a selected preset preserves copies of its appearance on any timers referencing it, then removes it and refreshes selectors; cancelling the editor must not deceptively undo or imply undo of an explicitly confirmed preset deletion.

Group font size, inner spacing and corner radius sliders together. Show installed font names and right-aligned `12:34:56.789`, each rendered in that font using a WPF item template; keep a safe fallback and virtualized list. Place hide-unit-labels and custom-format controls together in Visible content; the preview may show format help underneath if space is limited.

Format rules: Y = elapsed 365-day years, D = days, H = hours, M = minutes, S = seconds, MS = milliseconds. `H:M:S.MS` displays `00:00:00.000`. `D:H:M.MS` literally displays days/hours/minutes/milliseconds, not hours/minutes/seconds. Support selected-unit formatting with optional labels, plus explicit custom-format mode. Custom format uses literal separators and overrides unit selection/labels while active; disable conflicting controls without erasing their prior values. Validate nonempty tokens, allowed separators, uniqueness and descending unit order. The first selected unit rolls up omitted larger units; no elapsed time should vanish because hours exceed 24. Subsequent fields consume remainders; milliseconds use three digits. Invalid input must not crash ticks or overwrite the valid style. Hide-unit-labels defaults off and preserves existing HMS rendering for old styles.

## Complete feedback-to-change map

| Feedback | Current behavior | Planned change / acceptance |
|---|---|---|
| App and timer scale inputs | Continuous sliders with resets | Editable percent and 25% step buttons; no full update on each typed character |
| Independent default pop-out scale | No pop-out scale setting | Settings control plus link checkbox, off by default; saved across launch |
| Tiny T⁴ title-bar icon | Embedded ICO; no dedicated small-size treatment verified | Redraw with larger occupied area and legible superscript, explicit 16/20/24/32/48/256px variants; apply to main and detached windows |
| Saved notification | Success MessageBox | Brief top overlay reading Saved using app styling, no layout shift or focus theft; restart its timeout on repeated save |
| Remove long name and tagline | Two heading TextBlocks | Remove in-window heading/tagline; keep T⁴ logo and descriptive OS title/accessibility metadata |
| Grouped top controls | Separate heading, command row, slider row | Responsive command and scale/layout groups beside logo |
| Add timer and workspace actions | Text Add timer/Save/Load; one workspace | Teal alarm-clock-plus; named dropdown default `default`; New plus, Save floppy, Rename A with pencil/eraser, Duplicate overlapping squares, Delete X; hover tips and accessible names |
| Settings/Appearance/Backups labels | Ellipses on some labels | Plain labels; retain Export; move old Load saved recovery entry into Backups |
| Backup policy and deletion | Fixed 20 in released code; draft 32 | Global disable unchecked, max 32, Delete all backups with confirmation and count. Delete only recognized timestamped backup files, not active DB, managed assets or preserved migrations; disclose recovery-copy scope |
| Small optional status bar | Always-visible status row | Thin text-height bar, setting off by default; save errors/backup warnings remain independently visible |
| Saved dropdown | Missing | Confirmed by user as saved workspaces, not presets |
| Grid/list icons | Text-only toggle | Consistent 3x3 grid and four-line list glyphs with same border/stroke dimensions |
| Larger resize notch | 15x15 Thumb | At least 30x30 hit target with clearly visible grip, nonoverlapping controls |
| Shorter timer buttons | Generous vertical padding | Reduce height/padding; retain existing useful widths, focus feedback and labels |
| Compact and wide pop-outs | Height below 170 hides most fields regardless of width | Width/height layout policy above; maximize time readout and restore allowed information/actions when room exists |
| Style-editor columns | Single scrolling fields column plus preview | Two narrower columns, wider responsive dialog with all actions reachable |
| Preset controls | Load near top; Save at bottom; delete elsewhere | Load/Save as/Delete grouped at top, correct references after deletion |
| Style sliders | Font size/padding/radius text fields | Sliders with numeric values together; saved values match preview |
| Hide unit labels | Non-HMS formats include unit suffixes | Checkbox default off; colon-separated selected units, decimal separator before milliseconds |
| Remove months note | Label explicitly mentions excluded months | Rename to Elapsed units; retain correct formatter behavior |
| Font previews | Plain strings in ComboBox | Each font name and numeric sample displayed in that font |
| Custom number layout | No field in UI; draft formatter exists | Explicit mode, documented tokens, live preview, validation and preset persistence |
| Workspace sliders | Four text fields | Spacing/card width/card height/list-row-height sliders with numerical readouts |
| Reset workspace defaults | No reset button | Reset the settings-dialog draft to defaults, Apply commits; does not delete timers/history/presets. Individual custom sizes reset only through existing explicit size-reset choice |
| Suggestion controls | Remove selected and separate clear checkbox | Remove all button; Do not save name suggestions stops future collection (existing suggestions remain until removed); use those exact semantics in helper text |

## Implementation sequence and gates for Sol

1. **Persistence and lifecycle.** Refine or replace preliminary named-slot helpers with transactional catalog/session operations and backup policy. Add migration, switching and recovery tests before wiring toolbar actions. Preserve all prior data. Have Claude review data-loss and recovery risks read-only at this boundary.
2. **Reusable controls and shell.** Scale input, labeled slider, vector icons, header grouping, workspace actions/selector, Saved overlay, optional status bar. Hook errors into visible notifications even when status bar is hidden. Implement backup and suggestion management.
3. **Timer layout and icon.** Split update paths, implement width/height-aware layouts, independent pop-out scaling, larger grip, shorter buttons, and small-size ICO. Validate both text and seven-segment readouts, active alerts and context menus. Save icon generation source/script for reproducibility.
4. **Style/settings editors and format.** Dedicated responsive two-column style editor, preset controls, font previews, format options, sliders and draft-only Reset defaults. Test Apply versus Cancel and explicit preset actions.
5. **End-to-end review and preview.** Run the full suite, offscreen rendering and available live tests. Claude read-only review of all substantive code changes, then fix independently confirmed findings. Update documentation and publish/test v0.2.0 installer. Sync source to GitHub without force-pushing; upload a new preview release and verify its file hash. Do not replace v0.1.0 assets.

## Required verification

- Migration from real-shape schema 1/2 fixtures is repeatable; preserves IDs once assigned, timers, history, styles, names and preferences; failure leaves original database usable. Restoring backups recovers multiple workspaces. One corrupt catalog payload does not unnecessarily make every valid workspace inaccessible.
- Switching with active timers, autosave on/off, save/discard/cancel, and injected storage failure; exact measured segments remain correct. Switching back excludes inactive downtime. Baselines belong to the right workspace. Rename/duplicate/delete, deleting last workspace, deleted-workspace recovery, and exit cannot recreate deleted workspaces.
- Independent backup policy, retention 32 by default, custom retention, disabled scheduled/manual-save/exit-triggered backups, retained ability to explicitly export a portable backup, locked backups and partial deletion reporting. Manual export is a user action, not the automatic snapshot behavior being disabled.
- Custom formats, hidden units, large durations, milliseconds, lowercase tokens, invalid/duplicate/out-of-order tokens, preset round-trip and backward-compatible default displays.
- Scale inputs at arbitrary values and bounds, invalid values, keyboard use and one-action commit; independent/link/unlink pop-out behavior; app scaling leaves detached scale independent.
- Wide/short, tiny, normal and tall pop-outs; grid/list; 50 active timers; text and seven-segment; alert display; hidden-control context menus; resizing across column/scrollbar breakpoints. Measure before/after resize behavior rather than only asserting absence of exceptions.
- Dialog reset/cancel/apply, presets in use, font-list template, suggestion removal/disable, toast versus error display, and header reachability at narrow widths/high scale. Render at representative 100/150/200% DPI and inspect actual live behavior when available.
- Update old test expectations for schema 3 and 32 backups. Do not simply replace constants without adding retention/migration coverage. Current draft has only compiled, not passed the revised suite.
- Stage new source files before invoking `review-claude.ps1`; it snapshots tracked paths. Keep Claude tools disabled. Focus review on transactional boundaries, old-schema handling, deleted data, backup scope, custom format correctness and layout cycles. Record confirmed issues and remaining manual QA honestly.

## First action after switching to Sol

Read this plan, CHECKPOINT.md, notes1.txt and AGENTS.md; inspect the four-file working diff. Start with repository/session design and isolated tests. Do not apply the rejected UI patch from conversational history as if it had already been validated. Preserve the user's notes and current edits. No new tools or framework are needed for the implementation.
