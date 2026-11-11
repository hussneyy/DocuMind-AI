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
| POST | `/api/chat/ask` | cookie | Ask a grounded question. Body: `question` (required, 1-2000 chars), `conversationId` (optional — omit to start a new conversation), `documentIds` (optional scope). Returns the new user + assistant messages, the latter with resolved citations. |
| GET | `/api/chat/conversations` | cookie | The caller's own conversations, newest first. |
| GET | `/api/chat/conversations/{id}` | cookie | Full message history for one conversation, each assistant message with its citations. 404 if missing or not owned by the caller. |
| DELETE | `/api/chat/conversations/{id}` | cookie | Deletes a conversation and all its messages/citations. 404 if not owned. |
| GET | `/api/documents/{id}/file` | cookie | Streams the original uploaded PDF bytes (not a reconstruction), for inline viewing. Supports range requests. 404 if not owned. |
| GET | `/api/documents/{id}/summary` | cookie | Returns a cached summary, generating one first if none exists yet. Query params: `type` (`Short`\|`Detailed`\|`KeyPoints`, default `Short`), `regenerate` (bool, default `false`, forces a fresh generation even if cached). 400 if the document hasn't finished indexing. |

Every unsafe-method (`POST`/`PUT`/`PATCH`/`DELETE`) request to `/api/documents/*` and `/api/chat/*`
requires the `X-Requested-With` header (see [security.md](security.md)) in addition to the auth
cookie — `GET` requests only need the cookie, since the CSRF middleware only guards state-changing
methods.

### Example: register then upload

```http
POST /api/auth/register
Content-Type: application/json
X-Requested-With: DocuMindClient

{ "email": "ada@example.com", "password": "correct horse battery staple1", "firstName": "Ada", "lastName": "Lovelace", "country": "GB", "phoneNumber": "+44 7911123456", "preferredLanguage": "en" }
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

### Example: ask a grounded question

```http
POST /api/chat/ask
Cookie: documind.auth=...
X-Requested-With: DocuMindClient
Content-Type: application/json

{ "question": "When is the exam registration deadline?" }
```

```json
{
  "conversationId": "9f5a1e3a-6b8e-4b63-9a2a-9c6a9a2f6e11",
  "title": "When is the exam registration deadline?",
  "userMessage": {
    "id": "...", "role": "user", "content": "When is the exam registration deadline?",
    "createdAt": "2026-10-16T12:00:00Z", "citations": []
  },
  "assistantMessage": {
    "id": "...", "role": "assistant",
    "content": "Based on your documents: The exam registration deadline is March fifteenth this year [1].",
    "createdAt": "2026-10-16T12:00:00.001Z",
    "citations": [
      {
        "chunkId": "...", "documentId": "...", "documentFileName": "handbook.pdf",
        "pageStart": 2, "pageEnd": 2, "excerpt": "The exam registration deadline is March fifteenth this year."
      }
    ]
  }
}
```

Pass the returned `conversationId` back on the next call to continue the same conversation — prior
messages are replayed to the model so follow-up questions ("and if I miss it?") resolve correctly.
If nothing relevant was found in the caller's documents, `assistantMessage.content` explicitly
says so and `citations` is empty — the language model is never even called in that case, so it
cannot fabricate an answer. Every `[n]` marker in `content` corresponds to one entry in
`citations`, in order; a marker the model might emit for a passage that was never actually
retrieved is dropped before the response is built, never resolved to the wrong source.

### Example: document summary

```http
GET /api/documents/f13275e3-3d7e-4a7d-8ca6-3192cfd91d0b/summary?type=Short
Cookie: documind.auth=...
```

```json
{
  "summaryType": "Short",
  "content": "Based on your documents: The exam registration deadline is March fifteenth this year [1]...",
  "sourceChunkCount": 6,
  "generatedByModel": "mock-grounded-v1",
  "generatedAt": "2026-10-20T12:00:00Z"
}
```

The result is cached per `(documentId, type)` — calling this again without `regenerate=true`
returns the same `generatedAt`/`content` without a new model call. `type=Detailed` summarizes up
to `Summary:DetailedMaxChunks` chunks in document order; `type=Short`/`type=KeyPoints` instead
sample a handful of chunks evenly spaced across the whole document, so a short summary isn't
biased toward just the introduction. 400 if the document's `processingStatus` isn't `Indexed` yet.

### Example: source file

```http
GET /api/documents/f13275e3-3d7e-4a7d-8ca6-3192cfd91d0b/file
Cookie: documind.auth=...
```

Returns the original uploaded PDF bytes with `Content-Type: application/pdf` — not a
reconstruction from extracted text. The frontend loads this directly in an `<iframe>` and appends
`#page=n` to jump to a specific page via the browser's native PDF viewer.

## Planned endpoints

No new endpoints are planned for Milestones 7-8 (security/quality hardening and deployment
readiness) — remaining gaps are feature-internal limitations, not missing routes. See each
milestone's "Known limitations" in the root [README.md](../README.md), notably that scanned/
image-only PDFs have no OCR path at all, not even a partial one.
