# Microsoft Store listing draft

Prepared 2026-10-28 for owner review before submission. This is source copy, not a published Store listing.

## Title

Tommy's Time Tracking Tables

## Short description

Track several tasks at once with named stopwatches, exact session history, flexible layouts, and spreadsheet exports.

## Description

Tommy's Time Tracking Tables is a Windows desktop app for tracking time across several tasks at once. Add named timers with optional descriptions, run them independently, and review each start and stop segment with its system timestamp.

- Switch between a compact list and a grid. Adjust timer size, fonts, colors, digit style, backgrounds and saved appearance presets.
- Pop individual timers into separate windows with optional compact styles and remembered positions.
- View exact running time and configurable rounded totals. Export the full history or a selected date range as an Excel-compatible workbook or CSV table, including daily totals and individual segments.
- Keep timers and settings locally with automatic saving, backups and recovery options. Choose how the app handles an interrupted session or an exit.
- Use optional interval alerts with color, flashing or sound; alerts are off by default.

The app is designed for Windows 11 x64. It does not require a sign-in or a cloud account for time tracking.

## Privacy statement draft

The app stores timer names, descriptions, timestamps, style settings and any imported image or sound assets locally on the user's device. It does not contain developer-operated analytics, advertising or account services, and its application code does not transmit timer data to us. The user chooses when to create spreadsheet exports or portable backups and where to save them. If a user chooses a cloud-synced folder as the destination, that folder's provider controls its own syncing and privacy practices. Microsoft Store services may separately process installation and usage information under Microsoft's policies.

The final Store package's data path, deletion behavior and any migration from the current installer must be verified on a clean test system before this statement is finalized. Do not submit this draft as a legal privacy policy without the owner's review.

## Certification notes draft

This is a self-contained WPF/.NET desktop timer application. It runs without a network connection or user account. Its full-trust capability supports normal desktop windows, local SQLite persistence, user-selected image/sound files and export dialogs. Timer alerts are disabled by default. A reviewer can add a timer, start and stop it, open segment history, switch list/grid, pop out a timer and export to a chosen file. No credentials are required.
