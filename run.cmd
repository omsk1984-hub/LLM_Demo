@echo off
echo ================================
echo  LLM Demo - Full Stack запуск
echo ================================
echo.
echo [1/2] Установка зависимостей фронтенда...
cd /d "%~dp0src\LLM_Demo.Frontend"
call npm install
if %errorlevel% neq 0 (
    echo Ошибка при установке npm зависимостей
    exit /b %errorlevel%
)
echo.
echo [2/2] Запуск API + Frontend...
echo   - Backend:  http://localhost:5023
echo   - Frontend: http://localhost:5173
echo.
start "LLM_Demo_API" cmd /c "cd /d "%~dp0" && dotnet run --project src\LLM_Demo.Api"
start "LLM_Demo_Frontend" cmd /c "cd /d "%~dp0src\LLM_Demo.Frontend" && npm run dev"
echo.
echo Оба сервера запущены. Закройте окна терминалов для остановки.
pause
