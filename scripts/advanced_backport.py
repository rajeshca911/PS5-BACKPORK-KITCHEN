"""
Advanced Backport Orchestrator — Unified pipeline for PS5 PRX backporting.

Steps:
  1. ELF Analysis    — detect SDK versions, missing symbols, missing libs
  2. BPS Patching    — apply available binary patches from patch_database.json
  3. Auto-Stubbing   — stub remaining missing symbols with ARM64 ret-zero
  4. SDK Version Fix — patch PS5/PS4 SDK bytes to match target firmware
  5. Re-signing      — (placeholder) call external sign tool if requested
  6. Final Report    — JSON + color summary

Usage:
  python advanced_backport.py \\
      --game-folder /path/to/game \\
      --fw-current 10.01 \\
      --fw-target  7.61  \\
      [--apply-bps] [--stub-missing] [--resign] \\
      [--exports-dir data/exports] \\
      [--db data/patch_database.json] \\
      [--output-folder /path/out] \\
      [--selfutil /path/to/selfutil.exe]
"""

import argparse
import json
import os
import shutil
import struct
import subprocess
import sys
import time
from pathlib import Path


# ---------------------------------------------------------------------------
# Auto-install missing dependencies
# ---------------------------------------------------------------------------

def _ensure_deps():
    """Install pyelftools and capstone if not already available."""
    missing = []
    try:
        import elftools  # noqa: F401
    except ImportError:
        missing.append("pyelftools")
    try:
        import capstone  # noqa: F401
    except ImportError:
        missing.append("capstone")
    if missing:
        print("[ABP] Installing missing dependencies: {}".format(", ".join(missing)))
        subprocess.check_call(
            [sys.executable, "-m", "pip", "install", "--quiet"] + missing,
            stdout=subprocess.DEVNULL,
        )
        print("[ABP] Dependencies installed.")


_ensure_deps()


# ---------------------------------------------------------------------------
# Lazy imports (allow running --help without all deps installed)
# ---------------------------------------------------------------------------

def _import_bps():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    sys.path.insert(0, script_dir)
    from bps_engine import PatchDatabase, BPSError
    return PatchDatabase, BPSError


def _import_elf():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    sys.path.insert(0, script_dir)
    from elf_analyzer import ELFAnalyzer, ELFAnalyzerError
    return ELFAnalyzer, ELFAnalyzerError


def _import_stubber():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    sys.path.insert(0, script_dir)
    from auto_stubber import AutoStubber, AutoStubberError
    return AutoStubber, AutoStubberError


# ---------------------------------------------------------------------------
# ANSI helpers
# ---------------------------------------------------------------------------

RESET  = "\033[0m"
BOLD   = "\033[1m"
GREEN  = "\033[92m"
YELLOW = "\033[93m"
RED    = "\033[91m"
CYAN   = "\033[96m"
DIM    = "\033[2m"

_COLOR = sys.stdout.isatty()


def _c(text, color):
    return "{}{}{}".format(color, text, RESET) if _COLOR else text


def _log(msg, color=None):
    if color:
        print(_c(msg, color))
    else:
        print(msg)


def _header(title):
    bar = "─" * 60
    _log("\n{} {} {}".format(bar[:4], title, bar), BOLD)


# ---------------------------------------------------------------------------
# SDK Version Patching (pure Python, no external tool needed)
# ---------------------------------------------------------------------------

# PS5 SDK version bytes to patch inside ELF PT_SCE_PROCPARAM
# This is a simple byte-level replacement using known version constants.
# VersionProfiles from VB.NET project define pairs:
#   (ps5_sdk_bytes_current, ps5_sdk_bytes_target)

