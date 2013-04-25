@echo off
SET MsBuildPath="%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe"
SET LogFile="C:\ROS_Sharp\build_log.txt"
SET MSBuildParameters=/t:Clean;Build /p:Configuration=Debug
SET BuildErrors=""
IF EXIST %LogFile% del %LogFile%
ECHO.
ECHO Building message parsers and messages
%MsBuildPath% "C:\ROS_Sharp\YAMLParser\YAMLParser.sln" %MSBuildParameters%
cd C:\ROS_Sharp\YAMLParser\bin\Debug
YAMLParser.exe
ECHO.
echo ALL DONE!!!!!
pause
@echo ON