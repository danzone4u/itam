@echo off
title ITAM Application Server
echo ================================================
echo   ITAM - Inventory & Asset Management System
echo ================================================
echo.
echo Memulai aplikasi...
echo Buka browser dan akses: http://localhost:5000
echo.
echo Tekan CTRL+C untuk menghentikan server.
echo ================================================
cd /d "%~dp0"
dotnet run --urls "http://0.0.0.0:5000"
pause
