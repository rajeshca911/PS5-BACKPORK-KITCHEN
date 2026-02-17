"""
BPS Patch Engine — Beat Patch System implementation for PS5 PRX backporting.

BPS format:
  Header:  "BPS1"
  VLC:     source_size, target_size, metadata_size
  Data:    metadata (JSON UTF-8, optional)
  Actions: sequence of (action << 2 | length_vlc) records
    0 = SourceRead  : copy N bytes from source[src_pos]
    1 = TargetRead  : N literal bytes follow in patch
    2 = SourceCopy  : copy N bytes from source[src_pos + signed_delta]
    3 = TargetCopy  : copy N bytes from target[out_pos + signed_delta]
  Footer:  crc32(source)[4] + crc32(target)[4] + crc32(patch_body)[4]

Usage:
  python bps_engine.py apply  --source lib.sprx --patch fix.bps --output lib_patched.sprx
  python bps_engine.py validate --patch fix.bps
  python bps_engine.py list   --db data/patch_database.json
"""

import argparse
import hashlib
import json
import os
import struct
import sys
import zlib
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Optional


# ---------------------------------------------------------------------------
# BPS Core
# ---------------------------------------------------------------------------

class BPSError(Exception):
    pass


def _decode_vlc(data: bytes, offset: int) -> tuple[int, int]:
    """Decode a variable-length coded integer from data at offset.
    Returns (value, new_offset).
    Each byte: bits[6:0] = data, bit[7] = continuation flag.
    Final byte has bit[7] set; add (1 << (7*n)) for each subsequent group.
    """
    result = 0
    shift = 0
    while True:
        if offset >= len(data):
            raise BPSError("Truncated VLC at offset {}".format(offset))
        b = data[offset]
        offset += 1
        result += (b & 0x7F) << shift
        if b & 0x80:
            break
        shift += 7
        result += 1 << shift  # each new group adds implicit +1
    return result, offset


def _read_signed_vlc(data: bytes, offset: int) -> tuple[int, int]:
    """Decode signed delta for SourceCopy / TargetCopy."""
    value, offset = _decode_vlc(data, offset)
    if value & 1:
        return -(value >> 1), offset
    return value >> 1, offset


