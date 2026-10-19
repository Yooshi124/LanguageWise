locals {
  foundation_contract = {
    account_id                             = local.account_id
    region                                 = local.region
    cluster_arn                            = aws_ecs_cluster.existing.arn
    repository_url                         = aws_ecr_repository.existing.repository_url
    control_table_name                     = aws_dynamodb_table.control.name
    artifact_bucket_name                   = data.aws_s3_bucket.artifacts.id
    cleanup_project_name                   = "${local.prefix}-cleanup"
    cleanup_schedule_name                  = "${local.prefix}-cleanup"
    cleanup_schedule_group_name            = "${local.prefix}-cleanup"
    cleanup_alarm_name                     = "${local.prefix}-cleanup-failed"
    cleanup_role_arn                       = "arn:aws:iam::${local.account_id}:role/languagewise/${local.prefix}-cleanup"
    cleanup_dispatcher_arn                 = "arn:aws:lambda:${local.region}:${local.account_id}:function:${local.prefix}-cleanup-dispatcher"
    cleanup_dispatcher_role_arn            = "arn:aws:iam::${local.account_id}:role/languagewise/${local.prefix}-cleanup-dispatcher"
    cleanup_dispatcher_alarm_name          = "${local.prefix}-cleanup-dispatcher-failed"
    cleanup_dispatcher_throttle_alarm_name = "${local.prefix}-cleanup-dispatcher-throttled"
    cleanup_scheduler_alarm_name           = "${local.prefix}-cleanup-scheduler-failed"
    cleanup_failure_topic_arn              = "arn:aws:sns:${local.region}:${local.account_id}:${local.prefix}-cleanup-failure"
    release_role_arn                       = aws_iam_role.github["release"].arn
    deploy_role_arn                        = aws_iam_role.github["deploy"].arn
    control_role_arn                       = aws_iam_role.github["control"].arn
    service_execution_role_arns            = { for name, role in aws_iam_role.execution : name => role.arn }
    service_task_role_arns                 = { for name, role in aws_iam_role.task : name => role.arn }
    secret_arns                            = { for name, secret in aws_secretsmanager_secret.application : name => secret.arn }
    state_bucket_name                      = var.state_bucket_name
    demo_state_key                         = local.demo_key
  }
}

output "account_id" {
  value = local.foundation_contract.account_id
}
output "region" {
  value = local.foundation_contract.region
}
output "cluster_arn" {
  value = local.foundation_contract.cluster_arn
}
output "repository_url" {
  value = local.foundation_contract.repository_url
}
output "control_table_name" {
  value = local.foundation_contract.control_table_name
}
output "artifact_bucket_name" {
  value = local.foundation_contract.artifact_bucket_name
}
output "cleanup_project_name" {
  value = local.foundation_contract.cleanup_project_name
}
output "cleanup_schedule_name" {
  value = local.foundation_contract.cleanup_schedule_name
}
output "cleanup_schedule_group_name" {
  value = local.foundation_contract.cleanup_schedule_group_name
}
output "cleanup_alarm_name" {
  value = local.foundation_contract.cleanup_alarm_name
}
output "cleanup_role_arn" {
  value = local.foundation_contract.cleanup_role_arn
}
output "cleanup_dispatcher_arn" {
  value = local.foundation_contract.cleanup_dispatcher_arn
}
output "cleanup_dispatcher_role_arn" {
  value = local.foundation_contract.cleanup_dispatcher_role_arn
}
output "cleanup_dispatcher_alarm_name" {
  value = local.foundation_contract.cleanup_dispatcher_alarm_name
}
output "cleanup_dispatcher_throttle_alarm_name" {
  value = local.foundation_contract.cleanup_dispatcher_throttle_alarm_name
}
output "cleanup_scheduler_alarm_name" {
  value = local.foundation_contract.cleanup_scheduler_alarm_name
}
output "cleanup_failure_topic_arn" {
  value = local.foundation_contract.cleanup_failure_topic_arn
}
output "release_role_arn" {
  value = local.foundation_contract.release_role_arn
}
output "deploy_role_arn" {
  value = local.foundation_contract.deploy_role_arn
}
output "control_role_arn" {
  value = local.foundation_contract.control_role_arn
}
output "service_execution_role_arns" {
  value = local.foundation_contract.service_execution_role_arns
}
output "service_task_role_arns" {
  value = local.foundation_contract.service_task_role_arns
}
output "secret_arns" {
  value = local.foundation_contract.secret_arns
}
output "state_bucket_name" {
  value = local.foundation_contract.state_bucket_name
}
output "demo_state_key" {
  value = local.foundation_contract.demo_state_key
}