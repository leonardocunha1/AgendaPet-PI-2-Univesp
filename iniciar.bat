@echo off
REM Inicia o sistema. Depois abra http://localhost:5000 no navegador.
cd /d "%~dp0src\AgendaPet.Api"
start "" http://localhost:5000
dotnet run -c Release
