Imports System.IO
Imports System.Text

''' <summary>
''' Parses the param.sfo (System File Object) embedded in PKG files.
''' Extracts game title, content ID, version, and category.
''' </summary>
Public Class PKGMetadata

    Public Property Title As String = ""
    Public Property ContentId As String = ""
    Public Property TitleId As String = ""
    Public Property Version As String = ""
    Public Property AppVersion As String = ""
    Public Property Category As String = ""
    Public Property MinFirmware As String = ""

    ' All key-value pairs from the SFO
    Public Property AllParams As Dictionary(Of String, String)

    Public Sub New()
        AllParams = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    End Sub

    ''' <summary>
    ''' Parses param.sfo data from a byte array.
    ''' </summary>
    Public Sub LoadFromBytes(data As Byte())
        Using ms As New MemoryStream(data)
            Using br As New BinaryReader(ms)
                Load(br)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Parses param.sfo from a BinaryReader positioned at the start of the SFO data.
    ''' </summary>
    Public Sub Load(br As BinaryReader)
        Dim startPos = br.BaseStream.Position

        ' Read SFO magic (4 bytes, little-endian: 00 50 53 46 = "\0PSF")
        Dim magic = br.ReadUInt32()
        If magic <> PKGConstants.SFO_MAGIC Then
            Throw New InvalidOperationException(
                $"Invalid SFO magic: expected 0x{PKGConstants.SFO_MAGIC:X8}, got 0x{magic:X8}")
        End If

        ' SFO header
        Dim sfoVersion = br.ReadUInt32()       ' version
        Dim keyTableOffset = br.ReadUInt32()    ' offset to key table
        Dim dataTableOffset = br.ReadUInt32()   ' offset to data table
        Dim entryCount = br.ReadUInt32()        ' number of entries

        ' Read index entries
        Dim indices As New List(Of SfoIndexEntry)()
        For i = 0 To CInt(entryCount) - 1
            Dim idx As New SfoIndexEntry()
            idx.KeyOffset = br.ReadUInt16()
            idx.DataFormat = br.ReadUInt16()
            idx.DataLen = br.ReadUInt32()
            idx.DataMaxLen = br.ReadUInt32()
            idx.DataOffset = br.ReadUInt32()
            indices.Add(idx)
        Next

        ' Read each entry
        For Each idx In indices
            ' Read key
            br.BaseStream.Seek(startPos + keyTableOffset + idx.KeyOffset, SeekOrigin.Begin)
            Dim keyBytes As New List(Of Byte)()
            Dim b = br.ReadByte()
            While b <> 0
                keyBytes.Add(b)
                b = br.ReadByte()
            End While
            Dim key = Encoding.UTF8.GetString(keyBytes.ToArray())

            ' Read value
            br.BaseStream.Seek(startPos + dataTableOffset + idx.DataOffset, SeekOrigin.Begin)
            Dim value As String

            If idx.DataFormat = PKGConstants.SFO_DATA_FMT_UTF8 Then
                Dim valBytes = br.ReadBytes(CInt(idx.DataLen))
                Dim nullIdx = Array.IndexOf(valBytes, CByte(0))
                If nullIdx >= 0 Then
                    value = Encoding.UTF8.GetString(valBytes, 0, nullIdx)
                Else
                    value = Encoding.UTF8.GetString(valBytes)
                End If
            ElseIf idx.DataFormat = PKGConstants.SFO_DATA_FMT_INT32 Then
                Dim intVal = br.ReadUInt32()
                value = intVal.ToString()
            Else
                value = $"(format 0x{idx.DataFormat:X})"
            End If

            AllParams(key) = value
        Next

        ' Map well-known keys
        If AllParams.ContainsKey("TITLE") Then Title = AllParams("TITLE")
        If AllParams.ContainsKey("CONTENT_ID") Then ContentId = AllParams("CONTENT_ID")
        If AllParams.ContainsKey("TITLE_ID") Then TitleId = AllParams("TITLE_ID")
        If AllParams.ContainsKey("APP_VER") Then AppVersion = AllParams("APP_VER")
        If AllParams.ContainsKey("VERSION") Then Version = AllParams("VERSION")
        If AllParams.ContainsKey("CATEGORY") Then Category = AllParams("CATEGORY")
        If AllParams.ContainsKey("SYSTEM_VER") Then
            Dim sysVer As UInteger
            If UInteger.TryParse(AllParams("SYSTEM_VER"), sysVer) Then
                Dim major = (sysVer >> 24) And &HFF
                Dim minor = (sysVer >> 16) And &HFF
                MinFirmware = $"{major}.{minor:D2}"
            Else
                MinFirmware = AllParams("SYSTEM_VER")
            End If
        End If
    End Sub

    ' ---- Internal SFO index entry ----
    Private Class SfoIndexEntry
        Public KeyOffset As UShort
        Public DataFormat As UShort
        Public DataLen As UInteger
        Public DataMaxLen As UInteger
        Public DataOffset As UInteger
    End Class

End Class
