"""
Auto-Stubber — Patches missing PLT entries in PS5 ELF/PRX files
with safe stub implementations (NOP, ret-zero, ret-error).

PS5 uses x86_64 architecture (AMD Zen 2). ELFs are stripped (no section
headers), so PLT/GOT must be located via program headers and dynamic tags.

x86_64 PLT entry layout (16 bytes):
  jmp    *GOT_ENTRY(%rip)    ; FF 25 xx xx xx xx (6 bytes)
  push   $index              ; 68 xx xx xx xx    (5 bytes)
  jmp    PLT0                ; E9 xx xx xx xx    (5 bytes)

Stub modes (16 bytes to fill a PLT slot):
  nop       : 16 x NOP (0x90)
  ret_zero  : XOR EAX,EAX ; RET ; NOP padding
  ret_error : MOV EAX,-1  ; RET ; NOP padding

Dependencies:
  pip install capstone pyelftools

Usage:
  python auto_stubber.py stub --elf eboot.bin --missing missing.json --mode ret_zero --output eboot_stubbed.bin
  python auto_stubber.py stub --elf eboot.bin --missing missing.json --dry-run
  python auto_stubber.py info --elf eboot.bin
"""

import argparse
import json
import os
import struct
import sys
from typing import Optional

try:
    from capstone import Cs, CS_ARCH_X86, CS_MODE_64, CS_ARCH_AARCH64, CS_MODE_ARM
    _CAPSTONE = True
except ImportError:
    _CAPSTONE = False

try:
    from elftools.elf.elffile import ELFFile
    from elftools.elf.sections import SymbolTableSection
    _PYELFTOOLS = True
except ImportError:
    _PYELFTOOLS = False


# ---------------------------------------------------------------------------
# x86_64 Stub Bytecodes (pre-assembled, 16 bytes each)
# ---------------------------------------------------------------------------

X64_NOP     = b"\x90"
X64_RET     = b"\xc3"
X64_XOR_EAX = b"\x31\xc0"                # XOR EAX, EAX (2 bytes)
X64_MOV_NEG = b"\xb8\xff\xff\xff\xff"     # MOV EAX, -1 (5 bytes)

X64_STUB_MODES: dict[str, bytes] = {
    "nop":       X64_NOP * 16,                                                  # 16 bytes
    "ret_zero":  X64_XOR_EAX + X64_RET + X64_NOP * 13,                        # 16 bytes
    "ret_error": X64_MOV_NEG + X64_RET + X64_NOP * 10,                        # 16 bytes
}

# ---------------------------------------------------------------------------
# ARM64 Stub Bytecodes (for future ARM64 ELF support)
# ---------------------------------------------------------------------------

ARM64_NOP     = b"\x1f\x20\x03\xd5"
ARM64_MOV_XZR = b"\xe0\x03\x1f\xaa"
ARM64_MOV_NEG = b"\xe0\x03\x1f\x92"
ARM64_RET     = b"\xc0\x03\x5f\xd6"

ARM64_STUB_MODES: dict[str, bytes] = {
    "nop":       ARM64_NOP * 4,
    "ret_zero":  ARM64_MOV_XZR + ARM64_RET + ARM64_NOP + ARM64_NOP,
    "ret_error": ARM64_MOV_NEG + ARM64_RET + ARM64_NOP + ARM64_NOP,
}

PLT_ENTRY_SIZE = 16


# ---------------------------------------------------------------------------
# Raw ELF Parser for PS5 (stripped, no section headers)
# ---------------------------------------------------------------------------

