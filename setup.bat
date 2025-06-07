SET DLD=%TEMP%\%RANDOM%%RANDOM%
SET RUNTIMES=%~dp0VaettirNet.Btleplug\runtimes
SET /P VERSION=<%~dp0\btleplug-c.version.txt
mkdir %DLD%
curl -L https://github.com/ChadNedzlek/btleplug-c/releases/download/%VERSION%/binaries.tar.gz --fail -o %DLD%\binaries.tar.gz
mkdir %DLD%\extracted
tar -xf %DLD%\binaries.tar.gz --directory %DLD%\extracted
mkdir %RUNTIMES%\win-x64\ 2>nul
copy /Y %DLD%\extracted\target\win-x64\x86_64-pc-windows-msvc\release\* %RUNTIMES%\win-x64\
mkdir %RUNTIMES%\linux-x64\ 2>nul
copy /Y %DLD%\extracted\target\linux-x64\x86_64-unknown-linux-gnu\release\* %RUNTIMES%\linux-x64\
mkdir %RUNTIMES%\linux-arm64\ 2>nul
copy /Y %DLD%\extracted\target\linux-arm64\aarch64-unknown-linux-gnu\release* %RUNTIMES%\linux-arm64\
mkdir %RUNTIMES%\osx-arm64\ 2>nul
copy /Y %DLD%\extracted\target\macos-arm64\aarch64-apple-darwin\release\* %RUNTIMES%\osx-arm64\
mkdir %RUNTIMES%\osx-x64\ 2>nul
copy /Y %DLD%\extracted\target\macos-x64\x86_64-apple-darwin\release\* %RUNTIMES%\osx-x64\
rmdir /S /Q %DLD%