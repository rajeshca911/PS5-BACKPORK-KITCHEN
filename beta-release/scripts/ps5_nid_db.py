"""
PS5 NID Knowledge Base — Built-in database of known PS5 system functions.

Provides NID resolution, function classification, firmware availability checks,
and prefix-based heuristics without requiring firmware library dumps.

NID (Name Identifier) on PS5:
  nid = SHA1(symbol_name + ":")[0:8] big-endian -> 16-hex-char string

Usage:
  from ps5_nid_db import PS5NidDB
  db = PS5NidDB()
  name = db.resolve_nid("A4A8B1D0FBF1CA52")   # -> "sceKernelLoadStartModule"
  info = db.classify_function("sceNpTrophyUnlockTrophy")
  avail = db.is_function_available("sceAgcSubmitCommandBuffers", "6.00")
  missing = db.get_missing_for_fw(["sceNpAuthCreateAsyncRequest", ...], "6.00")
"""

import hashlib
from typing import Optional


# ---------------------------------------------------------------------------
# NID Computation
# ---------------------------------------------------------------------------

def calc_nid(symbol_name: str) -> str:
    """Compute the PS5 NID for a symbol name.
    NID = SHA1(name + ':')[0:8] big-endian -> 16-hex-char string.
    """
    digest = hashlib.sha1((symbol_name + ":").encode()).digest()
    return digest[:8].hex().upper()


# ---------------------------------------------------------------------------
# Stub Risk Levels
# ---------------------------------------------------------------------------

RISK_SAFE     = "safe"       # Stubbing is safe, no gameplay impact
RISK_LOW      = "low"        # Minimal risk, minor features may break
RISK_MEDIUM   = "medium"     # Some functionality affected
RISK_HIGH     = "high"       # Important functionality, may crash
RISK_CRITICAL = "critical"   # Never stub — GPU, kernel, memory management

# ---------------------------------------------------------------------------
# Stub Modes
# ---------------------------------------------------------------------------

STUB_NOP       = "nop"        # NOP sled (do nothing)
STUB_RET_ZERO  = "ret_zero"   # Return 0 / SCE_OK
STUB_RET_ERROR = "ret_error"  # Return -1 / error code
STUB_SKIP      = "skip"       # Do NOT stub (critical function)

# ---------------------------------------------------------------------------
# Function Categories
# ---------------------------------------------------------------------------

CAT_KERNEL     = "kernel"
CAT_MEMORY     = "memory"
CAT_THREAD     = "thread"
CAT_FS         = "filesystem"
CAT_GPU        = "gpu"
CAT_AUDIO      = "audio"
CAT_VIDEO      = "video"
CAT_NETWORK    = "network"
CAT_NP         = "np_platform"    # PlayStation Network
CAT_TROPHY     = "trophy"
CAT_SAVEDATA   = "savedata"
CAT_PAD        = "controller"
CAT_SYSTEM     = "system"
CAT_IME        = "ime"           # Input Method Editor
CAT_DIALOG     = "dialog"
CAT_HTTP       = "http"
CAT_SSL        = "ssl"
CAT_FIBER      = "fiber"
CAT_MISC       = "misc"

# ---------------------------------------------------------------------------
# Known PS5 Functions Database
# ---------------------------------------------------------------------------
# Format: (function_name, library, category, min_fw, risk, stub_mode)
# min_fw: earliest firmware where the function exists
#   "1.00" = always existed, higher values = added later

