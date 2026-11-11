# Bootstrap: one-time remote state setup

Terraform's main configuration (`infra/`) stores its state in an S3 bucket. That bucket has to
exist *before* Terraform can use it as a backend — a chicken-and-egg problem — so this tiny,
separate configuration creates it using plain local state, and you run it by hand exactly once.

## Run this once

```powershell
cd infra/bootstrap
terraform init
terraform apply -var="state_bucket_name=documind-ai-tfstate-<something-unique>"
```

S3 bucket names are global across every AWS account, so pick something you're confident is
unique (e.g. your GitHub username plus a short random suffix).

Note the two outputs:

```
bucket_name          = "documind-ai-tfstate-..."
dynamodb_table_name  = "documind-ai-tfstate-lock"
```

## Then wire the main config to it

Edit `infra/versions.tf`'s `backend "s3"` block, replacing the two `CHANGE_ME` placeholders with
the `bucket_name` output above (the `dynamodb_table` is already correct — it's a fixed name).

From then on, every `terraform init`/`plan`/`apply` in `infra/` reads and writes state through
that bucket, with the DynamoDB table preventing two concurrent applies (e.g. you locally and a
GitHub Actions run) from corrupting it.

You will not run anything in this `bootstrap/` directory again unless you tear down the whole
project and want to start over.
