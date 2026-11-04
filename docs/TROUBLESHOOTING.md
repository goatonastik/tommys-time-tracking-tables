# Troubleshooting and regression history

Start with isolated reproduction, the relevant source and existing tests. Dates/results below are historical. Copies of selected local evidence are under `evidence/`.

| Problem | Cause and solution | Where to look |
| --- | --- | --- |
| Wrong timer retained after clock moves backward | Wall timestamps cannot define logical start order. 0.5.3 persists StartSequence, seeds it on load, preserves concurrent resume order and handles maximum sequence renumbering. Old zero-sequence records retain fallback limitations. | TimerEngine; Program timing/recovery checks |
| Snoozed alert fires for a stopped timer | Stopped/disabled evaluation must dismiss stale state; recovery and power-resume playback must exclude stopped timers. Fixed in 0.5.3. | Alerts; MainWindow recovery/audio commands; Program |
| Pop-out resize not remembered | Pending native resize could be lost during suppression/layout changes. Flush before suppression and track relevant native changes; avoid saving automatic out-of-drag corrections over user memory. | WindowPlacementService, PopoutSession, Feedback4Checks |
| Timer scaling stops shrinking near 65% | Internal minimum dimensions prevented further reduction. Whole-card presentation scaling below 65% implemented from notes4. Tiny text is expected at minimum scale; evaluate readability on a real monitor. | TimerSizing, TimerCardPresenter, Feedback4Checks |
| Pop-out buttons scale differently from time | Joint proportional sizing implemented from notes4. Recheck small/large windows and separate styles if altering measurement. | PopoutSession, TimeReadout, Feedback4Checks |
| Claude reports ECONNREFUSED while signed in | Restricted runner blocks outbound API access. Network-permitted execution succeeded. Auth status and network failure are separate diagnoses. | review/README.md, review-claude.ps1 |
| Claude returns API Error with successful exit status | Wrapper now rejects API-error text, captures input/stream errors and removes inherited provider/URL overrides. Never count an error file as completed review. | review-claude.ps1 |
| NuGet NU1900 warnings | Historical runs lacked advisory-feed access. Restore/audit with network access; do not equate unavailable feed with a clean vulnerability scan. | Build logs, NuGet.Config |
| App says already open | Mutex is keyed by full data-folder path. Use a distinct TIMERSAPP_DATA_DIR for testing; check taskbar/process before launching another copy. | App.xaml.cs |
| Saved data cannot load | Startup recovery preserves originals and offers backup restoration/new workspace. Copy the entire data directory before investigation, including assets and any SQLite sidecars using a consistent backup procedure. | WorkspaceStore, App.xaml.cs, MainWindow.Recovery |

## Open layout work

`LAYOUT-STRESS-REPORT.md` contains exact reproduction instructions and measured 0.5.1 observations using twelve timers. Current source has later timing changes; reproduce before proposing fixes.

- At 480 px list width, titles/descriptions become almost invisible; extended unit formats force horizontal scrolling.
- At 760 px list width, descriptions usually ellipsize instead of wrapping.
- Long titles can truncate while description space is unused.
- Fixed grid card widths/unequal row heights leave unused strips and bands.
- Full-panel renders did not show digits clipped inside cards, but the narrow viewport crops the extended readout until scrolled.

Potential designs, not approved implementations: share spare title/description width, optional two-line/expanded descriptions, collapse secondary controls before losing titles, and optionally distribute grid spare width. Preserve compact rows and context-menu access.

## Verification gaps

Offscreen WPF layout tests do not validate real mouse dragging, mixed DPI, multiple displays, screen readers or Windows 11 rendering. Real sleep/audio and clean Windows installation remain manual work. The 0.5.3 isolated launch smoke terminated the process after responsiveness; it did not verify clean shutdown. Earlier release smoke results must not be substituted for that missing check.

A final static review with no confirmed defect is not proof of correctness. Optional hypotheses and missing test coverage are not confirmed bugs. See selected raw review copies for their exact limitations.
