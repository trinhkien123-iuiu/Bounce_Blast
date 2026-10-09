@echo off
chcp 65001 >nul
rem Kéo thả file .xapk / .apk (hoặc thư mục Data) vào file này.
rem Kết quả nằm ở thư mục "<tên file>_shaders" cạnh file game.
if "%~1"=="" (
  echo Cach dung: keo tha file .xapk / .apk vao rip_shaders.bat
  pause
  exit /b 1
)
python -m pip show UnityPy >nul 2>&1 || python -m pip install UnityPy
python "%~dp0shader_ripper.py" "%~1" -o "%~dpn1_shaders" --skip-builtin
pause