class BPSEngine:
    """Applies a BPS patch to produce an output file."""

    @staticmethod
    def validate_patch(patch_path: str) -> dict:
        """Validate a BPS patch file. Returns info dict or raises BPSError."""
        with open(patch_path, "rb") as f:
            data = f.read()

        if len(data) < 4 + 3 + 12:
            raise BPSError("File too small to be a BPS patch")

        if data[:4] != b"BPS1":
            raise BPSError("Invalid magic (expected 'BPS1', got {!r})".format(data[:4]))

        offset = 4
        source_size, offset = _decode_vlc(data, offset)
        target_size, offset = _decode_vlc(data, offset)
        metadata_size, offset = _decode_vlc(data, offset)

        metadata_str = ""
        if metadata_size > 0:
            metadata_bytes = data[offset:offset + metadata_size]
            metadata_str = metadata_bytes.decode("utf-8", errors="replace")
            offset += metadata_size

        # Footer is the last 12 bytes
        if len(data) < 12:
            raise BPSError("Patch data too short for footer")

        footer_offset = len(data) - 12
        src_crc, tgt_crc, patch_crc = struct.unpack_from("<III", data, footer_offset)

        # Verify patch CRC
        actual_patch_crc = zlib.crc32(data[:-4]) & 0xFFFFFFFF
        if actual_patch_crc != patch_crc:
            raise BPSError(
                "Patch CRC mismatch: expected 0x{:08X}, got 0x{:08X}".format(
                    patch_crc, actual_patch_crc))

        return {
            "valid": True,
            "source_size": source_size,
            "target_size": target_size,
            "metadata": metadata_str,
            "source_crc32": hex(src_crc),
            "target_crc32": hex(tgt_crc),
            "patch_crc32": hex(patch_crc),
            "patch_size": len(data),
        }

    @staticmethod
    def apply_patch(
        source_path: str,
        patch_path: str,
        output_path: str,
        verify: bool = True,
    ) -> bool:
        """Apply BPS patch to source, write output. Returns True on success."""
        # Load files
        with open(source_path, "rb") as f:
            source = bytearray(f.read())
        with open(patch_path, "rb") as f:
            patch = f.read()

        if patch[:4] != b"BPS1":
            raise BPSError("Invalid BPS magic")

        offset = 4
        source_size, offset = _decode_vlc(patch, offset)
        target_size, offset = _decode_vlc(patch, offset)
        metadata_size, offset = _decode_vlc(patch, offset)

        if verify and len(source) != source_size:
            raise BPSError(
                "Source size mismatch: patch expects {} bytes, got {}".format(
                    source_size, len(source)))

        offset += metadata_size  # skip metadata

        # Verify source CRC32
        footer_offset = len(patch) - 12
        src_crc, tgt_crc, patch_crc = struct.unpack_from("<III", patch, footer_offset)

        if verify:
            actual_src_crc = zlib.crc32(source) & 0xFFFFFFFF
            if actual_src_crc != src_crc:
                raise BPSError(
                    "Source CRC mismatch: expected 0x{:08X}, got 0x{:08X}".format(
                        src_crc, actual_src_crc))

        # Verify patch CRC
        actual_patch_crc = zlib.crc32(patch[:-4]) & 0xFFFFFFFF
        if actual_patch_crc != patch_crc:
            raise BPSError("Patch file is corrupted (CRC mismatch)")

        # Apply actions
        target = bytearray(target_size)
        src_pos = 0
        out_pos = 0
        actions_end = footer_offset

        while offset < actions_end:
            header_val, offset = _decode_vlc(patch, offset)
            action = header_val & 3
            length = (header_val >> 2) + 1  # +1 because length 0 means 1 byte

            if action == 0:  # SourceRead: copy from source at src_pos
                if out_pos + length > target_size:
                    raise BPSError("SourceRead overflow at offset {}".format(offset))
                target[out_pos:out_pos + length] = source[src_pos:src_pos + length]
                src_pos += length
                out_pos += length

            elif action == 1:  # TargetRead: literal bytes from patch
                if offset + length > len(patch):
                    raise BPSError("TargetRead overflows patch data")
                target[out_pos:out_pos + length] = patch[offset:offset + length]
                offset += length
                out_pos += length

            elif action == 2:  # SourceCopy: copy from source[src_pos + delta]
                delta, offset = _read_signed_vlc(patch, offset)
                src_pos += delta
                if src_pos < 0 or src_pos + length > len(source):
                    raise BPSError("SourceCopy out of bounds")
                target[out_pos:out_pos + length] = source[src_pos:src_pos + length]
                src_pos += length
                out_pos += length

            elif action == 3:  # TargetCopy: copy from target[out_pos + delta]
                delta, offset = _read_signed_vlc(patch, offset)
                copy_from = out_pos + delta
                if copy_from < 0:
                    raise BPSError("TargetCopy negative offset")
                for i in range(length):
                    target[out_pos + i] = target[copy_from + i]
                out_pos += length

        # Verify output
        if verify:
            actual_tgt_crc = zlib.crc32(target) & 0xFFFFFFFF
            if actual_tgt_crc != tgt_crc:
                raise BPSError(
                    "Output CRC mismatch: expected 0x{:08X}, got 0x{:08X}".format(
                        tgt_crc, actual_tgt_crc))

        os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
        with open(output_path, "wb") as f:
            f.write(target)

        print("[BPS] OK: {} -> {} ({} bytes)".format(
            os.path.basename(source_path),
            os.path.basename(output_path),
            len(target)))
        return True


# ---------------------------------------------------------------------------
# Patch Database
# ---------------------------------------------------------------------------

@dataclass
class PatchEntry:
    fw_from: str
    fw_to: str
    lib: str
    patch: str
    sha256_source: str = ""
    sha256_target: str = ""


