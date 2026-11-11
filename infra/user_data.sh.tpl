#!/bin/bash
# Rendered by Terraform's templatefile() in ec2.tf — the dollar-brace placeholders below are
# substituted at plan/apply time with real resource attributes (ECR URL, RDS endpoint, etc).
# Secrets are deliberately NOT baked in here: they're fetched live from SSM Parameter Store
# each time this script runs, so rotating a secret there takes effect on the next redeploy
# without touching this file.
set -euo pipefail

dnf install -y docker
systemctl enable --now docker

mkdir -p /opt/documind

# This is the one script both the initial boot (below) and every later GitHub Actions
# deploy (via SSM Run Command) invoke — keeping "pull the new image, read secrets, run the
# container" in exactly one place instead of duplicating it between Terraform and CI.
cat > /opt/documind/deploy.sh <<'DEPLOY_SCRIPT'
#!/bin/bash
set -euo pipefail

REGION="${region}"
ECR_REPO_URL="${ecr_repo_url}"
RDS_ENDPOINT="${rds_endpoint}"
DB_NAME="${db_name}"
DB_USERNAME="${db_username}"
STORAGE_BUCKET="${storage_bucket}"
CORS_ORIGIN="${cors_origin}"
SSM_PATH_PREFIX="${ssm_path_prefix}"

aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "$ECR_REPO_URL"

docker pull "$ECR_REPO_URL:latest"

DB_PASSWORD=$(aws ssm get-parameter --name "$SSM_PATH_PREFIX/db_password" --with-decryption --query Parameter.Value --output text --region "$REGION")
OPENAI_API_KEY=$(aws ssm get-parameter --name "$SSM_PATH_PREFIX/openai_api_key" --with-decryption --query Parameter.Value --output text --region "$REGION")

docker rm -f documind-api 2>/dev/null || true

docker run -d \
  --name documind-api \
  --restart unless-stopped \
  -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__Postgres="Host=$RDS_ENDPOINT;Port=5432;Database=$DB_NAME;Username=$DB_USERNAME;Password=$DB_PASSWORD" \
  -e Database__MigrateOnStartup=true \
  -e Cors__AllowedOrigins__0="$CORS_ORIGIN" \
  -e Storage__Provider=S3 \
  -e Storage__BucketName="$STORAGE_BUCKET" \
  -e Storage__Region="$REGION" \
  -e Embedding__Provider=OpenAI \
  -e Embedding__ApiKey="$OPENAI_API_KEY" \
  -e Llm__Provider=OpenAI \
  -e Llm__ApiKey="$OPENAI_API_KEY" \
  "$ECR_REPO_URL:latest"
DEPLOY_SCRIPT

chmod +x /opt/documind/deploy.sh
/opt/documind/deploy.sh
