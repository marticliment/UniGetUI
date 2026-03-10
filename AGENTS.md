# UniGetUI ΓÇô Copilot Instructions

## Project Overview

UniGetUI is a WinUI 3 desktop app (C#/.NET 8, Windows App SDK) providing a GUI for CLI package managers (WinGet, Scoop, Chocolatey, Pip, Npm, .NET Tool, PowerShell Gallery, Cargo, Vcpkg). Solution lives in `src/UniGetUI.sln`.

## Architecture

The codebase follows a **layered, modular structure** with ~40 projects:

- **`UniGetUI/`** ΓÇô WinUI 3 entry point, XAML pages, controls, and app shell (`EntryPoint.cs`, `MainWindow.xaml`)
- **`UniGetUI.Core.*`** ΓÇô Shared infrastructure: `Logger`, `Settings`, `Tools` (includes `CoreTools.Translate()`), `IconEngine`, `LanguageEngine`
- **`UniGetUI.PackageEngine.Interfaces`** ΓÇô Contracts: `IPackageManager`, `IPackage`, `IManagerSource`, `IPackageDetails`
- **`UniGetUI.PackageEngine.PackageManagerClasses`** ΓÇô Base implementations: `PackageManager` (abstract), `Package`, helpers (`BasePkgDetailsHelper`, `BasePkgOperationHelper`, `BaseSourceHelper`)
- **`UniGetUI.PackageEngine.Managers.*`** ΓÇô Concrete manager implementations (one project per manager: `WinGet`, `Scoop`, `Chocolatey`, `Pip`, `Npm`, etc.)
- **`UniGetUI.PackageEngine.Operations`** ΓÇô Install/update/uninstall operation orchestration
- **`UniGetUI.Interface.*`** ΓÇô Enums, telemetry, background API

## Adding a New Package Manager

Each manager extends `PackageManager` and must override three abstract methods:

```csharp
protected override IReadOnlyList<Package> FindPackages_UnSafe(string query);
protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe();
protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe();
```

Each manager also provides three helper classes (in a `Helpers/` subfolder):
- `*PkgDetailsHelper` extends `BasePkgDetailsHelper` ΓÇô overrides `GetDetails_UnSafe`, `GetInstallableVersions_UnSafe`, `GetIcon_UnSafe`, etc.
- `*PkgOperationHelper` extends `BasePkgOperationHelper` ΓÇô overrides `_getOperationParameters`, `_getOperationResult`
- `*SourceHelper` extends `BaseSourceHelper` ΓÇô overrides `GetSources_UnSafe`, `GetAddSourceParameters`, etc.

The constructor sets `Capabilities`, `Properties`, and wires the helpers. See `src/UniGetUI.PackageEngine.Managers.Scoop/Scoop.cs` as a clean reference implementation.

## Build & Test

```shell
# Restore & test (from src/)
dotnet restore
dotnet test --verbosity q --nologo

# Publish release build
dotnet publish src/UniGetUI/UniGetUI.csproj /p:Configuration=Release /p:Platform=x64

# Full release (runs version script, tests, publish, installer)
build_release.cmd
```

- Target framework: `net8.0-windows10.0.26100.0` (min `10.0.19041`)
- Build generates secrets via `src/UniGetUI/Services/generate-secrets.ps1` and integrity tree via `scripts/generate_integrity_tree.py`
- Self-contained, publish-trimmed (partial), Windows App SDK self-contained
- Tests use **xUnit** (`[Fact]`, `Assert.*`)

## Key Patterns & Conventions

### Settings
File-based settings via `Settings.Get(Settings.K.*)` / `Settings.Set(Settings.K.*, value)` and `Settings.GetValue(Settings.K.*)` / `Settings.SetValue(Settings.K.*, value)`. Setting keys are defined in the `Settings.K` enum in `SettingsEngine_Names.cs`. Boolean settings are stored as file existence; string settings as file content.

### Logging
Use `Logger.Info()`, `Logger.Warn()`, `Logger.Error()`, `Logger.Debug()`, `Logger.ImportantInfo()` from `UniGetUI.Core.Logging`. Accepts both `string` and `Exception` parameters.

### Localization
Use `CoreTools.Translate("text")` for all user-facing strings. Parameterized: `CoreTools.Translate("{0} packages found", count)`. In XAML, use the `TranslatedTextBlock` control. Translation files are managed externally via Tolgee; Python scripts in `scripts/` handle download and verification.

### Naming
- Types, methods, properties: **PascalCase**
- Private fields: `__doubleUnderscore` or `_singleUnderscore` prefix
- Internal unsafe methods: suffix `_UnSafe` (e.g., `FindPackages_UnSafe`)
- Nullable enabled globally; `LangVersion` is `latest`
- Code style enforced in build (`EnforceCodeStyleInBuild=true`)

### Manager conventions
- `FALSE_PACKAGE_NAMES`, `FALSE_PACKAGE_IDS`, `FALSE_PACKAGE_VERSIONS` static arrays filter CLI parsing noise
- Manager initialization flows through `Initialize()` ΓåÆ `_loadManagerExecutableFile()` ΓåÆ `_loadManagerVersion()` ΓåÆ `_performExtraLoadingSteps()`
- Operations that may fail return `OperationVeredict` (note: intentional misspelling used throughout codebase)

## Key Files

| Purpose | Path |
|---|---|
| Solution | `src/UniGetUI.sln` |
| Shared build props | `src/Directory.Build.props` |
| Version info | `src/SharedAssemblyInfo.cs` |
| Manager interface | `src/UniGetUI.PAckageEngine.Interfaces/IPackageManager.cs` |
| Base manager class | `src/UniGetUI.PackageEngine.PackageManagerClasses/Manager/PackageManager.cs` |
| Package class | `src/UniGetUI.PackageEngine.PackageManagerClasses/Packages/Package.cs` |
| Settings engine | `src/UniGetUI.Core.Settings/SettingsEngine.cs` |
| Setting keys | `src/UniGetUI.Core.Settings/SettingsEngine_Names.cs` |
| Logger | `src/UniGetUI.Core.Logger/Logger.cs` |
| CI test workflow | `.github/workflows/dotnet-test.yml` |
