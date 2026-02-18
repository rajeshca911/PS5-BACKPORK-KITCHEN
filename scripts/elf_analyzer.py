"""
ELF Analyzer — PS5 PRX dependency and symbol analysis.

NID (Name Identifier) on PS5:
  nid = SHA1(symbol_name + ":")[0:8]  interpreted as big-endian uint64 → 16 hex chars

Usage:
  python elf_analyzer.py analyze --elf eboot.bin --fw 7.61 --exports-dir data/exports --output report.json
  python elf_analyzer.py exports --fw 7.61 --exports-dir data/exports
  python elf_analyzer.py nid --name sceNpAuthPollPlusToken
"""

import argparse
import hashlib
import io
import json
import os
import struct
import sys
from pathlib import Path
from typing import Optional

try:
    from elftools.elf.elffile import ELFFile
    from elftools.elf.dynamic import DynamicSection
    from elftools.elf.sections import SymbolTableSection
    _PYELFTOOLS = True
except ImportError:
    _PYELFTOOLS = False


# ---------------------------------------------------------------------------
# SELF (Signed ELF) container handling
# ---------------------------------------------------------------------------

# PS4/PS5 SELF magic bytes
SELF_MAGIC_PS4 = b'\x4F\x15\x3D\x1D'
SELF_MAGIC_PS5 = b'\x54\x14\xF5\xEE'
ELF_MAGIC = b'\x7FELF'


def is_self_file(data: bytes) -> bool:
    """Check if data starts with a SELF (Signed ELF) magic."""
    return data[:4] in (SELF_MAGIC_PS4, SELF_MAGIC_PS5)


def extract_elf_from_self(data: bytes) -> bytes:
    """Find and extract the embedded ELF from a SELF container.

    SELF files wrap a standard ELF with a proprietary header.
    We scan for the ELF magic (\\x7FELF) and return from that offset.
    Note: segment data in SELF containers is typically encrypted, so
    pyelftools can only parse the ELF/program headers, not segment data.
    """
    idx = data.find(ELF_MAGIC)
    if idx < 0:
        raise ELFAnalyzerError("No embedded ELF found in SELF file")
    return data[idx:]


def get_self_info(data: bytes) -> dict:
    """Parse basic SELF header info without decryption.
    Returns dict with version, num_entries, file_size, etc.
    """
    if len(data) < 28:
        return {}
    magic = data[:4]
    version = data[4]
    mode = data[5]
    endian = data[6]
    attribs = data[7]
    key_type = struct.unpack_from("<I", data, 8)[0]
    header_size = struct.unpack_from("<H", data, 12)[0]
    meta_size = struct.unpack_from("<H", data, 14)[0]
    file_size = struct.unpack_from("<Q", data, 16)[0]
    num_entries = struct.unpack_from("<H", data, 24)[0]
    flags = struct.unpack_from("<H", data, 26)[0]
    return {
        "magic": magic.hex(),
        "version": version,
        "mode": mode,
        "endian": "LE" if endian == 1 else "BE",
        "key_type": key_type,
        "header_size": header_size,
        "meta_size": meta_size,
        "file_size": file_size,
        "num_entries": num_entries,
        "flags": flags,
    }


# ---------------------------------------------------------------------------
# NID helpers
# ---------------------------------------------------------------------------

def calc_nid(symbol_name: str) -> str:
    """Compute the PS5 NID for a symbol name.
    NID = SHA1(name + ':')[0:8] big-endian → 16-hex-char string.
    """
    digest = hashlib.sha1((symbol_name + ":").encode()).digest()
    return digest[:8].hex().upper()


# ---------------------------------------------------------------------------
# ELF Analyzer
# ---------------------------------------------------------------------------

class ELFAnalyzerError(Exception):
    pass


