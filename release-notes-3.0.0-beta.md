## PS5 BACKPORK KITCHEN v3.0.0-beta

### ⚠️ IMPORTANT - Beta Release for Testing

This is a **beta release** with major architectural changes. Please test thoroughly before using on production games. Current stable version is **2.3.0**.

---

## 🆕 What's New

### 1. Internal SELF Extraction Engine (Native VB.NET)

**No more external dependencies for SELF decryption!**

- Native SELF decoder written in VB.NET
- Automatic detection of SELF/ELF/Stripped ELF files
- Heuristic validation for stripped ELF files (zeroed headers)
- Fallback to external selfutil with 60-second timeout protection
- Prevents app freezing on corrupted/unsupported files

**Files:**
- `Services/SelfUtilCode/SelfFile.vb` - SELF parser and extractor
- `Services/SelfUtilCode/ElfLogger.vb` - ELF structure logging
- `Services/selfutilmodule.vb` - Unified interface

**To Test:**
- Load games with SELF-encrypted files (eboot.bin, *.prx)
- Load games with stripped ELF files (libSceFace.prx, libScePfs.prx)
- Verify decryption succeeds without external tools

---

### 2. Strict Pipeline Contract Enforcement

**Mandatory execution order for safe backporting:**

```
Step 1: Detect file type (SELF/ELF/StrippedELF)
Step 2: If SELF → Decrypt to valid ELF
Step 3: Validate ELF structural integrity
Step 4: Read and verify current SDK version
Step 5: Patch ELF headers to target SDK
Step 6: Optional libc.prx string patch (6xx compatibility)
Step 7: Re-sign patched ELF back to SELF (MANDATORY)
Step 8: Overwrite original ONLY after signing succeeds
```

**Critical Rules:**
- Signing is **always mandatory** (never return unsigned files)
- libc patch executes **after** SDK patch, **before** signing
- Original file **never modified** until signing succeeds
- Working copy strategy ensures atomicity

**Files:**
- `Architecture/Application/Services/ElfPatchingService.vb` - Pipeline implementation
- `core/ElfPatcher.vb` - SDK patching logic

**To Test:**
- Patch single files (eboot.bin) and verify signing occurs
- Test with files already at target SDK (should skip patch but still sign)
- Test with libc.prx and enable 6xx patch checkbox
- Verify original file unchanged if signing fails
- Check for atomic replacement (no partial writes)

---

### 3. Advanced Error Reporting & Diagnostics

**Full stack trace capture and detailed error reports.**

**VB.NET Side:**
- `PipelineError` structure: stage, file path, message, stack trace, timestamp, context
- `LogPipelineError()` captures all exceptions with diagnostic context
- `GenerateErrorReport()` formats detailed error log
- `SaveErrorReportToFile()` writes report to disk

**Python Side:**
- `PipelineError` class with exception type and context
- `generate_error_report()` creates formatted report
- Automatic `backport_errors.log` generation on failure
- Stack traces limited to last 2 frames for readability

**Files:**
- `ElfPatchingService.vb` - VB error tracking (lines 75-82, 378-415)
- `scripts/advanced_backport.py` - Python error tracking (lines 62-122, 378-415)

**To Test:**
- Trigger errors (invalid files, corrupted SELFs, missing SDK segments)
- Check error report file generation
- Verify stack traces are captured
- Verify context information (SDK versions, file paths, exception types)
- Check console output shows detailed error info

---

### 4. Service Layer Architecture Improvements

**Removed all UI dependencies from service layer.**

- Removed `Form1.chklibcpatch` direct reference
- libc patch flag now passed via `ElfPatchingService` constructor
- Removed `Form1.rtbStatus` from `ElfPatcher.vb`
- Logger-based output instead of direct UI updates

**Files:**
- `ElfPatchingService.vb` - Constructor now accepts `enableLibcPatch` parameter
- `ElfPatcher.vb` - Line 63 commented out (no Form1 reference)

**To Test:**
- Batch patching operations
- Verify logging still works correctly
- Check no UI freeze during long operations

---

### 5. NID Database & Smart Symbol Resolution

**Built-in knowledge base of 200+ PS5 functions.**

- Function classification by category (init/term/getter/setter/network/render)
- Risk-based stubbing (safe/low/medium/high/critical)
- Heuristic prefix matching for unknown functions
- Per-function stub mode selection

**Files:**
- `scripts/ps5_nid_db.py` - NID knowledge base
- `scripts/lib_compat.py` - Library compatibility analyzer
- `scripts/auto_stubber.py` - Smart stubbing engine

