@echo off
setlocal enableextensions

pushd "%~dp0"

set "PROTOC=.\protoc\bin\protoc.exe"
set "PROTO_DIR=.\Proto"
set "OUT_DIR=..\FusionContinent\Assets\Scripts\Generated\Proto"
set "INCLUDE=.\protoc\include"
set "PROTO_DIR_ABS=%CD%\Proto"
set "INCLUDE_ABS=%CD%\protoc\include"

if exist "%OUT_DIR%" (
  rmdir /s /q "%OUT_DIR%"
  if errorlevel 1 (
    set "ERR=1"
    goto :end
  )
)

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
set "ERR=0"

for /r "%PROTO_DIR%" %%F in (*.proto) do (
  "%PROTOC%" -I "%PROTO_DIR_ABS%" -I "%INCLUDE_ABS%" --csharp_out="%OUT_DIR%" "%%F"
  if errorlevel 1 (
    set "ERR=1"
    goto :end
  )
)

:end
popd
if "%ERR%"=="0" (
  echo [SUCCESS] Protoc C# generation completed. Output: "%OUT_DIR%"
) else (
  echo [FAIL] Protoc C# generation failed. See messages above.
)
pause
exit /b %ERR%
