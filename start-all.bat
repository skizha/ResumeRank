@echo off
echo Starting ResumeRank Application...
echo.

:: Get the directory where the batch file is located
set "ROOT_DIR=%~dp0"

:: Start Resume Parser Agent (Port 5100)
echo Starting Resume Parser Agent on port 5100...
start "Resume Parser Agent" cmd /k "cd /d %ROOT_DIR%src\agents && python -m uvicorn resume_parser.main:app --port 5100"

:: Wait a moment for the first agent to initialize
timeout /t 2 /nobreak >nul

:: Start Ranking Agent (Port 5101)
echo Starting Ranking Agent on port 5101...
start "Ranking Agent" cmd /k "cd /d %ROOT_DIR%src\agents && python -m uvicorn ranking_agent.main:app --port 5101"

:: Wait a moment for the second agent to initialize
timeout /t 2 /nobreak >nul

:: Start .NET Web Application (Port 5016)
echo Starting Web Application on port 5016...
start "ResumeRank Web" cmd /k "cd /d %ROOT_DIR% && dotnet run --project src\ResumeRank.Web"

echo.
echo All services are starting...
echo.
echo   Web Application:    http://localhost:5016
echo   Resume Parser:      http://localhost:5100
echo   Ranking Agent:      http://localhost:5101
echo.
echo Use stop-all.bat to stop all services.