# Target firmware SDK constants — used to write into PT_SCE_PROCPARAM
# Format: (ps5_sdk_uint32, ps4_sdk_uint32) stored little-endian in file
FW_SDK_MAP: dict[str, tuple[int, int]] = {
    "1.00":  (0x01000001, 0x05508001),
    "1.05":  (0x01050001, 0x05508001),
    "2.00":  (0x02000001, 0x06508001),
    "2.20":  (0x02200001, 0x06508001),
    "2.50":  (0x02500001, 0x06508001),
    "3.00":  (0x03000001, 0x07508001),
    "3.20":  (0x03200001, 0x07508001),
    "4.00":  (0x04000001, 0x08508001),
    "4.50":  (0x04500001, 0x08508001),
    "5.00":  (0x05000001, 0x08508001),
    "5.02":  (0x05020001, 0x08508001),
    "5.10":  (0x05100001, 0x08508001),
    "5.25":  (0x05250001, 0x08508001),
    "6.00":  (0x06000001, 0x09508001),
    "6.02":  (0x06020001, 0x09508001),
    "6.50":  (0x06500001, 0x09508001),
    "7.00":  (0x07000001, 0x09508001),
    "7.01":  (0x07010001, 0x09508001),
    "7.55":  (0x07550001, 0x09508001),
    "7.61":  (0x07610001, 0x09508001),
    "8.00":  (0x08000001, 0x09508001),
    "8.52":  (0x08520001, 0x09508001),
    "9.00":  (0x09000001, 0x09508001),
    "9.60":  (0x09600001, 0x09508001),
    "10.00": (0x0A000040, 0x12090001),
    "10.01": (0x0A010040, 0x12090001),
    "10.50": (0x0A500040, 0x12090001),
    "11.00": (0x0B000040, 0x12090001),
}

# SCE parameter segment types
_PT_SCE_PROCPARAM   = 0x61000001   # eboot.bin (process param)
_PT_SCE_MODULE_PARAM = 0x61000002  # .prx/.sprx (module param)

# Magic validation values
_SCE_PROCESS_PARAM_MAGIC = 0x4942524F  # "IBRO"
_SCE_MODULE_PARAM_MAGIC  = 0x3C13F4BF

# Byte offsets within the SCE parameter structure
_SCE_PARAM_MAGIC_OFF   = 0x08   # magic uint32
_SCE_PARAM_PS4_SDK_OFF = 0x10   # PS4 SDK uint32
_SCE_PARAM_PS5_SDK_OFF = 0x14   # PS5 SDK uint32


def _find_sce_param_offsets(data: bytes) -> list[tuple[int, int, str]]:
    """Find PT_SCE_PROCPARAM and PT_SCE_MODULE_PARAM segments in an ELF.
    Returns list of (ps4_sdk_file_offset, ps5_sdk_file_offset, segment_type).
    """
    if len(data) < 0x40 or data[:4] != b'\x7fELF':
        return []

    ei_class = data[4]
    if ei_class != 2:  # only 64-bit
        return []

    e_phoff = struct.unpack_from("<Q", data, 0x20)[0]
    e_phentsize = struct.unpack_from("<H", data, 0x36)[0]
    e_phnum = struct.unpack_from("<H", data, 0x38)[0]

    patchable_types = {_PT_SCE_PROCPARAM, _PT_SCE_MODULE_PARAM}

    results = []
    for i in range(e_phnum):
        off = e_phoff + i * e_phentsize
        if off + e_phentsize > len(data):
            break
        p_type = struct.unpack_from("<I", data, off)[0]
        if p_type not in patchable_types:
            continue
        p_offset = struct.unpack_from("<Q", data, off + 0x08)[0]
        p_filesz = struct.unpack_from("<Q", data, off + 0x20)[0]
        if p_filesz < 0x18 or p_offset + p_filesz > len(data):
            continue
        # Check that PS5 SDK offset is within bounds
        if p_offset + _SCE_PARAM_PS5_SDK_OFF + 4 > len(data):
            continue
        ps4_off = p_offset + _SCE_PARAM_PS4_SDK_OFF
        ps5_off = p_offset + _SCE_PARAM_PS5_SDK_OFF
        seg_name = "PROCPARAM" if p_type == _PT_SCE_PROCPARAM else "MODULE_PARAM"
        results.append((ps4_off, ps5_off, seg_name))
    return results


