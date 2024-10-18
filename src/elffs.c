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
#include <malloc.h>
#include <inttypes.h>
#include <elf.h>

#include <libdragon.h>
#include <system.h>

#include "elffs.h"

static uint32_t find_elf_header(void)
{
#define ELF_MAGIC 0x7F454C46
    uint32_t elf_header = 0x10001000;
    bool found_elf = false;
    for (int i = 0; i < 64 * 1024 * 1024 / 256; i++)
    {
        if (io_read(elf_header) == ELF_MAGIC)
        {
            found_elf = true;
            break;
        }
        elf_header += 0x100;
    }
    assert(found_elf);
    printf("elf_header = %08lx\n", elf_header);
    printf("io_read() = %08lx\n", io_read(elf_header));

    return elf_header;
    // return sys_elf_header();
}

struct elf_section
{
    char *name;
    uint32_t rom_offset, size;
};

static struct elf_section *read_elf_header(uint32_t elf_header, void **shstrtab_p, size_t *num_sections_p)
{
    // EI_CLASS
    uint8_t elf_class = io_read(elf_header + 0x4) >> 24;

    uint32_t e_shoff;
    uint16_t e_shentsize, e_shnum, e_shstrndx;

    switch (elf_class)
    {
    case ELFCLASS32:
    {
        Elf32_Ehdr *ehdr = CachedAddr(malloc_uncached(sizeof(Elf32_Ehdr)));
        dma_read(ehdr, elf_header, sizeof(Elf32_Ehdr));

        e_shoff = ehdr->e_shoff;
        e_shentsize = ehdr->e_shentsize;
        e_shnum = ehdr->e_shnum;
        e_shstrndx = ehdr->e_shstrndx;

        free(ehdr);
    }
    break;

    default:
    {
        assertf(false, "only elf32 supported");
        exit(EXIT_FAILURE);
    }
    break;
    }

    printf("e_shoff=%" PRIx32 " e_shentsize=%" PRId16 " e_shnum=%" PRId16 "\n", e_shoff, e_shentsize, e_shnum);

    assertf(e_shstrndx != SHN_UNDEF, "elf has no section name string table (hint: comment out invoking N64_ELFCOMPRESS in n64.mk)");
    assertf(e_shstrndx != SHN_XINDEX, "not implemented logic for real e_shstrndx >= SHN_LORESERVE (see man elf)");
    assert(e_shstrndx < e_shnum);

    /*
     * Copy section name string table to ram
     */

    void *shdr = CachedAddr(malloc_uncached(e_shentsize * e_shnum));
    dma_read(shdr, elf_header + e_shoff, e_shentsize * e_shnum);

    uint32_t shstrtab_offset, shstrtab_size;

    switch (elf_class)
    {
    case ELFCLASS32:
    {
        Elf32_Shdr *shdr32 = shdr;
        Elf32_Shdr *shdr_shstrtab = &shdr32[e_shstrndx];

        shstrtab_offset = shdr_shstrtab->sh_offset;
        shstrtab_size = shdr_shstrtab->sh_size;
    }
    break;

    default:
    {
        assertf(false, "only elf32 supported");
        exit(EXIT_FAILURE);
    }
    break;
    }

    char *shstrtab = CachedAddr(malloc_uncached(shstrtab_size));
    if (shstrtab_p != NULL)
        *shstrtab_p = shstrtab;
    dma_read(shstrtab, elf_header + shstrtab_offset, shstrtab_size);

    struct elf_section *elf_sections = malloc(sizeof(struct elf_section[e_shnum]));
    struct elf_section *es = elf_sections;

    /*
     * Read section headers
     */

    switch (elf_class)
    {
    case ELFCLASS32:
    {
        Elf32_Shdr *shdr32 = shdr;
        for (uint16_t i = 0; i < e_shnum; i++)
        {
            switch (shdr32[i].sh_type)
            {
            case SHT_NULL:
            case SHT_NOBITS:
                continue;
            }
            uint32_t sh_name = shdr32[i].sh_name;
            printf("sh_name = %08" PRIx32 "\n", sh_name);
            printf("[%2" PRId16 "] = `%.*s`\n", i, 32, &shstrtab[sh_name]);
            es->name = &shstrtab[sh_name];
            es->rom_offset = elf_header + shdr32[i].sh_offset;
            es->size = shdr32[i].sh_size;
            es++;
        }
    }
    break;

    default:
    {
        assertf(false, "only elf32 supported");
        exit(EXIT_FAILURE);
    }
    break;
    }

    free(shdr);

    if (num_sections_p != NULL)
        *num_sections_p = es - elf_sections;
    return elf_sections;
}

struct elffs_file
{
    uint32_t base; ///< Base address in the PI bus
    int ptr;       ///< Current pointer
    int size;      ///< File size
};

struct elf_section *elf_sections;
size_t num_sections;

static void *__elffs_open(char *name, int flags)
{
    if (flags != O_RDONLY)
    {
        errno = EACCES;
        return NULL;
    }

    struct elf_section *es = NULL;
    for (size_t i = 0; i < num_sections; i++)
    {
        if (strcmp(name, elf_sections[i].name) == 0)
        {
            es = &elf_sections[i];
            break;
        }
    }

    if (es == NULL)
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
    elf_sections = read_elf_header(find_elf_header(), NULL, &num_sections);
    attach_filesystem("elf:/", &elffs_fs);
}
