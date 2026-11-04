# TUMIIA

Intelligent filing for construction project documents. A user drops messy project files into a project
vault; TUMIIA works out what each file is, proposes where it belongs, and the user confirms or corrects it.

| Folder | What it is |
|---|---|
| `services/api` | ASP.NET Core API (.NET 10): controllers, use cases, domain rules, database, tests |
| `apps/web` | React app (Vite). Built separately by the frontend developer |
| `infra` | AWS CDK infrastructure (later phase) |
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

   LocalStack creates the `local-tumiia-documents` bucket and the `local-document-analysis` queue (with its
   dead-letter queue) on start-up.

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

## Run the tests

Docker must be running: the integration tests start their own PostgreSQL container.

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

## Continuous integration

Every pull request runs `.github/workflows/ci.yml`: backend build, tests and 70% coverage; frontend checks
(once `apps/web` exists); a check that `openapi/v1.json` is up to date; an ARM64 Docker build (once the
Dockerfile exists); and a check that `CHANGELOG.md` was updated. For a pure refactor with no behaviour
change, add the `no-changelog` label to the pull request. CI never deploys.

## Contributing

Read `docs/CONTRIBUTING.md` before your first change. Every change needs tests and a `CHANGELOG.md` entry.
