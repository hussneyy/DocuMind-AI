variable "project_name" {
  type    = string
  default = "documind-ai"
}

variable "render_region" {
  description = "One of frankfurt, ohio, oregon, singapore, virginia."
  type        = string
  default     = "oregon"
}

variable "github_repo_url" {
  description = "HTTPS URL of the GitHub repo Render builds from. Must already be connected to your Render account via its GitHub integration (dashboard > Account Settings > GitHub) — Terraform can declare what to build, but the one-time OAuth connection itself is a dashboard action, same category as generating the API key itself."
  type        = string
  default     = "https://github.com/hussneyy/DocuMind-AI"
}

variable "github_branch" {
  type    = string
  default = "main"
}

variable "openai_api_key" {
  description = "OpenAI API key. Never committed — pass via TF_VAR_openai_api_key or a gitignored terraform.tfvars."
  type        = string
  sensitive   = true
}

variable "cloudflare_account_id" {
  description = "Your Cloudflare account ID (dashboard right sidebar on any domain, or account home)."
  type        = string
}

variable "storage_bucket_name" {
  description = "R2 bucket name for uploaded PDFs."
  type        = string
  default     = "documind-ai-storage"
}

variable "r2_access_key_id" {
  description = "R2 API token's Access Key ID. R2 tokens with S3 API credentials aren't creatable via Terraform today — generate one manually at Cloudflare dashboard > R2 > Manage API tokens, scoped to just this bucket, and never commit it."
  type        = string
  sensitive   = true
}

variable "r2_secret_access_key" {
  type      = string
  sensitive = true
}

variable "cors_allowed_origin" {
  description = <<-EOT
    The frontend's real URL, e.g. "https://documind-ai-frontend.onrender.com" — needed by the
    API's CORS policy. This can't be computed automatically: the API needs the static site's URL,
    but Render only assigns that URL once the static site itself is created, and the static site
    separately needs the API's URL for its own build (see static_site.tf) — a genuine chicken-and-
    egg pair, not a Terraform limitation. Apply once with a placeholder, note the real
    frontend_url output, set this variable to that value, and apply again.
  EOT
  type        = string
  default     = "https://placeholder.onrender.com"
}
