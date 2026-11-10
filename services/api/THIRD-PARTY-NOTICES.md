# Third-party notices: TUMIIA API

Every dependency must be free for commercial use: MIT, Apache-2.0, BSD or the PostgreSQL licence
(see `docs/CONTRIBUTING.md`). Add a row here whenever you add a package, model or tool.

Licences were read from each package's own metadata (`.nuspec`) on 2026-11-04 (analysis packages on 2026-11-10). Transitive packages pulled
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
| Microsoft.AspNetCore.Identity.EntityFrameworkCore (+ Identity core and stores) | 10.0.14 | User accounts, password hashing (PBKDF2), lockout, reset tokens | MIT |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.14 | Data Protection keys stored in PostgreSQL | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.14 | Checks access tokens | MIT |
| Microsoft.IdentityModel.JsonWebTokens (+ IdentityModel tokens, protocols) | 8.23.0 / 8.19.2 | Creates and validates JWTs | MIT |
| AWSSDK.SimpleEmailV2 (+ AWSSDK.Core) | 4.0.106.7 | Sends email through SES | Apache-2.0 |
| AWSSDK.S3 | 4.0.104.9 | Pre-signed upload and download addresses, checking uploaded files | Apache-2.0 |
| AWSSDK.SQS | 4.0.100.23 | Queues documents for analysis, and the worker that reads the queue | Apache-2.0 |
| Anthropic | 13.2.1 | Anthropic's official C# SDK: calls Claude through the Messages API | Apache-2.0 |
| Anthropic.Bedrock (+ AWSSDK.SSO, AWSSDK.SSOOIDC, Microsoft.Extensions.AI.Abstractions, Microsoft.Bcl.Memory) | 1.0.1 | The Claude in Amazon Bedrock client (`bedrock-mantle` endpoint, AWS SigV4 signing) | Apache-2.0 (MIT for the Microsoft packages) |
| PdfPig | 0.1.16 | Reads the text layer of PDFs | Apache-2.0 |
| SkiaSharp, SkiaSharp.NativeAssets.Linux.NoDependencies (+ macOS, Win32 native assets) | 4.150.1 | Shrinks photos to JPEG before they are sent to Bedrock | MIT |
| DocumentFormat.OpenXml (+ OpenXml.Framework, System.IO.Packaging) | 3.5.1 / 10.0.2 | Reads Word (.docx) files | MIT |
| ExcelDataReader | 3.9.0 | Reads Excel (.xlsx, .xls) files | MIT |
| MetadataExtractor | 2.9.3 | Reads photo EXIF data (date taken, GPS, camera) | Apache-2.0 |
| XmpCore (transitive, MetadataExtractor) | 6.1.10.1 | XMP metadata inside photos | BSD-3-Clause (Adobe's XMP Toolkit licence; the package points to Adobe's page, so recheck before release) |
| .NET 10 runtime and ASP.NET Core (includes the built-in rate limiter) | 10.0 | Runtime | MIT |

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
| Microsoft.Extensions.TimeProvider.Testing | 10.11.0 | Fake clock for expiry tests | MIT |
| Microsoft.NET.Test.Sdk | 18.11.0 | Test host | MIT |
| coverlet.collector | 10.1.0 | Line coverage | MIT |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.14 | In-memory API for integration tests | MIT |
| Testcontainers.PostgreSql | 4.16.0 | Real PostgreSQL in Docker for integration tests | MIT |
| Testcontainers.LocalStack | 4.16.0 | LocalStack (S3, SQS) in Docker for integration tests | MIT |

## In the container image (`services/api/Dockerfile`)

| Component | Version | Used for | Licence |
|---|---|---|---|
| `mcr.microsoft.com/dotnet/aspnet:10.0-noble` | .NET 10 on Ubuntu 24.04 LTS | Runtime base image | MIT (.NET); Ubuntu packages under their own licences |
The image holds no OCR engine or AI model. Documents are read by Claude on Amazon Bedrock (an AWS service, not a
dependency shipped with the API); see the README for what that means for where data is processed.

## Infrastructure (`infra/`, not shipped with the API)

| Package or tool | Version | Used for | Licence |
|---|---|---|---|
| Amazon.CDK.Lib (+ Amazon.JSII.Runtime and CDK asset packages) | 2.275.0 | AWS CDK in C# | Apache-2.0 |
| Constructs | 10.8.1 | CDK construct base | Apache-2.0 |
| aws-cdk (npm CLI, run with npx) | 2.1147.0 | `cdk synth`, `diff`, `deploy`, `bootstrap` | Apache-2.0 |

## Local services (docker-compose, development only)

| Image | Used for | Licence |
|---|---|---|
| `postgres:17` | Local database | PostgreSQL |
| `localstack/localstack:4.9` | Local fake S3 and SQS | Apache-2.0 (community edition) |
