terraform {
  required_version = "~> 1.16.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
    archive = {
      source  = "hashicorp/archive"
      version = "2.7.1"
    }
  }
  backend "s3" {
    key                 = "foundation/terraform.tfstate"
    region              = "ap-southeast-2"
    encrypt             = true
    use_lockfile        = true
    allowed_account_ids = ["047754151573"]
  }
}

provider "aws" {
  region              = local.region
  allowed_account_ids = [local.account_id]
  default_tags {
    tags = { Project = "languagewise", Layer = "foundation" }
  }
}