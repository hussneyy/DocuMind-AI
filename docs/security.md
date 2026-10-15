# Security and privacy

This file tracks security measures as they are actually implemented and verified — not the full
target state from the project spec. See the root [README.md](../README.md#security-and-privacy-considerations)
for a summary.

## Implemented (Milestones 1–3)

- **No committed secrets.** The database connection string in `appsettings.json` is a non-working
  placeholder. The real local connection string is stored via `dotnet user-secrets`. `.env.example`
  documents every configuration key with placeholder values only.
- **Password hashing.** ASP.NET Core Identity hashes passwords (PBKDF2 with a per-user salt);
  plain-text passwords are never stored or logged.
- **Cookie auth configuration.** `documind.auth` cookie is `HttpOnly` (unreachable from JS),
  `SameSite=Lax`, and `Secure` outside `Development` (`Program.cs`, `ConfigureApplicationCookie`).
  401/403 are returned as status codes, not redirects to a login *page* — this is an API, not a
  Razor app.
- **CSRF defense via custom header, not antiforgery tokens.** Every unsafe-method request
  (`POST`/`PUT`/`PATCH`/`DELETE`) must carry `X-Requested-With`
  ([CsrfProtectionMiddleware.cs](../src/DocuMind.Api/Middleware/CsrfProtectionMiddleware.cs)).
  **Why this instead of antiforgery tokens:** a classic HTML form submitted cross-site cannot add
  a custom header, and a cross-origin `fetch`/XHR that tries to triggers a CORS preflight our
  policy only allows for the configured frontend origin. **Trade-off:** this protects against
  *other* origins, not against the origins we already trust via CORS — if the CORS allow-list ever
  included an origin we don't fully trust, this defense wouldn't help. Verified by an integration
  test asserting a request without the header is rejected with 400.
- **Brute-force protection.** `POST /api/auth/login` and `/register` are rate-limited to 10
  requests/minute (fixed window) via `Microsoft.AspNetCore.RateLimiting`. Identity additionally
  locks an account for 5 minutes after 5 consecutive failed login attempts.
  **Known trade-off:** `LoginAsync` returns a specific "too many failed attempts" message when an
  account is locked, distinct from the generic "invalid email or password" for a wrong password.
  This is standard practice but does subtly confirm the account exists; revisit if this app ever
  needs to resist account-enumeration specifically.
- **Per-user ownership enforced in the database query itself**, not as a secondary check:
  `DocumentRepository.GetByIdAsync`/`ListAsync` filter by `OwnerUserId` as part of the EF Core
  query predicate. A request for another user's document ID returns `404 Not Found` — identical to
  a nonexistent ID — so the API never confirms whether a given ID exists for someone else. Verified
  by `Document_IsNotVisibleToADifferentUser` in `DocumentsApiTests.cs`, which registers two real
  users and asserts the second gets 404 on the first's document via get, list, *and* delete.
- **Upload validation, in order:** file name/extension and declared size (fast, no I/O) →
  byte-for-byte PDF magic-number signature check → an actual `PdfDocument.Open` parse via PdfPig
  (catches corrupt/malformed files and detects encrypted PDFs, which are rejected rather than
  silently accepted as "indexed"). The browser-supplied `Content-Type` is never trusted as the
  sole gate. Stored files use a server-generated GUID name in a private, non-web-accessible
  directory — the original filename is kept only as display metadata, never used to build a
  filesystem path, so there is no path-traversal surface from user input.
- **No internal detail in error responses.** `AppExceptionHandler` maps known exceptions
  (`NotFoundException`, `AppValidationException`) to clean 404/400 `ProblemDetails`; anything
  unexpected returns a generic 500 with no stack trace, and is logged server-side only
  (`AppExceptionHandler.cs`).
- **Restricted CORS**: only the origins listed in `Cors:AllowedOrigins`.
- **Background indexing has no new attack surface.** `IDocumentRepository.GetByIdForProcessingAsync`
  bypasses the owner filter (the background worker doesn't act on behalf of any particular user),
  but it is only ever called from `DocumentIndexer`, never from a controller — there is no HTTP
  route that lets a request reach it with an arbitrary ID. The new `retry` and `chunks` endpoints
  use the normal owner-filtered lookup like every other document endpoint.

## Implemented (Milestone 4)

- **Embedding provider credentials never committed.** `Embedding:ApiKey` follows the same rule as
  the database connection string — placeholder in `appsettings.json`, real value from environment
  variables or user secrets only. The Mock provider (the default) needs no credential at all.
- **Search stays within ownership boundaries.** `DocumentChunkRepository.SemanticSearchAsync`/
  `KeywordSearchAsync` join through `Documents` and filter by `OwnerUserId` in the same query that
  does the similarity/full-text matching — there's no code path where a chunk from another user's
  document is even fetched from the database, let alone returned. Verified by
  `Search_NeverReturnsAnotherUsersChunks` in `SearchApiTests.cs`.
- **No fabricated results.** Below `Search:MinSimilarity`, semantic search returns nothing rather
  than a low-confidence top match — verified by `Search_ForATotallyUnrelatedQuery_ReturnsNoResultsRatherThanAWeakMatch`.
  This is the same "say so instead of guessing" principle the project's grounding rules require
  for the RAG milestone, applied one step earlier at the retrieval stage.

## A real test-isolation bug: two defects, found while fixing a flaky test

While adding search, `DocumentWithNoExtractableText_FailsWithAnExplicitOcrMessage_NotSilentSuccess`
started intermittently failing with a generic error instead of the expected OCR message. Chasing
it down surfaced two real, separate bugs — not test flakiness to paper over:

1. **Cross-instance race.** Each integration test class used `IClassFixture<DocuMindApiFactory>`,
   so xUnit created a *separate* factory — separate host, separate `DocumentIndexingBackgroundService`,
   separate local storage directory — per test class, but all of them pointed at the *same* shared
   `documind_test` database. Two workers could both see the same `Queued` document and race to
   claim it; whichever worker's `LocalFileStorage` *didn't* actually hold that document's file
   would throw `FileNotFoundException`, caught by the generic handler. **Fix:** one shared
   `DocuMindApiFactory` across every test class via an xUnit `[CollectionDefinition]`/
   `ICollectionFixture` ([DocuMindApiCollection.cs](../src/DocuMind.IntegrationTests/DocuMindApiCollection.cs)) —
   one host, one worker, one storage directory, and (as a bonus) test classes in that collection
   now run sequentially relative to each other, which is what let the *next* bug surface clearly.

2. **Test configuration silently losing to real user secrets.** The sequential test run started
   hitting real `429 Too Many Requests` after a handful of register calls — expected, since the
   auth rate limiter is deliberately strict, but the test factory was supposed to raise that limit
   for its own run. Diagnosing it showed every configuration override the test factory set via
   `IWebHostBuilder.ConfigureAppConfiguration` was being silently outranked by `dotnet user-secrets`,
   because `WebApplicationFactory` defaults to the `Development` environment, and ASP.NET Core's
   `WebApplication.CreateBuilder` auto-loads user secrets whenever `IsDevelopment()` is true — with
   higher precedence than the test's own override. **The real consequence:** every integration
   test had actually been connecting to the **real local development database** (`documind`, via
   the developer's own `ConnectionStrings:Postgres` user secret) instead of the intended, isolated
   `documind_test` — for the entire duration of Milestones 2 and 3. 180 test-created user accounts
   (and their documents) had accumulated in the real dev database by the time this was caught.
   **Fix:** the test factory now calls `builder.UseEnvironment("Testing")`, which stops user
   secrets from auto-loading, and applies its overrides via process environment variables (read
   unconditionally and early by `CreateBuilder`, unlike the `ConfigureAppConfiguration` callback,
   which — empirically, via direct diagnostic logging — never actually took effect for this
   minimal-hosting `Program.cs` regardless of environment) — see
   [DocuMindApiFactory.cs](../src/DocuMind.IntegrationTests/DocuMindApiFactory.cs). The
   contaminating accounts were deleted from the real `documind` database after confirming every
   one matched a known test-data naming pattern.

**Why this is worth documenting rather than quietly fixing:** it's a reminder that a test suite
reporting green doesn't prove test *isolation* — these tests were passing throughout Milestones 2
and 3 while quietly pointed at the wrong database. The fix is now itself verified: a diagnostic
log line (since removed) confirmed the connection string read inside `Program.cs` matched
`documind_test` only after both changes landed together.

## Verified supply-chain check

Before adding a PDF library, the NuGet package literally named `UglyToad.PdfPig` — the name used
in that project's own GitHub README — was checked and found to be **not the real package**: owned
by an unrelated account (`grinay`, not the `UglyToad`/`EliotJones`/`BobLd` maintainers), no
license, and a fabricated `1.7.0-custom-5` version with no changelog. The real package, confirmed
via its NuGet listing (owners `EliotJones`, `PdfPig`, `BobLd`; Apache-2.0; linked to the genuine
`github.com/UglyToad/PdfPig`), is published under the plain id `PdfPig`. This project depends on
`PdfPig`, not `UglyToad.PdfPig`. Worth remembering: a package name matching a library's own docs is
not proof it's the right package — check the owner, license, and version history before adding any
new dependency, especially one that will parse untrusted user-uploaded files.

## Not yet implemented (planned)

Rate limiting on the upload endpoint itself, audit logging, HTTPS enforcement in a real deployed
environment, and prompt-injection mitigation for AI-generated answers all arrive in later
milestones. There is currently no mechanism to revoke an issued auth cookie server-side before its
natural expiry (standard limitation of stateless cookie auth without a token blacklist) — logout
depends on the client honoring the cookie-clearing `Set-Cookie` response, which every real browser
does but which a test or a misbehaving client could ignore.
