"""
Auto-Stubber — Patches missing ARM64 PLT entries in PS5 ELF/PRX files
with safe stub implementations (NOP, ret-zero, ret-error).

ARM64 PLT entry layout (16 bytes):
  ADRP  X16, page
  ADD   X16, X16, offset
  LDR   X17, [X16]
  BR    X17

Stub modes (16 bytes to fill a PLT slot):
  nop       : 4 × NOP
  ret_zero  : MOV X0, XZR ; RET ; NOP ; NOP
  ret_error : MOV X0, #-1 ; RET ; NOP ; NOP

Dependencies:
  pip install capstone keystone-engine pyelftools

Usage:
  python auto_stubber.py stub --elf eboot.bin --missing missing.json --mode ret_zero --output eboot_stubbed.bin
  python auto_stubber.py stub --elf eboot.bin --missing missing.json --dry-run
  python auto_stubber.py disasm --elf eboot.bin --section .plt
  python auto_stubber.py find-calls --elf eboot.bin --address 0x12340
"""

import argparse
import json
import os
import struct
import sys
from typing import Optional

try:
    from capstone import Cs, CS_ARCH_AARCH64, CS_MODE_ARM
    _CAPSTONE = True
except ImportError:
    _CAPSTONE = False

try:
    from keystone import Ks, KS_ARCH_ARM64, KS_MODE_LITTLE_ENDIAN
    _KEYSTONE = True
except ImportError:
    _KEYSTONE = False

try:
    from elftools.elf.elffile import ELFFile
    from elftools.elf.sections import SymbolTableSection
    _PYELFTOOLS = True
except ImportError:
    _PYELFTOOLS = False


# ---------------------------------------------------------------------------
# ARM64 Stub Bytecodes (pre-assembled, 16 bytes each)
# ---------------------------------------------------------------------------

NOP_INSN    = b"\x1f\x20\x03\xd5"               # NOP
MOV_X0_XZR  = b"\xe0\x03\x1f\xaa"              # MOV X0, XZR
MOV_X0_NEG1 = b"\xe0\x03\x1f\x92"              # MOV X0, #-1  (actually MOVN X0, #0)
RET_INSN    = b"\xc0\x03\x5f\xd6"               # RET

STUB_MODES: dict[str, bytes] = {
    "nop":       NOP_INSN * 4,                                                    # 16 bytes
    "ret_zero":  MOV_X0_XZR + RET_INSN + NOP_INSN + NOP_INSN,                   # 16 bytes
    "ret_error": MOV_X0_NEG1 + RET_INSN + NOP_INSN + NOP_INSN,                  # 16 bytes
}


# ---------------------------------------------------------------------------
# Auto-Stubber
# ---------------------------------------------------------------------------

class AutoStubberError(Exception):
    pass


