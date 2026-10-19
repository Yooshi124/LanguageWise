resource "aws_db_subnet_group" "demo" {
  name       = "languagewise-demo-postgres"
  subnet_ids = values(aws_subnet.private)[*].id
}

resource "aws_db_parameter_group" "demo" {
  name_prefix = "languagewise-demo-"
  family      = "postgres18"
  parameter {
    name  = "rds.force_ssl"
    value = "1"
  }
}

resource "aws_db_instance" "demo" {
  identifier                   = "languagewise-demo-postgres"
  engine                       = "postgres"
  engine_version               = "18.6"
  instance_class               = "db.t4g.small"
  allocated_storage            = 20
  storage_type                 = "gp3"
  storage_encrypted            = true
  db_name                      = "postgres"
  username                     = "lw_admin"
  manage_master_user_password  = true
  db_subnet_group_name         = aws_db_subnet_group.demo.name
  parameter_group_name         = aws_db_parameter_group.demo.name
  vpc_security_group_ids       = [aws_security_group.database.id]
  publicly_accessible          = false
  multi_az                     = false
  backup_retention_period      = 0
  delete_automated_backups     = true
  skip_final_snapshot          = true
  deletion_protection          = false
  auto_minor_version_upgrade   = false
  performance_insights_enabled = false
  monitoring_interval          = 0
  apply_immediately            = true
  copy_tags_to_snapshot        = false
}

resource "aws_ecs_task_definition" "initializer" {
  family                   = "languagewise-demo-database-initializer"
  skip_destroy             = true
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = var.foundation.service_execution_role_arns["database-initializer"]
  task_role_arn            = var.foundation.service_task_role_arns["database-initializer"]
  runtime_platform {
    operating_system_family = "LINUX"
    cpu_architecture        = "X86_64"
  }
  container_definitions = jsonencode([{
    name      = "database-initializer"
    image     = var.release.images["database-initializer"]
    essential = true
    environment = [
      { name = "RDS_HOST", value = aws_db_instance.demo.address },
      { name = "AWS_DEFAULT_REGION", value = var.foundation.region },
      { name = "DATABASE_SECRET_ARNS", value = jsonencode({ for key in values(local.database_secrets) : key => var.foundation.secret_arns[key] }) }
    ]
    secrets = [{ name = "RDS_MASTER_SECRET", valueFrom = aws_db_instance.demo.master_user_secret[0].secret_arn }]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.task["database-initializer"].name
        awslogs-region        = var.foundation.region
        awslogs-stream-prefix = "initializer"
      }
    }
  }])
}