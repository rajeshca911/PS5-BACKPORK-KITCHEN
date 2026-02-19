"""
Library Compatibility Analyzer — Cross-references game library requirements
against available fakelibs and the NID knowledge base.

For each library the game requires:
  1. Check if a fakelib exists for the target firmware
  2. Check if the library is critical (GPU, kernel, etc.)
  3. Recommend action: use fakelib, stub individual functions, or flag as risky

Usage:
  python lib_compat.py --game-folder /path/to/game --fw-target 6.00 --fakelibs fakelibs.json
"""

import argparse
import json
import os
import sys
from typing import Optional


# ---------------------------------------------------------------------------
# Fakelibs Database
# ---------------------------------------------------------------------------

class FakelibsDB:
    """Loads and queries fakelibs.json for available fake library replacements."""

    def __init__(self, fakelibs_path: str):
        self._data = {}
        self._payloads = {}
        if os.path.exists(fakelibs_path):
            with open(fakelibs_path, encoding="utf-8") as f:
                self._data = json.load(f)
            self._payloads = self._data.get("payloads", {})

    def get_fakelibs_for_fw(self, target_fw: str) -> list[dict]:
        """Return fakelib entries available for the target firmware.
        Matches by major version number (e.g., "6.00" matches fw_version "6").
        """
        major = target_fw.split(".")[0]
        result = []
        for entry in self._payloads.get("fakelibs", []):
            if entry.get("fw_version") == major:
                result.append(entry)
        return result

    def get_available_fakelib_names(self, target_fw: str) -> set[str]:
        """Return set of library names available as fakelibs for target FW.
        Normalizes names: fakelibs.json may list "libSceAgc" without extension.
        """
        names = set()
        for entry in self.get_fakelibs_for_fw(target_fw):
            for fname in entry.get("files", []):
                # Normalize: add .sprx if no extension
                if not fname.endswith((".sprx", ".prx", ".elf")):
                    names.add(fname + ".sprx")
                names.add(fname)
        return names

    def get_recommended_fw(self) -> Optional[str]:
        """Return the recommended fakelib firmware version."""
        for entry in self._payloads.get("fakelibs", []):
            if entry.get("is_recommended"):
                return entry.get("fw_version")
        return None

    def has_fakelib(self, lib_name: str, target_fw: str) -> bool:
        """Check if a fakelib exists for the given library on target FW."""
        available = self.get_available_fakelib_names(target_fw)
        # Check with and without .sprx
        base = lib_name.replace(".sprx", "").replace(".prx", "")
        return lib_name in available or base in available or (base + ".sprx") in available


# ---------------------------------------------------------------------------
# Library Compatibility Analyzer
# ---------------------------------------------------------------------------