class AutoStubber:
    """Patches PLT entries in an ARM64 ELF with stub implementations."""

    PLT_ENTRY_SIZE = 16  # standard ARM64 PLT entry

    def __init__(self, elf_path: str):
        self._check_deps()
        if not os.path.exists(elf_path):
            raise AutoStubberError("File not found: {}".format(elf_path))
        self.elf_path = elf_path
        with open(elf_path, "rb") as f:
            self._data = bytearray(f.read())
        self._elf_obj = ELFFile(open(elf_path, "rb"))
        self._cs = Cs(CS_ARCH_AARCH64, CS_MODE_ARM)
        self._cs.detail = True

    @staticmethod
    def _check_deps():
        missing = []
        if not _CAPSTONE:   missing.append("capstone")
        if not _PYELFTOOLS: missing.append("pyelftools")
        if missing:
            raise AutoStubberError(
                "Missing dependencies: {}. Run: pip install {}".format(
                    ", ".join(missing), " ".join(missing)))

    # ---- PLT Resolution ----------------------------------------------------

    def _get_plt_info(self) -> tuple[Optional[int], Optional[int]]:
        """Return (plt_vaddr, plt_offset_in_file) or (None, None)."""
        section = self._elf_obj.get_section_by_name(".plt")
        if not section:
            return None, None
        vaddr = section.header.sh_addr
        offset = section.header.sh_offset
        return vaddr, offset

    def _get_dynsym(self):
        return self._elf_obj.get_section_by_name(".dynsym")

    def _get_rela_plt(self):
        return self._elf_obj.get_section_by_name(".rela.plt")

    def find_plt_entry(self, symbol_name: str) -> Optional[int]:
        """Return file offset of the PLT entry for the given symbol, or None."""
        plt_vaddr, plt_offset = self._get_plt_info()
        if plt_vaddr is None:
            return None

        dynsym = self._get_dynsym()
        rela_plt = self._get_rela_plt()
        if not dynsym or not rela_plt:
            return None

        # Build symbol index → name map
        sym_idx_to_name = {}
        for i, sym in enumerate(dynsym.iter_symbols()):
            if sym.name:
                sym_idx_to_name[i] = sym.name

        # Find relocation entry for our symbol
        # PLT[0] is the resolver trampoline; PLT[1] = first real entry
        for rel_idx, rel in enumerate(rela_plt.iter_relocations()):
            sym_idx = rel["r_info_sym"]
            if sym_idx_to_name.get(sym_idx) == symbol_name:
                # PLT entry index = rel_idx + 1 (PLT[0] is the resolver)
                plt_entry_index = rel_idx + 1
                file_off = plt_offset + plt_entry_index * self.PLT_ENTRY_SIZE
                return file_off

        return None

    # ---- Stubbing ----------------------------------------------------------

    def stub_plt_entry(self, symbol_name: str, mode: str = "ret_zero",
                       dry_run: bool = False) -> bool:
        """Overwrite the PLT entry for symbol_name with a stub.
        Returns True on success, False if symbol not found in PLT.
        """
        if mode not in STUB_MODES:
            raise AutoStubberError(
                "Unknown stub mode '{}'. Choose: {}".format(mode, list(STUB_MODES)))

        file_off = self.find_plt_entry(symbol_name)
        if file_off is None:
            return False

        stub = STUB_MODES[mode]
        if len(self._data) < file_off + len(stub):
            raise AutoStubberError(
                "PLT entry offset 0x{:X} is out of file bounds".format(file_off))

        if not dry_run:
            self._data[file_off:file_off + len(stub)] = stub
            print("[STUB] {} @ file+0x{:X} → {} ({} bytes)".format(
                symbol_name, file_off, mode, len(stub)))
        else:
            print("[DRY] Would stub {} @ file+0x{:X} → {}".format(
                symbol_name, file_off, mode))
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

    # ---- BL call finder ----------------------------------------------------

    def find_bl_calls(self, target_vaddr: int) -> list[int]:
        """Disassemble code sections and find all BL instructions that branch
        to target_vaddr. Returns list of file offsets of matching BL insns.
        """
        if not _CAPSTONE:
            raise AutoStubberError("capstone not installed")

        results = []
        for section in self._elf_obj.iter_sections():
            if section.header.sh_flags & 0x4 == 0:  # SHF_EXECINSTR
                continue
            sh_vaddr  = section.header.sh_addr
            sh_offset = section.header.sh_offset
            sh_size   = section.header.sh_size
            code = bytes(self._data[sh_offset:sh_offset + sh_size])

            for insn in self._cs.disasm(code, sh_vaddr):
                if insn.mnemonic == "bl":
                    # operand is the target address
                    try:
                        op_str = insn.op_str.strip().lstrip("#")
                        dest = int(op_str, 16)
                    except ValueError:
                        continue
                    if dest == target_vaddr:
                        file_off = sh_offset + (insn.address - sh_vaddr)
                        results.append(file_off)

        return results

    def patch_bl_to_nop(self, bl_offsets: list[int], dry_run: bool = False) -> int:
        """Replace BL instructions at the given file offsets with NOP.
        Returns number of patches applied.
        """
        count = 0
        for off in bl_offsets:
            if off + 4 > len(self._data):
                print("[WARN] BL offset 0x{:X} out of bounds, skipped".format(off))
                continue
            if not dry_run:
                self._data[off:off + 4] = NOP_INSN
                print("[PATCH] BL@0x{:X} → NOP".format(off))
            else:
                print("[DRY] Would NOP BL@0x{:X}".format(off))
            count += 1
        return count

    # ---- Save --------------------------------------------------------------

    def save(self, output_path: str):
        """Write the (potentially modified) data to output_path."""
        os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
        with open(output_path, "wb") as f:
            f.write(self._data)
        print("[STUB] Saved -> {} ({} bytes)".format(output_path, len(self._data)))

    # ---- Disassemble -------------------------------------------------------

    def disassemble_section(self, section_name: str, max_insns: int = 200):
        """Print disassembly of a named section."""
        if not _CAPSTONE:
            raise AutoStubberError("capstone not installed")
        section = self._elf_obj.get_section_by_name(section_name)
        if not section:
            print("[WARN] Section '{}' not found".format(section_name))
            return
        sh_vaddr  = section.header.sh_addr
        sh_offset = section.header.sh_offset
        sh_size   = section.header.sh_size
        code = bytes(self._data[sh_offset:sh_offset + sh_size])
        count = 0
        for insn in self._cs.disasm(code, sh_vaddr):
            print("  0x{:08X}  {:12s} {}".format(
                insn.address, insn.mnemonic, insn.op_str))
            count += 1
            if count >= max_insns:
                print("  ... (truncated at {} insns)".format(max_insns))
                break


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def cmd_stub(args):
    try:
        stubber = AutoStubber(args.elf)
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)

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
    if results["not_found"]:
        print("  Not found in PLT: {}".format(", ".join(results["not_found"])))

    if not args.dry_run:
        out = args.output or args.elf
        stubber.save(out)


