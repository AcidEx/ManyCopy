# Maintenance Notes

## Completed for 1.1.9

- Made overwrite backups mandatory and integrity-checked before replacement.
- Prevented Undo and Redo from changing files modified outside ManyCopy.
- Added cleanup for expired and end-of-session Undo backups.
- Added regression tests for copy, overwrite, Undo, Redo, and content comparison.
- Restricted pull-request release checks to unsigned, read-only packaging.
- Added a safe maximum for generated folder ranges.

## Possible future work

- Replace the compact source field with a visible per-file list.
- Move long-running copy work off the UI thread and add progress and cancellation.
- Use one copy plan for preview and execution.
- Replace fixed control coordinates with a responsive layout.
- Add UI automation in addition to the core tests.
- Use trusted code signing for public releases when a suitable certificate is available.
