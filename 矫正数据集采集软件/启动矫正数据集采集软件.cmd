@echo off
setlocal
set "APP=%~dp0发布版\矫正数据集采集软件.exe"
if not exist "%APP%" (
  echo 未找到发布版程序：%APP%
  echo 请先编译或重新生成发布版。
  pause
  exit /b 1
)
start "" "%APP%"
endlocal
