Imports System.IO

''' <summary>
''' Represents a UFS2 inode (256 bytes). Contains file metadata and block pointers.
''' </summary>
Public Class UFS2Inode

    ' ---- Inode Fields ----
    Public Property Mode As UShort           ' File type and permissions
    Public Property NLink As Short           ' Number of hard links
    Public Property Uid As UInteger          ' Owner user ID
    Public Property Gid As UInteger          ' Owner group ID
    Public Property BlkSize As UInteger      ' Block size for this file
    Public Property Size As Long             ' File size in bytes
    Public Property Blocks As Long           ' Blocks allocated (512-byte units)
    Public Property ATime As Long            ' Access time (Unix epoch)
    Public Property MTime As Long            ' Modification time (Unix epoch)
    Public Property CTime As Long            ' Change time (Unix epoch)
    Public Property BirthTime As Long        ' Creation time (Unix epoch)
    Public Property Flags As UInteger        ' File flags
    Public Property Gen As UInteger          ' Generation number

    ' 12 direct block pointers + 3 indirect block pointers (all Int64 in UFS2)
    Public Property DirectBlocks As Long()   ' DB[0..11]
    Public Property IndirectBlocks As Long() ' IB[0..2] (single, double, triple)

    ''' <summary>
    ''' Reads an inode at the specified absolute byte offset.
    ''' </summary>
    Public Sub Load(br As BinaryReader, offset As Long)
        br.BaseStream.Seek(offset, SeekOrigin.Begin)

        Mode = br.ReadUInt16()
        NLink = br.ReadInt16()
        ' 4 bytes: uid
        Uid = br.ReadUInt32()
        ' 4 bytes: gid
        Gid = br.ReadUInt32()
        ' 4 bytes: blksize
        BlkSize = br.ReadUInt32()

        ' Size (8 bytes)
        Size = br.ReadInt64()
        ' Blocks (8 bytes)
        Blocks = br.ReadInt64()

        ' Timestamps: atime_sec(8) + atime_nsec(4)
        ATime = br.ReadInt64()
        br.ReadInt32() ' atime nsec

        ' mtime_sec(8) + mtime_nsec(4)
        MTime = br.ReadInt64()
        br.ReadInt32() ' mtime nsec

        ' ctime_sec(8) + ctime_nsec(4)
        CTime = br.ReadInt64()
        br.ReadInt32() ' ctime nsec

        ' birthtime_sec(8) + birthtime_nsec(4)
        BirthTime = br.ReadInt64()
        br.ReadInt32() ' birthtime nsec

        ' gen(4) + kernflags(4) + flags(4)
        Gen = br.ReadUInt32()
        br.ReadUInt32() ' kernflags
        Flags = br.ReadUInt32()

        ' extsize(4)
        br.ReadInt32()

        ' extb[2] - 2 x Int64
        br.ReadInt64()
        br.ReadInt64()

        ' Direct block pointers: 12 x Int64
        DirectBlocks = New Long(UFS2Constants.DIRECT_BLOCK_COUNT - 1) {}
        For i = 0 To UFS2Constants.DIRECT_BLOCK_COUNT - 1
            DirectBlocks(i) = br.ReadInt64()
        Next

        ' Indirect block pointers: 3 x Int64
        IndirectBlocks = New Long(UFS2Constants.INDIRECT_LEVELS - 1) {}
        For i = 0 To UFS2Constants.INDIRECT_LEVELS - 1
            IndirectBlocks(i) = br.ReadInt64()
        Next
    End Sub

    ''' <summary>
    ''' True if this inode represents a directory.
    ''' </summary>
    Public ReadOnly Property IsDirectory As Boolean
        Get
            Return (Mode And UFS2Constants.S_IFMT) = UFS2Constants.S_IFDIR
        End Get
    End Property

    ''' <summary>
    ''' True if this inode represents a regular file.
    ''' </summary>
    Public ReadOnly Property IsRegularFile As Boolean
        Get
            Return (Mode And UFS2Constants.S_IFMT) = UFS2Constants.S_IFREG
        End Get
    End Property

    ''' <summary>
    ''' True if this inode represents a symbolic link.
    ''' </summary>
    Public ReadOnly Property IsSymlink As Boolean
        Get
            Return (Mode And UFS2Constants.S_IFMT) = UFS2Constants.S_IFLNK
        End Get
    End Property

    ''' <summary>
    ''' Returns the file type string based on mode bits.
    ''' </summary>
    Public ReadOnly Property FileTypeString As String
        Get
            Dim fmt = Mode And UFS2Constants.S_IFMT
            Select Case fmt
                Case UFS2Constants.S_IFREG : Return "File"
                Case UFS2Constants.S_IFDIR : Return "Directory"
                Case UFS2Constants.S_IFLNK : Return "Symlink"
                Case UFS2Constants.S_IFBLK : Return "Block Device"
                Case UFS2Constants.S_IFCHR : Return "Char Device"
                Case UFS2Constants.S_IFIFO : Return "FIFO"
                Case Else : Return $"Unknown (0x{fmt:X4})"
            End Select
        End Get
    End Property

    ''' <summary>
    ''' Returns the modification time as a DateTime.
    ''' </summary>
    Public ReadOnly Property ModifiedDate As DateTime
        Get
            If MTime = 0 Then Return DateTime.MinValue
            Return DateTimeOffset.FromUnixTimeSeconds(MTime).LocalDateTime
        End Get
    End Property

End Class
