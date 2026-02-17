/**
 * main.c — PS5 payload entry point.
 *
 * Loaded and executed by a PS5 exploit / payload loader.
 * Initialises the UDP logger and installs the module-redirect hook.
 *
 * Configure LOG_SERVER_IP to the IPv4 address of the PC running
 * scripts/udp_log_server.py before building.
 */

#include "ps5_payload.h"

/* ---- Configuration ------------------------------------------------------- */

/**
 * IP address of the PC running udp_log_server.py.
 * Change this before flashing the payload.
 */
#define LOG_SERVER_IP   "192.168.1.100"

/** UDP port — must match --port on udp_log_server.py (default 9090). */
#define LOG_SERVER_PORT 9090

/* ---- Entry point --------------------------------------------------------- */

int _main(void)
{
    /* 1. Bring up the UDP logger first so every subsequent step is visible. */
    udp_log_init(LOG_SERVER_IP, LOG_SERVER_PORT);
    udp_log("[PAYLOAD] PS5 Module Redirector v1.0 starting\n");
    udp_log("[PAYLOAD] Log target: %s:%d\n", LOG_SERVER_IP, LOG_SERVER_PORT);

    /* 2. Install the /common/lib/ → /app0/fakelib/ hook. */
    udp_log("[PAYLOAD] Installing module redirect hook ...\n");
    install_module_redirect_hook();

    udp_log("[PAYLOAD] All hooks active — payload running\n");
    return 0;
}
