@echo off
color 0A
:: ==========================================
::      Tim's Project - Auto Sync v2.1
:: ==========================================

:: 1. 核心修正：定位到当前脚本所在的目录
cd /d "%~dp0"

echo [Info] Current Directory: %cd%
echo.

:: 2. 检查 Git 是否安装
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [Error] Git ignores you. Please install Git first.
    pause
    exit
)

:: 3. 检查是否是 Git 仓库
git status >nul 2>&1
if %errorlevel% neq 0 (
    echo [Error] Not a Git repository. Run 'git init' first.
    pause
    exit
)

:: 4. 获取当前分支名
for /f "tokens=*" %%i in ('git branch --show-current') do set "current_branch=%%i"

echo [Info] Current branch: %current_branch%
echo.

:: 5. 添加所有变动
echo [Step 1/4] Adding files...
git add .

:: 6. 检查是否有文件需要提交
git diff --cached --quiet
if %errorlevel% equ 0 (
    echo [Info] No changes to commit.
    goto push
)

:: 7. 询问更新日志
:ask_commit
set "commit_msg="
set /p commit_msg="[Input] Enter commit message: "
if "%commit_msg%"=="" goto ask_commit

:: 8. 提交存档
echo.
echo [Step 2/4] Committing...
git commit -m "%commit_msg%"

:push
:: 9. 推送到 GitHub (origin)
echo.
echo [Step 3/4] Pushing to GitHub (origin)...
git push origin %current_branch%
if %errorlevel% neq 0 (
    color 0C
    echo [Warning] GitHub push failed! Check your network or proxy.
    echo [Tip] If this is the first push, try: git push -u origin %current_branch%
) else (
    echo [Success] GitHub synced.
)

:: 10. 推送到 Gitee (gitee)
echo.
echo [Step 4/4] Pushing to Gitee (remote: gitee)...
git push gitee %current_branch%
if %errorlevel% neq 0 (
    color 0C
    echo [Warning] Gitee push failed! Check your password or network.
    echo [Tip] If this is the first push, try: git push -u gitee %current_branch%
) else (
    echo [Success] Gitee synced.
)

:: 恢复颜色
color 0A

echo.
echo ==========================================
echo             All Tasks Finished
echo ==========================================
pause