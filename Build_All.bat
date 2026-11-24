@echo off
echo ========================================================
echo   Building Granny 3D: Single Portable EXE & Android APK
echo ========================================================

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2020.2.0f1\Editor\Unity.exe"
set PROJECT_PATH="%~dp0"

echo [1/3] Checking Unity process...
tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I /N "Unity.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo.
    echo NOTE: Unity Editor is currently open!
    echo To build while Unity Editor is open, simply click in the top menu:
    echo        Build -> 3. Build Both (APK & Single Portable EXE)
    echo.
    echo Or close Unity Editor and run this script again.
    pause
    exit /b
)

echo [2/3] Running Unity Batchmode Build...
%UNITY_PATH% -batchmode -quit -projectPath %PROJECT_PATH% -executeMethod ProjectAutoBuilder.BuildAllBatch -logFile "%~dp0build.log"

echo [3/3] Checking output...
if exist "%~dp0Builds\Granny_Game_PC.exe" (
    echo [SUCCESS] Windows Single Portable EXE built: Builds\Granny_Game_PC.exe
)
if exist "%~dp0Builds\Granny_Mobile.apk" (
    echo [SUCCESS] Android APK built: Builds\Granny_Mobile.apk
)

echo.
echo Opening Builds folder...
explorer "%~dp0Builds"

pause
