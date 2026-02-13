Imports System.IO

''' <summary>
''' Application-wide constants including firmware management paths
''' </summary>
Module Constants

    ' ===========================
    ' FIRMWARE MANAGEMENT PATHS
    ' ===========================

    ''' <summary>Base directory for firmware cache (temporary downloads)</summary>
    Public ReadOnly Property FirmwareCacheDirectory As String
        Get
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firmware_cache")
        End Get
    End Property

    ''' <summary>Directory containing external tools (ps5-pup-unpacker, etc.)</summary>
    Public ReadOnly Property ToolsDirectory As String
        Get
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools")
        End Get
    End Property

    ''' <summary>Path to firmware metadata JSON file</summary>
    Public ReadOnly Property FirmwareMetadataPath As String
        Get
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firmware_metadata.json")
        End Get
    End Property

    ''' <summary>Path to ps5-pup-unpacker executable</summary>
    Public ReadOnly Property PupUnpackerPath As String
        Get

            Return Path.Combine(ToolsDirectory, "pup_unpacker.exe")
        End Get
    End Property

    ''' <summary>Direct download URLs for ps5-pup-unpacker tool</summary>
    Public Const PupUnpackerDownloadUrl As String = "https://github.com/zecoxao/ps5-pup-unpacker/raw/main/pup_unpacker.exe"

    Public Const PupUnpackerDownloadUrlAlt As String = "https://raw.githubusercontent.com/zecoxao/ps5-pup-unpacker/main/pup_unpacker.exe"
    Public Const PupUnpackerDownloadUrlBackup As String = "https://github.com/SocraticBliss/ps5_pup_tool/raw/master/ps5_pup_unpacker.exe"

    ''' <summary>Supported firmware versions (1-10)</summary>
    Public ReadOnly SupportedFirmwareVersions As Integer() = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}

    ''' <summary>Get fakelib directory path for specific firmware version</summary>
    Public Function GetFakelibDirectory(firmwareVersion As Integer) As String
        Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{firmwareVersion}\fakelib")
    End Function

End Module
'Public Module ElfConstants

'    ' Segment types
'    Public Const PT_SCE_PROCPARAM As UInteger = &H61000001UI
'    Public Const PT_SCE_MODULE_PARAM As UInteger = &H61000002UI

'    ' Magic values
'    Public Const SCE_PROCESS_PARAM_MAGIC As UInteger = &H4942524FUI
'    Public Const SCE_MODULE_PARAM_MAGIC As UInteger = &H3C13F4BFUI

'    ' Offsets & sizes
'    Public Const SCE_PARAM_MAGIC_OFFSET As Integer = &H8
'    Public Const SCE_PARAM_MAGIC_SIZE As Integer = 4

'    Public Const SCE_PARAM_PS4_SDK_OFFSET As Integer = &H10
'    Public Const SCE_PARAM_PS5_SDK_OFFSET As Integer = &H14
'    Public Const SCE_PARAM_PS_VERSION_SIZE As Integer = 4

'    Public Const PHT_OFFSET_OFFSET As Integer = &H20
'    Public Const PHT_OFFSET_SIZE As Integer = 8

'    Public Const PHT_COUNT_OFFSET As Integer = &H38
'    Public Const PHT_COUNT_SIZE As Integer = 2

'    Public Const PHDR_ENTRY_SIZE As Integer = &H38
'    Public Const PHDR_TYPE_OFFSET As Integer = &H0
'    Public Const PHDR_TYPE_SIZE As Integer = 4

'    Public Const PHDR_OFFSET_OFFSET As Integer = &H8
'    Public Const PHDR_OFFSET_SIZE As Integer = 8

'    ' File magics
'    Public ReadOnly ELF_MAGIC As Byte() = {&H7F, &H45, &H4C, &H46}
'    Public ReadOnly PS4_FSELF_MAGIC As Byte() = {&H4F, &H15, &H3D, &H1D}
'    Public ReadOnly PS5_FSELF_MAGIC As Byte() = {&H54, &H14, &HF5, &HEE}

'End Module
Public Module ElfConstants

    Public Const PT_LOAD As UInteger = &H1
    Public Const PT_DYNAMIC As UInteger = &H2
    Public Const PT_INTERP As UInteger = &H3
    Public Const PT_TLS As UInteger = &H7

    Public Const PT_GNU_EH_FRAME As UInteger = &H6474E550
    Public Const PT_GNU_STACK As UInteger = &H6474E551

    Public Const PT_SCE_RELA As UInteger = &H60000000UI
    Public Const PT_SCE_DYNLIBDATA As UInteger = &H61000000UI
    Public Const PT_SCE_PROCPARAM As UInteger = &H61000001UI
    Public Const PT_SCE_MODULE_PARAM As UInteger = &H61000002UI
    Public Const PT_SCE_RELRO As UInteger = &H61000010UI
    Public Const PT_SCE_COMMENT As UInteger = &H6FFFFF00UI
    Public Const PT_SCE_VERSION As UInteger = &H6FFFFF01UI

    Public Const PF_EXEC As UInteger = &H1
    Public Const PF_WRITE As UInteger = &H2
    Public Const PF_READ As UInteger = &H4

    Public Const PF_READ_EXEC As UInteger = PF_READ Or PF_EXEC
    Public Const PF_READ_WRITE As UInteger = PF_READ Or PF_WRITE

    Public Const SCE_PROCESS_PARAM_MAGIC As UInteger = &H4942524FUI
    Public Const SCE_MODULE_PARAM_MAGIC As UInteger = &H3C13F4BFUI

    Public ReadOnly ELF_MAGIC As Byte() = {&H7F, &H45, &H4C, &H46}
    Public ReadOnly PS4_FSELF_MAGIC As Byte() = {&H4F, &H15, &H3D, &H1D}
    Public ReadOnly PS5_FSELF_MAGIC As Byte() = {&H54, &H14, &HF5, &HEE}

    Public Const SCE_PARAM_MAGIC_OFFSET As Integer = &H8
    Public Const SCE_PARAM_MAGIC_SIZE As Integer = 4

    Public Const SCE_PARAM_PS4_SDK_OFFSET As Integer = &H10
    Public Const SCE_PARAM_PS5_SDK_OFFSET As Integer = &H14
    Public Const SCE_PARAM_PS_VERSION_SIZE As Integer = 4

    Public Const PHT_OFFSET_OFFSET As Integer = &H20
    Public Const PHT_OFFSET_SIZE As Integer = 8

    Public Const PHT_COUNT_OFFSET As Integer = &H38
    Public Const PHT_COUNT_SIZE As Integer = 2

    Public Const PHDR_ENTRY_SIZE As Integer = &H38
    Public Const PHDR_TYPE_OFFSET As Integer = &H0
    Public Const PHDR_TYPE_SIZE As Integer = 4

    Public Const PHDR_OFFSET_OFFSET As Integer = &H8
    Public Const PHDR_OFFSET_SIZE As Integer = 8

End Module