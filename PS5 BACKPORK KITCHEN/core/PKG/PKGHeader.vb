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
        br.BaseStream.Seek(0, SeekOrigin.Begin)

        ' Magic (4 bytes, big-endian)
        Magic = ReadUInt32BE(br)
        If Magic <> PKGConstants.PKG_MAGIC Then
            Throw New InvalidOperationException(
                $"Invalid PKG magic: expected 0x{PKGConstants.PKG_MAGIC:X8}, got 0x{Magic:X8}")
        End If

        ' Skip to flags at offset 0x08
        br.BaseStream.Seek(PKGConstants.PKG_FLAGS_OFFSET, SeekOrigin.Begin)
        Flags = ReadUInt32BE(br)

        ' Skip 4 bytes padding
        br.ReadBytes(4)

        ' Entry count at offset 0x10
        EntryCount = ReadUInt32BE(br)

        ' Skip 4 bytes
        br.ReadBytes(4)

        ' Table offset at offset 0x18
        TableOffset = ReadUInt64BE(br)

        ' Entry data size at offset 0x20
        EntryDataSize = ReadUInt64BE(br)

        ' Body offset at offset 0x28
        BodyOffset = ReadUInt64BE(br)

        ' Body size at offset 0x30
        BodySize = ReadUInt64BE(br)

        ' Skip to content offset at 0x40
        br.BaseStream.Seek(&H38, SeekOrigin.Begin)
        br.ReadBytes(8) ' padding
        ContentOffset = ReadUInt64BE(br)
        ContentSize = ReadUInt64BE(br)

        ' Content ID at offset 0x40 is actually a 36-byte ASCII string
        ' It's at a fixed position in the header
        br.BaseStream.Seek(&H40, SeekOrigin.Begin)
        Dim contentIdBytes = br.ReadBytes(48)
        ContentId = Encoding.ASCII.GetString(contentIdBytes).TrimEnd(ChrW(0))

        ' DRM type at offset 0x70
        br.BaseStream.Seek(PKGConstants.PKG_DRM_TYPE_OFFSET, SeekOrigin.Begin)
        DrmType = ReadUInt32BE(br)

        ' Content type at offset 0x74
        ContentType = ReadUInt32BE(br)

        ' Content flags at offset 0x78
        ContentFlags = ReadUInt32BE(br)

        ' Total package size
        PackageSize = br.BaseStream.Length
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
