mock_provider "aws" {
  mock_resource "aws_db_instance" {
    defaults = {
      address = "demo.example.ap-southeast-2.rds.amazonaws.com"
      master_user_secret = [{
        secret_arn    = "arn:aws:secretsmanager:ap-southeast-2:123456789012:secret:rds-managed-fixture-abcdef" # pragma: allowlist secret
        kms_key_id    = "00000000-0000-0000-0000-000000000000"
        secret_status = "active" # pragma: allowlist secret
      }]
    }
  }
  mock_resource "aws_lb" {
    defaults = {
      arn      = "arn:aws:elasticloadbalancing:ap-southeast-2:123456789012:loadbalancer/app/languagewise-demo/0123456789abcdef"
      dns_name = "internal-demo.ap-southeast-2.elb.amazonaws.com"
    }
  }
  mock_data "aws_ec2_managed_prefix_list" {
    defaults = { id = "pl-0123456789abcdef0" }
  }
  mock_data "aws_security_group" {
    defaults = { id = "sg-0123456789abcdef0" }
  }
}

variables {
  foundation = {
    account_id           = "123456789012"
    region               = "ap-southeast-2"
    cluster_arn          = "arn:aws:ecs:ap-southeast-2:123456789012:cluster/languagewise"
    repository_url       = "123456789012.dkr.ecr.ap-southeast-2.amazonaws.com/languagewise"
    state_bucket_name    = "fixture-state"
    demo_state_key       = "demo/terraform.tfstate"
    control_table_name   = "fixture-control"
    artifact_bucket_name = "fixture-artifacts"
    cleanup_project_name = "fixture-cleanup"
    deploy_role_arn      = "arn:aws:iam::123456789012:role/fixture-deploy"
    control_role_arn     = "arn:aws:iam::123456789012:role/fixture-control"
    service_execution_role_arns = { for name in concat(
      ["database-initializer", "shared-db-api", "shared-backend", "shared-frontend", "garry-ai-service-backend", "mcp-server", "rag-server"],
      flatten([for feature in ["mini-games", "quizzes-courses", "chat-discussion", "quests-achievements-notifications", "leaderboard-analytics"] : concat(
        ["${feature}-service-backend", "${feature}-service-frontend"], feature == "leaderboard-analytics" ? [] : ["${feature}-service-db"]
      )])
    ) : name => "arn:aws:iam::123456789012:role/fixture-execution" }
    service_task_role_arns = { for name in concat(
      ["database-initializer", "shared-db-api", "shared-backend", "shared-frontend", "garry-ai-service-backend", "mcp-server", "rag-server"],
      flatten([for feature in ["mini-games", "quizzes-courses", "chat-discussion", "quests-achievements-notifications", "leaderboard-analytics"] : concat(
        ["${feature}-service-backend", "${feature}-service-frontend"], feature == "leaderboard-analytics" ? [] : ["${feature}-service-db"]
      )])
    ) : name => "arn:aws:iam::123456789012:role/fixture-task" }
    secret_arns = { for name in [
      "db-shared", "db-mini-games", "db-quizzes-courses", "db-chat-discussion", "db-quests",
      "openrouter-api-key", "signing-private-key", "signing-public-key", "mcp-api-key"
    ] : name => "arn:aws:secretsmanager:ap-southeast-2:123456789012:secret:${name}-abcdef" }
  }
  release = {
    schema_version = 2
    sha            = "0123456789abcdef0123456789abcdef01234567" # pragma: allowlist secret
    images = { for name in concat(
      ["database-initializer", "shared-db-api", "shared-backend", "shared-frontend", "garry-ai-service-backend", "mcp-server", "rag-server"],
      flatten([for feature in ["mini-games", "quizzes-courses", "chat-discussion", "quests-achievements-notifications", "leaderboard-analytics"] : concat(
        ["${feature}-service-backend", "${feature}-service-frontend"], feature == "leaderboard-analytics" ? [] : ["${feature}-service-db"]
      )])
    ) : name => "123456789012.dkr.ecr.ap-southeast-2.amazonaws.com/languagewise@sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" }
  }
}

