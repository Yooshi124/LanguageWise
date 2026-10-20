locals {
  regional = { StringEquals = { "aws:RequestedRegion" = local.region } }
  demo_tagged = { StringEquals = {
    "aws:RequestedRegion"     = local.region
    "aws:ResourceTag/Project" = "languagewise"
    "aws:ResourceTag/Layer"   = "demo"
  } }
  demo_creation = { StringEquals = {
    "aws:RequestedRegion"    = local.region
    "aws:RequestTag/Project" = "languagewise"
    "aws:RequestTag/Layer"   = "demo"
  } }
  state_arn    = "arn:aws:s3:::${var.state_bucket_name}"
  demo_objects = ["${local.state_arn}/${local.demo_key}", "${local.state_arn}/${local.demo_key}.tflock"]
  elb_arns = [
    "arn:aws:elasticloadbalancing:${local.region}:${local.account_id}:loadbalancer/app/${local.prefix}-*/*",
    "arn:aws:elasticloadbalancing:${local.region}:${local.account_id}:targetgroup/${local.prefix}-*/*",
    "arn:aws:elasticloadbalancing:${local.region}:${local.account_id}:listener/app/${local.prefix}-*/*/*",
    "arn:aws:elasticloadbalancing:${local.region}:${local.account_id}:listener-rule/app/${local.prefix}-*/*/*/*"
  ]
  rds_arns = [
    "arn:aws:rds:${local.region}:${local.account_id}:db:${local.prefix}-*",
    "arn:aws:rds:${local.region}:${local.account_id}:subgrp:${local.prefix}-*",
    "arn:aws:rds:${local.region}:${local.account_id}:pg:${local.prefix}-*",
    "arn:aws:rds:${local.region}:${local.account_id}:auto-backup:*"
  ]
  control_statements = [
    {
      Sid      = "ControlRecord", Effect = "Allow"
      Action   = ["dynamodb:DescribeTable", "dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:UpdateItem", "dynamodb:DeleteItem", "dynamodb:ConditionCheckItem"]
      Resource = aws_dynamodb_table.control.arn, Condition = local.regional
    },
    {
      Sid      = "ReadReleaseManifest", Effect = "Allow", Action = ["s3:GetObject", "s3:GetObjectVersion"]
      Resource = "${data.aws_s3_bucket.artifacts.arn}/releases/*", Condition = local.regional
    },
    {
      Sid      = "ArtifactLocation", Effect = "Allow", Action = ["s3:GetBucketLocation", "s3:ListBucket"]
      Resource = data.aws_s3_bucket.artifacts.arn, Condition = local.regional
    },
    {
      Sid       = "CleanupReadiness", Effect = "Allow", Action = ["codebuild:BatchGetProjects", "codebuild:ListBuildsForProject"]
      Resource  = "arn:aws:codebuild:${local.region}:${local.account_id}:project/${local.foundation_contract.cleanup_project_name}"
      Condition = local.regional
    },
    {
      Sid       = "ScheduleReadiness", Effect = "Allow", Action = ["scheduler:GetSchedule"]
      Resource  = "arn:aws:scheduler:${local.region}:${local.account_id}:schedule/${local.foundation_contract.cleanup_schedule_group_name}/${local.foundation_contract.cleanup_schedule_name}"
      Condition = local.regional
    },
    {
      Sid = "AlarmReadiness", Effect = "Allow", Action = ["cloudwatch:DescribeAlarms"]
      Resource = [for name in [local.foundation_contract.cleanup_alarm_name, local.foundation_contract.cleanup_scheduler_alarm_name,
        local.foundation_contract.cleanup_dispatcher_alarm_name, local.foundation_contract.cleanup_dispatcher_throttle_alarm_name
      ] : "arn:aws:cloudwatch:${local.region}:${local.account_id}:alarm:${name}"]
      Condition = local.regional
    },
    {
      Sid      = "DispatcherReadiness", Effect = "Allow", Action = ["lambda:GetFunctionConfiguration"]
      Resource = local.foundation_contract.cleanup_dispatcher_arn, Condition = local.regional
    },
    {
      Sid = "CleanupTrustReadiness", Effect = "Allow", Action = ["iam:GetRole"]
      Resource = concat([local.foundation_contract.cleanup_role_arn, local.foundation_contract.cleanup_dispatcher_role_arn,
      "arn:aws:iam::${local.account_id}:role/languagewise/${local.prefix}-scheduler"], [for role in aws_iam_role.github : role.arn])
    },
    {
      Sid       = "CleanupSourceReadiness", Effect = "Allow", Action = ["s3:GetObjectVersion"]
      Resource  = "${data.aws_s3_bucket.artifacts.arn}/${var.cleanup_bundle_key}"
      Condition = { StringEquals = { "s3:VersionId" = var.cleanup_bundle_version, "aws:RequestedRegion" = local.region } }
    },
    {
      Sid      = "CleanupNotificationReadiness", Effect = "Allow", Action = ["sns:GetTopicAttributes"]
      Resource = local.foundation_contract.cleanup_failure_topic_arn, Condition = local.regional
    },
    {
      Sid       = "CleanupBuildIdentity", Effect = "Allow", Action = ["codebuild:BatchGetBuilds"]
      Resource  = "arn:aws:codebuild:${local.region}:${local.account_id}:project/${local.foundation_contract.cleanup_project_name}"
      Condition = local.regional
    },
    {
      Sid      = "ResidualInventory", Effect = "Allow", Action = ["tag:GetResources"]
      Resource = "*", Condition = local.regional
    },
    {
      Sid      = "ApplicationSecretMetadata", Effect = "Allow", Action = ["secretsmanager:DescribeSecret"]
      Resource = [for secret in aws_secretsmanager_secret.application : secret.arn], Condition = local.regional
    }
  ]
  state_statements = [
    {
      Sid       = "DemoStateList", Effect = "Allow", Action = ["s3:ListBucket"], Resource = local.state_arn
      Condition = local.regional
    },
    {
      Sid       = "StateLocation", Effect = "Allow", Action = ["s3:GetBucketLocation"], Resource = local.state_arn
      Condition = local.regional
    },
    {
      Sid      = "DemoStateReadWrite", Effect = "Allow", Action = ["s3:GetObject", "s3:PutObject"]
      Resource = local.demo_objects, Condition = local.regional
    },
    {
      Sid      = "ReleaseNativeLock", Effect = "Allow", Action = ["s3:DeleteObject"]
      Resource = "${local.state_arn}/${local.demo_key}.tflock", Condition = local.regional
    },
    {
      Sid      = "NeverAccessAdministrativeState", Effect = "Deny", Action = ["s3:*Object*"]
      Resource = ["${local.state_arn}/foundation/*", "${local.state_arn}/bootstrap/*"]
    }
  ]
  runtime_statements = [
    {
      Sid    = "RegionalInventory", Effect = "Allow", Resource = "*", Condition = local.regional
      Action = ["ec2:Describe*", "rds:Describe*", "elasticloadbalancing:Describe*", "servicediscovery:ListNamespaces", "servicediscovery:ListServices", "ecs:ListTaskDefinitions", "ecs:DescribeTaskDefinition", "ecs:ListTasks", "ecs:ListServices", "logs:DescribeLogGroups"]
    },
    {
      Sid = "CreateTaggedNetwork", Effect = "Allow", Condition = local.demo_creation
      Resource = [for kind in ["vpc", "subnet", "internet-gateway", "route-table", "security-group"] :
      "arn:aws:ec2:${local.region}:${local.account_id}:${kind}/*"]
      Action = ["ec2:CreateVpc", "ec2:CreateSubnet", "ec2:CreateInternetGateway", "ec2:CreateRouteTable", "ec2:CreateSecurityGroup"]
    },
    {
      Sid      = "CreateWithinTaggedVpc", Effect = "Allow", Condition = local.demo_tagged
      Resource = "arn:aws:ec2:${local.region}:${local.account_id}:vpc/*"
      Action   = ["ec2:CreateSubnet", "ec2:CreateRouteTable", "ec2:CreateSecurityGroup"]
    },
    {
      Sid      = "AuthorizeTaggedSecurityGroup", Effect = "Allow", Condition = local.demo_tagged
      Resource = "arn:aws:ec2:${local.region}:${local.account_id}:security-group/*"
      Action   = ["ec2:AuthorizeSecurityGroupIngress", "ec2:AuthorizeSecurityGroupEgress"]
    },
    {
      Sid      = "CreateTaggedSecurityGroupRule", Effect = "Allow", Condition = local.demo_creation
      Resource = "arn:aws:ec2:${local.region}:${local.account_id}:security-group-rule/*"
      Action   = ["ec2:AuthorizeSecurityGroupIngress", "ec2:AuthorizeSecurityGroupEgress"]
    },
    {
      Sid    = "TagNetworkAtCreation", Effect = "Allow", Resource = "arn:aws:ec2:${local.region}:${local.account_id}:*/*"
      Action = ["ec2:CreateTags"]
      Condition = {
        StringEquals = {
          "aws:RequestedRegion" = local.region, "aws:RequestTag/Project" = "languagewise", "aws:RequestTag/Layer" = "demo"
          "ec2:CreateAction"    = ["CreateVpc", "CreateSubnet", "CreateInternetGateway", "CreateRouteTable", "CreateSecurityGroup", "AuthorizeSecurityGroupIngress", "AuthorizeSecurityGroupEgress"]
        }
      }
    },
    {
      Sid    = "MutateTaggedNetwork", Effect = "Allow", Resource = "arn:aws:ec2:${local.region}:${local.account_id}:*/*", Condition = local.demo_tagged
      Action = ["ec2:DeleteVpc", "ec2:ModifyVpcAttribute", "ec2:DeleteSubnet", "ec2:ModifySubnetAttribute", "ec2:DeleteInternetGateway", "ec2:AttachInternetGateway", "ec2:DetachInternetGateway", "ec2:DeleteRouteTable", "ec2:CreateRoute", "ec2:DeleteRoute", "ec2:AssociateRouteTable", "ec2:DisassociateRouteTable", "ec2:ReplaceRouteTableAssociation", "ec2:DeleteSecurityGroup", "ec2:RevokeSecurityGroupIngress", "ec2:RevokeSecurityGroupEgress", "ec2:DeleteTags", "ec2:CreateTags"]
    },
    {
      Sid    = "DemoDatabase", Effect = "Allow", Resource = local.rds_arns, Condition = local.regional
      Action = ["rds:CreateDBInstance", "rds:ModifyDBInstance", "rds:DeleteDBInstance", "rds:CreateDBSubnetGroup", "rds:ModifyDBSubnetGroup", "rds:DeleteDBSubnetGroup", "rds:CreateDBParameterGroup", "rds:ModifyDBParameterGroup", "rds:DeleteDBParameterGroup", "rds:AddTagsToResource", "rds:RemoveTagsFromResource", "rds:ListTagsForResource"]
    },
    {
      Sid      = "RdsManagedKeyMetadata", Effect = "Allow", Condition = local.regional
      Action   = ["kms:DescribeKey"]
      Resource = "arn:aws:kms:${local.region}:${local.account_id}:key/*"
    },
    {
      Sid      = "RdsManagedSecretMetadata", Effect = "Allow", Condition = local.regional
      Action   = ["secretsmanager:CreateSecret", "secretsmanager:TagResource", "secretsmanager:DescribeSecret", "secretsmanager:GetResourcePolicy"]
      Resource = "arn:aws:secretsmanager:${local.region}:${local.account_id}:secret:rds!db-*"
    },
    {
      Sid    = "DemoBalancer", Effect = "Allow", Resource = local.elb_arns, Condition = local.regional
      Action = ["elasticloadbalancing:CreateLoadBalancer", "elasticloadbalancing:DeleteLoadBalancer", "elasticloadbalancing:ModifyLoadBalancerAttributes", "elasticloadbalancing:SetSecurityGroups", "elasticloadbalancing:SetSubnets", "elasticloadbalancing:CreateTargetGroup", "elasticloadbalancing:DeleteTargetGroup", "elasticloadbalancing:ModifyTargetGroup", "elasticloadbalancing:ModifyTargetGroupAttributes", "elasticloadbalancing:CreateListener", "elasticloadbalancing:ModifyListener", "elasticloadbalancing:DeleteListener", "elasticloadbalancing:AddTags", "elasticloadbalancing:RemoveTags", "elasticloadbalancing:RegisterTargets", "elasticloadbalancing:DeregisterTargets"]
    },
    {
      Sid      = "DiscoveryRead", Effect = "Allow", Condition = local.regional
      Action   = ["servicediscovery:GetNamespace", "servicediscovery:GetService", "servicediscovery:GetOperation", "servicediscovery:ListTagsForResource", "servicediscovery:ListInstances"]
      Resource = "*"
    },
    {
      Sid    = "CreateTaggedDiscovery", Effect = "Allow", Resource = "*", Condition = local.demo_creation
      Action = ["servicediscovery:CreatePrivateDnsNamespace", "servicediscovery:CreateService"]
    },
    {
      Sid      = "MutateTaggedDiscovery", Effect = "Allow", Condition = local.demo_tagged
      Action   = ["servicediscovery:DeleteNamespace", "servicediscovery:DeleteService", "servicediscovery:UpdateService", "servicediscovery:TagResource", "servicediscovery:UntagResource"]
      Resource = ["arn:aws:servicediscovery:${local.region}:${local.account_id}:namespace/*", "arn:aws:servicediscovery:${local.region}:${local.account_id}:service/*"]
    },
    {
      Sid      = "EcsRegisterDefinitions", Effect = "Allow", Condition = local.demo_creation
      Resource = "arn:aws:ecs:${local.region}:${local.account_id}:task-definition/${local.prefix}-*:*"
      Action   = ["ecs:RegisterTaskDefinition"]
    },
    {
      Sid      = "EcsDefinitions", Effect = "Allow", Condition = local.regional
      Action   = ["ecs:TagResource", "ecs:UntagResource", "ecs:ListTagsForResource"]
      Resource = "arn:aws:ecs:${local.region}:${local.account_id}:task-definition/${local.prefix}-*:*"
    },
    {
      Sid      = "EcsServices", Effect = "Allow", Condition = local.regional
      Action   = ["ecs:CreateService", "ecs:UpdateService", "ecs:DeleteService", "ecs:DescribeServices", "ecs:TagResource", "ecs:UntagResource", "ecs:ListTagsForResource"]
      Resource = "arn:aws:ecs:${local.region}:${local.account_id}:service/languagewise/${local.prefix}-*"
    },
    {
      Sid       = "EcsRunInitializer", Effect = "Allow", Action = ["ecs:RunTask"]
      Resource  = "arn:aws:ecs:${local.region}:${local.account_id}:task-definition/${local.prefix}-database-initializer:*"
      Condition = { StringEquals = { "aws:RequestedRegion" = local.region }, ArnEquals = { "ecs:cluster" = aws_ecs_cluster.existing.arn } }
    },
    {
      Sid      = "EcsTasks", Effect = "Allow", Action = ["ecs:DescribeTasks", "ecs:StopTask", "ecs:ListTagsForResource", "ecs:TagResource"]
      Resource = "arn:aws:ecs:${local.region}:${local.account_id}:task/languagewise/*", Condition = local.regional
    },
    {
      Sid = "EcsClusterRead", Effect = "Allow", Action = ["ecs:DescribeClusters"], Resource = aws_ecs_cluster.existing.arn, Condition = local.regional
    },
    {
      Sid       = "ExactEcsPassRole", Effect = "Allow", Action = ["iam:PassRole"]
      Resource  = concat([for role in aws_iam_role.execution : role.arn], [for role in aws_iam_role.task : role.arn])
      Condition = { StringEquals = { "iam:PassedToService" = "ecs-tasks.amazonaws.com", "aws:RequestedRegion" = local.region } }
    },
    {
      Sid      = "DemoLogs", Effect = "Allow", Condition = local.regional
      Action   = ["logs:CreateLogGroup", "logs:DeleteLogGroup", "logs:PutRetentionPolicy", "logs:DeleteRetentionPolicy", "logs:ListTagsForResource", "logs:TagResource", "logs:UntagResource", "logs:DescribeLogStreams", "logs:GetLogEvents", "logs:FilterLogEvents"]
      Resource = ["arn:aws:logs:${local.region}:${local.account_id}:log-group:/languagewise/demo/*", "arn:aws:logs:${local.region}:${local.account_id}:log-group:/languagewise/demo/*:*"]
    },
    {
      Sid    = "CloudFrontInventory", Effect = "Allow", Resource = "*"
      Action = ["cloudfront:ListDistributions", "cloudfront:ListVpcOrigins", "cloudfront:ListCachePolicies", "cloudfront:ListOriginRequestPolicies", "cloudfront:GetCachePolicy", "cloudfront:GetOriginRequestPolicy"]
    },
    {
      Sid      = "CloudFrontRead", Effect = "Allow"
      Action   = ["cloudfront:GetDistribution", "cloudfront:GetDistributionConfig", "cloudfront:GetVpcOrigin", "cloudfront:ListTagsForResource"]
      Resource = ["arn:aws:cloudfront::${local.account_id}:distribution/*", "arn:aws:cloudfront::${local.account_id}:vpcorigin/*"]
    },
    {
      Sid       = "CreateTaggedCloudFront", Effect = "Allow"
      Action    = ["cloudfront:CreateDistribution", "cloudfront:CreateVpcOrigin"]
      Resource  = "*"
      Condition = { StringEquals = { "aws:RequestTag/Project" = "languagewise", "aws:RequestTag/Layer" = "demo" } }
    },
    {
      Sid      = "CloudFrontInitialOwnershipTags", Effect = "Allow", Action = ["cloudfront:TagResource"]
      Resource = ["arn:aws:cloudfront::${local.account_id}:distribution/*", "arn:aws:cloudfront::${local.account_id}:vpcorigin/*"]
      Condition = {
        StringEquals                = { "aws:RequestTag/Project" = "languagewise", "aws:RequestTag/Layer" = "demo" }
        Null                        = { "aws:ResourceTag/Project" = "true", "aws:ResourceTag/Layer" = "true" }
        "ForAllValues:StringEquals" = { "aws:TagKeys" = ["Project", "Layer", "Lifecycle"] }
      }
    },
    {
      Sid       = "MutateTaggedCloudFront", Effect = "Allow"
      Action    = ["cloudfront:UpdateDistribution", "cloudfront:DeleteDistribution", "cloudfront:UpdateVpcOrigin", "cloudfront:DeleteVpcOrigin", "cloudfront:TagResource", "cloudfront:UntagResource"]
      Resource  = ["arn:aws:cloudfront::${local.account_id}:distribution/*", "arn:aws:cloudfront::${local.account_id}:vpcorigin/*"]
      Condition = { StringEquals = { "aws:ResourceTag/Project" = "languagewise", "aws:ResourceTag/Layer" = "demo" } }
    },
    {
      Sid      = "AwsManagedPrefixListEntries", Effect = "Allow", Condition = local.regional
      Action   = ["ec2:GetManagedPrefixListEntries"]
      Resource = "arn:aws:ec2:${local.region}:aws:prefix-list/*"
    },
    {
      Sid      = "DiscoveryInitialOwnershipTags", Effect = "Allow"
      Action   = ["servicediscovery:TagResource"]
      Resource = "arn:aws:servicediscovery:${local.region}:${local.account_id}:*/*"
      Condition = merge(local.demo_creation, {
        Null                        = { "aws:ResourceTag/Project" = "true" }
        "ForAllValues:StringEquals" = { "aws:TagKeys" = ["Project", "Layer", "Lifecycle"] }
      })
    }
  ]
  image_read_statements = [
    { Effect = "Allow", Action = ["ecr:GetAuthorizationToken"], Resource = "*", Condition = local.regional },
    {
      Effect   = "Allow", Action = ["ecr:DescribeRepositories", "ecr:DescribeImages", "ecr:BatchGetImage", "ecr:GetDownloadUrlForLayer", "ecr:BatchCheckLayerAvailability"]
      Resource = aws_ecr_repository.existing.arn, Condition = local.regional
    }
  ]
  release_write_statements = [
    {
      Effect   = "Allow", Action = ["ecr:InitiateLayerUpload", "ecr:UploadLayerPart", "ecr:CompleteLayerUpload", "ecr:PutImage"]
      Resource = aws_ecr_repository.existing.arn, Condition = local.regional
    },
    {
      Effect = "Allow", Action = ["s3:PutObject"], Resource = "${data.aws_s3_bucket.artifacts.arn}/releases/*", Condition = local.regional
    }
  ]
  runtime_policy_parts = merge(
    { pass-role = [for statement in local.runtime_statements : statement if statement.Sid == "ExactEcsPassRole"] },
    { for index, indices in chunklist([for index, statement in local.runtime_statements : index if statement.Sid != "ExactEcsPassRole"], 5) : "resources-${index}" => [for statement_index in indices : local.runtime_statements[statement_index]] }
  )
  cleanup_policy_parts = {
    for name, statements in local.runtime_policy_parts : name => [
      for statement in statements : merge(statement, {
        Action = [for action in statement.Action : action if !can(regex(":(Create|Register|RunTask)", action)) && !(statement.Sid == "RdsManagedSecretMetadata" && !contains(["secretsmanager:DescribeSecret", "secretsmanager:GetResourcePolicy"], action))]
      }) if !contains(["CloudFrontInitialOwnershipTags", "DiscoveryInitialOwnershipTags"], statement.Sid) && length([for action in statement.Action : action if !can(regex(":(Create|Register|RunTask)", action)) && !(statement.Sid == "RdsManagedSecretMetadata" && !contains(["secretsmanager:DescribeSecret", "secretsmanager:GetResourcePolicy"], action))]) > 0
    ]
  }
}

