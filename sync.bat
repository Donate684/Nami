@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

echo =======================================
echo      Mikan Sync (GitHub)
echo =======================================
echo.

:: Check if git is initialized
if not exist ".git" (
    echo [ERROR] Git not initialized in this folder.
    pause
    exit /b
)

:: Show current status
echo --- Current Status ---
git status -s
echo.

:: Ask for commit message
set /p msg="Enter commit message (default: Update): "
if "!msg!"=="" set msg=Update

:: Stage and commit
echo.
echo Staging files...
git add .
echo Committing changes...
git commit -m "!msg!"

:: Push
echo.
echo Pushing to GitHub (origin master)...
git push origin master

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Push failed. Check your internet connection or credentials.
) else (
    echo.
    echo [SUCCESS] Sync complete!
)

echo.
pause
