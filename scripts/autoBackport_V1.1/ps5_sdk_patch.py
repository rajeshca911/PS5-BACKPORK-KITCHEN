import os
import struct
import shutil
import argparse

PT_SCE_PROCPARAM = 0x61000001
PT_SCE_MODULE_PARAM = 0x61000002

SCE_PROCESS_PARAM_MAGIC = 0x4942524F
SCE_MODULE_PARAM_MAGIC = 0x3C13F4BF

SCE_PARAM_PS5_SDK_OFFSET = 0xC
SCE_PARAM_PS4_SDK_OFFSET = 0x8

PHT_OFFSET_OFFSET = 0x20
PHT_OFFSET_SIZE = 0x8
PHT_COUNT_OFFSET = 0x38
PHT_COUNT_SIZE = 0x2

PHDR_ENTRY_SIZE = 0x38
PHDR_TYPE_OFFSET = 0x0
PHDR_TYPE_SIZE = 0x4
PHDR_OFFSET_OFFSET = 0x8
PHDR_OFFSET_SIZE = 0x8

ELF_MAGIC = b'\x7FELF'
PS4_FSELF_MAGIC = b'\x4F\x15\x3D\x1D'
PS5_FSELF_MAGIC = b'\x54\x14\xF5\xEE'

executable_extensions = [".bin", ".elf", ".self", ".prx", ".sprx"]

# === PRESETS SDK PS5 ===
PRESET_VERSIONS = {
    "1.00": 0x01000050,
    "2.00": 0x02000009,
    "3.00": 0x03000027,
    "4.00": 0x04000031,
    "5.00": 0x05000033,
    "6.00": 0x06000038,
    "7.00": 0x07000038,
    "8.00": 0x08000041,
    "9.00": 0x09000040
}

def patch_file(file, ps5_sdk_version, ps4_version):
    file.seek(PHT_COUNT_OFFSET)
    pht_count_bytes = file.read(PHT_COUNT_SIZE)
    segment_count = struct.unpack('<H', pht_count_bytes)[0]

    file.seek(PHT_OFFSET_OFFSET)
    pht_offset_bytes = file.read(PHT_OFFSET_SIZE)
    pht_offset = struct.unpack('<Q', pht_offset_bytes)[0]

    for i in range(segment_count):
        file.seek(pht_offset + i * PHDR_ENTRY_SIZE)
        
        file.seek(pht_offset + i * PHDR_ENTRY_SIZE + PHDR_TYPE_OFFSET)
        segment_type_bytes = file.read(PHDR_TYPE_SIZE)
        segment_type = struct.unpack('<I', segment_type_bytes)[0]

        file.seek(pht_offset + i * PHDR_ENTRY_SIZE + PHDR_OFFSET_OFFSET)
        segment_offset_bytes = file.read(PHDR_OFFSET_SIZE)
        struct_start_offset = struct.unpack('<Q', segment_offset_bytes)[0]

        file.seek(struct_start_offset)
        t_param_magic = file.read(4)
        param_magic = struct.unpack('<I', t_param_magic)[0]

        if segment_type == PT_SCE_PROCPARAM:
            if param_magic != SCE_PROCESS_PARAM_MAGIC:
                struct_start_offset += 0x8
                file.seek(struct_start_offset)
                t_param_magic = file.read(4)
                param_magic = struct.unpack('<I', t_param_magic)[0]
                if param_magic != SCE_PROCESS_PARAM_MAGIC:
                    raise Exception("Invalid magic")
        elif segment_type == PT_SCE_MODULE_PARAM:
            if param_magic != SCE_MODULE_PARAM_MAGIC:
                struct_start_offset += 0x8
                file.seek(struct_start_offset)
                t_param_magic = file.read(4)
                param_magic = struct.unpack('<I', t_param_magic)[0]
                if param_magic != SCE_MODULE_PARAM_MAGIC:
                    print(f"[?] Invalid module param magic for file '{file.name}', skipping")
                    continue
        else:
            continue

        file.seek(struct_start_offset + SCE_PARAM_PS5_SDK_OFFSET)
        original_ps5_sdk_version = struct.unpack('<I', file.read(4))[0]
        file.seek(struct_start_offset + SCE_PARAM_PS5_SDK_OFFSET)
        file.write(struct.pack('<I', ps5_sdk_version))
        print(f"Patched PS5 SDK version from 0x{original_ps5_sdk_version:08X} to 0x{ps5_sdk_version:08X} for file '{file.name}'")
        
        file.seek(struct_start_offset + SCE_PARAM_PS4_SDK_OFFSET)
        original_ps4_sdk_version = struct.unpack('<I', file.read(4))[0]
        file.seek(struct_start_offset + SCE_PARAM_PS4_SDK_OFFSET)
        file.write(struct.pack('<I', ps4_version))
        print(f"Patched PS4 SDK version from 0x{original_ps4_sdk_version:08X} to 0x{ps4_version:08X} for file '{file.name}'")
        return True

    return False

def process_file(file_path, create_backup, ps5_sdk_version, ps4_version):
    with open(file_path, 'r+b') as file:
        file.seek(0)
        magic = file.read(4)
        if magic != ELF_MAGIC:
            if magic == PS4_FSELF_MAGIC or magic == PS5_FSELF_MAGIC:
                print(f"Aborting, file '{file_path}' is a signed file, this script expects unsigned ELF files")
                exit(1)
            return

        file.seek(0)

        if create_backup and not os.path.exists(file_path + ".bak"):
            shutil.copyfile(file_path, file_path + ".bak")
            print(f"Backup created for '{file_path}'")

        patched = patch_file(file, ps5_sdk_version, ps4_version)

        if patched:
            print(f"Patched '{file_path}'")
        else:
            print(f"Failed to patch '{file_path}'")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Patches the SDK version of PS5 ELF files using presets")
    parser.add_argument("input", help="Path to an ELF file or a folder (processed recursively)")
    parser.add_argument("--ps4_ver", required=True, help="PS4 SDK version to set (e.g. 0x09040001)", type=lambda x: int(x, 0))
    parser.add_argument("--ps5_preset", required=True, help="PS5 SDK preset version (e.g. 4.00)")
    parser.add_argument("--no-backup", action="store_true", help="Do not create .bak backup files")
    args = parser.parse_args()

    # === Récupérer la valeur hexadécimale du preset
    preset_key = args.ps5_preset.strip()
    if preset_key not in PRESET_VERSIONS:
        print(f"[Erreur] Preset SDK PS5 inconnu : '{preset_key}'")
        print(f"Versions disponibles : {', '.join(PRESET_VERSIONS.keys())}")
        exit(1)

    ps5_sdk_version = PRESET_VERSIONS[preset_key]
    ps4_version = args.ps4_ver
    input_path = args.input
    create_backup = not args.no_backup

    if os.path.isfile(input_path):
        process_file(input_path, create_backup, ps5_sdk_version, ps4_version)
    elif os.path.isdir(input_path):
        all_files = [
            os.path.join(dp, f)
            for dp, dn, filenames in os.walk(input_path)
            for f in filenames if os.path.splitext(f)[1] in executable_extensions
        ]

        for file_path in all_files:
            process_file(file_path, create_backup, ps5_sdk_version, ps4_version)
    else:
        print(f"Invalid input path: '{input_path}'")
