# Private bucket for uploaded PDFs — read/written only by the API via its EC2 instance role
# (iam_oidc.tf / ec2.tf), never exposed directly to the browser.

resource "aws_s3_bucket" "storage" {
  bucket = var.storage_bucket_name

  tags = { Name = "${var.project_name}-storage" }
}

resource "aws_s3_bucket_public_access_block" "storage" {
  bucket                  = aws_s3_bucket.storage.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "storage" {
  bucket = aws_s3_bucket.storage.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}
