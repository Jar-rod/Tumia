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
| Microsoft.AspNetCore.Identity.EntityFrameworkCore (+ Identity core and stores) | 10.0.14 | User accounts, password hashing (PBKDF2), lockout, reset tokens | MIT |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.14 | Data Protection keys stored in PostgreSQL | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.14 | Checks access tokens | MIT |
| Microsoft.IdentityModel.JsonWebTokens (+ IdentityModel tokens, protocols) | 8.23.0 / 8.19.2 | Creates and validates JWTs | MIT |
| AWSSDK.SimpleEmailV2 (+ AWSSDK.Core) | 4.0.106.7 | Sends email through SES | Apache-2.0 |
| AWSSDK.S3 | 4.0.104.9 | Pre-signed upload and download addresses, checking uploaded files | Apache-2.0 |
| AWSSDK.SQS | 4.0.100.23 | Queues documents for analysis | Apache-2.0 |
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
| tesseract-ocr, libtesseract5, tesseract-ocr-eng | 5.3.4 (Ubuntu package) | OCR, run as a separate process | Apache-2.0 |
| Leptonica (libleptonica6, Tesseract dependency) | Ubuntu package | Image handling for Tesseract | BSD-2-Clause |
| sentence-transformers/all-MiniLM-L6-v2 | revision `1110a243fdf4706b3f48f1d95db1a4f5529b4d41` | Sentence embeddings (used from Phase 5) | Apache-2.0 |

The model files are checked against these SHA-256 hashes when the image is built:
`onnx/model.onnx` `6fd5d72fe4589f189f8ebc006442dbb529bb7ce38f8082112682524616046452` (matches the hash Hugging Face
publishes for that revision) and `vocab.txt` `07eced375cec144d27c900241f3e339478dec958f92fddbc551f295c992038a3`.

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
