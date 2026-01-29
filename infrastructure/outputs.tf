output "api_gateway_url" {
  description = "URL of the API Gateway endpoint"
  value       = aws_apigatewayv2_stage.main.invoke_url
}

output "s3_bucket_name" {
  description = "Name of the S3 bucket for resume storage"
  value       = aws_s3_bucket.resumes.id
}

output "s3_bucket_arn" {
  description = "ARN of the S3 bucket for resume storage"
  value       = aws_s3_bucket.resumes.arn
}

output "resume_parser_lambda_arn" {
  description = "ARN of the Resume Parser Lambda function"
  value       = aws_lambda_function.resume_parser.arn
}

output "ranking_agent_lambda_arn" {
  description = "ARN of the Ranking Agent Lambda function"
  value       = aws_lambda_function.ranking_agent.arn
}

output "lambda_execution_role_arn" {
  description = "ARN of the Lambda execution IAM role"
  value       = aws_iam_role.lambda_execution.arn
}

output "aws_region" {
  description = "AWS region where resources are deployed"
  value       = var.aws_region
}
