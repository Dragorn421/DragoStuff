// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

#include <stdlib.h>
#include <stddef.h>
#include <stdio.h>
#include <time.h>

#include "crunch64.h"

void cwrapcrunch64_free(void *ptr)
{
    free(ptr);
}

void *cwrapcrunch64_yaz0_compress(size_t src_size, const void *src, size_t *res_size_p, double *compress_seconds_p)
{
    Crunch64Error cerr;

    size_t dst_size;
    cerr = crunch64_yaz0_compress_bound(&dst_size, src_size, src);
    if (cerr != Crunch64Error_Okay)
    {
        fprintf(stderr, "cwrapcrunch64_yaz0_compress: "
                        "crunch64_yaz0_compress_bound -> err=%d\n",
                (int)cerr);
        return NULL;
    }

    char *dst;
    dst = malloc(dst_size);
    if (dst == NULL)
    {
        fprintf(stderr, "cwrapcrunch64_yaz0_compress: "
                        "dst == NULL\n");
        return NULL;
    }

    struct timespec tstart = {0, 0}, tend = {0, 0};
    clock_gettime(CLOCK_MONOTONIC, &tstart);
    cerr = crunch64_yaz0_compress(&dst_size, dst, src_size, src);
    clock_gettime(CLOCK_MONOTONIC, &tend);
    double compress_seconds = ((double)tend.tv_sec + 1.0e-9 * tend.tv_nsec) -
                              ((double)tstart.tv_sec + 1.0e-9 * tstart.tv_nsec);
    if (compress_seconds_p != NULL)
    {
        *compress_seconds_p = compress_seconds;
    }

    if (cerr != Crunch64Error_Okay)
    {
        fprintf(stderr, "cwrapcrunch64_yaz0_compress: "
                        "crunch64_yaz0_compress -> err=%d\n",
                (int)cerr);
        free(dst);
        return NULL;
    }

    if (res_size_p != NULL)
    {
        *res_size_p = dst_size;
    }
    return dst;
}
