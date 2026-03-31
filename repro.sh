#!/bin/sh
set -ex

make clean
make EXPECTED=1
rm -r expected
mkdir expected
cp -r build expected/
make clean
make
