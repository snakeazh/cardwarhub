@echo off
chcp 65001 >nul
cd /d %~dp0

where node >nul 2>nul
if errorlevel 1 (
    echo [错误] 未找到 Node.js，请先安装 https://nodejs.org/
    pause
    exit /b 1
)

if not exist ConfigEditor\node_modules (
    echo 首次运行，正在安装依赖...
    cd ConfigEditor
    call npm install --no-audit --no-fund
    if errorlevel 1 (
        echo [错误] 依赖安装失败
        cd ..
        pause
        exit /b 1
    )
    cd ..
)

start "" http://localhost:5678
cd ConfigEditor
node server.js 5678
pause
