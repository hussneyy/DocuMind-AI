# API documentation

The authoritative, always-current API reference is the generated OpenAPI document, served by the
running backend:

- Swagger UI: `http://localhost:5180/swagger`
- Raw OpenAPI JSON: `http://localhost:5180/swagger/v1/swagger.json`

This file tracks only what's actually implemented, to avoid drifting out of sync with the real API
surface.

## Implemented endpoints (Milestone 1)

| Method | Path | Purpose |
|---|---|---|
| GET | `/health/live` | Liveness probe — process is running. No dependency checks. |
| GET | `/health/ready` | Readiness probe — verifies a real PostgreSQL connection. |

## Planned endpoints

See the "Required endpoints" list in the project spec (authentication, documents, search, chat,
summaries, feedback). They will be added here, with example requests/responses, as each is
actually implemented — not before.
