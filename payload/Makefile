# Makefile — PS5 Module Redirector payload
#
# Requirements:
#   PS5 Payload SDK installed at $(PS5_PAYLOAD_SDK)
#   LLVM/Clang with AArch64 cross-compilation support
#
# Usage:
#   make                      — build module_redirect.bin
#   make clean                — remove object files and output
#   make PS5_PAYLOAD_SDK=/path/to/sdk

# ---------------------------------------------------------------------------
# Toolchain
# ---------------------------------------------------------------------------

CC      := clang
LD      := $(CC)

TARGET_TRIPLE := aarch64-sie-ps5

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

PS5_PAYLOAD_SDK ?= $(HOME)/ps5-payload-sdk

SDK_INC := $(PS5_PAYLOAD_SDK)/include
SDK_LIB := $(PS5_PAYLOAD_SDK)/lib

INC_DIR := include
SRC_DIR := src
OUT     := module_redirect.bin

# ---------------------------------------------------------------------------
# Compiler / Linker flags
# ---------------------------------------------------------------------------

CFLAGS  := \
    -target $(TARGET_TRIPLE) \
    -fPIC \
    -nostdlib \
    -fno-stack-protector \
    -fno-builtin \
    -Wall \
    -Wextra \
    -O2 \
    -I$(INC_DIR) \
    -I$(SDK_INC)

LDFLAGS := \
    -target $(TARGET_TRIPLE) \
    -nostdlib \
    -shared \
    -L$(SDK_LIB)

LIBS    :=  # add -lSceLibc etc. if the SDK requires them

# ---------------------------------------------------------------------------
# Sources
# ---------------------------------------------------------------------------

SRCS := \
    $(SRC_DIR)/main.c \
    $(SRC_DIR)/udp_logger.c \
    $(SRC_DIR)/module_redirect.c

OBJS := $(SRCS:.c=.o)

# ---------------------------------------------------------------------------
# Rules
# ---------------------------------------------------------------------------

.PHONY: all clean help

all: $(OUT)

$(OUT): $(OBJS)
	$(LD) $(LDFLAGS) -o $@ $^ $(LIBS)
	@echo "[BUILD] $(OUT) ready ($(shell wc -c < $(OUT)) bytes)"

$(SRC_DIR)/%.o: $(SRC_DIR)/%.c
	$(CC) $(CFLAGS) -c -o $@ $<

clean:
	$(RM) $(OBJS) $(OUT)

help:
	@echo "Usage: make [PS5_PAYLOAD_SDK=/path/to/sdk]"
	@echo "  all   — build $(OUT)"
	@echo "  clean — remove build artifacts"