def _patch_sdk_version_in_file(file_path: str, fw_to: str) -> tuple[bool, str]:
    """Patch PS5/PS4 SDK version bytes directly in PT_SCE_PROCPARAM.
    Instead of searching the whole file for byte patterns, this reads
    the actual SDK values from the known PROCPARAM offsets and overwrites
    them with the target firmware values.
    Returns (patched: bool, detail: str).
    """
    if fw_to not in FW_SDK_MAP:
        return False, "unknown target FW '{}'".format(fw_to)

    ps5_target, ps4_target = FW_SDK_MAP[fw_to]

    with open(file_path, "rb") as f:
        data = bytearray(f.read())

    segments = _find_sce_param_offsets(bytes(data))
    if not segments:
        return False, "no SCE param segment found"

    patched = False
    details = []
    for ps4_off, ps5_off, seg_name in segments:
        ps4_cur = struct.unpack_from("<I", data, ps4_off)[0]
        ps5_cur = struct.unpack_from("<I", data, ps5_off)[0]

        changed = False
        if ps5_cur != ps5_target and ps5_cur != 0:
            old_str = "{}.{}.{}.{}".format(
                (ps5_cur >> 24) & 0xFF, (ps5_cur >> 16) & 0xFF,
                (ps5_cur >> 8) & 0xFF, ps5_cur & 0xFF)
            new_str = "{}.{}.{}.{}".format(
                (ps5_target >> 24) & 0xFF, (ps5_target >> 16) & 0xFF,
                (ps5_target >> 8) & 0xFF, ps5_target & 0xFF)
            struct.pack_into("<I", data, ps5_off, ps5_target)
            details.append("{}: PS5 {} -> {}".format(seg_name, old_str, new_str))
            changed = True

        if ps4_cur != ps4_target and ps4_cur != 0:
            struct.pack_into("<I", data, ps4_off, ps4_target)
            changed = True

        if changed:
            patched = True

    if patched:
        with open(file_path, "wb") as f:
            f.write(data)

    return patched, "; ".join(details) if details else "already at target version"


# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

TARGET_EXTENSIONS = (".sprx", ".prx", ".bin")


