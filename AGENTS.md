# Repository Guidelines

## Project Structure and Module Organization
- Canonical code lives under `Src/` and `Tests/`. If similarly named top-level `DART.*` folders exist, treat them as legacy/migration artifacts unless the solution explicitly references them.
- `Src/DART.Console/` - .NET 8 console entry point (`Program.cs`, `DartOrchestrator.cs`) and runtime config (`config.json`, copied to output). Writes logs via Serilog and emits reports to `ReportConfiguration.OutputFilePath`.
- `Src/DART.Core/` - shared contracts, configuration models, and orchestration abstractions. Keep this project contract-oriented; do not add host-facing DI registration here.
- `Src/DART.Runtime/` - host-facing runtime composition for reusable orchestration (`DartExecutionRequest`, `IDartExecutionRunner`, `AddDartRuntime`) consumed by non-console hosts and by `DART.Console`.
- `Src/DART.BlackduckAnalysis/` - Black Duck collectors, parsing helpers, and API/report/file services.
- `Src/DART.EOLAnalysis/` - NuGet and NPM lifecycle analysis plus Azure DevOps clients and repository processors.
- `Src/DART.ReportGenerator/` - workbook and comparison report generation services (`IReportGenerator`, `ReportGenerator`).
- `Tests/` - xUnit test projects split by module (`DART.Core.Tests`, `DART.Runtime.Tests`, `DART.BlackduckAnalysis.Tests`, `DART.Console.Tests`, `DART.EOLAnalysis.Tests`, `DART.ReportGenerator.Tests`) plus any package-validation coverage added by the solution.
- `Docs/` - supporting documentation and migration/design notes.
- `artifacts/` and `coverage/` - generated build/test outputs.
- Solution file: `DependencyAnalysisReportingTool.sln` includes all projects above.

## Build, Test, and Development Commands
- Restore all projects: `dotnet restore`
- Build solution (Debug): `dotnet build DependencyAnalysisReportingTool.sln -c Debug`
- Run app (uses `Src/DART.Console/config.json`): `dotnet run --project Src/DART.Console/DART.Console.csproj -c Debug`
- Run all tests: `dotnet test DependencyAnalysisReportingTool.sln -c Debug`
- Run tests with coverage: `dotnet test DependencyAnalysisReportingTool.sln -c Debug --collect:"XPlat Code Coverage"`
- Run a subset of tests: `dotnet test Tests/DART.Runtime.Tests/DART.Runtime.Tests.csproj -c Debug --filter FullyQualifiedName~NamespaceOrClass`
- Publish single-file (Windows x64): `dotnet publish Src/DART.Console/DART.Console.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true`

## Coding Style and Naming Conventions
- C#/.NET style: 4-space indentation, `nullable` enabled, implicit usings on.
- Naming: PascalCase for types/members, camelCase for locals/parameters, interfaces prefixed with `I`, and async methods ending with `Async`.
- File layout: one public type per file; filename matches type (for example, `EOLAnalysisService.cs`). Organize by feature (`Services`, `Models`, `Clients`, `Helpers`, `Configuration`).
- Namespace policy for library projects: keep namespaces flat and project-based only. Use `namespace DART.Core;`, `namespace DART.BlackduckAnalysis;`, `namespace DART.EOLAnalysis;`, and `namespace DART.ReportGenerator;`.
- Do not add folder-based namespace suffixes for library projects (for example, avoid `namespace DART.Core.Services;`).
- Exceptions: `DART.Console` and test projects may use hierarchical namespaces as needed.
- Logging: prefer Serilog (`ILogger`) over `Console.WriteLine`.

## Testing Guidelines
- Frameworks: xUnit and NSubstitute; coverage via `coverlet.collector`.
- Test location: `Tests/DART.Tests/`; mirror production module structure where practical.
- Naming: files end with `Tests.cs`; test methods follow `MethodName_Should_When`.
- Unit tests must mock external I/O (network, filesystem). Keep tests deterministic and parallel-safe.

## Commit and Pull Request Guidelines
- Commits: concise, imperative subject (for example, `DART: add EOL config validation`). Group related changes only.
- PRs: include summary, linked issues, validation steps (commands/config used), and sample output location (for example, `OutputFilePath`). Call out any config schema changes.

## Security and Configuration Tips
- Never commit real tokens (Black Duck, Azure DevOps). Use placeholders in `Src/DART.Console/config.json`; keep secrets local.
- Paths in `config.json` on Windows must escape backslashes (for example, `C:\\BlackDuck`).
- Ensure output and log directories exist before running the tool.

## Agent-Specific Instructions
- Keep `TargetFramework` as `net8.0` and preserve publish properties in `Src/DART.Console/DART.Console.csproj`.
- When adding configuration keys, update both `Src/DART.Console/config.json` and `README.md` with defaults and descriptions.
- Avoid introducing runtime network calls in tests; mock services and clients behind interfaces.
- Keep host-facing runtime composition in `DART.Runtime`; do not reintroduce DI aggregation into `DART.Core`.
