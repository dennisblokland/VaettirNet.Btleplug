#!/bin/bash

set -x

DLD=$(mktemp -d)
RUNDIR="$(dirname "$(readlink -f "$0")")"
RUNTIMES="$RUNDIR/VaettirNet.Btleplug/runtimes"
VERSION=$(< $RUNDIR/btleplug-c.version.txt)

curl -L "https://github.com/dennisblokland/btleplug-c/releases/download/$VERSION/binaries.tar.gz" --fail -o $DLD/binaries.tar.gz
mkdir -p $DLD/extracted
tar -xf $DLD/binaries.tar.gz --directory $DLD/extracted
mkdir -p $RUNTIMES/win-x64/
cp -rf $DLD/extracted/target/win-x64/x86_64-pc-windows-msvc/release/* -t $RUNTIMES/win-x64/
mkdir -p $RUNTIMES/linux-x64/
cp -rf $DLD/extracted/target/linux-x64/x86_64-unknown-linux-gnu/release/* -t $RUNTIMES/linux-x64/
mkdir -p $RUNTIMES/linux-arm64/
cp -rf $DLD/extracted/target/linux-arm64/aarch64-unknown-linux-gnu/release* -t $RUNTIMES/linux-arm64/
mkdir -p $RUNTIMES/osx-arm64/
cp -rf $DLD/extracted/target/macos-arm64/aarch64-apple-darwin/release/* -t $RUNTIMES/osx-arm64/
mkdir -p $RUNTIMES/osx-x64/
cp -rf $DLD/extracted/target/macos-x64/x86_64-apple-darwin/release/* -t $RUNTIMES/osx-x64/
rm -r "$DLD"