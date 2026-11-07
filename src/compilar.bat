@echo off
rem Recompila o Digitalizar.exe usando o compilador que ja vem no Windows (nada a instalar)
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
set RES=/resource:Digitalizar.ico,Digitalizar.ico
for %%t in (16 20 24 32 40 48 64) do call set RES=%%RES%% /resource:icones\Digitalizar_%%t.png,ico_%%t.png
"%CSC%" /nologo /target:winexe /optimize+ /out:Digitalizar.exe /win32icon:Digitalizar.ico %RES% /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.dll Tema.cs Moldura.cs Controles.cs Comum.cs Editor.cs Opcoes.cs Sobre.cs Janela.cs
if errorlevel 1 (pause & exit /b 1)
echo Pronto: Digitalizar.exe
pause
