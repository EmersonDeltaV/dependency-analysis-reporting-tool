Test organization

- DART: tests for console app (orchestrator, services, models).
- DART.BlackduckAnalysis: tests for Black Duck library (services, models, constants).
- DART.EOLAnalysis: tests for EOL library (clients, configuration, helpers, services, models).

Naming

- Place tests under the matching folder tree.
- Name files as `*.Tests.cs` and methods as `MethodName_Should_When`.

Examples

- DART/Orchestrator/DartOrchestrator.Tests.cs
- DART.BlackduckAnalysis/Services/Implementation/BlackduckReportService.Tests.cs
- DART.EOLAnalysis/Services/NugetMetadataService.Tests.cs
