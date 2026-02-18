"""
ELF Analyzer — PS5 PRX dependency and symbol analysis.

PS5 ELFs are stripped (no section headers). All dynamic info must be
parsed from program headers (segments) using SCE-specific dynamic tags:
  DT_SCE_NEEDED, DT_SCE_SYMTAB, DT_SCE_STRTAB, DT_SCE_JMPREL, etc.

Symbols use NID encoding: base64-like NID + "#" + lib_suffix + "#" + module_suffix.

NID (Name Identifier) on PS5:
  nid = SHA1(symbol_name + ":")[0:8]  interpreted as big-endian uint64 -> 16 hex chars

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
    NID = SHA1(name + ':')[0:8] big-endian -> 16-hex-char string.
    """
    digest = hashlib.sha1((symbol_name + ":").encode()).digest()
    return digest[:8].hex().upper()


# ---------------------------------------------------------------------------
# PS5 ELF types and dynamic tags
# ---------------------------------------------------------------------------

# PS5/PS4 ELF types
ET_SCE_EXEC   = 0xFE00
ET_SCE_DYNEXEC = 0xFE10
ET_SCE_DYNAMIC = 0xFE18

# PS5/PS4 segment types
PT_SCE_PROCPARAM    = 0x61000001   # eboot.bin process param
PT_SCE_MODULE_PARAM = 0x61000002   # .prx/.sprx module param
PT_SCE_DYNLIBDATA   = 0x61000010

# SCE param segment magic values (at offset +0x08 within the segment)
SCE_PROCESS_PARAM_MAGIC = 0x4942524F  # "IBRO"
SCE_MODULE_PARAM_MAGIC  = 0x3C13F4BF

# PS5/PS4 dynamic tags
DT_SCE_NEEDED      = 0x6100000D
DT_SCE_MODULE_INFO = 0x6100000E
DT_SCE_SYMTAB      = 0x61000011
DT_SCE_STRTAB      = 0x61000013
DT_SCE_STRSZ       = 0x61000015
DT_SCE_HASH        = 0x61000019
DT_SCE_SYMTABSZ    = 0x61000025
DT_SCE_RELA        = 0x61000035
DT_SCE_RELASZ      = 0x61000037
DT_SCE_RELAENT     = 0x61000039
DT_SCE_PLTGOT      = 0x6100003B
DT_SCE_PLTRELSZ    = 0x6100003D
DT_SCE_PLTREL      = 0x6100003F
DT_SCE_JMPREL      = 0x61000041
DT_SCE_ORIGINAL_FILENAME = 0x61000007


# ---------------------------------------------------------------------------
# Raw PS5 ELF Parser (no section headers needed)
# ---------------------------------------------------------------------------

