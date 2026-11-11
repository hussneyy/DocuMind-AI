output "frontend_url" {
  value = render_static_site.frontend.url
}

output "api_url" {
  value = render_web_service.api.url
}

output "r2_bucket_name" {
  value = cloudflare_r2_bucket.storage.name
}

output "postgres_expiry_note" {
  value = "Free Render Postgres instances expire 30 days after creation (14-day grace period before data deletion) — see the README's AWS/Render section for the renewal process."
}
