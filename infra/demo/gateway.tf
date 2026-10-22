resource "aws_lb" "gateway" {
  name                       = "languagewise-demo-gateway"
  internal                   = true
  load_balancer_type         = "application"
  subnets                    = values(aws_subnet.private)[*].id
  security_groups            = concat([aws_security_group.alb.id], var.services_enabled ? [] : [aws_security_group.cloudfront_bootstrap.id])
  drop_invalid_header_fields = true
  idle_timeout               = 180
}

resource "aws_lb_target_group" "gateway" {
  name        = "languagewise-demo-gateway"
  port        = 80
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = aws_vpc.demo.id
  health_check {
    path                = "/health"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
    interval            = 30
  }
  deregistration_delay = 30
}

resource "aws_lb_listener" "gateway" {
  load_balancer_arn = aws_lb.gateway.arn
  port              = 80
  protocol          = "HTTP"
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.gateway.arn
  }
}

resource "aws_cloudfront_vpc_origin" "gateway" {
  vpc_origin_endpoint_config {
    name                   = "languagewise-demo"
    arn                    = aws_lb.gateway.arn
    http_port              = 80
    https_port             = 443
    origin_protocol_policy = "http-only"
    origin_ssl_protocols {
      items    = ["TLSv1.2"]
      quantity = 1
    }
  }
  depends_on = [aws_internet_gateway.demo, aws_lb_listener.gateway, aws_vpc_security_group_ingress_rule.cloudfront_bootstrap]
}

data "aws_security_group" "cloudfront_managed" {
  filter {
    name   = "group-name"
    values = ["CloudFront-VPCOrigins-Service-SG"]
  }
  vpc_id     = aws_vpc.demo.id
  depends_on = [aws_cloudfront_vpc_origin.gateway]
}

resource "aws_vpc_security_group_ingress_rule" "cloudfront_managed" {
  security_group_id            = aws_security_group.alb.id
  referenced_security_group_id = data.aws_security_group.cloudfront_managed.id
  ip_protocol                  = "tcp"
  from_port                    = 80
  to_port                      = 80
}

resource "aws_cloudfront_distribution" "gateway" {
  enabled             = true
  wait_for_deployment = true
  comment             = "Disposable LanguageWise demo"
  http_version        = "http2"
  origin {
    domain_name = aws_lb.gateway.dns_name
    origin_id   = "gateway"
    vpc_origin_config {
      vpc_origin_id            = aws_cloudfront_vpc_origin.gateway.id
      origin_read_timeout      = 60
      origin_keepalive_timeout = 5
    }
  }
  default_cache_behavior {
    target_origin_id       = "gateway"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD"]
    compress               = false
    min_ttl                = 0
    default_ttl            = 0
    max_ttl                = 0
    forwarded_values {
      query_string = true
      headers      = ["*"]
      cookies {
        forward = "all"
      }
    }
  }
  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }
  viewer_certificate {
    cloudfront_default_certificate = true
  }
  depends_on = [aws_vpc_security_group_ingress_rule.cloudfront_managed]
}