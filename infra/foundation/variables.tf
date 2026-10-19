variable "state_bucket_name" {
  type    = string
  default = "languagewise-047754151573-ap-southeast-2-tfstate"
  validation {
    condition     = can(regex("^languagewise-[a-z0-9-]{3,49}$", var.state_bucket_name))
    error_message = "Use the project-prefixed bucket created by bootstrap."
  }
}

variable "cleanup_bundle_key" {
  type        = string
  description = "Operator-published, reviewed ZIP containing repo-relative infra/scripts and infra/demo paths."
  validation {
    condition     = can(regex("^cleanup/[A-Za-z0-9/_-]+\\.zip$", var.cleanup_bundle_key))
    error_message = "Supply an actual ZIP object under cleanup/, not a placeholder or release manifest."
  }
}

variable "cleanup_bundle_version" {
  type        = string
  description = "Actual non-null S3 VersionId of the reviewed cleanup ZIP."
  validation {
    condition     = length(trimspace(var.cleanup_bundle_version)) > 0 && lower(var.cleanup_bundle_version) != "null"
    error_message = "A real, versioned S3 source is mandatory."
  }
}

variable "terraform_version" {
  type        = string
  default     = "1.16.6"
  description = "Reviewed Linux amd64 Terraform release, constrained to the root's supported minor."
  validation {
    condition     = can(regex("^1\\.16\\.[0-9]+$", var.terraform_version))
    error_message = "Use a reviewed Terraform 1.16 patch release."
  }
}

variable "terraform_sha256" {
  type        = string
  description = "Operator-reviewed SHA256 of the terraform_version linux_amd64.zip, from authenticated HashiCorp checksums."
  validation {
    condition     = can(regex("^[a-fA-F0-9]{64}$", var.terraform_sha256))
    error_message = "Provide the reviewed 64-hex-character archive checksum."
  }
}