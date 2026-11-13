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
