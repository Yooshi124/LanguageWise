output "url" {
  value = "https://${aws_cloudfront_distribution.gateway.domain_name}"
}

output "initializer_task_definition_arn" {
  value = aws_ecs_task_definition.initializer.arn
}

output "subnet_ids" {
  value = values(aws_subnet.tasks)[*].id
}

output "initializer_security_group_ids" {
  value = [aws_security_group.task["database-initializer"].id]
}

output "service_names" {
  value = [for name in sort(tolist(local.names)) : "languagewise-demo-${name}"]
}

output "rds_identifier" {
  value = aws_db_instance.demo.identifier
}

output "vpc_id" {
  value = aws_vpc.demo.id
}

output "rds_endpoint" {
  value = aws_db_instance.demo.address
}