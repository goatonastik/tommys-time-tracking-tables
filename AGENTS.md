# Project collaboration

- Use Claude for independent review of substantive code changes in this project, as requested by the user. Use `review-claude.ps1`; keep Claude read-only (no editing, shell, MCP or other action tools). Do not enable bypass-permissions or delegate implementation to Claude.
- Check findings against source and meaningful tests before acting. Record false positives and outstanding uncertainties honestly. The initial review is under `artifacts/claude-reviews/20261026-155655-481/` locally.
- The reviewer snapshots tracked paths and current working-file contents. Add new source files to Git before reviewing them. Keep raw snapshots and reviews in ignored `artifacts/`.
- Run `build.ps1` for relevant verification and `publish.ps1` for updated portable packages. Tests must use isolated data, never the user's normal workspace database.
- GitHub repository: https://github.com/goatonastik/tommys-time-tracking-tables (private). Local and GitHub initial histories differ; preserve both and do not force-push. GitHub integration uploads are available; do not assume local Git authentication is configured.
