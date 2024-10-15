#include "libdragon.h"

#include "assets/mystrings.h"

int main()
{
    int ret;
    ret = dfs_init(DFS_DEFAULT_LOCATION);
    assert(ret == 0);

    void *mystrings_start = asset_load("rom:/mystrings.bin", NULL);
#define MYSTRINGS_PTR(offset) (void *)((uintptr_t)mystrings_start + (uintptr_t)(offset))
    struct thedevs *data = MYSTRINGS_PTR(thedevs_data);
    int n_data = *(int *)MYSTRINGS_PTR(&thedevs_data_count);

    debug_init(DEBUG_FEATURE_ALL);
    console_init();

    printf("thedevs_data = %p\n", thedevs_data);
    printf("&thedevs_data_count = %p\n", &thedevs_data_count);
    printf("n_data = %d\n", n_data);

    for (int i = 0; i < n_data; i++)
    {
        printf("%d %s\n", i, (char *)MYSTRINGS_PTR(data[i].category));
        char **devs = MYSTRINGS_PTR(data[i].devs);
        for (int j = 0; j < data[i].ndevs; j++)
        {
            printf(" - %s\n", (char *)MYSTRINGS_PTR(devs[j]));
        }
    }

    return 0;
}
