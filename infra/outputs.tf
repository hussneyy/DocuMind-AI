output "frontend_url" {
  value = "https://${aws_cloudfront_distribution.frontend.domain_name}"
}

output "api_url" {
  value = "http://${aws_eip.api.public_ip}"
}

output "ecr_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "ec2_instance_id" {
  description = "Set this as the EC2_INSTANCE_ID repository variable in GitHub so deploy.yml can target it via SSM."
  value       = aws_instance.api.id
}

output "github_actions_plan_role_arn" {
  description = "Set this as the AWS_PLAN_ROLE_ARN repository secret."
  value       = aws_iam_role.github_actions_plan.arn
}

output "github_actions_deploy_role_arn" {
  description = "Set this as the AWS_DEPLOY_ROLE_ARN repository secret."
  value       = aws_iam_role.github_actions_deploy.arn
}

output "db_endpoint" {
  value = aws_db_instance.main.address
}
