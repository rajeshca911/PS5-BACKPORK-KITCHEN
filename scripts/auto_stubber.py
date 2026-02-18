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
  python auto_stubber.py disasm --elf eboot.bin --offset 0x1000 --size 256
  python auto_stubber.py find-calls --elf eboot.bin --address 0x12340
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
X64_INT3    = b"\xcc"                     # INT3 / breakpoint

X64_STUB_MODES: dict[str, bytes] = {
    "nop":       X64_NOP * 16,                                                  # 16 bytes
    "ret_zero":  X64_XOR_EAX + X64_RET + X64_NOP * 13,                        # 16 bytes
    "ret_error": X64_MOV_NEG + X64_RET + X64_NOP * 10,                        # 16 bytes
}

# ---------------------------------------------------------------------------
# ARM64 Stub Bytecodes (for future ARM64 ELF support)
# ---------------------------------------------------------------------------

ARM64_NOP     = b"\x1f\x20\x03\xd5"
ARM64_MOV_XZR = b"\xe0\x03\x1f\xaa"      # MOV X0, XZR
ARM64_MOV_NEG = b"\xe0\x03\x1f\x92"      # MOV X0, #-1 (MOVN X0, #0)
ARM64_RET     = b"\xc0\x03\x5f\xd6"

ARM64_STUB_MODES: dict[str, bytes] = {
    "nop":       ARM64_NOP * 4,                                                # 16 bytes
    "ret_zero":  ARM64_MOV_XZR + ARM64_RET + ARM64_NOP + ARM64_NOP,          # 16 bytes
    "ret_error": ARM64_MOV_NEG + ARM64_RET + ARM64_NOP + ARM64_NOP,          # 16 bytes
}

PLT_ENTRY_SIZE = 16


# ---------------------------------------------------------------------------
# Raw ELF Parser for PS5 (stripped, no section headers)
# ---------------------------------------------------------------------------

