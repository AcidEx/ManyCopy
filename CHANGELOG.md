# Changelog

All notable changes to ManyCopy will be documented in this file. This project
follows the principles of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.1.9] - 2026-09-03

### Added
- Added transactional file staging and SHA-256 integrity checks for copy, overwrite, Undo, and Redo.
- Added regression tests covering new copies, overwrites, changed destinations, missing backups, and Redo safeguards.
- Added a 10,000-folder safety limit to the range helper.

### Changed
- Consolidated CI and release automation into one workflow for each purpose.
- Added a manual release dry run and strict tag, version, branch, signing, and signature validation.
- Reduced published release output to one canonical versioned Windows x64 package.
- Made pull-request release checks unsigned and reduced their token permissions to read-only.
- Corrected preview reporting for existing files when overwrite is disabled.

### Fixed
- Stopped overwrite when a verified backup cannot be created.
- Prevented Undo and Redo from deleting or replacing files changed outside ManyCopy.
- Kept recovery backups when restoration fails and cleaned them when history expires or the app closes.
- Replaced corrupted bullet characters in copy status messages.
- Prevented integer overflow in extreme folder ranges.

### Removed
- Removed duplicate CI and obsolete 1.1.6 branch-publishing workflows.
- Removed unused test dependencies, dead log-saving code, stale handover notes, and the obsolete branch timestamp file.

## [1.1.8] - 2026-09-03
### Fixed
- Removed nullable-reference warnings from the splash-image lookup and source-file display code.
- Corrected stale version references and restored the missing 1.1.7 release history in the documentation.
- Aligned the signing scripts with the existing `CN=ManyCopy` code-signing certificate and an explicit SHA-256 digest.

## [1.1.7] - 2025-10-09
### Added
- Accent color choices with light and dark theme variants.
- Multi-file source selection through browsing or drag and drop, with remove and clear controls.
- Optional automatic clearing of sources and destinations after copying.

### Changed
- Tightened the range-helper layout and kept the bottom action buttons aligned when resizing.

### Fixed
- Matched checkbox and radio-button backgrounds to their parent controls in dark mode.

## [1.1.6.1] - 2025-10-07
### Fixed
- Rebuilt self-contained Windows x64 executable and verified tests.
- Updated version metadata and README to 1.1.6.1.

## [1.1.6] - 2025-10-07
### Fixed
- Updated CI to use the .NET 8 SDK and removed the unused extra test
  invocation so builds succeed from a clean checkout.
- Adjusted CI commands so restore, build, test, and publish run from the
  repository root just like local builds.

## [1.1.5] - 2024-05-05
### Changed
- Retargeted the application to .NET 8.0 for compatibility with the stable SDK
  and to unblock the failing build.
- Bumped the application metadata and UI labels to display version 1.1.5 on the
  splash screen and main window.
- Updated the README so building from the repository root (`dotnet build`) is
  the default workflow and the .NET 8.0 dependency is clearly documented.
- Added this changelog to make it easier to track user-facing updates.

### Removed
- The `ManyCopy.Tests` MSTest project, eliminating the package dependency that
  was breaking `dotnet build` in clean environments.

## [1.1.4]
### Added
- Range helper support for preserving user-specified zero padding when
  generating destination folders.
- Automated tests that exercise range prefixes, suffix handling, and padding
  behavior for `BuildTargetName`.

### Fixed
- Undo history trimming so that the oldest entries are discarded when the limit
  is exceeded instead of hanging.
- Range creation logging to clearly state when folders are created.

### Removed
- The outdated COM interop comment from the project file so the documentation
  matches the current implementation details.

