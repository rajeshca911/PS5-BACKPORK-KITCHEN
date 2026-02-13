Imports System.IO
Imports System.Text

''' <summary>
''' Represents a single entry in the PKG entry table.
''' </summary>
Public Class PKGEntry
    Public Property Id As UInteger
    Public Property FilenameOffset As UInteger
    Public Property Flags1 As UInteger
    Public Property Flags2 As UInteger
    Public Property DataOffset As ULong
    Public Property DataSize As ULong
    Public Property FileName As String

    ''' <summary>
    ''' True if this entry's data is encrypted.
    ''' </summary>
    Public ReadOnly Property IsEncrypted As Boolean
        Get
            Return (Flags1 And &H80000000UI) <> 0
        End Get
    End Property

    ''' <summary>
    ''' Returns a display-friendly size string.
    ''' </summary>
    Public ReadOnly Property SizeString As String
        Get
            If DataSize < 1024 Then Return $"{DataSize} B"
            If DataSize < 1024 * 1024 Then Return $"{DataSize / 1024.0:F1} KB"
            If DataSize < 1024UL * 1024 * 1024 Then Return $"{DataSize / (1024.0 * 1024):F1} MB"
            Return $"{DataSize / (1024.0 * 1024 * 1024):F2} GB"
        End Get
    End Property
End Class

''' <summary>
''' Parses the PKG entry table and resolves filenames from the name table.
''' </summary>
Public Class PKGEntryTable

    Public Property Entries As List(Of PKGEntry)

    Public Sub New()
        Entries = New List(Of PKGEntry)()
    End Sub

    ''' <summary>
    ''' Loads the entry table from the PKG file.
    ''' </summary>
    Public Sub Load(br As BinaryReader, tableOffset As ULong, entryCount As UInteger)
        Entries.Clear()

        ' Validate offset is within file bounds
        If CLng(tableOffset) >= br.BaseStream.Length Then
            Throw New InvalidOperationException(
                $"Entry table offset 0x{tableOffset:X} is beyond file size.")
        End If

        ' Sanity check entry count (avoid allocating millions of entries for corrupt files)
        If entryCount > 10000 Then
            Throw New InvalidOperationException(
                $"Entry count {entryCount} seems too large, file may be corrupt.")
        End If

        br.BaseStream.Seek(CLng(tableOffset), SeekOrigin.Begin)

        ' Each entry is 32 bytes; check we have enough data
        Dim requiredBytes = CLng(entryCount) * PKGConstants.ENTRY_SIZE
        If CLng(tableOffset) + requiredBytes > br.BaseStream.Length Then
            ' Adjust count to what we can actually read
            entryCount = CUInt((br.BaseStream.Length - CLng(tableOffset)) \ PKGConstants.ENTRY_SIZE)
        End If

        For i As UInteger = 0 To entryCount - 1UI
            If br.BaseStream.Position + PKGConstants.ENTRY_SIZE > br.BaseStream.Length Then
                Exit For
            End If

            Dim entry As New PKGEntry()
            entry.Id = ReadUInt32BE(br)
            entry.FilenameOffset = ReadUInt32BE(br)
            entry.Flags1 = ReadUInt32BE(br)
            entry.Flags2 = ReadUInt32BE(br)
            entry.DataOffset = ReadUInt64BE(br)
            entry.DataSize = ReadUInt64BE(br)

            ' Set initial filename from known IDs
            entry.FileName = GetKnownFileName(entry.Id)

            Entries.Add(entry)
        Next
    End Sub

    ''' <summary>
    ''' Resolves filenames from the name table entry (if present).
    ''' Call this after Load().
    ''' </summary>
    Public Sub ResolveFilenames(br As BinaryReader)
        ' Find the name table entry (typically the first entries contain names)
        For Each entry In Entries
            If entry.FilenameOffset > 0 AndAlso String.IsNullOrEmpty(entry.FileName) Then
                Try
                    ' The filename offset is relative to the entry data area
                    ' Try reading from the data offset
                    If entry.DataSize > 0 AndAlso entry.DataSize < 65536 Then
                        br.BaseStream.Seek(CLng(entry.DataOffset), SeekOrigin.Begin)
                        Dim nameBytes = br.ReadBytes(CInt(Math.Min(entry.DataSize, 256)))
                        Dim nullIdx = Array.IndexOf(nameBytes, CByte(0))
                        If nullIdx > 0 Then
                            entry.FileName = Encoding.UTF8.GetString(nameBytes, 0, nullIdx)
                        End If
                    End If
                Catch
                    ' Keep the ID-based name
                End Try
            End If

            ' Final fallback
            If String.IsNullOrEmpty(entry.FileName) Then
                entry.FileName = $"entry_0x{entry.Id:X4}"
            End If
        Next
    End Sub

    ''' <summary>
    ''' Returns a known filename for well-known PKG entry IDs.
    ''' </summary>
    Private Shared Function GetKnownFileName(entryId As UInteger) As String
        Select Case entryId
            Case PKGConstants.ENTRY_ID_PARAM_SFO : Return "param.sfo"
            Case PKGConstants.ENTRY_ID_ICON0_PNG : Return "icon0.png"
            Case PKGConstants.ENTRY_ID_PIC1_PNG : Return "pic1.png"
            Case Else : Return ""
        End Select
    End Function

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
