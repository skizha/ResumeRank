<#
.SYNOPSIS
    Destroys ResumeRank AWS infrastructure.

.DESCRIPTION
    This script runs Terraform destroy to remove all AWS resources
    created by the deploy script.

.PARAMETER Environment
    Deployment environment (dev, staging, prod). Default: dev

.PARAMETER Region
    AWS region. Default: us-east-1

.PARAMETER AutoApprove
    Skip Terraform confirmation prompts.

.PARAMETER Force
    Also destroy S3 bucket contents (required if bucket is not empty).

.EXAMPLE
    .\destroy-aws.ps1
    .\destroy-aws.ps1 -Environment prod -AutoApprove
    .\destroy-aws.ps1 -Force -AutoApprove
#>

param(
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",

    [string]$Region = "us-east-1",

    [switch]$AutoApprove,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$InfraDir = Join-Path $ProjectRoot "infrastructure"

Write-Host "============================================" -ForegroundColor Red
Write-Host "  ResumeRank AWS Destruction" -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Red
Write-Host "Environment: $Environment"
Write-Host "Region: $Region"
Write-Host ""

if (-not $AutoApprove) {
    Write-Host "WARNING: This will destroy all AWS resources!" -ForegroundColor Yellow
    $confirm = Read-Host "Type 'yes' to continue"
    if ($confirm -ne "yes") {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 0
    }
}

Push-Location $InfraDir
try {
    # Initialize Terraform if needed
    if (-not (Test-Path ".terraform")) {
        Write-Host "Initializing Terraform..." -ForegroundColor Yellow
        terraform init
        if ($LASTEXITCODE -ne 0) {
            throw "Terraform init failed"
        }
    }

    # Build destroy arguments
    $tfVars = @(
        "-var", "environment=$Environment",
        "-var", "aws_region=$Region"
    )

    if ($Force) {
        $tfVars += @("-var", "s3_force_destroy=true")
    }

    $destroyArgs = @("destroy") + $tfVars
    if ($AutoApprove) {
        $destroyArgs += "-auto-approve"
    }

    # Run destroy
    Write-Host "`nDestroying AWS resources..." -ForegroundColor Yellow
    terraform @destroyArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform destroy failed"
    }

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  Destruction Complete!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green

} finally {
    Pop-Location
}
