#!/bin/bash

DLD=$(mktemp -d)
RUNTIMES="$(dirname "$(readlink -f "$0")")/VaettirNet.Btleplug/runtimes"
VERSION="0.0.4"

curl -L "https://github.com/ChadNedzlek/btleplug-c/releases/download/$VERSION/binaries.zip" --fail -o $DLD/binaries.zip
mkdir -p $DLD/extracted
unzip -q $DLD/binaries.zip -d $DLD/extracted
mkdir -p $RUNTIMES/win-x64/
cp -r -f -t $RUNTIMES/win-x64/ $DLD/extracted/x86_64-pc-windows-gnu/release/* 
mkdir -p $RUNTIMES/linux-x64/
cp -r -f -t $RUNTIMES/linux-x64/ $DLD/extracted/x86_64-unknown-linux-gnu/release/* 
#rm -r "$DLD"