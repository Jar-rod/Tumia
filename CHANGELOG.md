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
- CI workflow (`.github/workflows/ci.yml`) on every pull request and push to `main`. It never deploys. Checks: `backend` (format, Release build, tests, 70% line coverage gate, coverage report as an artifact and in the job summary), `frontend` (lint, type check, Vitest with 70% line coverage, build; passes with a note until `apps/web` exists), `api-contract` (fails if `openapi/v1.json` is out of date), `docker` (builds the ARM64 image without pushing; passes with a note until the Dockerfile exists in Phase 2) and `changelog` (fails if code changed without a `CHANGELOG.md` entry, unless the PR has the `no-changelog` label). Each job runs only when its folder changed; `changelog` always runs.
- Pull request template (`.github/pull_request_template.md`) with the checklist from `docs/CONTRIBUTING.md`.
- AWS infrastructure as CDK (C#) in `infra/`, all in af-south-1: GitHub OIDC deploy role (no long-lived keys), VPC without a NAT Gateway, RDS PostgreSQL 17 (`db.t4g.micro`, encrypted, private, 7-day backups, deletion protection), private versioned documents bucket, `prod-document-analysis` queue with dead-letter queue and an email alarm, ECS Fargate on ARM64 behind an HTTPS load balancer that only CloudFront can reach, S3 + CloudFront for the React app with `/api/*` routed to the API, and an SES domain identity. CloudFront sends a secret `X-Origin-Verify` header (generated in Secrets Manager) to the API.
- CDK assertion tests (`infra/tests`) for the key rules: database not public, buckets block public access, no NAT gateway, ARM64 tasks, dead-letter alarm, CloudFront-only load balancer, originals can never be deleted, Bedrock off by default.
- `services/api/Dockerfile`: multi-stage ARM64 image with Tesseract 5, the pinned `all-MiniLM-L6-v2` model (SHA-256 checked) and the EF Core migrations bundle at `/app/efbundle`. Runs as the non-root `app` user, with a container health check on `/api/v1/health`.
- Manual deploy workflow (`.github/workflows/deploy.yml`) with targets `auto`, `frontend`, `backend`, `both` and `infra`. Tests always run first. A backend deploy runs the database migrations as a one-off ECS task before the new version starts, and stops if they fail. Successful deploys are tagged `deploy-<part>-<date>-<sha>`, which `auto` uses to deploy only what changed.
- `docs/deploy-setup.md`: the one-off manual steps (enable af-south-1, CDK bootstrap, JWT key, GitHub environment and variables, SES sandbox).
- CI job `infra` runs the CDK tests when `infra/` changes.

- User accounts with ASP.NET Core Identity: `POST /api/v1/auth/register`, `login`, `refresh`, `logout`, `forgot-password`, `reset-password` and `GET /api/v1/auth/me`. Passwords need 10+ characters; 5 wrong passwords lock an account for 15 minutes, and every login failure gets the same answer. Access tokens are 15-minute JWTs (HS256); refresh tokens are random, stored only as SHA-256 hashes, rotated on every use, and kept in an HttpOnly, Secure, SameSite=Strict cookie for 14 days. Reusing an already-rotated refresh token ends that whole sign-in. A password reset ends every sign-in of the user.
- Roles `User` and `Admin`; anyone listed in `Admin:Emails` becomes an Admin when they register or sign in.
- Password reset emails through Amazon SES (`Email:Provider=Ses` in AWS). Locally, emails are written to `.dev-emails/` instead of being sent. Data Protection keys are stored in PostgreSQL so reset links survive restarts.
- `audit_events` table recording `Registered`, `LoginSucceeded`, `LoginFailed`, `LockedOut`, `PasswordResetRequested`, `PasswordReset`, `RefreshTokenReuseDetected` and `LoggedOut`, with user id, IP, time and success.
- Rate limiting (built-in ASP.NET Core): sign-in endpoints 5 a minute per client IP, upload requests 30 a minute per user, everything 120 a minute per user or IP. Over the limit: 429 with `Retry-After` and a ProblemDetails body. Limits are in the `RateLimiting` configuration section.
- Outside Development the API rejects (403) requests without the correct `X-Origin-Verify` header, i.e. that did not come through CloudFront. The load balancer's health check (`/api/v1/health`) is exempt.
- `ClientIpResolver`: uses CloudFront's `CloudFront-Viewer-Address` header only when the origin check passed, otherwise the connection address.
- `docs/frontend-integration.md` with the sign-in rules for the React app (token in memory, refresh on load and after a 401, `credentials: "include"`).
- Migration `AddUsersRefreshTokensAndAuditEvents`: `users`, `roles` (User and Admin seeded), `user_roles`, `refresh_tokens`, `audit_events`, `data_protection_keys` and the other Identity tables.
- Optional `ADMIN_EMAILS` deploy variable, passed to the API in AWS.

- Project vaults: `POST /api/v1/projects`, `GET /api/v1/projects`, `GET /api/v1/projects/{projectId}`. The creator is the project's Owner (`project_members`, so more roles can be added later).
- Uploading straight from the browser to S3, never through the API: `POST /api/v1/projects/{projectId}/documents/upload-requests` checks up to 50 files (type, size up to 50 MB, SHA-256) and returns a 5-minute pre-signed PUT address per new file, with the exact size and content type signed in so S3 refuses anything else; a file already in the project returns `existingDocumentId` instead. `POST /api/v1/documents/{documentId}/uploaded` checks the file is in S3 at the right size, marks the document `New` and sends `{ documentId }` to the analysis queue. Allowed types and limits are in the `Uploads` configuration section.
- `GET /api/v1/projects/{projectId}/documents` (filter by `status` and `category`), `GET /api/v1/documents/{documentId}`, and `GET /api/v1/documents/{documentId}/source-url` (5-minute address; PDFs and common images open in the browser, other files download under their original name).
- `documents` table with the v1.1 record (storage key `originals/{projectId}/{documentId}`, claimed and detected content type, size, SHA-256, capture source, status, consequence level, confirmed fields as `jsonb`, …), unique per project and SHA-256. New status `PendingUpload` for a file between the two upload calls.
- One `ProjectAccessGuard` for every project and document endpoint: a project or document the caller does not belong to answers 404, exactly like one that does not exist.
- Audit events `DocumentUploadRequested` and `DocumentUploaded`, with a new `document_id` column on `audit_events`.
- Migration `AddProjectsAndDocuments`.
- Integration tests run the upload flow against a LocalStack container (S3 and SQS).

### Changed
- API enums are now written as names in JSON and in the OpenAPI contract (`"New"`, `"drag_drop"`), not numbers.
- Local LocalStack checks pre-signed URL signatures like real S3 (`S3_SKIP_SIGNATURE_VALIDATION=0`) and the local documents bucket allows browser uploads from `http://localhost:5173`.
- Every controller action now needs a signed-in user unless it has `[AllowAnonymous]`. Swagger UI shows a padlock on every operation that needs a token.
- The request log now records the real client IP (from CloudFront in AWS) and whether the request was rate limited.
- In AWS the API gets `App__BaseUrl` (for links in emails), `Email__Provider=Ses` and `Email__FromAddress=no-reply@<domain>` instead of `Email__FromDomain`.
- The API can now build its database connection from separate `Database:Host`, `Database:Port`, `Database:Name`, `Database:Username` and `Database:Password` settings (as supplied by the RDS secret in AWS), and then requires TLS. A full `ConnectionStrings:Tumiia` still takes priority, as used locally.
- The EF Core tools and the migrations bundle read the connection from environment variables, falling back to the local Docker database.
