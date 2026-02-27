#!/usr/bin/env python3
"""
UDP Log Server for PS5 BackPork Kitchen.

Receives UDP log packets from a PS5 console and prints them to stdout.
Used by the Advanced Backport Pipeline to capture real-time logs from
a running game on the console.

Usage:
    python udp_log_server.py --port 9090
    python udp_log_server.py --port 9090 --bind 0.0.0.0
"""

import argparse
import socket
import sys
import signal
import datetime


def main():
    parser = argparse.ArgumentParser(description="UDP Log Server for PS5 BackPork Kitchen")
    parser.add_argument("--port", type=int, default=9090, help="UDP port to listen on (default: 9090)")
    parser.add_argument("--bind", type=str, default="0.0.0.0", help="Address to bind to (default: 0.0.0.0)")
    parser.add_argument("--buffer-size", type=int, default=4096, help="Receive buffer size (default: 4096)")
    args = parser.parse_args()

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    # Allow clean shutdown
    running = True

    def signal_handler(sig, frame):
        nonlocal running
        running = False
        print("\n[UDP] Server shutting down...", flush=True)
        sock.close()
        sys.exit(0)

    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)

    try:
        sock.bind((args.bind, args.port))
    except OSError as e:
        print(f"[ERROR] Cannot bind to {args.bind}:{args.port} — {e}", file=sys.stderr, flush=True)
        sys.exit(1)

    print(f"[UDP] Listening on {args.bind}:{args.port}", flush=True)
    print(f"[UDP] Waiting for log packets from PS5 console...", flush=True)

    sock.settimeout(1.0)  # 1s timeout for clean shutdown support

    while running:
        try:
            data, addr = sock.recvfrom(args.buffer_size)
            timestamp = datetime.datetime.now().strftime("%H:%M:%S.%f")[:-3]
            message = data.decode("utf-8", errors="replace").rstrip("\r\n\x00")
            if message:
                print(f"[{timestamp}] {addr[0]}:{addr[1]} | {message}", flush=True)
        except socket.timeout:
            continue
        except OSError:
            if running:
                break

    sock.close()
    print("[UDP] Server stopped.", flush=True)


if __name__ == "__main__":
    main()