class PS5ELFParser:
    """Parses PS5 ELF files directly from program headers and raw bytes.
    PS5 ELFs typically have e_shnum=0 (no section headers), so all
    dynamic linking info must come from PT_DYNAMIC segment tags.
    """

    def __init__(self, data: bytes):
        self._data = data
        if data[:4] != ELF_MAGIC:
            raise ELFAnalyzerError("Not a valid ELF file")

        # Parse ELF header
        self.ei_class = data[4]  # 1=32bit, 2=64bit
        if self.ei_class != 2:
            raise ELFAnalyzerError("Only 64-bit ELFs supported")

        self.e_type = struct.unpack_from("<H", data, 16)[0]
        self.e_machine = struct.unpack_from("<H", data, 18)[0]
        self.e_phoff = struct.unpack_from("<Q", data, 32)[0]
        self.e_phentsize = struct.unpack_from("<H", data, 54)[0]
        self.e_phnum = struct.unpack_from("<H", data, 56)[0]
        self.e_shnum = struct.unpack_from("<H", data, 60)[0]

        # Parse program headers
        self._phdrs = []
        for i in range(self.e_phnum):
            off = self.e_phoff + i * self.e_phentsize
            if off + 56 > len(data):
                break
            phdr = {
                "p_type":   struct.unpack_from("<I", data, off)[0],
                "p_flags":  struct.unpack_from("<I", data, off + 4)[0],
                "p_offset": struct.unpack_from("<Q", data, off + 8)[0],
                "p_vaddr":  struct.unpack_from("<Q", data, off + 16)[0],
                "p_paddr":  struct.unpack_from("<Q", data, off + 24)[0],
                "p_filesz": struct.unpack_from("<Q", data, off + 32)[0],
                "p_memsz":  struct.unpack_from("<Q", data, off + 40)[0],
                "p_align":  struct.unpack_from("<Q", data, off + 48)[0],
            }
            self._phdrs.append(phdr)

        # Build LOAD segment map for vaddr->file_offset translation
        self._loads = []
        for ph in self._phdrs:
            if ph["p_type"] == 1:  # PT_LOAD
                self._loads.append((ph["p_vaddr"], ph["p_offset"], ph["p_filesz"]))

        # Parse dynamic tags
        self._dyntags = {}
        self._dyntags_list = []
        self._needed_offsets = []
        self._sce_needed_offsets = []
        self._parse_dynamic()

        # Parse string table
        self._strtab = b""
        self._parse_strtab()

    def _vaddr_to_offset(self, va: int) -> int:
        """Convert virtual address to file offset using LOAD segments."""
        for v, o, sz in self._loads:
            if v <= va < v + sz:
                return o + (va - v)
        return va  # fallback: treat as file offset

    def _parse_dynamic(self):
        """Parse PT_DYNAMIC segment to extract all dynamic tags."""
        for ph in self._phdrs:
            if ph["p_type"] != 2:  # PT_DYNAMIC
                continue
            dyndata = self._data[ph["p_offset"]:ph["p_offset"] + ph["p_filesz"]]
            pos = 0
            while pos + 16 <= len(dyndata):
                d_tag = struct.unpack_from("<q", dyndata, pos)[0]
                d_val = struct.unpack_from("<Q", dyndata, pos + 8)[0]
                if d_tag == 0:
                    break
                self._dyntags_list.append((d_tag, d_val))
                self._dyntags[d_tag] = d_val
                if d_tag == 1:  # DT_NEEDED
                    self._needed_offsets.append(d_val)
                elif d_tag == DT_SCE_NEEDED:
                    self._sce_needed_offsets.append(d_val)
                pos += 16
            break

    def _parse_strtab(self):
        """Load the dynamic string table (DT_STRTAB / DT_SCE_STRTAB)."""
        # Try standard DT_STRTAB first, then SCE variant
        strtab_va = self._dyntags.get(5, 0)  # DT_STRTAB
        strsz = self._dyntags.get(10, 0)  # DT_STRSZ
        if not strtab_va:
            strtab_va = self._dyntags.get(DT_SCE_STRTAB, 0)
            strsz = self._dyntags.get(DT_SCE_STRSZ, 0)
        if strtab_va and strsz:
            off = self._vaddr_to_offset(strtab_va)
            if off + strsz <= len(self._data):
                self._strtab = self._data[off:off + strsz]

    def _read_str(self, offset: int) -> str:
        """Read a null-terminated string from the string table."""
        if offset >= len(self._strtab):
            return ""
        end = self._strtab.find(b"\x00", offset)
        if end < 0:
            return self._strtab[offset:].decode("ascii", errors="replace")
        return self._strtab[offset:end].decode("ascii", errors="replace")

    def is_ps5_elf(self) -> bool:
        """Check if this is a PS5/PS4 SCE ELF."""
        return self.e_type in (ET_SCE_EXEC, ET_SCE_DYNEXEC, ET_SCE_DYNAMIC)

    def get_needed_libs(self) -> list[str]:
        """Return list of DT_NEEDED library names."""
        libs = []
        for off in self._needed_offsets:
            name = self._read_str(off)
            if name:
                libs.append(name)
        return libs

    def get_sce_needed_libs(self) -> list[str]:
        """Return list of DT_SCE_NEEDED library names (PS5 specific)."""
        libs = []
        for off in self._sce_needed_offsets:
            name = self._read_str(off)
            if name:
                libs.append(name)
        return libs

    def get_all_needed_libs(self) -> list[str]:
        """Return combined list of DT_NEEDED + DT_SCE_NEEDED (deduplicated)."""
        seen = set()
        result = []
        for name in self.get_needed_libs() + self.get_sce_needed_libs():
            if name not in seen:
                seen.add(name)
                result.append(name)
        return result

    def get_symbols(self) -> list[dict]:
        """Parse symbol table from DT_SYMTAB (or DT_SCE_SYMTAB).
        Returns list of {"name": str, "nid_encoded": str, "bind": int,
                         "type": int, "shndx": int, "value": int}
        """
        # Try standard SYMTAB, then SCE variant
        symtab_va = self._dyntags.get(6, 0)  # DT_SYMTAB
        syment = self._dyntags.get(11, 24)  # DT_SYMENT (default 24 for Elf64)
        if not symtab_va:
            symtab_va = self._dyntags.get(DT_SCE_SYMTAB, 0)

        # Determine symbol table size
        symtabsz = self._dyntags.get(DT_SCE_SYMTABSZ, 0)

        if not symtab_va:
            return []

        symtab_off = self._vaddr_to_offset(symtab_va)

        # If no explicit size, estimate from strtab position
        if not symtabsz:
            strtab_va = self._dyntags.get(5, self._dyntags.get(DT_SCE_STRTAB, 0))
            if strtab_va and strtab_va > symtab_va:
                strtab_off = self._vaddr_to_offset(strtab_va)
                symtabsz = strtab_off - symtab_off
            else:
                symtabsz = 0x10000  # reasonable limit

        symbols = []
        pos = symtab_off
        end = min(symtab_off + symtabsz, len(self._data))
        idx = 0
        while pos + syment <= end:
            st_name = struct.unpack_from("<I", self._data, pos)[0]
            st_info = self._data[pos + 4]
            st_other = self._data[pos + 5]
            st_shndx = struct.unpack_from("<H", self._data, pos + 6)[0]
            st_value = struct.unpack_from("<Q", self._data, pos + 8)[0]
            st_size = struct.unpack_from("<Q", self._data, pos + 16)[0]

            bind = (st_info >> 4)
            stype = st_info & 0xF

            name = self._read_str(st_name) if st_name < len(self._strtab) else ""

            if idx > 0:  # skip null symbol
                symbols.append({
                    "index": idx,
                    "name": name,
                    "bind": bind,
                    "type": stype,
                    "shndx": st_shndx,
                    "value": st_value,
                    "size": st_size,
                })

            pos += syment
            idx += 1

        return symbols

    def get_imported_symbols(self) -> list[dict]:
        """Return symbols with shndx=0 (undefined = imported).
        Each entry: {"name": str, "nid_encoded": str, "lib_suffix": str, "type": int}
        """
        result = []
        for sym in self.get_symbols():
            if sym["shndx"] == 0 and sym["name"]:
                # Parse NID-encoded name: "NID_BASE64#lib_suffix#module_suffix"
                parts = sym["name"].split("#")
                nid_part = parts[0] if parts else sym["name"]
                lib_suffix = parts[1] if len(parts) > 1 else ""
                mod_suffix = parts[2] if len(parts) > 2 else ""
                result.append({
                    "name": sym["name"],
                    "nid_encoded": nid_part,
                    "lib_suffix": lib_suffix,
                    "module_suffix": mod_suffix,
                    "type": sym["type"],
                    "bind": sym["bind"],
                })
        return result

    def get_exported_symbols(self) -> list[dict]:
        """Return symbols with shndx != 0 and global/weak binding."""
        result = []
        for sym in self.get_symbols():
            if (sym["shndx"] != 0 and sym["bind"] in (1, 2)  # STB_GLOBAL, STB_WEAK
                    and sym["type"] in (1, 2)  # STT_OBJECT, STT_FUNC
                    and sym["name"]):
                result.append({
                    "name": sym["name"],
                    "value": sym["value"],
                    "size": sym["size"],
                    "type": sym["type"],
                })
        return result

    def get_jmprel_entries(self) -> list[dict]:
        """Parse PLT relocation entries from DT_JMPREL / DT_SCE_JMPREL.
        Returns list of {"offset": int, "sym_idx": int, "type": int}
        """
        jmprel_va = self._dyntags.get(0x17, 0)  # DT_JMPREL standard
        pltrelsz = self._dyntags.get(2, 0)  # DT_PLTRELSZ standard
        if not jmprel_va:
            jmprel_va = self._dyntags.get(DT_SCE_JMPREL, 0)
            pltrelsz = self._dyntags.get(DT_SCE_PLTRELSZ, 0)
        if not jmprel_va or not pltrelsz:
            return []

        jmprel_off = self._vaddr_to_offset(jmprel_va)
        entries = []
        pos = jmprel_off
        end = min(jmprel_off + pltrelsz, len(self._data))
        while pos + 24 <= end:
            r_offset = struct.unpack_from("<Q", self._data, pos)[0]
            r_info = struct.unpack_from("<Q", self._data, pos + 8)[0]
            r_addend = struct.unpack_from("<q", self._data, pos + 16)[0]
            sym_idx = r_info >> 32
            rtype = r_info & 0xFFFFFFFF
            entries.append({
                "offset": r_offset,
                "sym_idx": int(sym_idx),
                "type": int(rtype),
                "addend": r_addend,
            })
            pos += 24
        return entries

    def get_text_segments(self) -> list[dict]:
        """Return executable LOAD segments (code sections)."""
        result = []
        for ph in self._phdrs:
            if ph["p_type"] == 1 and (ph["p_flags"] & 1):  # PT_LOAD + PF_X
                result.append({
                    "offset": ph["p_offset"],
                    "vaddr": ph["p_vaddr"],
                    "size": ph["p_filesz"],
                    "memsz": ph["p_memsz"],
                })
        return result

    def get_procparam(self) -> dict:
        """Parse PT_SCE_PROCPARAM or PT_SCE_MODULE_PARAM for SDK version info.
        eboot.bin uses PROCPARAM (0x61000001), .prx uses MODULE_PARAM (0x61000002).
        """
        sce_param_types = {PT_SCE_PROCPARAM, PT_SCE_MODULE_PARAM}
        valid_magics = {SCE_PROCESS_PARAM_MAGIC, SCE_MODULE_PARAM_MAGIC}

        for ph in self._phdrs:
            if ph["p_type"] not in sce_param_types:
                continue
            off = ph["p_offset"]
            sz = ph["p_filesz"]
            if sz < 0x18 or off + sz > len(self._data):
                continue
            # Validate magic at offset +0x08
            magic = struct.unpack_from("<I", self._data, off + 0x08)[0]
            if magic not in valid_magics:
                continue
            # PS4 SDK at +0x10, PS5 SDK at +0x14
            ps4_sdk = struct.unpack_from("<I", self._data, off + 0x10)[0] if sz >= 0x14 else 0
            ps5_sdk = struct.unpack_from("<I", self._data, off + 0x14)[0] if sz >= 0x18 else 0

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


