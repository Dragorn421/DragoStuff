// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

/*
 * Based on https://github.com/DragonMinded/libdragon/blob/b300b568ab8ecd7cdf72164408ed99f1c685b546/src/pifile.c
 */

#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <fcntl.h>
#include <string.h>
#include <sys/stat.h>

#include <libdragon.h>
#include <system.h>

#include "elffs.h"
#include "elfreader.h"

struct elffs_file
{
    uint32_t base; ///< Base address in the PI bus
    int ptr;       ///< Current pointer
    int size;      ///< File size
};

static void *__elffs_open(char *name, int flags)
{
    if (flags != O_RDONLY)
    {
        errno = EACCES;
        return NULL;
    }

    struct elf_section *es = elf_find_section(name);

    if (es == NULL || es->type == ELF_ST_NOBITS)
    {
        errno = EINVAL;
        return NULL;
    }

    struct elffs_file *file = malloc(sizeof(struct elffs_file));
    if (file == NULL)
    {
        errno = ENOMEM;
        return NULL;
    }

    file->base = es->rom_offset;
    file->size = es->size;
    file->ptr = 0;
    return file;
}

static int __elffs_fstat(void *file, struct stat *st)
{
    struct elffs_file *f = file;
    memset(st, 0, sizeof(struct stat));
    st->st_mode = S_IFREG;
    st->st_size = f->size;
    st->st_nlink = 1;
    return 0;
}

static int __elffs_lseek(void *file, int offset, int whence)
{
    struct elffs_file *f = file;
    switch (whence)
    {
    case SEEK_SET:
        f->ptr = offset;
        break;
    case SEEK_CUR:
        f->ptr += offset;
        break;
    case SEEK_END:
        f->ptr = f->size + offset;
        break;
    default:
        errno = EINVAL;
        return -1;
    }

    if (f->ptr < 0)
        f->ptr = 0;
    if (f->ptr > f->size)
        f->ptr = f->size;
    return f->ptr;
}

static int __elffs_read(void *file, uint8_t *buf, int len)
{
    struct elffs_file *f = file;
    if (f->ptr + len > f->size)
        len = f->size - f->ptr;
    if (len <= 0)
        return 0;

    // Check if we can DMA directly to the output buffer
    if ((((f->base + f->ptr) ^ (uint32_t)buf) & 1) == 0)
    {
        data_cache_hit_writeback_invalidate(buf, len);
        dma_read_async(buf, f->base + f->ptr, len);
        dma_wait();
        f->ptr += len;
    }
    else
    {
        // Go through a temporary buffer
        uint8_t *tmp = alloca(512 + 1);
        if ((f->base + f->ptr) & 1)
            tmp++;

        while (len > 0)
        {
            int n = len > 512 ? 512 : len;
            data_cache_hit_writeback_invalidate(tmp, n);
            dma_read_async(tmp, f->base + f->ptr, n);
            dma_wait();
            memcpy(buf, tmp, n);
            buf += n;
            f->ptr += n;
            len -= n;
        }
    }

    return len;
}

static int __elffs_close(void *file)
{
    free(file);
    return 0;
}

static filesystem_t elffs_fs = {
    .open = __elffs_open,
    .fstat = __elffs_fstat,
    .lseek = __elffs_lseek,
    .read = __elffs_read,
    .close = __elffs_close,
};

void elffs_init(void)
{
    read_elf_metadata();
    attach_filesystem("elf:/", &elffs_fs);
}
