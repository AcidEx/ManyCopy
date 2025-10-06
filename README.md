# ManyCopy

ManyCopy is a Windows utility that copies a single source file into many
folders. The app supports sequential folder generation with optional range
suffixes, configurable padding for folder numbers (e.g. `001`, `002`, `003`),
and a log window that records every operation so you can quickly review what
ran.

Current release: **1.1.5**. Review the [changelog](CHANGELOG.md) for a
breakdown of recent updates.

## Requirements

- Windows desktop. The WinForms UI relies on Windows-specific APIs.
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download) with Windows
  targeting enabled. The main project and the accompanying test project both
  target `net8.0-windows`.

## Building the application

Use the .NET CLI to restore dependencies and build the WinForms executable:

```bash
dotnet clean
dotnet restore
dotnet build ManyCopy.csproj
```

The `dotnet clean` step ensures old artifacts from previous builds are removed
before compiling. The project is configured to publish a single-file `win-x64`
executable. To create a packaged build, run:

```bash
dotnet publish ManyCopy.csproj -c Release
```

After building or publishing, launch the executable to verify that the splash
screen still shows the progress bar and `1.1.5` version label before ManyCopy
opens.

## Running tests

Unit tests live in the `ManyCopy.Tests` project. If `dotnet test` fails with an
error similar to `dotnet: command not found`, install the .NET SDK so the
`dotnet` CLI is available on your `PATH` before running the command below:

```bash
dotnet test ManyCopy.Tests/ManyCopy.Tests.csproj
```

## Contributing

Issues and pull requests are welcome! Before sending changes, please:

1. Run the unit test suite (`dotnet test ManyCopy.Tests/ManyCopy.Tests.csproj`).
2. Update `CHANGELOG.md` with a summary of your changes.
3. Update this README when you add or modify user-facing functionality.

## License

ManyCopy is released under the [MIT License](LICENSE).
