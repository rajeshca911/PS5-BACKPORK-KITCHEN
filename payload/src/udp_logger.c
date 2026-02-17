/**
 * udp_logger.c — Lightweight UDP log sender for PS5 payloads.
 *
 * Sends UTF-8 text messages to a remote UDP server (e.g. udp_log_server.py)
 * using the BSD socket API available in PS5 userland.
 *
 * Thread-safety: basic (no mutex; assumes single-threaded payload).
 */

#include "ps5_payload.h"

#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <stdarg.h>
#include <string.h>

/* Compiled with -nostdlib; vsnprintf is provided by the payload SDK or
 * a minimal libc shim bundled with the PS5 Payload SDK. */
extern int vsnprintf(char *buf, size_t size, const char *fmt, va_list ap);

/* -------------------------------------------------------------------------
 * Module state
 * ---------------------------------------------------------------------- */

static int                  g_sock   = -1;
static struct sockaddr_in   g_server;

/* Maximum UDP datagram we will send (stay well under typical MTU 1472). */
#define LOG_BUF_SIZE 512

/* -------------------------------------------------------------------------
 * Public API
 * ---------------------------------------------------------------------- */

void udp_log_init(const char *server_ip, uint16_t port)
{
    /* Close any previous socket. */
    if (g_sock >= 0) {
        close(g_sock);
        g_sock = -1;
    }

    g_sock = socket(AF_INET, SOCK_DGRAM, 0);
    if (g_sock < 0)
        return;

    memset(&g_server, 0, sizeof(g_server));
    g_server.sin_family = AF_INET;
    g_server.sin_port   = htons(port);
    inet_pton(AF_INET, server_ip, &g_server.sin_addr);
}

void udp_log(const char *fmt, ...)
{
    if (g_sock < 0 || !fmt)
        return;

    char buf[LOG_BUF_SIZE];
    va_list ap;
    va_start(ap, fmt);
    int n = vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);

    if (n <= 0)
        return;
    if (n >= (int)sizeof(buf))
        n = (int)sizeof(buf) - 1;

    sendto(g_sock, buf, (size_t)n, 0,
           (const struct sockaddr *)&g_server, sizeof(g_server));
}

void udp_log_close(void)
{
    if (g_sock >= 0) {
        close(g_sock);
        g_sock = -1;
    }
}
