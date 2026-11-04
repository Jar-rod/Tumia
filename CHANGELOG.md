# Changelog

All notable changes to this project are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Repository skeleton, coding standards (`docs/CONTRIBUTING.md`) and this changelog.
- Backend solution `services/api/Tumiia.sln` (.NET 10) with the Api, Application, Domain and Infrastructure projects and unit and integration test projects. Warnings are errors; package versions are set once in `Directory.Packages.props`.
- `GET /api/v1/health`: anonymous health check that also checks the database connection. Returns 200 when healthy, 503 when not.
- API versioning in the URL (`/api/v1/...`); responses carry an `api-supported-versions` header.
- Swagger UI at `/swagger` and the OpenAPI document at `/openapi/v1.json`, with an "Authorize" button for JWT bearer tokens. On by default in Development only (`Swagger:Enabled`).
- The OpenAPI contract is written to `services/api/src/Tumiia.Api/openapi/v1.json` on every build and committed for the frontend.
- Every error response is RFC 7807 ProblemDetails with a `correlationId`; no stack traces outside Development.
- Serilog JSON logging: one line per request with route template (not the raw path), API version, status, duration, user id, client IP and correlation id. The `X-Correlation-Id` header is accepted and echoed back.
- EF Core `TumiiaDbContext` (PostgreSQL, snake_case names) with an empty `InitialCreate` migration, applied on start-up in Development only.
- `docker-compose.yml` with PostgreSQL 17 and LocalStack; LocalStack creates the documents bucket and the analysis queue with its dead-letter queue.
- `SwaggerDocumentTests`: fails if any controller action is missing from the OpenAPI document, has no summary, or leaves out a response code it declares.
- CORS for the Vite dev server (`http://localhost:5173`) in Development only.
- `services/api/THIRD-PARTY-NOTICES.md` listing every dependency and its licence.
