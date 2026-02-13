# Changelog

All notable changes to PS5 BackPork Kitchen will be documented in this file.
Here’s a **clean, release-ready CHANGELOG** you can paste directly into `CHANGELOG.md`.
I’ve written it in a **maintainer-friendly but gamer/dev-readable tone**, matching your project style.

---

## 🧾 CHANGELOG


## [2.2.0] – Payload Management System

### ✨ New Features

- **📤 Payload Manager**: Complete payload library and deployment system
  - SQLite-based payload library with metadata tracking
  - Add, edit, delete payloads with categories (Jailbreak, Homebrew, Debug, Backup, Custom)
  - File validation and path management
  - Tags and versioning support
- **🚀 One-Click Payload Deployment**: Send payloads to PS5 instantly
  - Single payload send via FTP
  - Batch send all visible payloads
  - Progress tracking with real-time status updates
  - Send history tracking per payload
- **📊 Payload Statistics**: Track deployment activity
  - Total payloads and sends
  - Success rate calculation
  - Most sent payload tracking
  - Category breakdown
- **🔧 Smart Validation**: Pre-send checks ensure reliability
  - File existence verification
  - Size validation
  - Target path validation
  - Connection status checks

### 🛠 Technical Details

- New services: `PayloadLibrary.vb`, `PayloadSenderService.vb`
- New UI forms: `PayloadManagerForm.vb`, `PayloadEditForm.vb`
- Integration with existing FTP infrastructure (FluentFTP)
- Color palette extended with `FeatureTeal` for button theming
- Payload database: `payloads.db` with 3 tables (Payloads, SendHistory, PayloadProfiles)

### 🔮 Future Plans

- HTTP protocol support for payload delivery
- USB protocol support
- Payload profiles for batch operations
- Auto-send on connection

---

## [2.1.0] – Statistics & Analytics System

### ✨ New Features

- **📊 SQLite Statistics Database**: Replaced JSON-based stats with SQLite
  - Unlimited session history (no more 100-session limit)
  - 4-table schema: Sessions, Operations, DailyStats, AppSettings
  - Fast queries with indexed columns
- **📈 Statistics Dashboard**: Rich visual statistics interface
  - Overall stats: Total operations, success rate, files patched, total time
  - Operation types breakdown with counts
  - Recent sessions with color-coded status
  - Time period filters (7/30/90 days, all time)
  - Operation type filters
- **📤 Export Functionality**: Export stats to CSV format
  - Session data export
  - Daily statistics export
- **🔧 IDisposable Pattern**: Proper resource management for database connections

### 🛠 Technical Details

- New service: `StatisticsDatabase.vb`
- New UI form: `StatisticsForm.vb`
- Database: `statistics.db` in AppData folder
- Replaced old `StatisticsManager.vb` JSON approach

---


## [2.0.0] – Major Update

This release focuses on stability, cleaner workflows, and powerful ELF handling improvements.
Many internal changes were made to ensure safer operations and a smoother user experience.

### ✨ New & Improved Features

- Improved **Game Library** handling with better metadata flow and smoother UI behavior.
- Added a unified **ELF operation logic**, ensuring consistent behavior across:
  - Decrypt
  - Patch
  - Sign
- Introduced context-menu based ELF actions for faster, power-user workflows.
- Improved **Full Pipeline** support:
  - Decrypt → Patch → Sign now follows a clean and predictable flow.
  - Per-file operation results are handled more safely.

### 🛠 Patching & Signing Improvements

- Prevents **double-signing** of already signed ELFs.
- Detects **already patched** ELF files and skips unnecessary operations.
- Cleaner handling of optional and non-fatal patches.
- More reliable firmware-aware behavior during downgrade and patching steps.

### ⚡ Stability & UI Fixes

- Fixed multiple UI bugs and layout issues.
- Improved form load timing to prevent unexpected UI behavior.
- Better control alignment and interaction consistency.
- Safer refresh logic to avoid UI or state desync issues.

### 🧹 Internal Cleanup & Refactoring

- Simplified internal logic for better maintainability.
- Removed unused or legacy code paths.
- Improved overall code readability and structure.
- Ignored IDE-specific user files to keep the repository clean.

### 🙏 Credits & Thanks