class ELFAnalyzer:
    """Analyzes a PS5 ELF/SELF/PRX for library dependencies and imported symbols."""

    # PT_SCE_PROCPARAM type (PS4/PS5 specific program header)
    PT_SCE_PROCPARAM = 0x61000001

    def __init__(self, elf_path: str):
        if not _PYELFTOOLS:
            raise ELFAnalyzerError(
                "pyelftools is not installed. Run: pip install pyelftools")
        if not os.path.exists(elf_path):
            raise ELFAnalyzerError("File not found: {}".format(elf_path))
        self.elf_path = elf_path
        self._data = open(elf_path, "rb").read()
        self._is_self = is_self_file(self._data)

        if self._is_self:
            # SELF container: extract the embedded ELF for pyelftools.
            elf_data = extract_elf_from_self(self._data)
            self._elf = ELFFile(io.BytesIO(elf_data))
        else:
            self._elf = ELFFile(io.BytesIO(self._data))

    # ---- Internal helpers --------------------------------------------------

    def _iter_segments_safe(self):
        """Iterate over ELF segments, skipping any that cause errors
        (common in SELF containers where some segments point beyond the
        extracted data)."""
        for i in range(self._elf.num_segments()):
            try:
                yield self._elf.get_segment(i)
            except Exception:
                continue

    # ---- Public API --------------------------------------------------------

    def get_required_libs(self) -> list[str]:
        """Return list of DT_NEEDED library names from DYNAMIC segment."""
        libs = []
        for seg in self._iter_segments_safe():
            try:
                if seg.header.p_type == "PT_DYNAMIC":
                    for tag in seg.iter_tags():
                        if tag.entry.d_tag == "DT_NEEDED":
                            name = tag.needed
                            if name:
                                libs.append(name)
            except Exception:
                continue
        return libs

    def get_imported_symbols(self) -> list[dict]:
        """Return list of imported symbols from .dynsym / .rela.plt.
        Each entry: {"name": str, "nid": str, "plt_offset": int | None}
        """
        symbols = {}

        try:
            # Collect undefined symbols from .dynsym
            dynsym = self._elf.get_section_by_name(".dynsym")
            if dynsym and isinstance(dynsym, SymbolTableSection):
                for sym in dynsym.iter_symbols():
                    if (sym.name and sym.entry.st_shndx == "SHN_UNDEF"
                            and sym.entry.st_info.type in
                            ("STT_FUNC", "STT_NOTYPE", "STT_OBJECT")):
                        symbols[sym.name] = {
                            "name": sym.name,
                            "nid": calc_nid(sym.name),
                            "plt_offset": None,
                        }

            # Enrich with PLT offsets from .rela.plt
            rela_plt = self._elf.get_section_by_name(".rela.plt")
            if rela_plt and dynsym:
                for rel in rela_plt.iter_relocations():
                    sym_idx = rel["r_info_sym"]
                    sym = dynsym.get_symbol(sym_idx)
                    if sym and sym.name in symbols:
                        symbols[sym.name]["plt_offset"] = rel["r_offset"]
        except Exception:
            pass

        return list(symbols.values())

    def get_exported_symbols(self) -> list[dict]:
        """Return list of symbols exported by this ELF.
        Each entry: {"name": str, "nid": str, "address": int}
        """
        result = []
        dynsym = self._elf.get_section_by_name(".dynsym")
        if not dynsym:
            return result
        for sym in dynsym.iter_symbols():
            if (sym.name
                    and sym.entry.st_shndx != "SHN_UNDEF"
                    and sym.entry.st_info.bind in ("STB_GLOBAL", "STB_WEAK")
                    and sym.entry.st_info.type in ("STT_FUNC", "STT_OBJECT")):
                result.append({
                    "name": sym.name,
                    "nid": calc_nid(sym.name),
                    "address": sym.entry.st_value,
                })
        return result

    def get_sdk_versions(self) -> dict:
        """Extract PS5/PS4 SDK version from PT_SCE_PROCPARAM program header.
        Returns {"ps5_sdk": int, "ps4_sdk": int, "ps5_sdk_str": str, "ps4_sdk_str": str}
        """
        import struct
        for seg in self._iter_segments_safe():
            if seg.header.p_type == self.PT_SCE_PROCPARAM:
                raw = seg.data()
                if len(raw) >= 0x18:
                    # Offsets within PROCPARAM are SDK-defined; common layout:
                    # 0x00: size, 0x04: magic, 0x08: ps4_sdk, 0x10: entry_point
                    # 0x14: ps5_sdk_version (may vary by game)
                    ps4_sdk = struct.unpack_from("<I", raw, 0x08)[0] if len(raw) >= 0x0C else 0
                    ps5_sdk = struct.unpack_from("<I", raw, 0x14)[0] if len(raw) >= 0x18 else 0

                    def _fmt(v):
                        return "{}.{}.{}.{}".format(
                            (v >> 24) & 0xFF, (v >> 16) & 0xFF,
                            (v >> 8) & 0xFF, v & 0xFF)

                    return {
                        "ps4_sdk": ps4_sdk,
                        "ps5_sdk": ps5_sdk,
                        "ps4_sdk_str": _fmt(ps4_sdk),
                        "ps5_sdk_str": _fmt(ps5_sdk),
                    }
        return {"ps4_sdk": 0, "ps5_sdk": 0, "ps4_sdk_str": "unknown", "ps5_sdk_str": "unknown"}

    def generate_report(self, target_fw: str, exports_dir: str) -> dict:
        """Analyze compatibility against a target firmware version.
        Returns full report dict.
        """
        db = FirmwareExportsDB(exports_dir)

        required_libs = self.get_required_libs()
        imported = self.get_imported_symbols()
        sdk_info = self.get_sdk_versions()

        # Determine which libs exist in the target firmware
        fw_libs = db.get_all_lib_names(target_fw)
        missing_libs = [lib for lib in required_libs if lib not in fw_libs]

        # Check each imported symbol
        found_symbols = []
        missing_symbols = []
        for sym in imported:
            # Find which required lib this symbol belongs to
            owning_lib = db.find_owning_lib(target_fw, sym["name"])
            if owning_lib:
                found_symbols.append({**sym, "lib": owning_lib})
            else:
                # Determine severity: symbols from missing libs are critical
                severity = "critical"
                for lib in required_libs:
                    if lib in missing_libs:
                        pass
                    else:
                        severity = "warning"
                missing_symbols.append({**sym, "severity": severity})

        # Compatibility score
        total = len(imported)
        score = int((len(found_symbols) / total) * 100) if total else 100

        report = {
            "elf": os.path.basename(self.elf_path),
            "is_self": self._is_self,
            "file_size": len(self._data),
            "sdk_ps5": sdk_info["ps5_sdk_str"],
            "sdk_ps4": sdk_info["ps4_sdk_str"],
            "target_fw": target_fw,
            "required_libs": required_libs,
            "missing_libs": missing_libs,
            "total_imported": total,
            "found_symbols": len(found_symbols),
            "missing_symbols": missing_symbols,
            "compatibility_score": score,
        }
        if self._is_self:
            report["self_info"] = get_self_info(self._data)
            if total == 0:
                report["note"] = "SELF encrypted — full analysis needs selfutil decryption"
        return report


