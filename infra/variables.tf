variable "region" {
  description = "AWS region for every resource."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Short name used in resource names and tags."
  type        = string
  default     = "documind-ai"
}

variable "vpc_cidr" {
  type    = string
  default = "10.0.0.0/16"
}

variable "public_subnet_cidr" {
  description = "Subnet for the EC2 instance (needs a public IP, no ALB in front of it)."
  type        = string
  default     = "10.0.1.0/24"
}

variable "private_subnet_cidrs" {
  description = "Two subnets for RDS, in different AZs — a DB subnet group requires at least two."
  type        = list(string)
  default     = ["10.0.2.0/24", "10.0.3.0/24"]
}

variable "ec2_instance_type" {
  type    = string
  default = "t3.micro" # Free Tier eligible for 12 months on a new AWS account
}

variable "db_instance_class" {
  type    = string
  default = "db.t3.micro" # Free Tier eligible for 12 months on a new AWS account
}

variable "db_name" {
  type    = string
  default = "documind"
}

variable "db_username" {
  type    = string
  default = "documind"
}

variable "github_repo" {
  description = "GitHub \"owner/repo\" allowed to assume the deploy role via OIDC."
  type        = string
  default     = "hussneyy/DocuMind-AI"
}

variable "budget_limit_usd" {
  type    = number
  default = 5
}

variable "budget_alert_email" {
  description = "Where the AWS Budgets alarm sends its notification."
  type        = string
  default     = "Hussna.alee2512@gmail.com"
}

variable "openai_api_key" {
  description = "OpenAI API key, written to SSM Parameter Store as a SecureString. Never committed — supply via a .tfvars file that is gitignored, or TF_VAR_openai_api_key."
  type        = string
  sensitive   = true
}

variable "frontend_bucket_name" {
  description = "Globally-unique S3 bucket name for the built React app."
  type        = string
}

variable "storage_bucket_name" {
  description = "Globally-unique S3 bucket name for uploaded PDFs."
  type        = string
}
