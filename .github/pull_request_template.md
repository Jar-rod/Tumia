## What and why

<!-- One or two sentences: what this changes and why. Link the issue if there is one. -->

## Checklist

See `docs/CONTRIBUTING.md`. Items marked (CI) are checked automatically.

### Tests
- [ ] All tests pass (CI)
- [ ] Backend and frontend line coverage is at least 70% (CI)
- [ ] New or changed behaviour has tests; test names read as `Method_WhenCondition_ExpectedResult`
- [ ] Code that touches the database has an integration test (Testcontainers, real PostgreSQL)

### Code
- [ ] Simple enough for a junior developer: short methods, clear names, one class per file
- [ ] Every public method has a plain-English XML doc comment (C#) or JSDoc (TypeScript)
- [ ] `dotnet format` and ESLint + Prettier are clean, no warnings (CI)

### API
- [ ] Every endpoint is in Swagger with a summary and all its response codes (CI)
- [ ] `services/api/src/Tumiia.Api/openapi/v1.json` is rebuilt and committed (CI)
- [ ] Requests and responses are DTO records; errors are ProblemDetails

### Security
- [ ] New endpoints require a signed-in user, or the reason they are anonymous is given above
- [ ] Project and document endpoints check the caller has access to that project
- [ ] Nothing logs passwords, tokens, reset links, file contents or extracted field values
- [ ] No secrets in the repo or in `appsettings*.json`

### Dependencies and data
- [ ] New dependencies are MIT, Apache-2.0, BSD or PostgreSQL licensed and listed in `THIRD-PARTY-NOTICES.md`
- [ ] Database changes are a new, descriptively named migration; no merged migration was edited
- [ ] Original files and the original AI prediction are never overwritten or deleted
- [ ] Only synthetic test documents; no real client files

### Changelog
- [ ] `CHANGELOG.md` has an entry under `[Unreleased]`, or this PR has the `no-changelog` label (CI)
