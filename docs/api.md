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

`processingStatus` is an integer: `0 Uploaded, 1 Queued, 2 Processing, 3 Indexed, 4 Failed`.
Indexing (anything beyond `Uploaded`) doesn't exist yet — that's Milestone 3.

## Planned endpoints

Summaries, search, chat, conversations, and feedback endpoints from the project spec will be
added here as they are actually implemented — not before.
