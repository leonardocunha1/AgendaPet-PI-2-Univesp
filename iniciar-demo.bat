@echo off
REM Modo DEMONSTRACAO: dados ficticios em um banco separado (agendapet-demo.db).
REM Use para gravar o video. Os dados reais (agendapet.db) nao sao alterados.
cd /d "%~dp0src\AgendaPet.Api"
set Demo=true
start "" http://localhost:5000
dotnet run -c Release
