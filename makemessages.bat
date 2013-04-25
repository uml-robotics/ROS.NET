@echo off & setLocal EnableDelayedExpansion

cd C:\ROS_Sharp

xcopy /Y OPENVPN\* "C:\Program Files\OpenVPN\config\"

set str=%CD%
set C=\
set N=0
set AFTER=0

:loop
if !str:~0^,1! equ !C! (
set /a N+=1
)
if "!str:~1!" neq "" (
if %N% neq 0 (
set /a AFTER+=1
)
set str=!str:~1!
goto :loop
)
if %AFTER% neq 0 (
elevate.cmd UACFTL.bat %CD%\
goto :done
)
elevate.cmd UACFTL.bat %CD%
:done