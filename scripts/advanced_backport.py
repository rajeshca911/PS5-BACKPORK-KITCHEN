"""
Advanced Backport Orchestrator -- Unified pipeline for PS5 PRX backporting.

Steps:
  1. ELF Analysis    -- detect SDK versions, missing symbols, missing libs
  2. BPS Patching    -- apply available binary patches from patch_database.json
  3. Auto-Stubbing   -- stub remaining missing symbols with ARM64 ret-zero
  4. SDK Version Fix -- patch PS5/PS4 SDK bytes to match target firmware
  5. Re-signing      -- (placeholder) call external sign tool if requested
  6. Final Report    -- JSON + color summary

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
import sys
import time
from pathlib import Path


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
    # Ensure output works on Windows cp1252 consoles
    try:
        if color:
            print(_c(msg, color))
        else:
            print(msg)
    except UnicodeEncodeError:
        safe = msg.encode("ascii", errors="replace").decode("ascii")
        print(safe)


def _header(title):
    bar = "-" * 60
    _log("\n{} {} {}".format(bar[:4], title, bar), BOLD)


# ---------------------------------------------------------------------------
# SDK Version Patching (pure Python, no external tool needed)
# ---------------------------------------------------------------------------

# PS5 SDK version bytes to patch inside ELF PT_SCE_PROCPARAM
# This is a simple byte-level replacement using known version constants.
# VersionProfiles from VB.NET project define pairs:
#   (ps5_sdk_bytes_current, ps5_sdk_bytes_target)

def _fw_to_major(fw_str: str) -> int:
    """Convert firmware version string (e.g. '9.60') to major version int (9)."""
    try:
        return int(fw_str.split(".")[0])
    except (ValueError, IndexError):
        return -1


# SDK byte pairs per major firmware version -- matches VersionProfiles.vb exactly.
# Key = major firmware version (int).
# Value = (PS5_SDK_uint32, PS4_SDK_uint32) stored as little-endian in the binary.
_SDK_PAIRS: dict[int, tuple[int, int]] = {
    1:  (0x01000050, 0x07590001),
    2:  (0x02000009, 0x08050001),
    3:  (0x03000027, 0x08540001),
    4:  (0x04000031, 0x09040001),
    5:  (0x05000033, 0x09590001),
    6:  (0x06000038, 0x10090001),
    7:  (0x07000038, 0x10590001),
    8:  (0x08000041, 0x11090001),
    9:  (0x09000040, 0x11590001),
    10: (0x10000040, 0x12090001),
}


def _patch_sdk_version_in_file(file_path: str, fw_from: str, fw_to: str) -> bool:
    """Patch PS5/PS4 SDK version bytes in a binary file (ELF/SELF/PRX).
    Returns True if any bytes were patched.
    """
    major_from = _fw_to_major(fw_from)
    major_to = _fw_to_major(fw_to)

    if major_from not in _SDK_PAIRS or major_to not in _SDK_PAIRS:
        _log("  [SDK] Unsupported firmware: {} -> {} (major {} -> {})".format(
            fw_from, fw_to, major_from, major_to), YELLOW)
        return False

    if major_from == major_to:
        return False

    ps5_from, ps4_from = _SDK_PAIRS[major_from]
    ps5_to,   ps4_to   = _SDK_PAIRS[major_to]

    with open(file_path, "rb") as f:
        data = bytearray(f.read())

    patched = False
    for old_val, new_val in [(ps5_from, ps5_to), (ps4_from, ps4_to)]:
        old_bytes = struct.pack("<I", old_val)
        new_bytes = struct.pack("<I", new_val)
        pos = 0
        while True:
            idx = data.find(old_bytes, pos)
            if idx < 0:
                break
            data[idx:idx + 4] = new_bytes
            patched = True
            pos = idx + 4

    if patched:
        with open(file_path, "wb") as f:
            f.write(data)

    return patched


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

    # ---- Step 1: ELF Analysis ------------------------------------------

    def step_analysis(self, files: list[str]):
        _header("Step 1: ELF Analysis")
        try:
            ELFAnalyzer, ELFAnalyzerError = _import_elf()
        except ImportError:
            _log("[WARN] pyelftools not installed -- skipping analysis", YELLOW)
            return

        encrypted_count = 0
        for fpath in files:
            try:
                analyzer = ELFAnalyzer(fpath)
                report = analyzer.generate_report(
                    self.args.fw_target, self.args.exports_dir)
                fname = os.path.basename(fpath)
                self.results["step_analysis"][fname] = report
                score = report["compatibility_score"]
                is_self = report.get("is_self", False)
                note = report.get("note", "")

                if is_self and "encrypted" in note.lower():
                    encrypted_count += 1
                    sdk_str = report.get("sdk_ps5", "unknown")
                    _log("  {} -- SELF encrypted (SDK: {})".format(
                        fname, sdk_str), YELLOW)
                else:
                    color = GREEN if score >= 90 else (YELLOW if score >= 70 else RED)
                    _log("  {} -- score: {}%  missing: {}".format(
                        fname, score, len(report["missing_symbols"])), color)
            except Exception as ex:
                _log("  [WARN] Could not analyze {}: {}".format(
                    os.path.basename(fpath), ex), YELLOW)

        if encrypted_count > 0:
            _log("  [INFO] {} files are encrypted SELF -- SDK patching will still be attempted".format(
                encrypted_count), DIM)

    # ---- Step 2: BPS Patching ------------------------------------------

    def step_bps(self, files: list[str]):
        if not self.args.apply_bps:
            return
        _header("Step 2: BPS Patch Application")
        try:
            PatchDatabase, BPSError = _import_bps()
        except ImportError:
            _log("[WARN] bps_engine not available -- skipping BPS step", YELLOW)
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
                _log("  [ERR] {} -- {}".format(fname, ex), RED)
                self.results["step_bps"]["errors"].append(
                    {"file": fname, "error": str(ex)})

    # ---- Step 3: Auto-Stubbing -----------------------------------------

    def step_stub(self, files: list[str]):
        if not self.args.stub_missing:
            return
        _header("Step 3: Auto-Stubbing")
        try:
            AutoStubber, AutoStubberError = _import_stubber()
        except ImportError:
            _log("[WARN] capstone/keystone not installed -- skipping stub step", YELLOW)
            return

        for fpath in files:
            fname = os.path.basename(fpath)
            analysis = self.results["step_analysis"].get(fname, {})
            missing = analysis.get("missing_symbols", [])
            if not missing:
                continue
            try:
                stubber = AutoStubber(fpath)
                res = stubber.stub_missing(missing, mode="ret_zero")
                if res["stubbed"]:
                    stubber.save(fpath)
                    self.results["step_stub"]["stubbed"].extend(res["stubbed"])
                self.results["step_stub"]["not_found"].extend(res["not_found"])
                _log("  {} -- stubbed: {} / not_found: {}".format(
                    fname, len(res["stubbed"]), len(res["not_found"])),
                    GREEN if res["stubbed"] else YELLOW)
            except Exception as ex:
                _log("  [ERR] {} -- {}".format(fname, ex), RED)
                self.results["step_stub"]["errors"].append(
                    {"file": fname, "error": str(ex)})

    # ---- Step 4: SDK Version Patch -------------------------------------

    def step_sdk_patch(self, files: list[str]):
        _header("Step 4: SDK Version Patch")
        major_from = _fw_to_major(self.args.fw_current)
        major_to = _fw_to_major(self.args.fw_target)
        if major_from in _SDK_PAIRS and major_to in _SDK_PAIRS:
            ps5_f, ps4_f = _SDK_PAIRS[major_from]
            ps5_t, ps4_t = _SDK_PAIRS[major_to]
            _log("  PS5 SDK: 0x{:08X} -> 0x{:08X}".format(ps5_f, ps5_t), DIM)
            _log("  PS4 SDK: 0x{:08X} -> 0x{:08X}".format(ps4_f, ps4_t), DIM)
        for fpath in files:
            fname = os.path.basename(fpath)
            try:
                patched = _patch_sdk_version_in_file(
                    fpath, self.args.fw_current, self.args.fw_target)
                if patched:
                    self.results["step_sdk_patch"]["patched"].append(fname)
                    _log("  [SDK] {} -- patched".format(fname), GREEN)
                else:
                    self.results["step_sdk_patch"]["skipped"].append(fname)
                    _log("  [SDK] {} -- no match (skipped)".format(fname), DIM)
            except Exception as ex:
                _log("  [WARN] SDK patch failed for {}: {}".format(fname, ex), YELLOW)

    # ---- Step 5: Re-signing (placeholder) ------------------------------

    def step_resign(self, files: list[str]):
        if not self.args.resign:
            return
        _header("Step 5: Re-signing")
        selfutil = self.args.selfutil
        if not selfutil or not os.path.exists(selfutil):
            _log("[WARN] --selfutil not found -- skipping re-signing", YELLOW)
            return
        import subprocess
        for fpath in files:
            fname = os.path.basename(fpath)
            try:
                proc = subprocess.run(
                    [selfutil, "--resign", fpath],
                    capture_output=True, text=True, timeout=60)
                if proc.returncode == 0:
                    self.results["step_resign"]["resigned"].append(fname)
                    _log("  [SIGN] {} -- OK".format(fname), GREEN)
                else:
                    _log("  [SIGN] {} -- FAILED: {}".format(fname, proc.stderr[:200]), RED)
                    self.results["step_resign"]["errors"].append(
                        {"file": fname, "error": proc.stderr[:200]})
            except Exception as ex:
                _log("  [SIGN] {} -- error: {}".format(fname, ex), RED)
                self.results["step_resign"]["errors"].append(
                    {"file": fname, "error": str(ex)})

    # ---- Run -----------------------------------------------------------

    def run(self):
        t0 = time.time()
        _log(_c("\n[ABP] Advanced Backport Pipeline -- {} -> {}".format(
            self.args.fw_current, self.args.fw_target), BOLD + CYAN))

        files = self.collect_files()
        if not files:
            _log("[WARN] No target files found in {}".format(self.args.game_folder), YELLOW)

        # Auto-backup before modifying in-place
        if not self.args.output_folder or self.args.output_folder == self.args.game_folder:
            backup_dir = os.path.join(self.args.game_folder, "backup_pre_backport")
            if not os.path.exists(backup_dir):
                _log("[ABP] Creating backup in backup_pre_backport/ ...", DIM)
                for fpath in files:
                    rel = os.path.relpath(fpath, self.args.game_folder)
                    dst = os.path.join(backup_dir, rel)
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    shutil.copy2(fpath, dst)
                _log("[ABP] Backup created ({} files)".format(len(files)), GREEN)
            else:
                _log("[ABP] Backup already exists -- skipping", DIM)

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

        self.step_analysis(files)
        self.step_bps(files)
        self.step_stub(files)
        self.step_sdk_patch(files)
        self.step_resign(files)

        self.results["total_time_s"] = round(time.time() - t0, 2)
        return self.results

    def print_summary(self):
        r = self.results
        _header("Final Summary")
        _log("  Files found:      {}".format(len(r["files_found"])))
        _log("  BPS applied:      {}".format(len(r["step_bps"]["applied"])))
        _log("  BPS skipped:      {}".format(len(r["step_bps"]["skipped"])))
        _log("  BPS errors:       {}".format(len(r["step_bps"]["errors"])))
        _log("  Symbols stubbed:  {}".format(len(r["step_stub"]["stubbed"])))
        _log("  SDK patched:      {}".format(len(r["step_sdk_patch"]["patched"])))
        _log("  Re-signed:        {}".format(len(r["step_resign"]["resigned"])))
        _log("  Total time:       {}s".format(r["total_time_s"]))
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