# ---------------------------------------------------------------------------
# Firmware Exports DB
# ---------------------------------------------------------------------------

class FirmwareExportsDB:
    """Manages a directory of per-firmware JSON export files.

    File format — exports/{fw_version}.json:
    {
      "libFoo.sprx": {
        "funcName": "NID_HEX",
        ...
      },
      ...
    }
    """

    def __init__(self, exports_dir: str):
        self.exports_dir = exports_dir
        self._cache: dict[str, dict] = {}

    def _load_fw(self, fw_version: str) -> dict:
        if fw_version in self._cache:
            return self._cache[fw_version]
        path = os.path.join(self.exports_dir, "{}.json".format(fw_version))
        if not os.path.exists(path):
            self._cache[fw_version] = {}
            return {}
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        self._cache[fw_version] = data
        return data

    def get_all_lib_names(self, fw_version: str) -> list[str]:
        return list(self._load_fw(fw_version).keys())

    def get_all_exports(self, fw_version: str, lib: str) -> dict:
        """Return {symbol_name: nid} for the given lib in a firmware."""
        return self._load_fw(fw_version).get(lib, {})

    def has_symbol(self, fw_version: str, lib: str, symbol_name: str) -> bool:
        return symbol_name in self.get_all_exports(fw_version, lib)

    def find_owning_lib(self, fw_version: str, symbol_name: str) -> Optional[str]:
        """Return the lib that exports this symbol, or None."""
        for lib, syms in self._load_fw(fw_version).items():
            if symbol_name in syms:
                return lib
        return None


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def cmd_analyze(args):
    try:
        analyzer = ELFAnalyzer(args.elf)
    except ELFAnalyzerError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)

    report = analyzer.generate_report(args.fw, args.exports_dir)

    output = json.dumps(report, indent=2)
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(output)
        print("[ELF] Report saved to {}".format(args.output))
    else:
        print(output)

    score = report["compatibility_score"]
    color = "\033[92m" if score >= 90 else ("\033[93m" if score >= 70 else "\033[91m")
    print("\n{}[ELF] Compatibility score: {}%\033[0m".format(color, score))


def cmd_exports(args):
    db = FirmwareExportsDB(args.exports_dir)
    libs = db.get_all_lib_names(args.fw)
    if not libs:
        print("No exports found for firmware {}".format(args.fw))
        return
    for lib in sorted(libs):
        syms = db.get_all_exports(args.fw, lib)
        print("  {} ({} symbols)".format(lib, len(syms)))


def cmd_nid(args):
    nid = calc_nid(args.name)
    print("{} -> {}".format(args.name, nid))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="ELF Analyzer for PS5 PRX backporting")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_analyze = sub.add_parser("analyze", help="Analyze ELF against target firmware")
    p_analyze.add_argument("--elf", required=True)
    p_analyze.add_argument("--fw", required=True, help="Target firmware version (e.g. 7.61)")
    p_analyze.add_argument("--exports-dir", default="data/exports")
    p_analyze.add_argument("--output", help="Save JSON report to file")

    p_exports = sub.add_parser("exports", help="List library exports for a firmware")
    p_exports.add_argument("--fw", required=True)
    p_exports.add_argument("--exports-dir", default="data/exports")

    p_nid = sub.add_parser("nid", help="Compute NID for a symbol name")
    p_nid.add_argument("--name", required=True)

    parsed = parser.parse_args()
    {
        "analyze": cmd_analyze,
        "exports": cmd_exports,
        "nid":     cmd_nid,
    }[parsed.cmd](parsed)
