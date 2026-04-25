
# ⚠️ Project Discontinued

**PS5 BackPork Kitchen is no longer actively maintained.**

Due to internal circumstances, active development and support for PS5 BackPork Kitchen have officially ended.
We recommend users explore alternative backport tools that may be available in the community.

Thank you for your support and understanding.

---

# PS5-BACKPORK-KITCHEN

Simplify your PS5 homebrew workflow with **PS5 BackPork Kitchen** — a practical backporting utility that bridges the gap between modern game binaries and lower firmware versions.
This tool builds upon the existing PS5 backporting concept and scripts used in the community, providing a GUI-based and automated workflow for convenience.



## Features

- 📦 Automatic Backup System
- 🖥️ GUI-Based Workflow
- 🛡️ Safe & Reversible
- ⚡ Fast & Lightweight
- 📥 Automatic Dependency Download
- 🧩 One-Click Solution Setup

## Requirements

- Windows 10 / 11 (64-bit)
- .NET 10 Desktop Runtime (Windows)
- Internet connection (required on first run for automatic dependency download)
- Read/Write access to game and working directories


## How To Use

1. **Launch the Tool**
   - Open the PS5 Backport Tool.
   - On first launch, required dependencies will be downloaded automatically.

2. **Select Game Folder**
   - Click **Browse Folder**.
   - Select the PS5 homebrew / game folder (usually starting with `PPSA`).

3. **Automatic Detection**
   - The tool will automatically detect:
     - Game icon
     - Metadata (title, SDK, flags)
   - No manual configuration required.

4. **Start Backporting**
   - Click **Start Cooking**.
   - Sit back — the tool will handle patching, validation, and backups.

5. **Backup Created**
   - A backup of original libraries and files will be created
   - Backup is stored alongside the selected homebrew folder.

6. **Transfer to PS5**
   - Copy the patched game folder to your PS5 (USB / SSD / preferred method).

7. **Run the Game**
   - Launch `ps5-backpork.elf` from your PS5 homebrew environment.

8. **Profit 😎**

## Roadmap 
### Short-term (Stability & UX)
- 🧹 UI safety improvements (form load timing, grid stability)
- 🐛 Bug fixes discovered during real-world usage
- 📝 Improve inline comments and documentation
- 🎨 Minor UI/UX refinements (clarity over complexity)
### Mid-term (Workflow Enhancements)
- 🎮 **Game Library Manager** enhancements
  - Better filtering & search
  - Persistent notes / metadata
  - Improved statistics view
- 📦 Optional caching of detected game metadata
- 🧩 Modularization of features (enable/disable components)
### Long-term (Power Features)
- ~~🌐 **FTP integration with PS5**~~ ✅ *implemented*


  - Pull libraries directly from console
  - Push patched libraries back to console
  - Reduce manual copy steps
- 🗄️ Optional SQLite backend for game/library metadata
- ⚙️ Advanced diagnostics & validation tools
- 🔌 Plugin-style architecture for future extensions
## 🛠️ Advanced Operations (Already Implemented)

* 🔐 Decrypt → Patch → Re-sign workflow automation
* 🧬 Smart ELF / PRX patch detection (skip already-patched regions)
* ♻️ Safe retry logic with file-handle protection
* 🧾 Automatic backups before destructive operations
* 🚦 Conditional patching (optional / non-fatal patches)
* 🧠 Firmware-aware logic (FW-specific handling & validation)
* 📄 Detailed logging for debugging and verification
## 🧩 ELFInspector – TODO / Roadmap

### 🔍 Core Improvements


### 🖱️ Context Menu (Main Power Feature)

*(Right-click on selected ELF(s) in DGV)*
*Added on Version 2.0.0*
* [x] **Decrypt**
  * Decrypt selected ELF(s)
  * Skip already decrypted files
  * Show decrypt result per file

* [x] **Downgrade / Backport**
  * Downgrade selected ELF(s) to chosen firmware
  * Firmware-aware downgrade rules
  * Dry-run / analysis mode (no write)

* [x] **Patch**
  * Apply common patches (SDK check, FW check)
  * Optional patches (non-fatal)
  * Detect already patched ELFs

* [x] **Sign**
  * Sign selected ELF(s)
  * Auto-detect if signing is required
  * Prevent double-signing

* [x] **Full Pipeline**
  * Decrypt → Patch → Sign (one-click)
  * Per-file result summary
---
## Beta Status

⚠️ This project is currently in **beta**.

- Some games or ELFs may not be supported
- Unexpected behavior is possible
- Always keep backups of original files

Use at your own discretion.

## Disclaimer

This tool is intended for **educational, research, and homebrew development purposes only**.

The authors do not promote piracy or copyright infringement.
Users are responsible for how they use this software.

## License

This project is open-source.
Licensed under the MIT License.

## Credits

- [@BestPig ](https://github.com/BestPig/BackPork) - **PS5 BackPork**
- [@idlesauce ](https://gist.github.com/idlesauce/2ded24b7b5ff296f21792a8202542aaa) - **Downgrade Script**
- [@john-tornblom ](https://github.com/ps5-payload-dev/sdk/blob/master/samples/install_app/make_fself.py) - **make_fself.py**
- [@CyB1K](https://github.com/CyB1K/SelfUtil-Patched/releases/tag/1.2) - **SelfUtil**

## Inspiration & Credits

This project builds upon ideas, scripts, and research shared by the PS5 homebrew community.
Core concepts and low-level techniques are credited to their respective authors.

This tool focuses on automation, safety, and a user-friendly GUI.







