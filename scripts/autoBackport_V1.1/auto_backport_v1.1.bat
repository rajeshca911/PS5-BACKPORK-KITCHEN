@echo off
setlocal enabledelayedexpansion

@echo off
REM === Liste des presets disponibles ===
echo.
echo =================================
echo   AutoBackport v1.1 by Markus95
echo =================================
echo.
echo  SDK PS5 DISPONIBLES :
echo.
echo  1.00
echo  2.00
echo  3.00
echo  4.00
echo  5.00
echo  6.00
echo  7.00
echo  8.00
echo  9.00
echo.
echo =================================
echo.

REM === Demande à l'utilisateur de choisir un preset ===
set /p FIRMWARE=Entrez la version du SDK voulu (ex: 4.00 pour 4.03/4.50/4.51) : 
echo.

:: Nettoyage des anciens fichiers
for /r %%f in (*.bin *.prx) do (
    if exist "%%f" (
        echo Suppression de "%%f"
        del /f /q "%%f"
    )
)

for /r %%f in (*.prx.esbak) do (
    set "file=%%~nxf"
    set "new=!file:.prx.esbak=.prx!"
    ren "%%f" "!new!"
)

for /r %%f in (*eboot.bin.esbak) do (
    set "file=%%~nxf"
    set "new=!file:.esbak=!"
    ren "%%f" "!new!"
)

:: Patch du SDK avec le preset
python ps5_sdk_patch.py --ps4_ver 0x09040001 --ps5_preset %FIRMWARE% "%cd%"

echo.
echo PS5 Make Fake Self Script By EchoStretch
echo Requires LightningMods_ Updated Make Fself By Flatz
echo.

set "fself=make_fself_python3-1.py"

cd /d "%cd%"

:: Cryptage des fichiers
FOR /R %%i IN ("*.sprx" "*.prx" "*.elf" "*.self" "*eboot.bin") DO (
    echo Encrypting %%i...
    python %fself% "%%i" "%%i.estemp"
    REN "%%i" "%%~nxi.esbak"
)

:: Renommage des fichiers temporaires
echo.
echo Renaming Temporary Files...
FOR /R %%i IN (*.estemp) DO (
    REN "%%i" "%%~ni"
)

:: Nettoyage des .bak
for /r %%f in (*.bak) do (
    del /f /q "%%f"
)

pause
