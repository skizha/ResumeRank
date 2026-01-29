<#
.SYNOPSIS
    Deploys ResumeRank infrastructure and Lambda functions to AWS.

.DESCRIPTION
    This script:
    1. Packages Lambda functions
    2. Runs Terraform to create/update AWS infrastructure
    3. Outputs the API Gateway URL and S3 bucket name

.PARAMETER Environment
    Deployment environment (dev, staging, prod). Default: dev

.PARAMETER Region
    AWS region. Default: us-east-1

.PARAMETER AutoApprove
    Skip Terraform confirmation prompts.

.EXAMPLE
    .\deploy-aws.ps1
    .\deploy-aws.ps1 -Environment prod -Region us-west-2
    .\deploy-aws.ps1 -AutoApprove
#>

param(
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",

    [string]$Region = "us-east-1",

    [switch]$AutoApprove
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$InfraDir = Join-Path $ProjectRoot "infrastructure"
$ScriptsDir = $PSScriptRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ResumeRank AWS Deployment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "Region: $Region"
Write-Host ""

# Step 1: Package Lambda functions
Write-Host "Step 1: Packaging Lambda functions..." -ForegroundColor Yellow
& "$ScriptsDir\package-lambda.ps1"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to package Lambda functions"
}

# Step 2: Initialize Terraform
Write-Host "`nStep 2: Initializing Terraform..." -ForegroundColor Yellow
Push-Location $InfraDir
try {
    terraform init
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform init failed"
    }

    # Step 3: Plan Terraform changes
    Write-Host "`nStep 3: Planning Terraform changes..." -ForegroundColor Yellow
    $tfVars = @(
        "-var", "environment=$Environment",
        "-var", "aws_region=$Region"
    )

    terraform plan @tfVars -out=tfplan
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform plan failed"
    }

    # Step 4: Apply Terraform changes
    Write-Host "`nStep 4: Applying Terraform changes..." -ForegroundColor Yellow
    $applyArgs = @("apply")
    if ($AutoApprove) {
        $applyArgs += "-auto-approve"
    }
    $applyArgs += "tfplan"

    terraform @applyArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform apply failed"
    }

    # Step 5: Get outputs
    Write-Host "`nStep 5: Retrieving deployment outputs..." -ForegroundColor Yellow
    $apiUrl = terraform output -raw api_gateway_url
    $s3Bucket = terraform output -raw s3_bucket_name

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  Deployment Complete!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "API Gateway URL: $apiUrl" -ForegroundColor Cyan
    Write-Host "S3 Bucket: $s3Bucket" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To configure the .NET app for AWS mode, update appsettings.json:"
    Write-Host ""
    Write-Host "  {" -ForegroundColor Gray
    Write-Host "    `"AgentMode`": `"AWS`"," -ForegroundColor Gray
    Write-Host "    `"AWS`": {" -ForegroundColor Gray
    Write-Host "      `"Region`": `"$Region`"," -ForegroundColor Gray
    Write-Host "      `"ApiGatewayUrl`": `"$apiUrl`"," -ForegroundColor Gray
    Write-Host "      `"S3BucketName`": `"$s3Bucket`"" -ForegroundColor Gray
    Write-Host "    }" -ForegroundColor Gray
    Write-Host "  }" -ForegroundColor Gray
    Write-Host ""

    # Clean up plan file
    if (Test-Path "tfplan") {
        Remove-Item "tfplan"
    }

} finally {
    Pop-Location
}
