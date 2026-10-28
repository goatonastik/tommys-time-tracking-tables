# Twelve-timer layout inspection

Tested 2026-10-28 against the 0.5.1 application source. The automated WPF render uses an isolated workspace with twelve stopped timers, mixed name and description lengths, a three-line description, a long word without spaces, and one seven-segment timer showing years through milliseconds. The test machine is Windows 10 22H2; the intended target is Windows 11. No normal workspace or installed app data was changed.

The repeatable test is `tests/TimersApp.Tests/LayoutStressChecks.cs`, called by `build.ps1`. It renders the complete timer panel and the main-window viewport into ignored `artifacts/tests/stress-*.png` files and records measurements in `artifacts/tests/stress-layout-report.json`. These are offscreen WPF renders and measured layouts; they do not prove physical pointer, display scaling, or Windows 11 behavior.

| Case | Window content size | Timer scale | Grid columns | Timer panel vs viewport width | Finding |
| --- | --- | --- | --- | --- | --- |
| Grid normal | 1220 × 820 | 100% | 3 | 1018 / 1188 | Full digits and wrapped descriptions; 170 px of viewport unused at right. |
| Grid narrow | 480 × 620 | 100% | 1 | 330 / 431 | Full digits and wrapped descriptions; about 101 px unused at right. |
| Grid small | 1220 × 820 | 65% | 3 | 773 / 1188 | Full digits; 415 px unused at right; uneven card heights leave blank bands under shorter cards. |
| Grid minimum | 1220 × 820 | 25% | 9 | 825 / 1188 | Full but tiny digits; 363 px unused at right; the second row has only 3 timers. |
| List normal | 1220 × 820 | 100% | 1 | 1188 / 1188 | One long title and one long description show ellipses. The long title is shortened despite unused space following its short description. |
| List medium | 760 × 620 | 100% | 1 | 711 / 711 | 2 titles and 9 descriptions show ellipses; descriptions remain on one line. |
| List narrow | 480 × 620 | 100% | 1 | 680 / 431 | All 12 titles and 11 nonempty descriptions are reduced to near-zero width; the extended-format timer forces horizontal scrolling. |
| List narrow, small timers | 480 × 620 | 65% | 1 | 510 / 431 | 8 titles and 11 descriptions are truncated; horizontal scrolling remains. |

No timer digits appeared cut off within their cards or overlapped another card in the complete-panel renders. In the 480 px list viewport, the extended readout extends offscreen and is cropped until the user scrolls horizontally. All twelve card sizes met their computed minimums, and their internal digit heights were at least 24 device-independent pixels. At the 25% timer scale the presentation transform makes the rendered digits much smaller; readability should be judged on a real display.

## Reproduce the visible issues

1. Create twelve timers and enter varied descriptions. For one timer, choose seven-segment digits and enable the units from years through milliseconds. Give another timer a long title, and one a short description. Keep total display set to both exact and rounded.
2. In grid view at 100% timer size and a roughly 1220 px-wide window, compare the card area with the unused area at the right. Shrink the window to its 480 px minimum and compare the 330 px-wide single card with the unused strip.
3. In list view at roughly 1220 px width, use the title “Client meeting and follow-up decisions for the entire project” and description “A short description.” The title gains an ellipsis while space remains to the right of the description.
4. Narrow list view to roughly 760 px: most descriptions become single-line ellipses. At the 480 px minimum, titles and descriptions become almost invisible while the extended time row triggers a horizontal scrollbar. Try 65% timer size; the scrollbar still appears.

The list's single-line ellipses are deliberate in the current layout, but the title-space allocation and total disappearance of names at the minimum width are usability issues. A possible design is to give the title unused width from a short description, and allow an optional two-line description or an expanded row while preserving compact rows by default. The narrowest list may need to move the name to a second line or collapse secondary controls/rounded total before removing the name. Grid cards could optionally expand to use leftover row width, without changing the user's selected timer scale.

Verification: `artifacts/stress-build-assertions.log` reports 61 passing checks and no failures. The build emitted NU1900 warnings because the online NuGet advisory endpoint was unavailable during this run; no compiler warnings were reported. A read-only Claude review was attempted under `artifacts/claude-reviews/20261028-114848-328/`, but it returned ECONNREFUSED and produced no findings.
