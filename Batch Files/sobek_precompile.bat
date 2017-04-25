echo off
REM ******** Some values used to support alternate locations **********
set source=C:\GitRepository\SobekCM-Web-Application
set iis=D:\PreCompile\Temp
set installer=D:\PreCompile\Output
echo.

echo COPYING WEB FILES INTO PRECOMPILE DIRECTORY
echo ....sobekcm
robocopy "%source%\SobekCM" "%iis%\SobekCM" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo ....sobekcm_library
robocopy "%source%\SobekCM_Library" "%iis%\SobekCM_Library" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo ....sobekcm_resource_object
robocopy "%source%\SobekCM_Resource_Object" "%iis%\SobekCM_Resource_Object" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo ....sobekcm_tools
robocopy "%source%\SobekCM_Tools" "%iis%\SobekCM_Tools" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo ....sobekcm_url_rewriter
robocopy "%source%\SobekCM_URL_Rewriter" "%iis%\SobekCM_URL_Rewriter" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo ....sobekcm_core
robocopy "%source%\SobekCM_Core" "%iis%\SobekCM_Core" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo ....sobekcm_engine_library
robocopy "%source%\SobekCM_Engine_Library" "%iis%\SobekCM_Engine_Library" /mir /NFL /NDL /NJH /NJS /nc /ns /np 

echo REMOVING ANY EXISTING STAGING DIRECTORIES
rmdir "%installer%\Staging64" /s /q
rmdir "%installer%\Staging32" /s /q
echo.

echo REMOVING DIRECTORIES TO NOT MOVE TO STAGING
rmdir %iis%\SobekCM\config /s /q
rmdir %iis%\SobekCM\design /s /q
rmdir %iis%\SobekCM\default /s /q
rmdir %iis%\SobekCM\iipimage /s /q
rmdir %iis%\SobekCM\mysobek /s /q
rmdir %iis%\SobekCM\dev /s /q
rmdir %iis%\SobekCM\temp /s /q
rmdir %iis%\SobekCM\obj /s /q
rmdir %iis%\SobekCM\Properties /s /q
rmdir %iis%\SobekCM\content /s /q
echo.


echo COMPILING 64-BIT VERSION
cd c:\windows\microsoft.net\framework64\v4.0.30319
aspnet_compiler -p "%iis%\SobekCM" -v / "%installer%\Staging64"
echo.

echo DELETING UNNECESSARY 64-BIT FILES
del "%installer%\Staging64\web.config"
del "%installer%\Staging64\SobekCM.csproj"
del "%installer%\Staging64\SobekCM.csproj.user"
del "%installer%\Staging64\Web.Debug.config"
del "%installer%\Staging64\Web.Release.config"
echo.

echo COPYING OTHER FILES INTO INSTALLER DIRECTORY
robocopy "%source%\SobekCM\config" "%installer%\FilesToInclude\config" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
robocopy "%source%\SobekCM\default" "%installer%\FilesToInclude\default" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
robocopy "%source%\SobekCM\iipimage" "%installer%\FilesToInclude\iipimage" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
robocopy "%source%\SobekCM\mySobek" "%installer%\FilesToInclude\mySobek" /mir /NFL /NDL /NJH /NJS /nc /ns /np 
echo.

echo DELETING SOME SUBFOLDERS TO NOT BE INCLUDED IN INSTALLER
rmdir "%installer%\FilesToInclude\mySobek\InProcess" /s /q
rmdir "%installer%\FilesToInclude\mySobek\templates\user" /s /q
REM rmdir "%installer%\FilesToInclude\default\css" /s /q
REM rmdir "%installer%\FilesToInclude\default\images" /s /q
REM rmdir "%installer%\FilesToInclude\default\includes" /s /q
REM rmdir "%installer%\FilesToInclude\default\js" /s /q
del "%installer%\FilesToInclude\config\sobekcm.config"
echo.

cd c:\


echo COPY FILES TO INCLUDE FOLDER OVER
xcopy "%installer%\FilesToInclude" "%installer%\Staging64" /s /e /i /y /q
echo.





