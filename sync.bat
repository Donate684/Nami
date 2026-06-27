@echo off
chcp 65001 >nul
echo ==============================================
echo       Синхронизация Nami с GitHub
echo       https://github.com/Donate684/Nami
echo ==============================================
echo.

echo Добавляем измененные файлы...
git add .
echo.

git commit -m "Update"
echo.

echo Отправляем изменения на сервер...
git push origin master
echo.

echo ==============================================
echo Синхронизация завершена!
echo ==============================================
pause
