variable "foundation" {
  description = "Nonsecret retained-foundation outputs. Never supply secret values."
  type        = any
}

variable "release" {
  type = object({
    sha            = string
    images         = map(string)
    schema_version = optional(number, 1)
  })
  validation {
    condition     = can(regex("^[0-9a-f]{40}$", var.release.sha))
    error_message = "release.sha must be the full tested Git commit SHA."
  }
  validation {
    condition = alltrue([for image in values(var.release.images) : can(regex(
      "^[0-9]{12}\\.dkr\\.ecr\\.[a-z0-9-]+\\.amazonaws\\.com/[a-z0-9/_-]+@sha256:[0-9a-f]{64}$", image
    ))])
    error_message = "Every image must be an immutable private ECR URI@sha256 digest."
  }
  validation {
    condition     = toset(keys(var.release.images)) == setunion(var.release.schema_version == 2 ? local.all_names : local.legacy_names, toset(["database-initializer"]))
    error_message = "Release must contain the exact service inventory for its schema version and database-initializer."
  }
  validation {
    condition     = contains([1, 2], var.release.schema_version) && (!var.services_enabled || var.release.schema_version == 2)
    error_message = "Only version 2 may enable services; version 1 remains valid for legacy destruction."
  }
  validation {
    condition     = alltrue([for image in values(var.release.images) : startswith(image, "${var.foundation.repository_url}@sha256:")])
    error_message = "All images must belong to the retained foundation repository."
  }
}

variable "services_enabled" {
  description = "Enable only after the controller has awaited a successful initializer exit."
  type        = bool
  default     = false
}

variable "cold_start" {
  description = "Skip individual ECS waiters only for controller-verified empty-runtime provisioning. The controller waits for aggregate task health."
  type        = bool
  default     = false
}

variable "startup_phase" {
  description = "Cold provisioning starts database APIs and RAG before application services."
  type        = string
  default     = "all"
  validation {
    condition     = contains(["all", "dependencies"], var.startup_phase) && (var.startup_phase == "all" || (var.cold_start && var.services_enabled))
    error_message = "Dependency staging is allowed only during an enabled cold start."
  }
}

provider "aws" {
  region              = var.foundation.region
  allowed_account_ids = [var.foundation.account_id]
  default_tags {
    tags = {
      Project   = "languagewise"
      Layer     = "demo"
      Lifecycle = "disposable-demo"
    }
  }
}