@echo off
setlocal
set "PROJECT_DIR=%~dp0"
if not exist "%PROJECT_DIR%Assets\OneMoreFloor\Runtime\GameApp.cs" goto missing_project
if not exist "%PROJECT_DIR%ProjectSettings\ProjectVersion.txt" goto missing_project
set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if exist "%UNITY_EXE%" goto launch
if defined ProgramW6432 set "UNITY_EXE=%ProgramW6432%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if exist "%UNITY_EXE%" goto launch
set "UNITY_EXE=D:\6000.6.7f1\Editor\Unity.exe"
if exist "%UNITY_EXE%" goto launch
echo Unity 6000.6.7f1 was not found in the standard installation folder.
echo In Unity Hub: Installs - 6000.6.7f1 - Show in Explorer.
echo Find Editor\Unity.exe and paste its full path below.
set "UNITY_EXE="
set /p "UNITY_EXE=Full path to Unity.exe: "
set "UNITY_EXE=%UNITY_EXE:"=%"
if not exist "%UNITY_EXE%" goto missing_editor
:launch
echo Opening One More Floor in Unity 6000.6.7f1...
echo Wait for the import to finish. Do not launch another copy.
start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%." -logFile "%PROJECT_DIR%Unity-open.log"
exit /b 0
:missing_project
echo Extract the WHOLE ZIP first. Keep this file next to Assets and ProjectSettings.
pause
exit /b 1
:missing_editor
echo Unity.exe was not found. No project files were changed.
pause
exit /b 1