class _RawELF:
    """Minimal ELF parser using only program headers.
    Works with PS5 stripped ELFs (e_shnum=0).
    """

    # Dynamic tag IDs
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
        self._symtabsz = 0
        self._parse()

    def _parse(self):
        # Parse program headers
        for i in range(self.e_phnum):
            off = self.e_phoff + i * self.e_phentsize
            p_type = struct.unpack_from("<I", self.data, off)[0]
            p_offset = struct.unpack_from("<Q", self.data, off + 8)[0]
            p_vaddr = struct.unpack_from("<Q", self.data, off + 16)[0]
            p_filesz = struct.unpack_from("<Q", self.data, off + 32)[0]

            if p_type == 1:  # PT_LOAD
                self._loads.append((p_vaddr, p_offset, p_filesz))
            elif p_type == 2:  # PT_DYNAMIC
                self._parse_dynamic(p_offset, p_filesz)

        # Load string table
        strtab_va = self._dyntags.get(self.DT_STRTAB,
                     self._dyntags.get(self.DT_SCE_STRTAB, 0))
        strsz = self._dyntags.get(self.DT_STRSZ,
                 self._dyntags.get(self.DT_SCE_STRSZ, 0))
        if strtab_va and strsz:
            off = self._va2off(strtab_va)
            if off + strsz <= len(self.data):
                self._strtab = self.data[off:off + strsz]
                self._strsz = strsz

        # Symbol table
        symtab_va = self._dyntags.get(self.DT_SYMTAB,
                     self._dyntags.get(self.DT_SCE_SYMTAB, 0))
        if symtab_va:
            self._symtab_off = self._va2off(symtab_va)
        self._syment = self._dyntags.get(self.DT_SYMENT, 24)
        self._symtabsz = self._dyntags.get(self.DT_SCE_SYMTABSZ, 0)

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
        """Return (name, bind, type, shndx, value) for symbol at index."""
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
        """Return list of (r_offset, sym_idx, r_type, r_addend)."""
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
        """Return list of (file_offset, vaddr, size) for executable segments."""
        result = []
        for i in range(self.e_phnum):
            off = self.e_phoff + i * self.e_phentsize
            p_type = struct.unpack_from("<I", self.data, off)[0]
            p_flags = struct.unpack_from("<I", self.data, off + 4)[0]
            p_offset = struct.unpack_from("<Q", self.data, off + 8)[0]
            p_vaddr = struct.unpack_from("<Q", self.data, off + 16)[0]
            p_filesz = struct.unpack_from("<Q", self.data, off + 32)[0]
            if p_type == 1 and (p_flags & 1):  # PT_LOAD + PF_X
                result.append((p_offset, p_vaddr, p_filesz))
        return result

    def is_x86_64(self):
        return self.e_machine == 0x3E  # EM_X86_64

    def is_aarch64(self):
        return self.e_machine == 0xB7  # EM_AARCH64


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

        # Select architecture-specific stubs
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

        # Try to locate PLT region by scanning for PLT patterns near GOT
        self._plt_base = self._find_plt_base()

        # Init capstone for disassembly
        self._cs = None
        if _CAPSTONE:
            if self._raw.is_x86_64():
                self._cs = Cs(CS_ARCH_X86, CS_MODE_64)
            elif self._raw.is_aarch64():
                self._cs = Cs(CS_ARCH_AARCH64, CS_MODE_ARM)
            if self._cs:
                self._cs.detail = True

    def _find_plt_base(self) -> int:
        """Locate PLT start by scanning code segments for PLT entry patterns.
        x86_64 PLT entries: FF 25 (jmp *GOT(%rip)) as first 2 bytes.
        We look for a cluster of FF 25 instructions spaced 16 bytes apart.
        """
        pltgot_va = self._raw.get_pltgot_va()
        if not pltgot_va:
            return 0

        # Scan executable segments for PLT-like patterns
        for seg_off, seg_va, seg_sz in self._raw.get_text_segments():
            # PLT is usually near the end of the first text segment
            # or at the beginning. Scan for FF 25 pattern clusters.
            scan_start = max(0, seg_sz - 0x20000)  # last 128KB
            data = self._data[seg_off + scan_start:seg_off + seg_sz]
            base_off = seg_off + scan_start

            # Find first occurrence of FF 25 that starts a 16-byte aligned block
            for i in range(0, len(data) - PLT_ENTRY_SIZE, PLT_ENTRY_SIZE):
                if data[i] == 0xFF and data[i + 1] == 0x25:
                    # Verify next entries also start with FF 25
                    count = 0
                    for j in range(i, min(i + PLT_ENTRY_SIZE * 10, len(data)), PLT_ENTRY_SIZE):
                        if data[j] == 0xFF and data[j + 1] == 0x25:
                            count += 1
                    if count >= 3:
                        return base_off + i
        return 0

    # ---- PLT Resolution (raw, no sections needed) --------------------------

    def find_plt_entry_by_got(self, symbol_name: str) -> Optional[int]:
        """Find PLT entry for a symbol by matching its GOT relocation
        to a PLT jmp instruction that references the same GOT slot.
        """
        jmprel_info = self._sym_to_jmprel.get(symbol_name)
        if not jmprel_info:
            return None

        got_va = jmprel_info[0]  # r_offset = GOT entry virtual address

        if self._raw.is_x86_64():
            return self._find_plt_entry_x64(got_va)
        elif self._raw.is_aarch64():
            return self._find_plt_entry_arm64(got_va)
        return None

    def _find_plt_entry_x64(self, got_va: int) -> Optional[int]:
        """Find x86_64 PLT entry that jumps to a specific GOT address.
        PLT entry: FF 25 xx xx xx xx (jmp *disp32(%rip))
        The target GOT addr = PLT_entry_VA + 6 + disp32
        """
        for seg_off, seg_va, seg_sz in self._raw.get_text_segments():
            # Scan this segment for "FF 25" instructions
            data = self._data[seg_off:seg_off + seg_sz]
            for i in range(0, len(data) - 6, 1):
                if data[i] == 0xFF and data[i + 1] == 0x25:
                    disp32 = struct.unpack_from("<i", data, i + 2)[0]
                    insn_va = seg_va + i
                    target = insn_va + 6 + disp32  # RIP-relative
                    if target == got_va:
                        # Found it — return file offset aligned to 16 bytes
                        entry_offset = seg_off + i
                        # Align to PLT entry boundary
                        aligned = entry_offset - (entry_offset % PLT_ENTRY_SIZE)
                        if abs(aligned - entry_offset) <= 6:
                            return aligned
                        return entry_offset
        return None

    def _find_plt_entry_arm64(self, got_va: int) -> Optional[int]:
        """Find ARM64 PLT entry — ADRP+LDR+BR pattern."""
        # For ARM64 we'd need to decode ADRP+ADD/LDR pairs
        # Simplified: use JMPREL index to estimate PLT position
        return None

    # ---- Stubbing ----------------------------------------------------------

    def stub_plt_entry(self, symbol_name: str, mode: str = "ret_zero",
                       dry_run: bool = False) -> bool:
        """Overwrite the PLT entry for symbol_name with a stub.
        Returns True on success, False if symbol not found in PLT.
        """
        if mode not in self._stub_modes:
            raise AutoStubberError(
                "Unknown stub mode '{}'. Choose: {}".format(mode, list(self._stub_modes)))

        file_off = self.find_plt_entry_by_got(symbol_name)
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
        missing_symbols: list of {"name": str, ...} dicts.
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
        """Patch a GOT entry to point to a specific virtual address.
        Useful for redirecting symbol resolution to stub functions.
        """
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

    # ---- Call finder --------------------------------------------------------

    def find_calls_to(self, target_vaddr: int) -> list[int]:
        """Find all CALL/BL instructions that target a specific address.
        Returns list of file offsets.
        """
        if not self._cs:
            return []

        results = []
        for seg_off, seg_va, seg_sz in self._raw.get_text_segments():
            code = bytes(self._data[seg_off:seg_off + seg_sz])
            for insn in self._cs.disasm(code, seg_va):
                mnemonic = insn.mnemonic
                if mnemonic in ("call", "bl"):
                    try:
                        op_str = insn.op_str.strip().lstrip("#")
                        dest = int(op_str, 0) if op_str.startswith("0") else int(op_str, 16)
                    except (ValueError, TypeError):
                        continue
                    if dest == target_vaddr:
                        file_off = seg_off + (insn.address - seg_va)
                        results.append(file_off)
        return results

    def patch_call_to_nop(self, call_offsets: list[int],
                          dry_run: bool = False) -> int:
        """Replace CALL/BL instructions with NOP at given file offsets."""
        count = 0
        for off in call_offsets:
            if self._raw.is_x86_64():
                # x86_64 CALL is 5 bytes: E8 xx xx xx xx
                if off + 5 > len(self._data):
                    continue
                if not dry_run:
                    self._data[off:off + 5] = X64_NOP * 5
                count += 1
            elif self._raw.is_aarch64():
                # ARM64 BL is 4 bytes
                if off + 4 > len(self._data):
                    continue
                if not dry_run:
                    self._data[off:off + 4] = ARM64_NOP
                count += 1
        return count

    # ---- Disassemble -------------------------------------------------------

    def disassemble_at(self, file_offset: int, size: int = 256,
                       max_insns: int = 50):
        """Disassemble code at a file offset."""
        if not self._cs:
            print("[WARN] capstone not available for disassembly")
            return

        # Find containing segment to get VA
        va_base = file_offset
        for seg_off, seg_va, seg_sz in self._raw.get_text_segments():
            if seg_off <= file_offset < seg_off + seg_sz:
                va_base = seg_va + (file_offset - seg_off)
                break

        code = bytes(self._data[file_offset:file_offset + size])
        count = 0
        for insn in self._cs.disasm(code, va_base):
            print("  0x{:08X}  {:12s} {}".format(
                insn.address, insn.mnemonic, insn.op_str))
            count += 1
            if count >= max_insns:
                break

    # ---- Info ---------------------------------------------------------------

    def get_info(self) -> dict:
        """Return summary info about the ELF."""
        text_segs = self._raw.get_text_segments()
        code_size = sum(sz for _, _, sz in text_segs)
        return {
            "arch": self._arch,
            "plt_base": self._plt_base,
            "jmprel_count": len(self._jmprel),
            "symbols_resolved": len(self._sym_to_jmprel),
            "code_size": code_size,
            "text_segments": len(text_segs),
        }

    # ---- Save --------------------------------------------------------------

    def save(self, output_path: str):
        """Write the (potentially modified) data to output_path."""
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
    print("[STUB] Arch: {} | PLT entries: {} | Symbols mapped: {}".format(
        info["arch"], info["jmprel_count"], info["symbols_resolved"]))

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


