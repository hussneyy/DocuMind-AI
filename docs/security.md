# Security and privacy

This file tracks security measures as they are actually implemented and verified — not the full
target state from the project spec. See the root [README.md](../README.md#security-and-privacy-considerations)
for a summary.

## Implemented (Milestone 1)

- **No committed secrets.** The database connection string in `appsettings.json` is a non-working
  placeholder (`CHANGE_ME` credentials). The real local connection string is stored via
  `dotnet user-secrets` (outside the repository). `.env.example` documents every configuration key
  with placeholder values only.
- **Restricted CORS.** The API only allows the origins listed in `Cors:AllowedOrigins`
  (`appsettings.json`), not a wildcard.
- **No internal detail in health checks.** `/health/live` and `/health/ready` report only
  aggregate status per check, not connection strings or stack traces.

## Not yet implemented (planned)

Authentication and password hashing, per-user document/conversation ownership enforcement, upload
validation (file signature, size limits, path traversal protection), rate limiting, HTTPS
enforcement in deployed environments, prompt-injection mitigation for AI-generated answers, and
audit logging all arrive in later milestones. Until Milestone 2 lands authentication, there is no
access control to document at all — the API currently exposes no endpoints that touch user data.
