# Independent Claude reviews

Install Claude Code using Anthropic's official Windows installer, then sign in with your Claude subscription using `claude auth login`. If `claude` is not on PATH, use `& "$env:USERPROFILE\.local\bin\claude.exe" auth login` in PowerShell.

From PowerShell 7 in the project folder:

```powershell
./review-claude.ps1 -PrepareOnly
./review-claude.ps1
./review-claude.ps1 -Focus 'Review crash recovery and save/discard behavior.'
```

The script defaults to the `sonnet` model alias; pass `-Model opus` for a different available reviewer. Review calls use your Claude subscription and its usage limits. `-PrepareOnly` does not contact Claude.

The script reads tracked source paths from Git and snapshots their current working-file contents, with line numbers. It includes specifications, implementation, tests and build instructions. Generated files, images, sounds, databases and credentials are excluded. New source files must be added to Git before they appear in the snapshot.

Claude receives that snapshot through standard input. Safe mode disables customizations; built-in tools and MCP tools are disabled for the review. Inherited API credentials/provider selections are removed from the child process so this workflow uses subscription sign-in. No credentials are saved in this repository.

Input snapshots, file lists, diagnostics and reviews are saved under `artifacts/claude-reviews/`, which Git ignores. Read the findings critically: this is static analysis, not executed tests. Verify findings against source and reproduce bugs before implementing fixes. Claude's responses do not authorize changes or other external actions.

Official references: [installation](https://code.claude.com/docs/en/setup) and [CLI options](https://code.claude.com/docs/en/cli-reference).
