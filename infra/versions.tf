terraform {
  required_version = ">= 1.9.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Remote state: created once by infra/bootstrap/ (see its README). Replace the two CHANGE_ME
  # values with the outputs from `terraform apply` in that directory, or pass them at init time
  # with `terraform init -backend-config="bucket=..." -backend-config="dynamodb_table=..."`.
  backend "s3" {
    bucket         = "CHANGE_ME-documind-ai-tfstate"
    key            = "documind-ai/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "documind-ai-tfstate-lock"
    encrypt        = true
  }
}
