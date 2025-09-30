# Dependency Analysis Reporting Tool (DART)
## Overview
The Dependency Analysis Reporting Tool (DART) is a comprehensive .NET 8 solution that analyzes both BlackDuck vulnerability reports and End-of-Life (EOL) dependency data, generating unified Excel reports with detailed mitigation recommendations. Unlike standard reports, DART provides actionable insights by combining vulnerability analysis with dependency lifecycle information, enabling development teams to make informed decisions about security risks and technical debt.
DART combines vulnerability scanning with dependency lifecycle analysis in a single, easy-to-use console application.
## Features
### BlackDuck Vulnerability Analysis
- Processes BlackDuck vulnerability reports (CSV format)
- Retrieves recommended fixes via BlackDuck API
- Identifies security risks and mitigation strategies
- Supports both manual report processing and automated download
### End-of-Life (EOL) Dependency Analysis
- Analyzes NuGet packages in Azure DevOps repositories
- Identifies outdated dependencies and their lifecycle status
- Provides upgrade recommendations and licensing information
- Calculates dependency age and latest version availability
### Unified Reporting
- Generates combined Excel reports with multiple worksheets
- Professional formatting with auto-fit columns and filtering
- Configurable feature toggles for flexible workflows
- Structured logging for audit trails
### Integration Capabilities
- Self-contained executable deployment
- Dependency injection architecture for extensibility
- Azure Pipelines support with parameterized configurations
## Getting Started
### Prerequisites
- **.NET 8.0 SDK** - Required for building and running the application
- **Microsoft Excel Desktop or Web** - For viewing generated reports
- **BlackDuck Account** - With read access for vulnerability analysis
- **Azure DevOps Access** - For EOL analysis of repositories (optional)
- **Git** - For cloning the repository
### Dependencies (Automatically Restored)
The following NuGet packages are automatically installed during build:
- **ClosedXML** - Excel file generation
- **Newtonsoft.Json** - JSON configuration and API responses  
- **Serilog** - Structured logging
- **Microsoft.Extensions.Hosting** - Dependency injection and hosted services
- **CsvHelper** - CSV file processing
- **Microsoft.TeamFoundationServer.Client** - Azure DevOps integration
- **NuGet.Configuration & NuGet.Protocol** - Package analysis
### Installation
1. Clone the repository:
```sh
git clone https://github.com/EmersonDeltaV/Amber_DependencyAnalyzer.git
cd Amber_DependencyAnalyzer
```
2. Restore dependencies:
```sh
dotnet restore
```
3. Build the solution:
```sh
# Debug build
dotnet build
# Release build  
dotnet build --configuration Release
```
4. (Optional) Create self-contained executable:
```sh
dotnet publish DART/DART.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish
```
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
### BlackDuck Auto-Download Configuration (Optional)
For automatic report downloading:
```json
{
  "DownloadConfiguration": {
    "VulnerabilityReportParameters": {
      "reportFormat": "CSV",
      "ProjectLinks": [
        "https://blackduck.company.com/api/projects/project-guid-1",
        "https://blackduck.company.com/api/projects/project-guid-2"
      ]
    },
    "DownloadParameters": {
      "MaxTries": 20
    }
  }
}
```
## Usage
### Setup Required Tokens
1. **BlackDuck Access Token**:
   - Login to your BlackDuck account
   - Navigate to 'Access Tokens' → 'Create Token'
   - **For manual report processing**: Select 'Read Access Only' scope
   - **For automatic report download**: Select 'Read and Write Access' scope (required when `EnableDownloadTool` is `true`)
   - Copy the token to `BlackduckToken` in config.json
2. **Azure DevOps PAT Token** (for EOL analysis):
   - Go to Azure DevOps → User Settings → Personal Access Tokens
   - Create token with 'Code (read)' permission
   - Copy the token to `EOLAnalysis.Pat` in config.json
### Running the Tool
1. **Option 1: Run directly with .NET**
```sh
dotnet run --project DART/DART.csproj
```
2. **Option 2: Run published executable**
```sh
# After publishing (see installation step 4)
./publish/DART.exe
```
3. **Option 3: Run as administrator** (recommended for file system access)
```sh
# Right-click and "Run as administrator" on Windows
```
### Understanding the Output
DART generates a single Excel file with multiple worksheets:
1. **"Black Duck Security Risks"** - Vulnerability analysis with mitigation recommendations
2. **"EOL Analysis"** - Dependency lifecycle information with upgrade suggestions
The output file follows the naming pattern: `blackduck-summary-YYYY-MM-DD-HHMMSS.xlsx`
### Workflow Options
**Full Analysis** (BlackDuck + EOL):
- Set both `EnableDownloadTool` and `EnableEOLAnalysis` to `true`
- Configure both BlackDuck and EOL analysis settings
- Run DART to get comprehensive dependency analysis
**BlackDuck Only**:
- Set `EnableEOLAnalysis` to `false`
- Focus on vulnerability analysis only
**EOL Only**:
- Set `EnableDownloadTool` to `false`
- Manually place BlackDuck CSV files in `ReportFolderPath` or leave empty
- Focus on dependency lifecycle analysis
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
## Architecture
### Project Structure
```
DependencyAnalysisReportingTool/
├── DART/                           # Main console application
│   ├── Program.cs                  # Entry point and DI configuration  
│   ├── BlackduckReportAnalysisProgram.cs  # Main workflow orchestration
│   ├── Services/                   # Core business services
│   ├── Models/                     # Configuration and data models
│   └── config.json                 # Application configuration
├── BlackduckReportGeneratorTool/   # BlackDuck API integration
└── DART.EOLAnalysis/               # EOL analysis functionality
```
### Key Technologies
- **.NET 8.0** - Runtime and SDK
- **Microsoft.Extensions.Hosting** - Dependency injection and hosting
- **ClosedXML** - Excel file generation and manipulation
- **Serilog** - Structured logging with multiple sinks
- **Azure DevOps REST APIs** - Repository and package analysis
- **NuGet Protocol APIs** - Package metadata and version information
## Contributing
Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request
### Development Setup
1. Clone the repository
2. Ensure .NET 8.0 SDK is installed
3. Run `dotnet restore` to install dependencies
4. Use your preferred IDE (Visual Studio, VS Code, Rider)
5. Build and test with `dotnet build` and `dotnet test`
## License
This project is licensed under the MIT License - see the LICENSE file for details.