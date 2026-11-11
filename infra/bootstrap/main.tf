# One-time setup for Terraform's own remote state.
#
# This is deliberately NOT part of the main `infra/` configuration: Terraform can't store its
# state in an S3 bucket that doesn't exist yet, so this tiny config uses plain local state
# (a bootstrap.tfstate file on your machine) and you apply it exactly once, by hand, before
# touching anything else. After this runs, the main config's `backend "s3"` block points at
# the bucket this creates.
#
# Run from this directory:
#   terraform init
#   terraform apply
# Then note the bucket_name and dynamodb_table_name outputs — you'll put them into
# infra/main.tf's backend block.

terraform {
  required_version = ">= 1.9.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Project   = "documind-ai"
      ManagedBy = "terraform"
      Component = "bootstrap"
    }
  }
}

variable "region" {
  description = "AWS region for the state bucket and lock table."
  type        = string
  default     = "us-east-1"
}

variable "state_bucket_name" {
  description = "Globally-unique S3 bucket name for Terraform remote state."
  type        = string
}

# S3 bucket names are global across all AWS accounts, so there is no sensible default — you
# must pick one (e.g. "documind-ai-tfstate-<your-initials>-<random>") and pass it explicitly.

resource "aws_s3_bucket" "tf_state" {
  bucket = var.state_bucket_name

  # Prevents `terraform destroy` on the MAIN config from ever touching this bucket (it's a
  # different state file entirely), and stops accidental deletion of the bucket that holds
  # every other resource's state.
  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_s3_bucket_versioning" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "tf_state" {
  bucket                  = aws_s3_bucket.tf_state.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Guards against two `terraform apply` runs (e.g. you locally + a GitHub Actions run) racing
# on the same state file at once.
resource "aws_dynamodb_table" "tf_lock" {
  name         = "documind-ai-tfstate-lock"
  billing_mode = "PAY_PER_REQUEST" # free tier covers this table's usage either way
  hash_key     = "LockID"

  attribute {
    name = "LockID"
    type = "S"
  }
}

output "bucket_name" {
  value = aws_s3_bucket.tf_state.bucket
}

output "dynamodb_table_name" {
  value = aws_dynamodb_table.tf_lock.name
}
