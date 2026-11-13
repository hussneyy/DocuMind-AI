resource "render_web_service" "api" {
  name   = "${var.project_name}-api"
  plan   = "free" # Render's own docs list only paid plans here, but their community support confirms "free" is valid for web services too — if apply rejects it, check the dashboard's "New Web Service" flow for the exact free-tier plan identifier and swap it in; it's a one-line fix.
  region = var.render_region

  runtime_source = {
    docker = {
      repo_url        = var.github_repo_url
      branch          = var.github_branch
      dockerfile_path = "src/DocuMind.Api/Dockerfile"
      context         = "."
    }
  }

  health_check_path = "/health/live"

  env_vars = {
    ASPNETCORE_ENVIRONMENT      = { value = "Production" }
    Database__MigrateOnStartup  = { value = "true" }
    ConnectionStrings__Postgres = { value = render_postgres.main.connection_info.internal_connection_string }

    # Storage:Provider is left unset deliberately — "Local" is the app's default, and it's the
    # right choice here: Cloudflare R2 (and every other S3-compatible provider checked) turned
    # out to need a card too, so this avoids a second account entirely. The real trade-off: free
    # Render web services have an ephemeral filesystem, so uploaded PDFs are lost whenever the
    # service restarts (after ~15 min idle). Fine for a live demo you drive yourself; not meant
    # to hold onto files long-term. See the README for this called out explicitly.

    # Mock by default — needs no OpenAI account, no key, no per-use cost, and the full RAG
    # pipeline (upload, chunking, search, chat, citations) works end-to-end against it, just with
    # synthetic embeddings/answers instead of real ones. Set openai_api_key in terraform.tfvars
    # and change both Provider values below to "OpenAI" if you want the real thing later.
    Embedding__Provider = { value = "Mock" }
    Llm__Provider       = { value = "Mock" }

    Cors__AllowedOrigins__0 = { value = var.cors_allowed_origin }
  }
}