class LibCompatAnalyzer:
    """Analyzes library compatibility for a game against a target firmware."""

    def __init__(self, fakelibs_path: str):
        self.fakelibs = FakelibsDB(fakelibs_path)

        # Try to load NID DB for library metadata
        self._nid_db = None
        self._known_libs = {}
        try:
            from ps5_nid_db import PS5NidDB, KNOWN_LIBRARIES
            self._nid_db = PS5NidDB()
            self._known_libs = KNOWN_LIBRARIES
        except ImportError:
            pass

    def analyze(self, required_libs: list[str], target_fw: str,
                analysis_report: dict = None) -> dict:
        """Analyze library compatibility.

        Args:
            required_libs: List of library names the game requires
            target_fw: Target firmware version (e.g., "6.00")
            analysis_report: Optional ELF analysis report with missing_symbols

        Returns dict with:
            - lib_results: per-library analysis
            - recommendations: actionable recommendations
            - fakelibs_available: which fakelibs exist
            - compatibility_score: 0-100
            - risk_level: NONE/LOW/MEDIUM/HIGH/CRITICAL
        """
        available_fakelibs = self.fakelibs.get_available_fakelib_names(target_fw)
        recommended_fw = self.fakelibs.get_recommended_fw()

        lib_results = []
        total_score = 0
        max_risk = "NONE"
        recommendations = []

        for lib in required_libs:
            result = self._analyze_lib(lib, target_fw, available_fakelibs,
                                       analysis_report)
            lib_results.append(result)

            # Track worst risk
            lib_risk = result.get("risk", "NONE")
            max_risk = _worst_risk(max_risk, lib_risk)

            # Accumulate score
            total_score += result.get("score", 100)

            # Build recommendations
            rec = result.get("recommendation")
            if rec:
                recommendations.append(rec)

        # Overall score: average of per-lib scores
        n_libs = len(required_libs) if required_libs else 1
        overall_score = total_score // n_libs if n_libs > 0 else 100

        # Also check for fakelibs we recommend but the game doesn't explicitly need
        # (some games implicitly depend on libraries loaded by others)
        also_recommend = []
        if recommended_fw:
            for fakelib_entry in self.fakelibs.get_fakelibs_for_fw(target_fw):
                for fname in fakelib_entry.get("files", []):
                    if fname.endswith(".elf"):
                        continue  # skip payload ELFs
                    normalized = fname if fname.endswith(".sprx") else fname + ".sprx"
                    if normalized not in required_libs:
                        also_recommend.append(fname)

        return {
            "target_fw": target_fw,
            "required_libs": required_libs,
            "lib_results": lib_results,
            "recommendations": recommendations,
            "fakelibs_available": sorted(available_fakelibs),
            "also_recommend": also_recommend,
            "compatibility_score": overall_score,
            "risk_level": max_risk,
            "recommended_fakelib_fw": recommended_fw,
        }

    def _analyze_lib(self, lib_name: str, target_fw: str,
                     available_fakelibs: set, analysis_report: dict = None) -> dict:
        """Analyze a single library's compatibility."""
        base_name = lib_name.replace(".sprx", "").replace(".prx", "")
        has_fakelib = self.fakelibs.has_fakelib(lib_name, target_fw)

        # Get library metadata from NID DB
        lib_info = self._known_libs.get(lib_name, {})
        category = lib_info.get("category", "unknown")
        is_essential = lib_info.get("essential", False)
        description = lib_info.get("desc", lib_name)

        # Count missing symbols for this library from analysis report
        missing_for_lib = 0
        critical_missing = 0
        if analysis_report:
            for sym in analysis_report.get("missing_symbols", []):
                sym_lib = sym.get("library", "")
                if sym_lib == lib_name or base_name in sym_lib:
                    missing_for_lib += 1
                    if sym.get("stub_risk") == "critical":
                        critical_missing += 1

        # Determine risk and recommendation
        if has_fakelib:
            risk = "LOW"
            score = 90
            recommendation = {
                "lib": lib_name,
                "action": "use_fakelib",
                "detail": "Fakelib available for FW {} — use it".format(target_fw.split(".")[0]),
            }
        elif is_essential and category in ("kernel", "gpu"):
            risk = "CRITICAL"
            score = 20
            recommendation = {
                "lib": lib_name,
                "action": "fakelib_needed",
                "detail": "CRITICAL: {} is essential, no fakelib available — high crash risk".format(
                    description),
            }
        elif critical_missing > 0:
            risk = "HIGH"
            score = 40
            recommendation = {
                "lib": lib_name,
                "action": "stub_risky",
                "detail": "{} critical function(s) cannot be safely stubbed".format(
                    critical_missing),
            }
        elif missing_for_lib > 0:
            risk = "MEDIUM"
            score = 70
            recommendation = {
                "lib": lib_name,
                "action": "stub_functions",
                "detail": "Stub {} missing function(s) individually".format(missing_for_lib),
            }
        else:
            risk = "NONE"
            score = 100
            recommendation = None

        return {
            "lib": lib_name,
            "category": category,
            "essential": is_essential,
            "description": description,
            "has_fakelib": has_fakelib,
            "missing_functions": missing_for_lib,
            "critical_missing": critical_missing,
            "risk": risk,
            "score": score,
            "recommendation": recommendation,
        }


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

_RISK_ORDER = {"NONE": 0, "LOW": 1, "MEDIUM": 2, "HIGH": 3, "CRITICAL": 4}


def _worst_risk(a: str, b: str) -> str:
    return a if _RISK_ORDER.get(a, 0) >= _RISK_ORDER.get(b, 0) else b


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Library Compatibility Analyzer for PS5 backporting")
    parser.add_argument("--required-libs", nargs="*", default=[],
                        help="Library names the game requires")
    parser.add_argument("--analysis-report", default=None,
                        help="Path to ELF analysis report JSON (optional)")
    parser.add_argument("--fw-target", required=True,
                        help="Target firmware version (e.g., 6.00)")
    parser.add_argument("--fakelibs", required=True,
                        help="Path to fakelibs.json")
    parser.add_argument("--output", default=None,
                        help="Save report as JSON")
    args = parser.parse_args()

    analyzer = LibCompatAnalyzer(args.fakelibs)

    analysis = None
    if args.analysis_report and os.path.exists(args.analysis_report):
        with open(args.analysis_report, encoding="utf-8") as f:
            analysis = json.load(f)

    result = analyzer.analyze(args.required_libs, args.fw_target, analysis)

    output = json.dumps(result, indent=2)
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(output)
        print("[COMPAT] Report saved to {}".format(args.output))
    else:
        print(output)

    score = result["compatibility_score"]
    risk = result["risk_level"]
    print("\n[COMPAT] Score: {}% — Risk: {}".format(score, risk))

    if result["recommendations"]:
        print("\nRecommendations:")
        for rec in result["recommendations"]:
            action = rec["action"]
            icon = {"use_fakelib": "+", "fakelib_needed": "!",
                    "stub_risky": "!", "stub_functions": "~"}.get(action, "?")
            print("  [{}] {} — {}".format(icon, rec["lib"], rec["detail"]))


if __name__ == "__main__":
    main()
