@echo off
echo ============================================
echo   VK Video Desktop - Installer
echo ============================================
echo.
echo Установка MSIX пакета...
echo.

powershell -Command "Add-AppxPackage -Path '%~dp0VKVideoDesktop.msix' -AllowUnsigned"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Установка завершена успешно!
    echo Приложение доступно в меню "Пуск".
    echo.
    echo Для удаления: Settings > Apps > VK Video Desktop > Uninstall
) else (
    echo.
    echo Ошибка установки. Попробуйте:
    echo 1. ПКМ на VKVideoDesktop.msix > Install
    echo 2. Или: powershell -Command "Add-AppxPackage -Path 'VKVideoDesktop.msix'"
)

pause
