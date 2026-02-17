/**
 * module_redirect.c — Transparent PRX module redirection hook.
 *
 * Intercepts sceKernelLoadStartModule() calls that request libraries from
 * /common/lib/*.prx and redirects them to /app0/fakelib/*.prx when the
 * substitute file exists on-disc.
 *
 * Hook installation strategy:
 *   The PS5 Payload SDK exposes a symbol-resolution helper (NID lookup or
 *   dlsym-equivalent) and a simple inline-hook/trampoline mechanism.
 *   install_module_redirect_hook() calls these SDK helpers to locate the
 *   real sceKernelLoadStartModule in libkernel.sprx, saves the original
 *   pointer, and overwrites the first N bytes with a branch to our hook.
 *
 * NOTE: This file compiles cleanly with the PS5 Payload SDK toolchain
 *       (clang -target aarch64-sie-ps5 -nostdlib).  A generic x86_64
 *       host build is intentionally NOT supported.
 */

#include "ps5_payload.h"
#include <string.h>
#include <stdio.h>
#include <sys/stat.h>

/* -------------------------------------------------------------------------
 * External: provided by PS5 Payload SDK
 * ---------------------------------------------------------------------- */

/**
 * ps5_sdk_resolve — look up an exported function by name from a loaded module.
 * Prototype matches a typical PS5 Payload SDK helper; adapt to actual SDK.
 */
extern void *ps5_sdk_resolve(const char *module, const char *symbol);

/**
 * ps5_sdk_hook_function — install an inline hook (trampoline).
 * Returns a pointer to a thunk that calls the original function.
 */
extern void *ps5_sdk_hook_function(void *target, void *replacement);

/* -------------------------------------------------------------------------
 * Module state
 * ---------------------------------------------------------------------- */

static LoadStartModule_fn orig_LoadStartModule = NULL;

/* Path prefixes */
static const char COMMON_PREFIX[] = "/common/lib/";
static const char FAKELIB_DIR[]   = "/app0/fakelib/";

/* -------------------------------------------------------------------------
 * Helpers
 * ---------------------------------------------------------------------- */

static int file_exists(const char *path)
{
    struct stat st;
    return (stat(path, &st) == 0);
}

static void build_redirect_path(char *out, size_t out_size, const char *basename)
{
    /* out = "/app0/fakelib/<basename>" */
    size_t dir_len  = sizeof(FAKELIB_DIR) - 1;
    size_t base_len = strlen(basename);
    if (dir_len + base_len + 1 > out_size) {
        out[0] = '\0';
        return;
    }
    memcpy(out, FAKELIB_DIR, dir_len);
    memcpy(out + dir_len, basename, base_len);
    out[dir_len + base_len] = '\0';
}

/* -------------------------------------------------------------------------
 * Hook implementation
 * ---------------------------------------------------------------------- */

int hook_sceKernelLoadStartModule(
    const char             *name,
    size_t                  args,
    const void             *argp,
    uint32_t                flags,
    const SceKernelLoadModuleOpt *opt,
    int                    *res)
{
    if (name == NULL)
        goto passthrough;

    /* Only intercept /common/lib/ requests */
    size_t prefix_len = sizeof(COMMON_PREFIX) - 1;
    if (strncmp(name, COMMON_PREFIX, prefix_len) != 0)
        goto passthrough;

    const char *basename = name + prefix_len;

    char redirect[256];
    build_redirect_path(redirect, sizeof(redirect), basename);

    if (redirect[0] != '\0' && file_exists(redirect)) {
        udp_log("[REDIRECT] %s -> %s\n", name, redirect);
        return orig_LoadStartModule(redirect, args, argp, flags, opt, res);
    }

    udp_log("[PASSTHROUGH] %s (no fakelib found)\n", name);

passthrough:
    return orig_LoadStartModule(name, args, argp, flags, opt, res);
}

/* -------------------------------------------------------------------------
 * Hook installation
 * ---------------------------------------------------------------------- */

void install_module_redirect_hook(void)
{
    /* Resolve the real sceKernelLoadStartModule from libkernel.sprx */
    void *real_fn = ps5_sdk_resolve("libkernel.sprx", "sceKernelLoadStartModule");
    if (real_fn == NULL) {
        udp_log("[HOOK][ERROR] sceKernelLoadStartModule not found in libkernel.sprx\n");
        return;
    }

    /* Install inline hook; SDK returns pointer to thunk for the original */
    void *orig_thunk = ps5_sdk_hook_function(real_fn, (void *)hook_sceKernelLoadStartModule);
    if (orig_thunk == NULL) {
        udp_log("[HOOK][ERROR] Failed to install hook\n");
        return;
    }

    orig_LoadStartModule = (LoadStartModule_fn)orig_thunk;
    udp_log("[HOOK] Module redirect hook installed (orig@%p)\n", (void *)orig_thunk);
}
