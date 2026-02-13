Imports System.IO
Imports System.Text

''' <summary>
''' Parses the PKG file header structure.
''' PKG uses big-endian byte order for multi-byte fields.
''' </summary>
Public Class PKGHeader

    Public Property Magic As UInteger
    Public Property Flags As UInteger
    Public Property EntryCount As UInteger
    Public Property TableOffset As ULong
    Public Property EntryDataSize As ULong
    Public Property BodyOffset As ULong
    Public Property BodySize As ULong
    Public Property ContentOffset As ULong
    Public Property ContentSize As ULong
    Public Property ContentId As String
    Public Property DrmType As UInteger
    Public Property ContentType As UInteger
    Public Property ContentFlags As UInteger
    Public Property PackageSize As Long

    ''' <summary>
    ''' Reads the PKG header from the given BinaryReader.
    ''' The reader should be positioned at the start of the file.
    ''' </summary>
    Public Sub Load(br As BinaryReader)
        Dim fileLength = br.BaseStream.Length
        If fileLength < &H100 Then
            Throw New InvalidOperationException("File is too small to be a valid PKG.")
        End If

        br.BaseStream.Seek(0, SeekOrigin.Begin)

        ' Magic (4 bytes, big-endian) at 0x00
        Magic = ReadUInt32BE(br)
        If Magic <> PKGConstants.PKG_MAGIC Then
            Throw New InvalidOperationException(
                $"Invalid PKG magic: expected 0x{PKGConstants.PKG_MAGIC:X8}, got 0x{Magic:X8}")
        End If

        ' Flags at 0x04
        Flags = ReadUInt32BE(br)

        ' Skip 0x08-0x0F (unknown + file_count)
        br.BaseStream.Seek(&H10, SeekOrigin.Begin)

        ' Entry count at 0x10 (4 bytes)
        EntryCount = ReadUInt32BE(br)

        ' Skip sc_entry_count (2 bytes) + entry_count_2 (2 bytes) at 0x14-0x17
        br.BaseStream.Seek(&H18, SeekOrigin.Begin)

        ' Table offset at 0x18 (4 bytes, NOT 8)
        TableOffset = ReadUInt32BE(br)

        ' Entry data size at 0x1C (4 bytes, NOT 8)
        EntryDataSize = ReadUInt32BE(br)

        ' Body offset at 0x20 (8 bytes)
        BodyOffset = ReadUInt64BE(br)

        ' Body size at 0x28 (8 bytes)
        BodySize = ReadUInt64BE(br)

        ' Content offset at 0x30 (8 bytes)
        ContentOffset = ReadUInt64BE(br)

        ' Content size at 0x38 (8 bytes)
        ContentSize = ReadUInt64BE(br)

        ' Content ID at 0x40 (36 bytes ASCII, null-padded)
        br.BaseStream.Seek(&H40, SeekOrigin.Begin)
        Dim contentIdBytes = br.ReadBytes(36)
        ContentId = Encoding.ASCII.GetString(contentIdBytes).TrimEnd(ChrW(0))

        ' DRM type at 0x68 (4 bytes)
        br.BaseStream.Seek(&H68, SeekOrigin.Begin)
        DrmType = ReadUInt32BE(br)

        ' Content type at 0x6C (4 bytes)
        ContentType = ReadUInt32BE(br)

        ' Content flags at 0x70 (4 bytes)
        ContentFlags = ReadUInt32BE(br)

        ' Total package size
        PackageSize = fileLength

        ' Validate table offset is within file bounds
        If TableOffset > CULng(fileLength) Then
            Throw New InvalidOperationException(
                $"Invalid table offset 0x{TableOffset:X} exceeds file size {fileLength}.")
        End If
    End Sub

    ''' <summary>
    ''' True if this is a Fake PKG (FPKG) - no DRM encryption.
    ''' </summary>
    Public ReadOnly Property IsFPKG As Boolean
        Get
            Return DrmType = PKGConstants.DRM_TYPE_NONE
        End Get
    End Property

    ''' <summary>
    ''' Returns the human-readable content type string.
    ''' </summary>
    Public ReadOnly Property ContentTypeString As String
        Get
            Return PKGConstants.GetContentTypeName(ContentType)
        End Get
    End Property

    ''' <summary>
    ''' Returns either "FPKG" or "Retail PKG".
    ''' </summary>
    Public ReadOnly Property PackageTypeString As String
        Get
            Return If(IsFPKG, "FPKG (Fake PKG)", "Retail PKG (Encrypted)")
        End Get
    End Property

    ' ---- Big-Endian Helpers ----

    Private Shared Function ReadUInt32BE(br As BinaryReader) As UInteger
        Dim bytes = br.ReadBytes(4)
        Array.Reverse(bytes)
        Return BitConverter.ToUInt32(bytes, 0)
    End Function

    Private Shared Function ReadUInt64BE(br As BinaryReader) As ULong
        Dim bytes = br.ReadBytes(8)
        Array.Reverse(bytes)
        Return BitConverter.ToUInt64(bytes, 0)
    End Function

End Class
