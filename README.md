Use segmented addresses symbols in libdragon

A segmented address (or segmented offset, ...) is a value like 0x0YZZZZZZ, where Y is the segment and ZZZZZZ is the offset.

It means it references data at ZZZZZZ bytes into the file denoted by segment number Y

This PoC allows using such addresses with symbols in libdragon, just like is done in other codebases like OoT64 decomp. For example an asset file may contain `char string[] = "hi";` 0xC bytes into the file and be linked at VMA = `0x0100_0000` (segment Y=1), then the symbol `string` takes value `0x0100_000C` and can be used to reference the data given that the asset file is loaded and segment 1 is set to it.

The "dereferencing" of segmented symbols is done with a macro `SEGMENTED_TO_VIRTUAL`. The mapping segment number -> assets file is kept in a `gSegments` array. (see code.c)

In this PoC, the segment for each assets file (= a subfolder of `assets/`) is set in `mkldscript.py`. A custom linker script is needed to link the assets as `(OVERLAYS)` (see mkdlscript.py), and into the elf at the same time as the code, allowing crossreferences between the two. Ideally there would only need to be a supplementary script to the base n64.ld, but the base n64.ld needs modification to ensure no assets are linked into the main code blob (see mkldscript.py using `EXCLUDE_FILE(build/assets/*)`).

At the moment this PoC also needs to not have n64elfcompress run at all, as that tool discards sections as well as data that is not to be loaded on boot. This requires manually editing the installed n64.mk at `build/libdragon/include/n64.mk`.
