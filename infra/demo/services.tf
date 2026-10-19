locals {
  features = toset([
    "mini-games", "quizzes-courses", "chat-discussion",
    "quests-achievements-notifications", "leaderboard-analytics"
  ])
  frontends = toset(concat(["shared-frontend"], [for feature in local.features : "${feature}-service-frontend"]))
  backends  = toset(concat(["shared-backend", "garry-ai-service-backend"], [for feature in local.features : "${feature}-service-backend"]))
  database_secrets = {
    shared-db-api                                = "db-shared"
    mini-games-service-db                        = "db-mini-games"
    quizzes-courses-service-db                   = "db-quizzes-courses"
    chat-discussion-service-db                   = "db-chat-discussion"
    quests-achievements-notifications-service-db = "db-quests"
  }
  names = setunion(local.frontends, local.backends, toset(keys(local.database_secrets)))
  ports = { for name in local.names : name => (
    contains(local.frontends, name) ? 80 : name == "quests-achievements-notifications-service-db" ? 3000 : 8080
  ) }
  endpoint = { for name, port in local.ports : name => "http://${name}.languagewise.internal:${port}" }
  dependencies = merge(
    { for feature in local.features : "${feature}-service-frontend" => ["${feature}-service-backend"] },
    { for feature in setsubtract(local.features, toset(["leaderboard-analytics"])) : "${feature}-service-backend" => concat(
      ["${feature}-service-db", "garry-ai-service-backend"],
      feature == "quests-achievements-notifications" ? [] : ["quests-achievements-notifications-service-backend"],
      feature == "mini-games" ? ["quizzes-courses-service-backend"] : []
    ) },
    {
      shared-frontend                       = concat(["shared-backend"], [for feature in local.features : "${feature}-service-frontend"], [for feature in local.features : "${feature}-service-backend"])
      shared-backend                        = ["shared-db-api", "quests-achievements-notifications-service-backend", "garry-ai-service-backend"]
      leaderboard-analytics-service-backend = ["quizzes-courses-service-backend", "garry-ai-service-backend"]
      chat-discussion-service-db            = ["quizzes-courses-service-db", "shared-db-api"]
    }
  )
  caller_edges = merge([for caller, targets in local.dependencies : {
    for target in targets : "${caller}:${target}" => { caller = caller, target = target }
  }]...)
  service_environment = merge(
    { for feature in setsubtract(local.features, toset(["leaderboard-analytics"])) : "${feature}-service-backend" => merge({
      Services__Database = local.endpoint["${feature}-service-db"]
      Services__Garry    = local.endpoint["garry-ai-service-backend"]
      }, feature == "quests-achievements-notifications" ? {} : {
      Services__Achievements = local.endpoint["quests-achievements-notifications-service-backend"]
      }, feature == "mini-games" ? {
      Services__Courses = local.endpoint["quizzes-courses-service-backend"]
    } : {}) },
    {
      shared-backend = {
        Services__Database           = local.endpoint["shared-db-api"]
        Services__Achievements       = local.endpoint["quests-achievements-notifications-service-backend"]
        Services__Garry              = local.endpoint["garry-ai-service-backend"]
        ReverseProxy__TrustedNetwork = "10.82.0.0/16"
      }
      leaderboard-analytics-service-backend = {
        Services__QuizzesCourses = local.endpoint["quizzes-courses-service-backend"]
        Services__Garry          = local.endpoint["garry-ai-service-backend"]
      }
      chat-discussion-service-db = {
        Services__Courses = local.endpoint["quizzes-courses-service-db"]
        Services__Users   = local.endpoint["shared-db-api"]
      }
      quests-achievements-notifications-service-db = {
        PGRST_DB_SCHEMA    = "api"
        PGRST_DB_ANON_ROLE = "web_anon"
      }
    }
  )
  openrouter_consumers = toset([
    "garry-ai-service-backend", "quests-achievements-notifications-service-backend", "leaderboard-analytics-service-backend"
  ])
  environment = { for name in local.names : name => merge(
    lookup(local.service_environment, name, {}),
    contains(local.frontends, name) ? {
      DNS_RESOLVER = "169.254.169.253"
      DNS_SUFFIX   = ".languagewise.internal"
    } : {},
    name == "shared-frontend" ? { FORWARDED_SCHEME = "https" } : {},
    contains(local.backends, name) ? { Mcp__Enabled = "false", Rag__Enabled = "false", ASPNETCORE_HTTP_PORTS = "8080" } : {},
    contains(local.openrouter_consumers, name) ? {
      OpenRouter__BaseUrl         = "https://openrouter.ai/api/v1"
      OpenRouter__Model           = "google/gemma-4-26b-a4b-it"
      OpenRouter__MaxOutputTokens = "1024"
    } : {}
  ) }
  service_secrets = { for name in local.names : name => merge(
    contains(keys(local.database_secrets), name) ? {
      (name == "quests-achievements-notifications-service-db" ? "PGRST_DB_URI" : "Database__ConnectionString") = "${var.foundation.secret_arns[local.database_secrets[name]]}:${name == "quests-achievements-notifications-service-db" ? "uri" : "connection"}::"
    } : {},
    contains(local.openrouter_consumers, name) ? { OpenRouter__ApiKey = var.foundation.secret_arns["openrouter-api-key"] } : {},
    name == "shared-backend" ? { Auth__SigningKeyPem = var.foundation.secret_arns["signing-private-key"] } : {},
    contains(setsubtract(local.backends, toset(["shared-backend"])), name) ? { Auth__VerificationKeyPem = var.foundation.secret_arns["signing-public-key"] } : {}
  ) }
}