- Special thanks to [DroneTechIT](https://github.com/DroneTechIT) for valuable contributions,
  feature ideas, and improvements that helped shape this release.

- Thanks to all testers and community members who provided feedback and validation.

---

### 🚀 v1.3.0 — FTP Browser & Stability Update

#### ✨ New Features

* 📡 **Built-in FTP Browser (PS5 ⇄ PC)**

  * Direct file transfer without USB
  * Browse PS5 filesystem visually
  * Download / upload files and folders
  * Context menu actions (Download, Upload, Rename, Delete, New Folder)
* 💾 **FTP Profile Support**
  * Save and reuse multiple PS5 connections
  * Test connection before use
* 🧭 **Fast Navigation**

  * Home, Up, Refresh buttons
  * Double-click folders to navigate
* 📊 **Progress Feedback**

  * Transfer progress bar
  * Live status updates during operations

#### 🖥️ UI / UX Improvements

* 🧼 Fully refactored FTP Browser UI (clean, modern, DPI-safe)
* 🧱 Stable layout (no overlapping controls)
* 🖱️ Context menus integrated into file list

#### 🛠️ Fixes & Stability

* 🐛 Fixed multiple **NullReferenceException** crashes
* 🐛 Fixed DataGridView binding errors and column mismatches
* 🐛 Fixed selection change crashes during refresh / disconnect
* 🐛 Fixed UI freeze during async FTP operations
* 🐛 Fixed fakelib copying error
* 🧯 Safer async handling for connect / disconnect
* 🧯 Guarded UI updates to prevent race conditions

#### 🔧 Internal Improvements

* ♻️ Rewritten `FtpBrowserForm` for safety and maintainability
* ♻️ Hardened `FtpManager` (connection lifecycle, error handling)
* 🧠 Reusable FTP operation runner for all file actions
* 📁 Cleaner separation of UI, logic, and services
* 🛡️ Reduced antivirus false positives (safer process execution)

#### 🧪 Developer Notes

* PR #10 fully reviewed, fixed, and merged
* FTP feature tested end-to-end locally
* No breaking changes to existing workflows

---

### 🔜 Planned (Roadmap)

* 📦 Batch operations (multi-select)
* 🧾 Transfer history / logs
* 🔐 Optional FTP over SSL (if supported by payloads)

---



## [1.2.0] - 2025-01-25

### Added - NEW WINDOWS (Priority 2 Features)
- **Advanced Settings Dialog**: Complete settings window with tabbed interface
  - General tab: Backup settings, backup location configuration
  - Appearance tab: Theme selector, background image picker with preview, language selector
  - Advanced tab: Logging options, performance settings
  - Real-time preview of background image
  - Apply/OK/Cancel workflow
- **Operation History Viewer**: Dedicated window for viewing operation history
  - DataGridView with sortable columns
  - Filters: All, Success Only, Failed Only, Today, Last 7 Days
  - Export history to CSV
  - Clear history functionality
  - Color-coded success/failure indicators
  - Status bar with operation count
- **ELF Inspector Window**: Professional analysis window (replaces MessageBox)
  - Split-panel interface: file list + detailed view
  - Real-time folder analysis with progress bar
  - Per-file SDK detection and patchability status
  - Summary statistics: min/max/recommended SDK
  - Export analysis to TXT or CSV
  - File details panel with SDK information
  - Automatic detection of all ELF file types

### Added - Previous Features
- **Custom Background Image Support**: Set custom background images for personalized UI
  - Load images from file system
  - Automatic scaling and layout options
  - Persistent background preferences
  - Compatible with all themes
- **Recent Folders Quick Access**: Button with popup menu for last 10 patched games
  - Game name detection from param.json
  - Last used SDK version display
  - Last used date tracking
  - Clear history option
- **Preset Selector**: ComboBox for quick configuration switching
  - Apply saved presets instantly
  - Visual feedback on preset application
  - Compatible with all existing presets
- **Multi-Language UI**: Complete interface translation system
  - English, Italian, German support
  - Language switcher in top-right corner
  - 100% translated dialogs and messages
  - Persistent language preference
- **Theme Selector Button**: Easy theme switching
  - Light, Dark, High Contrast, System themes
  - Visual theme menu with current selection
  - Seamless theme application
- **Modern Gradient Backgrounds**: Professional UI appearance
  - Light Theme: AliceBlue (#F0F8FF)
  - Dark Theme: Deep Blue-Purple (#191C2D) 
  - High Contrast: Pure Black for accessibility
  - Subtle diagonal pattern overlay (skipped in High Contrast)

### Improved
- **UI Layout Optimization**: Fixed all overlapping issues
  - Feature buttons repositioned to left side (y=330-386)
  - Reduced button height to 25px for cleaner look
  - Optimized spacing between all UI elements
  - No overlap with Separator or other controls
- **Theme System Overhaul**: Better readability across all themes
  - Dark theme colors inspired by VS Code (RGB 45,45,48)
  - High Contrast with pure black/white for accessibility
  - All labels now forced to use theme foreground color
  - LinkLabels with proper accent colors
  - Custom controls (RealTaiizor) excluded from theme application
  - Feature buttons maintain custom colors in all themes
- **Text Visibility**: All text elements now readable
  - Credits label always visible
  - Drag & Drop hint always visible
  - All status messages properly colored
  - Transparent backgrounds for better rendering

### Fixed
- UI overlapping issues with Statistics, ELF Inspector, and Batch buttons
- Text visibility in Dark and High Contrast themes
- Label colors not respecting theme settings
- Background pattern showing over custom images
- Drag & Drop hint disappearing in some themes

### Technical Details
- Added background image management to ThemeManager
- Made `IsSystemDarkMode()` public for external access
- Added `OnPaint()` override for decorative pattern
- Enabled `DoubleBuffered` for smooth rendering
- Smart control exclusion for custom UI libraries
- 20+ new translation keys per language
- 4 major UI commits with incremental improvements



### Added
- **Batch Processing System**: Process multiple game folders in one operation
  - Sequential and concurrent processing modes
  - Detailed batch reports with success/failure tracking
  - Per-game progress tracking
- **Recent Folders Manager**: Quick access to previously patched folders
  - Automatic game name detection from param.json
  - Last used SDK version tracking
  - Success count tracking per folder
- **SDK Auto-Detection**: Automatically detect SDK versions from ELF files
  - Analyze entire folder structure
  - Recommend optimal target SDK
  - Generate detailed analysis reports
  - Support for version formatting and comparison
- **Preset Manager**: Save and manage patch configurations
  - 5 built-in presets (Maximum Compatibility, Safe Standard, Modern Features, Quick Patch, Professional)
  - Create custom presets
  - Import/export presets
  - Clone presets
  - Track preset usage statistics
- **ELF Inspector**: Deep analysis of ELF files
  - Detect file type (ELF, FSELF PS4/PS5)
  - Extract SDK versions
  - Analyze library dependencies
  - Generate warnings for potential issues
  - Compare two ELF files
  - Batch inspection of folders
- **Statistics Dashboard**: Track all operations
  - Total operations and success rate
  - Files patched/skipped/failed counters
  - Time spent tracking
  - Session history (last 100 sessions)
  - Period statistics (last 7/30 days)
  - Top productive days
  - Export statistics to file
- **Multi-Language Support**: 8 languages supported
  - English, Italian, Spanish, French, German, Japanese, Portuguese, Russian
  - Persistent language preference
  - Easy switching between languages
- **Theme Manager**: Customizable UI themes
  - Light, Dark, High Contrast themes
  - System theme detection (Windows 10/11)
  - Automatic theme application to all controls
  - Export theme colors
- **Drag & Drop Support**: Drag folders directly into the app
  - Single folder drag & drop
  - Multiple folders drag & drop (batch mode)
  - Visual feedback on drag over
  - Automatic validation of dropped folders

### Technical Details
- Added 9 new service modules
- ~2,100 lines of new code
- All features fully documented
- Zero breaking changes

## [1.1.0] - 2025-01-23

### Added
- **Advanced File Logger**: Comprehensive logging system with file output and automatic log rotation
  - Logs saved to `backpork_log.txt` with automatic rotation at 10MB
  - Log levels: Info, Success, Warning, Error, Debug
  - Session tracking with timestamps
- **Input Validation System**: Robust validation for game folders before patching
  - Checks for read/write permissions
  - Validates folder structure and ELF file presence
  - Displays detailed validation reports
- **Integrity Verification**: Post-patch file integrity checking
  - Verifies ELF structure after patching
  - SHA256 checksum calculation for backup manifests
  - Detailed verification reports
- **Enhanced Backup System**: Comprehensive backup with manifest support
  - JSON manifest with checksums for all backed up files
  - Metadata tracking (date, file count, total size)
  - Restore functionality with verification
- **Progress Tracking**: Detailed progress reporting system
  - Real-time progress updates with percentage
  - Stage-based tracking (validation, backup, patching, verification)
  - Estimated time remaining calculations
- **Operation Reports**: Exportable operation reports
  - Text and JSON export formats
  - Comprehensive statistics (files patched, skipped, failed)
  - Success rate calculations
  - Detailed file lists for each operation result
- **Configuration Manager**: Persistent configuration system
  - Save/load user preferences
  - Default SDK version management
  - Auto-backup and auto-verify settings
  - Export/import configuration support

### Improved
- **Code Quality**: Major refactoring and cleanup
  - Removed dead code and commented-out sections
  - Improved variable naming conventions
  - Added XML documentation comments
  - Consistent code style throughout
- **Constants Management**: Centralized application constants
  - Version information
  - File extensions and names
  - Backup and temp folder naming
- **Global Variables**: Renamed for clarity and consistency
  - `mytwitter` → `AuthorTwitterUrl`
  - `selectedfolder` → `SelectedGameFolder`
  - `selfutilpath` → `SelfUtilPath`
  - `backupDir` → `BackupDirectory`
  - `skippedcount` → `SkippedFilesCount`
  - `patchedcount` → `PatchedFilesCount`
  - Added operation tracking with detailed lists
- **Error Handling**: Enhanced error messages and exception handling
  - Better error context and stack traces
  - User-friendly error messages
  - Graceful fallbacks for non-critical errors

### Fixed
- Improved temp folder creation with proper error handling
- Fixed backup folder naming consistency
- Enhanced null checking throughout codebase

### Technical Details
- Added 6 new service modules:
  - `InputValidator.vb`: Input validation and folder structure checks
  - `IntegrityVerifier.vb`: Post-patch verification and checksums
  - `BackupService.vb`: Enhanced backup with manifest support
  - `OperationReport.vb`: Report generation and export
  - `ProgressTracker.vb`: Progress tracking and estimation
  - `ConfigurationManager.vb`: User preferences and settings

## [1.0.0] - Initial Release

### Features
- PS5 ELF backporting functionality
- Automatic dependency download
- GUI-based workflow
- Basic backup system
- SDK version selection
- Credits and documentation

---

## Legend
- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Vulnerability fixes
