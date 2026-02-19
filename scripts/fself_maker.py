"""
Fake SELF Creator — wraps a plain ELF into a PS5 fake-signed SELF container.

This is a Python port of the VB.NET SignedElfFile implementation.
The resulting file can be loaded on exploited PS5 firmware.

Usage:
  python fself_maker.py --input eboot.elf --output eboot.bin
"""

import hashlib
import math
import os
import struct
import sys


# ---------------------------------------------------------------------------
# Constants (from SelfConstants.vb)
# ---------------------------------------------------------------------------

SELF_MAGIC = bytes([0x4F, 0x15, 0x3D, 0x1D])

SELF_VERSION = 0x00
SELF_MODE = 0x01
SELF_ENDIAN = 0x01
SELF_ATTRIBS = 0x12

SELF_KEY_TYPE = 0x101

DIGEST_SIZE = 0x20
SIGNATURE_SIZE = 0x100

BLOCK_SIZE = 0x4000

FLAGS_SEGMENT_SIGNED_SHIFT = 4

PTYPE_FAKE = 0x1
PTYPE_NPDRM_EXEC = 0x4
PTYPE_NPDRM_DYNLIB = 0x5

DEFAULT_PAID = 0x3100000000000002

# ELF program header types
PT_LOAD = 0x1
PT_SCE_RELRO = 0x61000010
PT_SCE_DYNLIBDATA = 0x61000000
PT_SCE_COMMENT = 0x6FFFFF00
PT_SCE_VERSION = 0x6FFFFF01

# ELF header size and phdr size for 64-bit
ELF_EHDR_SIZE = 0x40  # 64 bytes
ELF_PHDR_SIZE = 0x38  # 56 bytes


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _align_up(value, alignment):
    return (value + alignment - 1) & ~(alignment - 1)


def _ilog2(value):
    if value <= 0:
        raise ValueError("math domain error")
    return int(math.log2(value))


def _sha256(data):
    return hashlib.sha256(data).digest()


# ---------------------------------------------------------------------------
# ELF Parser (minimal, just what we need for SELF creation)
# ---------------------------------------------------------------------------

class _ElfHeader:
    """Parsed 64-bit ELF header."""
    def __init__(self, data):
        if len(data) < ELF_EHDR_SIZE or data[:4] != b'\x7fELF':
            raise ValueError("Not a valid ELF file")
        if data[4] != 2:  # ELFCLASS64
            raise ValueError("Not a 64-bit ELF")
        if data[5] != 1:  # ELFDATA2LSB
            raise ValueError("Not little-endian ELF")

        (self.e_type, self.e_machine, self.e_version,
         self.e_entry, self.e_phoff, self.e_shoff,
         self.e_flags, self.e_ehsize, self.e_phentsize,
         self.e_phnum, self.e_shentsize, self.e_shnum,
         self.e_shstrndx) = struct.unpack_from(
            "<HHI QQQ IHHHHHH", data, 0x10)

        # Store raw ident for re-serialization
        self.e_ident = data[:16]

    def to_bytes(self):
        """Serialize header back to bytes (with shnum=0)."""
        buf = bytearray(self.e_ident)
        buf += struct.pack("<HHI QQQ IHHHHHH",
                           self.e_type, self.e_machine, self.e_version,
                           self.e_entry, self.e_phoff, self.e_shoff,
                           self.e_flags, self.e_ehsize, self.e_phentsize,
                           self.e_phnum, self.e_shentsize,
                           0,  # shnum = 0 (ignore sections)
                           self.e_shstrndx)
        return bytes(buf)


class _ElfPhdr:
    """Parsed 64-bit ELF program header."""
    SIZE = ELF_PHDR_SIZE

    def __init__(self, data, offset):
        (self.p_type, self.p_flags, self.p_offset,
         self.p_vaddr, self.p_paddr,
         self.p_filesz, self.p_memsz,
         self.p_align) = struct.unpack_from("<II QQQQQ", data, offset)

    def to_bytes(self):
        return struct.pack("<II QQQQQ",
                           self.p_type, self.p_flags, self.p_offset,
                           self.p_vaddr, self.p_paddr,
                           self.p_filesz, self.p_memsz, self.p_align)


# ---------------------------------------------------------------------------
# Fake SELF builder
# ---------------------------------------------------------------------------

# Segment types that get entries in the SELF
_SELF_SEGMENT_TYPES = {PT_LOAD, PT_SCE_RELRO, PT_SCE_DYNLIBDATA, PT_SCE_COMMENT}


