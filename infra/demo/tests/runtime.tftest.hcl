mock_provider "aws" {
  mock_resource "aws_db_instance" {
    defaults = {
      address = "demo.example.ap-southeast-2.rds.amazonaws.com"
      master_user_secret = [{
        secret_arn    = "arn:aws:secretsmanager:ap-southeast-2:123456789012:secret:rds-managed-fixture-abcdef"
        kms_key_id    = "00000000-0000-0000-0000-000000000000"
        secret_status = "active"
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
      ["database-initializer", "shared-db-api", "shared-backend", "shared-frontend", "garry-ai-service-backend"],
      flatten([for feature in ["mini-games", "quizzes-courses", "chat-discussion", "quests-achievements-notifications", "leaderboard-analytics"] : concat(
        ["${feature}-service-backend", "${feature}-service-frontend"], feature == "leaderboard-analytics" ? [] : ["${feature}-service-db"]
      )])
    ) : name => "arn:aws:iam::123456789012:role/fixture-execution" }
    service_task_role_arns = { for name in concat(
      ["database-initializer", "shared-db-api", "shared-backend", "shared-frontend", "garry-ai-service-backend"],
      flatten([for feature in ["mini-games", "quizzes-courses", "chat-discussion", "quests-achievements-notifications", "leaderboard-analytics"] : concat(
        ["${feature}-service-backend", "${feature}-service-frontend"], feature == "leaderboard-analytics" ? [] : ["${feature}-service-db"]
      )])
    ) : name => "arn:aws:iam::123456789012:role/fixture-task" }
    secret_arns = { for name in [
      "db-shared", "db-mini-games", "db-quizzes-courses", "db-chat-discussion", "db-quests",
      "openrouter-api-key", "signing-private-key", "signing-public-key"
    ] : name => "arn:aws:secretsmanager:ap-southeast-2:123456789012:secret:${name}-abcdef" }
  }
  release = {
    sha = "0123456789abcdef0123456789abcdef01234567"
    images = { for name in concat(
      ["database-initializer", "shared-db-api", "shared-backend", "shared-frontend", "garry-ai-service-backend"],
      flatten([for feature in ["mini-games", "quizzes-courses", "chat-discussion", "quests-achievements-notifications", "leaderboard-analytics"] : concat(
        ["${feature}-service-backend", "${feature}-service-frontend"], feature == "leaderboard-analytics" ? [] : ["${feature}-service-db"]
      )])
    ) : name => "123456789012.dkr.ecr.ap-southeast-2.amazonaws.com/languagewise@sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" }
  }
}

run "infrastructure_before_seed" {
  command = plan

  assert {
    condition     = length(aws_ecs_service.app) == 0 && length(aws_ecs_task_definition.app) == 18
    error_message = "Phase one must define all eighteen tasks but create no application services."
  }
  assert {
    condition     = aws_ecs_task_definition.initializer.cpu == "512" && aws_ecs_task_definition.initializer.memory == "1024"
    error_message = "The initializer must have the same approved Fargate size."
  }
  assert {
    condition     = alltrue([for task in aws_ecs_task_definition.app : task.cpu == "512" && task.memory == "1024" && task.network_mode == "awsvpc"])
    error_message = "All eighteen tasks must use the approved Fargate size."
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
      for entry in jsondecode(aws_ecs_task_definition.app[name].container_definitions)[0].environment : entry.value == "false"
      if contains(["Mcp__Enabled", "Rag__Enabled"], entry.name)
    ])])
    error_message = "MCP and RAG must be disabled on every backend."
  }
  assert {
    condition     = !contains(keys(jsondecode(aws_ecs_task_definition.app["quests-achievements-notifications-service-db"].container_definitions)[0]), "healthCheck")
    error_message = "PostgREST must not receive an unsupported shell health check."
  }
}

run "services_after_seed" {
  command = plan
  variables {
    services_enabled = true
  }
  assert {
    condition     = length(aws_ecs_service.app) == 18 && alltrue([for service in aws_ecs_service.app : service.desired_count == 1 && service.deployment_circuit_breaker[0].enable && service.deployment_circuit_breaker[0].rollback])
    error_message = "Phase two must create exactly eighteen healthy, rollback-enabled ECS services."
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
      sha    = "0123456789abcdef0123456789abcdef01234567"
      images = {}
    }
  }
  expect_failures = [var.release]
}