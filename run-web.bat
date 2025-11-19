@echo off
echo.
echo ===============================================
echo   Starting Komputa Web Chat Interface
echo ===============================================
echo.
echo The web interface will be available at:
echo   http://localhost:5000
echo.
echo Press Ctrl+C to stop the server
echo.

cd src\Komputa.Presentation.WebAPI
dotnet run
