# ManyCopy

ManyCopy is a Windows utility that copies a single source file into many
folders. The app supports sequential folder generation with optional range
suffixes, configurable padding for folder numbers (e.g. `001`, `002`, `003`),
and a log window that records every operation so you can quickly review what
ran.

Current release: **1.1.6.1**. Review the [changelog](CHANGELOG.md) for a
breakdown of recent updates.

## Requirements

- Windows desktop. The WinForms UI relies on Windows-specific APIs.
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download) with Windows
  targeting enabled. The WinForms project targets `net8.0-windows` so it can
  compile against the desktop APIs.

## Building the application

From the repository root, use the .NET CLI to restore dependencies and build
the WinForms executable. Running the commands in the top-level directory works
because the repository is organised around the single `ManyCopy.csproj` file:

```bash
dotnet clean
dotnet restore
dotnet build
```

The `dotnet clean` step ensures old artifacts from previous builds are removed
before compiling. The project is configured to publish a single-file `win-x64`
executable. To create a packaged build, run:

```bash
dotnet publish ManyCopy.csproj -c Release
```

After building or publishing, launch the executable from the `bin` or
`publish` directory to verify that the splash screen still shows the progress
bar and `1.1.6.1` version label before ManyCopy opens.

## Verification

Automated tests are included. You can run `dotnet test -c Release` from the
repository root. In addition, perform a quick manual smoke test:

1. Start the application and confirm the splash screen shows the progress bar
   and version number.
2. Create a few range-based folders to ensure leading-zero padding is preserved.
3. Copy a source file into the generated folders and review the log window for
   success messages.

## Contributing

Issues and pull requests are welcome! Before sending changes, please:

1. Update `CHANGELOG.md` with a summary of your changes.
2. Update this README when you add or modify user-facing functionality.

## License

ManyCopy is released under the [MIT License](LICENSE).

## Tests

- SDK: Repo pinned via `global.json` to .NET SDK 8.0.414.
- Restore: `dotnet restore`
- Run: `dotnet test -c Release`

If a newer SDK is installed, `global.json` will keep tooling on a compatible 8.0 feature band.
