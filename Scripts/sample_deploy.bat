@echo off
setlocal

REM This is a sample deploy script (similar to the one we use at SobekDigital) which
REM does some additional cleanup and copying files and then ultimately copies this 
REM out to a network location to either manually grab or allow a CI to launch a 
REM process.
REM
REM To use this and have it run automatically at the end of the build_release 
REM batch file, just put this in your C:\Staging folder.


set webSource=C:\Staging\Web
set deployShare=\\network\staging

REM Remove the sobekcm config file, so it doesn't overwrite the live one
del /f /q "%webSource%\config\SobekCM.config"

REM Copy over the config with our list of IP addresses
copy /y "C:\Staging\Deploy\sobekdigital_engine_ips.config" "%webSource%\config\default\sobekdigital_engine_ips.config"

REM Sanitize the build for existing sites
if exist "%webSource%\config\user" (
    rmdir /s /q "%webSource%\config\user"
    if errorlevel 1 (
        echo.
        echo Failed to remove config\user folder. Aborting.
        exit /b 1
    )
)

REM Replace the now missing config\user folder
mkdir "%webSource%\config\user"

REM Determine the dated target folder name
for /f %%D in ('powershell -NoProfile -Command "Get-Date -Format ddMMyyyy"') do set deploydate=%%D
set target=%deployShare%\Web_%deploydate%

REM /mir creates the target folder if it doesn't exist yet, or clears it to exactly
REM match the source if it already does -- covers both cases in one step
echo Copying %webSource% to %target% ...
robocopy "%webSource%" "%target%" /mir /NFL /NDL /NJH /NJS /nc /ns /np
if errorlevel 8 (
    echo.
    echo ERROR: robocopy failed copying build to %target%
    exit /b 1
)

echo.
echo Build copied to %target% pending final deployment

endlocal
exit /b 0
