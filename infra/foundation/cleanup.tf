resource "aws_iam_role" "cleanup" {
  name = "${local.prefix}-cleanup"
  path = "/languagewise/"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow", Action = "sts:AssumeRole", Principal = { Service = "codebuild.amazonaws.com" }
      Condition = {
        StringEquals = { "aws:SourceAccount" = local.account_id }
        ArnEquals    = { "aws:SourceArn" = "arn:aws:codebuild:${local.region}:${local.account_id}:project/${local.foundation_contract.cleanup_project_name}" }
      }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "cleanup_runtime" {
  for_each   = aws_iam_policy.cleanup_runtime
  role       = aws_iam_role.cleanup.name
  policy_arn = each.value.arn
}

resource "aws_iam_role_policy" "cleanup" {
  name = "IndependentCleanupStateAndSource"
  role = aws_iam_role.cleanup.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = concat(local.control_statements, local.state_statements, [
      {
        Sid       = "CleanupSourceCurrentObjectRead"
        Effect    = "Allow", Action = ["s3:GetObject"]
        Resource  = "${data.aws_s3_bucket.artifacts.arn}/${var.cleanup_bundle_key}"
        Condition = local.regional
      },
      {
        Sid       = "CleanupSourcePinnedVersionRead"
        Effect    = "Allow", Action = ["s3:GetObjectVersion"]
        Resource  = "${data.aws_s3_bucket.artifacts.arn}/${var.cleanup_bundle_key}"
        Condition = { StringEquals = { "s3:VersionId" = var.cleanup_bundle_version, "aws:RequestedRegion" = local.region } }
      },
      {
        Effect   = "Allow", Action = ["s3:GetBucketAcl"]
        Resource = data.aws_s3_bucket.artifacts.arn, Condition = local.regional
      },
      {
        Effect   = "Allow", Action = ["logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "${aws_cloudwatch_log_group.cleanup.arn}:*", Condition = local.regional
      }
    ])
  })
}

resource "aws_cloudwatch_log_group" "cleanup" {
  name              = "/languagewise/foundation/cleanup"
  retention_in_days = 7
}

resource "aws_codebuild_project" "cleanup" {
  name                   = local.foundation_contract.cleanup_project_name
  description            = "Independent lease-expiry cleanup; reviewed versioned source, no GitHub credentials."
  service_role           = aws_iam_role.cleanup.arn
  build_timeout          = 120
  queued_timeout         = 30
  concurrent_build_limit = 1
  source_version         = var.cleanup_bundle_version
  artifacts {
    type = "NO_ARTIFACTS"
  }
  source {
    type     = "S3"
    location = "${data.aws_s3_bucket.artifacts.id}/${var.cleanup_bundle_key}"
    buildspec = yamlencode({
      version = "0.2"
      phases = {
        install = {
          "runtime-versions" = { nodejs = "22" }
          commands = [
            "curl --fail --silent --show-error --location --retry 3 -o /tmp/terraform.zip https://releases.hashicorp.com/terraform/${var.terraform_version}/terraform_${var.terraform_version}_linux_amd64.zip",
            "printf '%s  /tmp/terraform.zip\\n' '${lower(var.terraform_sha256)}' | sha256sum --check --strict -",
            "unzip -o /tmp/terraform.zip -d /usr/local/bin",
            "terraform version"
          ]
        }
        build = { commands = ["node infra/scripts/demo.mjs cleanup"] }
      }
    })
  }
  environment {
    compute_type                = "BUILD_GENERAL1_SMALL"
    image                       = "aws/codebuild/standard:7.0"
    type                        = "LINUX_CONTAINER"
    image_pull_credentials_type = "CODEBUILD"
    privileged_mode             = false
    environment_variable {
      name  = "AWS_REGION"
      value = local.region
    }
    environment_variable {
      name  = "AWS_DEFAULT_REGION"
      value = local.region
    }
    environment_variable {
      name  = "FOUNDATION_CONFIG"
      value = jsonencode(local.foundation_contract)
    }
    environment_variable {
      name  = "TF_VAR_foundation"
      value = jsonencode(local.foundation_contract)
    }
    environment_variable {
      name  = "TF_IN_AUTOMATION"
      value = "true"
    }
    environment_variable {
      name  = "TF_INPUT"
      value = "false"
    }
  }
  logs_config {
    cloudwatch_logs {
      group_name  = aws_cloudwatch_log_group.cleanup.name
      stream_name = "cleanup"
    }
    s3_logs {
      status = "DISABLED"
    }
  }
  depends_on = [aws_iam_role_policy.cleanup, aws_iam_role_policy_attachment.cleanup_runtime]
}

