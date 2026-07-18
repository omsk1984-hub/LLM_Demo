@echo off
cd /d "%~dp0"

rem ========================================
rem  Применение миграций БД через EF Core
rem ========================================
echo Применение миграций базы данных...
echo.

rem Восстанавливаем локальный dotnet-ef (версия из .config/dotnet-tools.json)
dotnet tool restore >nul 2>&1

echo Миграция: dotnet ef database update...

dotnet ef database update --project src\LLM_Demo.Infrastructure --startup-project src\LLM_Demo.Api
set "EXIT_CODE=%errorlevel%"

if "%EXIT_CODE%"=="0" (
    echo.
    echo Миграция успешно применена!
    echo.
    pause
    exit /b 0
)

echo.
echo ОШИБКА: Миграция не применилась (exit code: %EXIT_CODE%).
echo.
echo Возможные причины:
echo   - База данных недоступна (проверьте docker-compose)
echo   - Строка подключения некорректна (appsettings.json)
echo.
echo Альтернативный вариант: применить SQL-скрипт вручную:
echo   psql -h localhost -p 5434 -U llm_demo_user -d llm_demo -f sql\migrate_add_connector_fields.sql
echo.
pause
exit /b %EXIT_CODE%
