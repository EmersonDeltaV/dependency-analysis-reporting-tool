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

DART uses a single `config.json` file in the DART folder for all configuration settings.

### Basic Configuration

Navigate to the `DART/config.json` file and configure the following core settings:
```json
{
  "ReportFolderPath": "C:\\BlackDuck\\Tool\\Reports",
  "OutputFilePath": "C:\\BlackDuck\\Tool\\Analysis", 
  "LogPath": "C:\\BlackDuck\\Log",
  "BlackduckToken": "your-blackduck-token-here",
  "BaseUrl": "https://your-blackduck-url.com",
  "ProductName": "Your Product Name",
  "ProductVersion": "1.0",
  "ProductIteration": "Sprint 1"
}
```

### Feature Toggles

Control which analysis features are enabled:
```json
{
  "FeatureToggles": {
    "EnableDownloadTool": true,
    "EnableEOLAnalysis": true
  }
}
```

### EOL Analysis Configuration

To enable dependency end-of-life analysis, add the EOLAnalysis section:
```json
{
  "EOLAnalysis": {
    "Pat": "your-azure-devops-pat-token",
    "Repositories": [
      {
        "Name": "ProjectName",
        "Url": "https://dev.azure.com/org/project/_git/repository",
        "Branch": "main"
      }
    ]
  }
}
```

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
- Verify `ReportFolderPath` contains CSV files for BlackDuck analysis
- Ensure Azure DevOps repositories are accessible and contain supported project files
- Check that feature toggles are correctly configured

### Logging

DART uses Serilog for comprehensive logging:
- Console output shows real-time progress
- File logs are saved to the configured `LogPath` with timestamp rotation
- Log level can be configured in the Serilog section of config.json

## License

This project is licensed under the MIT License - see the LICENSE file for details.
