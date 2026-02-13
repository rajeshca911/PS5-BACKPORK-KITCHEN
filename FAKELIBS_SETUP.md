# Fakelibs Setup Guide

## Firmware Versions Supported

PS5 BACKPORK KITCHEN supports the following PS5 firmware versions:
- Firmware 1.x
- Firmware 2.x
- Firmware 3.x
- Firmware 4.x (included)
- Firmware 5.x (included)
- Firmware 6.x (included)
- Firmware 7.x (included)
- Firmware 8.x
- Firmware 9.x
- Firmware 10.x

## How to Add Missing Fakelibs

Fakelibs cannot be distributed directly due to legal restrictions. You must extract them from official PS5 firmware files.

### Directory Structure

Create numbered folders in the same directory as the application executable:

```
PS5 BACKPORK KITCHEN/
├── PS5 BACKPORK KITCHEN.exe
├── 1/          (Firmware 1.x fakelibs)
├── 2/          (Firmware 2.x fakelibs)
├── 3/          (Firmware 3.x fakelibs)
├── 4/          (Firmware 4.x fakelibs)
├── 5/          (Firmware 5.x fakelibs)
├── 6/          (Firmware 6.x fakelibs)
├── 7/          (Firmware 7.x fakelibs)
├── 8/          (Firmware 8.x fakelibs)
├── 9/          (Firmware 9.x fakelibs)
└── 10/         (Firmware 10.x fakelibs)
```

### What Are Fakelibs?

Fakelibs are modified system libraries (.prx and .sprx files) extracted from PS5 firmware that allow games to run on lower firmware versions through library replacement.

### How to Obtain Fakelibs

1. **Official Firmware Files**: Download official PS5 firmware from Sony's servers
2. **Extraction Tools**: Use PS5 firmware extraction tools to extract system libraries
3. **Patching**: Apply BPS patches to make libraries compatible with your target firmware

### Recommended Resources

- **BestPig/BackPork**: [https://github.com/BestPig/BackPork](https://github.com/BestPig/BackPork)
  - Provides BPS patches for converting firmware 10.01 libraries to firmware 7.61
  - Contains documentation on how BackPork library sideloading works

- **Nazky/Auto-Backpork**: [https://github.com/Nazky/Auto-Backpork](https://github.com/Nazky/Auto-Backpork)
  - Automated workflow for creating backported games
  - Includes scripts for fakelib management

### Supported Firmware Versions in the Wild

Based on PS5 homebrew community resources:
- **Firmware 3.00-7.61**: Most commonly used for homebrew/backporting
- **Firmware 8.00-10.01**: Newer firmwares with increasing support

### Important Notes

- Libraries must come from a firmware version compatible with the game
- Libraries must be modified to remove dependencies not available on your current firmware
- The application will detect missing fakelibs and display a warning message
- Place .prx and .sprx files directly in the numbered firmware folders

### Troubleshooting

If you see errors like:
- "Fakelibs not Found for FW:X" - The firmware folder X is empty or missing
- "Invalid FW value" - Check that SDK version is properly selected

Create the numbered folders even if empty - the application will recognize them as available slots.

### Legal Notice

This tool is for educational and research purposes only. Users are responsible for ensuring they have legal rights to any firmware files they extract and use.