class PatchDatabase:
    """Manages a JSON database of BPS patches for firmware combinations."""

    def __init__(self, db_path: str):
        self.db_path = db_path
        self._base_dir = os.path.dirname(os.path.abspath(db_path))
        self.entries: list[PatchEntry] = []
        self._load()

    def _load(self):
        if not os.path.exists(self.db_path):
            self.entries = []
            return
        with open(self.db_path, encoding="utf-8") as f:
            data = json.load(f)
        self.entries = [PatchEntry(**e) for e in data.get("patches", [])]

    def find_patch(self, fw_from: str, fw_to: str, lib_name: str) -> Optional[str]:
        """Return absolute path to the .bps file, or None if not found."""
        for e in self.entries:
            if (e.fw_from == fw_from and e.fw_to == fw_to
                    and e.lib.lower() == lib_name.lower()):
                return os.path.join(self._base_dir, e.patch)
        return None

    def apply_auto(
        self,
        source_folder: str,
        fw_from: str,
        fw_to: str,
        progress_cb: Optional[Callable[[str], None]] = None,
    ) -> dict:
        """Apply all available patches for a firmware combination to a folder.
        Returns {"applied": [...], "skipped": [...], "errors": [...]}
        """
        results = {"applied": [], "skipped": [], "errors": []}

        for root, _dirs, files in os.walk(source_folder):
            for fname in files:
                if not fname.lower().endswith((".sprx", ".prx")):
                    continue
                patch_path = self.find_patch(fw_from, fw_to, fname)
                if not patch_path:
                    results["skipped"].append(fname)
                    continue

                src = os.path.join(root, fname)
                out = src + ".patched"
                if progress_cb:
                    progress_cb("[BPS] Patching {} ...".format(fname))
                try:
                    BPSEngine.apply_patch(src, patch_path, out)
                    # Replace original with patched
                    os.replace(out, src)
                    results["applied"].append(fname)
                except BPSError as ex:
                    results["errors"].append({"file": fname, "error": str(ex)})

        return results

    def list_patches(self) -> list[dict]:
        return [
            {"fw_from": e.fw_from, "fw_to": e.fw_to,
             "lib": e.lib, "patch": e.patch}
            for e in self.entries
        ]


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def cmd_apply(args):
    try:
        BPSEngine.apply_patch(args.source, args.patch, args.output,
                              verify=not args.no_verify)
    except BPSError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)


def cmd_validate(args):
    try:
        info = BPSEngine.validate_patch(args.patch)
        print(json.dumps(info, indent=2))
    except BPSError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)


def cmd_list(args):
    db = PatchDatabase(args.db)
    patches = db.list_patches()
    if not patches:
        print("No patches in database.")
    for p in patches:
        print("  [{fw_from} -> {fw_to}] {lib}  ({patch})".format(**p))


def cmd_auto(args):
    db = PatchDatabase(args.db)
    results = db.apply_auto(
        args.folder, args.fw_from, args.fw_to,
        progress_cb=print)
    print("\n[SUMMARY]")
    print("  Applied: {}".format(len(results["applied"])))
    print("  Skipped: {}".format(len(results["skipped"])))
    print("  Errors:  {}".format(len(results["errors"])))
    for err in results["errors"]:
        print("  [ERR] {file}: {error}".format(**err))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="BPS Patch Engine for PS5 PRX backporting")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_apply = sub.add_parser("apply", help="Apply a BPS patch")
    p_apply.add_argument("--source", required=True)
    p_apply.add_argument("--patch",  required=True)
    p_apply.add_argument("--output", required=True)
    p_apply.add_argument("--no-verify", action="store_true")

    p_val = sub.add_parser("validate", help="Validate a BPS patch file")
    p_val.add_argument("--patch", required=True)

    p_list = sub.add_parser("list", help="List patches in database")
    p_list.add_argument("--db", default="data/patch_database.json")

    p_auto = sub.add_parser("auto", help="Auto-apply patches from database to a folder")
    p_auto.add_argument("--folder",  required=True)
    p_auto.add_argument("--fw-from", required=True)
    p_auto.add_argument("--fw-to",   required=True)
    p_auto.add_argument("--db", default="data/patch_database.json")

    parsed = parser.parse_args()
    {
        "apply":    cmd_apply,
        "validate": cmd_validate,
        "list":     cmd_list,
        "auto":     cmd_auto,
    }[parsed.cmd](parsed)
