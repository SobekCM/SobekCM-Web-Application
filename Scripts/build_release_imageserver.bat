@echo off
setlocal

set scriptdir=%~dp0
set stagingroot=C:\Staging
set staging=%stagingroot%\ImageServer
set quiet=0
set symbols=0

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="-q" set quiet=1
if /I "%~1"=="-quiet" set quiet=1
if /I "%~1"=="-symbols" set symbols=1
shift
goto parse_args
:args_done

set publishargs=
if "%symbols%"=="1" set publishargs=-p:CopyOutputSymbolsToPublishDirectory=true -p:AllowedReferenceRelatedFileExtensions=.pdb

echo.
echo ======================================================
echo  Step 1: Clear last build
echo ======================================================
if not exist "%stagingroot%" goto step0_create

dir /b /a "%stagingroot%\*" >nul 2>&1
if errorlevel 1 goto step0_clear
if "%quiet%"=="1" goto step0_clear

echo.
echo WARNING: %stagingroot% is not empty.
echo.
set /p confirm=Delete everything under %staging% and continue? (Y/N):
if /I "%confirm%"=="Y" goto step0_clear

echo.
echo Aborted - nothing was deleted.
exit /b 1

:step0_clear
echo.
if "%quiet%"=="0" echo In the future you can run this with a -quiet flag to skip the confirmation.
if exist "%staging%" rmdir /s /q "%staging%"
if errorlevel 1 (
    echo.
    echo Failed to clear %staging%. Aborting build.
    exit /b 1
)

:step0_create
mkdir "%staging%"

echo.
echo ======================================================
echo  Step 2: Publish SobekCM_ImageServer ^(Release^) to %staging%
echo ======================================================
if "%symbols%"=="1" echo Including .pdb symbol files ^(-symbols flag^).
dotnet publish "%scriptdir%..\Code\SobekCM_ImageServer\SobekCM_ImageServer.csproj" -c Release -o "%staging%" %publishargs%
if errorlevel 1 (
    echo.
    echo dotnet publish failed.
    exit /b 1
)

echo.
echo ======================================================
echo  SUCCESS!
echo ======================================================
echo.
echo Note: unlike build_release.bat, this script does NOT call %stagingroot%\deploy.bat -
echo that script is Web-specific. Deploying this build (and setting up the ImageScratch
echo folder, the shared key file, and the iipsrv.fcgi IIS handler) is a separate manual
echo step for now.

endlocal
