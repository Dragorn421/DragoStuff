# SPDX-FileCopyrightText: 2024 Dragorn421
# SPDX-License-Identifier: CC0-1.0

export N64_INST := build/libdragon
$(info Using N64_INST = $(N64_INST))
ifeq ($(wildcard $(N64_INST)/include/n64.mk),)
  $(error run ./build_libdragon.sh)
endif

BUILD_DIR := build
ROM := poc.z64
ELF := $(BUILD_DIR)/$(ROM:.z64=.elf)

all: $(ROM)

include $(N64_INST)/include/n64.mk

code_SRCS := $(wildcard src/*.c)
code_OBJS := $(foreach f,$(code_SRCS),$(BUILD_DIR)/$(f:.c=.o))
assets_SRCS := $(shell find assets -name '*.c')
assets_OBJS := $(foreach f,$(assets_SRCS),$(BUILD_DIR)/$(f:.c=.o))

CFLAGS += -G0 -I.

$(ELF): build/ldscript.ld $(code_OBJS) $(assets_OBJS)
	@mkdir -p $(dir $@)
	@echo "    [LD] $@"
	$(N64_CXX) -o $@ $(code_OBJS) $(assets_OBJS) -lc -Tbuild/ldscript.ld $(patsubst %,-Wl$(COMMA)%,$(filter-out -Tn64.ld,$(LDFLAGS))) -Wl,-Map=$(ELF:.elf=.map)
	$(N64_SIZE) -G $@

build/ldscript.ld: mkldscript.py
	@echo "    [mkldscript] $@"
	python3 mkldscript.py
