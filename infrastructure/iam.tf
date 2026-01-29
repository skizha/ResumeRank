# IAM role for Lambda execution
resource "aws_iam_role" "lambda_execution" {
  name = "${var.project_name}-lambda-execution-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

# Basic Lambda execution policy (CloudWatch Logs)
resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# S3 access policy for Lambda
resource "aws_iam_policy" "lambda_s3_access" {
  name        = "${var.project_name}-lambda-s3-access-${var.environment}"
  description = "Allow Lambda to read/write resume files in S3"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Resource = [
          aws_s3_bucket.resumes.arn,
          "${aws_s3_bucket.resumes.arn}/*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_s3_access" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = aws_iam_policy.lambda_s3_access.arn
}

# Bedrock access policy for Lambda
resource "aws_iam_policy" "lambda_bedrock_access" {
  name        = "${var.project_name}-lambda-bedrock-access-${var.environment}"
  description = "Allow Lambda to invoke Bedrock models"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "bedrock:InvokeModel",
          "bedrock:InvokeModelWithResponseStream"
        ]
        Resource = [
          "arn:aws:bedrock:${var.aws_region}::foundation-model/${var.bedrock_model_id}",
          "arn:aws:bedrock:${var.aws_region}::foundation-model/anthropic.*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_bedrock_access" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = aws_iam_policy.lambda_bedrock_access.arn
}
