Imports System.IO

''' <summary>
''' Constants for the UFS2 (Unix File System 2) binary format used in PS5 images.
''' </summary>
Public Module UFS2Constants

    ' ---- Superblock ----
    Public Const SUPERBLOCK_OFFSET As Long = &H10000L
    Public Const UFS2_MAGIC As UInteger = &H19540119UI
    Public Const SUPERBLOCK_SIZE As Integer = 1376

    ' ---- Inode ----
    Public Const INODE_SIZE As Integer = 256
    Public Const DIRECT_BLOCK_COUNT As Integer = 12
    Public Const INDIRECT_LEVELS As Integer = 3

    ' ---- Directory Entry ----
    Public Const DT_UNKNOWN As Byte = 0
    Public Const DT_FIFO As Byte = 1
    Public Const DT_CHR As Byte = 2
    Public Const DT_DIR As Byte = 4
    Public Const DT_BLK As Byte = 6
    Public Const DT_REG As Byte = 8
    Public Const DT_LNK As Byte = 10
    Public Const DT_SOCK As Byte = 12
    Public Const DT_WHT As Byte = 14

    ' ---- Inode Mode Bits ----
    Public Const S_IFMT As UShort = &HF000US    ' Format mask
    Public Const S_IFREG As UShort = &H8000US   ' Regular file
    Public Const S_IFDIR As UShort = &H4000US   ' Directory
    Public Const S_IFLNK As UShort = &HA000US   ' Symbolic link
    Public Const S_IFBLK As UShort = &H6000US   ' Block device
    Public Const S_IFCHR As UShort = &H2000US   ' Character device
    Public Const S_IFIFO As UShort = &H1000US   ' FIFO

    ' ---- Superblock Field Offsets (relative to superblock start) ----
    Public Const SB_SBLKNO_OFFSET As Integer = &H18      ' superblock blkno in cg
    Public Const SB_CBLKNO_OFFSET As Integer = &H1C      ' cg block blkno in cg
    Public Const SB_IBLKNO_OFFSET As Integer = &H20      ' inode block blkno in cg
    Public Const SB_FRAG_OFFSET As Integer = &H60         ' fragment size
    Public Const SB_BSIZE_OFFSET As Integer = &H64        ' block size
    Public Const SB_FSIZE_OFFSET As Integer = &H68        ' frag size
    Public Const SB_FPGRP_OFFSET As Integer = &H78        ' frags per group
    Public Const SB_IPGRP_OFFSET As Integer = &H80        ' inodes per group
    Public Const SB_MAGIC_OFFSET As Integer = &H55C       ' magic number

    ' ---- Root Inode ----
    Public Const ROOT_INODE As UInteger = 2

    ''' <summary>
    ''' Returns the human-readable name for a directory entry type byte.
    ''' </summary>
    Public Function GetDirectoryEntryTypeName(entryType As Byte) As String
        Select Case entryType
            Case DT_REG : Return "File"
            Case DT_DIR : Return "Directory"
            Case DT_LNK : Return "Symlink"
            Case DT_CHR : Return "CharDev"
            Case DT_BLK : Return "BlockDev"
            Case DT_FIFO : Return "FIFO"
            Case DT_SOCK : Return "Socket"
            Case DT_WHT : Return "Whiteout"
            Case Else : Return "Unknown"
        End Select
    End Function

End Module
