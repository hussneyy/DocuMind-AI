# The master password is generated here rather than typed into a .tfvars file, then written to
# SSM Parameter Store (ssm_parameters.tf) for the EC2 user-data script to read at boot. It never
# appears in a tfvars file, a GitHub secret, or application code.
resource "random_password" "db_password" {
  length  = 32
  special = false # Postgres connection strings choke on some special characters; alphanumeric is plenty strong at this length
}

resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-db-subnets"
  subnet_ids = aws_subnet.private[*].id

  tags = { Name = "${var.project_name}-db-subnets" }
}

resource "aws_db_instance" "main" {
  identifier     = "${var.project_name}-db"
  engine         = "postgres"
  engine_version = "16"

  # RDS Postgres 15.2+ ships the pgvector extension as a trusted extension — the master user
  # can run `CREATE EXTENSION vector` without superuser access. The app's existing EF Core
  # migration already does this (same as it does against the local pgvector/pgvector image),
  # so no extra step is needed here.
  instance_class = var.db_instance_class

  allocated_storage = 20 # Free Tier covers up to 20GB
  storage_type      = "gp2"

  db_name  = var.db_name
  username = var.db_username
  password = random_password.db_password.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  publicly_accessible    = false

  multi_az                = false # Multi-AZ is not Free Tier eligible
  backup_retention_period = 1
  skip_final_snapshot     = true # Simplifies `terraform destroy` for a portfolio project; reconsider for anything holding real data
  deletion_protection     = false

  tags = { Name = "${var.project_name}-db" }
}
