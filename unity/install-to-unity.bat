@echo off
chcp 65001 >nul
setlocal

REM ============================================================
REM  一键把 SketchToImage 脚本复制到 Unity 工程
REM  目标：D:\Unity\Pj\2026\2026.7.11涂鸦工程
REM ============================================================

set "TARGET=D:\Unity\Pj\2026\2026.7.11涂鸦工程"
set "SOURCE=%~dp0SketchToImage"

if not exist "%TARGET%\Assets" (
    echo [错误] 找不到 Unity 工程：%TARGET%
    echo 请确认路径是否正确，或修改本 bat 里的 TARGET 变量。
    pause
    exit /b 1
)

if not exist "%SOURCE%\Scripts" (
    echo [错误] 找不到脚本目录：%SOURCE%\Scripts
    echo 请把本 bat 放在与 SketchToImage 文件夹同级再运行。
    pause
    exit /b 1
)

echo.
echo 复制到：%TARGET%\Assets\SketchToImage\
echo.

xcopy "%SOURCE%\Scripts" "%TARGET%\Assets\SketchToImage\Scripts\" /E /I /Y
if errorlevel 1 (
    echo [错误] 复制失败
    pause
    exit /b 1
)

if not exist "%TARGET%\Assets\StreamingAssets" mkdir "%TARGET%\Assets\StreamingAssets"
echo.
echo [完成] 脚本已复制到 Assets\SketchToImage\Scripts\
echo.
echo 接下来请你：
echo   1. 打开 Unity 工程，等脚本编译完成
echo   2. ComfyUI 里 Save ^(API Format^) 导出 workflow_api.json
echo   3. 放到 %TARGET%\Assets\StreamingAssets\workflow_api.json
echo   4. Create - SketchToImage - ComfyUI Settings 并配置节点 id
echo.
pause
