# Architecture and source map

Baseline: 0.5.3, inspected 2026-11-04. Windows desktop app written in C# with .NET 10 and WPF. It runs locally; GitHub and Claude are development services, not required app runtime services.

## Main flow

`App.xaml.cs` selects a data folder, acquires a per-folder single-instance mutex, checks database readability/recovery, then opens MainWindow. MainWindow owns the selected workspace, TimerEngine, persistence, UI refresh, pop-outs and audio coordination. UI commands mutate the shared model through engine/window methods, then refresh/persist. Pop-outs share timer state; they are not independent timer engines.

| Area | Source under `src/` | Responsibilities |
| --- | --- | --- |
| Domain | `TimersApp.Core/Models.cs` | Workspace, AppSettings, TimerRecord, Segment, IDs and serialized settings |
| Time | `TimersApp.Core/TimerEngine.cs` | Start/stop, elapsed duration, checkpoint, recovery/sleep, start ordering |
| Alerts | `TimersApp.Core/Alerts.cs` | Interval evaluation, active state, dismiss/snooze |
| Reporting | `TimersApp.Core/Reports.cs`, `ExportTable.cs` | Date allocation/filtering, exact durations and rounded daily rows |
| Appearance | `TimersApp.Core/Appearance.cs`, `PopoutPlacementPolicy.cs` | Styles/presets and placement policy |
| Main UI | `TimersApp.Desktop/MainWindow.xaml` and `MainWindow*.cs` | Commands, settings drafts, workspace switching, recovery, appearance |
| Timer layout | `TimerCard.cs`, `TimerCardPresenter.cs`, `TimeReadout.cs`, `TimerSizing.cs`, `TimerGridPanel.cs` | Card measurement, digits, compact/list layout and scaling |
| Pop-outs | `PopoutSession.cs`, `WindowPlacementService.cs` | Separate windows, native resize tracking and remembered bounds |
| Storage | `WorkspaceStore.cs`, `WorkspaceCatalog.cs` | Partial WorkspaceStore class: SQLite, JSON snapshots, validation, catalog/preferences and database backups |
| Portable data | `WorkspaceArchive.cs`, `AssetLibrary.cs` | Portable archives and managed image/audio files |
| Editors | `StyleEditor.cs`, `BackgroundEditor.cs`, `ImageBackdrop.cs`, `EditorControls.cs` | Preview, image fitting, styles/fonts and controls |
| Audio | `AlertAudio.cs`, `SoundCatalog.cs`, `StartupSoundSettings.cs` | Alert playback and separate global startup preference |
| Export | `ExportDialog.cs`, `ExcelExport.cs` | Selection UI and ClosedXML spreadsheet output |

Desktop paths in the table are relative to `src/TimersApp.Desktop/` unless stated otherwise.

## Time model: critical invariants

System time describes when events occurred. `TimeProvider.GetTimestamp()` and elapsed-time calculation measure live duration monotonically. Do not replace elapsed measurement with `Now - Start`: system time can jump. An active dictionary associates segment IDs with monotonic anchors and already measured ticks; those process-local anchors cannot survive a restart. Persisted elapsed ticks and checkpoint boundaries support recovery.

Each Start creates a Segment with name/description snapshots and a RunId. Stop checkpoints the same segment. Reset changes the timer's current RunId while retaining old segments; archive retains history. Total uses the current run; reporting can use retained historical segments. Do not erase segments to reset a visible readout.

StartSequence persists logical start order independently of wall timestamps. The engine seeds its counter from stored segments; overflow is handled by renumbering. Single-timer enforcement and concurrent recovery must preserve original logical ordering. Legacy zero-sequence records fall back to system start time; their lost historical ordering cannot be reconstructed.

## Persistence and recovery

The default folder is `%LOCALAPPDATA%\TimersApp`, database `workspace.db`; `TIMERSAPP_DATA_DIR` overrides the entire folder. SQLite stores serialized workspace snapshots/catalog/preferences; this is not a normalized segment-per-row reporting database. Inspect WorkspaceStore decode/validation before modifying schema or importing JSON.

Autosave checkpoints running data every five seconds and persists commands when enabled. Backups and assets are separate concerns. Database snapshots include all saved workspaces but do not embed managed assets. Portable `.t4backup` includes one workspace and referenced image/audio assets. Global startup sound preferences are not part of that single-workspace portable export.

Exit Discard restores the workspace-entry baseline. Autosaved current state must not become that baseline accidentally. Recovery uses CleanShutdown/checkpoint information and explicit user choices. Preserve malformed/original database files during recovery; never fix a user's live database in place without a backup.

## Dependencies and tests

Desktop project references Microsoft.Data.Sqlite, SQLitePCLRaw, ClosedXML, NAudio.WinMM and NLayer; versions are in the csproj. Keep bundled third-party notices when publishing. Core has no WPF dependency.

`tests/TimersApp.Tests/Program.cs` is a custom executable runner, not a conventional xUnit suite. It includes ManualClock-based timing checks and WPF checks in FeedbackChecks, Feedback2/3/4Checks, ReliabilityChecks and LayoutStressChecks. WPF renders are offscreen evidence and cannot establish physical-device behavior.
