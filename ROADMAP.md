# 🗺️ PS5 BackPork Kitchen - Roadmap

This document outlines planned features and improvements for future releases.

---

## 🎯 v2.1 - Statistics & Analytics Overhaul

### Statistics Database Migration
**Goal**: Replace current statistics tracking with persistent SQLite database

**Benefits**:
- Persistent statistics across sessions
- Historical tracking of operations
- Better performance with large datasets
- Query capabilities for advanced analytics

**Implementation**:
- Create SQLite schema for tracking:
  - Game library operations (decrypt, patch, sign counts)
  - Firmware downloads and extractions
  - FTP transfers (upload/download volumes)
  - Success/failure rates per operation type
  - Session history with timestamps
- Migrate existing statistics logic to database queries
- Add dedicated Statistics Form with:
  - Visual charts (operation counts, success rates)
  - Historical trends (daily/weekly/monthly)
  - Export functionality (CSV, JSON)
  - Filter by date range, operation type, game

**Proposed Location**: New `StatisticsForm.vb` + `Services/StatisticsDatabase.vb`

---

## 🚀 v2.2 - Payload Management System

### FTP Payload Sender
**Goal**: Streamlined payload deployment to PS5 via FTP

**Features**:
- **Payload Library Manager**:
  - Browse and organize payload files (.bin, .elf)
  - Categorize by type (exploits, homebrew, tools)
  - Add descriptions and version info
  - Import from local folders or URLs

- **Quick Send Interface**:
  - One-click payload deployment to PS5
  - Support for common payload paths:
    - `/data/` (GoldHEN payloads)
    - `/system_ex/app/` (app injections)
    - Custom paths
  - Batch send multiple payloads
  - Auto-restart PS5 services if needed

- **Payload Profiles**:
  - Save payload sets (e.g., "Dev Setup", "Backup Tools")
  - Deploy entire profiles with one click
  - Version management for updated payloads

- **Protocol Support**:
  - FTP/FTPS (primary)
  - HTTP payload hosting (secondary)
  - Direct USB transfer (fallback)

**Proposed Location**: New `PayloadManagerForm.vb` + `Services/PayloadSender.vb`

---

## 🔮 v2.3 - Quality of Life Improvements

### UI/UX Enhancements
- [ ] Dark mode theme option
- [ ] Customizable keyboard shortcuts
- [ ] Drag & drop file support (drop ELF to process)
- [ ] Recent files/folders quick access
- [ ] Toast notifications for long operations

### Performance Optimizations
- [ ] Async/await all blocking operations
- [ ] Background task queue system
- [ ] Cache frequently accessed ELF metadata
- [ ] Parallel batch processing for multi-file operations

### Advanced ELF Operations
- [ ] ELF diff viewer (compare two ELF files)
- [ ] Batch rename utility for processed files
- [ ] Auto-organize output files by operation type
- [ ] ELF hex viewer/editor for manual patches

---

## 🛠️ v2.4 - Developer Tools

### Logging & Debugging
- [ ] Comprehensive logging system
- [ ] Log viewer with filtering
- [ ] Export logs for bug reports
- [ ] Verbose mode toggle

### Backup & Restore
- [ ] Project backup system (save entire game state)
- [ ] Restore from backup
- [ ] Cloud backup integration (optional)

### Plugin System (Experimental)
- [ ] Plugin architecture for community extensions
- [ ] Plugin manager UI
- [ ] Sample plugins (custom patchers, validators)

---

## 📋 Backlog (Unscheduled)

### Integration & Automation
- [ ] PS5 SDK version auto-detection from PUP files
- [ ] Game update checker (PSN API integration)
- [ ] Automated testing suite for operations
- [ ] CI/CD pipeline for releases

### Documentation
- [ ] Video tutorials for common workflows
- [ ] Wiki with troubleshooting guides
- [ ] API documentation for developers

### Community Features
- [ ] Share game library configurations
- [ ] Payload repository browser
- [ ] Built-in updater for app

---

## 🤝 Contributing

Have ideas for features? Open an issue or PR with:
- Clear description of the feature
- Use case / why it's useful
- (Optional) Implementation sketch

---

## 📝 Notes

**Priority**: v2.1 and v2.2 are highest priority based on maintainer feedback.

**Timeline**: Features are implemented as time permits. No strict deadlines.

**Feedback**: Community input shapes this roadmap. Suggest features via GitHub Issues!

---

Last Updated: 2026-01-30
Maintained by: rajeshca911 & DroneTechTI
