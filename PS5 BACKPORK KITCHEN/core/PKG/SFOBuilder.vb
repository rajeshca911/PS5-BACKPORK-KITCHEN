Imports System.IO
Imports System.Text

''' <summary>
''' Builds param.sfo binary data from key-value pairs.
''' Output is compatible with PKGMetadata.LoadFromBytes() for round-trip verification.
''' SFO format uses little-endian byte order.
''' </summary>
Public Class SFOBuilder

    ''' <summary>
    ''' Represents a single SFO parameter entry.
    ''' </summary>
    Public Class SFOParam
        Public Property Key As String
        Public Property Value As String
        Public Property MaxLength As Integer
        Public Property Format As UShort

        Public Sub New(key As String, value As String, maxLength As Integer,
                       Optional format As UShort = PKGConstants.SFO_DATA_FMT_UTF8)
            Me.Key = key
            Me.Value = value
            Me.MaxLength = maxLength
            Me.Format = format
        End Sub
    End Class

    ''' <summary>
    ''' Creates the standard set of PS5 SFO parameters.
    ''' </summary>
    Public Shared Function CreateDefaultParams(contentId As String, title As String,
                                                titleId As String,
                                                Optional appVersion As String = "01.00",
                                                Optional version As String = "01.00",
                                                Optional category As String = "gd") As List(Of SFOParam)
        Dim params As New List(Of SFOParam)()

        params.Add(New SFOParam("APP_VER", appVersion, 8))
        params.Add(New SFOParam("CATEGORY", category, 4))
        params.Add(New SFOParam("CONTENT_ID", contentId, 48))
        params.Add(New SFOParam("TITLE", title, 128))
        params.Add(New SFOParam("TITLE_ID", titleId, 12))
        params.Add(New SFOParam("VERSION", version, 8))

        Return params
    End Function

    ''' <summary>
    ''' Builds the SFO binary from a list of parameters.
    ''' Keys are sorted alphabetically per SFO spec.
    ''' </summary>
    Public Shared Function Build(params As List(Of SFOParam)) As Byte()
        ' Sort keys alphabetically (SFO spec requirement)
        Dim sorted = params.OrderBy(Function(p) p.Key, StringComparer.Ordinal).ToList()

        ' Calculate key table: null-terminated strings
        Dim keyTableBytes As New List(Of Byte)()
        Dim keyOffsets As New List(Of UShort)()
        For Each p In sorted
            keyOffsets.Add(CUShort(keyTableBytes.Count))
            keyTableBytes.AddRange(Encoding.UTF8.GetBytes(p.Key))
            keyTableBytes.Add(0) ' null terminator
        Next

        ' Calculate data table: values padded to MaxLength
        Dim dataTableBytes As New List(Of Byte)()
        Dim dataOffsets As New List(Of UInteger)()
        Dim dataLengths As New List(Of UInteger)()
        For Each p In sorted
            dataOffsets.Add(CUInt(dataTableBytes.Count))

            If p.Format = PKGConstants.SFO_DATA_FMT_UTF8 Then
                Dim valBytes = Encoding.UTF8.GetBytes(p.Value)
                Dim dataLen = valBytes.Length + 1 ' include null terminator
                dataLengths.Add(CUInt(dataLen))
                dataTableBytes.AddRange(valBytes)
                dataTableBytes.Add(0) ' null terminator
                ' Pad to MaxLength
                Dim padding = p.MaxLength - dataLen
                If padding > 0 Then
                    dataTableBytes.AddRange(New Byte(padding - 1) {})
                End If
            ElseIf p.Format = PKGConstants.SFO_DATA_FMT_INT32 Then
                Dim intVal As UInteger
                UInteger.TryParse(p.Value, intVal)
                dataLengths.Add(4)
                dataTableBytes.AddRange(BitConverter.GetBytes(intVal))
            End If
        Next

        ' Calculate offsets
        Dim indexTableSize = sorted.Count * PKGConstants.SFO_ENTRY_SIZE
        Dim keyTableOffset = CUInt(PKGConstants.SFO_HEADER_SIZE + indexTableSize)
        Dim dataTableOffset = keyTableOffset + CUInt(keyTableBytes.Count)
        ' Align data table to 4-byte boundary
        Dim dataTableAlignPad = CInt((4 - (dataTableOffset Mod 4)) Mod 4)
        dataTableOffset += CUInt(dataTableAlignPad)

        ' Build the SFO binary
        Using ms As New MemoryStream()
            Using bw As New BinaryWriter(ms)
                ' SFO Header (20 bytes, little-endian)
                bw.Write(PKGConstants.SFO_MAGIC)          ' Magic
                bw.Write(CUInt(&H101))                    ' Version 1.1
                bw.Write(keyTableOffset)                   ' Key table offset
                bw.Write(dataTableOffset)                  ' Data table offset
                bw.Write(CUInt(sorted.Count))             ' Entry count

                ' Index entries (16 bytes each, little-endian)
                For i = 0 To sorted.Count - 1
                    bw.Write(keyOffsets(i))                ' Key offset (2 bytes)
                    bw.Write(sorted(i).Format)            ' Data format (2 bytes)
                    bw.Write(dataLengths(i))               ' Data length (4 bytes)
                    bw.Write(CUInt(sorted(i).MaxLength))  ' Data max length (4 bytes)
                    bw.Write(dataOffsets(i))               ' Data offset (4 bytes)
                Next

                ' Key table
                bw.Write(keyTableBytes.ToArray())

                ' Alignment padding before data table
                If dataTableAlignPad > 0 Then
                    bw.Write(New Byte(dataTableAlignPad - 1) {})
                End If

                ' Data table
                bw.Write(dataTableBytes.ToArray())
            End Using

            Return ms.ToArray()
        End Using
    End Function

End Class
