
@REM This file builds the project and packs the files into zip archives 
@REM that are ready to be uploaded to GitHub releases.
@REM It also spits out the ISO 8601 timestamp that gets put in changelog.txt

@set projectpath=%cd%
@set packedpath=PackedRelease
@set projectname=FezChaosMod
@set projectsubpath=FEZ.ChaosMod.mm

@IF [%1]==[/nobuild] GOTO AFTERBUILD
@IF [%1]==[/timeonly] GOTO PRINTBUILDTIME

@echo Publishing project...

@REM this should be where dotnet.exe is located
@REM cd /d "C:\Program Files\dotnet"

@REM the following line should build all the projects in the solution
@REM todo: change net10.0-windows to net10.0 when winforms gets removed
dotnet publish "%projectpath%\FezChaosMod-NET10.slnx" -c Release -f net10.0-windows -p:ArtifactsPath="." -p:ArtifactsPivots="" -p:ArtifactsProjectName=""
@echo Builds completed.

cd /d %projectpath%

:AFTERBUILD
@echo Packing projects...

@echo Copying Metadata.xml ...
@copy Metadata.xml %projectsubpath%\publish\Metadata.xml
@IF NOT EXIST %packedpath% @mkdir %packedpath%

@rem bin\x86\Debug\net10.0
@echo Packing the files that are in %projectsubpath%\publish into %packedpath%\FezMultiplayerMod.zip ...
@powershell -command "Compress-Archive -Path '%projectsubpath%\publish\*' -DestinationPath '%packedpath%\FezMultiplayerMod.zip' -Force"

@echo Finished packing projects. zip files are in %cd%\%packedpath%

:PRINTBUILDTIME
@rem Get the filemtime
@set filePath=%projectsubpath%\publish\FezMultiplayerMod.dll
@for /f "delims=" %%G in ('powershell -command "(Get-Item '%projectpath%\%filePath%').LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')"') do @set "utcTime=%%G"

@REM this timestamp is what goes in changelog.txt
@echo Build time: %utcTime%
@echo Build time: %utcTime%>%packedpath%\buildtime.txt
@echo Done.
@pause
