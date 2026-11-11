# pgvector is supported on Render Postgres 13+ via the same `CREATE EXTENSION vector` the app's
# existing EF Core migration already runs — same as the local pgvector/pgvector Docker image and
# the AWS RDS deployment, no extra step needed here either.
resource "render_postgres" "main" {
  name          = "${var.project_name}-db"
  plan          = "free" # Confirmed valid for Postgres in Render's own docs. 1GB storage, 256MB RAM, expires after 30 days — see the README for the renewal process this implies.
  region        = var.render_region
  version       = "17"
  database_name = "documind"
  database_user = "documind"
}
