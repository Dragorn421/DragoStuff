#!/bin/sh
my_time () {
    /bin/time -f '%Uuser %Ssystem %Eelapsed' -a -o times_out.txt $@
}
echo Start > times_out.txt
my_time ./compress_pyromcompress_crunch64.sh
my_time ./compress_z64compress.sh
my_time ./compress_pyromcompress_crunch64.sh
my_time ./compress_z64compress.sh
my_time ./compress_pyromcompress_crunch64.sh
my_time ./compress_z64compress.sh
my_time ./compress_pyromcompress_crunch64.sh
my_time ./compress_z64compress.sh
my_time ./compress_pyromcompress_crunch64.sh
my_time ./compress_z64compress.sh
echo End >> times_out.txt
