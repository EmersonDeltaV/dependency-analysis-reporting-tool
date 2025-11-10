# Dependency Analysis Reporting Tool (DART)

## Overview

Blackduck and End-of-Life (EOL) Analysis reports that once took hours or even days to create manually can now be generated in just minutes.

The **Dependency Analysis Report Tool (DART)** is a .NET console application that provides actionable insights by merging vulnerability data with dependency lifecycle information, enabling development teams to make informed decisions about security risks and technical debt every PI.

## Key Benefits

- Unified Excel reports combining Blackduck vulnerability and EOL analysis
- Automated Blackduck report downloading and processing
- NuGet package lifecycle analysis for Azure DevOps repositories

## Getting Started

### Prerequisites

- **Microsoft Excel Desktop or Web** - For viewing generated reports
- **BlackDuck Account** - With read access for vulnerability analysis
- **Azure DevOps Access** - For EOL analysis of repositories (optional)

### How to Guide

https://emerson.stackenterprise.co/articles/4266

## Configuration

Navigate to the `DART/config.json` file and configure the following core settings:

### Report Configuration

```json
{
  "ReportConfiguration": {
    "OutputFilePath": "C:\\BlackDuck\\Tool\\Analysis",
    "LogPath": "C:\\BlackDuck",
    "ProductName": "ProductX",
    "ProductVersion": "v1.1",
    "ProductIteration": "PIXX"
    },
```

### Black Duck Configuration

```json
"BlackduckConfiguration": {
  "BaseUrl": "https://blackduck.emrsn.org",
  "Token": "your-blackduck-token",
  "IncludeTransitiveDependency": "true",
  "ProjectVersionsToInclude": "main",
  "PreviousResults": "",
  "CurrentResults": "",
  "BlackduckRepositories": [
    {
      "Name": "ProjectX",
      "Url": "https://blackduck.emrsn.org/api/projects/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    },
    {
      "Name": "ProjectX",
      "Url": "https://blackduck.emrsn.org/api/projects/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    }
  ],
  "DownloadParameters": {
    "MaxTries": 20,
    "PollingDelayMilliseconds": 5000
  }
},
```

### Feature Toggles

```json
{
  "FeatureToggles": {
    "EnableEOLAnalysis": true,
    "EnableBlackduckAnalysis": true
  }
}
```

- EnableBlackduckAnalysis: When true, runs all Black Duck download, processing, and comparison steps. When false, Black Duck steps are skipped. Defaults to true. Black Duck configuration is only required when this is true.
- EnableEOLAnalysis: When true, adds an EOL analysis sheet. Can run standalone (with Black Duck disabled) or alongside Black Duck. Requires EOL repo configuration.

### Run Modes

- Black Duck only: `EnableBlackduckAnalysis = true`, `EnableEOLAnalysis = false`
- EOL only: `EnableBlackduckAnalysis = false`, `EnableEOLAnalysis = true`
- Both: `EnableBlackduckAnalysis = true`, `EnableEOLAnalysis = true`

### EOL Analysis Configuration

```json
{
  "EOLAnalysis": {
    "Pat": "your-azure-devops-pat-token",
    "NuGetApiUrl": "https://api.nuget.org/v3/index.json",
    "Repositories": [
      {
        "Name": "ProjectName",
        "Url": "https://dev.azure.com/org/project/_git/repository",
        "Branch": "main"
      }
    ],
    "PackageRecommendation": {
      "OldPackageThresholdYears": 3.0,
      "NearEolThresholdYears": 2.0,
      "SkipInternalPackagesFilter": [
        "Emerson.*"
      ],
      "Messages": {
        "OldPackageDefault": "Package is over 3 yrs old; investigate or replace/remove.",
        "UpdateToNewer": "Update to newer version",
        "NearEolUpdate": "Near EOL consider updating to newer version",
        "NoAction": "N/A",
        "ToBeDecided": "TBD",
        "SkipInternal": "Skip. Internal package"
      }
    }
  }
}
```

- SkipInternalPackagesFilter: Optional wildcard patterns (supports `*` and `?`) used to mark packages by Id as internal/skipped. Matching packages remain in results with the Recommended Action set to `Messages.SkipInternal`.

## Troubleshooting

### Common Issues

**Build Errors**:
- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` to restore all dependencies
- Check that all project references are correctly configured
**Configuration Issues**:
- Use double backslashes (`\\`) in Windows file paths in config.json
- Ensure all required directories exist and are accessible
- Verify tokens have appropriate permissions (BlackDuck: Read Access, Azure DevOps: Code Read)
**Runtime Issues**:
- Run as administrator if encountering file access issues
- Check network connectivity to BlackDuck and Azure DevOps services
- Review log files in the configured `LogPath` directory
**Empty Results**:
- Ensure Azure DevOps repositories are accessible and contain supported project files
- Check that feature toggles are correctly configured

### Logging

DART uses Serilog for comprehensive logging:
- Console output shows real-time progress
- File logs are saved to the configured `LogPath` with timestamp rotation
- Log level can be configured in the Serilog section of config.json

## License

This project is licensed under the MIT License - see the LICENSE file for details.

