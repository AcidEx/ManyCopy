# Changelog

All notable changes to ManyCopy will be documented in this file. This project
follows the principles of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.1.5] - 2024-05-05
### Changed
- Bumped the application metadata and UI labels to display version 1.1.5 on the
  splash screen and main window.
- Expanded the README with build verification steps, including cleaning the
  workspace before compiling and confirming the splash screen visuals after the
  build.
- Added this changelog to make it easier to track user-facing updates.

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