resource "aws_scheduler_schedule_group" "cleanup" {
  name = local.foundation_contract.cleanup_schedule_group_name
}

data "archive_file" "expiry" {
  type        = "zip"
  output_path = "${path.module}/.terraform/expiry.zip"
  source {
    content  = file("${path.module}/../scripts/expiry.mjs")
    filename = "expiry.mjs"
  }
  source {
    content  = file("${path.module}/../scripts/control.mjs")
    filename = "control.mjs"
  }
}

resource "aws_cloudwatch_log_group" "dispatcher" {
  name              = "/aws/lambda/${local.prefix}-cleanup-dispatcher"
  retention_in_days = 7
}

resource "aws_iam_role" "dispatcher" {
  name = "${local.prefix}-cleanup-dispatcher"
  path = "/languagewise/"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow", Action = "sts:AssumeRole", Principal = { Service = "lambda.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy" "dispatcher" {
  name = "ReadControlAndStartOnlyCleanup"
  role = aws_iam_role.dispatcher.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow", Action = "dynamodb:GetItem", Resource = aws_dynamodb_table.control.arn
        Condition = { StringEquals = { "aws:RequestedRegion" = local.region }, "ForAllValues:StringEquals" = { "dynamodb:LeadingKeys" = ["control"] } }
      },
      {
        Effect    = "Allow", Action = "codebuild:StartBuild", Resource = aws_codebuild_project.cleanup.arn
        Condition = local.regional
      },
      {
        Effect   = "Allow", Action = ["logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "${aws_cloudwatch_log_group.dispatcher.arn}:*", Condition = local.regional
      }
    ]
  })
}

resource "aws_lambda_function" "dispatcher" {
  function_name                  = "${local.prefix}-cleanup-dispatcher"
  role                           = aws_iam_role.dispatcher.arn
  runtime                        = "nodejs22.x"
  handler                        = "expiry.handler"
  filename                       = data.archive_file.expiry.output_path
  source_code_hash               = data.archive_file.expiry.output_base64sha256
  timeout                        = 15
  memory_size                    = 128
  reserved_concurrent_executions = 1
  environment {
    variables = {
      CONTROL_TABLE_NAME   = local.foundation_contract.control_table_name
      CLEANUP_PROJECT_NAME = local.foundation_contract.cleanup_project_name
    }
  }
  lifecycle {
    prevent_destroy = true
  }
  depends_on = [aws_iam_role_policy.dispatcher, aws_cloudwatch_log_group.dispatcher]
}

resource "aws_iam_role" "scheduler" {
  name = "${local.prefix}-scheduler"
  path = "/languagewise/"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow", Action = "sts:AssumeRole", Principal = { Service = "scheduler.amazonaws.com" }
      Condition = {
        StringEquals = { "aws:SourceAccount" = local.account_id }
        ArnEquals    = { "aws:SourceArn" = aws_scheduler_schedule_group.cleanup.arn }
      }
    }]
  })
}

resource "aws_iam_role_policy" "scheduler" {
  name = "StartOnlyIndependentCleanup"
  role = aws_iam_role.scheduler.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow", Action = "lambda:InvokeFunction", Resource = aws_lambda_function.dispatcher.arn
    }]
  })
}

