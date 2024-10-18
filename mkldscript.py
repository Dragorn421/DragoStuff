# SPDX-FileCopyrightText: 2024 Dragorn421
# SPDX-License-Identifier: CC0-1.0

from pathlib import Path

assets = {
    "afoo": 1,
    "abar": 1,
    "mystrings": 2,
}

script_assets = """
    _offset = .;
""" + "".join(
    f"""
    .assets.{_name} {_seg << 24:#08X} (OVERLAY) : AT(_offset) {{
        KEEP(build/assets/{_name}/* (.data* .rodata*))
    }}
    _offset += SIZEOF(.assets.{_name});
    _offset = ALIGN(_offset, 8);
"""
    for _name, _seg in assets.items()
)

Path("build/ldscript.ld").write_text(
    """
/* based on libdragon's n64.ld */

OUTPUT_FORMAT ("elf32-bigmips", "elf32-bigmips", "elf32-littlemips")
OUTPUT_ARCH (mips)
EXTERN (_start)
ENTRY (_start)

PHDRS
{
    irq PT_LOAD AT ( 0x80000000 );
    main PT_LOAD AT ( 0x80000400 );
}

SECTIONS {
    .intvectors 0x80000000 : {
        . = ALIGN(32);
        KEEP(*(.intvectors))
        __intvectors_end = .;
    } :irq

    .text 0x80000400 : {
        EXCLUDE_FILE(build/assets/*) *(.boot)
        . = ALIGN(16);
        __text_start = .;
        EXCLUDE_FILE(build/assets/*) *(.text)
        EXCLUDE_FILE(build/assets/*) *(.text.*)
        EXCLUDE_FILE(build/assets/*) *(.init)
        EXCLUDE_FILE(build/assets/*) *(.fini)
        EXCLUDE_FILE(build/assets/*) *(.gnu.linkonce.t.*)
        . = ALIGN(16);
        __text_end  = .;
    } :main

   .eh_frame_hdr : { EXCLUDE_FILE(build/assets/*) *(.eh_frame_hdr) }
   .eh_frame : { 
		__EH_FRAME_BEGIN__ = .;
		KEEP (EXCLUDE_FILE(build/assets/*) *(.eh_frame)) 
	}
   .gcc_except_table : { EXCLUDE_FILE(build/assets/*) *(.gcc_except_table*) }
   .jcr : { KEEP (EXCLUDE_FILE(build/assets/*) *(.jcr)) }

    .rodata : {
        EXCLUDE_FILE(build/assets/*) *(.rdata)
        EXCLUDE_FILE(build/assets/*) *(.rodata)
        EXCLUDE_FILE(build/assets/*) *(.rodata.*)
        EXCLUDE_FILE(build/assets/*) *(.gnu.linkonce.r.*)
        . = ALIGN(8);
    }

    . = ALIGN(4);

    .ctors : {
        __CTOR_LIST__ = .;
        KEEP(EXCLUDE_FILE(build/assets/*) *(.ctors))
        __CTOR_END__ = .;
    }

    . = ALIGN(8);

    .data : {
        __data_start = .;
        EXCLUDE_FILE(build/assets/*) *(.data)
        EXCLUDE_FILE(build/assets/*) *(.data.*)
        EXCLUDE_FILE(build/assets/*) *(.gnu.linkonce.d.*)
        . = ALIGN(8);
    }

    .sdata : {
        _gp = . + 0x8000;
        EXCLUDE_FILE(build/assets/*) *(.sdata)
        EXCLUDE_FILE(build/assets/*) *(.sdata.*)
        EXCLUDE_FILE(build/assets/*) *(.gnu.linkonce.s.*)
        . = ALIGN(8);
    }

    .lit8 : {
        EXCLUDE_FILE(build/assets/*) *(.lit8)
        . = ALIGN(8);
    }
    .lit4 : {
        EXCLUDE_FILE(build/assets/*) *(.lit4)
        . = ALIGN(8);
    }

    . = ALIGN(8);
    __data_end = .;

    . = ALIGN(8);
    __rom_end = .;

    .sbss (NOLOAD) : {
         __bss_start = .;
        EXCLUDE_FILE(build/assets/*) *(.sbss)
        EXCLUDE_FILE(build/assets/*) *(.sbss.*)
        EXCLUDE_FILE(build/assets/*) *(.gnu.linkonce.sb.*)
        EXCLUDE_FILE(build/assets/*) *(.scommon)
        EXCLUDE_FILE(build/assets/*) *(.scommon.*)
    }

    . = ALIGN(8);
    .bss (NOLOAD) : {
        EXCLUDE_FILE(build/assets/*) *(.bss)
        EXCLUDE_FILE(build/assets/*) *(.bss*)
        EXCLUDE_FILE(build/assets/*) *(.gnu.linkonce.b.*)
        EXCLUDE_FILE(build/assets/*) *(COMMON)
        . = ALIGN(8);
         __bss_end = .;
    }

    . = ALIGN(8);

"""
    + script_assets
    + """

    /DISCARD/ : {
"""
    + "".join(f"        build/assets/{_name}/* (*)\n" for _name in assets.keys())
    + """
    }
}
"""
)