def cmd_disasm(args):
    try:
        stubber = AutoStubber(args.elf)
        stubber.disassemble_section(args.section, max_insns=args.limit)
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)


def cmd_find_calls(args):
    try:
        stubber = AutoStubber(args.elf)
        addr = int(args.address, 0)
        offsets = stubber.find_bl_calls(addr)
        if not offsets:
            print("No BL calls to 0x{:X} found.".format(addr))
        else:
            print("BL calls to 0x{:X} at file offsets:".format(addr))
            for off in offsets:
                print("  0x{:X}".format(off))
    except AutoStubberError as e:
        print("[ERROR] {}".format(e), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Auto-Stubber — stub missing ARM64 PLT entries in PS5 ELFs")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_stub = sub.add_parser("stub", help="Stub PLT entries for missing symbols")
    p_stub.add_argument("--elf", required=True)
    grp = p_stub.add_mutually_exclusive_group()
    grp.add_argument("--missing", help="JSON file with missing symbols (list of {name:...})")
    grp.add_argument("--symbol", help="Single symbol name to stub")
    p_stub.add_argument("--mode", choices=list(STUB_MODES), default="ret_zero")
    p_stub.add_argument("--output", help="Output path (default: overwrite input)")
    p_stub.add_argument("--dry-run", action="store_true",
                        help="Show what would be patched without writing")

    p_disasm = sub.add_parser("disasm", help="Disassemble an ELF section")
    p_disasm.add_argument("--elf", required=True)
    p_disasm.add_argument("--section", default=".plt")
    p_disasm.add_argument("--limit", type=int, default=200)

    p_find = sub.add_parser("find-calls", help="Find BL instructions targeting an address")
    p_find.add_argument("--elf", required=True)
    p_find.add_argument("--address", required=True, help="Target virtual address (hex)")

    parsed = parser.parse_args()
    {"stub": cmd_stub, "disasm": cmd_disasm, "find-calls": cmd_find_calls}[parsed.cmd](parsed)
