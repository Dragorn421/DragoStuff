# crwasmp

A C rewrite of a **tiny** portion of https://github.com/simonlindholm/asm-processor

- replace floats by the IEEE-754 binary representation ("in hex") in CutsceneData
- convert encoding from utf-8 to euc-jp
- run compiler on the processed source

Notably, *not* the main feature of asm-processor `GLOBAL_ASM` :)

It is possible this may only be buildable on Linux (definitely not on Windows, but maybe not on other Unices either)

Dependencies: libuuid, libpcre2

Install dependencies on Ubuntu:

```
apt-get install uuid-dev libpcre2-dev
```
