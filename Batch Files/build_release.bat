@echo off
setlocal

set scriptdir=%~dp0
set staging=C:\Staging

echo ============================================
echo  Step 0: Clear %staging%
echo ============================================
if exist "%staging%" (
    rmdir /s /q "%staging%"
    if errorlevel 1 (
        echo.
        echo Failed to clear %staging%. Aborting build.
        exit /b 1
    )
)
mkdir "%staging%"

echo.
echo ============================================
echo  Step 1: Minify CSS/JS assets
echo ============================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%scriptdir%Minify-Assets.ps1"
if errorlevel 1 (
    echo.
    echo Minify-Assets.ps1 failed. Aborting build.
    exit /b 1
)

echo.
echo ============================================
echo  Step 2: Publish SobekCM ^(Release^) to %staging%
echo ============================================
dotnet publish "%scriptdir%..\Code\SobekCM\SobekCM.csproj" -c Release -o "%staging%"
if errorlevel 1 (
    echo.
    echo dotnet publish failed.
    exit /b 1
)

echo.
echo Build complete. Output in %staging%

endlocal
