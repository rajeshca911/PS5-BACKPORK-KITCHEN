Imports System.IO

''' <summary>
''' Main reader for UFS2 filesystem images. Opens an image file, reads the superblock,
''' resolves inodes, builds a file tree, and extracts files.
''' </summary>
Public Class UFS2ImageReader
    Implements IDisposable

    Private _stream As FileStream
    Private _reader As BinaryReader
    Private _superblock As UFS2Superblock
    Private _disposed As Boolean

    Public ReadOnly Property Superblock As UFS2Superblock
        Get
            Return _superblock
        End Get
    End Property

    Public ReadOnly Property ImagePath As String

    ''' <summary>
    ''' Opens a UFS2 image file and parses the superblock.
    ''' </summary>
    Public Sub Open(imagePath As String)
        If Not File.Exists(imagePath) Then
            Throw New FileNotFoundException("Image file not found", imagePath)
        End If

        _ImagePath = imagePath
        _stream = New FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read)
        _reader = New BinaryReader(_stream)

        _superblock = New UFS2Superblock()
        _superblock.Load(_reader)
    End Sub

    ''' <summary>
    ''' Reads an inode by its inode number using the superblock geometry.
    ''' </summary>
    Public Function ReadInode(inodeNumber As UInteger) As UFS2Inode
        Dim offset = GetInodeOffset(inodeNumber)
        Dim inode As New UFS2Inode()
        inode.Load(_reader, offset)
        Return inode
    End Function

    ''' <summary>
    ''' Calculates the absolute byte offset of an inode from its number.
    ''' </summary>
    Private Function GetInodeOffset(inodeNumber As UInteger) As Long
        Dim cgIndex = CInt(inodeNumber) \ _superblock.InodesPerGroup
        Dim inoInGroup = CInt(inodeNumber) Mod _superblock.InodesPerGroup

        ' Cylinder group start offset
        Dim cgStart As Long = CLng(cgIndex) * CLng(_superblock.FragsPerGroup) * CLng(_superblock.FragmentSize)

        ' Inode table offset within cylinder group
        Dim inodeTableOffset As Long = CLng(_superblock.IblkNo) * CLng(_superblock.FragmentSize)

        Return cgStart + inodeTableOffset + (CLng(inoInGroup) * UFS2Constants.INODE_SIZE)
    End Function

    ''' <summary>
    ''' Reads the raw data of an inode (supports direct, single/double/triple indirect blocks).
    ''' </summary>
    Public Function ReadInodeData(inode As UFS2Inode) As Byte()
        If inode.Size <= 0 Then Return Array.Empty(Of Byte)()

        Dim result As New MemoryStream()
        Dim remaining = inode.Size
        Dim fragSize = _superblock.FragmentSize
        Dim pointersPerBlock = _superblock.BlockSize \ 8  ' Int64 pointers

        ' Direct blocks
        For i = 0 To UFS2Constants.DIRECT_BLOCK_COUNT - 1
            If remaining <= 0 Then Exit For
            Dim blk = inode.DirectBlocks(i)
            If blk = 0 Then Continue For

            Dim toRead = CInt(Math.Min(remaining, fragSize))
            _reader.BaseStream.Seek(blk * fragSize, SeekOrigin.Begin)
            Dim data = _reader.ReadBytes(toRead)
            result.Write(data, 0, data.Length)
            remaining -= data.Length
        Next

        ' Single indirect
        If remaining > 0 AndAlso inode.IndirectBlocks(0) <> 0 Then
            ReadIndirectBlocks(inode.IndirectBlocks(0), fragSize, pointersPerBlock, remaining, result, 1)
        End If

        ' Double indirect
        If remaining > 0 AndAlso inode.IndirectBlocks(1) <> 0 Then
            ReadIndirectBlocks(inode.IndirectBlocks(1), fragSize, pointersPerBlock, remaining, result, 2)
        End If

        ' Triple indirect
        If remaining > 0 AndAlso inode.IndirectBlocks(2) <> 0 Then
            ReadIndirectBlocks(inode.IndirectBlocks(2), fragSize, pointersPerBlock, remaining, result, 3)
        End If

        Return result.ToArray()
    End Function

    ''' <summary>
    ''' Recursively reads indirect block chains.
    ''' </summary>
    Private Sub ReadIndirectBlocks(blockNum As Long, fragSize As Integer, pointersPerBlock As Integer,
                                   ByRef remaining As Long, output As MemoryStream, level As Integer)
        If blockNum = 0 OrElse remaining <= 0 Then Return

        ' Read the pointer block
        _reader.BaseStream.Seek(blockNum * fragSize, SeekOrigin.Begin)
        Dim ptrBytes = _reader.ReadBytes(pointersPerBlock * 8)

        For i = 0 To pointersPerBlock - 1
            If remaining <= 0 Then Exit For

            Dim ptr = BitConverter.ToInt64(ptrBytes, i * 8)
            If ptr = 0 Then Continue For

            If level = 1 Then
                ' Direct data block
                Dim toRead = CInt(Math.Min(remaining, fragSize))
                _reader.BaseStream.Seek(ptr * fragSize, SeekOrigin.Begin)
                Dim data = _reader.ReadBytes(toRead)
                output.Write(data, 0, data.Length)
                remaining -= data.Length
            Else
                ' Another level of indirection
                ReadIndirectBlocks(ptr, fragSize, pointersPerBlock, remaining, output, level - 1)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Reads directory entries from a directory inode's data.
    ''' </summary>
    Public Function ReadDirectoryEntries(dirInode As UFS2Inode) As List(Of UFS2DirectoryEntry)
        Dim entries As New List(Of UFS2DirectoryEntry)()
        Dim dirData = ReadInodeData(dirInode)

        Using ms As New MemoryStream(dirData)
            Using br As New BinaryReader(ms)
                While ms.Position < ms.Length
                    Dim entry = UFS2DirectoryEntry.Load(br, ms.Length)
                    If entry Is Nothing Then Exit While
                    If entry.IsValid Then
                        entries.Add(entry)
                    End If
                End While
            End Using
        End Using

        Return entries
    End Function

    ''' <summary>
    ''' Builds the complete file tree starting from the root inode.
    ''' </summary>
    Public Function BuildFileTree(Optional progress As IProgress(Of String) = Nothing) As UFS2FileNode
        Dim visitedInodes As New HashSet(Of UInteger)()
        Return BuildFileTreeRecursive(UFS2Constants.ROOT_INODE, "/", visitedInodes, progress)
    End Function

    Private Function BuildFileTreeRecursive(inodeNumber As UInteger, path As String,
                                             visitedInodes As HashSet(Of UInteger),
                                             progress As IProgress(Of String)) As UFS2FileNode
        ' Prevent infinite loops from circular references
        If visitedInodes.Contains(inodeNumber) Then Return Nothing
        visitedInodes.Add(inodeNumber)

        Dim inode = ReadInode(inodeNumber)
        Dim node As New UFS2FileNode() With {
            .Name = IO.Path.GetFileName(path.TrimEnd("/"c)),
            .FullPath = path,
            .InodeNumber = inodeNumber,
            .Size = inode.Size,
            .IsDirectory = inode.IsDirectory,
            .FileType = inode.FileTypeString,
            .ModifiedDate = inode.ModifiedDate,
            .Mode = inode.Mode
        }

        If String.IsNullOrEmpty(node.Name) Then node.Name = "/"

        If inode.IsDirectory Then
            progress?.Report($"Scanning: {path}")

            Dim entries = ReadDirectoryEntries(inode)
            For Each entry In entries
                If entry.IsDotEntry Then Continue For

                Dim childPath = If(path.EndsWith("/"), path & entry.Name, path & "/" & entry.Name)
                Dim childNode = BuildFileTreeRecursive(entry.InodeNumber, childPath, visitedInodes, progress)
                If childNode IsNot Nothing Then
                    node.Children.Add(childNode)
                End If
            Next
        End If

        Return node
    End Function

    ''' <summary>
    ''' Extracts a single file to the specified output path.
    ''' </summary>
    Public Sub ExtractFile(inodeNumber As UInteger, outputPath As String)
        Dim inode = ReadInode(inodeNumber)
        Dim data = ReadInodeData(inode)

        Dim dir = Path.GetDirectoryName(outputPath)
        If Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If

        File.WriteAllBytes(outputPath, data)
    End Sub

    ''' <summary>
    ''' Extracts all files from the tree to the specified output directory.
    ''' </summary>
    Public Sub ExtractAll(rootNode As UFS2FileNode, outputDir As String,
                          Optional progress As IProgress(Of Integer) = Nothing)
        Dim allFiles = rootNode.GetAllFiles()
        Dim total = allFiles.Count
        Dim current = 0

        For Each fileNode In allFiles
            Dim relativePath = fileNode.FullPath.TrimStart("/"c).Replace("/"c, Path.DirectorySeparatorChar)
            Dim outputPath = Path.Combine(outputDir, relativePath)

            ExtractFile(fileNode.InodeNumber, outputPath)

            current += 1
            progress?.Report(CInt(current * 100 / Math.Max(total, 1)))
        Next
    End Sub

    ''' <summary>
    ''' Reads up to maxBytes from a file's inode data for hex preview purposes.
    ''' </summary>
    Public Function ReadFilePreview(inodeNumber As UInteger, Optional maxBytes As Integer = 4096) As Byte()
        Dim inode = ReadInode(inodeNumber)
        If inode.Size <= 0 Then Return Array.Empty(Of Byte)()

        Dim data = ReadInodeData(inode)
        If data.Length <= maxBytes Then Return data

        Dim preview(maxBytes - 1) As Byte
        Array.Copy(data, preview, maxBytes)
        Return preview
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
