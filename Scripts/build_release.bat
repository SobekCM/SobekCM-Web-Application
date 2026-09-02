@echo off
setlocal

set scriptdir=%~dp0
set stagingroot=C:\Staging
set staging=%stagingroot%\Web
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
set /p confirm=Delete everything under %stagingroot% and continue? (Y/N):
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
echo ======================================================
echo  Step 2: Minify CSS/JS assets
echo ======================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%scriptdir%Minify-Assets.ps1"
if errorlevel 1 (
    echo.
    echo Minify-Assets.ps1 failed. Aborting build.
    exit /b 1
)

echo.
echo ======================================================
echo  Step 3: Clean and publish SobekCM ^(Release^) to %staging%
echo ======================================================
REM Forces every project to actually recompile instead of MSBuild reusing an
REM untouched project's cached bin\/obj\ output -- otherwise a reused DLL keeps
REM its original compile date when copied into the freshly-cleared staging
REM folder, so files can show up with older dates than the rest of the build.
dotnet clean "%scriptdir%..\Code\SobekCM\SobekCM.csproj" -c Release
if errorlevel 1 (
    echo.
    echo dotnet clean failed.
    exit /b 1
)
if "%symbols%"=="1" echo Including .pdb symbol files ^(-symbols flag^).
dotnet publish "%scriptdir%..\Code\SobekCM\SobekCM.csproj" -c Release -o "%staging%" %publishargs%
if errorlevel 1 (
    echo.
    echo dotnet publish failed.
    exit /b 1
)


echo.
echo ======================================================
echo  Step 4: Create needed empty folders for deployment
echo ======================================================
mkdir "%staging%\data"
mkdir "%staging%\temp"
mkdir "%staging%\plugins"


echo.
echo.
echo ======================================================
echo  Step 5: Copy over basic folders for blank instance
echo ======================================================
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
echo ======================================================
echo  Step 6: Call deploy script
echo ======================================================
 if exist "%stagingroot%\deploy.bat" (
     call "%stagingroot%\deploy.bat"
     if errorlevel 1 (
         echo.
         echo deploy.bat failed.
         exit /b 1
     )
 ) else (
     echo No deploy.bat found in %stagingroot% - skipping.
 )

echo.
echo ======================================================
echo  SUCCESS!
echo ======================================================

endlocal