**To Test:**
- Run advanced backport pipeline on games
- Check NID resolution accuracy in analysis reports
- Verify critical functions are NOT stubbed (logged as warnings)
- Check compatibility score calculation

---

### 6. Fakelib-Only Backporting (Default Safe Mode)

**SDK/Param patching now OPT-IN (disabled by default).**

- Checkbox for SDK patching (unchecked by default)
- Checkbox for param.json patching (unchecked by default)
- Fakelib installation with automatic cleanup
- Runtime patcher detection for FW7+

**Files:**
- `01Frms/AdvancedBackportForm.vb` - Added `chkPatchSdk`, `chkPatchParam`
- `scripts/advanced_backport.py` - `--patch-sdk` and `--patch-param` flags

**To Test:**
- Run backport with both checkboxes unchecked (fakelib-only mode)
- Verify game files remain unmodified
- Verify fakelib folder installed correctly
- Enable checkboxes and verify SDK/param patching occurs

---

## 📋 Testing Checklist

### Basic Functionality
- [ ] Application launches without errors
- [ ] Load game folder successfully
- [ ] Detect SDK version correctly
- [ ] SELF decryption works (internal engine)
- [ ] Stripped ELF detection works
- [ ] SDK patching works (when enabled)
- [ ] Signing always occurs
- [ ] Error report generated on failures

### Pipeline Integrity
- [ ] Original files unchanged on failure
- [ ] Signing is mandatory (never skipped)
- [ ] libc patch executes before signing
- [ ] Atomic file replacement (no partial writes)
- [ ] Timeout protection prevents freezing

### Error Handling
- [ ] Stack traces captured correctly
- [ ] Error report file created
- [ ] Console shows detailed errors
- [ ] Context information included

### Advanced Features
- [ ] NID resolution works
- [ ] Smart stubbing logic correct
- [ ] Library compatibility analysis
- [ ] Fakelib installation works
- [ ] Runtime patcher detected (FW7+)

---

## 🐛 Known Issues

- None reported yet (beta testing phase)

---

## 📦 Installation

1. Extract ZIP to desired location
2. Ensure .NET 8.0 Runtime installed
3. Run `PS5 BACKPORK KITCHEN.exe`
4. SelfUtil and FreeFakeSign downloaded automatically on first run

---

## 🔄 Upgrade from v2.3.0

**Backup your data first!**

1. Close v2.3.0 application
2. Extract v3.0.0-beta to NEW folder (don't overwrite)
3. Copy your config files if needed
4. Test with non-critical games first
5. Report any issues on GitHub

---

## 📝 Changelog

### Added
- Internal SELF extraction engine (native VB.NET)
- Stripped ELF heuristic detection
- Strict pipeline contract with mandatory signing
- Advanced error reporting with stack traces
- PipelineError structure for diagnostics
- Error report file generation
- Service layer architecture cleanup
- NID database with 200+ functions
- Smart symbol stubbing
- Library compatibility analyzer
- Fakelib-only mode (default)
- SDK/param patching opt-in checkboxes
- Timeout protection for selfutil
- Atomic file replacement

### Changed
- Signing now mandatory for all files
- libc patch executes before signing (not after)
- Original files preserved until signing succeeds
- Error handling includes full stack traces
- Service layer no longer depends on UI

### Fixed
- App freezing on corrupted SELF files
- Partial file writes on errors
- Missing error context in logs
- UI coupling in service layer

---

## 🤝 Contributing

This is a beta release. Please report:
- Bugs and crashes
- Files that fail to process
- Unexpected behavior
- Performance issues

Create issues at: https://github.com/DroneTechTI/PS5-BACKPORK-KITCHEN/issues

---

## ⚙️ Technical Details

**Build Info:**
- .NET 8.0 (Windows)
- VB.NET + Python hybrid architecture
- Dependencies: FluentFTP, Newtonsoft.Json, ReaLTaiizor, SharpCompress, SQLite

**Pipeline Architecture:**
- VB.NET: GUI, service layer, SELF engine
- Python: Analysis, stubbing, NID resolution

**File Structure:**
- `PS5 BACKPORK KITCHEN.exe` - Main application
- `scripts/` - Python pipeline scripts
- `tools/` - PUP unpacker tools
- `Resources/` - Images and assets

---

## ⚠️ Disclaimer

Use at your own risk. Always backup game files before patching. This tool is for educational and research purposes.