# ---------------------------------------------------------------------------
# ELF Analyzer (high-level API)
# ---------------------------------------------------------------------------

class ELFAnalyzerError(Exception):
    pass


class ELFAnalyzer:
    """Analyzes a PS5 ELF/SELF/PRX for library dependencies and imported symbols.
    Uses raw binary parsing for PS5 ELFs (which have no section headers).
    Falls back to pyelftools for standard ELFs.
    """

    def __init__(self, elf_path: str):
        if not os.path.exists(elf_path):
            raise ELFAnalyzerError("File not found: {}".format(elf_path))
        self.elf_path = elf_path
        with open(elf_path, "rb") as f:
            self._data = f.read()
        self._is_self = is_self_file(self._data)

        # Get ELF data (strip SELF header if needed)
        if self._is_self:
            elf_data = extract_elf_from_self(self._data)
        else:
            elf_data = self._data

        # Use raw PS5 parser (works with stripped ELFs)
        self._ps5 = PS5ELFParser(elf_data)

        # Also try pyelftools for standard ELFs with section headers
        self._elf = None
        if _PYELFTOOLS and self._ps5.e_shnum > 0:
            try:
                self._elf = ELFFile(io.BytesIO(elf_data))
            except Exception:
                pass

    # ---- Public API --------------------------------------------------------

    def get_required_libs(self) -> list[str]:
        """Return list of required library names (DT_NEEDED + DT_SCE_NEEDED)."""
        return self._ps5.get_all_needed_libs()

    def get_imported_symbols(self) -> list[dict]:
        """Return list of imported symbols.
        Each entry: {"name": str, "nid_encoded": str, "lib_suffix": str,
                     "plt_offset": int | None}
        """
        imports = self._ps5.get_imported_symbols()

        # Try to enrich with PLT offsets from JMPREL
        jmprel = self._ps5.get_jmprel_entries()
        all_syms = self._ps5.get_symbols()
        sym_by_idx = {s["index"]: s for s in all_syms}

        import_by_name = {s["name"]: s for s in imports}
        for entry in jmprel:
            sym = sym_by_idx.get(entry["sym_idx"])
            if sym and sym["name"] in import_by_name:
                import_by_name[sym["name"]]["plt_offset"] = entry["offset"]

        return imports

    def get_exported_symbols(self) -> list[dict]:
        """Return list of symbols exported by this ELF."""
        return self._ps5.get_exported_symbols()

    def get_sdk_versions(self) -> dict:
        """Extract PS5/PS4 SDK version from PT_SCE_PROCPARAM."""
        return self._ps5.get_procparam()

    def generate_report(self, target_fw: str, exports_dir: str) -> dict:
        """Analyze compatibility against a target firmware version.
        Returns full report dict.
        """
        db = FirmwareExportsDB(exports_dir)

        required_libs = self.get_required_libs()
        imported = self.get_imported_symbols()
        exported = self.get_exported_symbols()
        sdk_info = self.get_sdk_versions()
        text_segs = self._ps5.get_text_segments()
        jmprel = self._ps5.get_jmprel_entries()

        # Determine which libs exist in the target firmware
        fw_libs = db.get_all_lib_names(target_fw)
        has_exports_db = len(fw_libs) > 0

        missing_libs = [lib for lib in required_libs if lib not in fw_libs] if has_exports_db else []

        # Check each imported symbol against exports DB
        found_symbols = []
        missing_symbols = []
        if has_exports_db:
            for sym in imported:
                nid_name = sym.get("nid_encoded", sym["name"])
                owning_lib = db.find_owning_lib(target_fw, nid_name)
                if owning_lib:
                    found_symbols.append({**sym, "lib": owning_lib})
                else:
                    severity = "critical" if any(
                        lib in missing_libs for lib in required_libs
                    ) else "warning"
                    missing_symbols.append({**sym, "severity": severity})

        # Compatibility score
        total = len(imported)
        if has_exports_db:
            score = int((len(found_symbols) / total) * 100) if total else 100
        else:
            # Without exports DB, assume compatible (no data to compare)
            score = 100

        # Code size from executable segments
        code_size = sum(s["size"] for s in text_segs)

        report = {
            "elf": os.path.basename(self.elf_path),
            "is_self": self._is_self,
            "is_sce": self._ps5.is_ps5_elf(),
            "elf_type": "0x{:04X}".format(self._ps5.e_type),
            "file_size": len(self._data),
            "code_size": code_size,
            "sdk_ps5": sdk_info["ps5_sdk_str"],
            "sdk_ps4": sdk_info["ps4_sdk_str"],
            "target_fw": target_fw,
            "required_libs": required_libs,
            "missing_libs": missing_libs,
            "total_imported": total,
            "total_exported": len(exported),
            "plt_entries": len(jmprel),
            "found_symbols": len(found_symbols),
            "missing_symbols": missing_symbols,
            "compatibility_score": score,
        }
        if self._is_self:
            report["self_info"] = get_self_info(self._data)
            if total == 0 and not required_libs:
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
