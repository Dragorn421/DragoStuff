BUILD_DIR := build
ROM := iwish_poc.z64
ELF := $(BUILD_DIR)/$(ROM:.z64=.elf)

all: $(ROM)

include $(N64_INST)/include/n64.mk

code_SRCS := $(shell find src -name '*.c')
code_OBJS := $(foreach f,$(code_SRCS),$(BUILD_DIR)/$(f:.c=.o))
assets_SRCS := $(shell find assets -name '*.c')
assets_OBJS := $(foreach f,$(assets_SRCS),$(BUILD_DIR)/$(f:.c=.o))

CFLAGS += -I.

# based on build/assets/mystrings.map . Update the offsets then touch src/main.c and make
LDFLAGS += --defsym=thedevs_data=0x58
LDFLAGS += --defsym=thedevs_data_count=0

CFLAGS += -G0

$(ELF): $(code_OBJS)

$(ROM): $(BUILD_DIR)/dfs.dfs

$(BUILD_DIR)/dfs.dfs: $(BUILD_DIR)/dfs/mystrings.bin

$(BUILD_DIR)/dfs/mystrings.bin: $(BUILD_DIR)/assets/mystrings.elf
	@mkdir -p $(dir $@)
	@echo "    [OBJCOPY] $<"
	$(N64_OBJCOPY) --only-section=mystrings -O binary $< $@

$(BUILD_DIR)/assets/mystrings.elf: $(BUILD_DIR)/assets/mystrings.o
	@mkdir -p $(dir $@)
	@echo "    [LD] $<"
	$(N64_LD) -Tassets/mystrings.ld -o $@ $< -Map=$(@:.elf=.map)

$(BUILD_DIR)/assets/mystrings.o: assets/mystrings.c
	@mkdir -p $(dir $@)
	@echo "    [CC] $<"
	$(N64_CC) -c $(CFLAGS) $(N64_CFLAGS) -o $@ $<
