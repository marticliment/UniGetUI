@echo off
rmdir /q /s src\UniGetUI\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\publish\
dotnet publish src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64 -v m
python3 scripts\generate_integrity_tree.py %cd%\src\UniGetUI\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\publish\
echo %cd%\src\UniGetUI\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\publish\
src\UniGetUI\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\publish\UniGetUI.exe
pause