def cmd_disasm(args):
    try:
        stubber = AutoStubber(args.elf)
        offset = int(args.offset, 0)
        stubber.disassemble_at(offset, size=args.size, max_insns=args.limit)
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)


def cmd_find_calls(args):
    try:
        stubber = AutoStubber(args.elf)
        addr = int(args.address, 0)
        offsets = stubber.find_calls_to(addr)
        if not offsets:
            print("No calls to 0x{:X} found.".format(addr))
        else:
            print("Calls to 0x{:X} at file offsets:".format(addr))
            for off in offsets:
                print("  0x{:X}".format(off))
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)


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

    p_disasm = sub.add_parser("disasm", help="Disassemble at file offset")
    p_disasm.add_argument("--elf", required=True)
    p_disasm.add_argument("--offset", required=True, help="File offset (hex)")
    p_disasm.add_argument("--size", type=int, default=256)
    p_disasm.add_argument("--limit", type=int, default=50)

    p_find = sub.add_parser("find-calls", help="Find CALL instructions targeting an address")
    p_find.add_argument("--elf", required=True)
    p_find.add_argument("--address", required=True, help="Target virtual address (hex)")

    parsed = parser.parse_args()
    {"stub": cmd_stub, "disasm": cmd_disasm, "find-calls": cmd_find_calls}[parsed.cmd](parsed)
