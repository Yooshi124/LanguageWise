terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "6.69.1"
    }
  }
}

provider "aws" {
  region                      = "ap-southeast-2"
  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true
  skip_region_validation      = true
  endpoints {
    servicediscovery = "http://127.0.0.1:9"
  }
}

variable "empty_custom_health" {
  type    = bool
  default = true
}

resource "aws_service_discovery_service" "app" {
  name = "fixture"
  dns_config {
    namespace_id   = "ns-fixture"
    routing_policy = "MULTIVALUE"
    dns_records {
      ttl  = 10
      type = "A"
    }
  }
  dynamic "health_check_custom_config" {
    for_each = var.empty_custom_health ? [true] : []
    content {}
  }
}
