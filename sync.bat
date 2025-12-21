@echo off
color 0A
:: ==========================================
::      Tim's Project - Daily Sync v3.0
:: ==========================================

cd /d "%~dp0"
echo [Info] Current Directory: %cd%

:: 1. 获取当前分支 (自动适配 main 或 master)
for /f "tokens=*" %%i in ('git branch --show-current') do set "current_branch=%%i"
echo [Info] Current branch: %current_branch%
echo.

:: 2. 添加所有变动
echo [Step 1/3] Adding files...
git add .

:: 3. 检查状态
git diff --cached --quiet
if %errorlevel% equ 0 (
    echo [Info] No changes found. Everything is clean.
    goto :end
)

:: 4. 提交
:ask_commit
set "commit_msg="
set /p commit_msg="[Input] Enter commit message: "
if "%commit_msg%"=="" goto ask_commit
echo.
echo [Step 2/3] Committing...
git commit -m "%commit_msg%"

:: 5. 双端推送
echo.
echo [Step 3/3] Syncing to Remote...

echo [1/2] Pushing to GitHub (origin)...
git push origin %current_branch%

echo.
echo [2/2] Pushing to Gitee (gitee)...
git push gitee %current_branch%

:end
echo.
echo ==========================================
echo             Sync Complete
echo ==========================================
pause