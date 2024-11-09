@echo off
rmdir /q /s src\UniGetUI\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\publish\
dotnet publish src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64 -v m
%signcommand% "src\UniGetUI\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\publish\UniGetUI.exe"
src\UniGetUI\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\publish\UniGetUI.exe
pause