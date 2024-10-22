#include <stddef.h>
#include <stdint.h>

enum elf_section_type {
    ELF_ST_PROGBITS,
    ELF_ST_NOBITS,
    ELF_ST_OTHER
};

struct elf_section
{
    char *name;
    uint32_t addr, rom_offset, size;
    enum elf_section_type type;
};

extern struct elf_section *elf_sections;
extern size_t num_elf_sections;

void read_elf_metadata(void);

struct elf_section *elf_find_section(const char *name);
