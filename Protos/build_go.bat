@echo off
setlocal enableextensions

pushd "%~dp0"

set "PROTOC=.\protoc\bin\protoc.exe"
set "PROTO_DIR=.\Proto"
set "COPY_DIR=..\Server\proto\cardwar"
set "OUT_DIR=..\Server\proto\gen\cardwar"
set "INCLUDE=.\protoc\include"
set "PROTO_DIR_ABS=%CD%\Proto"
set "INCLUDE_ABS=%CD%\protoc\include"

echo ========================================
echo Copying .proto files to %COPY_DIR%...
echo ========================================

if not exist "%COPY_DIR%" mkdir "%COPY_DIR%"
xcopy /y /q "%PROTO_DIR%\*.proto" "%COPY_DIR%\"
if errorlevel 1 (
  echo [FAIL] Failed to copy .proto files.
  popd
  pause
  exit /b 1
)
echo [OK] .proto files copied.

echo.
echo ========================================
echo Generating .pb.go files to %OUT_DIR%...
echo ========================================

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

for /r "%PROTO_DIR%" %%F in (*.proto) do (
  echo Processing: %%~nxF
  "%PROTOC%" -I "%PROTO_DIR_ABS%" -I "%INCLUDE_ABS%" --go_out="%OUT_DIR%" --go_opt=paths=source_relative "%%F"
  if errorlevel 1 (
    echo [FAIL] Protoc generation failed for: %%~nxF
    popd
    pause
    exit /b 1
  )
)

popd
echo.
echo ========================================
echo [SUCCESS] Copy to "%COPY_DIR%"
echo [SUCCESS] Generate to "%OUT_DIR%"
echo ========================================
pause
exit /b 0
