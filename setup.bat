SET DLD=%TEMP%\%RANDOM%%RANDOM%
SET RUNTIMES=%~dp0\VaettirNet.Btleplug\runtimes
SET VERSION=0.0.2
mkdir %DLD%
curl -L https://github.com/ChadNedzlek/btleplug-c/releases/download/%VERSION%/binaries.zip --fail -o %DLD%\binaries.zip
mkdir %DLD%\extracted
tar -xf %DLD%\binaries.zip --directory %DLD%\extracted
mkdir -p %RUNTIMES%\win-x64\
copy /Y %DLD%\extracted\x86_64-pc-windows-gnu\release\* %RUNTIMES%\win-x64\
mkdir -p %RUNTIMES%\linux-x64\
copy /Y %DLD%\extracted\x86_64-unknown-linux-gnu\release\* %RUNTIMES%\linux-x64\
rmdir /S /Q %DLD%