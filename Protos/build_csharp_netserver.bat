@echo off
setlocal enableextensions

pushd "%~dp0"

set "PROTOC=.\protoc\bin\protoc.exe"
set "PROTO_DIR=.\Proto"
set "OUT_DIR=..\netserver\src\CardWar.Contracts\Generated"
set "INCLUDE=.\protoc\include"
set "PROTO_DIR_ABS=%CD%\Proto"
set "INCLUDE_ABS=%CD%\protoc\include"

echo ========================================
echo Generating C# for netserver...
echo Output: %OUT_DIR%
echo ========================================

if not exist "%PROTOC%" (
  echo [FAIL] protoc not found: %PROTOC%
  set "ERR=1"
  goto :end
)

if not exist "..\netserver\src\CardWar.Contracts" (
  echo [FAIL] netserver Contracts project not found: ..\netserver\src\CardWar.Contracts
  set "ERR=1"
  goto :end
)

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
  echo Processing: %%~nxF
  "%PROTOC%" -I "%PROTO_DIR_ABS%" -I "%INCLUDE_ABS%" --csharp_out="%OUT_DIR%" "%%F"
  if errorlevel 1 (
    set "ERR=1"
    goto :end
  )
)

echo.
echo Remapping namespace Cardwar -^> CardWar.Contracts ...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-ChildItem -LiteralPath '%OUT_DIR%' -Filter '*.cs' | ForEach-Object { $c = [IO.File]::ReadAllText($_.FullName); $c = $c.Replace('namespace Cardwar', 'namespace CardWar.Contracts').Replace('global::Cardwar.', 'global::CardWar.Contracts.'); [IO.File]::WriteAllText($_.FullName, $c) }"
if errorlevel 1 (
  set "ERR=1"
  goto :end
)

:end
popd
if "%ERR%"=="0" (
  echo.
  echo [SUCCESS] C# generation completed. Output: "%OUT_DIR%"
) else (
  echo.
  echo [FAIL] Protoc C# generation failed. See messages above.
)
pause
exit /b %ERR%
