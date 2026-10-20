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
- **Dimension-less pgvector column.** `DocumentChunks.Embedding` is mapped as `vector` (no `(n)`)
  rather than, say, `vector(1536)`. pgvector allows this, and it means switching the configured
  embedding model/provider — which can change the vector dimension — never requires an `ALTER
  COLUMN` migration. The cost: Postgres will error if a single similarity query ever compares
  vectors of two different dimensions, so the app's own responsibility is to keep every stored
  embedding on the currently configured model — `DocumentIndexer` always re-embeds with the
  current provider, and a document must be retried (not just left alone) after a model change for
  its embeddings to be comparable again. No ANN index (ivfflat/hnsw) is created yet, since an
  index needs either a fixed dimension or enough data to be worth the build cost — add one before
  this project's document count ever gets large.
- **Reciprocal Rank Fusion for hybrid search**, not a weighted score blend. A cosine similarity
  (roughly 0-1) and a Postgres `ts_rank` score (unbounded, corpus-dependent) aren't on comparable
  scales, so averaging them directly would be meaningless. RRF only uses each list's *rank
  position*, which is always comparable regardless of the underlying scoring method.
- **The RAG pipeline retrieves through `ISearchService.RetrieveAsync`, not a second retrieval
  implementation.** Early on it would have been easy to write a RAG-specific semantic+keyword+RRF
  query against `IDocumentChunkRepository` directly, since the LLM needs full chunk text rather
  than `SearchResultItem`'s UI-sized excerpt. Instead, `SearchService.SearchAsync` was refactored
  to call a new `RetrieveAsync` internally, and RAG calls the same method — so `/api/search` and
  the chat pipeline can never silently drift onto different ranking logic, thresholds, or
  ownership filters.
- **An LLM provider abstraction (`ILlmProvider`) mirroring `IEmbeddingProvider`**: `Mock` (default,
  deterministic, no API key) or `OpenAI` (real chat-completions call), selected the same way at DI
  registration time. The mock isn't a fixed string — it picks the most question-relevant sentence
  out of each retrieved passage and cites it, which is enough to exercise the full grounded-answer-
  with-citations pipeline without a paid key.
- **Citation markers are resolved by the generator, never trusted from the model.** The LLM is
  asked to cite with `[n]` markers against a *numbered list of context blocks the generator itself
  just built*; after the model responds, `RagAnswerGenerator` parses `[n]` markers out of the
  answer text and only keeps the ones that match a block number it actually provided. A
  hallucinated or out-of-range marker is dropped, not resolved to the nearest real chunk. This
  means a citation that reaches the frontend is always backed by a chunk this request actually
  retrieved for this user — the model controls the *wording*, never which sources get cited.
- **The LLM is never called when retrieval found nothing relevant.** Below `Search:MinSimilarity`,
  `RagAnswerGenerator` returns a fixed "not enough information" answer without a model call at
  all — the same grounding principle already applied at the search layer (Milestone 4), now
  applied one step earlier, before generation rather than after.
- **Summaries reuse `ILlmProvider` with a summarization-flavored `LlmRequest`, not a separate
  summarization provider interface.** A summary is mechanically "answer this instruction using
  this context" — the same shape as a chat question — so `SummaryService` builds an `LlmRequest`
  whose `Question` field is actually an instruction ("Write a concise 2-3 sentence summary...")
  and whose context is a sample of the document's own chunks. This avoids a parallel provider
  abstraction for what is, from the provider's point of view, the same operation.
- **Summary chunk selection differs by type, not just the instruction wording.** Detailed takes the
  first `Summary:DetailedMaxChunks` chunks in document order (bounds prompt size for very large
  documents — a real map-reduce summarization strategy for documents that exceed this is not yet
  implemented). Short and Key Points instead *sample* a small number of chunks evenly spaced across
  the whole document (`ordered[i * count/sampleSize]` for `i` in `0..sampleSize`), so a short
  summary still reflects the document's middle and end, not just its introduction — a naive
  "first N chunks" sample would systematically bias short summaries toward whatever topic the
  document happens to open with.
- **The source viewer serves the original PDF bytes directly** (`GET /api/documents/{id}/file`,
  streamed via the same `IFileStorage`/`IDocumentRepository` ownership path as every other document
  endpoint) **rather than reconstructing a page view from extracted text.** Reconstructing from
  `DocumentChunk` text would be lossy (chunking is sentence-based and can merge or split content
  across page boundaries) and wouldn't let a user visually verify a citation against the actual
  source. The frontend renders it in an `<iframe>` and jumps to a page via the browser's native PDF
  viewer's `#page=n` URL fragment, rather than shipping a PDF.js-based custom viewer — the simplest
  thing that actually works in Chromium-based browsers, at the cost of not controlling the viewer's
  own UI or working identically in every browser.

## Milestone roadmap

1. **Foundation** — solution structure, DB connection, health checks, Swagger,
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
