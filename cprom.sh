#!/bin/sh
set -x

cp "$1" ./oot_u10.z64

md5sum --check oot_u10.z64_checksum_md5.txt
