@echo off
echo Stopping ResumeRank Application...
echo.

:: Kill processes by port
echo Stopping Resume Parser Agent (port 5100)...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5100 ^| findstr LISTENING') do (
    taskkill /F /PID %%a 2>nul
)

echo Stopping Ranking Agent (port 5101)...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5101 ^| findstr LISTENING') do (
    taskkill /F /PID %%a 2>nul
)

echo Stopping Web Application (port 5016)...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5016 ^| findstr LISTENING') do (
    taskkill /F /PID %%a 2>nul
)

:: Close any remaining command windows with our titles
taskkill /FI "WINDOWTITLE eq Resume Parser Agent*" /F 2>nul
taskkill /FI "WINDOWTITLE eq Ranking Agent*" /F 2>nul
taskkill /FI "WINDOWTITLE eq ResumeRank Web*" /F 2>nul

echo.
echo All services have been stopped.
