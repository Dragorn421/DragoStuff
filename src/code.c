// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#include <stdlib.h>

#include "libdragon.h"

#include "elffs.h"

int gCode1int = 1;

extern char str_bar1[], str_bar2[], str_foo[];
#include "assets/mystrings/mystrings.h"

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
    return 0;
}