resource "aws_iam_role_policy" "control" {
  for_each = aws_iam_role.github
  name     = "ControlAndReleaseAccess"
  role     = each.value.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = concat(local.control_statements, local.image_read_statements,
    each.key == "control" ? [] : local.release_write_statements)
  })
}

resource "aws_iam_policy" "runtime" {
  for_each = local.runtime_policy_parts
  name     = "languagewise-demo-${each.key}"
  path     = "/languagewise/"
  policy = jsonencode({
    Version   = "2012-10-17"
    Statement = each.value
  })
  lifecycle {
    precondition {
      condition     = length(jsonencode({ Version = "2012-10-17", Statement = each.value })) <= 6144
      error_message = "IAM managed policy exceeds the 6144-character limit; split the resource statements further."
    }
  }
}

resource "aws_iam_role_policy_attachment" "runtime" {
  for_each   = { for pair in setproduct(["deploy", "control"], keys(local.runtime_policy_parts)) : "${pair[0]}-${pair[1]}" => pair }
  role       = aws_iam_role.github[each.value[0]].name
  policy_arn = aws_iam_policy.runtime[each.value[1]].arn
}

resource "aws_iam_policy" "cleanup_runtime" {
  for_each = local.cleanup_policy_parts
  name     = "languagewise-cleanup-${each.key}"
  path     = "/languagewise/"
  policy = jsonencode({
    Version   = "2012-10-17"
    Statement = each.value
  })
  lifecycle {
    precondition {
      condition     = length(jsonencode({ Version = "2012-10-17", Statement = each.value })) <= 6144
      error_message = "Cleanup managed policy exceeds the 6144-character IAM limit."
    }
  }
}

resource "aws_iam_role_policy" "state" {
  for_each = toset(["deploy", "control"])
  name     = "DemoStateOnly"
  role     = aws_iam_role.github[each.key].id
  policy   = jsonencode({ Version = "2012-10-17", Statement = local.state_statements })
}