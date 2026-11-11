# AWS Systems Manager Parameter Store, standard tier — free, unlike AWS Secrets Manager
# ($0.40/secret/month). The EC2 instance's own IAM role (ec2.tf) can read these by name; no
# access keys are ever involved. The values never appear in Terraform plan output in plain
# text (they're marked sensitive) and are not written to the state file in plain text either
# once a SecureString type is used — Terraform still stores the value in state (this is a
# known Terraform limitation for this resource type), so treat the S3 state bucket itself as
# sensitive and keep it private, which bootstrap/main.tf already does.

resource "aws_ssm_parameter" "db_password" {
  name  = "/${var.project_name}/db_password"
  type  = "SecureString"
  value = random_password.db_password.result
}

resource "aws_ssm_parameter" "openai_api_key" {
  name  = "/${var.project_name}/openai_api_key"
  type  = "SecureString"
  value = var.openai_api_key
}
