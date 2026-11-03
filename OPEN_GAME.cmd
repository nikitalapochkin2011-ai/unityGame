@echo off
setlocal
set "PROJECT_DIR=%~dp0"
if not exist "%PROJECT_DIR%Assets\OneMoreFloor\Runtime\GameApp.cs" goto missing_project
if not exist "%PROJECT_DIR%ProjectSettings\ProjectVersion.txt" goto missing_project
set "UNITY_EXE="
if exist "%PROJECT_DIR%.unity-editor-path.txt" set /p "UNITY_EXE=" < "%PROJECT_DIR%.unity-editor-path.txt"
if defined UNITY_EXE if exist "%UNITY_EXE%" goto launch
set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if exist "%UNITY_EXE%" goto launch
if defined ProgramW6432 set "UNITY_EXE=%ProgramW6432%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if exist "%UNITY_EXE%" goto launch
set "UNITY_EXE=D:\6000.6.7f1\Editor\Unity.exe"
if exist "%UNITY_EXE%" goto launch
echo Unity 6000.6.7f1 was not found in the standard installation folders.
echo In Unity Hub: Installs - 6000.6.7f1 - Show in Explorer.
echo Find Editor\Unity.exe and paste its full path below.
echo This path will be remembered for your next launch.
set "UNITY_EXE="
set /p "UNITY_EXE=Full path to Unity.exe: "
set "UNITY_EXE=%UNITY_EXE:"=%"
if not defined UNITY_EXE goto missing_editor
if not exist "%UNITY_EXE%" goto missing_editor
> "%PROJECT_DIR%.unity-editor-path.txt" echo(%UNITY_EXE%
:launch
echo Opening One More Floor and its game scene in Unity 6000.6.7f1...
echo Wait for the import and font preparation. Then press Play in the Editor.
echo If the project is already open, use that Editor window.
start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%." -executeMethod OneMoreFloor.Editor.FirstOpenSetup.OpenFromLauncher -logFile "%PROJECT_DIR%Unity-open.log"
exit /b 0
:missing_project
echo Extract the WHOLE ZIP first. Keep this file next to Assets and ProjectSettings.
pause
exit /b 1
:missing_editor
echo Unity.exe was not found. Install Unity 6000.6.7f1 or check its path.
pause
exit /b 1
