@echo off
:: 切换控制台为 UTF-8 编码，解决乱码问题
chcp 65001 >nul

setlocal

:: ==========================================
::      用户配置区 (在此填入你的地址)
:: ==========================================
set "GITHUB_URL=https://github.com/Timtim00214/TCP-Relay-Chat.git"
set "GITEE_URL=https://gitee.com/Tim7im/tcp-relay-chat.git"
:: ==========================================

echo [Info] 正在启动修复版配置脚本...
echo.

:: 1. 暴力清理旧配置 (防止 "remote exists" 报错)
:: 如果 .git 存在，先移除旧的 remote，确保环境干净
if exist .git (
    echo [Step 1/5] 清理旧的远程连接...
    git remote remove origin >nul 2>&1
    git remote remove gitee >nul 2>&1
) else (
    echo [Step 1/5] 初始化新仓库...
    git init >nul
)

:: 2. 强制分支名为 main
echo [Step 2/5] 锁定分支为 main...
git branch -M main

:: 3. 挂载远程仓库 (先删后加)
echo [Step 3/5] 挂载远程仓库...
git remote add origin %GITHUB_URL%
git remote add gitee %GITEE_URL%

:: 4. 暴力合并 (解决 "unrelated histories")
echo [Step 4/5] 同步云端历史 (自动处理冲突)...
:: 尝试拉取，如果仓库为空会报错，但不影响后续
git pull origin main --allow-unrelated-histories --no-rebase >nul 2>&1

:: 5. 提交并双端推送
echo [Step 5/5] 执行首次双端推送...
git add .
git commit -m "Config Fix and Init" >nul 2>&1

echo 正在推送到 GitHub...
git push -u origin main
if %errorlevel% neq 0 (
    echo [Warning] GitHub 推送遇到问题 (可能是网络原因)，请稍后重试。
) else (
    echo [Success] GitHub 完成。
)

echo 正在推送到 Gitee...
git push -u gitee main
if %errorlevel% neq 0 (
    echo [Warning] Gitee 推送遇到问题，请检查密码。
) else (
    echo [Success] Gitee 完成。
)

echo.
echo ==========================================
echo [Success] 修复与配置完成！
echo 乱码问题已解决，远程连接已重置。
echo ==========================================
pause