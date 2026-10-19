locals {
  operator_trust = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow", Action = "sts:AssumeRole"
      Principal = { AWS = [local.user_arn, "arn:aws:iam::${local.account_id}:role/managed/AccountFullAccessRole"] }
    }]
  })
  ecs_trust = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow", Action = "sts:AssumeRole", Principal = { Service = "ecs-tasks.amazonaws.com" }
      Condition = {
        StringEquals = { "aws:SourceAccount" = local.account_id }
        ArnLike      = { "aws:SourceArn" = "arn:aws:ecs:${local.region}:${local.account_id}:*" }
      }
    }]
  })
}

resource "aws_iam_role" "github" {
  for_each             = toset(["release", "deploy", "control"])
  name                 = "languagewise-${each.key}"
  path                 = "/languagewise/"
  assume_role_policy   = local.operator_trust
  max_session_duration = 7200
}

resource "aws_iam_user_policy" "routine_assumption" {
  name = "AssumeLanguageWiseRoutineRoles"
  user = aws_iam_user.runner.name
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow", Action = "sts:AssumeRole"
      Resource = [for role in aws_iam_role.github : role.arn]
    }]
  })
}

resource "aws_iam_role" "execution" {
  for_each           = local.services
  name               = "lw-demo-exec-${each.key}"
  path               = "/languagewise/"
  assume_role_policy = local.ecs_trust
}

resource "aws_iam_role" "task" {
  for_each           = local.services
  name               = "lw-demo-task-${each.key}"
  path               = "/languagewise/"
  assume_role_policy = local.ecs_trust
}

resource "aws_iam_role_policy" "execution" {
  for_each = local.services
  name     = "PullLogsAndExactSecrets"
  role     = aws_iam_role.execution[each.key].id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = concat([
      { Effect = "Allow", Action = ["ecr:GetAuthorizationToken"], Resource = "*" },
      {
        Effect   = "Allow", Action = ["ecr:BatchGetImage", "ecr:GetDownloadUrlForLayer", "ecr:BatchCheckLayerAvailability"]
        Resource = aws_ecr_repository.existing.arn
      },
      {
        Effect   = "Allow", Action = ["logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "arn:aws:logs:${local.region}:${local.account_id}:log-group:/languagewise/demo/${each.key}:*"
      }
      ], length(lookup(local.secret_consumers, each.key, [])) == 0 ? [] : [{
        Effect   = "Allow", Action = ["secretsmanager:GetSecretValue"]
        Resource = [for name in local.secret_consumers[each.key] : aws_secretsmanager_secret.application[name].arn]
        }], each.key == "database-initializer" ? [{
        Effect   = "Allow", Action = ["secretsmanager:GetSecretValue"]
        Resource = "arn:aws:secretsmanager:${local.region}:${local.account_id}:secret:rds!db-*"
    }] : [])
  })
}

resource "aws_iam_role_policy" "initializer" {
  name = "WriteOnlyDatabaseCredentials"
  role = aws_iam_role.task["database-initializer"].id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow", Action = ["secretsmanager:PutSecretValue", "secretsmanager:DescribeSecret"]
      Resource = [for name in local.database_secrets : aws_secretsmanager_secret.application[name].arn]
    }]
  })
}