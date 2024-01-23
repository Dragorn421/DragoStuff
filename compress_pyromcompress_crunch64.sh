#!/bin/sh
set -x
python3 py_crunch64_romcompress.py oot_u10.decompressed.z64 oot_u10.recompressed_py.z64
diff oot_u10.z64 oot_u10.recompressed_py.z64 -s
