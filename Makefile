LDSCRIPT := ldscript.ld
BUILD_DIR := build

OBJCOPY := mips-linux-gnu-objcopy

ifeq ($(IDO7RECOMP),)
  $(error No path to ido 7.1 recomp set. Export IDO7RECOMP as an environment variable or pass it to make.)
endif

IDO := $(IDO7RECOMP)

DEFINES :=
ifeq ($(EXPECTED),1)
  DEFINES := -DEXPECTED
endif

LD := mips-linux-gnu-ld
LDFLAGS := -T $(LDSCRIPT) -Map $(BUILD_DIR)/rom.map

default: $(BUILD_DIR)/rom.bin
.PHONY: default clean

clean:
	$(RM) -r $(BUILD_DIR)

$(BUILD_DIR)/%.o: %.c
	@mkdir -p $(dir $@)
	$(IDO) $(DEFINES) -c -G 0 -non_shared -fullwarn -verbose -Xcpluscomm -Wab,-r4300_mul -mips2 -O2 -woff 649,838 -o $@ $<

$(BUILD_DIR)/rom.elf: $(LDSCRIPT) $(BUILD_DIR)/src/test.o
	@mkdir -p $(dir $@)
	$(LD) $(LDFLAGS) -o $@

$(BUILD_DIR)/rom.bin: $(BUILD_DIR)/rom.elf
	@mkdir -p $(dir $@)
	$(OBJCOPY) -O binary $< $@
