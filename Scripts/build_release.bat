@echo off
setlocal

set scriptdir=%~dp0
set staging=C:\Staging
set quiet=0

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="-q" set quiet=1
if /I "%~1"=="-quiet" set quiet=1
shift
goto parse_args
:args_done

echo ============================================
echo  Step 0: Clear %staging%
echo ============================================
if not exist "%staging%" goto step0_create

dir /b /a "%staging%\*" >nul 2>&1
if errorlevel 1 goto step0_clear
if "%quiet%"=="1" goto step0_clear

echo.
echo WARNING: %staging% is not empty.
echo.
set /p confirm=Delete everything under %staging% and continue? (Y/N):
if /I "%confirm%"=="Y" goto step0_clear

echo.
echo Aborted - nothing was deleted.
exit /b 1

:step0_clear
echo.
if "%quiet%"=="0" echo In the future you can run this with a -quiet flag to skip the confirmation.
rmdir /s /q "%staging%"
if errorlevel 1 (
    echo.
    echo Failed to clear %staging%. Aborting build.
    exit /b 1
)

:step0_create
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
echo ============================================
echo  Step 3: Create needed empty folders for deployment
echo ============================================
mkdir "%staging%\data"
mkdir "%staging%\temp"
mkdir "%staging%\plugins"

echo.
echo ============================================
echo  Step 4: Copy over basic folders for blank instance
echo ============================================
REM Pulled from Code\Includes (not Code\SobekCM), since the folders under SobekCM\
REM often have local test/dev files mixed in that shouldn't ship in a release.
robocopy "%scriptdir%..\Code\Includes\design" "%staging%\design" /mir /NFL /NDL /NJH /NJS /nc /ns /np
if errorlevel 8 (
    echo.
    echo robocopy failed copying design folder.
    exit /b 1
)

robocopy "%scriptdir%..\Code\Includes\mySobek" "%staging%\mySobek" /mir /NFL /NDL /NJH /NJS /nc /ns /np
if errorlevel 8 (
    echo.
    echo robocopy failed copying mySobek folder.
    exit /b 1
)

echo.
echo Build complete. Output in %staging%

endlocal
