<#
.SYNOPSIS
    Packages Lambda functions and dependencies for deployment.

.DESCRIPTION
    Creates deployment packages for the ResumeRank Lambda functions:
    - lambda-layer.zip: Python dependencies
    - resume-parser.zip: Resume parser function
    - ranking-agent.zip: Ranking agent function

.PARAMETER PythonPath
    Path to Python executable. Defaults to 'python'.

.EXAMPLE
    .\package-lambda.ps1
    .\package-lambda.ps1 -PythonPath "python3.11"
#>

param(
    [string]$PythonPath = "python"
)

$ErrorActionPreference = "Stop"

# Project paths
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LambdaSource = Join-Path $ProjectRoot "src\agents\aws_lambda"
$DistDir = Join-Path $ProjectRoot "dist"
$TempDir = Join-Path $env:TEMP "resumerank-lambda-build"

Write-Host "Packaging Lambda functions..." -ForegroundColor Cyan

# Create dist directory
if (Test-Path $DistDir) {
    Remove-Item -Recurse -Force $DistDir
}
New-Item -ItemType Directory -Path $DistDir | Out-Null

# Create temp directory
if (Test-Path $TempDir) {
    Remove-Item -Recurse -Force $TempDir
}
New-Item -ItemType Directory -Path $TempDir | Out-Null

try {
    # Package Lambda Layer (dependencies)
    Write-Host "`nPackaging Lambda Layer..." -ForegroundColor Yellow
    $LayerDir = Join-Path $TempDir "layer"
    $LayerPythonDir = Join-Path $LayerDir "python"
    New-Item -ItemType Directory -Path $LayerPythonDir | Out-Null

    # Install dependencies to layer directory
    $RequirementsFile = Join-Path $LambdaSource "requirements.txt"
    & $PythonPath -m pip install -r $RequirementsFile -t $LayerPythonDir --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install dependencies"
    }

    # Create layer zip
    $LayerZip = Join-Path $DistDir "lambda-layer.zip"
    Compress-Archive -Path "$LayerDir\*" -DestinationPath $LayerZip -Force
    Write-Host "Created: $LayerZip" -ForegroundColor Green

    # Package Resume Parser
    Write-Host "`nPackaging Resume Parser..." -ForegroundColor Yellow
    $ParserDir = Join-Path $TempDir "resume-parser"
    New-Item -ItemType Directory -Path $ParserDir | Out-Null

    # Copy handler and parser files
    Copy-Item (Join-Path $LambdaSource "resume_parser\handler.py") $ParserDir
    Copy-Item (Join-Path $LambdaSource "resume_parser\parser.py") $ParserDir

    # Copy shared module
    $SharedDir = Join-Path $ParserDir "shared"
    New-Item -ItemType Directory -Path $SharedDir | Out-Null
    Copy-Item (Join-Path $LambdaSource "shared\__init__.py") $SharedDir
    Copy-Item (Join-Path $LambdaSource "shared\bedrock_client.py") $SharedDir
    Copy-Item (Join-Path $LambdaSource "shared\s3_client.py") $SharedDir

    # Create parser zip
    $ParserZip = Join-Path $DistDir "resume-parser.zip"
    Compress-Archive -Path "$ParserDir\*" -DestinationPath $ParserZip -Force
    Write-Host "Created: $ParserZip" -ForegroundColor Green

    # Package Ranking Agent
    Write-Host "`nPackaging Ranking Agent..." -ForegroundColor Yellow
    $RankerDir = Join-Path $TempDir "ranking-agent"
    New-Item -ItemType Directory -Path $RankerDir | Out-Null

    # Copy handler and ranker files
    Copy-Item (Join-Path $LambdaSource "ranking_agent\handler.py") $RankerDir
    Copy-Item (Join-Path $LambdaSource "ranking_agent\ranker.py") $RankerDir

    # Copy shared module
    $SharedDir = Join-Path $RankerDir "shared"
    New-Item -ItemType Directory -Path $SharedDir | Out-Null
    Copy-Item (Join-Path $LambdaSource "shared\__init__.py") $SharedDir
    Copy-Item (Join-Path $LambdaSource "shared\bedrock_client.py") $SharedDir
    Copy-Item (Join-Path $LambdaSource "shared\s3_client.py") $SharedDir

    # Create ranker zip
    $RankerZip = Join-Path $DistDir "ranking-agent.zip"
    Compress-Archive -Path "$RankerDir\*" -DestinationPath $RankerZip -Force
    Write-Host "Created: $RankerZip" -ForegroundColor Green

    Write-Host "`nPackaging complete!" -ForegroundColor Cyan
    Write-Host "Output directory: $DistDir"

} finally {
    # Cleanup temp directory
    if (Test-Path $TempDir) {
        Remove-Item -Recurse -Force $TempDir
    }
}
