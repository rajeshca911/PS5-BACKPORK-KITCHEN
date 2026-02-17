"""
Symbol Diff — Compare exported symbols between two PS5 firmware versions.

Usage:
  python sym_diff.py --from data/exports/7.61.json --to data/exports/10.01.json
  python sym_diff.py --from data/exports/7.61.json --to data/exports/10.01.json --lib libSceNpAuth.sprx
  python sym_diff.py --from data/exports/7.61.json --to data/exports/10.01.json --output diff_report.json
"""

import argparse
import json
import os
import sys


# ---------------------------------------------------------------------------
# Diff Logic
# ---------------------------------------------------------------------------

def load_exports(path: str) -> dict:
    """Load {lib: {name: nid}} from a firmware exports JSON file."""
    if not os.path.exists(path):
        print("[ERROR] File not found: {}".format(path), file=sys.stderr)
        sys.exit(1)
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def diff_firmware(fw_from: dict, fw_to: dict, lib_filter: str = None) -> dict:
    """Compute full diff between two firmware exports.

    Returns:
    {
      "libs_added":   [list of lib names],
      "libs_removed": [list of lib names],
      "libs_changed": {
        lib_name: {
          "symbols_added":   {name: nid},
          "symbols_removed": {name: nid},
        }
      },
      "stats": {
        "total_libs_from": int,
        "total_libs_to":   int,
        "total_syms_from": int,
        "total_syms_to":   int,
      }
    }
    """
    libs_from = set(fw_from.keys())
    libs_to = set(fw_to.keys())

    if lib_filter:
        libs_from = {l for l in libs_from if l == lib_filter}
        libs_to   = {l for l in libs_to   if l == lib_filter}

    result = {
        "libs_added":   sorted(libs_to - libs_from),
        "libs_removed": sorted(libs_from - libs_to),
        "libs_changed": {},
        "stats": {
            "total_libs_from": len(fw_from),
            "total_libs_to":   len(fw_to),
            "total_syms_from": sum(len(v) for v in fw_from.values()),
            "total_syms_to":   sum(len(v) for v in fw_to.values()),
        },
    }

    common_libs = libs_from & libs_to
    for lib in sorted(common_libs):
        syms_from = fw_from[lib]
        syms_to   = fw_to[lib]
        added   = {k: v for k, v in syms_to.items()   if k not in syms_from}
        removed = {k: v for k, v in syms_from.items() if k not in syms_to}
        if added or removed:
            result["libs_changed"][lib] = {
                "symbols_added":   added,
                "symbols_removed": removed,
            }

    return result


# ---------------------------------------------------------------------------
# Pretty Print
# ---------------------------------------------------------------------------

RESET = "\033[0m"
GREEN = "\033[92m"
RED   = "\033[91m"
CYAN  = "\033[96m"
BOLD  = "\033[1m"


def print_diff(diff: dict, fw_from_name: str, fw_to_name: str):
    stats = diff["stats"]
    print("{}Symbol Diff: {} → {}{}".format(BOLD, fw_from_name, fw_to_name, RESET))
    print("  Libraries: {} → {}".format(stats["total_libs_from"], stats["total_libs_to"]))
    print("  Symbols:   {} → {}".format(stats["total_syms_from"], stats["total_syms_to"]))
    print()

    if diff["libs_added"]:
        print("{}[+] Libraries added ({}){}".format(GREEN, len(diff["libs_added"]), RESET))
        for lib in diff["libs_added"]:
            print("      {}+{} {}".format(GREEN, RESET, lib))
        print()

    if diff["libs_removed"]:
        print("{}[-] Libraries removed ({}){}".format(RED, len(diff["libs_removed"]), RESET))
        for lib in diff["libs_removed"]:
            print("      {}-{} {}".format(RED, RESET, lib))
        print()

    if diff["libs_changed"]:
        print("{}[~] Libraries changed ({}){}".format(CYAN, len(diff["libs_changed"]), RESET))
        for lib, changes in diff["libs_changed"].items():
            added_count   = len(changes["symbols_added"])
            removed_count = len(changes["symbols_removed"])
            print("  {} (+{} / -{})".format(lib, added_count, removed_count))
            for name in sorted(changes["symbols_added"])[:10]:
                print("      {}+{} {}".format(GREEN, RESET, name))
            if added_count > 10:
                print("      {} ... and {} more added".format(GREEN, added_count - 10))
            for name in sorted(changes["symbols_removed"])[:10]:
                print("      {}-{} {}".format(RED, RESET, name))
            if removed_count > 10:
                print("      {} ... and {} more removed".format(RED, removed_count - 10))
        print()

    total_changed = sum(
        len(v["symbols_added"]) + len(v["symbols_removed"])
        for v in diff["libs_changed"].values()
    )
    print("Total changes: {} lib additions, {} lib removals, {} symbol changes".format(
        len(diff["libs_added"]), len(diff["libs_removed"]), total_changed))


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Compare PS5 firmware symbol exports")
    parser.add_argument("--from", dest="fw_from", required=True,
                        help="Path to source firmware exports JSON")
    parser.add_argument("--to", dest="fw_to", required=True,
                        help="Path to target firmware exports JSON")
    parser.add_argument("--lib", default=None,
                        help="Filter to a single library name")
    parser.add_argument("--output", default=None,
                        help="Save diff report as JSON")
    parser.add_argument("--json", action="store_true",
                        help="Print JSON output instead of human-readable")
    args = parser.parse_args()

    fw_from_data = load_exports(args.fw_from)
    fw_to_data   = load_exports(args.fw_to)

    diff = diff_firmware(fw_from_data, fw_to_data, lib_filter=args.lib)

    fw_from_name = os.path.splitext(os.path.basename(args.fw_from))[0]
    fw_to_name   = os.path.splitext(os.path.basename(args.fw_to))[0]

    if args.json:
        print(json.dumps(diff, indent=2))
    else:
        print_diff(diff, fw_from_name, fw_to_name)

    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(diff, f, indent=2)
        print("\n[SYM_DIFF] Report saved to {}".format(args.output))