class BackportPipeline:
    def __init__(self, args):
        self.args = args
        self.results = {
            "game_folder":   args.game_folder,
            "fw_current":    args.fw_current,
            "fw_target":     args.fw_target,
            "files_found":   [],
            "step_analysis": {},
            "step_bps":      {"applied": [], "skipped": [], "errors": []},
            "step_stub":     {"stubbed": [], "not_found": [], "errors": []},
            "step_sdk_patch": {"patched": [], "skipped": []},
            "step_param":    {"patched": []},
            "step_resign":   {"resigned": [], "errors": []},
            "errors":        [],
            "total_time_s":  0.0,
        }
        self._work_folder = args.output_folder or args.game_folder

    # ---- Step 0: Collect files ----------------------------------------

    def collect_files(self) -> list[str]:
        files = []
        for root, _dirs, fnames in os.walk(self.args.game_folder):
            for fname in fnames:
                if fname.lower().endswith(TARGET_EXTENSIONS):
                    files.append(os.path.join(root, fname))
        self.results["files_found"] = [os.path.basename(f) for f in files]
        _log("[ABP] Found {} target files".format(len(files)), CYAN)
        return files

    # ---- Step 0.5: SELF Decryption ------------------------------------

    def step_decrypt_self(self, files: list[str]) -> dict[str, str]:
        """Decrypt SELF files using selfutil so analysis / stubbing can work.
        Returns a mapping {original_path: decrypted_elf_path}.
        Decrypted files are placed in a temp subfolder.
        """
        selfutil = self._find_selfutil(self.args.selfutil)
        if not selfutil:
            return {}

        from elf_analyzer import is_self_file
        _header("Step 0: SELF Decryption (for analysis)")

        temp_dir = os.path.join(self._work_folder, "_abp_temp_elf")
        os.makedirs(temp_dir, exist_ok=True)
        mapping = {}

        for fpath in files:
            with open(fpath, "rb") as f:
                magic = f.read(4)
            if not is_self_file(magic):
                continue  # already plain ELF

            fname = os.path.basename(fpath)
            elf_out = os.path.join(temp_dir, fname + ".elf")
            try:
                proc = subprocess.run(
                    [selfutil, "--verbose", "--overwrite",
                     "--input", fpath, "--output", elf_out],
                    capture_output=True, text=True, timeout=120)
                if proc.returncode == 0 and os.path.exists(elf_out) and \
                        os.path.getsize(elf_out) > 0:
                    mapping[fpath] = elf_out
                    _log("  [DECRYPT] {} — OK".format(fname), GREEN)
                else:
                    err = proc.stderr[:200].strip() if proc.stderr else "unknown"
                    _log("  [DECRYPT] {} — FAILED: {}".format(fname, err), YELLOW)
            except Exception as ex:
                _log("  [DECRYPT] {} — error: {}".format(fname, ex), YELLOW)

        if len(mapping) == 0:
            _log("  All {} files are already plain ELF — no decryption needed".format(
                len(files)), GREEN)
        else:
            _log("  Decrypted {}/{} SELF files".format(len(mapping), len(files)), CYAN)
        return mapping

    # ---- Step 1: ELF Analysis ------------------------------------------

    def step_analysis(self, files: list[str],
                      decrypt_map: dict[str, str] | None = None):
        _header("Step 1: ELF / PRX Analysis")
        try:
            ELFAnalyzer, ELFAnalyzerError = _import_elf()
        except ImportError:
            _log("[WARN] pyelftools not installed — skipping analysis", YELLOW)
            return

        if decrypt_map is None:
            decrypt_map = {}

        total_imports = 0
        total_libs = 0
        total_plt = 0

        for fpath in files:
            fname = os.path.basename(fpath)
            # Use decrypted ELF for analysis if available
            analyze_path = decrypt_map.get(fpath, fpath)
            try:
                analyzer = ELFAnalyzer(analyze_path)
                report = analyzer.generate_report(
                    self.args.fw_target, self.args.exports_dir)
                self.results["step_analysis"][fname] = report

                elf_type = report.get("elf_type", "")
                is_sce = report.get("is_sce", False)
                size_kb = report.get("file_size", 0) // 1024
                code_kb = report.get("code_size", 0) // 1024
                libs = report.get("required_libs", [])
                n_imp = report.get("total_imported", 0)
                n_exp = report.get("total_exported", 0)
                n_plt = report.get("plt_entries", 0)
                missing = report.get("missing_symbols", [])
                note = report.get("note", "")

                total_imports += n_imp
                total_libs += len(libs)
                total_plt += n_plt

                if note:
                    _log("  {} [{}] {}KB — {}".format(fname, elf_type, size_kb, note), CYAN)
                else:
                    tag = "SCE" if is_sce else "ELF"
                    _log("  {} [{}] {}KB code:{}KB libs:{} imports:{} PLT:{} exports:{}".format(
                        fname, tag, size_kb, code_kb, len(libs), n_imp, n_plt, n_exp),
                        GREEN if n_imp > 0 else CYAN)
            except Exception as ex:
                _log("  [WARN] Could not analyze {}: {}".format(fname, ex), YELLOW)

        _log("  --- Total: {} imports, {} libs, {} PLT entries across {} files".format(
            total_imports, total_libs, total_plt, len(files)), CYAN)

    # ---- Step 2: BPS Patching ------------------------------------------

    def step_bps(self, files: list[str]):
        if not self.args.apply_bps:
            return
        _header("Step 2: BPS Patch Application")
        try:
            PatchDatabase, BPSError = _import_bps()
        except ImportError:
            _log("[WARN] bps_engine not available — skipping BPS step", YELLOW)
            return

        db = PatchDatabase(self.args.db)
        for fpath in files:
            fname = os.path.basename(fpath)
            patch_path = db.find_patch(self.args.fw_current, self.args.fw_target, fname)
            if not patch_path:
                self.results["step_bps"]["skipped"].append(fname)
                continue
            if not os.path.exists(patch_path):
                _log("  [WARN] Patch file missing: {}".format(patch_path), YELLOW)
                self.results["step_bps"]["errors"].append(
                    {"file": fname, "error": "patch file not found"})
                continue
            try:
                from bps_engine import BPSEngine
                out = fpath + ".patched"
                BPSEngine.apply_patch(fpath, patch_path, out)
                os.replace(out, fpath)
                self.results["step_bps"]["applied"].append(fname)
            except Exception as ex:
                _log("  [ERR] {} — {}".format(fname, ex), RED)
                self.results["step_bps"]["errors"].append(
                    {"file": fname, "error": str(ex)})

    # ---- Step 3: Auto-Stubbing -----------------------------------------

    def step_stub(self, files: list[str],
                  decrypt_map: dict[str, str] | None = None):
        if not self.args.stub_missing:
            return
        _header("Step 3: PLT Stub Patching (x86_64 / ARM64)")
        try:
            AutoStubber, AutoStubberError = _import_stubber()
        except ImportError:
            _log("[WARN] capstone not installed — skipping stub step", YELLOW)
            return

        if decrypt_map is None:
            decrypt_map = {}

        total_analyzed = 0
        total_stubbed = 0
        total_notfound = 0

        for fpath in files:
            fname = os.path.basename(fpath)
            analysis = self.results["step_analysis"].get(fname, {})
            missing = analysis.get("missing_symbols", [])
            n_imp = analysis.get("total_imported", 0)
            n_plt = analysis.get("plt_entries", 0)

            # Use decrypted ELF for stubbing if available
            stub_path = decrypt_map.get(fpath, fpath)

            try:
                stubber = AutoStubber(stub_path)
                info = stubber.get_info()
                arch = info["arch"]

                if missing:
                    res = stubber.stub_missing(missing, mode="ret_zero")
                    n_stub = len(res["stubbed"])
                    n_nf = len(res["not_found"])
                    if n_stub > 0:
                        stubber.save(stub_path)
                        if stub_path != fpath:
                            self._merge_stub_back(fpath, stub_path)
                        self.results["step_stub"]["stubbed"].extend(res["stubbed"])
                    self.results["step_stub"]["not_found"].extend(res["not_found"])
                    total_stubbed += n_stub
                    total_notfound += n_nf
                    _log("  {} [{}] PLT:{} — stubbed:{} not_found:{}".format(
                        fname, arch, n_plt, n_stub, n_nf),
                        GREEN if n_stub else YELLOW)
                else:
                    _log("  {} [{}] PLT:{} imports:{} — no missing symbols".format(
                        fname, arch, n_plt, n_imp), DIM)

                total_analyzed += 1
            except Exception as ex:
                _log("  [ERR] {} — {}".format(fname, ex), RED)
                self.results["step_stub"]["errors"].append(
                    {"file": fname, "error": str(ex)})

        _log("  --- Analyzed:{} stubbed:{} not_found:{}".format(
            total_analyzed, total_stubbed, total_notfound), CYAN)

    @staticmethod
    def _merge_stub_back(original_self: str, patched_elf: str):
        """After stubbing a decrypted ELF, replace the original file.
        selfutil --resign will re-sign later, so we replace the original
        with the patched decrypted ELF (it needs re-signing anyway).
        """
        shutil.copy2(patched_elf, original_self)

    # ---- Step 4: SDK Version Patch -------------------------------------

    def step_sdk_patch(self, files: list[str]):
        _header("Step 4: SDK Version Patch ({} -> {})".format(
            self.args.fw_current, self.args.fw_target))
        n_patched = 0
        n_skipped = 0
        n_already = 0
        for fpath in files:
            fname = os.path.basename(fpath)
            try:
                patched, detail = _patch_sdk_version_in_file(
                    fpath, self.args.fw_target)
                if patched:
                    self.results["step_sdk_patch"]["patched"].append(fname)
                    _log("  [SDK] {} — {}".format(fname, detail), GREEN)
                    n_patched += 1
                else:
                    self.results["step_sdk_patch"]["skipped"].append(fname)
                    if "already" in detail:
                        n_already += 1
                    else:
                        n_skipped += 1
                    _log("  [SDK] {} — {}".format(fname, detail), DIM)
            except Exception as ex:
                _log("  [WARN] SDK patch failed for {}: {}".format(fname, ex), YELLOW)
        _log("  --- SDK patched: {}, already at target: {}, no param: {}".format(
            n_patched, n_already, n_skipped), CYAN)

    # ---- Step 4b: Patch param.json / param.sfo --------------------------

    def step_patch_param(self):
        """Patch requiredSystemSoftwareVersion and sdkVersion in param.json
        (PS5 format) so the console accepts the backported game.
        Also patches param.sfo SYSTEM_VER if present.
        """
        _header("Step 4b: Param Metadata Patch")

        fw_to = self.args.fw_target
        if fw_to not in FW_SDK_MAP:
            _log("  [PARAM] Unknown target FW '{}' — skipping".format(fw_to), YELLOW)
            return

        # Build the 64-bit hex string used in param.json
        # Format: "0x0MMNN000000000000" where MM=major, NN=minor
        parts = fw_to.split(".")
        major = int(parts[0])
        minor = int(parts[1]) if len(parts) > 1 else 0
        param_hex = "0x{:02X}{:02X}000000000000".format(major, minor)

        patched_any = False

        # Search for param.json
        for root, _dirs, fnames in os.walk(self.args.game_folder):
            for fname in fnames:
                fpath = os.path.join(root, fname)

                if fname == "param.json":
                    try:
                        with open(fpath, "r", encoding="utf-8") as f:
                            content = f.read()
                        original = content

                        # Replace requiredSystemSoftwareVersion
                        import re
                        content = re.sub(
                            r'("requiredSystemSoftwareVersion"\s*:\s*)"0x[0-9A-Fa-f]+"',
                            r'\1"{}"'.format(param_hex),
                            content)
                        # Replace sdkVersion
                        content = re.sub(
                            r'("sdkVersion"\s*:\s*)"0x[0-9A-Fa-f]+"',
                            r'\1"{}"'.format(param_hex),
                            content)

                        if content != original:
                            with open(fpath, "w", encoding="utf-8") as f:
                                f.write(content)
                            _log("  [PARAM] {} — patched to {}".format(fname, param_hex), GREEN)
                            self.results["step_param"]["patched"].append(fname)
                            patched_any = True
                        else:
                            _log("  [PARAM] {} — already at target".format(fname), DIM)
                    except Exception as ex:
                        _log("  [PARAM] {} — error: {}".format(fname, ex), RED)

                elif fname == "param.sfo":
                    try:
                        with open(fpath, "rb") as f:
                            sfo_data = bytearray(f.read())

                        # SFO SYSTEM_VER is a uint32 stored as: 0xMMNN0000
                        target_sysver = (major << 24) | (minor << 16)
                        target_bytes = struct.pack("<I", target_sysver)

                        # Search for SYSTEM_VER key in SFO
                        key_pos = sfo_data.find(b"SYSTEM_VER\x00")
                        if key_pos >= 0:
                            # SFO format: keys table points to values table
                            # Simple approach: find the 4-byte value near the key
                            # SFO values are in a separate section, but we can
                            # search for any uint32 that looks like a system version
                            # (0x0?000000 pattern) near the end of the file
                            changed = False
                            # Look for version pattern in data section (last half of file)
                            half = len(sfo_data) // 2
                            for i in range(half, len(sfo_data) - 3):
                                val = struct.unpack_from("<I", sfo_data, i)[0]
                                if val != 0 and (val & 0x0000FFFF) == 0 and \
                                        ((val >> 24) & 0xFF) <= 0x10:
                                    # Looks like a system version (major.minor.0.0)
                                    if val != target_sysver:
                                        struct.pack_into("<I", sfo_data, i, target_sysver)
                                        changed = True
                                        break
                            if changed:
                                with open(fpath, "wb") as f:
                                    f.write(sfo_data)
                                _log("  [PARAM] {} — SYSTEM_VER patched".format(fname), GREEN)
                                patched_any = True
                            else:
                                _log("  [PARAM] {} — already at target or not found".format(
                                    fname), DIM)
                        else:
                            _log("  [PARAM] {} — no SYSTEM_VER key found".format(fname), DIM)
                    except Exception as ex:
                        _log("  [PARAM] {} — error: {}".format(fname, ex), RED)

        if not patched_any:
            _log("  No param.json or param.sfo found to patch", YELLOW)

    # ---- Step 5: Re-signing (placeholder) ------------------------------

    @staticmethod
    def _find_selfutil(explicit_path: str | None) -> str | None:
        """Locate selfutil_patched.exe: explicit flag → SelfUtil/ next to app → PATH."""
        if explicit_path and os.path.exists(explicit_path):
            return explicit_path
        # Walk up from scripts/ to find the app's SelfUtil directory
        base = os.path.dirname(os.path.abspath(__file__))
        for _ in range(5):
            candidate = os.path.join(base, "SelfUtil", "selfutil_patched.exe")
            if os.path.exists(candidate):
                return candidate
            # Also check bin/Debug output
            candidate2 = os.path.join(base, "PS5 BACKPORK KITCHEN", "bin",
                                       "Debug", "net8.0-windows", "SelfUtil",
                                       "selfutil_patched.exe")
            if os.path.exists(candidate2):
                return candidate2
            parent = os.path.dirname(base)
            if parent == base:
                break
            base = parent
        return None

    def step_resign(self, files: list[str],
                    decrypt_map: dict[str, str] | None = None):
        if not self.args.resign:
            return
        _header("Step 5: Re-signing")

        from elf_analyzer import is_self_file

        selfutil = self._find_selfutil(self.args.selfutil)
        if not selfutil:
            _log("[WARN] selfutil_patched.exe not found — skipping re-signing", YELLOW)
            _log("       Download from: https://github.com/CyB1K/SelfUtil-Patched", DIM)
            _log("       Place in SelfUtil/ folder next to the application.", DIM)
            return

        if decrypt_map is None:
            decrypt_map = {}

        _log("[SIGN] Using selfutil: {}".format(selfutil), DIM)
        n_signed = 0
        n_plain = 0
        for fpath in files:
            fname = os.path.basename(fpath)

            # Only re-sign files that are (or were) SELF containers.
            # Plain ELF files (already decrypted / DUPLEX dumps) don't need
            # selfutil and will crash it with "vector subscript out of range".
            with open(fpath, "rb") as f:
                magic = f.read(4)
            was_self = fpath in decrypt_map  # was originally SELF, decrypted by us
            is_self_now = is_self_file(magic)

            if not is_self_now and not was_self:
                n_plain += 1
                _log("  [SIGN] {} — skipped (plain ELF, no SELF signing needed)".format(
                    fname), DIM)
                continue

            try:
                # selfutil uses --input/--output flags (not --resign)
                proc = subprocess.run(
                    [selfutil, "--verbose", "--overwrite",
                     "--input", fpath, "--output", fpath],
                    capture_output=True, text=True, timeout=120)
                if proc.returncode == 0:
                    self.results["step_resign"]["resigned"].append(fname)
                    _log("  [SIGN] {} — OK".format(fname), GREEN)
                    n_signed += 1
                else:
                    err = (proc.stderr or proc.stdout or "unknown")[:200].strip()
                    _log("  [SIGN] {} — FAILED: {}".format(fname, err), RED)
                    self.results["step_resign"]["errors"].append(
                        {"file": fname, "error": err})
            except subprocess.TimeoutExpired:
                _log("  [SIGN] {} — timeout (selfutil took too long)".format(fname), RED)
                self.results["step_resign"]["errors"].append(
                    {"file": fname, "error": "timeout"})
            except Exception as ex:
                _log("  [SIGN] {} — error: {}".format(fname, ex), RED)
                self.results["step_resign"]["errors"].append(
                    {"file": fname, "error": str(ex)})

        _log("  --- Signed: {}, plain ELF (skipped): {}".format(n_signed, n_plain), CYAN)

    # ---- Run -----------------------------------------------------------

    def run(self):
        t0 = time.time()
        _log(_c("\n[ABP] Advanced Backport Pipeline — {} → {}".format(
            self.args.fw_current, self.args.fw_target), BOLD + CYAN))

        files = self.collect_files()
        if not files:
            _log("[WARN] No target files found in {}".format(self.args.game_folder), YELLOW)

        # Copy to output folder if different
        if self.args.output_folder and self.args.output_folder != self.args.game_folder:
            _log("[ABP] Copying files to output folder ...", DIM)
            for fpath in files:
                rel = os.path.relpath(fpath, self.args.game_folder)
                dst = os.path.join(self.args.output_folder, rel)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copy2(fpath, dst)
            # Redirect files list to output folder
            files = [
                os.path.join(self.args.output_folder,
                             os.path.relpath(f, self.args.game_folder))
                for f in files
            ]

        # Decrypt SELF files for analysis (if selfutil available)
        decrypt_map = self.step_decrypt_self(files)

        self.step_analysis(files, decrypt_map)
        self.step_bps(files)
        self.step_stub(files, decrypt_map)
        self.step_sdk_patch(files)
        self.step_patch_param()
        self.step_resign(files, decrypt_map)

        # Cleanup temp decrypted files
        temp_dir = os.path.join(self._work_folder, "_abp_temp_elf")
        if os.path.exists(temp_dir):
            shutil.rmtree(temp_dir, ignore_errors=True)

        # Package modified files into a ZIP for easy deployment
        zip_path = self.create_output_zip(files)
        if zip_path:
            self.results["zip_path"] = zip_path

        self.results["total_time_s"] = round(time.time() - t0, 2)
        return self.results

    def create_output_zip(self, files: list[str]):
        """Package all modified files into a ZIP for easy deployment."""
        import zipfile
        import datetime

        modified = (
            self.results["step_bps"]["applied"]
            + self.results["step_sdk_patch"]["patched"]
            + [s["name"] if isinstance(s, dict) else s
               for s in self.results["step_stub"]["stubbed"]]
            + self.results["step_resign"]["resigned"]
        )
        param_patched = self.results["step_param"]["patched"]

        if not modified and not param_patched:
            _log("[ZIP] No files were modified — skipping ZIP creation.", YELLOW)
            return None

        game_name = os.path.basename(self.args.game_folder.rstrip("/\\"))
        ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        zip_name = "backport_{}_{}_to_{}_{}.zip".format(
            game_name, self.args.fw_current, self.args.fw_target, ts)
        zip_path = os.path.join(
            self.args.output_folder or self.args.game_folder, zip_name)

        modified_set = set(modified)
        n_zipped = 0
        with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
            for fpath in files:
                fname = os.path.basename(fpath)
                if fname in modified_set:
                    arcname = os.path.relpath(fpath, self.args.game_folder)
                    zf.write(fpath, arcname)
                    n_zipped += 1
            # Also include patched param.json/param.sfo
            if param_patched:
                for root, _dirs, fnames in os.walk(self.args.game_folder):
                    for fname in fnames:
                        if fname in param_patched:
                            fpath = os.path.join(root, fname)
                            arcname = os.path.relpath(fpath, self.args.game_folder)
                            zf.write(fpath, arcname)
                            n_zipped += 1

        _log("[ZIP] Created: {} ({} files)".format(zip_path, n_zipped), GREEN)
        return zip_path

    def print_summary(self):
        r = self.results
        _header("Final Summary")
        _log("  Files scanned:    {}".format(len(r["files_found"])))

        # Analysis totals
        total_libs = 0
        total_imports = 0
        total_plt = 0
        for fname, report in r["step_analysis"].items():
            total_libs += len(report.get("required_libs", []))
            total_imports += report.get("total_imported", 0)
            total_plt += report.get("plt_entries", 0)
        if total_imports:
            _log("  Libraries found:  {}".format(total_libs))
            _log("  Imports resolved: {}".format(total_imports))
            _log("  PLT entries:      {}".format(total_plt))

        _log("  BPS applied:      {}".format(len(r["step_bps"]["applied"])))
        _log("  PLT stubs:        {}".format(len(r["step_stub"]["stubbed"])))
        _log("  SDK patched:      {}".format(len(r["step_sdk_patch"]["patched"])))
        _log("  Param patched:    {}".format(len(r["step_param"]["patched"])))
        _log("  Re-signed:        {}".format(len(r["step_resign"]["resigned"])))
        _log("  Total time:       {}s".format(r["total_time_s"]))
        if r.get("zip_path"):
            _log("  Output ZIP:       {}".format(r["zip_path"]), GREEN)
        if r["errors"]:
            _log("  Errors:", RED)
            for e in r["errors"]:
                _log("    {}".format(e), RED)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Advanced Backport Orchestrator for PS5 PRX files")
    parser.add_argument("--game-folder", required=True,
                        help="Path to game folder containing .sprx/.prx/.bin files")
    parser.add_argument("--fw-current", required=True,
                        help="Current firmware version (e.g. 10.01)")
    parser.add_argument("--fw-target", required=True,
                        help="Target firmware version (e.g. 7.61)")
    parser.add_argument("--apply-bps", action="store_true",
                        help="Apply BPS patches from database")
    parser.add_argument("--stub-missing", action="store_true",
                        help="Stub missing ARM64 PLT entries")
    parser.add_argument("--resign", action="store_true",
                        help="Re-sign ELFs with selfutil after patching")
    parser.add_argument("--exports-dir", default="data/exports",
                        help="Firmware exports directory (default: data/exports)")
    parser.add_argument("--db", default="data/patch_database.json",
                        help="Patch database JSON (default: data/patch_database.json)")
    parser.add_argument("--selfutil", default=None,
                        help="Path to selfutil executable for re-signing")
    parser.add_argument("--output-folder", default=None,
                        help="Output folder (default: modify game folder in-place)")
    parser.add_argument("--output-report", default=None,
                        help="Save final report as JSON")
    parser.add_argument("--no-color", action="store_true",
                        help="Disable ANSI color output")
    args = parser.parse_args()

    if args.no_color:
        _COLOR = False

    pipeline = BackportPipeline(args)
    results = pipeline.run()
    pipeline.print_summary()

    if args.output_report:
        with open(args.output_report, "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2)
        _log("\n[ABP] Report saved to {}".format(args.output_report), DIM)