_KNOWN_FUNCTIONS = [
    # ===== libkernel =====
    # Core kernel — mostly available from FW 1.00
    ("sceKernelLoadStartModule",         "libkernel.sprx",  CAT_KERNEL,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelStopUnloadModule",        "libkernel.sprx",  CAT_KERNEL,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelDlsym",                   "libkernel.sprx",  CAT_KERNEL,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelJitCreateSharedMemory",   "libkernel.sprx",  CAT_KERNEL,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelJitCreateAliasOfSharedMemory", "libkernel.sprx", CAT_KERNEL, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelJitMapSharedMemory",      "libkernel.sprx",  CAT_KERNEL,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMmap",                    "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMunmap",                  "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMprotect",               "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMapDirectMemory",         "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMapFlexibleMemory",       "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelAllocateDirectMemory",    "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelReleaseDirectMemory",     "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelGetDirectMemorySize",     "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelAvailableDirectMemorySize", "libkernel.sprx", CAT_MEMORY, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelCheckedReleaseDirectMemory", "libkernel.sprx", CAT_MEMORY, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMapNamedDirectMemory",    "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMapNamedFlexibleMemory",  "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelQueryMemoryProtection",   "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelVirtualQuery",            "libkernel.sprx",  CAT_MEMORY,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelBatchMap",                "libkernel.sprx",  CAT_MEMORY,  "3.00", RISK_HIGH,     STUB_SKIP),
    ("sceKernelBatchMap2",               "libkernel.sprx",  CAT_MEMORY,  "5.00", RISK_HIGH,     STUB_SKIP),
    # Threading
    ("sceKernelCreateEqueue",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelDeleteEqueue",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelWaitEqueue",              "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelAddReadEvent",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceKernelAddUserEvent",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceKernelAddUserEventEdge",        "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadCreate",                 "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadJoin",                   "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadMutexInit",              "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadMutexLock",              "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadMutexUnlock",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadMutexDestroy",           "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePthreadCondInit",               "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadCondWait",               "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePthreadCondSignal",             "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadCondBroadcast",          "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadCondDestroy",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePthreadRwlockInit",             "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadRwlockRdlock",           "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadRwlockWrlock",           "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadRwlockUnlock",           "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePthreadSelf",                   "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePthreadSetaffinity",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePthreadGetaffinity",            "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelSleep",                   "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelUsleep",                  "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelNanosleep",               "libkernel.sprx",  CAT_THREAD,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelClockGettime",            "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelGettimeofday",            "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelGetCpuTemperature",       "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelGetCpuFrequency",         "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelGetProcessTime",          "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelGetProcessTimeCounter",   "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelGetProcessTimeCounterFrequency", "libkernel.sprx", CAT_SYSTEM, "1.00", RISK_SAFE, STUB_RET_ZERO),
    ("sceKernelIsNeoMode",               "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceKernelGetCompiledSdkVersion",   "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelGetFsSandboxRandomWord",  "libkernel.sprx",  CAT_FS,      "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceKernelDebugOutText",            "libkernel.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    # Filesystem
    ("sceKernelOpen",                    "libkernel.sprx",  CAT_FS,      "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelClose",                   "libkernel.sprx",  CAT_FS,      "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelRead",                    "libkernel.sprx",  CAT_FS,      "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelWrite",                   "libkernel.sprx",  CAT_FS,      "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelLseek",                   "libkernel.sprx",  CAT_FS,      "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceKernelFstat",                   "libkernel.sprx",  CAT_FS,      "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceKernelStat",                    "libkernel.sprx",  CAT_FS,      "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceKernelMkdir",                   "libkernel.sprx",  CAT_FS,      "1.00", RISK_HIGH,     STUB_RET_ZERO),
    ("sceKernelRename",                  "libkernel.sprx",  CAT_FS,      "1.00", RISK_HIGH,     STUB_RET_ZERO),
    ("sceKernelUnlink",                  "libkernel.sprx",  CAT_FS,      "1.00", RISK_HIGH,     STUB_RET_ZERO),

    # ===== libSceAgc (AMD GPU Command — PS5 specific) =====
    # AGC was introduced at PS5 launch but the API evolved significantly
    ("sceAgcInitialize",                 "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcFinalize",                   "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceAgcSubmitCommandBuffers",       "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSubmitAndFlipCommandBuffers", "libSceAgc.sprx", CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcGetShaderBinaryInfo",        "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcGetLastError",               "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceAgcGetGpuTimestamp",            "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceAgcResetDefaultRenderState",    "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceAgcSetVsShader",                "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSetPsShader",                "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSetCsShader",                "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDrawIndex",                  "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDrawIndexAuto",              "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDispatch",                   "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDingDong",                   "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_CRITICAL, STUB_SKIP),
    # Newer AGC functions added in later firmwares
    ("sceAgcSubmitAsc",                  "libSceAgc.sprx",  CAT_GPU,     "4.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSubmitHighPriorityAndFlipCommandBuffers", "libSceAgc.sprx", CAT_GPU, "5.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcQueryPerformanceData",       "libSceAgc.sprx",  CAT_GPU,     "4.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceAgcSetPredication",             "libSceAgc.sprx",  CAT_GPU,     "7.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcReleaseResource",            "libSceAgc.sprx",  CAT_GPU,     "1.00", RISK_HIGH,     STUB_RET_ZERO),

    # ===== libSceAgcDriver =====
    ("sceAgcDriverInitialize",           "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDriverFinalize",             "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceAgcDriverGetGpuClock",          "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceAgcDriverGetMemoryClock",       "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceAgcDriverSubmitDone",           "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDriverRegisterResource",     "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDriverUnregisterResource",   "libSceAgcDriver.sprx", CAT_GPU, "1.00", RISK_HIGH,     STUB_RET_ZERO),

    # ===== libSceGnmDriver (GNM is PS4 GPU API, available on PS5 for compat) =====
    ("sceGnmSubmitCommandBuffers",       "libSceGnmDriver.sprx", CAT_GPU, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceGnmSubmitAndFlipCommandBuffers", "libSceGnmDriver.sprx", CAT_GPU, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceGnmSubmitDone",                 "libSceGnmDriver.sprx", CAT_GPU, "1.00", RISK_CRITICAL, STUB_SKIP),

    # ===== libSceVideoOut =====
    ("sceVideoOutOpen",                  "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutClose",                 "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceVideoOutSetFlipRate",           "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_HIGH,     STUB_RET_ZERO),
    ("sceVideoOutSetBufferAttribute",    "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutRegisterBuffers",       "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutSubmitFlip",            "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutGetFlipStatus",         "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceVideoOutGetResolutionStatus",   "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceVideoOutGetVblankStatus",       "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceVideoOutAddFlipEvent",          "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceVideoOutSetWindowModeMargins",  "libSceVideoOut.sprx", CAT_VIDEO, "7.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceVideoOutConfigureOutputMode",   "libSceVideoOut.sprx", CAT_VIDEO, "4.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceVideoOutWaitVblank",            "libSceVideoOut.sprx", CAT_VIDEO, "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceVideoOutColorSettingsSetGamma", "libSceVideoOut.sprx", CAT_VIDEO, "4.50", RISK_LOW,      STUB_RET_ZERO),
    ("sceVideoOutGetDeviceCapabilityInfo", "libSceVideoOut.sprx", CAT_VIDEO, "4.00", RISK_LOW,    STUB_RET_ZERO),

    # ===== libSceAudioOut =====
    ("sceAudioOutInit",                  "libSceAudioOut.sprx", CAT_AUDIO, "1.00", RISK_HIGH,     STUB_RET_ZERO),
    ("sceAudioOutOpen",                  "libSceAudioOut.sprx", CAT_AUDIO, "1.00", RISK_HIGH,     STUB_SKIP),
    ("sceAudioOutClose",                 "libSceAudioOut.sprx", CAT_AUDIO, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceAudioOutOutput",               "libSceAudioOut.sprx", CAT_AUDIO, "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceAudioOutSetVolume",             "libSceAudioOut.sprx", CAT_AUDIO, "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceAudioOutGetPortState",          "libSceAudioOut.sprx", CAT_AUDIO, "1.00", RISK_SAFE,     STUB_RET_ZERO),

    # ===== libSceNpAuth =====
    ("sceNpAuthCreateAsyncRequest",      "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNpAuthDeleteAsyncRequest",      "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpAuthAbortAsyncRequest",       "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpAuthPollAsyncResult",         "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpAuthWaitAsyncResult",         "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpAuthGetAuthorizationCode",    "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpAuthGetIdToken",              "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpAuthCreateRequest",           "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNpAuthDeleteRequest",           "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpAuthGetAuthorizationCodeV3",  "libSceNpAuth.sprx", CAT_NP,     "4.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpAuthGetIdTokenV3",            "libSceNpAuth.sprx", CAT_NP,     "4.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpAuthPollPlusToken",           "libSceNpAuth.sprx", CAT_NP,     "1.00", RISK_SAFE,     STUB_RET_ZERO),

    # ===== libSceNpTrophy =====
    ("sceNpTrophyCreateContext",         "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyDestroyContext",        "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyRegisterContext",       "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyUnlockTrophy",          "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyGetTrophyUnlockState",  "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyGetTrophyInfo",         "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyGetGameInfo",           "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyGetGameIcon",           "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyGetTrophyIcon",         "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyCaptureScreenshot",     "libSceNpTrophy.sprx", CAT_TROPHY, "3.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpTrophyShowTrophyList",        "libSceNpTrophy.sprx", CAT_TROPHY, "1.00", RISK_SAFE,    STUB_RET_ZERO),

    # ===== libSceSaveData =====
    ("sceSaveDataInitialize3",           "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_MEDIUM, STUB_RET_ZERO),
    ("sceSaveDataTerminate",             "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceSaveDataMount",                 "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_MEDIUM, STUB_RET_ERROR),
    ("sceSaveDataMount2",                "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_MEDIUM, STUB_RET_ERROR),
    ("sceSaveDataUmount",                "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceSaveDataDelete",                "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_MEDIUM, STUB_RET_ZERO),
    ("sceSaveDataDirNameSearch",         "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_MEDIUM, STUB_RET_ZERO),
    ("sceSaveDataSaveIcon",              "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceSaveDataSetParam",              "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceSaveDataGetParam",              "libSceSaveData.sprx", CAT_SAVEDATA, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceSaveDataSyncSaveDataMemory",    "libSceSaveData.sprx", CAT_SAVEDATA, "2.00", RISK_MEDIUM, STUB_RET_ZERO),
    ("sceSaveDataTransferringMount",     "libSceSaveData.sprx", CAT_SAVEDATA, "4.00", RISK_MEDIUM, STUB_RET_ERROR),
    ("sceSaveDataCheckBackupData",       "libSceSaveData.sprx", CAT_SAVEDATA, "3.00", RISK_LOW,    STUB_RET_ERROR),
    ("sceSaveDataBackup",                "libSceSaveData.sprx", CAT_SAVEDATA, "3.00", RISK_LOW,    STUB_RET_ZERO),

    # ===== libScePad (Controller) =====
    ("scePadInit",                       "libScePad.sprx",   CAT_PAD,     "1.00", RISK_HIGH,     STUB_RET_ZERO),
    ("scePadOpen",                       "libScePad.sprx",   CAT_PAD,     "1.00", RISK_HIGH,     STUB_SKIP),
    ("scePadClose",                      "libScePad.sprx",   CAT_PAD,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePadRead",                       "libScePad.sprx",   CAT_PAD,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePadReadState",                  "libScePad.sprx",   CAT_PAD,     "1.00", RISK_CRITICAL, STUB_SKIP),
    ("scePadGetHandle",                  "libScePad.sprx",   CAT_PAD,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePadSetVibration",               "libScePad.sprx",   CAT_PAD,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("scePadSetLightBar",                "libScePad.sprx",   CAT_PAD,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("scePadResetLightBar",              "libScePad.sprx",   CAT_PAD,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("scePadSetMotionSensorState",       "libScePad.sprx",   CAT_PAD,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("scePadGetControllerInformation",   "libScePad.sprx",   CAT_PAD,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePadSetTriggerEffect",           "libScePad.sprx",   CAT_PAD,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("scePadGetConnectionCount",         "libScePad.sprx",   CAT_PAD,     "4.00", RISK_SAFE,     STUB_RET_ZERO),

    # ===== libSceUserService =====
    ("sceUserServiceInitialize",         "libSceUserService.sprx", CAT_SYSTEM, "1.00", RISK_HIGH, STUB_RET_ZERO),
    ("sceUserServiceTerminate",          "libSceUserService.sprx", CAT_SYSTEM, "1.00", RISK_LOW,  STUB_RET_ZERO),
    ("sceUserServiceGetInitialUser",     "libSceUserService.sprx", CAT_SYSTEM, "1.00", RISK_LOW,  STUB_RET_ZERO),
    ("sceUserServiceGetLoginUserIdList", "libSceUserService.sprx", CAT_SYSTEM, "1.00", RISK_LOW,  STUB_RET_ZERO),
    ("sceUserServiceGetUserName",        "libSceUserService.sprx", CAT_SYSTEM, "1.00", RISK_LOW,  STUB_RET_ZERO),

    # ===== libSceSystemService =====
    ("sceSystemServiceHideSplashScreen", "libSceSystemService.sprx", CAT_SYSTEM, "1.00", RISK_SAFE, STUB_RET_ZERO),
    ("sceSystemServiceLoadExec",         "libSceSystemService.sprx", CAT_SYSTEM, "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceSystemServiceParamGetInt",      "libSceSystemService.sprx", CAT_SYSTEM, "1.00", RISK_LOW,  STUB_RET_ZERO),
    ("sceSystemServiceParamGetString",   "libSceSystemService.sprx", CAT_SYSTEM, "1.00", RISK_LOW,  STUB_RET_ZERO),
    ("sceSystemServiceGetDisplaySafeAreaInfo", "libSceSystemService.sprx", CAT_SYSTEM, "1.00", RISK_LOW, STUB_RET_ZERO),
    ("sceSystemServiceReceiveEvent",     "libSceSystemService.sprx", CAT_SYSTEM, "1.00", RISK_MEDIUM, STUB_RET_ZERO),

    # ===== libSceNet / libSceHttp / libSceSsl =====
    ("sceNetInit",                       "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceNetTerm",                       "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNetPoolCreate",                 "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceNetPoolDestroy",                "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNetSocket",                     "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_HIGH,     STUB_RET_ERROR),
    ("sceNetConnect",                    "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_HIGH,     STUB_RET_ERROR),
    ("sceNetSend",                       "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_HIGH,     STUB_RET_ERROR),
    ("sceNetRecv",                       "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_HIGH,     STUB_RET_ERROR),
    ("sceNetSocketClose",                "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNetGetMacAddress",              "libSceNet.sprx",   CAT_NETWORK, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceHttpInit",                      "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceHttpTerm",                      "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceHttpCreateConnectionWithURL",   "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_MEDIUM,   STUB_RET_ERROR),
    ("sceHttpCreateRequestWithURL",      "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_MEDIUM,   STUB_RET_ERROR),
    ("sceHttpSendRequest",               "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_MEDIUM,   STUB_RET_ERROR),
    ("sceHttpGetResponseContentLength",  "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceHttpReadData",                  "libSceHttp.sprx",  CAT_HTTP,    "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceSslInit",                       "libSceSsl.sprx",   CAT_SSL,     "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceSslTerm",                       "libSceSsl.sprx",   CAT_SSL,     "1.00", RISK_LOW,      STUB_RET_ZERO),

    # ===== libSceNpManager =====
    ("sceNpCheckCallback",               "libSceNpManager.sprx", CAT_NP,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpRegisterStateCallback",       "libSceNpManager.sprx", CAT_NP,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpUnregisterStateCallback",     "libSceNpManager.sprx", CAT_NP,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpGetState",                    "libSceNpManager.sprx", CAT_NP,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpSetNpTitleId",                "libSceNpManager.sprx", CAT_NP,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNpGetNpId",                     "libSceNpManager.sprx", CAT_NP,  "1.00", RISK_LOW,      STUB_RET_ZERO),

    # ===== libSceFiber =====
    ("sceFiberInitialize",               "libSceFiber.sprx", CAT_FIBER,   "1.00", RISK_HIGH,     STUB_RET_ZERO),
    ("sceFiberFinalize",                 "libSceFiber.sprx", CAT_FIBER,   "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceFiberRun",                      "libSceFiber.sprx", CAT_FIBER,   "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceFiberSwitch",                   "libSceFiber.sprx", CAT_FIBER,   "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceFiberReturnToThread",           "libSceFiber.sprx", CAT_FIBER,   "1.00", RISK_CRITICAL, STUB_SKIP),
    ("sceFiberGetSelf",                  "libSceFiber.sprx", CAT_FIBER,   "1.00", RISK_LOW,      STUB_RET_ZERO),

    # ===== libSceIme (Input Method) =====
    ("sceImeOpen",                       "libSceIme.sprx",   CAT_IME,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceImeClose",                      "libSceIme.sprx",   CAT_IME,     "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceImeUpdate",                     "libSceIme.sprx",   CAT_IME,     "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceImeDialogInit",                 "libSceIme.sprx",   CAT_DIALOG,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceImeDialogTerm",                 "libSceIme.sprx",   CAT_DIALOG,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceImeDialogGetStatus",            "libSceIme.sprx",   CAT_DIALOG,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceImeDialogGetResult",            "libSceIme.sprx",   CAT_DIALOG,  "1.00", RISK_LOW,      STUB_RET_ZERO),

    # ===== libSceMsgDialog =====
    ("sceMsgDialogInitialize",           "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_LOW,   STUB_RET_ZERO),
    ("sceMsgDialogTerminate",            "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_SAFE,  STUB_RET_ZERO),
    ("sceMsgDialogOpen",                 "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_LOW,   STUB_RET_ZERO),
    ("sceMsgDialogClose",                "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_SAFE,  STUB_RET_ZERO),
    ("sceMsgDialogUpdateStatus",         "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_SAFE,  STUB_RET_ZERO),
    ("sceMsgDialogGetResult",            "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_LOW,   STUB_RET_ZERO),
    ("sceMsgDialogProgressBarSetValue",  "libSceMsgDialog.sprx", CAT_DIALOG, "1.00", RISK_SAFE,  STUB_RET_ZERO),

    # ===== libSceCommonDialog =====
    ("sceCommonDialogInitialize",        "libSceCommonDialog.sprx", CAT_DIALOG, "1.00", RISK_LOW, STUB_RET_ZERO),
    ("sceCommonDialogIsUsed",            "libSceCommonDialog.sprx", CAT_DIALOG, "1.00", RISK_SAFE, STUB_RET_ZERO),

    # ===== libSceNpWebApi =====
    ("sceNpWebApiInitialize",            "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNpWebApiTerminate",             "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpWebApiCreateContext",         "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpWebApiDeleteContext",         "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpWebApiSendRequest",           "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpWebApiCreateRequest",         "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_LOW,      STUB_RET_ERROR),
    ("sceNpWebApiDeleteRequest",         "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpWebApiReadData",              "libSceNpWebApi.sprx", CAT_NP,   "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNpWebApiCreatePushEventFilter", "libSceNpWebApi.sprx", CAT_NP,   "4.00", RISK_LOW,      STUB_RET_ERROR),

    # ===== libSceNpCommerce =====
    ("sceNpCommerceDialogInitialize",    "libSceNpCommerce.sprx", CAT_NP,  "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpCommerceDialogTerminate",     "libSceNpCommerce.sprx", CAT_NP,  "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpCommerceDialogOpen",          "libSceNpCommerce.sprx", CAT_NP,  "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpCommerceDialogGetResult",     "libSceNpCommerce.sprx", CAT_NP,  "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpCommerceDialogUpdateStatus",  "libSceNpCommerce.sprx", CAT_NP,  "1.00", RISK_SAFE,    STUB_RET_ZERO),

    # ===== libSceNpSignaling =====
    ("sceNpSignalingInitialize",         "libSceNpSignaling.sprx", CAT_NP, "1.00", RISK_LOW,     STUB_RET_ZERO),
    ("sceNpSignalingTerminate",          "libSceNpSignaling.sprx", CAT_NP, "1.00", RISK_SAFE,    STUB_RET_ZERO),

    # ===== libSceNpMatching2 =====
    ("sceNpMatching2Initialize",         "libSceNpMatching2.sprx", CAT_NP, "1.00", RISK_LOW,     STUB_RET_ZERO),
    ("sceNpMatching2Terminate",          "libSceNpMatching2.sprx", CAT_NP, "1.00", RISK_SAFE,    STUB_RET_ZERO),
    ("sceNpMatching2CreateContext",       "libSceNpMatching2.sprx", CAT_NP, "1.00", RISK_LOW,    STUB_RET_ERROR),
    ("sceNpMatching2DestroyContext",      "libSceNpMatching2.sprx", CAT_NP, "1.00", RISK_SAFE,   STUB_RET_ZERO),

    # ===== libSceAppContent =====
    ("sceAppContentInitialize",          "libSceAppContent.sprx", CAT_SYSTEM, "1.00", RISK_MEDIUM, STUB_RET_ZERO),
    ("sceAppContentAddcontMount",        "libSceAppContent.sprx", CAT_SYSTEM, "1.00", RISK_MEDIUM, STUB_RET_ERROR),
    ("sceAppContentAddcontUnmount",      "libSceAppContent.sprx", CAT_SYSTEM, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceAppContentGetAddcontInfo",      "libSceAppContent.sprx", CAT_SYSTEM, "1.00", RISK_LOW,    STUB_RET_ZERO),
    ("sceAppContentAddcontEnqueueDownload", "libSceAppContent.sprx", CAT_SYSTEM, "1.00", RISK_LOW, STUB_RET_ERROR),

    # ===== Newer FW functions (introduced in 4.x+) =====
    ("sceNpAuthCreateAsyncRequestWithServiceLabel", "libSceNpAuth.sprx", CAT_NP, "4.00", RISK_LOW, STUB_RET_ZERO),
    ("sceSaveDataMount5",                "libSceSaveData.sprx", CAT_SAVEDATA, "5.00", RISK_MEDIUM, STUB_RET_ERROR),
    ("sceVideoOutSubmitEopFlip",         "libSceVideoOut.sprx", CAT_VIDEO, "7.00", RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutSetBufferAttribute2",   "libSceVideoOut.sprx", CAT_VIDEO, "8.00", RISK_CRITICAL, STUB_SKIP),
    ("scePadGetCapability",              "libScePad.sprx",   CAT_PAD,     "5.00", RISK_SAFE,     STUB_RET_ZERO),
    ("scePadSetForceIntercedeMode",      "libScePad.sprx",   CAT_PAD,     "7.00", RISK_SAFE,     STUB_RET_ZERO),

    # ===== libSceRtc =====
    ("sceRtcGetCurrentTick",             "libSceRtc.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceRtcGetTick",                    "libSceRtc.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceRtcSetTick",                    "libSceRtc.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceRtcGetCurrentClock",            "libSceRtc.sprx",  CAT_SYSTEM,  "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceRtcConvertUtcToLocalTime",      "libSceRtc.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),
    ("sceRtcConvertLocalTimeToUtc",      "libSceRtc.sprx",  CAT_SYSTEM,  "1.00", RISK_SAFE,     STUB_RET_ZERO),

    # ===== libScePlayGo =====
    ("scePlayGoInitialize",              "libScePlayGo.sprx", CAT_SYSTEM, "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("scePlayGoTerminate",               "libScePlayGo.sprx", CAT_SYSTEM, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePlayGoOpen",                    "libScePlayGo.sprx", CAT_SYSTEM, "1.00", RISK_MEDIUM,   STUB_RET_ZERO),
    ("scePlayGoClose",                   "libScePlayGo.sprx", CAT_SYSTEM, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePlayGoGetProgress",             "libScePlayGo.sprx", CAT_SYSTEM, "1.00", RISK_LOW,      STUB_RET_ZERO),
    ("scePlayGoGetChunkProgress",        "libScePlayGo.sprx", CAT_SYSTEM, "1.00", RISK_LOW,      STUB_RET_ZERO),

    # ===== libSceSharePlay / libSceScreenShot =====
    ("sceScreenShotSetParam",            "libSceScreenShot.sprx", CAT_SYSTEM, "1.00", RISK_SAFE, STUB_RET_ZERO),
    ("sceScreenShotDisable",             "libSceScreenShot.sprx", CAT_SYSTEM, "1.00", RISK_SAFE, STUB_RET_ZERO),
    ("sceScreenShotEnable",              "libSceScreenShot.sprx", CAT_SYSTEM, "1.00", RISK_SAFE, STUB_RET_ZERO),

    # ===== Functions added in specific firmware versions =====
    # FW 7.xx additions
    ("sceAgcDriverQueryCapabilities",    "libSceAgcDriver.sprx", CAT_GPU, "7.00", RISK_LOW,      STUB_RET_ZERO),
    ("sceNpAuthGetAuthorizationCodeAsync", "libSceNpAuth.sprx", CAT_NP,  "7.00", RISK_LOW,      STUB_RET_ERROR),
    # FW 8.xx additions
    ("sceNpAuthCreateOauthRequest",      "libSceNpAuth.sprx", CAT_NP,    "8.00", RISK_LOW,      STUB_RET_ERROR),
    # FW 9.xx additions
    ("sceAgcSetGraphicsShader",          "libSceAgc.sprx",  CAT_GPU,     "9.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSetMeshShader",              "libSceAgc.sprx",  CAT_GPU,     "9.00", RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDrawMeshTasksAuto",          "libSceAgc.sprx",  CAT_GPU,     "9.00", RISK_CRITICAL, STUB_SKIP),
    ("sceSaveDataMount6",                "libSceSaveData.sprx", CAT_SAVEDATA, "9.00", RISK_MEDIUM, STUB_RET_ERROR),
    # FW 10.xx additions
    ("sceAgcSubmitCommandBuffersAndFlip2", "libSceAgc.sprx", CAT_GPU,    "10.00", RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutSubmitFlip2",           "libSceVideoOut.sprx", CAT_VIDEO, "10.00", RISK_CRITICAL, STUB_SKIP),
    ("sceNpWebApiCreateMultipartRequest", "libSceNpWebApi.sprx", CAT_NP, "10.00", RISK_LOW,     STUB_RET_ERROR),
]


# ---------------------------------------------------------------------------
# Prefix-based heuristics for unknown functions
# ---------------------------------------------------------------------------
# (prefix, category, risk, stub_mode)
# Checked in order — first match wins

_PREFIX_HEURISTICS = [
    # CRITICAL — never stub
    ("sceKernelLoad",                    CAT_KERNEL,   RISK_CRITICAL, STUB_SKIP),
    ("sceKernelDlsym",                   CAT_KERNEL,   RISK_CRITICAL, STUB_SKIP),
    ("sceKernelJit",                     CAT_KERNEL,   RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMmap",                    CAT_MEMORY,   RISK_CRITICAL, STUB_SKIP),
    ("sceKernelMapDirect",               CAT_MEMORY,   RISK_CRITICAL, STUB_SKIP),
    ("sceKernelAllocate",                CAT_MEMORY,   RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSubmit",                     CAT_GPU,      RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDraw",                       CAT_GPU,      RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDispatch",                   CAT_GPU,      RISK_CRITICAL, STUB_SKIP),
    ("sceAgcSet",                        CAT_GPU,      RISK_CRITICAL, STUB_SKIP),
    ("sceAgcDingDong",                   CAT_GPU,      RISK_CRITICAL, STUB_SKIP),
    ("sceGnmSubmit",                     CAT_GPU,      RISK_CRITICAL, STUB_SKIP),
    ("sceFiberRun",                      CAT_FIBER,    RISK_CRITICAL, STUB_SKIP),
    ("sceFiberSwitch",                   CAT_FIBER,    RISK_CRITICAL, STUB_SKIP),
    ("scePadRead",                       CAT_PAD,      RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutRegister",              CAT_VIDEO,    RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutSubmit",                CAT_VIDEO,    RISK_CRITICAL, STUB_SKIP),
    ("sceVideoOutOpen",                  CAT_VIDEO,    RISK_CRITICAL, STUB_SKIP),
    ("sceKernelOpen",                    CAT_FS,       RISK_CRITICAL, STUB_SKIP),
    ("sceKernelRead",                    CAT_FS,       RISK_CRITICAL, STUB_SKIP),
    ("sceKernelWrite",                   CAT_FS,       RISK_CRITICAL, STUB_SKIP),
    ("sceKernelCreate",                  CAT_KERNEL,   RISK_HIGH,     STUB_SKIP),
    ("scePthreadCreate",                 CAT_THREAD,   RISK_CRITICAL, STUB_SKIP),
    ("scePthreadMutex",                  CAT_THREAD,   RISK_HIGH,     STUB_SKIP),
    ("scePthreadCond",                   CAT_THREAD,   RISK_HIGH,     STUB_SKIP),

    # SAFE — stub freely
    ("sceNpTrophy",                      CAT_TROPHY,   RISK_SAFE,     STUB_RET_ZERO),
    ("sceScreenShot",                    CAT_SYSTEM,   RISK_SAFE,     STUB_RET_ZERO),
    ("sceNpCommerce",                    CAT_NP,       RISK_SAFE,     STUB_RET_ZERO),
    ("sceMsgDialog",                     CAT_DIALOG,   RISK_SAFE,     STUB_RET_ZERO),
    ("sceImeDialog",                     CAT_DIALOG,   RISK_SAFE,     STUB_RET_ZERO),

    # LOW risk
    ("sceNpAuth",                        CAT_NP,       RISK_LOW,      STUB_RET_ZERO),
    ("sceNpManager",                     CAT_NP,       RISK_LOW,      STUB_RET_ZERO),
    ("sceNpWebApi",                      CAT_NP,       RISK_LOW,      STUB_RET_ERROR),
    ("sceNpMatching",                    CAT_NP,       RISK_LOW,      STUB_RET_ZERO),
    ("sceNpSignaling",                   CAT_NP,       RISK_LOW,      STUB_RET_ZERO),
    ("sceNp",                            CAT_NP,       RISK_LOW,      STUB_RET_ZERO),
    ("sceUserService",                   CAT_SYSTEM,   RISK_LOW,      STUB_RET_ZERO),
    ("sceRtc",                           CAT_SYSTEM,   RISK_LOW,      STUB_RET_ZERO),
    ("scePlayGo",                        CAT_SYSTEM,   RISK_LOW,      STUB_RET_ZERO),
    ("sceCommonDialog",                  CAT_DIALOG,   RISK_LOW,      STUB_RET_ZERO),

    # MEDIUM risk
    ("sceSaveData",                      CAT_SAVEDATA, RISK_MEDIUM,   STUB_RET_ERROR),
    ("sceHttp",                          CAT_HTTP,     RISK_MEDIUM,   STUB_RET_ERROR),
    ("sceSsl",                           CAT_SSL,      RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceNet",                           CAT_NETWORK,  RISK_MEDIUM,   STUB_RET_ERROR),
    ("scePad",                           CAT_PAD,      RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceAudioOut",                      CAT_AUDIO,    RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceAppContent",                    CAT_SYSTEM,   RISK_MEDIUM,   STUB_RET_ZERO),

    # HIGH risk
    ("sceAgcDriver",                     CAT_GPU,      RISK_HIGH,     STUB_SKIP),
    ("sceAgc",                           CAT_GPU,      RISK_HIGH,     STUB_SKIP),
    ("sceGnm",                           CAT_GPU,      RISK_HIGH,     STUB_SKIP),
    ("sceVideoOut",                      CAT_VIDEO,    RISK_HIGH,     STUB_RET_ZERO),
    ("sceFiber",                         CAT_FIBER,    RISK_HIGH,     STUB_SKIP),
    ("sceSystemService",                 CAT_SYSTEM,   RISK_MEDIUM,   STUB_RET_ZERO),
    ("sceIme",                           CAT_IME,      RISK_LOW,      STUB_RET_ZERO),
    ("sceKernel",                        CAT_KERNEL,   RISK_HIGH,     STUB_SKIP),
]

# ---------------------------------------------------------------------------
# Suffix-based heuristics (applied if no prefix match)
# ---------------------------------------------------------------------------
# (suffix, risk, stub_mode)

_SUFFIX_HEURISTICS = [
    ("Initialize",     RISK_LOW,    STUB_RET_ZERO),
    ("Init",           RISK_LOW,    STUB_RET_ZERO),
    ("Terminate",      RISK_LOW,    STUB_RET_ZERO),
    ("Term",           RISK_LOW,    STUB_RET_ZERO),
    ("Finalize",       RISK_LOW,    STUB_RET_ZERO),
    ("Destroy",        RISK_LOW,    STUB_RET_ZERO),
    ("Delete",         RISK_LOW,    STUB_RET_ZERO),
    ("Free",           RISK_LOW,    STUB_NOP),
    ("Close",          RISK_LOW,    STUB_RET_ZERO),
    ("GetStatus",      RISK_LOW,    STUB_RET_ZERO),
    ("GetResult",      RISK_LOW,    STUB_RET_ZERO),
    ("GetInfo",        RISK_LOW,    STUB_RET_ZERO),
    ("GetState",       RISK_LOW,    STUB_RET_ZERO),
    ("GetParam",       RISK_LOW,    STUB_RET_ZERO),
    ("SetParam",       RISK_LOW,    STUB_RET_ZERO),
    ("Poll",           RISK_SAFE,   STUB_RET_ZERO),
    ("Wait",           RISK_SAFE,   STUB_RET_ZERO),
    ("UpdateStatus",   RISK_SAFE,   STUB_RET_ZERO),
    ("SetVibration",   RISK_SAFE,   STUB_RET_ZERO),
    ("SetLightBar",    RISK_SAFE,   STUB_RET_ZERO),
    ("ResetLightBar",  RISK_SAFE,   STUB_RET_ZERO),
    ("Disable",        RISK_SAFE,   STUB_RET_ZERO),
    ("Enable",         RISK_SAFE,   STUB_RET_ZERO),
]


# ---------------------------------------------------------------------------
# Known Libraries metadata
# ---------------------------------------------------------------------------
# lib_name -> {category, is_essential, description}

KNOWN_LIBRARIES = {
    "libkernel.sprx":              {"category": CAT_KERNEL,   "essential": True,  "desc": "PS5 Kernel"},
    "libSceAgc.sprx":              {"category": CAT_GPU,      "essential": True,  "desc": "AMD GPU Commands"},
    "libSceAgcDriver.sprx":        {"category": CAT_GPU,      "essential": True,  "desc": "AGC Driver Interface"},
    "libSceGnmDriver.sprx":        {"category": CAT_GPU,      "essential": True,  "desc": "GNM GPU Driver (PS4 compat)"},
    "libSceVideoOut.sprx":         {"category": CAT_VIDEO,    "essential": True,  "desc": "Video Output"},
    "libSceAudioOut.sprx":         {"category": CAT_AUDIO,    "essential": False, "desc": "Audio Output"},
    "libScePad.sprx":              {"category": CAT_PAD,      "essential": True,  "desc": "Controller Input"},
    "libSceUserService.sprx":      {"category": CAT_SYSTEM,   "essential": True,  "desc": "User Service"},
    "libSceSystemService.sprx":    {"category": CAT_SYSTEM,   "essential": True,  "desc": "System Service"},
    "libSceNpAuth.sprx":           {"category": CAT_NP,       "essential": False, "desc": "NP Authentication"},
    "libSceNpTrophy.sprx":         {"category": CAT_TROPHY,   "essential": False, "desc": "Trophy System"},
    "libSceSaveData.sprx":         {"category": CAT_SAVEDATA, "essential": False, "desc": "Save Data"},
    "libSceSaveData.native.sprx":  {"category": CAT_SAVEDATA, "essential": False, "desc": "Save Data (Native)"},
    "libSceNet.sprx":              {"category": CAT_NETWORK,  "essential": False, "desc": "Network"},
    "libSceHttp.sprx":             {"category": CAT_HTTP,     "essential": False, "desc": "HTTP Client"},
    "libSceSsl.sprx":              {"category": CAT_SSL,      "essential": False, "desc": "SSL/TLS"},
    "libSceFiber.sprx":            {"category": CAT_FIBER,    "essential": False, "desc": "Fiber (Coroutine)"},
    "libSceIme.sprx":              {"category": CAT_IME,      "essential": False, "desc": "Input Method"},
    "libSceMsgDialog.sprx":        {"category": CAT_DIALOG,   "essential": False, "desc": "Message Dialog"},
    "libSceCommonDialog.sprx":     {"category": CAT_DIALOG,   "essential": False, "desc": "Common Dialog"},
    "libSceNpManager.sprx":        {"category": CAT_NP,       "essential": False, "desc": "NP Manager"},
    "libSceNpWebApi.sprx":         {"category": CAT_NP,       "essential": False, "desc": "NP Web API"},
    "libSceNpCommerce.sprx":       {"category": CAT_NP,       "essential": False, "desc": "NP Commerce"},
    "libSceNpSignaling.sprx":      {"category": CAT_NP,       "essential": False, "desc": "NP Signaling"},
    "libSceNpMatching2.sprx":      {"category": CAT_NP,       "essential": False, "desc": "NP Matchmaking"},
    "libSceAppContent.sprx":       {"category": CAT_SYSTEM,   "essential": False, "desc": "App Content / DLC"},
    "libSceRtc.sprx":              {"category": CAT_SYSTEM,   "essential": False, "desc": "Real-Time Clock"},
    "libScePlayGo.sprx":           {"category": CAT_SYSTEM,   "essential": False, "desc": "PlayGo Streaming"},
    "libSceScreenShot.sprx":       {"category": CAT_SYSTEM,   "essential": False, "desc": "Screenshot"},
    "libSceNpAuthAuthorizedAppDialog.sprx": {"category": CAT_NP, "essential": False, "desc": "NP Auth Dialog"},
    "libSceJson.sprx":             {"category": CAT_MISC,     "essential": False, "desc": "JSON Parser"},
    "libSceJson2.sprx":            {"category": CAT_MISC,     "essential": False, "desc": "JSON Parser v2"},
    "libSceLibcInternal.sprx":     {"category": CAT_SYSTEM,   "essential": True,  "desc": "Internal libc"},
    "libScePosix.sprx":            {"category": CAT_SYSTEM,   "essential": True,  "desc": "POSIX Layer"},
}


# ---------------------------------------------------------------------------
# PS5NidDB — Main API Class
# ---------------------------------------------------------------------------

class PS5NidDB:
    """Built-in knowledge base of PS5 system functions.

    Provides NID resolution, function classification, firmware availability,
    and prefix-based heuristic analysis without requiring firmware dumps.
    """

    def __init__(self):
        # Build lookup indices
        self._by_name: dict[str, tuple] = {}    # name -> (lib, cat, min_fw, risk, stub)
        self._by_nid: dict[str, str] = {}        # NID hex -> name
        self._nid_to_entry: dict[str, tuple] = {} # NID hex -> full entry

        for name, lib, cat, min_fw, risk, stub in _KNOWN_FUNCTIONS:
            nid = calc_nid(name)
            self._by_name[name] = (lib, cat, min_fw, risk, stub)
            self._by_nid[nid] = name
            self._nid_to_entry[nid] = (name, lib, cat, min_fw, risk, stub)

    # ---- NID Resolution ---------------------------------------------------

    def resolve_nid(self, nid_hex: str) -> Optional[str]:
        """Resolve a NID hex string to a function name.
        Returns None if the NID is not in the database.
        """
        return self._by_nid.get(nid_hex.upper())

    def resolve_nid_full(self, nid_hex: str) -> Optional[dict]:
        """Resolve a NID to full function info.
        Returns dict with name, library, category, min_fw, stub_risk, stub_mode.
        """
        entry = self._nid_to_entry.get(nid_hex.upper())
        if not entry:
            return None
        name, lib, cat, min_fw, risk, stub = entry
        return {
            "name": name,
            "library": lib,
            "category": cat,
            "min_fw": min_fw,
            "stub_risk": risk,
            "stub_mode": stub,
        }

    # ---- Function Classification ------------------------------------------

    def classify_function(self, name: str) -> dict:
        """Classify a function by name.
        Returns {"category", "stub_risk", "stub_mode", "source"}.
        Source is "db" for known functions or "heuristic" for prefix/suffix match.
        """
        # Check known DB first
        entry = self._by_name.get(name)
        if entry:
            lib, cat, min_fw, risk, stub = entry
            return {
                "category": cat,
                "stub_risk": risk,
                "stub_mode": stub,
                "source": "db",
                "library": lib,
                "min_fw": min_fw,
            }

        # Try prefix heuristics
        for prefix, cat, risk, stub in _PREFIX_HEURISTICS:
            if name.startswith(prefix):
                return {
                    "category": cat,
                    "stub_risk": risk,
                    "stub_mode": stub,
                    "source": "heuristic_prefix",
                }

        # Try suffix heuristics
        for suffix, risk, stub in _SUFFIX_HEURISTICS:
            if name.endswith(suffix):
                return {
                    "category": CAT_MISC,
                    "stub_risk": risk,
                    "stub_mode": stub,
                    "source": "heuristic_suffix",
                }

        # Unknown function — default to medium risk
        return {
            "category": CAT_MISC,
            "stub_risk": RISK_MEDIUM,
            "stub_mode": STUB_RET_ZERO,
            "source": "unknown",
        }

    # ---- Firmware Availability -------------------------------------------

    def is_function_available(self, name: str, target_fw: str) -> bool:
        """Check if a function is available on the target firmware version.
        Returns True if the function exists on target_fw, or if unknown.
        """
        entry = self._by_name.get(name)
        if not entry:
            return True  # Unknown function — assume available
        min_fw = entry[2]
        return _fw_compare(target_fw, min_fw) >= 0

    def get_function_min_fw(self, name: str) -> Optional[str]:
        """Return the minimum firmware version where a function exists."""
        entry = self._by_name.get(name)
        if not entry:
            return None
        return entry[2]

    def get_missing_for_fw(self, func_names: list[str], target_fw: str) -> list[dict]:
        """Given a list of function names, return those not available on target_fw.
        Each entry: {"name", "min_fw", "category", "stub_risk", "stub_mode"}.
        """
        missing = []
        for name in func_names:
            entry = self._by_name.get(name)
            if entry:
                lib, cat, min_fw, risk, stub = entry
                if _fw_compare(target_fw, min_fw) < 0:
                    missing.append({
                        "name": name,
                        "library": lib,
                        "min_fw": min_fw,
                        "category": cat,
                        "stub_risk": risk,
                        "stub_mode": stub,
                    })
        return missing

    # ---- Library Metadata ------------------------------------------------

    def get_library_info(self, lib_name: str) -> Optional[dict]:
        """Return metadata about a known PS5 library."""
        return KNOWN_LIBRARIES.get(lib_name)

    def get_known_function_count(self) -> int:
        """Return the total number of functions in the database."""
        return len(self._by_name)

    def get_all_known_names(self) -> list[str]:
        """Return all known function names."""
        return list(self._by_name.keys())

    # ---- Statistics ------------------------------------------------------

    def get_stats(self) -> dict:
        """Return database statistics."""
        risk_counts = {}
        cat_counts = {}
        lib_counts = {}
        for name, (lib, cat, min_fw, risk, stub) in self._by_name.items():
            risk_counts[risk] = risk_counts.get(risk, 0) + 1
            cat_counts[cat] = cat_counts.get(cat, 0) + 1
            lib_counts[lib] = lib_counts.get(lib, 0) + 1

        return {
            "total_functions": len(self._by_name),
            "total_libraries": len(KNOWN_LIBRARIES),
            "by_risk": risk_counts,
            "by_category": cat_counts,
            "by_library": lib_counts,
        }


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _fw_compare(fw_a: str, fw_b: str) -> int:
    """Compare two firmware version strings.
    Returns: >0 if a > b, 0 if equal, <0 if a < b.
    """
    def parse(v):
        parts = v.split(".")
        result = []
        for p in parts:
            try:
                result.append(int(p))
            except ValueError:
                result.append(0)
        while len(result) < 3:
            result.append(0)
        return result

    a = parse(fw_a)
    b = parse(fw_b)
    for x, y in zip(a, b):
        if x != y:
            return x - y
    return 0


# ---------------------------------------------------------------------------
# CLI (for testing / exploration)
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="PS5 NID Knowledge Base")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_resolve = sub.add_parser("resolve", help="Resolve NID to function name")
    p_resolve.add_argument("--nid", required=True, help="NID hex string (16 chars)")

    p_classify = sub.add_parser("classify", help="Classify a function name")
    p_classify.add_argument("--name", required=True)

    p_check = sub.add_parser("check", help="Check function availability on firmware")
    p_check.add_argument("--name", required=True)
    p_check.add_argument("--fw", required=True, help="Target firmware (e.g. 6.00)")

    p_stats = sub.add_parser("stats", help="Show database statistics")

    p_missing = sub.add_parser("missing", help="Show functions missing on target FW")
    p_missing.add_argument("--fw", required=True, help="Target firmware (e.g. 6.00)")

    parsed = parser.parse_args()

    db = PS5NidDB()

    if parsed.cmd == "resolve":
        name = db.resolve_nid(parsed.nid)
        if name:
            info = db.classify_function(name)
            print("{} -> {} [{}] risk={}".format(parsed.nid, name,
                  info["category"], info["stub_risk"]))
        else:
            print("NID {} not found in database".format(parsed.nid))

    elif parsed.cmd == "classify":
        info = db.classify_function(parsed.name)
        for k, v in info.items():
            print("  {}: {}".format(k, v))

    elif parsed.cmd == "check":
        avail = db.is_function_available(parsed.name, parsed.fw)
        min_fw = db.get_function_min_fw(parsed.name)
        if min_fw:
            status = "AVAILABLE" if avail else "MISSING (needs FW {})".format(min_fw)
            print("{} on FW {}: {}".format(parsed.name, parsed.fw, status))
        else:
            print("{}: not in database (assumed available)".format(parsed.name))

    elif parsed.cmd == "stats":
        stats = db.get_stats()
        print("Functions: {}".format(stats["total_functions"]))
        print("Libraries: {}".format(stats["total_libraries"]))
        print("\nBy risk:")
        for risk, count in sorted(stats["by_risk"].items()):
            print("  {}: {}".format(risk, count))
        print("\nBy category:")
        for cat, count in sorted(stats["by_category"].items()):
            print("  {}: {}".format(cat, count))

    elif parsed.cmd == "missing":
        all_names = db.get_all_known_names()
        missing = db.get_missing_for_fw(all_names, parsed.fw)
        if missing:
            print("Functions NOT available on FW {}:".format(parsed.fw))
            for m in missing:
                print("  {} [{}] needs FW {} — risk: {} stub: {}".format(
                    m["name"], m["library"], m["min_fw"],
                    m["stub_risk"], m["stub_mode"]))
            print("\nTotal missing: {}".format(len(missing)))
        else:
            print("All known functions available on FW {}".format(parsed.fw))
