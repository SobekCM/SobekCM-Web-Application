@echo off
setlocal

REM Builds the one-off usage statistics backfill tool ( Utilities\StatsBackfill ) into
REM C:\Staging\StatsBackfill, ready to copy to the web server.  See the tool's README.md.

set scriptdir=%~dp0
set stagingroot=C:\Staging
set staging=%stagingroot%\StatsBackfill
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
if not exist "%staging%" goto step1_create
if "%quiet%"=="1" goto step1_clear

echo.
echo WARNING: %staging% already exists.
echo.
set /p confirm=Delete everything under %staging% and continue? (Y/N):
if /I "%confirm%"=="Y" goto step1_clear

echo.
echo Aborted - nothing was deleted.
exit /b 1

:step1_clear
echo.
if "%quiet%"=="0" echo In the future you can run this with a -quiet flag to skip the confirmation.
rmdir /s /q "%staging%"
if errorlevel 1 (
    echo.
    echo Failed to clear %staging%. Aborting build.
    exit /b 1
)

:step1_create
mkdir "%staging%"

echo.
echo ======================================================
echo  Step 2: Publish SobekCM_Stats_Backfill ^(Release^) to %staging%
echo ======================================================
if "%symbols%"=="1" echo Including .pdb symbol files ^(-symbols flag^).
dotnet publish "%scriptdir%..\Utilities\StatsBackfill\StatsBackfill.csproj" -c Release -o "%staging%" %publishargs%
if errorlevel 1 (
    echo.
    echo dotnet publish failed.
    exit /b 1
)

copy /y "%scriptdir%..\Utilities\StatsBackfill\README.md" "%staging%\" >nul

echo.
echo ======================================================
echo  SUCCESS!
echo ======================================================
echo.
echo Copy %staging% to the web server, then start with a dry run:
echo.
echo   SobekCM_Stats_Backfill --connection "..." --logs "D:\...\W3SVC{n}" --scan
echo.
echo Add --execute once the dry run looks right.  See README.md for every option.

endlocal
