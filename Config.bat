@echo off
color 0A
setlocal

:: ==========================================
::      用户配置区 (在此填入你的地址)
:: ==========================================
set "GITHUB_URL=https://github.com/Timtim00214/TCP-Relay-Chat.git"
set "GITEE_URL=https://gitee.com/Tim7im/tcp-relay-chat.git"
:: ==========================================

echo [Info] 开始执行初始化配置...
echo.

:: 1. 清理旧的 .git (加个判断防止误删)
if exist .git (
    echo [Warning] 检测到旧的 .git 目录。
    choice /M "是否删除旧配置并重新初始化？(不可逆)"
    if errorlevel 2 goto :skip_delete
    
    echo [Step 1/6] 正在清除旧 .git...
    rmdir /s /q .git
)
:skip_delete

:: 2. 初始化
echo [Step 2/6] 初始化 Git...
git init

:: 3. 强制分支名为 main (解决 master/main 命名问题)
echo [Step 3/6] 强制统一分支名为 main...
git branch -M main

:: 4. 配置远程仓库 (沿用 origin 和 gitee 双 remote 策略)
echo [Step 4/6] 挂载远程仓库...
git remote add origin %GITHUB_URL%
git remote add gitee %GITEE_URL%

:: 5. 暴力合并远程历史 (解决 "远程与本地冲突" 问题)
:: 逻辑：先尝试把 GitHub 上的 README/LICENSE 拉下来合并
echo [Step 5/6] 尝试拉取远程文件并暴力合并...
git pull origin main --allow-unrelated-histories --no-rebase >nul 2>&1
if %errorlevel% equ 0 (
    echo [Success] 远程历史已合并。
) else (
    echo [Info] 拉取失败或仓库为空，跳过合并，准备直接覆盖。
)

:: 6. 首次提交并设置追踪
echo [Step 6/6] 提交并设置 Upstream...
git add .
git commit -m "Initial config by Tim-Script" >nul 2>&1

:: 推送到 GitHub 并绑定默认分支
echo 正在推送到 GitHub...
git push -u origin main
if %errorlevel% neq 0 echo [Error] GitHub 推送失败，请检查网络。

:: 推送到 Gitee 并绑定默认分支
echo 正在推送到 Gitee...
git push -u gitee main
if %errorlevel% neq 0 echo [Error] Gitee 推送失败，请检查密码。

echo.
echo ==========================================
echo [Success] 初始化配置完毕！
echo 分支已锁定为: main
echo 双端已绑定，后续请直接使用 Push.bat
echo ==========================================
pause