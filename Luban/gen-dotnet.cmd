@echo off
REM 仅生成 netserver 用的 cs-dotnet-json 配置代码/数据（对齐 luban_examples/Csharp_DotNet_json）
setlocal
cd /d "%~dp0"

set LUBAN_DLL=Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet "%LUBAN_DLL%" ^
    -t server ^
    -c cs-dotnet-json ^
    -d json ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -x outputCodeDir=..\netserver\src\CardWar.Config\Gen ^
    -x outputDataDir=..\Config\Luban\json-dotnet ^
    -x l10n.textProviderFile=Datas\Common\TextInfo.xlsx ^
    --timeZone "China Standard Time"
if errorlevel 1 exit /b 1

if not exist "..\netserver\src\CardWar.Config\Json\" mkdir "..\netserver\src\CardWar.Config\Json\"
xcopy "..\Config\Luban\json-dotnet\*" "..\netserver\src\CardWar.Config\Json\" /s /e /y /q
if errorlevel 1 exit /b 1

echo Luban cs-dotnet-json for netserver done.
exit /b 0
