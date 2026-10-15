# API documentation

The authoritative, always-current API reference is the generated OpenAPI document, served by the
running backend:

- Swagger UI: `http://localhost:5180/swagger`
- Raw OpenAPI JSON: `http://localhost:5180/swagger/v1/swagger.json`

This file tracks only what's actually implemented, to avoid drifting out of sync with the real API
surface.

## Implemented endpoints

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/health/live` | none | Liveness probe — process is running. |
| GET | `/health/ready` | none | Readiness probe — verifies a real PostgreSQL connection. |
| POST | `/api/auth/register` | none | Create an account, sign in, return the profile. Rate-limited. |
| POST | `/api/auth/login` | none | Validate credentials, sign in, return the profile. Rate-limited, lockout after 5 failed attempts. |
| POST | `/api/auth/logout` | cookie | End the session. |
| GET | `/api/auth/me` | cookie | Return the authenticated user's profile. |
| POST | `/api/documents` | cookie | Upload a PDF (`multipart/form-data`, field name `file`). Validates signature, size, page count; rejects encrypted/corrupt PDFs. |
| GET | `/api/documents` | cookie | Paginated list, scoped to the caller. Query params: `page`, `pageSize`, `search`, `status`, `sortBy` (`uploadedAt`\|`name`), `sortDesc`. |
| GET | `/api/documents/{id}` | cookie | Full document metadata. 404 if missing or not owned by the caller. |
| GET | `/api/documents/{id}/status` | cookie | Just the processing status/error. |
| DELETE | `/api/documents/{id}` | cookie | Deletes the document and its stored file. 404 if not owned. |
| POST | `/api/documents/{id}/retry` | cookie | Re-queues a `Failed` document for indexing. 400 if it isn't currently `Failed`. |
| GET | `/api/documents/{id}/chunks` | cookie | The document's extracted chunks (`id`, `chunkIndex`, `textContent`, `pageStart`, `pageEnd`), ordered by index. 404 if not owned. |
| POST | `/api/search` | cookie | Semantic/keyword/hybrid search over the caller's indexed chunks. Body: `query` (required), `mode` (`Semantic`\|`Keyword`\|`Hybrid`, default `Hybrid`), `documentIds` (optional scope), `topK` (optional, 1-50). |

Every `/api/documents/*` request requires the `X-Requested-With` header (see
[security.md](security.md)) in addition to the auth cookie.

### Example: register then upload

```http
POST /api/auth/register
Content-Type: application/json
X-Requested-With: DocuMindClient

{ "email": "ada@example.com", "password": "correct horse battery staple1", "displayName": "Ada Lovelace" }
```

```http
POST /api/documents
Cookie: documind.auth=...
X-Requested-With: DocuMindClient
Content-Type: multipart/form-data; boundary=...

--...
Content-Disposition: form-data; name="file"; filename="handbook.pdf"
Content-Type: application/pdf

<binary PDF bytes>
--...--
```

Response (`201 Created`):

```json
{
  "id": "f13275e3-3d7e-4a7d-8ca6-3192cfd91d0b",
  "originalFileName": "handbook.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 345,
  "pageCount": 1,
  "processingStatus": 0,
  "processingError": null,
  "uploadedAt": "2026-10-14T15:56:51.99Z",
  "indexedAt": null
}
```

`processingStatus` is an integer: `0 Uploaded, 1 Queued, 2 Processing, 3 Indexed, 4 Failed`. In
practice a document is set to `Queued` immediately on upload — `Uploaded` is never actually
observed — and a background worker moves it through `Processing` to `Indexed` or `Failed` within
a few seconds (poll interval is configurable, default 2s). A PDF with no extractable text (e.g.
scanned/image-only) ends in `Failed` with an explicit `processingError` saying OCR isn't
implemented, rather than a silent empty "success".

### Example: chunks response

```json
[
  { "id": "...", "chunkIndex": 0, "textContent": "The exam registration deadline is March 15th.", "pageStart": 2, "pageEnd": 2 },
  { "id": "...", "chunkIndex": 1, "textContent": "...", "pageStart": 2, "pageEnd": 3 }
]
```

A chunk's `pageStart`/`pageEnd` differ when it spans a page boundary — citations should reference
the full range, not just one page.

### Example: search

```http
POST /api/search
Cookie: documind.auth=...
X-Requested-With: DocuMindClient
Content-Type: application/json

{ "query": "exam registration deadline", "mode": "Hybrid" }
```

```json
[
  {
    "chunkId": "f52e1b51-7403-4051-b361-48f96c171c69",
    "documentId": "b2cd48df-e8ff-4565-8bfa-5014cc558041",
    "documentFileName": "handbook.pdf",
    "excerpt": "...The exam registration deadline is March fifteenth...",
    "pageStart": 1,
    "pageEnd": 3,
    "score": 0.0328
  }
]
```

An empty array means no sufficiently relevant passage was found (below `Search:MinSimilarity`
for semantic mode, or no full-text match for keyword mode) — never a fabricated low-confidence
result. `score` is only comparable within a single response for `Semantic` or `Keyword` mode;
`Hybrid` mode's score is an RRF-combined rank value, not a similarity.

## Planned endpoints

Chat, summaries, conversations, and feedback endpoints from the project spec will be added here
as they are actually implemented — not before.
