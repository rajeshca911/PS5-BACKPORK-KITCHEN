"""
NID Builder — Extracts symbol exports from PS5 firmware libraries (.sprx)
and builds the exports database used by elf_analyzer.py.

For each .sprx in a firmware lib folder:
  1. Parse ELF exports via pyelftools
  2. Compute NID = SHA1(name + ':')[0:8]
  3. Write/update exports/{fw_version}.json

Usage:
  python nid_builder.py build --fw 7.61 --lib-dir /path/to/fw_libs --exports-dir data/exports
  python nid_builder.py merge --fw 7.61 --input extra_syms.json --exports-dir data/exports
  python nid_builder.py query --fw 7.61 --lib libSceNpAuth.sprx --exports-dir data/exports
"""

import argparse
import hashlib
import json
import os
import sys
from pathlib import Path

try:
    from elftools.elf.elffile import ELFFile
    from elftools.elf.sections import SymbolTableSection
    _PYELFTOOLS = True
except ImportError:
    _PYELFTOOLS = False


# ---------------------------------------------------------------------------
# NID
# ---------------------------------------------------------------------------

def calc_nid(symbol_name: str) -> str:
    digest = hashlib.sha1((symbol_name + ":").encode()).digest()
    return digest[:8].hex().upper()


# ---------------------------------------------------------------------------
# NID Builder
# ---------------------------------------------------------------------------

class NIDBuilderError(Exception):
    pass


class NIDBuilder:
    """Extracts exports from a folder of .sprx files and builds the DB."""

    def __init__(self, exports_dir: str):
        if not _PYELFTOOLS:
            raise NIDBuilderError(
                "pyelftools is not installed. Run: pip install pyelftools")
        self.exports_dir = exports_dir
        os.makedirs(exports_dir, exist_ok=True)

    def _extract_exports_from_elf(self, elf_path: str) -> dict[str, str]:
        """Return {symbol_name: nid} for all globally exported symbols."""
        result = {}
        try:
            with open(elf_path, "rb") as f:
                elf = ELFFile(f)
                dynsym = elf.get_section_by_name(".dynsym")
                if not dynsym or not isinstance(dynsym, SymbolTableSection):
                    return result
                for sym in dynsym.iter_symbols():
                    if (sym.name
                            and sym.entry.st_shndx != "SHN_UNDEF"
                            and sym.entry.st_info.bind in ("STB_GLOBAL", "STB_WEAK")
                            and sym.entry.st_info.type in ("STT_FUNC", "STT_OBJECT")):
                        result[sym.name] = calc_nid(sym.name)
        except Exception as ex:
            print("[WARN] Could not parse {}: {}".format(elf_path, ex), file=sys.stderr)
        return result

    def build_from_folder(self, fw_version: str, lib_dir: str,
                          progress_cb=None) -> dict:
        """Scan lib_dir for .sprx files, extract exports, write exports/{fw}.json.
        Returns summary {"libs": int, "symbols": int, "output": str}
        """
        db = {}
        total_syms = 0

        extensions = (".sprx", ".prx", ".so")
        for fname in sorted(os.listdir(lib_dir)):
            if not fname.lower().endswith(extensions):
                continue
            fpath = os.path.join(lib_dir, fname)
            if progress_cb:
                progress_cb("[NID] Scanning {} ...".format(fname))
            syms = self._extract_exports_from_elf(fpath)
            if syms:
                db[fname] = syms
                total_syms += len(syms)

        output_path = os.path.join(self.exports_dir, "{}.json".format(fw_version))
        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(db, f, indent=2, sort_keys=True)

        return {
            "libs": len(db),
            "symbols": total_syms,
            "output": output_path,
        }

    def merge_json(self, fw_version: str, input_path: str) -> dict:
        """Merge a manually created {lib: {name: nid}} JSON into the DB."""
        with open(input_path, encoding="utf-8") as f:
            extra = json.load(f)

        output_path = os.path.join(self.exports_dir, "{}.json".format(fw_version))
        if os.path.exists(output_path):
            with open(output_path, encoding="utf-8") as f:
                existing = json.load(f)
        else:
            existing = {}

        merged_syms = 0
        for lib, syms in extra.items():
            if lib not in existing:
                existing[lib] = {}
            before = len(existing[lib])
            existing[lib].update(syms)
            merged_syms += len(existing[lib]) - before

        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(existing, f, indent=2, sort_keys=True)

        return {"merged_new_symbols": merged_syms, "output": output_path}

    def query(self, fw_version: str, lib: str) -> dict:
        """Return all exported symbols for a lib in a firmware."""
        path = os.path.join(self.exports_dir, "{}.json".format(fw_version))
        if not os.path.exists(path):
            return {}
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        return data.get(lib, {})


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def cmd_build(args):
    try:
        builder = NIDBuilder(args.exports_dir)
    except NIDBuilderError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)

    result = builder.build_from_folder(args.fw, args.lib_dir, progress_cb=print)
    print("\n[NID] Done — {} libs, {} symbols -> {}".format(
        result["libs"], result["symbols"], result["output"]))


def cmd_merge(args):
    try:
        builder = NIDBuilder(args.exports_dir)
    except NIDBuilderError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)

    result = builder.merge_json(args.fw, args.input)
    print("[NID] Merged {} new symbols -> {}".format(
        result["merged_new_symbols"], result["output"]))


def cmd_query(args):
    try:
        builder = NIDBuilder(args.exports_dir)
    except NIDBuilderError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)

    syms = builder.query(args.fw, args.lib)
    if not syms:
        print("No exports found for {} in firmware {}".format(args.lib, args.fw))
        return
    for name, nid in sorted(syms.items()):
        print("  {} -> {}".format(name, nid))
    print("\n  Total: {}".format(len(syms)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="NID Builder — build PS5 symbol exports DB from firmware libs")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_build = sub.add_parser("build", help="Scan a lib folder and build exports JSON")
    p_build.add_argument("--fw", required=True)
    p_build.add_argument("--lib-dir", required=True)
    p_build.add_argument("--exports-dir", default="data/exports")

    p_merge = sub.add_parser("merge", help="Merge a manually created JSON into exports DB")
    p_merge.add_argument("--fw", required=True)
    p_merge.add_argument("--input", required=True)
    p_merge.add_argument("--exports-dir", default="data/exports")

    p_query = sub.add_parser("query", help="List exports for a specific lib")
    p_query.add_argument("--fw", required=True)
    p_query.add_argument("--lib", required=True)
    p_query.add_argument("--exports-dir", default="data/exports")

    parsed = parser.parse_args()
    {"build": cmd_build, "merge": cmd_merge, "query": cmd_query}[parsed.cmd](parsed)
