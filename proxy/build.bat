@echo off
chcp 65001 >nul
echo ========================================
echo   本地代理服务 - PyInstaller 打包工具
echo ========================================
echo.

:: 检查Python
python --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到Python，请先安装Python 3.8+
    pause
    exit /b 1
)

:: 检查pip
pip --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到pip
    pause
    exit /b 1
)

:: 安装依赖
echo [1/3] 安装依赖...
pip install -r requirements.txt -q
if errorlevel 1 (
    echo [错误] 依赖安装失败
    pause
    exit /b 1
)
echo       完成

:: 安装PyInstaller
echo [2/3] 安装PyInstaller...
pip install pyinstaller -q
if errorlevel 1 (
    echo [错误] PyInstaller安装失败
    pause
    exit /b 1
)
echo       完成

:: 打包
echo [3/3] 打包中...
echo.
pyinstaller ^
    --name="ProxyService" ^
    --onefile ^
    --console ^
    --icon=NONE ^
    --add-data="config.json;." ^
    --hidden-import=fastapi ^
    --hidden-import=uvicorn ^
    --hidden-import=uvicorn.logging ^
    --hidden-import=uvicorn.loops ^
    --hidden-import=uvicorn.loops.auto ^
    --hidden-import=uvicorn.protocols ^
    --hidden-import=uvicorn.protocols.http ^
    --hidden-import=uvicorn.protocols.http.auto ^
    --hidden-import=uvicorn.protocols.websockets ^
    --hidden-import=uvicorn.protocols.websockets.auto ^
    --hidden-import=uvicorn.lifespan ^
    --hidden-import=uvicorn.lifespan.auto ^
    --hidden-import=starlette ^
    --hidden-import=pydantic ^
    --hidden-import=pydantic.BaseModel ^
    main.py

if errorlevel 1 (
    echo.
    echo [错误] 打包失败
    pause
    exit /b 1
)

:: 复制配置文件
echo.
echo 复制配置文件...
copy config.json dist\ >nul 2>&1

echo.
echo ========================================
echo   打包完成！
echo ========================================
echo.
echo 可执行文件: dist\ProxyService.exe
echo.
echo 使用方法:
echo   1. 双击 ProxyService.exe 运行
echo   2. 访问 http://localhost:6789
echo   3. 配置文件 config.json 可自定义端口等
echo.
echo 开机自启设置:
echo   将 ProxyService.exe 快捷方式放入:
echo   C:\Users\你的用户名\AppData\Roaming\Microsoft\Windows\开始菜单\程序\启动
echo.
pause
