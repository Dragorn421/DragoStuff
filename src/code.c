// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#include <stdlib.h>

#include "libdragon.h"

#include "elffs.h"
#include "elfreader.h"

int gCode1int = 1;

extern char str_bar1[], str_bar2[], str_foo[];
#include "assets/mystrings/mystrings.h"

extern void myso(void);
extern void myso_hello(void);

char code_string[] = "code_string\n";
char code_string_percents[] = "%p\n";

void *dll_load(const char *name);

int main()
{
    console_init();
    debug_init(DEBUG_FEATURE_ALL);

    printf("abc\n");
    printf("%p %p %p\n", str_bar1, str_bar2, str_foo);

    elffs_init();

    FILE *f = fopen("elf:/.assets.afoo", "r");
    if (f == NULL)
    {
        perror("fopen");
    }
    else
    {
        fclose(f);
    }

    uintptr_t gSegments[16];
#define SEGMENT_OFFSET(a) ((uintptr_t)(a) & 0x00FFFFFF)
#define SEGMENT_NUMBER(a) (((uintptr_t)(a) << 4) >> 28)
#define SEGMENTED_TO_VIRTUAL(addr) (void *)(gSegments[SEGMENT_NUMBER(addr)] + SEGMENT_OFFSET(addr) + (uintptr_t)KSEG0_START_ADDR)

    void *assets_abar = asset_load("elf:/.assets.abar", NULL);
    gSegments[1] = (uintptr_t)PhysicalAddr(assets_abar);
    printf("str_bar1 p= %p\n", SEGMENTED_TO_VIRTUAL(str_bar1));
    printf("str_bar1 s= %s\n", (char *)SEGMENTED_TO_VIRTUAL(str_bar1));

    gSegments[2] = (uintptr_t)PhysicalAddr(asset_load("elf:/.assets.mystrings", NULL));
    struct thedevs *data = SEGMENTED_TO_VIRTUAL(thedevs_data);
    int n_data = *(int *)SEGMENTED_TO_VIRTUAL(&thedevs_data_count);

    printf("thedevs_data = %p\n", thedevs_data);
    printf("&thedevs_data_count = %p\n", &thedevs_data_count);
    printf("n_data = %d\n", n_data);

    for (int i = 0; i < n_data; i++)
    {
        printf("%d %s\n", i, (char *)SEGMENTED_TO_VIRTUAL(data[i].category));
        char **devs = SEGMENTED_TO_VIRTUAL(data[i].devs);
        for (int j = 0; j < data[i].ndevs; j++)
        {
            printf(" - %s\n", (char *)SEGMENTED_TO_VIRTUAL(devs[j]));
        }
    }

    void *myso_start = dll_load("myso");

    printf("> myso\n");
    ((void (*)(void))((uintptr_t)myso - 0x80800000 + (uintptr_t)myso_start))();
    printf("< myso\n");

    free(myso_start);

    return 0;
}

//#define DLL_LOAD_VERBOSE
void *dll_load(const char *name)
{
    char buf[256];
    snprintf(buf, sizeof(buf), "dlls.%s", name);
    struct elf_section *sec_code = elf_find_section(buf);
    snprintf(buf, sizeof(buf), "dlls.bss.%s", name);
    struct elf_section *sec_code_bss = elf_find_section(buf);
    snprintf(buf, sizeof(buf), "dlls.rel.%s", name);
    struct elf_section *sec_rel = elf_find_section(buf);

    uint32_t addr_start = sec_code->addr;
    uint32_t addr_end = sec_code_bss->addr + sec_code_bss->size;
    void *mem = CachedAddr(malloc_uncached(addr_end - addr_start));

    // DMA code
    dma_read(mem, sec_code->rom_offset, sec_code->size);

    // Set code.bss to 0
    memset((char *)mem + (sec_code_bss->addr - addr_start), 0, sec_code_bss->size);

    // Relocation

    uint32_t *rel_data = CachedAddr(malloc_uncached(sec_rel->size));
    dma_read(rel_data, sec_rel->rom_offset, sec_rel->size);

    int32_t s = (uint32_t)mem - addr_start; // mipsabi.pdf says this should be the opposite???
    bool pending_hi = false, met_any_hi = false;
    uint32_t last_hi_offset;
    for (uint32_t i = 0; i < sec_rel->size / 4; i++)
    {
        uint32_t rel_type = rel_data[i] & 3;
        uint32_t rel_offset = rel_data[i] & ~3;
        if (pending_hi)
            assertf(rel_type == 3 /* LO16 */, "orphaned HI16");
        switch (rel_type)
        {
        case 0: // 32
        {
            uint32_t *word_p = (uint32_t *)((uintptr_t)mem + rel_offset);
            uint32_t a = *word_p;
            *word_p = a + s;
#ifdef DLL_LOAD_VERBOSE
            printf("32 a=%lx *word_p=%lx\n", a, *word_p);
#endif
        }
        break;
        case 1: // 26
        {
            uint32_t *instr_p = (uint32_t *)((uintptr_t)mem + rel_offset);
            uint32_t a = *instr_p & 0x03FFFFFF;
            uint32_t relocated_a = ((a << 2) + s) >> 2;
            *instr_p = (*instr_p & 0xFC000000) | (relocated_a & 0x03FFFFFF);
#ifdef DLL_LOAD_VERBOSE
            printf("26 a<<2=%lx relocated_a<<2=%lx\n", a << 2, relocated_a << 2);
#endif
        }
        break;
        case 2: // HI16
#ifdef DLL_LOAD_VERBOSE
            printf("HI16\n");
#endif
            met_any_hi = true;
            pending_hi = true;
            last_hi_offset = rel_offset;
            break;
        case 3: // LO16
            assert(met_any_hi);
            uint16_t *ahi_p = (uint16_t *)((uintptr_t)mem + last_hi_offset + 2);
            int16_t *alo_p = (int16_t *)((uintptr_t)mem + rel_offset + 2);
            uint16_t ahi = *ahi_p;
            int16_t alo = *alo_p;
            uint32_t ahl = (ahi << 16) + alo;
            if (pending_hi)
                *ahi_p = ((ahl + s) - (int16_t)(ahl + s)) >> 16;
            *alo_p = (int16_t)(ahl + s);
#ifdef DLL_LOAD_VERBOSE
            printf("LO16 last_hi_offset=%lx rel_offset=%lx\n", last_hi_offset, rel_offset);
            printf("ahi=%hx alo=%hx ahl=%lx s=%lx ahl+s=%lx\n", ahi, alo, ahl, s, ahl + s);
            printf("*ahi_p=%hx *alo_p=%hx\n", *ahi_p, *alo_p);
#endif
            pending_hi = false;
            break;
        }
    }

    free(rel_data);

    data_cache_hit_writeback(mem, addr_end - addr_start);

    return mem;
}
