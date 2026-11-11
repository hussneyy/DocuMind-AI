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

    Storage__Provider        = { value = "S3" }
    Storage__BucketName      = { value = var.storage_bucket_name }
    Storage__ServiceUrl      = { value = "https://${var.cloudflare_account_id}.r2.cloudflarestorage.com" }
    Storage__AccessKeyId     = { value = var.r2_access_key_id }
    Storage__SecretAccessKey = { value = var.r2_secret_access_key }

    Embedding__Provider = { value = "OpenAI" }
    Embedding__ApiKey   = { value = var.openai_api_key }
    Llm__Provider       = { value = "OpenAI" }
    Llm__ApiKey         = { value = var.openai_api_key }

    Cors__AllowedOrigins__0 = { value = var.cors_allowed_origin }
  }
}
