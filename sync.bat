@echo off
echo ==============================================
echo       Syncing Nami with GitHub
echo       https://github.com/Donate684/Nami
echo ==============================================
echo.

echo Adding changed files...
git add .
echo.

git commit -m "Update"
echo.

echo Pushing changes to server...
git push origin master
echo.

echo ==============================================
echo Sync completed!
echo ==============================================
pause
