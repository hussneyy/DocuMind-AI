provider "render" {
  # Reads RENDER_API_KEY and RENDER_OWNER_ID from the environment — never hardcode these.
  # Generate the key at https://dashboard.render.com/u/settings#api-keys; find the owner ID
  # (starts with usr- or tea-) in your account/team settings URL.
}

provider "cloudflare" {
  # Reads CLOUDFLARE_API_TOKEN from the environment.
}
