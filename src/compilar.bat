@echo off
rem Recompila o Digitalizar.exe usando o compilador que ja vem no Windows (nada a instalar)
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /out:Digitalizar.exe /win32icon:Digitalizar.ico /resource:Digitalizar.ico,Digitalizar.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll /r:System.Core.dll Comum.cs Controles.cs Editor.cs Janela.cs
if errorlevel 1 (pause & exit /b 1)
echo Pronto: Digitalizar.exe
pause
