[Version]
Class=IEXPRESS
SEDVersion=3

[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
IconFile=%IconFile%
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles

[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=D:\project\myproject\term\src\zmd\bin\Release\net7.0-windows\win-x64\zmd-setup.exe
FriendlyName=zmd Setup
AppLaunched=install.cmd
PostInstallCmd=<None>
IconFile=D:\project\myproject\term\src\zmd\app.ico
FILE0=app.zip
FILE1=install.cmd

[SourceFiles]
SourceFiles0=D:\project\myproject\term\src\zmd\installer\

[SourceFiles0]
%FILE0%=
%FILE1%=
