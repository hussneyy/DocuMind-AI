terraform {
  required_version = ">= 1.9.0"

  required_providers {
    render = {
      source  = "render-oss/render"
      version = "~> 1.9"
    }
    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Local state, deliberately — this deployment path exists specifically to avoid needing an
  # AWS account (the infra/ directory's S3 remote state backend isn't available here for that
  # reason). For a solo portfolio project, local state is a reasonable choice as long as you
  # don't lose terraform.tfstate; back it up somewhere (it contains sensitive values, so not a
  # public place) if that matters to you. Terraform Cloud's free tier is the usual upgrade path
  # if you want remote state without standing up an AWS account.
}
