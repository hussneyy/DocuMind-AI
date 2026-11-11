# The bucket itself is managed here. The S3-compatible API credentials the app actually
# authenticates with (var.r2_access_key_id / var.r2_secret_access_key) are NOT Terraform-managed:
# Cloudflare's R2 "API tokens with S3 credentials" aren't exposed as a Terraform resource today,
# so generate one by hand at Cloudflare dashboard > R2 > Manage API tokens, scoped to just this
# bucket (Object Read & Write), and pass it in via a gitignored terraform.tfvars or
# TF_VAR_r2_access_key_id / TF_VAR_r2_secret_access_key. This is the one real "created by hand"
# step in this whole deployment, and it's called out here rather than left implicit.
resource "cloudflare_r2_bucket" "storage" {
  account_id = var.cloudflare_account_id
  name       = var.storage_bucket_name
  location   = "wnam" # Western North America — closest to Render's "oregon" region; change alongside var.render_region if you pick a different one
}
