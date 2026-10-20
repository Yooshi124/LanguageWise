locals {
  zones = { a = "${var.foundation.region}a", b = "${var.foundation.region}b" }
}

resource "aws_vpc" "demo" {
  cidr_block           = "10.82.0.0/16"
  enable_dns_support   = true
  enable_dns_hostnames = true
  tags                 = { Name = "languagewise-demo" }
}

resource "aws_internet_gateway" "demo" {
  vpc_id = aws_vpc.demo.id
}

resource "aws_subnet" "tasks" {
  for_each          = local.zones
  vpc_id            = aws_vpc.demo.id
  availability_zone = each.value
  cidr_block        = each.key == "a" ? "10.82.0.0/24" : "10.82.1.0/24"
  tags              = { Name = "languagewise-demo-tasks-${each.key}" }
}

resource "aws_subnet" "private" {
  for_each          = local.zones
  vpc_id            = aws_vpc.demo.id
  availability_zone = each.value
  cidr_block        = each.key == "a" ? "10.82.10.0/24" : "10.82.11.0/24"
  tags              = { Name = "languagewise-demo-private-${each.key}" }
}

resource "aws_route_table" "tasks" {
  vpc_id = aws_vpc.demo.id
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.demo.id
  }
}

resource "aws_route_table_association" "tasks" {
  for_each       = aws_subnet.tasks
  subnet_id      = each.value.id
  route_table_id = aws_route_table.tasks.id
}

resource "aws_route_table" "private" {
  vpc_id = aws_vpc.demo.id
}

resource "aws_route_table_association" "private" {
  for_each       = aws_subnet.private
  subnet_id      = each.value.id
  route_table_id = aws_route_table.private.id
}

resource "aws_security_group" "task" {
  for_each    = setunion(local.names, toset(["database-initializer"]))
  name_prefix = "lw-demo-${substr(each.key, 0, 40)}-"
  vpc_id      = aws_vpc.demo.id
}

resource "aws_vpc_security_group_egress_rule" "tasks" {
  for_each          = aws_security_group.task
  security_group_id = each.value.id
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
}

resource "aws_vpc_security_group_ingress_rule" "caller" {
  for_each                     = local.caller_edges
  security_group_id            = aws_security_group.task[each.value.target].id
  referenced_security_group_id = aws_security_group.task[each.value.caller].id
  ip_protocol                  = "tcp"
  from_port                    = local.ports[each.value.target]
  to_port                      = local.ports[each.value.target]
}

resource "aws_security_group" "database" {
  name_prefix = "languagewise-demo-rds-"
  vpc_id      = aws_vpc.demo.id
}

resource "aws_vpc_security_group_ingress_rule" "database" {
  for_each                     = setunion(toset(keys(local.database_secrets)), toset(["database-initializer"]))
  security_group_id            = aws_security_group.database.id
  referenced_security_group_id = aws_security_group.task[each.key].id
  ip_protocol                  = "tcp"
  from_port                    = 5432
  to_port                      = 5432
}

resource "aws_security_group" "alb" {
  name_prefix = "languagewise-demo-alb-"
  vpc_id      = aws_vpc.demo.id
}

resource "aws_security_group" "cloudfront_bootstrap" {
  name_prefix = "languagewise-demo-cloudfront-bootstrap-"
  vpc_id      = aws_vpc.demo.id
}

data "aws_ec2_managed_prefix_list" "cloudfront" {
  name = "com.amazonaws.global.cloudfront.origin-facing"
}

resource "aws_vpc_security_group_ingress_rule" "cloudfront_bootstrap" {
  security_group_id = aws_security_group.cloudfront_bootstrap.id
  prefix_list_id    = data.aws_ec2_managed_prefix_list.cloudfront.id
  ip_protocol       = "tcp"
  from_port         = 80
  to_port           = 80
}

resource "aws_vpc_security_group_egress_rule" "alb" {
  security_group_id            = aws_security_group.alb.id
  referenced_security_group_id = aws_security_group.task["shared-frontend"].id
  ip_protocol                  = "tcp"
  from_port                    = 80
  to_port                      = 80
}

resource "aws_vpc_security_group_ingress_rule" "gateway" {
  security_group_id            = aws_security_group.task["shared-frontend"].id
  referenced_security_group_id = aws_security_group.alb.id
  ip_protocol                  = "tcp"
  from_port                    = 80
  to_port                      = 80
}