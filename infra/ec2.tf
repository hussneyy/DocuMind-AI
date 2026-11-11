data "aws_caller_identity" "current" {}

resource "aws_iam_role" "ec2" {
  name = "${var.project_name}-ec2-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })
}

# Lets GitHub Actions reach the instance via SSM Run Command instead of SSH — no key pair,
# no open port 22.
resource "aws_iam_role_policy_attachment" "ec2_ssm" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_role_policy" "ec2_app_access" {
  name = "${var.project_name}-ec2-app-access"
  role = aws_iam_role.ec2.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "ReadAppSecrets"
        Effect = "Allow"
        Action = ["ssm:GetParameter", "ssm:GetParameters"]
        Resource = [
          aws_ssm_parameter.db_password.arn,
          aws_ssm_parameter.openai_api_key.arn,
        ]
      },
      {
        Sid    = "PullFromEcr"
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken",
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:BatchGetImage",
        ]
        Resource = "*" # GetAuthorizationToken is only ever account/region-scoped by AWS itself, not by resource ARN
      },
      {
        Sid      = "ReadWritePdfBucket"
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
        Resource = "${aws_s3_bucket.storage.arn}/*"
      },
    ]
  })
}

resource "aws_iam_instance_profile" "ec2" {
  name = "${var.project_name}-ec2-profile"
  role = aws_iam_role.ec2.name
}

resource "aws_instance" "api" {
  ami                    = data.aws_ami.amazon_linux_2023.id
  instance_type          = var.ec2_instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.ec2.id]
  iam_instance_profile   = aws_iam_instance_profile.ec2.name

  user_data = templatefile("${path.module}/user_data.sh.tpl", {
    region          = var.region
    ecr_repo_url    = aws_ecr_repository.api.repository_url
    rds_endpoint    = aws_db_instance.main.address
    db_name         = var.db_name
    db_username     = var.db_username
    storage_bucket  = aws_s3_bucket.storage.bucket
    cors_origin     = "https://${aws_cloudfront_distribution.frontend.domain_name}"
    ssm_path_prefix = "/${var.project_name}"
  })

  # Changing user_data alone doesn't re-run it on an existing instance; redeploys instead go
  # through SSM Run Command (see .github/workflows/deploy.yml), which re-invokes the same
  # /opt/documind/deploy.sh this script installs. This tags the instance for replacement only
  # when something that genuinely requires a fresh boot changes.
  tags = { Name = "${var.project_name}-api" }
}

resource "aws_eip" "api" {
  instance = aws_instance.api.id
  domain   = "vpc"

  tags = { Name = "${var.project_name}-api-eip" }
}
