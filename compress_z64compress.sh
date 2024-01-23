#!/bin/sh
./z64compress/z64compress \
--in "./oot_u10.decompressed.z64" \
--out "oot_u10.recompressed_z64compress.z64" \
--matching --mb 32 --codec yaz \
--dma "0x7430,1526" \
--compress "0-END" \
--skip "0" --skip "1" --skip "2" --skip "3" --skip "4" \
--skip "5" --skip "6" --skip "7" --skip "8" --skip "9" \
--skip "15" --skip "16" --skip "17" --skip "18" --skip "19" \
--skip "20" --skip "21" --skip "22" --skip "23" --skip "24" \
--skip "25" --skip "26" --skip "942" --skip "944" --skip "946" \
--skip "948" --skip "950" --skip "952" --skip "954" --skip "956" \
--skip "958" --skip "960" --skip "962" --skip "964" --skip "966" \
--skip "968" --skip "970" --skip "972" --skip "974" --skip "976" \
--skip "978" --skip "980" --skip "982" --skip "984" --skip "986" \
--skip "988" --skip "990" --skip "992" --skip "994" --skip "996" \
--skip "998" --skip "1000" --skip "1002" --skip "1004" --skip "1510" \
--skip "1511" --skip "1512" --skip "1513" --skip "1514" --skip "1515" \
--skip "1516" --skip "1517" --skip "1518" --skip "1519" --skip "1520" \
--skip "1521" --skip "1522" --skip "1523" --skip "1524" --skip "1525"

diff oot_u10.z64 oot_u10.recompressed_z64compress.z64 -s
