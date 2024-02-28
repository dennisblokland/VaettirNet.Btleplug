SET DLD=%TEMP%\%RANDOM%%RANDOM%
SET RUNTIMES=%~dp0VaettirNet.Btleplug\runtimes
SET VERSION=0.0.4
mkdir %DLD%
curl -L https://github.com/ChadNedzlek/btleplug-c/releases/download/%VERSION%/binaries.zip --fail -o %DLD%\binaries.zip
mkdir %DLD%\extracted
tar -xf %DLD%\binaries.zip --directory %DLD%\extracted
mkdir %RUNTIMES%\win-x64\ 2>nul
copy /Y %DLD%\extracted\x86_64-pc-windows-gnu\release\* %RUNTIMES%\win-x64\
mkdir %RUNTIMES%\linux-x64\ 2>nul
copy /Y %DLD%\extracted\x86_64-unknown-linux-gnu\release\* %RUNTIMES%\linux-x64\
rmdir /S /Q %DLD%