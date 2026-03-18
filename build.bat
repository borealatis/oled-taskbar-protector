@echo off
setlocal enabledelayedexpansion

echo.
echo  ================================================
echo   OLED Dimmer - Derleniyor...
echo  ================================================
echo.

:: OledDimmer.cs ayni klasorde mi?
if not exist "%~dp0OledDimmer.cs" (
    echo  [HATA] OledDimmer.cs bulunamadi!
    echo  build.bat ile OledDimmer.cs ayni klasorde olmali.
    echo.
    echo  Simdi bu klasordesiniz: %~dp0
    pause
    exit /b 1
)

:: csc.exe ara
set CSC=
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"   if "!CSC!"=="" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

:: Bulunamadiysa daha genis ara
if "%CSC%"=="" (
    for /r "%WINDIR%\Microsoft.NET" %%f in (csc.exe) do (
        if "!CSC!"=="" set "CSC=%%f"
    )
)

if "%CSC%"=="" (
    echo  [HATA] .NET Framework bulunamadi.
    echo  Lutfen su adresten indirip yukleyin:
    echo  https://dotnet.microsoft.com/download/dotnet-framework/net48
    echo.
    pause
    exit /b 1
)

echo  Derleyici bulundu: %CSC%
echo  Derleniyor...
echo.

cd /d "%~dp0"

"%CSC%" /target:winexe /platform:anycpu /optimize+ /out:OledDimmer.exe /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll OledDimmer.cs

if %ERRORLEVEL%==0 (
    echo.
    echo  ================================================
    echo   BASARILI! OledDimmer.exe olusturuldu.
    echo  ================================================
    echo.
    start "" "%~dp0OledDimmer.exe"
    echo  Program baslatildi! Sistem tepsisine bakin (sag alt).
    echo.
    pause
) else (
    echo.
    echo  ================================================
    echo   DERLEME BASARISIZ - Yukardaki hataya bakin
    echo  ================================================
    echo.
    echo  Bu ekranin fotografini cekip paylasin.
    pause
)
