# TUMIIA

Intelligent filing for construction project documents. A user drops messy project files into a project
vault; TUMIIA works out what each file is, proposes where it belongs, and the user confirms or corrects it.

| Folder | What it is |
|---|---|
| `services/api` | ASP.NET Core API (.NET 10): controllers, use cases, domain rules, database, tests |
| `apps/web` | React app (Vite). Built separately by the frontend developer |
| `infra` | AWS CDK infrastructure (C#) |
| `docs` | Coding standards (`CONTRIBUTING.md`) and architecture decisions (`adr/`) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22 LTS](https://nodejs.org/) (for the React app)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), running

## Run locally

1. Start PostgreSQL and LocalStack (fake S3 and SQS) from the repo root:

   ```bash
   docker compose up -d
   ```

   LocalStack creates the `local-tumiia-documents` bucket (with CORS for the Vite dev server) and the
   `local-document-analysis` queue (with its dead-letter queue) on start-up. It checks pre-signed URL signatures
   like real S3, so an upload of the wrong size or type is refused locally too. Look inside with, for example,
   `docker compose exec localstack awslocal s3 ls s3://local-tumiia-documents --recursive`.

2. Start the API:

   ```bash
   cd services/api
   dotnet tool restore
   dotnet run --project src/Tumiia.Api
   ```

   In Development the API applies database migrations on start-up. It listens on http://localhost:5194.

   - Health check: http://localhost:5194/api/v1/health
   - Swagger UI: http://localhost:5194/swagger
   - OpenAPI contract: http://localhost:5194/openapi/v1.json

3. Start the React app (once `apps/web` exists):

   ```bash
   cd apps/web
   npm install
   npm run dev
   ```

   It runs on http://localhost:5173. The API allows that address through CORS in Development only.

Stop the local services with `docker compose down` (add `-v` to delete the local database too).

## Signing in locally

The API has accounts from the start: register through Swagger UI (`POST /api/v1/auth/register`), then press
"Authorize" and paste the `accessToken`. Locally the JWT signing key is generated at start-up, so tokens stop
working when the API restarts. Emails are not sent: a password reset email is written to
`services/api/src/Tumiia.Api/.dev-emails/`. To make yourself an admin, set `Admin:Emails` (for example with
`dotnet user-secrets` or an environment variable `Admin__Emails=you@example.com`) and sign in again.

How the React app should handle tokens and the refresh cookie is in `docs/frontend-integration.md`.

## Run the tests

Docker must be running: the integration tests start their own PostgreSQL and LocalStack containers.

```bash
cd services/api
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:TestResults/report -reporttypes:"Html;TextSummary"
```

Open `TestResults/report/index.html` for the coverage report. The minimum is 70% line coverage.

## The API contract

Every build of `Tumiia.Api` writes the OpenAPI contract to `services/api/src/Tumiia.Api/openapi/v1.json`.
Commit it with any API change. The frontend generates its API client from this file.

## Database migrations

```bash
cd services/api
dotnet ef migrations add <DescriptiveName> --project src/Tumiia.Infrastructure --startup-project src/Tumiia.Infrastructure --output-dir Persistence/Migrations
```

Never edit a migration that has been merged to `main`.

## Infrastructure and deployment

AWS infrastructure is defined with the CDK in C# under `infra/`, all in af-south-1 (Cape Town).
Deploys are manual: **Actions → Deploy → Run workflow**, choosing `auto`, `frontend`, `backend`, `both` or
`infra`. The one-off setup (region, CDK bootstrap, GitHub environment) is in `docs/deploy-setup.md`.

Check the infrastructure rules locally (needs Node.js; no AWS account):

```bash
cd infra
dotnet test
```

Build the API image the way AWS runs it:

```bash
cd services/api
docker build --platform linux/arm64 -t tumiia-api .
```

## Continuous integration

Every pull request runs `.github/workflows/ci.yml`: backend build, tests and 70% coverage; frontend checks
(once `apps/web` exists); a check that `openapi/v1.json` is up to date; an ARM64 Docker build (once the
Dockerfile exists); and a check that `CHANGELOG.md` was updated. For a pure refactor with no behaviour
change, add the `no-changelog` label to the pull request. CI never deploys.

## Contributing

Read `docs/CONTRIBUTING.md` before your first change. Every change needs tests and a `CHANGELOG.md` entry.
