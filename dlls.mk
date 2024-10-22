$(BUILD_DIR)/src/dlls/myso/dll.partial.o: $(BUILD_DIR)/src/dlls/myso/myso.o $(BUILD_DIR)/src/dlls/myso/myso_function.o
dlls_OBJS += $(BUILD_DIR)/src/dlls/myso/dll.o

$(BUILD_DIR)/src/dlls/myso_hello/dll.partial.o: $(BUILD_DIR)/src/dlls/myso_hello/myso_hello.o
dlls_OBJS += $(BUILD_DIR)/src/dlls/myso_hello/dll.o
