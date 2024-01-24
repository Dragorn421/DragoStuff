All patches are to be applied on top of a OoT64 NTSC USA 1.0 retail rom (md5 hash `5bd1fe107bf8106b2ab6650abecd54d6`).

The primary location to download the patches from is https://hylianmodding.com/competition-2023

If you are using Linux or have WSL here are some commands to download and build the [Flips](https://github.com/Alcaro/Flips) patcher and apply all patches:

```sh
mkdir HMcomp2023hacks
cd HMcomp2023hacks
git clone git@github.com:Alcaro/Flips.git
cd Flips
make -j8
cd ..
# put oot_usa_1_0.z64 in HMcomp2023hacks folder
# check rom:
md5sum oot_usa_1_0.z64
# 5bd1fe107bf8106b2ab6650abecd54d6  oot_usa_1_0.z64
# put bps files in HMcomp2023hacks folder
find . -name '*.bps' -exec ./Flips/flips --apply {} oot_usa_1_0.z64 \;
```
