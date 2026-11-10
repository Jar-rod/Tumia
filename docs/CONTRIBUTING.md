# Contributing to TUMIIA

These are the standards for every change to this repo. CI enforces the ones marked **(CI)**.

## Repo layout

```
.github/workflows/   ci.yml (every PR) · deploy.yml (manual only)
apps/web/            React + TypeScript (Vite)
services/api/        ASP.NET Core (.NET 10), Dockerfile, tests
infra/               AWS CDK (C#)
docs/adr/            one short file per architecture decision
CHANGELOG.md
```

Backend projects and what goes in each:

| Project | Holds | May reference |
|---|---|---|
| `Tumiia.Api` | Controllers, Swagger, versioning, rate limiting, auth setup, `Program.cs` | Application, Infrastructure |
| `Tumiia.Application` | Use cases: register, upload, analyse, confirm, correct, export | Domain |
| `Tumiia.Domain` | Entities, value objects, confidence and classification rules, interfaces (`IOcrEngine`, `IHostedDocumentAnalyser`, `IFileStore`, …) | nothing |
| `Tumiia.Infrastructure` | EF Core, S3, SQS, Bedrock (Claude), SES | Application, Domain |

Rule of thumb: if it decides something, it belongs in Domain. If it talks to the outside world, it belongs in Infrastructure.

## Tests

- **(CI)** All tests pass on every PR.
- **(CI)** Line coverage ≥ **70%** for the backend and for the frontend. Generated code, `Program.cs`, EF migrations and DTO-only files are excluded.
- Unit tests: xUnit, Shouldly, NSubstitute. Integration tests: Testcontainers with real PostgreSQL.
- Frontend: Vitest + Testing Library.
- Test names describe behaviour: `Method_WhenCondition_ExpectedResult`.

## Code style

- Write for a junior developer. Short methods, clear names, no clever tricks.
- Every public method has a plain-English XML doc comment (C#) or JSDoc (TypeScript) that says what it does and why.
- One class per file. Async methods end in `Async` and take a `CancellationToken`.
- Nullable reference types on. Warnings are errors **(CI)**.
- `dotnet format` and ESLint + Prettier must be clean **(CI)**.

## APIs

- Route pattern: `/api/v{version}/...`, with `Asp.Versioning`. Start at `v1`.
- **(CI)** Every endpoint appears in the OpenAPI (Swagger) document with a summary and all response codes.
- Request/response types are DTO records. EF entities never leave the API.
- Errors use RFC 7807 `ProblemDetails`.
- The API contract is committed at `services/api/src/Tumiia.Api/openapi/v1.json`. **(CI)** fails if it is out of date. Generate any frontend client from it; never hand-edit a generated client.

## Security

- Every endpoint requires a signed-in user unless it is explicitly anonymous (register, login, refresh, forgot/reset password, health).
- Every project or document endpoint checks the caller has access to that project.
- Never log passwords, tokens, reset links, file contents or extracted field values.
- Secrets live in AWS SSM Parameter Store, never in the repo or in `appsettings*.json`.

## Dependencies

Free for commercial use only: MIT, Apache-2.0, BSD, or the PostgreSQL licence. Record each new one in `THIRD-PARTY-NOTICES.md`.

Do not add these (they moved to commercial licences or add complexity we do not need): FluentAssertions v8+, Moq, MediatR, AutoMapper, MassTransit v9+, SixLabors ImageSharp, any paid DWG library.

## CHANGELOG

**(CI)** A PR that changes anything under `apps/`, `services/`, `infra/` or `.github/` must also change `CHANGELOG.md`. Add the label `no-changelog` to skip this for pure refactors.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). New entries go under `## [Unreleased]`, grouped by `Added`, `Changed`, `Fixed`, `Removed`, `Security`.

## Commits and PRs

- Branch from `main`: `feature/…`, `fix/…`, `chore/…`.
- Small PRs. One reviewer approval before merge.
- Commit messages: short imperative summary (`Add field-level confidence to analysis`).

## Product rules

- AI only suggests. A person confirms. Payment, contract and legal documents always need explicit confirmation.
- Never overwrite or delete an original file or the original AI prediction.
- UI text is plain language with simple yes/no choices.
- No real money movement anywhere in the product.
- Use synthetic test documents only. Never commit real client files.