class _RawELF:
    """Minimal ELF parser using only program headers."""

    DT_NEEDED = 1
    DT_STRTAB = 5
    DT_SYMTAB = 6
    DT_STRSZ = 10
    DT_SYMENT = 11
    DT_JMPREL = 0x17
    DT_PLTRELSZ = 2
    DT_PLTGOT = 3
    DT_SCE_SYMTAB = 0x61000011
    DT_SCE_STRTAB = 0x61000013
    DT_SCE_STRSZ = 0x61000015
    DT_SCE_JMPREL = 0x61000041
    DT_SCE_PLTRELSZ = 0x6100003D
    DT_SCE_PLTGOT = 0x6100003B
    DT_SCE_SYMTABSZ = 0x61000025

    def __init__(self, data: bytes):
        self.data = data
        self.e_machine = struct.unpack_from("<H", data, 18)[0]
        self.e_phoff = struct.unpack_from("<Q", data, 32)[0]
        self.e_phentsize = struct.unpack_from("<H", data, 54)[0]
        self.e_phnum = struct.unpack_from("<H", data, 56)[0]
        self.e_shnum = struct.unpack_from("<H", data, 60)[0]

        self._loads = []
        self._dyntags = {}
        self._strtab = b""
        self._strsz = 0
        self._symtab_off = 0
        self._syment = 24
        self._parse()

    def _parse(self):
        for i in range(self.e_phnum):
            off = self.e_phoff + i * self.e_phentsize
            p_type = struct.unpack_from("<I", self.data, off)[0]
            p_offset = struct.unpack_from("<Q", self.data, off + 8)[0]
            p_vaddr = struct.unpack_from("<Q", self.data, off + 16)[0]
            p_filesz = struct.unpack_from("<Q", self.data, off + 32)[0]
            if p_type == 1:
                self._loads.append((p_vaddr, p_offset, p_filesz))
            elif p_type == 2:
                self._parse_dynamic(p_offset, p_filesz)

        strtab_va = self._dyntags.get(self.DT_STRTAB,
                     self._dyntags.get(self.DT_SCE_STRTAB, 0))
        strsz = self._dyntags.get(self.DT_STRSZ,
                 self._dyntags.get(self.DT_SCE_STRSZ, 0))
        if strtab_va and strsz:
            off = self._va2off(strtab_va)
            if off + strsz <= len(self.data):
                self._strtab = self.data[off:off + strsz]
                self._strsz = strsz

        symtab_va = self._dyntags.get(self.DT_SYMTAB,
                     self._dyntags.get(self.DT_SCE_SYMTAB, 0))
        if symtab_va:
            self._symtab_off = self._va2off(symtab_va)
        self._syment = self._dyntags.get(self.DT_SYMENT, 24)

    def _parse_dynamic(self, offset, size):
        pos = offset
        end = offset + size
        while pos + 16 <= end and pos + 16 <= len(self.data):
            d_tag = struct.unpack_from("<q", self.data, pos)[0]
            d_val = struct.unpack_from("<Q", self.data, pos + 8)[0]
            if d_tag == 0:
                break
            self._dyntags[d_tag] = d_val
            pos += 16

    def _va2off(self, va):
        for v, o, sz in self._loads:
            if v <= va < v + sz:
                return o + (va - v)
        return va

    def _readstr(self, off):
        if off >= self._strsz:
            return ""
        end = self._strtab.find(b"\x00", off)
        if end < 0:
            return self._strtab[off:].decode("ascii", errors="replace")
        return self._strtab[off:end].decode("ascii", errors="replace")

    def get_symbol(self, idx):
        pos = self._symtab_off + idx * self._syment
        if pos + self._syment > len(self.data):
            return None
        st_name = struct.unpack_from("<I", self.data, pos)[0]
        st_info = self.data[pos + 4]
        st_shndx = struct.unpack_from("<H", self.data, pos + 6)[0]
        st_value = struct.unpack_from("<Q", self.data, pos + 8)[0]
        name = self._readstr(st_name) if st_name < self._strsz else ""
        return (name, st_info >> 4, st_info & 0xF, st_shndx, st_value)

    def get_jmprel(self):
        va = self._dyntags.get(self.DT_JMPREL,
              self._dyntags.get(self.DT_SCE_JMPREL, 0))
        sz = self._dyntags.get(self.DT_PLTRELSZ,
              self._dyntags.get(self.DT_SCE_PLTRELSZ, 0))
        if not va or not sz:
            return []
        off = self._va2off(va)
        entries = []
        pos = off
        end = min(off + sz, len(self.data))
        while pos + 24 <= end:
            r_offset = struct.unpack_from("<Q", self.data, pos)[0]
            r_info = struct.unpack_from("<Q", self.data, pos + 8)[0]
            r_addend = struct.unpack_from("<q", self.data, pos + 16)[0]
            entries.append((r_offset, int(r_info >> 32), int(r_info & 0xFFFFFFFF), r_addend))
            pos += 24
        return entries

    def get_pltgot_va(self):
        return self._dyntags.get(self.DT_PLTGOT,
                self._dyntags.get(self.DT_SCE_PLTGOT, 0))

    def get_text_segments(self):
        result = []
        for i in range(self.e_phnum):
            off = self.e_phoff + i * self.e_phentsize
            p_type = struct.unpack_from("<I", self.data, off)[0]
            p_flags = struct.unpack_from("<I", self.data, off + 4)[0]
            p_offset = struct.unpack_from("<Q", self.data, off + 8)[0]
            p_vaddr = struct.unpack_from("<Q", self.data, off + 16)[0]
            p_filesz = struct.unpack_from("<Q", self.data, off + 32)[0]
            if p_type == 1 and (p_flags & 1):
                result.append((p_offset, p_vaddr, p_filesz))
        return result

    def is_x86_64(self):
        return self.e_machine == 0x3E

    def is_aarch64(self):
        return self.e_machine == 0xB7


