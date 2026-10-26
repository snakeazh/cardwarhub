set LUBAN_DLL=Tools\Luban\Luban.dll
set CONF_ROOT=.

	
	
dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=..\FusionContinent\Assets\Scripts\Data\Client\Luban ^
    -x outputDataDir=..\Config\Luban\c ^
	-x l10n.textProviderFile=Datas\Common\TextInfo.xlsx ^
	--customTemplateDir Templates ^
	--timeZone "China Standard Time"


	
dotnet %LUBAN_DLL% ^
    -t server ^
    -c go-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=..\Server\data\configFiles\ ^
    -x outputDataDir=..\Config\Luban\json-go ^
	-x l10n.textProviderFile=Datas\Common\TextInfo.xlsx ^
	-x lubanGoModule=ruoyi-go/app/service/luban ^
	--customTemplateDir Templates ^
	--timeZone "China Standard Time"
	
	

rd /s /q ..\FusionContinent\Assets\Resources\Config\Json\
xcopy ..\Config\Luban\c\ ..\FusionContinent\Assets\Res\Config\Json\ /s /e /y
xcopy ..\Config\Luban\json-go\ ..\Server\data\json\ /s /e /y

pause