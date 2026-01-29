# Lambda layer for Python dependencies
resource "aws_lambda_layer_version" "dependencies" {
  filename            = "${path.module}/../dist/lambda-layer.zip"
  layer_name          = "${var.project_name}-dependencies-${var.environment}"
  compatible_runtimes = ["python3.11", "python3.12"]
  description         = "Python dependencies for ResumeRank Lambda functions"

  lifecycle {
    create_before_destroy = true
  }
}

# Resume Parser Lambda function
resource "aws_lambda_function" "resume_parser" {
  filename         = "${path.module}/../dist/resume-parser.zip"
  function_name    = "${var.project_name}-parser-${var.environment}"
  role             = aws_iam_role.lambda_execution.arn
  handler          = "handler.lambda_handler"
  runtime          = "python3.11"
  timeout          = var.lambda_timeout
  memory_size      = var.lambda_memory_size
  source_code_hash = filebase64sha256("${path.module}/../dist/resume-parser.zip")

  layers = [aws_lambda_layer_version.dependencies.arn]

  environment {
    variables = {
      S3_BUCKET_NAME   = aws_s3_bucket.resumes.id
      BEDROCK_MODEL_ID = var.bedrock_model_id
      ENVIRONMENT      = var.environment
    }
  }

  tags = {
    Name = "${var.project_name}-parser"
  }
}

# Ranking Agent Lambda function
resource "aws_lambda_function" "ranking_agent" {
  filename         = "${path.module}/../dist/ranking-agent.zip"
  function_name    = "${var.project_name}-ranking-${var.environment}"
  role             = aws_iam_role.lambda_execution.arn
  handler          = "handler.lambda_handler"
  runtime          = "python3.11"
  timeout          = var.lambda_timeout
  memory_size      = var.lambda_memory_size
  source_code_hash = filebase64sha256("${path.module}/../dist/ranking-agent.zip")

  layers = [aws_lambda_layer_version.dependencies.arn]

  environment {
    variables = {
      BEDROCK_MODEL_ID = var.bedrock_model_id
      ENVIRONMENT      = var.environment
    }
  }

  tags = {
    Name = "${var.project_name}-ranking"
  }
}

# CloudWatch Log Groups with retention
resource "aws_cloudwatch_log_group" "resume_parser" {
  name              = "/aws/lambda/${aws_lambda_function.resume_parser.function_name}"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "ranking_agent" {
  name              = "/aws/lambda/${aws_lambda_function.ranking_agent.function_name}"
  retention_in_days = 14
}

# Lambda permissions for API Gateway
resource "aws_lambda_permission" "resume_parser_api" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.resume_parser.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.main.execution_arn}/*/*"
}

resource "aws_lambda_permission" "ranking_agent_api" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.ranking_agent.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.main.execution_arn}/*/*"
}
