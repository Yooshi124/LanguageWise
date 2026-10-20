locals {
  account_id = "047754151573"
  region     = "ap-southeast-2"
  prefix     = "languagewise-demo"
  user_arn   = "arn:aws:iam::${local.account_id}:user/languagewise/languagewise-runner"
  demo_key   = "demo/terraform.tfstate"
  services = toset([
    "shared-db-api", "shared-backend", "shared-frontend",
    "garry-ai-service-backend",
    "mini-games-service-db", "mini-games-service-backend", "mini-games-service-frontend",
    "quizzes-courses-service-db", "quizzes-courses-service-backend", "quizzes-courses-service-frontend",
    "chat-discussion-service-db", "chat-discussion-service-backend", "chat-discussion-service-frontend",
    "quests-achievements-notifications-service-db", "quests-achievements-notifications-service-backend",
    "quests-achievements-notifications-service-frontend",
    "leaderboard-analytics-service-backend", "leaderboard-analytics-service-frontend",
    "database-initializer"
  ])
  secret_consumers = {
    shared-db-api                                     = ["db-shared"]
    mini-games-service-db                             = ["db-mini-games"]
    quizzes-courses-service-db                        = ["db-quizzes-courses"]
    chat-discussion-service-db                        = ["db-chat-discussion"]
    quests-achievements-notifications-service-db      = ["db-quests"]
    shared-backend                                    = ["signing-private-key"]
    garry-ai-service-backend                          = ["signing-public-key", "openrouter-api-key"]
    mini-games-service-backend                        = ["signing-public-key"]
    quizzes-courses-service-backend                   = ["signing-public-key"]
    chat-discussion-service-backend                   = ["signing-public-key"]
    quests-achievements-notifications-service-backend = ["signing-public-key", "openrouter-api-key"]
    leaderboard-analytics-service-backend             = ["signing-public-key", "openrouter-api-key"]
  }
  database_secrets = toset(["db-shared", "db-mini-games", "db-quizzes-courses", "db-chat-discussion", "db-quests"])
  secret_names     = setunion(local.database_secrets, toset(["openrouter-api-key", "signing-private-key", "signing-public-key"]))
}

import {
  to = aws_ecs_cluster.existing
  id = "languagewise"
}

resource "aws_ecs_cluster" "existing" {
  name = "languagewise"
  setting {
    name  = "containerInsights"
    value = "disabled"
  }
  lifecycle {
    prevent_destroy = true
  }
}

import {
  to = aws_ecr_repository.existing
  id = "41026release2/languagewise"
}

resource "aws_ecr_repository" "existing" {
  name                 = "41026release2/languagewise"
  image_tag_mutability = "MUTABLE"
  force_delete         = false
  encryption_configuration {
    encryption_type = "AES256"
  }
  image_scanning_configuration {
    scan_on_push = false
  }
  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_ecr_lifecycle_policy" "existing" {
  repository = aws_ecr_repository.existing.name
  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Expire only untagged images after one day"
      selection = {
        tagStatus   = "untagged"
        countType   = "sinceImagePushed"
        countUnit   = "days"
        countNumber = 1
      }
      action = { type = "expire" }
    }]
  })
}

import {
  to = aws_iam_user.runner
  id = "languagewise-runner"
}

resource "aws_iam_user" "runner" {
  name          = "languagewise-runner"
  path          = "/languagewise/"
  force_destroy = false
  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_dynamodb_table" "control" {
  name         = "${local.prefix}-control"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "id"
  attribute {
    name = "id"
    type = "S"
  }
  server_side_encryption {
    enabled = true
  }
  point_in_time_recovery {
    enabled = false
  }
  deletion_protection_enabled = true
  lifecycle {
    prevent_destroy = true
  }
}

data "aws_s3_bucket" "artifacts" {
  bucket = "languagewise-${local.account_id}-${local.region}-artifacts"
}

resource "aws_secretsmanager_secret" "application" {
  for_each                = local.secret_names
  name                    = "languagewise/demo/${each.key}"
  recovery_window_in_days = 30
  lifecycle {
    prevent_destroy = true
  }
}