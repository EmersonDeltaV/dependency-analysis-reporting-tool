# Copilot Instructions

Full project conventions are in [AGENTS.md](../AGENTS.md). The essentials for immediate productivity are below.

## Project Layout

- `Src/` — source modules: `DART.Console` (entry point), `DART.Core` (contracts/orchestration), `DART.BlackduckAnalysis`, `DART.EOLAnalysis`, `DART.ReportGenerator`
- `Tests/` — five per-module xUnit projects: `DART.Console.Tests`, `DART.BlackduckAnalysis.Tests`, `DART.Core.Tests`, `DART.EOLAnalysis.Tests`, `DART.ReportGenerator.Tests`
- Legacy top-level `DART.*` folders are migration artifacts — ignore them

## Build and Test Commands

```powershell
dotnet build DependencyAnalysisReportingTool.sln -c Debug
dotnet test -c Debug
dotnet test Tests/DART.Core.Tests/DART.Core.Tests.csproj -c Debug   # single project
dotnet run --project Src/DART.Console/DART.Console.csproj -c Debug
```

## Key Conventions

- **Namespaces**: flat and project-based only — `namespace DART.Core;`, never `namespace DART.Core.Services;`
- **Test mocking**: xUnit + NSubstitute; never make real network or filesystem calls in tests
- **Test naming**: `MethodName_Should_When`
- **Logging**: Serilog (`ILogger`), not `Console.WriteLine`
- **Async**: suffix all async methods with `Async`
- **Config**: never commit real tokens; use placeholders in `Src/DART.Console/config.json`

## Common Pitfalls

- Adding new config keys requires updating both `Src/DART.Console/config.json` **and** `README.md`
- Windows paths in `config.json` must escape backslashes (`C:\\path`)
- Output and log directories must exist before running the tool
