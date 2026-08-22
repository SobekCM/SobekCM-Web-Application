@echo off
setlocal

set scriptdir=%~dp0
set stagingroot=C:\Staging
set staging=%stagingroot%\Builder
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
echo  Step 2: Publish SobekCM Builder ^(Release^) to %staging%
echo ======================================================
if "%symbols%"=="1" echo Including .pdb symbol files ^(-symbols flag^).
dotnet publish "%scriptdir%..\Code\SobekCM Builder\SobekCM Builder.csproj" -c Release -o "%staging%" %publishargs%
if errorlevel 1 (
    echo.
    echo dotnet publish failed.
    exit /b 1
)


echo.
echo ======================================================
echo  Step 3: Create needed empty folders for deployment
echo ======================================================
REM logs/ and plugins/ are created empty, not copied - the source folders under
REM "Folders Under Executable" only hold old dev/test files that shouldn't ship.
mkdir "%staging%\logs"
mkdir "%staging%\plugins"
mkdir "%staging%\config"


echo.
echo ======================================================
echo  Step 4: Copy over basic runtime folders
echo ======================================================
REM Only the config TEMPLATE ships - not "Folders Under Executable\config\sobekcm.config",
REM which holds this dev machine's real database connection strings and shouldn't be
REM shipped in a release build. A fresh instance fills in its own config from the template.
copy /y "%scriptdir%..\Code\SobekCM Builder\Folders Under Executable\config\sobekcm_config_template.xml" "%staging%\config\" >nul
if errorlevel 1 (
    echo.
    echo Failed to copy sobekcm_config_template.xml.
    exit /b 1
)

robocopy "%scriptdir%..\Code\SobekCM Builder\Folders Under Executable\images" "%staging%\images" /mir /NFL /NDL /NJH /NJS /nc /ns /np
if errorlevel 8 (
    echo.
    echo robocopy failed copying images folder.
    exit /b 1
)
robocopy "%scriptdir%..\Code\SobekCM Builder\Folders Under Executable\Kakadu" "%staging%\Kakadu" /mir /NFL /NDL /NJH /NJS /nc /ns /np
if errorlevel 8 (
    echo.
    echo robocopy failed copying Kakadu folder.
    exit /b 1
)
robocopy "%scriptdir%..\Code\SobekCM Builder\Folders Under Executable\z3950" "%staging%\z3950" /mir /NFL /NDL /NJH /NJS /nc /ns /np
if errorlevel 8 (
    echo.
    echo robocopy failed copying z3950 folder.
    exit /b 1
)

echo.
echo ======================================================
echo  SUCCESS!
echo ======================================================
echo.
echo Note: unlike build_release.bat, this script does NOT call %stagingroot%\deploy.bat -
echo that script is Web-specific (operates on %stagingroot%\Web and pushes to a production
echo network share) and has no Builder-related logic. Deploying the Builder build is a
echo separate manual step for now.

endlocal
