Imports System.IO
Imports System.Text

''' <summary>
''' Configuration for building an FPKG file.
''' </summary>
Public Class FPKGConfig
    Public Property ContentId As String = ""
    Public Property Title As String = ""
    Public Property TitleId As String = ""
    Public Property ContentType As UInteger = PKGConstants.CONTENT_TYPE_GD
    Public Property AppVersion As String = "01.00"
    Public Property Version As String = "01.00"
    Public Property Category As String = "gd"
    Public Property IconPath As String = ""
    Public Property BackgroundPath As String = ""
End Class

''' <summary>
''' Reports build progress to the UI.
''' </summary>
Public Class BuildProgress
    Public Property Stage As String = ""
    Public Property PercentComplete As Integer = 0
    Public Property CurrentFile As String = ""
End Class

''' <summary>
''' Represents a file entry to be included in the FPKG.
''' </summary>
Friend Class FPKGFileEntry
    Public Property Id As UInteger
    Public Property FileName As String
    Public Property FilePath As String     ' source file on disk (Nothing for generated entries)
    Public Property Data As Byte()        ' in-memory data (for param.sfo, etc.)
    Public Property FileSize As ULong
    Public Property FilenameOffset As UInteger
    Public Property DataOffset As ULong
End Class

''' <summary>
''' Assembles a complete FPKG from a source folder and configuration.
''' Uses PKGBinaryWriter for all big-endian output.
''' Streams file data in 64KB chunks for large files.
''' </summary>
Public Class FPKGBuilder

    Private Const STREAM_BUFFER_SIZE As Integer = 65536  ' 64KB chunks
    Private Const DATA_ALIGNMENT As Integer = 16         ' 16-byte alignment for file data
    Private Const USER_ENTRY_START_ID As UInteger = &H2000

    ''' <summary>
    ''' Builds an FPKG from the specified source folder.
    ''' </summary>
    Public Sub Build(sourceFolder As String, outputPath As String,
                     config As FPKGConfig, Optional progress As IProgress(Of BuildProgress) = Nothing)

        ' Step 1: Collect entries
        progress?.Report(New BuildProgress With {.Stage = "Collecting entries...", .PercentComplete = 0})
        Dim entries = CollectEntries(sourceFolder, config)

        If entries.Count = 0 Then
            Throw New InvalidOperationException("No files found to package.")
        End If

        ' Step 2: Build name table
        progress?.Report(New BuildProgress With {.Stage = "Building name table...", .PercentComplete = 5})
        Dim nameTable = BuildNameTable(entries)

        ' Step 3: Calculate layout
        progress?.Report(New BuildProgress With {.Stage = "Calculating layout...", .PercentComplete = 10})
        CalculateLayout(entries, nameTable)

        ' Step 4-7: Write output
        Dim outputDir = Path.GetDirectoryName(outputPath)
        If Not String.IsNullOrEmpty(outputDir) AndAlso Not Directory.Exists(outputDir) Then
            Directory.CreateDirectory(outputDir)
        End If

        Using fs As New FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None)
            Using writer As New PKGBinaryWriter(fs)

                ' Step 4: Write header
                progress?.Report(New BuildProgress With {.Stage = "Writing header...", .PercentComplete = 15})
                WriteHeader(writer, entries, config)

                ' Step 5: Write entry table
                progress?.Report(New BuildProgress With {.Stage = "Writing entry table...", .PercentComplete = 20})
                WriteEntryTable(writer, entries)

                ' Step 6: Write name table
                progress?.Report(New BuildProgress With {.Stage = "Writing name table...", .PercentComplete = 25})
                writer.SeekTo(CLng(PKGConstants.HEADER_SIZE) + entries.Count * PKGConstants.ENTRY_SIZE)
                writer.WriteBytes(nameTable)

                ' Step 7: Write file data (streamed)
                progress?.Report(New BuildProgress With {.Stage = "Writing file data...", .PercentComplete = 30})
                WriteFileData(writer, entries, progress)
            End Using
        End Using

        progress?.Report(New BuildProgress With {.Stage = "Build complete", .PercentComplete = 100})
    End Sub

    ''' <summary>
    ''' Collects all file entries to be packaged.
    ''' </summary>
    Private Function CollectEntries(sourceFolder As String, config As FPKGConfig) As List(Of FPKGFileEntry)
        Dim entries As New List(Of FPKGFileEntry)()

        ' Entry 1: param.sfo (generated)
        Dim sfoParams = SFOBuilder.CreateDefaultParams(
            config.ContentId, config.Title, config.TitleId,
            config.AppVersion, config.Version, config.Category)
        Dim sfoData = SFOBuilder.Build(sfoParams)

        entries.Add(New FPKGFileEntry With {
            .Id = PKGConstants.ENTRY_ID_PARAM_SFO,
            .FileName = "param.sfo",
            .Data = sfoData,
            .FileSize = CULng(sfoData.Length)
        })

        ' Entry 2: icon0.png (optional)
        Dim iconPath = config.IconPath
        If String.IsNullOrEmpty(iconPath) Then
            iconPath = Path.Combine(sourceFolder, "icon0.png")
        End If
        If File.Exists(iconPath) Then
            Dim iconInfo As New FileInfo(iconPath)
            entries.Add(New FPKGFileEntry With {
                .Id = PKGConstants.ENTRY_ID_ICON0_PNG,
                .FileName = "icon0.png",
                .FilePath = iconPath,
                .FileSize = CULng(iconInfo.Length)
            })
        End If

        ' Entry 3: pic1.png (optional)
        Dim bgPath = config.BackgroundPath
        If String.IsNullOrEmpty(bgPath) Then
            bgPath = Path.Combine(sourceFolder, "pic1.png")
        End If
        If File.Exists(bgPath) Then
            Dim bgInfo As New FileInfo(bgPath)
            entries.Add(New FPKGFileEntry With {
                .Id = PKGConstants.ENTRY_ID_PIC1_PNG,
                .FileName = "pic1.png",
                .FilePath = bgPath,
                .FileSize = CULng(bgInfo.Length)
            })
        End If

        ' User files (all files in source folder, excluding icon0/pic1 already added)
        Dim userFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
        Dim nextId = USER_ENTRY_START_ID
        Dim addedPaths As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        ' Track already-added files
        If File.Exists(iconPath) Then addedPaths.Add(Path.GetFullPath(iconPath))
        If File.Exists(bgPath) Then addedPaths.Add(Path.GetFullPath(bgPath))

        For Each filePath In userFiles.OrderBy(Function(f) f)
            Dim fullPath = Path.GetFullPath(filePath)
            If addedPaths.Contains(fullPath) Then Continue For

            Dim relativePath = GetRelativePath(sourceFolder, filePath)
            Dim fInfo As New FileInfo(filePath)

            entries.Add(New FPKGFileEntry With {
                .Id = nextId,
                .FileName = relativePath.Replace(Path.DirectorySeparatorChar, "/"c),
                .FilePath = filePath,
                .FileSize = CULng(fInfo.Length)
            })

            nextId += 1UI
        Next

        Return entries
    End Function

    ''' <summary>
    ''' Builds the name table: null-terminated filenames concatenated.
    ''' Sets FilenameOffset on each entry.
    ''' </summary>
    Private Function BuildNameTable(entries As List(Of FPKGFileEntry)) As Byte()
        Using ms As New MemoryStream()
            For Each entry In entries
                entry.FilenameOffset = CUInt(ms.Position)
                Dim nameBytes = Encoding.UTF8.GetBytes(entry.FileName)
                ms.Write(nameBytes, 0, nameBytes.Length)
                ms.WriteByte(0) ' null terminator
            Next
            Return ms.ToArray()
        End Using
    End Function

    ''' <summary>
    ''' Calculates the data offset for each entry based on the header, entry table, and name table sizes.
    ''' </summary>
    Private Sub CalculateLayout(entries As List(Of FPKGFileEntry), nameTable As Byte())
        ' Data starts after: header (0x2000) + entry table (N*32) + name table + alignment
        Dim entryTableSize = CLng(entries.Count) * PKGConstants.ENTRY_SIZE
        Dim afterNameTable = CLng(PKGConstants.HEADER_SIZE) + entryTableSize + nameTable.Length

        ' Align to 16 bytes
        Dim dataStart = AlignValue(afterNameTable, DATA_ALIGNMENT)

        Dim currentOffset = dataStart
        For Each entry In entries
            entry.DataOffset = CULng(currentOffset)
            currentOffset += CLng(entry.FileSize)
            ' Align next entry to 16 bytes
            currentOffset = AlignValue(currentOffset, DATA_ALIGNMENT)
        Next
    End Sub

    ''' <summary>
    ''' Writes the PKG header at offset 0x0.
    ''' </summary>
    Private Sub WriteHeader(writer As PKGBinaryWriter, entries As List(Of FPKGFileEntry),
                            config As FPKGConfig)
        writer.SeekTo(0)

        ' Magic at 0x00
        writer.WriteUInt32BE(PKGConstants.PKG_MAGIC)

        ' Padding to 0x08
        writer.WritePadding(4)

        ' Flags at 0x08
        writer.WriteUInt32BE(0)

        ' Padding to 0x10
        writer.WritePadding(4)

        ' Entry count at 0x10
        writer.WriteUInt32BE(CUInt(entries.Count))

        ' Padding at 0x14
        writer.WritePadding(4)

        ' Table offset at 0x18 (entry table starts at 0x2000)
        writer.WriteUInt64BE(CULng(PKGConstants.HEADER_SIZE))

        ' Entry data size at 0x20
        Dim entryDataSize = CLng(entries.Count) * PKGConstants.ENTRY_SIZE
        writer.WriteUInt64BE(CULng(entryDataSize))

        ' Body offset at 0x28 (first file data offset)
        Dim bodyOffset = If(entries.Count > 0, entries(0).DataOffset, CULng(PKGConstants.HEADER_SIZE))
        writer.WriteUInt64BE(bodyOffset)

        ' Body size at 0x30 (total size of all file data)
        Dim lastEntry = entries.Last()
        Dim bodySize = (lastEntry.DataOffset + lastEntry.FileSize) - bodyOffset
        writer.WriteUInt64BE(bodySize)

        ' Skip to 0x40 for Content ID (48 bytes ASCII, null-padded)
        writer.SeekTo(&H40)
        Dim contentIdBytes = New Byte(47) {}
        Dim cidBytes = Encoding.ASCII.GetBytes(If(config.ContentId, ""))
        Array.Copy(cidBytes, contentIdBytes, Math.Min(cidBytes.Length, 48))
        writer.WriteBytes(contentIdBytes)

        ' DRM type at 0x70 (0 = FPKG, no DRM)
        writer.SeekTo(&H70)
        writer.WriteUInt32BE(PKGConstants.DRM_TYPE_NONE)

        ' Content type at 0x74
        writer.WriteUInt32BE(config.ContentType)

        ' Content flags at 0x78
        writer.WriteUInt32BE(0)

        ' Pad rest of header to HEADER_SIZE
        Dim remaining = PKGConstants.HEADER_SIZE - CInt(writer.BaseStream.Position)
        If remaining > 0 Then
            writer.WritePadding(remaining)
        End If
    End Sub

    ''' <summary>
    ''' Writes the entry table starting at TableOffset (0x2000).
    ''' Each entry is 32 bytes, all fields big-endian.
    ''' </summary>
    Private Sub WriteEntryTable(writer As PKGBinaryWriter, entries As List(Of FPKGFileEntry))
        writer.SeekTo(PKGConstants.HEADER_SIZE)

        For Each entry In entries
            writer.WriteUInt32BE(entry.Id)              ' Entry ID (4)
            writer.WriteUInt32BE(entry.FilenameOffset)  ' Filename offset (4)
            writer.WriteUInt32BE(0)                     ' Flags1 (4)
            writer.WriteUInt32BE(0)                     ' Flags2 (4)
            writer.WriteUInt64BE(entry.DataOffset)      ' Data offset (8)
            writer.WriteUInt64BE(entry.FileSize)        ' Data size (8)
        Next
    End Sub

    ''' <summary>
    ''' Writes file data for all entries, streaming large files in 64KB chunks.
    ''' </summary>
    Private Sub WriteFileData(writer As PKGBinaryWriter, entries As List(Of FPKGFileEntry),
                              progress As IProgress(Of BuildProgress))
        Dim totalEntries = entries.Count
        Dim buffer(STREAM_BUFFER_SIZE - 1) As Byte

        For i = 0 To entries.Count - 1
            Dim entry = entries(i)

            ' Seek to data offset
            writer.SeekTo(CLng(entry.DataOffset))

            ' Report progress
            Dim pct = 30 + CInt(i * 70.0 / Math.Max(totalEntries, 1))
            progress?.Report(New BuildProgress With {
                .Stage = "Writing file data...",
                .PercentComplete = pct,
                .CurrentFile = entry.FileName
            })

            If entry.Data IsNot Nothing Then
                ' In-memory data (param.sfo, etc.)
                writer.WriteBytes(entry.Data)
            ElseIf Not String.IsNullOrEmpty(entry.FilePath) Then
                ' Stream from disk in chunks
                Using fs As New FileStream(entry.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim bytesRead As Integer
                    Do
                        bytesRead = fs.Read(buffer, 0, buffer.Length)
                        If bytesRead > 0 Then
                            writer.BaseStream.Write(buffer, 0, bytesRead)
                        End If
                    Loop While bytesRead > 0
                End Using
            End If
        Next
    End Sub

    ''' <summary>
    ''' Gets the relative path from a base directory to a file.
    ''' </summary>
    Private Shared Function GetRelativePath(basePath As String, fullPath As String) As String
        Dim baseUri As New Uri(If(basePath.EndsWith(Path.DirectorySeparatorChar),
                                  basePath,
                                  basePath & Path.DirectorySeparatorChar))
        Dim fileUri As New Uri(fullPath)
        Dim relUri = baseUri.MakeRelativeUri(fileUri)
        Return Uri.UnescapeDataString(relUri.ToString()).Replace("/"c, Path.DirectorySeparatorChar)
    End Function

    ''' <summary>
    ''' Aligns a value up to the specified boundary.
    ''' </summary>
    Private Shared Function AlignValue(value As Long, alignment As Integer) As Long
        Dim remainder = value Mod alignment
        If remainder = 0 Then Return value
        Return value + (alignment - remainder)
    End Function

End Class