resource "aws_scheduler_schedule" "cleanup" {
  name                         = local.foundation_contract.cleanup_schedule_name
  group_name                   = aws_scheduler_schedule_group.cleanup.name
  schedule_expression          = "rate(15 minutes)"
  schedule_expression_timezone = "Australia/Sydney"
  state                        = "ENABLED"
  flexible_time_window {
    mode = "OFF"
  }
  target {
    arn      = aws_lambda_function.dispatcher.arn
    role_arn = aws_iam_role.scheduler.arn
    input    = jsonencode({})
    retry_policy {
      maximum_event_age_in_seconds = 900
      maximum_retry_attempts       = 2
    }
  }
  depends_on = [aws_iam_role_policy.scheduler]
}

resource "aws_sns_topic" "cleanup_failure" {
  name = "${local.prefix}-cleanup-failure"
}

resource "aws_sns_topic_policy" "cleanup_failure" {
  arn = aws_sns_topic.cleanup_failure.arn
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "AlarmOnlyPublish", Effect = "Allow", Principal = { Service = "cloudwatch.amazonaws.com" }
      Action = "sns:Publish", Resource = aws_sns_topic.cleanup_failure.arn
      Condition = {
        StringEquals = { "aws:SourceAccount" = local.account_id }
        ArnEquals = { "aws:SourceArn" = [for name in [
          local.foundation_contract.cleanup_alarm_name, local.foundation_contract.cleanup_scheduler_alarm_name,
          local.foundation_contract.cleanup_dispatcher_alarm_name, local.foundation_contract.cleanup_dispatcher_throttle_alarm_name
        ] : "arn:aws:cloudwatch:${local.region}:${local.account_id}:alarm:${name}"] }
      }
    }]
  })
}

resource "aws_cloudwatch_metric_alarm" "cleanup_failure" {
  alarm_name          = local.foundation_contract.cleanup_alarm_name
  alarm_description   = "Independent cleanup failed; inspect residual paid resources and durable control state."
  namespace           = "AWS/CodeBuild"
  metric_name         = "FailedBuilds"
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  comparison_operator = "GreaterThanOrEqualToThreshold"
  threshold           = 1
  treat_missing_data  = "notBreaching"
  dimensions          = { ProjectName = aws_codebuild_project.cleanup.name }
  alarm_actions       = [aws_sns_topic.cleanup_failure.arn]
}

resource "aws_cloudwatch_metric_alarm" "scheduler_failure" {
  alarm_name          = local.foundation_contract.cleanup_scheduler_alarm_name
  alarm_description   = "Scheduler cannot invoke the independent cleanup dispatcher."
  namespace           = "AWS/Scheduler"
  metric_name         = "TargetErrorCount"
  statistic           = "Sum"
  period              = 900
  evaluation_periods  = 1
  comparison_operator = "GreaterThanOrEqualToThreshold"
  threshold           = 1
  treat_missing_data  = "notBreaching"
  dimensions          = { ScheduleGroup = aws_scheduler_schedule_group.cleanup.name }
  alarm_actions       = [aws_sns_topic.cleanup_failure.arn]
}

resource "aws_cloudwatch_metric_alarm" "dispatcher_failure" {
  for_each = {
    Errors    = local.foundation_contract.cleanup_dispatcher_alarm_name
    Throttles = local.foundation_contract.cleanup_dispatcher_throttle_alarm_name
  }
  alarm_name          = each.value
  alarm_description   = "Independent cleanup dispatcher failed; cleanup may be delayed."
  namespace           = "AWS/Lambda"
  metric_name         = each.key
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  comparison_operator = "GreaterThanOrEqualToThreshold"
  threshold           = 1
  treat_missing_data  = "notBreaching"
  dimensions          = { FunctionName = aws_lambda_function.dispatcher.function_name }
  alarm_actions       = [aws_sns_topic.cleanup_failure.arn]
}