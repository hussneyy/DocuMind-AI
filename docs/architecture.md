# Architecture

## Layered solution structure

```
DocuMind/
  DocuMind.sln
  src/
    DocuMind.Api/              ASP.NET Core Web API host: controllers, DI wiring, Swagger, health checks
    DocuMind.Application/      Use cases, DTOs, provider/repository interfaces (no external dependencies)
    DocuMind.Domain/           Entities and domain rules (no dependencies on other layers)
    DocuMind.Infrastructure/   EF Core + Npgsql + pgvector, AI providers, PDF processing, file storage
    DocuMind.Tests/            Unit tests
    DocuMind.IntegrationTests/ Integration tests against DocuMind.Api
  frontend/                    React + TypeScript + Vite application
  docs/                        This documentation
  docker-compose.yml           Local PostgreSQL + pgvector (API/frontend containers added in Milestone 8)
```

Dependency direction is strictly inward: `Api → Application → Domain`, and
`Infrastructure → Application + Domain`. Domain has no dependencies at all. This keeps business
rules testable without a database or AI provider, and keeps controllers free of business logic.

## Why these choices

- **ASP.NET Core Web API + EF Core + PostgreSQL/pgvector**: a single relational store holds both
  ownership/metadata and vector embeddings, avoiding a second specialized vector database for a
  project at this scale.
- **Swashbuckle over the built-in `Microsoft.AspNetCore.OpenApi` template default**: the template
  only generates the OpenAPI document; Swashbuckle adds the interactive Swagger UI the spec
  requires.
- **Serilog**: structured logging with a clear place to add correlation IDs for background job
  failures in later milestones, without logging document contents or secrets.
- **Mock AI/embedding provider mode**: lets the app run, and lets automated tests run, without a
  paid API key — required explicitly by the project spec.
- **Database-backed background queue over a message broker**: `DocumentIndexingBackgroundService`
  polls for `Queued` documents and indexes them with bounded concurrency, using PostgreSQL itself
  as the queue (no RabbitMQ/Azure Service Bus/etc.). This is a single-instance design — there is
  no cross-instance claim locking, so running more than one API instance would let two instances
  pick up the same document. Justified by the project's explicit scale guidance ("don't add a
  complicated distributed queue unless justified"); if this app ever needed multiple instances,
  the fix is a `SELECT ... FOR UPDATE SKIP LOCKED`-style atomic claim, not a new broker.

## Milestone roadmap

1. **Foundation** (this milestone) — solution structure, DB connection, health checks, Swagger,
   React scaffold, landing page, Docker Compose for Postgres, Git.
2. **Authentication & document library** — registration/login, document upload, validation,
   listing, deletion.
3. **PDF extraction & indexing** — page-level text extraction, chunking, background processing,
   retry support.
4. **Semantic search** — embeddings, pgvector similarity search, relevance thresholds, hybrid
   search, search UI.
5. **RAG question answering** — grounded answer generation, citation resolution/validation, chat
   UI, conversation persistence.
6. **Summaries & source viewer** — short/detailed/section summaries, page-level source navigation.
7. **Security & quality hardening** — ownership checks, automated test coverage, accessibility.
8. **Deployment & portfolio readiness** — full Docker Compose stack, CI, final README, demo data.

Each milestone's actual, verified completion state is tracked in the root
[README.md](../README.md#project-status) rather than here, so there's one source of truth for
"what's really done."