run "infrastructure_before_seed" {
  command = plan

  assert {
    condition     = length(aws_ecs_service.app) == 0 && length(aws_ecs_task_definition.app) == 20
    error_message = "Phase one must define all twenty tasks but create no application services."
  }
  assert {
    condition     = aws_ecs_task_definition.initializer.cpu == "512" && aws_ecs_task_definition.initializer.memory == "1024"
    error_message = "The initializer must have the same approved Fargate size."
  }
  assert {
    condition     = alltrue([for task in aws_ecs_task_definition.app : task.cpu == "512" && task.memory == "1024" && task.network_mode == "awsvpc"])
    error_message = "All twenty tasks must use the approved Fargate size."
  }
  assert {
    condition     = aws_db_instance.demo.publicly_accessible == false && aws_db_instance.demo.storage_encrypted && aws_db_instance.demo.backup_retention_period == 0 && aws_db_instance.demo.skip_final_snapshot && aws_db_instance.demo.delete_automated_backups
    error_message = "RDS must be private, encrypted and fully disposable."
  }
  assert {
    condition     = length(aws_vpc_security_group_ingress_rule.database) == 6 && alltrue([for edge in aws_vpc_security_group_ingress_rule.database : edge.from_port == 5432 && edge.to_port == 5432])
    error_message = "Only the five database APIs and initializer may reach PostgreSQL."
  }
  assert {
    condition = toset([for name, task in aws_ecs_task_definition.app : name if contains(
      [for secret in jsondecode(task.container_definitions)[0].secrets : secret.name], "OpenRouter__ApiKey"
    )]) == toset(["garry-ai-service-backend", "quests-achievements-notifications-service-backend", "leaderboard-analytics-service-backend"])
    error_message = "Only the three intended AI consumers may receive OpenRouter credentials."
  }
  assert {
    condition = alltrue([for name in local.backends : alltrue([
      for entry in jsondecode(aws_ecs_task_definition.app[name].container_definitions)[0].environment :
      entry.value == (entry.name == "Mcp__Enabled" ? (name == "shared-backend" ? "false" : "true") : (name == "shared-backend" ? "true" : "false"))
      if contains(["Mcp__Enabled", "Rag__Enabled"], entry.name)
    ])])
    error_message = "Only the six MCP callers and shared RAG caller may enable their integrations."
  }
  assert {
    condition     = !contains(keys(jsondecode(aws_ecs_task_definition.app["quests-achievements-notifications-service-db"].container_definitions)[0]), "healthCheck")
    error_message = "PostgREST must not receive an unsupported shell health check."
  }
  assert {
    condition = (
      jsondecode(aws_ecs_task_definition.app["mcp-server"].container_definitions)[0].portMappings[0].containerPort == 8080 &&
      jsondecode(aws_ecs_task_definition.app["rag-server"].container_definitions)[0].portMappings[0].containerPort == 8100 &&
      jsondecode(aws_ecs_task_definition.app["rag-server"].container_definitions)[0].healthCheck.command[0] == "CMD" &&
      jsondecode(aws_ecs_task_definition.app["rag-server"].container_definitions)[0].healthCheck.command[1] == "python" &&
      jsondecode(aws_ecs_task_definition.app["rag-server"].container_definitions)[0].healthCheck.startPeriod == 300
    )
    error_message = "MCP and RAG need their real private ports and an executable Python RAG probe with bounded cold-start grace."
  }
  assert {
    condition = (
      toset([for edge in values(local.caller_edges) : edge.caller if edge.target == "mcp-server"]) == local.mcp_callers &&
      toset([for edge in values(local.caller_edges) : edge.caller if edge.target == "rag-server"]) == toset(["shared-backend", "mcp-server"]) &&
      toset([for edge in values(local.caller_edges) : edge.target if edge.caller == "mcp-server"]) == setunion(toset([for feature in local.features : "${feature}-service-backend"]), toset(["rag-server"]))
    )
    error_message = "Only approved private callers may reach MCP/RAG and the tool server may reach only its declared downstream services."
  }
  assert {
    condition = (
      toset([for name, task in aws_ecs_task_definition.app : name if contains([for secret in jsondecode(task.container_definitions)[0].secrets : secret.name], "Mcp__ApiKey")]) == setunion(local.mcp_callers, toset(["mcp-server"])) &&
      length(jsondecode(aws_ecs_task_definition.app["rag-server"].container_definitions)[0].secrets) == 0 &&
      toset([for secret in jsondecode(aws_ecs_task_definition.app["mcp-server"].container_definitions)[0].secrets : secret.name]) == toset(["Mcp__ApiKey", "Auth__VerificationKeyPem"])
    )
    error_message = "Only MCP and its six callers receive the MCP key; RAG receives no application credentials."
  }
}

run "services_after_seed" {
  command = plan
  variables {
    services_enabled = true
  }
  assert {
    condition     = length(aws_ecs_service.app) == 20 && alltrue([for service in aws_ecs_service.app : service.desired_count == 1 && service.deployment_circuit_breaker[0].enable && service.deployment_circuit_breaker[0].rollback])
    error_message = "Phase two must create exactly twenty healthy, rollback-enabled ECS services."
  }
  assert {
    condition     = length(aws_lb.gateway.security_groups) == 1
    error_message = "The enabled ALB must detach the bootstrap prefix-list SG."
  }
}

run "reject_missing_image" {
  command = plan
  variables {
    release = {
      sha    = "0123456789abcdef0123456789abcdef01234567" # pragma: allowlist secret
      images = {}
    }
  }
  expect_failures = [var.release]
}

run "legacy_destroy_inventory" {
  command = plan
  variables {
    release = merge(var.release, {
      schema_version = 1
      images         = { for name, image in var.release.images : name => image if !contains(["mcp-server", "rag-server"], name) }
    })
  }
  assert {
    condition     = length(aws_ecs_task_definition.app) == 18 && length(aws_ecs_service.app) == 0 && !contains(keys(aws_ecs_task_definition.app), "mcp-server") && !contains(keys(aws_ecs_task_definition.app), "rag-server")
    error_message = "Legacy destruction must resolve its original eighteen image keys without requesting nonexistent MCP/RAG images."
  }
}

run "reject_legacy_service_enablement" {
  command = plan
  variables {
    services_enabled = true
    release = merge(var.release, {
      schema_version = 1
      images         = { for name, image in var.release.images : name => image if !contains(["mcp-server", "rag-server"], name) }
    })
  }
  expect_failures = [var.release]
}