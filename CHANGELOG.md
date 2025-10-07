# Changelog

All notable changes to ManyCopy will be documented in this file. This project
follows the principles of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed
- Updated the CI workflow to use the .NET 8 SDK and remove the unused test
  invocation so builds succeed from a clean checkout.

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