def make_fself(elf_path, output_path, paid=DEFAULT_PAID, ptype=PTYPE_FAKE,
               app_version=0, fw_version=0):
    """Create a fake-signed SELF from a plain ELF file.

    Args:
        elf_path: Path to input ELF file.
        output_path: Path for output SELF file.
        paid: Program Auth ID (default: 0x3100000000000002).
        ptype: Program type (default: PTYPE_FAKE = 1).
        app_version: Application version (default: 0).
        fw_version: Firmware version (default: 0).

    Returns:
        True on success.

    Raises:
        ValueError: If input is not a valid ELF.
        IOError: On file I/O errors.
    """
    # Read entire ELF
    with open(elf_path, "rb") as f:
        elf_data = f.read()

    digest = _sha256(elf_data)

    # Parse ELF header
    ehdr = _ElfHeader(elf_data)

    # Parse program headers
    phdrs = []
    segments = []
    version_data = None

    for i in range(ehdr.e_phnum):
        off = ehdr.e_phoff + i * ehdr.e_phentsize
        ph = _ElfPhdr(elf_data, off)
        phdrs.append(ph)

        # Read segment data
        if ph.p_filesz > 0:
            seg = elf_data[ph.p_offset:ph.p_offset + ph.p_filesz]
        else:
            seg = b""
        segments.append(seg)

        # Capture version segment
        if ph.p_type == PT_SCE_VERSION:
            version_data = seg

    # Build SELF entries (meta + data pairs for each qualifying segment)
    entries = []  # list of dicts
    entry_index = 0

    for i, ph in enumerate(phdrs):
        if ph.p_type not in _SELF_SEGMENT_TYPES:
            continue

        # Meta entry
        meta_props = 0
        # Signed = true
        meta_props |= (1 << 2)
        # HasDigests = true
        meta_props |= (1 << 16)
        # SegmentIndex = entry_index + 1
        meta_props |= ((entry_index + 1) & 0xFFFF) << 20

        entries.append({
            "props": meta_props,
            "offset": 0,
            "filesz": 0,
            "memsz": 0,
            "data": None,
            "phdr_idx": i,
            "is_meta": True,
        })

        # Data entry
        data_props = 0
        # Signed = true
        data_props |= (1 << 2)
        # HasBlocks = true
        data_props |= (1 << 11)
        # BlockSize: ilog2(BLOCK_SIZE) - 12 = ilog2(0x4000) - 12 = 14 - 12 = 2
        block_val = _ilog2(BLOCK_SIZE) - 12
        data_props |= (block_val & 0xF) << 12
        # SegmentIndex = i (index into phdr table)
        data_props |= (i & 0xFFFF) << 20

        entries.append({
            "props": data_props,
            "offset": 0,
            "filesz": 0,
            "memsz": 0,
            "data": None,
            "phdr_idx": i,
            "is_meta": False,
        })

        entry_index += 2

    num_entries = len(entries)

    # Flags
    signed_block_count = 2
    flags = 0x2 | (signed_block_count << FLAGS_SEGMENT_SIGNED_SHIFT)

    # Header sizes
    COMMON_HEADER_SIZE = 8   # magic(4) + version,mode,endian,attribs(4)
    EXT_HEADER_SIZE = 20     # keytype(4) + headersize(2) + metasize(2) + filesize(8) + numentries(2) + flags(2) + pad(4) = 24...

    # From VB.NET: COMMON=8, EXT=20
    # But EXT: I(4) + 2H(4) + Q(8) + 2H(4) + 4x(4) = 24 actually
    # Let me recalculate from the VB.NET Save():
    #   magic(4) + version(1) + mode(1) + endian(1) + attribs(1) = 8  (common)
    #   keytype(4) + headersize(2) + metasize(2) + filesize(8) + numentries(2) + flags(2) + pad(4) = 24 (ext... but VB says 20)
    # VB.NET comment says "<I2HQ2H4x>" which is 4+2+2+8+2+2+4 = 24
    # But EXT_HEADER_SIZE is 20 in the VB code... Let me check the actual struct:
    # Actually looking at VB Save: writes KeyType(4), HeaderSize as UShort(2), MetaSize as UShort(2),
    # FileSize(8), NumEntries as UShort(2), Flags as UShort(2), padding(4) = 4+2+2+8+2+2+4 = 24
    # But VB code uses 20 for the size calculation... checking more carefully:
    # The VB format string says "<I2HQ2H4x>" which is struct format for 4+4+8+4+4 = 24
    # Actually "<I2HQ2H4x>" = uint32(4) + 2*uint16(4) + uint64(8) + 2*uint16(4) = 20... + 4x padding = 24
    # But VB code says EXT_HEADER_SIZE = 20 and then the padding is implicit...
    # Let me just use 20 as in VB code (the 4x padding is extra)
    # Actually: 4 + 2 + 2 + 8 + 2 + 2 = 20, then 4 bytes padding = 24 total
    # VB saves the padding separately (bw.Write(New Byte(3) {}))
    # So header_data_size = COMMON(8) + EXT(20) + PAD(4) + entries * 32
    # = 8 + 20 + 4 + entries * 32 = 32 + entries * 32
    # Wait... VB code:
    #   HeaderSize = COMMON_HEADER_SIZE + EXT_HEADER_SIZE + entries*32 + max(ehsize, phoff+phentsize*phnum)
    #   then AlignUp(16) + 64 (ExInfo) + 48 (NPDRM)
    # And the Save writes: common(8) + ext(20+4pad=24) + entries + elf headers + exinfo + npdrm

    elf_headers_size = max(ehdr.e_ehsize,
                           ehdr.e_phoff + ehdr.e_phentsize * ehdr.e_phnum)

    header_size = (COMMON_HEADER_SIZE + EXT_HEADER_SIZE +
                   num_entries * 32 + elf_headers_size)
    header_size = _align_up(header_size, 16)
    header_size += 64   # ExInfo
    header_size += 48   # NPDRM

    # Meta size
    meta_size = num_entries * 80 + 80 + SIGNATURE_SIZE  # blocks + footer + sig

    # Build entry data and compute offsets
    offset = header_size + meta_size

    for e in entries:
        ph = phdrs[e["phdr_idx"]]

        if e["is_meta"]:
            num_blocks = _align_up(ph.p_filesz, BLOCK_SIZE) // BLOCK_SIZE
            e["data"] = bytes(num_blocks * DIGEST_SIZE)
            e["offset"] = offset
            e["filesz"] = len(e["data"])
            e["memsz"] = e["filesz"]
            offset = _align_up(offset + e["filesz"], 16)
        else:
            e["data"] = segments[e["phdr_idx"]]
            e["offset"] = offset
            e["filesz"] = ph.p_filesz
            e["memsz"] = ph.p_filesz
            offset = _align_up(offset + e["filesz"], 16)

    file_size = offset

    # --- Write output ---
    out_dir = os.path.dirname(output_path)
    if out_dir and not os.path.exists(out_dir):
        os.makedirs(out_dir, exist_ok=True)

    with open(output_path, "wb") as f:
        base_offset = 0

        # ---- Common header (8 bytes) ----
        f.write(SELF_MAGIC)
        f.write(bytes([SELF_VERSION, SELF_MODE, SELF_ENDIAN, SELF_ATTRIBS]))

        # ---- Extended header (24 bytes with padding) ----
        f.write(struct.pack("<I", SELF_KEY_TYPE))
        f.write(struct.pack("<H", header_size))
        f.write(struct.pack("<H", meta_size))
        f.write(struct.pack("<Q", file_size))
        f.write(struct.pack("<H", num_entries))
        f.write(struct.pack("<H", flags))
        f.write(bytes(4))  # padding

        # ---- Entries (32 bytes each) ----
        for e in entries:
            f.write(struct.pack("<QQQQ", e["props"], e["offset"],
                                e["filesz"], e["memsz"]))

        # ---- ELF headers ----
        elf_header_start = f.tell()

        # Write ELF header (with shnum=0)
        f.write(ehdr.to_bytes())

        # Write program headers
        for ph in phdrs:
            f.write(ph.to_bytes())

        # Align to elf_headers_size
        elf_headers_aligned = _align_up(elf_headers_size, 16)
        f.seek(base_offset + 8 + 24 + num_entries * 32 + elf_headers_aligned)

        # ---- ExInfo (64 bytes) ----
        # paid(8) + ptype(8) + app_version(8) + fw_version(8) + digest(32) = 64
        f.write(struct.pack("<QQQQ", paid, ptype, app_version, fw_version))
        f.write(digest)

        # ---- NPDRM control block (48 bytes) ----
        # type(2) + padding(14) + content_id(19) + random_pad(13) = 48
        f.write(struct.pack("<H", 0x3))  # TYPE_NPDRM
        f.write(bytes(14))   # padding
        f.write(bytes(19))   # content_id (empty)
        f.write(bytes(13))   # random_pad

        # ---- Meta blocks (80 bytes each) ----
        for _ in entries:
            f.write(bytes(80))

        # ---- Meta footer (80 bytes) ----
        f.write(bytes(48))         # padding
        f.write(struct.pack("<I", 0x10000))   # Unknown1
        f.write(bytes(28))         # padding

        # ---- Signature (256 bytes, zeroed for fake) ----
        f.write(bytes(SIGNATURE_SIZE))

        # ---- Segment data ----
        for e in entries:
            f.seek(base_offset + e["offset"])
            f.write(e["data"])

        # ---- Final pad ----
        if f.tell() < file_size:
            f.seek(file_size - 1)
            f.write(b'\x00')

        # ---- Append version data (after file_size, like VB.NET) ----
        if version_data is not None and len(version_data) > 0:
            f.seek(0, 2)  # end of file
            f.write(version_data)

    return True


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Create a fake-signed SELF from a plain ELF")
    parser.add_argument("--input", required=True, help="Input ELF file")
    parser.add_argument("--output", required=True, help="Output SELF file")
    parser.add_argument("--paid", type=lambda x: int(x, 0),
                        default=DEFAULT_PAID, help="Program Auth ID")
    parser.add_argument("--ptype", type=lambda x: int(x, 0),
                        default=PTYPE_FAKE, help="Program type")
    args = parser.parse_args()

    try:
        make_fself(args.input, args.output, paid=args.paid, ptype=args.ptype)
        print("[FSELF] OK: {} -> {}".format(args.input, args.output))
    except Exception as ex:
        print("[FSELF] ERROR: {}".format(ex), file=sys.stderr)
        sys.exit(1)
