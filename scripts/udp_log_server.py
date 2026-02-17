"""
UDP Log Server — Receives and displays log messages from PS5 payloads.

Listens on a UDP port for text messages sent by udp_logger.c (payload).
Colorizes output by detected log level and timestamps each message.

Usage:
  python udp_log_server.py
  python udp_log_server.py --host 0.0.0.0 --port 9090
  python udp_log_server.py --port 9090 --log udp_session.log
  python udp_log_server.py --no-color
"""

import argparse
import asyncio
import datetime
import os
import sys


# ---------------------------------------------------------------------------
# ANSI Colors
# ---------------------------------------------------------------------------

RESET   = "\033[0m"
BOLD    = "\033[1m"
GREEN   = "\033[92m"
YELLOW  = "\033[93m"
RED     = "\033[91m"
CYAN    = "\033[96m"
MAGENTA = "\033[95m"
WHITE   = "\033[97m"
DIM     = "\033[2m"

_USE_COLOR = sys.stdout.isatty()

_LEVEL_COLORS = {
    "ERROR":    RED,
    "ERR":      RED,
    "WARN":     YELLOW,
    "WARNING":  YELLOW,
    "OK":       GREEN,
    "HOOK":     MAGENTA,
    "REDIRECT": CYAN,
    "PAYLOAD":  CYAN,
    "INFO":     WHITE,
}

_LEVEL_PRIORITY = ["ERROR", "ERR", "WARN", "WARNING", "HOOK", "REDIRECT", "PAYLOAD", "OK", "INFO"]


def _detect_level(msg: str) -> str:
    upper = msg.upper()
    for lvl in _LEVEL_PRIORITY:
        if "[{}]".format(lvl) in upper or upper.startswith(lvl + ":"):
            return lvl
    return "INFO"


def _colorize(text: str, color: str) -> str:
    if not _USE_COLOR:
        return text
    return "{}{}{}".format(color, text, RESET)


def _timestamp() -> str:
    return datetime.datetime.now().strftime("%H:%M:%S.%f")[:-3]


# ---------------------------------------------------------------------------
# Log file helper
# ---------------------------------------------------------------------------

class _LogFile:
    def __init__(self, path: str):
        os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
        self._f = open(path, "a", encoding="utf-8")

    def write(self, ts: str, addr: str, msg: str):
        self._f.write("[{}] {} {}\n".format(ts, addr, msg))
        self._f.flush()

    def close(self):
        self._f.close()


# ---------------------------------------------------------------------------
# Async UDP Protocol
# ---------------------------------------------------------------------------

class UDPLogProtocol(asyncio.DatagramProtocol):
    def __init__(self, log_file: _LogFile = None):
        self._log_file = log_file
        self._count = 0

    def datagram_received(self, data: bytes, addr: tuple):
        try:
            msg = data.decode("utf-8", errors="replace").rstrip("\r\n")
        except Exception:
            msg = repr(data)

        ts = _timestamp()
        host = addr[0]
        level = _detect_level(msg)
        color = _LEVEL_COLORS.get(level, WHITE)

        ts_str   = _colorize("[{}]".format(ts), DIM)
        host_str = _colorize("{}:{}".format(host, addr[1]), CYAN if _USE_COLOR else "")
        msg_str  = _colorize(msg, color)

        print("{} {} {}".format(ts_str, host_str, msg_str))

        if self._log_file:
            self._log_file.write(ts, "{}:{}".format(host, addr[1]), msg)

        self._count += 1

    def error_received(self, exc: Exception):
        ts = _timestamp()
        print(_colorize("[{}] [SOCK-ERROR] {}".format(ts, exc), RED))

    def connection_lost(self, exc):
        pass

    @property
    def message_count(self) -> int:
        return self._count


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

async def run_server(host: str, port: int, log_file: _LogFile = None):
    loop = asyncio.get_event_loop()
    protocol_factory = lambda: UDPLogProtocol(log_file=log_file)

    try:
        transport, protocol = await loop.create_datagram_endpoint(
            protocol_factory,
            local_addr=(host, port),
        )
    except OSError as e:
        print(_colorize("[ERROR] Cannot bind {}:{} — {}".format(host, port, e), RED))
        sys.exit(1)

    banner = "{}[UDP LOG SERVER]{} Listening on {}:{} — Ctrl+C to stop".format(
        BOLD, RESET, host, port) if _USE_COLOR else \
        "[UDP LOG SERVER] Listening on {}:{} — Ctrl+C to stop".format(host, port)
    print(banner)

    if log_file:
        print(_colorize("[LOG FILE] Writing to {}".format(log_file._f.name), DIM))

    try:
        await asyncio.sleep(float("inf"))
    except asyncio.CancelledError:
        pass
    finally:
        transport.close()
        if log_file:
            log_file.close()
        print("\n[UDP LOG SERVER] Stopped. Messages received: {}".format(
            protocol.message_count))


def main():
    global _USE_COLOR

    parser = argparse.ArgumentParser(
        description="UDP Log Server — receive PS5 payload logs")
    parser.add_argument("--host", default="0.0.0.0",
                        help="Bind address (default: 0.0.0.0)")
    parser.add_argument("--port", type=int, default=9090,
                        help="UDP port to listen on (default: 9090)")
    parser.add_argument("--log", metavar="FILE",
                        help="Append messages to this log file")
    parser.add_argument("--no-color", action="store_true",
                        help="Disable ANSI color output")
    args = parser.parse_args()

    if args.no_color:
        _USE_COLOR = False

    log_file = _LogFile(args.log) if args.log else None

    try:
        asyncio.run(run_server(args.host, args.port, log_file=log_file))
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
