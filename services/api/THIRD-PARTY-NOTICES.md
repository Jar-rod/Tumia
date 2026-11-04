# Third-party notices: TUMIIA API

Every dependency must be free for commercial use: MIT, Apache-2.0, BSD or the PostgreSQL licence
(see `docs/CONTRIBUTING.md`). Add a row here whenever you add a package, model or tool.

Licences were read from each package's own metadata (`.nuspec`) on 2026-11-04. Transitive packages pulled
in by the ones below were checked the same way; all are MIT, Apache-2.0, BSD-3-Clause or PostgreSQL.

## Shipped with the API

| Package | Version | Used for | Licence |
|---|---|---|---|
| Asp.Versioning.Mvc, Asp.Versioning.Mvc.ApiExplorer | 10.2.1 | `/api/v1/...` URL versioning | MIT |
| Asp.Versioning.OpenApi | 10.2.3 | One OpenAPI document per API version | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.14 | Generates the OpenAPI documents | MIT |
| Microsoft.OpenApi (transitive) | 2.12.2 | OpenAPI object model | MIT |
| Swashbuckle.AspNetCore.SwaggerUI | 10.3.1 | Swagger UI page at `/swagger` | MIT |
| Serilog.AspNetCore (+ Serilog, sinks, settings) | 10.0.0 | Structured logging and request logging | Apache-2.0 |
| Serilog.Formatting.Compact | 3.0.0 | Compact JSON log format for CloudWatch | Apache-2.0 |
| Microsoft.EntityFrameworkCore | 10.0.14 | Database access | MIT |
| Npgsql.EntityFrameworkCore.PostgreSQL (+ Npgsql) | 10.0.3 | PostgreSQL provider | PostgreSQL |
| EFCore.NamingConventions | 10.0.1 | snake_case table and column names | Apache-2.0 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.14 | Database check in `/api/v1/health` | MIT |
| .NET 10 runtime and ASP.NET Core | 10.0 | Runtime | MIT |

## Build and development only (not shipped)

| Package or tool | Version | Used for | Licence |
|---|---|---|---|
| Microsoft.Extensions.ApiDescription.Server | 10.0.14 | Writes `openapi/v1.json` at build time | MIT |
| Microsoft.EntityFrameworkCore.Design | 10.0.14 | EF migrations tooling | MIT |
| dotnet-ef (local tool) | 10.0.14 | `dotnet ef migrations ...` | MIT |
| dotnet-reportgenerator-globaltool (local tool) | 5.5.12 | Coverage reports | Apache-2.0 |
| xunit.v3, xunit.runner.visualstudio | 4.0.2, 4.0.1 | Test framework | Apache-2.0 |
| Shouldly | 4.3.0 | Test assertions | BSD-3-Clause |
| NSubstitute | 6.2.0 | Test doubles | BSD-3-Clause |
| Microsoft.NET.Test.Sdk | 18.11.0 | Test host | MIT |
| coverlet.collector | 10.1.0 | Line coverage | MIT |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.14 | In-memory API for integration tests | MIT |
| Testcontainers.PostgreSql | 4.16.0 | Real PostgreSQL in Docker for integration tests | MIT |

## Local services (docker-compose, development only)

| Image | Used for | Licence |
|---|---|---|
| `postgres:17` | Local database | PostgreSQL |
| `localstack/localstack:4.9` | Local fake S3 and SQS | Apache-2.0 (community edition) |
