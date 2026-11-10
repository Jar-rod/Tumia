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

- Manual **Start or stop AWS** workflow (`.github/workflows/environment.yml`, script `.github/scripts/environment.sh`): `stop` scales the API to 0 tasks and stops the database, `start` starts the database and runs 1 API task again, `status` shows both. Stopped, the environment costs about US$0.75 a day (the load balancer cannot be stopped). Deploys start a stopped database first.
- The deploy role may describe databases, and stop or start only those tagged `project=tumiia` and `env=<environment>`. The data stack outputs `DatabaseInstanceId`.

- Optional `ENABLE_SWAGGER` deploy variable: when `true`, the API in AWS serves Swagger UI at `/swagger` and the OpenAPI documents at `/openapi/`, and CloudFront routes `/swagger*` and `/openapi/*` to the API (uncached, like `/api/*`). Off by default.

- **Pasting text.** `POST /api/v1/projects/{projectId}/documents/text` with `{ text, title? }` stores pasted text or a copied message unchanged as a `.txt` original (capture source `paste`) and queues it for analysis like an upload. 201 for new text, 200 with the existing document when the same text is already in the project.
- **Document analysis with Claude on Amazon Bedrock.** A background worker in the API process reads the analysis queue one message at a time (20-second long polling) and analyses each document. It deletes the message only after the result is saved. A repeated message does nothing. After 3 failed tries the document is marked `Failed` with a plain reason, and SQS moves the message to the dead-letter queue. Switch the worker off with `Analysis:WorkerEnabled=false`.
  - Detects the real file type from its first bytes. A mismatch with the extension is recorded and lowers confidence.
  - Sends each document to Claude Haiku 5.5 on Amazon Bedrock (`anthropic.claude-haiku-5-5`, Bedrock Messages API endpoint in af-south-1, signed with the task role). PDFs, typed or scanned, go as PDFs. Photos and screenshots are shrunk to JPEG. Word, Excel, CSV, text, DXF and pasted text go as the text TUMIIA reads from them. The answer is structured JSON: category (from the taxonomy), confidence, runner-up, readability, language, facts with page and quoted evidence, and warnings. The document's own text is treated as data, never as instructions.
  - **Data residency:** from af-south-1, Bedrock serves Claude only through its global endpoint, so document content is processed outside South Africa. Files, the database and the API stay in Cape Town.
  - Checks the AI's facts before keeping them. Amounts, dates and currencies must read properly (they are rewritten as `18450.00`, `yyyy-MM-dd` and ISO codes), otherwise they are dropped with a warning. When the document has typed text, each quoted snippet must be in it; otherwise the fact keeps half its confidence and is marked "not found in the document's text". Photo date and GPS come from EXIF.
  - Scores the classification confidence from weighted signals (weights in `Analysis:Weights`): the AI's confidence and margin, readability, whether the keyword rules agree, expected fields found, and format match. Then decides: sure and not high-consequence: `AutoFiled`; fairly sure, or any payment, contract or legal record: `AwaitingConfirmation`; not sure: `NeedsReview`. Thresholds are per category.
  - **Fallback:** when Bedrock is off (`Bedrock:Enabled=false`, the default locally and in tests) or does not answer (after the SDK's 2 retries, a refusal, an unreadable answer, a PDF over 20 MB, or a TIFF or HEIC image), the keyword rules classify the document. Their confidence is kept below Medium, so the document goes to Needs Review with the reason recorded.
  - Writes a one-line plain-language summary from a template.
  - Each analysis records its analyser: `bedrock` and the model id, or `local` for keywords only.
- **Taxonomy in the database.** New `taxonomy_categories` table, filled from `Seed/taxonomy.json` (the 11 v1.1 categories with keywords, patterns, subject chips, expected fields, fields to save, consequence and thresholds) the first time it is read. Later edits to the table are never overwritten. The categories, descriptions, subject chips and labelled fields are also what Bedrock is told to choose from.
- `GET /api/v1/documents/{id}` now includes `analysis` (proposed category, runner-up, confidence and band, outcome, summary, fields with confidence and evidence, signals, warnings) and `failureReason`.
- Append-only `document_analyses` table: one row per document and pipeline version, never updated, so the AI's original proposal is always kept. Audit events `DocumentAnalysed` and `DocumentAnalysisFailed`.
- Migration `AddDocumentAnalysesAndTaxonomy` (new tables, and `latest_analysis_id` and `failure_reason` on `documents`; nothing existing is changed).
- `docs/frontend-integration.md`: pasting text, the statuses after analysis, and how to read and show the analysis.
- **Bedrock in AWS.** Deploys now send documents to Bedrock unless the `ENABLE_BEDROCK` variable is `false`. The task role gets `bedrock-mantle:CreateInference`, and the container gets `Bedrock__Enabled`, `Bedrock__Region` and `Bedrock__ModelId`. The model can be changed with the optional `BEDROCK_MODEL_ID` variable; `BEDROCK_INFERENCE_PROFILE_ARN` is no longer used. Model access for Claude Haiku 5.5 must be enabled in the Bedrock console first.

### Changed
- **A custom domain is now optional for AWS.** Without `DOMAIN_NAME` (and `HOSTED_ZONE_ID`, `HOSTED_ZONE_NAME`), users open the CloudFront address (`*.cloudfront.net`), no certificate or Route 53 records are made, and password reset emails come from a single SES-verified address: the new optional `SENDER_EMAIL` deploy variable, defaulting to `ALERT_EMAIL`. Setting the domain variables later and deploying `infra` switches to the domain.
- The API's load balancer is now **internal** (no public address), in new isolated `load-balancer` subnets, with an HTTP listener on port 80. CloudFront reaches it through a **CloudFront VPC origin**, so it no longer needs the `origin.<domain>` certificate and DNS record. It still only accepts CloudFront's prefix list, and CloudFront still sends `X-Origin-Verify`.
- The API reads `App__BaseUrl` from the SSM parameter `/prod/tumiia/app-base-url`, written by the web stack (the domain, or the CloudFront address), instead of a plain environment variable.
- The database now selects its subnets by name (`database`), since the VPC has two isolated subnet groups.
- API enums are now written as names in JSON and in the OpenAPI contract (`"New"`, `"drag_drop"`), not numbers.
- Local LocalStack checks pre-signed URL signatures like real S3 (`S3_SKIP_SIGNATURE_VALIDATION=0`) and the local documents bucket allows browser uploads from `http://localhost:5173`.
- Every controller action now needs a signed-in user unless it has `[AllowAnonymous]`. Swagger UI shows a padlock on every operation that needs a token.
- The request log now records the real client IP (from CloudFront in AWS) and whether the request was rate limited.
- In AWS the API gets `App__BaseUrl` (for links in emails), `Email__Provider=Ses` and `Email__FromAddress=no-reply@<domain>` instead of `Email__FromDomain`.
- The API can now build its database connection from separate `Database:Host`, `Database:Port`, `Database:Name`, `Database:Username` and `Database:Password` settings (as supplied by the RDS secret in AWS), and then requires TLS. A full `ConnectionStrings:Tumiia` still takes priority, as used locally.
- The EF Core tools and the migrations bundle read the connection from environment variables, falling back to the local Docker database.
