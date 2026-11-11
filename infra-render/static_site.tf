resource "render_static_site" "frontend" {
  name           = "${var.project_name}-frontend"
  repo_url       = var.github_repo_url
  branch         = var.github_branch
  root_directory = "frontend"
  build_command  = "npm ci && npm run build"
  # Relative to root_directory, not the repo root — Render scopes every path (build commands and
  # publish_path alike) to root_directory once it's set.
  publish_path = "dist"

  env_vars = {
    # Vite bakes env vars in at build time, not read at runtime — this is why it has to be a
    # Terraform reference to the API's own URL rather than something set later by hand.
    VITE_API_BASE_URL = { value = "${render_web_service.api.url}/api" }
  }

  # Client-side routing (React Router): any path that isn't a real built file should still
  # serve index.html and let the router take over, instead of a 404 from Render's static host.
  routes = [{
    source      = "/*"
    destination = "/index.html"
    type        = "rewrite"
  }]
}
