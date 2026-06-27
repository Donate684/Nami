@echo off
echo Starting Release build...

:: Clean previous builds
if exist "out\Release" rmdir /s /q "out\Release"

:: Publish the application
:: -c Release : Builds in release configuration
:: -r win-x64 : Targets Windows 64-bit
:: --self-contained true : Includes the .NET runtime so the user doesn't need to install it
:: -p:PublishSingleFile=true : Packages everything into a single .exe
:: -p:IncludeNativeLibrariesForSelfExtract=true : Ensures native DLLs (like MediaInfo) work in a single file
:: -o out\Release : Outputs to the out\Release folder

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o out\Release

if %errorlevel% neq 0 (
    echo.
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo Build successful! The release files are located in the "out\Release" folder.
pause
