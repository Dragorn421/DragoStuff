# SPDX-FileCopyrightText: 2024 Dragorn421
# SPDX-License-Identifier: CC0-1.0

BUILD_DIR := build
C_FILES := $(wildcard *.c)
O_FILES := $(foreach f,$(C_FILES),$(BUILD_DIR)/$(f:.c=.o))
ELF := crwasmp.elf

PCRE2_CODE_UNIT_WIDTH := 8
MACROS := -DINPUT_MUST_BE_UTF8=1 -DPCRE2_CODE_UNIT_WIDTH=$(PCRE2_CODE_UNIT_WIDTH)

CFLAGS ?= -O2 -ggdb3 -Wall

$(ELF): $(O_FILES)
# Note: using ld instead of gcc turns up the error:
# build/process_line.o: undefined reference to symbol 'memmem@@GLIBC_2.2.5'
# idk why
	gcc $(LDFLAGS) -o $@ $^ -luuid -lpcre2-$(PCRE2_CODE_UNIT_WIDTH)

build/%.o: %.c
	@mkdir -p $(dir $@)
	gcc $(CFLAGS) $(MACROS) -c $^ -o $@

clean:
	$(RM) -r $(BUILD_DIR)

mrproper: clean
	$(RM) $(ELF)

.PHONY: clean mrproper
