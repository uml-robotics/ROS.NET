@echo off
echo Crawling solution directory for msgs+srvs
cd "%*"
set TMPFILE=%TMP%\baglist.txt
set TMPDIR=%TMP%\msgs_flat
call:listmsgs
call:mapmsgs
del /Q %TMPFILE%
goto:eof

:parentpath
pushd "%*\.."
set "parent=%CD%\"
popd
goto:eof

:dirname
set file=%~1
set target=%~2
call:parentpath %target%
call set "dirname=%%target:%parent%=%%"
call set "dirname=%%dirname:\=%%"

REM if the .msg or .srv file is in a folder named msgs, then its package name is one directory higher than the ROS built-ins' package name
if "%dirname%" equ "msg" call:parentpath %parent%
if "%dirname%" equ "msgs" call:parentpath %parent%
if "%dirname%" equ "srv" call:parentpath %parent%
if "%dirname%" equ "srvs" call:parentpath %parent%
call set "dirname=%%target:%parent%=%%"
call set "dirname=%%dirname:\=%%"

REM copy the file to %TMPDIR%\package_name\message.msg
xcopy /I /Y /Q /D %file% %TMPDIR%\%dirname%\
goto:eof

:mapmsgs
for /F "tokens=*" %%I in (%TMPFILE%) do call:dirname "%%~I" "%%~dpI"
goto:eof

:listmsgs
setlocal enableDelayedExpansion
for /r %%a in (*) do ( if "%%~xa" equ ".msg" echo %%a >> %TMPFILE%
if "%%~xa" equ ".srv" echo %%a >> %TMPFILE%
)
endlocal
goto:eof