# ---------------------------------------------------------------------------
# Auto-Stubber
# ---------------------------------------------------------------------------

class AutoStubberError(Exception):
    pass


class AutoStubber:
    """Patches PLT/GOT entries in PS5 ELFs with stub implementations.
    Supports both x86_64 (PS5) and ARM64 architectures.
    Works with stripped ELFs (no section headers) using raw parsing.
    """

    def __init__(self, elf_path: str):
        if not os.path.exists(elf_path):
            raise AutoStubberError("File not found: {}".format(elf_path))
        self.elf_path = elf_path
        with open(elf_path, "rb") as f:
            self._data = bytearray(f.read())

        if self._data[:4] != b"\x7fELF":
            raise AutoStubberError("Not a valid ELF file")

        self._raw = _RawELF(bytes(self._data))

        if self._raw.is_x86_64():
            self._arch = "x86_64"
            self._stub_modes = X64_STUB_MODES
        elif self._raw.is_aarch64():
            self._arch = "aarch64"
            self._stub_modes = ARM64_STUB_MODES
        else:
            self._arch = "unknown"
            self._stub_modes = X64_STUB_MODES

        # Build symbol-to-JMPREL mapping
        self._jmprel = self._raw.get_jmprel()
        self._sym_to_jmprel = {}
        for r_offset, sym_idx, r_type, r_addend in self._jmprel:
            sym_info = self._raw.get_symbol(sym_idx)
            if sym_info and sym_info[0]:
                self._sym_to_jmprel[sym_info[0]] = (r_offset, sym_idx, r_type)

        # Pre-build GOT_VA -> PLT file offset lookup table (one-time scan)
        self._got_to_plt: dict[int, int] = {}
        self._build_plt_map()

        # Init capstone for disassembly (lazy)
        self._cs = None

    def _build_plt_map(self):
        """Scan code segments once to build GOT_VA -> PLT_file_offset map.
        Finds all x86_64 PLT entries (FF 25 jmp *disp32(%rip)) and maps
        each GOT target address to the PLT entry's file offset.
        """
        if not self._raw.is_x86_64():
            return

        for seg_off, seg_va, seg_sz in self._raw.get_text_segments():
            data = self._data[seg_off:seg_off + seg_sz]
            i = 0
            while i < len(data) - 6:
                if data[i] == 0xFF and data[i + 1] == 0x25:
                    disp32 = struct.unpack_from("<i", data, i + 2)[0]
                    insn_va = seg_va + i
                    got_target = insn_va + 6 + disp32
                    file_off = seg_off + i
                    # Align to 16-byte PLT entry boundary
                    aligned = file_off - (file_off % PLT_ENTRY_SIZE)
                    if abs(aligned - file_off) <= 6:
                        self._got_to_plt[got_target] = aligned
                    else:
                        self._got_to_plt[got_target] = file_off
                    i += PLT_ENTRY_SIZE  # skip to next PLT entry
                else:
                    i += 1

    def _get_cs(self):
        """Lazy-init capstone disassembler."""
        if self._cs is None and _CAPSTONE:
            if self._raw.is_x86_64():
                self._cs = Cs(CS_ARCH_X86, CS_MODE_64)
            elif self._raw.is_aarch64():
                self._cs = Cs(CS_ARCH_AARCH64, CS_MODE_ARM)
            if self._cs:
                self._cs.detail = True
        return self._cs

    # ---- PLT Resolution (fast O(1) lookup) ---------------------------------

    def find_plt_entry(self, symbol_name: str) -> Optional[int]:
        """Find PLT file offset for a symbol. O(1) lookup via pre-built map."""
        jmprel_info = self._sym_to_jmprel.get(symbol_name)
        if not jmprel_info:
            return None
        got_va = jmprel_info[0]  # r_offset = GOT entry virtual address
        return self._got_to_plt.get(got_va)

    # ---- Stubbing ----------------------------------------------------------

    def stub_plt_entry(self, symbol_name: str, mode: str = "ret_zero",
                       dry_run: bool = False) -> bool:
        """Overwrite the PLT entry for symbol_name with a stub."""
        if mode not in self._stub_modes:
            raise AutoStubberError(
                "Unknown stub mode '{}'. Choose: {}".format(mode, list(self._stub_modes)))

        file_off = self.find_plt_entry(symbol_name)
        if file_off is None:
            return False

        stub = self._stub_modes[mode]
        if len(self._data) < file_off + len(stub):
            return False

        if not dry_run:
            self._data[file_off:file_off + len(stub)] = stub
        return True

    def stub_missing(self, missing_symbols: list[dict], mode: str = "ret_zero",
                     dry_run: bool = False) -> dict:
        """Stub all symbols in the missing list.
        Returns {"stubbed": [name, ...], "not_found": [name, ...]}
        """
        stubbed = []
        not_found = []
        for sym in missing_symbols:
            name = sym.get("name", "")
            if not name:
                continue
            ok = self.stub_plt_entry(name, mode=mode, dry_run=dry_run)
            (stubbed if ok else not_found).append(name)
        return {"stubbed": stubbed, "not_found": not_found}

    # ---- GOT Patching (alternative to PLT stubbing) -------------------------

    def patch_got_entry(self, symbol_name: str, target_va: int,
                        dry_run: bool = False) -> bool:
        """Patch a GOT entry to redirect a symbol to a different address."""
        jmprel_info = self._sym_to_jmprel.get(symbol_name)
        if not jmprel_info:
            return False
        got_va = jmprel_info[0]
        got_off = self._raw._va2off(got_va)
        if got_off + 8 > len(self._data):
            return False
        if not dry_run:
            struct.pack_into("<Q", self._data, got_off, target_va)
        return True

    # ---- Info ---------------------------------------------------------------

    def get_info(self) -> dict:
        text_segs = self._raw.get_text_segments()
        code_size = sum(sz for _, _, sz in text_segs)
        return {
            "arch": self._arch,
            "jmprel_count": len(self._jmprel),
            "symbols_resolved": len(self._sym_to_jmprel),
            "plt_mapped": len(self._got_to_plt),
            "code_size": code_size,
            "text_segments": len(text_segs),
        }

    # ---- Save --------------------------------------------------------------

    def save(self, output_path: str):
        os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
        with open(output_path, "wb") as f:
            f.write(self._data)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def cmd_stub(args):
    try:
        stubber = AutoStubber(args.elf)
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)

    info = stubber.get_info()
    print("[STUB] Arch: {} | JMPREL: {} | PLT mapped: {} | Symbols: {}".format(
        info["arch"], info["jmprel_count"], info["plt_mapped"], info["symbols_resolved"]))

    missing = []
    if args.missing:
        with open(args.missing, encoding="utf-8") as f:
            missing = json.load(f)
    elif args.symbol:
        missing = [{"name": args.symbol}]

    if not missing:
        print("[WARN] No symbols to stub.")
        return

    results = stubber.stub_missing(missing, mode=args.mode, dry_run=args.dry_run)
    print("\n[SUMMARY] stubbed={} not_found={}".format(
        len(results["stubbed"]), len(results["not_found"])))

    if not args.dry_run and results["stubbed"]:
        out = args.output or args.elf
        stubber.save(out)
        print("[STUB] Saved -> {}".format(out))


def cmd_info(args):
    try:
        stubber = AutoStubber(args.elf)
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)
    info = stubber.get_info()
    for k, v in info.items():
        if isinstance(v, int) and v > 1024:
            print("  {}: {} ({}KB)".format(k, v, v // 1024))
        else:
            print("  {}: {}".format(k, v))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Auto-Stubber — stub missing PLT entries in PS5 ELFs")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_stub = sub.add_parser("stub", help="Stub PLT entries for missing symbols")
    p_stub.add_argument("--elf", required=True)
    grp = p_stub.add_mutually_exclusive_group()
    grp.add_argument("--missing", help="JSON file with missing symbols (list of {name:...})")
    grp.add_argument("--symbol", help="Single symbol name to stub")
    p_stub.add_argument("--mode", choices=list(X64_STUB_MODES), default="ret_zero")
    p_stub.add_argument("--output", help="Output path (default: overwrite input)")
    p_stub.add_argument("--dry-run", action="store_true",
                        help="Show what would be patched without writing")

    p_info = sub.add_parser("info", help="Show ELF info (arch, PLT, symbols)")
    p_info.add_argument("--elf", required=True)

    parsed = parser.parse_args()
    {"stub": cmd_stub, "info": cmd_info}[parsed.cmd](parsed)
