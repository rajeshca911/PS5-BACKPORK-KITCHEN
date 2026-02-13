''' <summary>
''' Constants for PlayStation PKG/FPKG file format.
''' </summary>
Public Module PKGConstants

    ' ---- Magic Numbers ----
    Public Const PKG_MAGIC As UInteger = &H7F434E54UI   ' 0x7F 'C' 'N' 'T'
    Public Const SFO_MAGIC As UInteger = &H46535000UI   ' PSF\0 (little-endian: 00 50 53 46)

    ' ---- Header Offsets ----
    Public Const HEADER_SIZE As Integer = &H2000
    Public Const PKG_FLAGS_OFFSET As Integer = &H8
    Public Const PKG_ENTRY_COUNT_OFFSET As Integer = &H10
    Public Const PKG_TABLE_OFFSET_OFFSET As Integer = &H18
    Public Const PKG_ENTRY_DATA_SIZE_OFFSET As Integer = &H20
    Public Const PKG_BODY_OFFSET_OFFSET As Integer = &H28
    Public Const PKG_BODY_SIZE_OFFSET As Integer = &H30
    Public Const PKG_CONTENT_OFFSET_OFFSET As Integer = &H40
    Public Const PKG_CONTENT_SIZE_OFFSET As Integer = &H48
    Public Const PKG_CONTENT_ID_OFFSET As Integer = &H40
    Public Const PKG_DRM_TYPE_OFFSET As Integer = &H70
    Public Const PKG_CONTENT_TYPE_OFFSET As Integer = &H74
    Public Const PKG_CONTENT_FLAGS_OFFSET As Integer = &H78

    ' ---- Entry Table ----
    Public Const ENTRY_SIZE As Integer = 32
    Public Const ENTRY_ID_OFFSET As Integer = 0
    Public Const ENTRY_FILENAME_OFFSET_FIELD As Integer = 4
    Public Const ENTRY_FLAGS1_OFFSET As Integer = 8
    Public Const ENTRY_FLAGS2_OFFSET As Integer = 12
    Public Const ENTRY_DATA_OFFSET_FIELD As Integer = 16
    Public Const ENTRY_DATA_SIZE_FIELD As Integer = 24

    ' ---- DRM Types ----
    Public Const DRM_TYPE_NONE As UInteger = 0
    Public Const DRM_TYPE_PS4 As UInteger = &HF

    ' ---- Content Types ----
    Public Const CONTENT_TYPE_GD As UInteger = &H1A      ' Game Data
    Public Const CONTENT_TYPE_AC As UInteger = &H1B      ' Additional Content (DLC)
    Public Const CONTENT_TYPE_AL As UInteger = &H1C      ' App License
    Public Const CONTENT_TYPE_DP As UInteger = &H1E      ' Delta Patch

    ' ---- Well-Known Entry IDs ----
    Public Const ENTRY_ID_PARAM_SFO As UInteger = &H1000
    Public Const ENTRY_ID_ICON0_PNG As UInteger = &H1200
    Public Const ENTRY_ID_PIC1_PNG As UInteger = &H1220

    ' ---- SFO Format ----
    Public Const SFO_HEADER_SIZE As Integer = 20
    Public Const SFO_ENTRY_SIZE As Integer = 16
    Public Const SFO_DATA_FMT_UTF8 As UShort = 516
    Public Const SFO_DATA_FMT_INT32 As UShort = 1028

    ''' <summary>
    ''' Returns a human-readable content type string.
    ''' </summary>
    Public Function GetContentTypeName(contentType As UInteger) As String
        Select Case contentType
            Case CONTENT_TYPE_GD : Return "Game Data"
            Case CONTENT_TYPE_AC : Return "Additional Content (DLC)"
            Case CONTENT_TYPE_AL : Return "App License"
            Case CONTENT_TYPE_DP : Return "Delta Patch"
            Case Else : Return $"Unknown (0x{contentType:X})"
        End Select
    End Function

End Module
