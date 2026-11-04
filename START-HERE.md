# Tommy’s Time Tracking Tables — project handoff

Prepared 2026-11-04 for continuing development without the original chat/account.
Repository: https://github.com/goatonastik/tommys-time-tracking-tables (private).
Product name: **Tommy’s Time Tracking Tables**, also styled T⁴. Installer author: **Goatonastick**; GitHub username: **goatonastik**.

## Read first

1. Read `AGENTS.md` for mandatory collaboration and data-protection rules.
2. Read this file, then `docs/ARCHITECTURE.md`, `docs/BEHAVIOR-AND-DECISIONS.md`, `docs/TROUBLESHOOTING.md`, and `docs/DEVELOPMENT-AND-RELEASE.md`.
3. Consult `SPECIFICATION.md`, `notes1.txt` through `notes4.txt`, and their `FEEDBACK*-IMPLEMENTATION-PLAN.md` files for original requirements. README describes current user-facing behavior. PROGRESS and older CHECKPOINT entries are historical evidence, not a pending task list.

## Verified baseline

Application source version is **0.5.3**, latest recorded published prerelease **v0.5.3-preview**:
https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.5.3-preview

Local implementation commit: `617b5af`; remote release source: `8125bbf434c0767069432f2e05fda153e7329bc1`.
Local documentation baseline before this handoff: `22887d8`.
The two histories differ intentionally. Preserve both; never force-push or assume their commit IDs match.

The 2026-11-03 release records report 67 passing checks, zero build warnings/errors, final read-only Claude review with no confirmed defect, successful packaging and GitHub Windows checks. Installer/portable checksums and evidence are in `docs/evidence/INDEX.md`. These are historical results; handoff creation does not constitute a fresh application test run.

No application change or new installer is part of this handoff. The latest fixes preserve start order across backward system-clock changes, concurrent sleep/crash recovery, and stop snoozed alerts from reactivating.

## Outstanding work and boundaries

- Twelve-timer layout usability findings remain documented in `LAYOUT-STRESS-REPORT.md`; they were observed against 0.5.1 and should be reproduced on current source before a fix.
- Windows 11 clean installation, physical pointer/screen-reader behavior, audible playback, real suspend/resume, and mixed-DPI/multiple-monitor behavior need manual validation.
- Signing and Microsoft Store publication are **paused at the user's request**. Downloads are unsigned. No safety or reliability certification is claimed.
- Await the user's next requested change; optional reviewer suggestions are not automatic authorization for unrelated work.

Default personal data is `%LOCALAPPDATA%\TimersApp`. Never run tests against it or replace the installed app to test an installer. Set `TIMERSAPP_DATA_DIR` to a fresh folder under `artifacts/` for every manual test session.

## Starter prompt for a new assistant

> Continue work on Tommy’s Time Tracking Tables from this repository. Read AGENTS.md and START-HERE.md, then the linked architecture, decisions, troubleshooting and release documents. Verify the working tree, current source version and repository state before changing anything. Preserve my real workspace data and both Git histories. Use Claude only for independent read-only review of substantive code changes via review-claude.ps1. Report what is verified versus historical or untested. Signing and Store publication remain paused. My next requested task is: [describe the task].

## Keep this handoff current

After a release or material fix, update this baseline, known issues, verification limits and evidence index. Keep old records dated and historical. If documentation conflicts with source, investigate and report the discrepancy; do not silently change established user behavior. Credentials and sign-ins must be reconnected on a new account/computer; they are deliberately absent here.