resource "aws_cloudwatch_log_group" "task" {
  for_each          = setunion(local.names, toset(["database-initializer"]))
  name              = "/languagewise/demo/${each.key}"
  retention_in_days = 7
}

resource "aws_service_discovery_private_dns_namespace" "demo" {
  name = "languagewise.internal"
  vpc  = aws_vpc.demo.id
}

resource "aws_service_discovery_service" "app" {
  for_each = local.names
  name     = each.key
  dns_config {
    namespace_id   = aws_service_discovery_private_dns_namespace.demo.id
    routing_policy = "MULTIVALUE"
    dns_records {
      ttl  = 10
      type = "A"
    }
  }
  health_check_custom_config {
  }
}

resource "aws_ecs_task_definition" "app" {
  for_each                 = local.names
  family                   = "languagewise-demo-${each.key}"
  skip_destroy             = true
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = var.foundation.service_execution_role_arns[each.key]
  task_role_arn            = var.foundation.service_task_role_arns[each.key]
  runtime_platform {
    operating_system_family = "LINUX"
    cpu_architecture        = "X86_64"
  }
  container_definitions = jsonencode([merge({
    name         = each.key
    image        = var.release.images[each.key]
    essential    = true
    portMappings = [{ containerPort = local.ports[each.key], hostPort = local.ports[each.key], protocol = "tcp" }]
    environment  = [for key, value in local.environment[each.key] : { name = key, value = value }]
    secrets      = [for key, value in local.service_secrets[each.key] : { name = key, valueFrom = value }]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.task[each.key].name
        awslogs-region        = var.foundation.region
        awslogs-stream-prefix = "app"
      }
    }
    }, each.key == "quests-achievements-notifications-service-db" ? {} : {
    healthCheck = {
      command     = ["CMD-SHELL", contains(local.frontends, each.key) ? "wget --quiet --tries=1 --spider http://127.0.0.1/${each.key == "shared-frontend" ? "health" : "remoteEntry.js"} || exit 1" : "curl --fail --silent http://127.0.0.1:8080/health || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 60
    }
  })])
}

resource "aws_ecs_service" "app" {
  for_each                           = var.services_enabled ? local.names : toset([])
  name                               = "languagewise-demo-${each.key}"
  cluster                            = var.foundation.cluster_arn
  task_definition                    = aws_ecs_task_definition.app[each.key].arn
  desired_count                      = 1
  launch_type                        = "FARGATE"
  platform_version                   = "1.4.0"
  wait_for_steady_state              = true
  deployment_minimum_healthy_percent = 100
  deployment_maximum_percent         = 200
  health_check_grace_period_seconds  = each.key == "shared-frontend" ? 60 : null
  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }
  network_configuration {
    subnets          = values(aws_subnet.tasks)[*].id
    security_groups  = [aws_security_group.task[each.key].id]
    assign_public_ip = true
  }
  service_registries {
    registry_arn = aws_service_discovery_service.app[each.key].arn
  }
  dynamic "load_balancer" {
    for_each = each.key == "shared-frontend" ? [1] : []
    content {
      target_group_arn = aws_lb_target_group.gateway.arn
      container_name   = each.key
      container_port   = 80
    }
  }
  timeouts {
    create = "30m"
    update = "30m"
    delete = "20m"
  }
  depends_on = [aws_lb_listener.gateway, aws_vpc_security_group_ingress_rule.caller, aws_vpc_security_group_ingress_rule.database, aws_route_table_association.tasks]
}