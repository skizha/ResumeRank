variable "aws_region" {
  description = "AWS region to deploy resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name (e.g., dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "resumerank"
}

variable "lambda_memory_size" {
  description = "Memory size for Lambda functions in MB"
  type        = number
  default     = 512
}

variable "lambda_timeout" {
  description = "Timeout for Lambda functions in seconds"
  type        = number
  default     = 120
}

variable "bedrock_model_id" {
  description = "AWS Bedrock model ID to use for AI processing"
  type        = string
  default     = "anthropic.claude-3-sonnet-20240229-v1:0"
}

variable "s3_force_destroy" {
  description = "Allow S3 bucket to be destroyed even if not empty"
  type        = bool
  default     = false
}
