@echo off
setlocal

set "APP_NAME=zmd"
set "INSTALL_DIR=%LocalAppData%\Programs\zmd"
set "START_MENU_DIR=%AppData%\Microsoft\Windows\Start Menu\Programs\zmd"
set "DESKTOP_LINK=%UserProfile%\Desktop\zmd.lnk"

powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $null = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F1F3E5B3-4E81-4D52-BFDD-C4A91CBA64FA}' -ErrorAction Stop } catch { try { $null = Get-ItemProperty 'HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F1F3E5B3-4E81-4D52-BFDD-C4A91CBA64FA}' -ErrorAction Stop } catch { Add-Type -AssemblyName PresentationFramework; [System.Windows.MessageBox]::Show('未检测到 Microsoft Edge WebView2 Runtime。zmd 需要 WebView2 才能显示终端界面，请先安装 WebView2 Runtime。','zmd 安装提示') | Out-Null } }"

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%~dp0app.zip' -DestinationPath '%INSTALL_DIR%' -Force"

if not exist "%START_MENU_DIR%" mkdir "%START_MENU_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell = New-Object -ComObject WScript.Shell; $shortcut = $shell.CreateShortcut('%START_MENU_DIR%\zmd.lnk'); $shortcut.TargetPath = '%INSTALL_DIR%\zmd.exe'; $shortcut.WorkingDirectory = '%INSTALL_DIR%'; $shortcut.Save(); $desktop = $shell.CreateShortcut('%DESKTOP_LINK%'); $desktop.TargetPath = '%INSTALL_DIR%\zmd.exe'; $desktop.WorkingDirectory = '%INSTALL_DIR%'; $desktop.Save()"

start "" "%INSTALL_DIR%\zmd.exe"
endlocal
