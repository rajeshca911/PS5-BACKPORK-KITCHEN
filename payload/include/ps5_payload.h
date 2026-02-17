/**
 * ps5_payload.h — Common header for PS5 payload modules.
 *
 * Provides basic PS5/FreeBSD type definitions, syscall stubs,
 * and prototypes for the module-redirect + UDP-logger components.
 *
 * Compile with:
 *   clang -target aarch64-sie-ps5 -fPIC -nostdlib -I include
 */

#pragma once

#include <stdarg.h>
#include <stddef.h>
#include <stdint.h>

/* -------------------------------------------------------------------------
 * Basic PS5 / FreeBSD types
 * ---------------------------------------------------------------------- */

typedef int32_t  SceInt32;
typedef uint32_t SceUInt32;
typedef int64_t  SceInt64;
typedef uint64_t SceUInt64;
typedef uint32_t SceUInt;
typedef int      SceBool;

#ifndef NULL
#define NULL ((void *)0)
#endif

#ifndef TRUE
#define TRUE  1
#define FALSE 0
#endif

/* -------------------------------------------------------------------------
 * Minimal stat structure (FreeBSD 64-bit layout, enough for file_exists)
 * ---------------------------------------------------------------------- */

struct sce_stat {
    uint32_t st_dev;
    uint32_t st_ino;
    uint16_t st_mode;
    uint16_t st_nlink;
    uint32_t st_uid;
    uint32_t st_gid;
    uint32_t st_rdev;
    int64_t  st_size;
    int64_t  st_atime;
    int64_t  st_mtime;
    int64_t  st_ctime;
    int32_t  st_blksize;
    int64_t  st_blocks;
    uint32_t st_flags;
    uint32_t st_gen;
};

/* -------------------------------------------------------------------------
 * sceKernelLoadStartModule flags
 * ---------------------------------------------------------------------- */

#define SCE_KERNEL_LOAD_START_MODULE_FLAG_NONE 0

typedef struct SceKernelLoadModuleOpt {
    size_t   size;
    uint32_t flags;
    uint32_t reserved[4];
} SceKernelLoadModuleOpt;

/* -------------------------------------------------------------------------
 * Function pointer types used by module_redirect.c
 * ---------------------------------------------------------------------- */

typedef int (*LoadStartModule_fn)(
    const char             *name,
    size_t                  args,
    const void             *argp,
    uint32_t                flags,
    const SceKernelLoadModuleOpt *opt,
    int                    *res);

/* -------------------------------------------------------------------------
 * udp_logger.c — public API
 * ---------------------------------------------------------------------- */

/**
 * udp_log_init — initialise the UDP logging socket.
 * @server_ip  IPv4 address of the log server (e.g. "192.168.1.100")
 * @port       UDP port the server listens on (e.g. 9090)
 */
void udp_log_init(const char *server_ip, uint16_t port);

/**
 * udp_log — send a printf-formatted message to the log server.
 * Safe to call before udp_log_init (silently dropped).
 */
void udp_log(const char *fmt, ...);

/**
 * udp_log_close — close the logging socket.
 */
void udp_log_close(void);

/* -------------------------------------------------------------------------
 * module_redirect.c — public API
 * ---------------------------------------------------------------------- */

/**
 * install_module_redirect_hook — patches sceKernelLoadStartModule so that
 * requests for /common/lib/*.prx are transparently redirected to
 * /app0/fakelib/*.prx when the file exists.
 */
void install_module_redirect_hook(void);

/* -------------------------------------------------------------------------
 * Payload entry point (main.c)
 * ---------------------------------------------------------------------- */

int _main(void);
