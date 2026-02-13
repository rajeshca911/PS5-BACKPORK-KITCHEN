Imports System.IO
Imports System.Drawing

''' <summary>
''' Main reader for PlayStation PKG/FPKG files.
''' Parses the header, entry table, metadata, and extracts files from FPKGs.
''' </summary>
Public Class PKGReader
    Implements IDisposable

    Private _stream As FileStream
    Private _reader As BinaryReader
    Private _header As PKGHeader
    Private _entryTable As PKGEntryTable
    Private _metadata As PKGMetadata
    Private _disposed As Boolean

    Public ReadOnly Property Header As PKGHeader
        Get
            Return _header
        End Get
    End Property

    Public ReadOnly Property EntryTable As PKGEntryTable
        Get
            Return _entryTable
        End Get
    End Property

    Public ReadOnly Property Metadata As PKGMetadata
        Get
            Return _metadata
        End Get
    End Property

    Public ReadOnly Property FilePath As String

    ''' <summary>
    ''' Opens a PKG file and parses header, entry table, and metadata.
    ''' </summary>
    Public Sub Open(pkgPath As String)
        If Not File.Exists(pkgPath) Then
            Throw New FileNotFoundException("PKG file not found", pkgPath)
        End If

        _FilePath = pkgPath
        _stream = New FileStream(pkgPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        _reader = New BinaryReader(_stream)

        ' Parse header
        _header = New PKGHeader()
        _header.Load(_reader)

        ' Parse entry table
        _entryTable = New PKGEntryTable()
        _entryTable.Load(_reader, _header.TableOffset, _header.EntryCount)
        _entryTable.ResolveFilenames(_reader)

        ' Try to parse param.sfo metadata
        _metadata = Nothing
        TryLoadMetadata()
    End Sub

    ''' <summary>
    ''' Attempts to find and parse the param.sfo entry.
    ''' </summary>
    Private Sub TryLoadMetadata()
        Dim sfoEntry = _entryTable.Entries.FirstOrDefault(
            Function(e) e.Id = PKGConstants.ENTRY_ID_PARAM_SFO)

        If sfoEntry Is Nothing OrElse sfoEntry.DataSize = 0 Then Return

        Try
            Dim data = ReadEntryData(sfoEntry)
            If data IsNot Nothing AndAlso data.Length > 0 Then
                _metadata = New PKGMetadata()
                _metadata.LoadFromBytes(data)
            End If
        Catch
            ' SFO parsing failed - metadata will be Nothing
            _metadata = Nothing
        End Try
    End Sub

    ''' <summary>
    ''' Returns the list of file entries in the PKG.
    ''' </summary>
    Public Function GetFileEntries() As List(Of PKGEntry)
        Return _entryTable.Entries
    End Function

    ''' <summary>
    ''' Reads raw data for a PKG entry. Returns Nothing if encrypted.
    ''' </summary>
    Public Function ReadEntryData(entry As PKGEntry) As Byte()
        If entry.IsEncrypted AndAlso Not _header.IsFPKG Then
            Return Nothing
        End If

        If entry.DataSize = 0 Then Return Array.Empty(Of Byte)()

        _reader.BaseStream.Seek(CLng(entry.DataOffset), SeekOrigin.Begin)
        Return _reader.ReadBytes(CInt(Math.Min(entry.DataSize, Integer.MaxValue)))
    End Function

    ''' <summary>
    ''' Extracts a single entry to the specified output path.
    ''' Returns True on success, False if encrypted.
    ''' </summary>
    Public Function ExtractEntry(entry As PKGEntry, outputPath As String) As Boolean
        Dim data = ReadEntryData(entry)
        If data Is Nothing Then Return False

        Dim dir = Path.GetDirectoryName(outputPath)
        If Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If

        File.WriteAllBytes(outputPath, data)
        Return True
    End Function

    ''' <summary>
    ''' Extracts all extractable entries to the specified output directory.
    ''' </summary>
    Public Sub ExtractAll(outputDir As String, Optional progress As IProgress(Of Integer) = Nothing)
        If Not Directory.Exists(outputDir) Then
            Directory.CreateDirectory(outputDir)
        End If

        Dim total = _entryTable.Entries.Count
        Dim current = 0

        For Each entry In _entryTable.Entries
            Dim fileName = If(String.IsNullOrEmpty(entry.FileName),
                              $"entry_0x{entry.Id:X4}",
                              entry.FileName)

            ' Sanitize filename
            For Each c In Path.GetInvalidFileNameChars()
                fileName = fileName.Replace(c, "_"c)
            Next

            Dim outputPath = Path.Combine(outputDir, fileName)
            ExtractEntry(entry, outputPath)

            current += 1
            progress?.Report(CInt(current * 100 / Math.Max(total, 1)))
        Next
    End Sub

    ''' <summary>
    ''' Attempts to load the icon0.png image from the PKG.
    ''' Returns Nothing if not found or encrypted.
    ''' </summary>
    Public Function GetIcon() As Image
        Dim iconEntry = _entryTable.Entries.FirstOrDefault(
            Function(e) e.Id = PKGConstants.ENTRY_ID_ICON0_PNG)

        If iconEntry Is Nothing Then Return Nothing

        Try
            Dim data = ReadEntryData(iconEntry)
            If data Is Nothing OrElse data.Length = 0 Then Return Nothing

            Using ms As New MemoryStream(data)
                Return Image.FromStream(ms)
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ' ---- IDisposable ----

    Public Sub Dispose() Implements IDisposable.Dispose
        If Not _disposed Then
            _reader?.Close()
            _stream?.Close()
            _disposed = True
        End If
    End Sub

End Class
