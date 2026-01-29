# API Gateway HTTP API
resource "aws_apigatewayv2_api" "main" {
  name          = "${var.project_name}-api-${var.environment}"
  protocol_type = "HTTP"
  description   = "ResumeRank API Gateway for Lambda functions"

  cors_configuration {
    allow_headers = ["Content-Type", "Authorization", "X-Amz-Date", "X-Api-Key"]
    allow_methods = ["GET", "POST", "OPTIONS"]
    allow_origins = ["*"] # In production, restrict to your domain
    max_age       = 300
  }

  tags = {
    Name = "${var.project_name}-api"
  }
}

# API Gateway stage
resource "aws_apigatewayv2_stage" "main" {
  api_id      = aws_apigatewayv2_api.main.id
  name        = var.environment
  auto_deploy = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      routeKey       = "$context.routeKey"
      status         = "$context.status"
      responseLength = "$context.responseLength"
      errorMessage   = "$context.error.message"
    })
  }

  tags = {
    Name = "${var.project_name}-api-stage"
  }
}

# CloudWatch Log Group for API Gateway
resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apigateway/${var.project_name}-${var.environment}"
  retention_in_days = 14
}

# Lambda integration for Resume Parser
resource "aws_apigatewayv2_integration" "resume_parser" {
  api_id                 = aws_apigatewayv2_api.main.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.resume_parser.invoke_arn
  integration_method     = "POST"
  payload_format_version = "2.0"
}

# Lambda integration for Ranking Agent
resource "aws_apigatewayv2_integration" "ranking_agent" {
  api_id                 = aws_apigatewayv2_api.main.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.ranking_agent.invoke_arn
  integration_method     = "POST"
  payload_format_version = "2.0"
}

# Route for /parse endpoint
resource "aws_apigatewayv2_route" "parse" {
  api_id    = aws_apigatewayv2_api.main.id
  route_key = "POST /parse"
  target    = "integrations/${aws_apigatewayv2_integration.resume_parser.id}"
}

# Route for /rank endpoint
resource "aws_apigatewayv2_route" "rank" {
  api_id    = aws_apigatewayv2_api.main.id
  route_key = "POST /rank"
  target    = "integrations/${aws_apigatewayv2_integration.ranking_agent.id}"
}

# Health check route for Resume Parser
resource "aws_apigatewayv2_route" "parse_health" {
  api_id    = aws_apigatewayv2_api.main.id
  route_key = "GET /parse/health"
  target    = "integrations/${aws_apigatewayv2_integration.resume_parser.id}"
}

# Health check route for Ranking Agent
resource "aws_apigatewayv2_route" "rank_health" {
  api_id    = aws_apigatewayv2_api.main.id
  route_key = "GET /rank/health"
  target    = "integrations/${aws_apigatewayv2_integration.ranking_agent.id}"
}
