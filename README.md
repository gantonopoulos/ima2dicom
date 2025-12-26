# ima2dicom
Siemens IMA (VB41) CT to DICOM Converter

This project contains a C# tool that converts legacy Siemens CT .ima files (VB41-era, non-standard format) into standard DICOM images using fo-dicom for visualization and analysis.
It was developed through reverse engineering of publicly available file characteristics and empirical validation in Weasis and GIMP.

## Download

Pre-built self-contained executables are available in the [Releases](../../releases) section. Download the appropriate version for your platform:
- **Linux x64**: `ima2dicom-linux-x64.tar.gz`
- **Windows x64**: `ima2dicom-win-x64.zip`
- **macOS Intel**: `ima2dicom-macos-x64.tar.gz`
- **macOS Apple Silicon**: `ima2dicom-macos-arm64.tar.gz`

No additional dependencies or runtime installation required.

## Usage

```bash
# Show help
ima2dicom --help

# Convert files in current directory
ima2dicom

# Convert with custom input/output directories
ima2dicom --in=/path/to/ima/files --out=/path/to/output

# Use custom configuration file
ima2dicom --in=/input --out=/output --config=my-config.json

# Generate default configuration file for customization
ima2dicom --genconf
```

All command-line arguments are optional with sensible defaults:
- Input directory defaults to current directory
- Output directory defaults to current directory (auto-created if missing)
- Configuration defaults to built-in settings (or generate with `--genconf`)

## Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview)

### Dependencies
This project uses the following third-party libraries:
- **[LanguageExt.Core](https://github.com/louthy/language-ext)** (v4.4.9) - Functional programming library for C# (MIT License)
- **[fo-dicom](https://github.com/fo-dicom/fo-dicom)** (v5.2.5) - DICOM library for .NET

Third-party licenses can be found in the [LICENSES](./LICENSES) directory.

### Build and Test
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Publish self-contained executable (example for Linux)
dotnet publish ImaToDicomConverter/ImaToDicomConverter.csproj \
  -c Release \
  --self-contained true \
  -r linux-x64 \
  -p:PublishSingleFile=true \
  -o ./publish
```

## CI/CD

The project includes automated GitHub Actions workflows that:
- Build and test the code on every push to main
- Create self-contained releases for Linux, Windows, and macOS
- Automatically publish releases with downloadable executables

Releases are created automatically when code is pushed to the main branch or when pull requests are merged.


#Disclaimer

This project is not affiliated with, endorsed by, or supported by Siemens Healthineers or Siemens AG.

The .ima file format used by legacy Siemens CT systems is proprietary and undocumented.
This software was developed through:

Inspection of user-owned data

Publicly available documentation

Empirical testing in open-source viewers

No proprietary source code, SDKs, confidential documentation, or reverse-engineered binaries were used.

This software is provided “as is”, without warranty of any kind.
It is intended for research, educational, archival, and visualization purposes only.

Do not use this software for clinical diagnosis or medical decision-making.
