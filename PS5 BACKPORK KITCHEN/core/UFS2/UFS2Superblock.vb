Imports System.IO

''' <summary>
''' Parses the UFS2 superblock structure from a disk image.
''' The superblock sits at offset 0x10000 and contains filesystem geometry.
''' </summary>
Public Class UFS2Superblock

    ' ---- Key Geometry Fields ----
    Public Property Magic As UInteger
    Public Property BlockSize As Integer
    Public Property FragmentSize As Integer
    Public Property FragsPerGroup As Integer
    Public Property InodesPerGroup As Integer
    Public Property SblkNo As Integer         ' superblock blkno in cg
    Public Property CblkNo As Integer         ' cg block blkno in cg
    Public Property IblkNo As Integer         ' inode block blkno in cg
    Public Property CgSize As Integer         ' cylinder group count
    Public Property Ncg As Integer            ' number of cylinder groups
    Public Property DataOffset As Long        ' offset to first data block

    ''' <summary>
    ''' Reads the superblock from the given BinaryReader.
    ''' The reader should be positioned at the start of the image (offset 0).
    ''' </summary>
    Public Sub Load(br As BinaryReader)
        ' Seek to superblock offset
        br.BaseStream.Seek(UFS2Constants.SUPERBLOCK_OFFSET, SeekOrigin.Begin)

        ' Read raw superblock bytes
        Dim sbBytes = br.ReadBytes(UFS2Constants.SUPERBLOCK_SIZE)

        ' Validate magic at offset 0x55C within superblock
        Magic = BitConverter.ToUInt32(sbBytes, UFS2Constants.SB_MAGIC_OFFSET)
        If Magic <> UFS2Constants.UFS2_MAGIC Then
            Throw New InvalidOperationException(
                $"Invalid UFS2 magic: expected 0x{UFS2Constants.UFS2_MAGIC:X8}, got 0x{Magic:X8}")
        End If

        ' Parse geometry
        SblkNo = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_SBLKNO_OFFSET)
        CblkNo = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_CBLKNO_OFFSET)
        IblkNo = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_IBLKNO_OFFSET)
        FragmentSize = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_FSIZE_OFFSET)
        BlockSize = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_BSIZE_OFFSET)
        FragsPerGroup = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_FPGRP_OFFSET)
        InodesPerGroup = BitConverter.ToInt32(sbBytes, UFS2Constants.SB_IPGRP_OFFSET)

        ' Number of cylinder groups is at offset 0xAC
        Ncg = BitConverter.ToInt32(sbBytes, &HAC)

        ' Validate sensible ranges
        If FragmentSize <= 0 OrElse FragmentSize > 65536 Then
            Throw New InvalidOperationException($"Invalid fragment size: {FragmentSize}")
        End If
        If BlockSize <= 0 OrElse BlockSize > 65536 Then
            Throw New InvalidOperationException($"Invalid block size: {BlockSize}")
        End If
        If InodesPerGroup <= 0 Then
            Throw New InvalidOperationException($"Invalid inodes per group: {InodesPerGroup}")
        End If
    End Sub

    ''' <summary>
    ''' Returns a formatted summary of the superblock for display.
    ''' </summary>
    Public Function ToSummary() As String
        Return $"UFS2 Superblock:" & Environment.NewLine &
               $"  Magic:           0x{Magic:X8}" & Environment.NewLine &
               $"  Block Size:      {BlockSize}" & Environment.NewLine &
               $"  Fragment Size:   {FragmentSize}" & Environment.NewLine &
               $"  Frags/Group:     {FragsPerGroup}" & Environment.NewLine &
               $"  Inodes/Group:    {InodesPerGroup}" & Environment.NewLine &
               $"  Cylinder Groups: {Ncg}" & Environment.NewLine &
               $"  IblkNo:          {IblkNo}"
    End Function

End Class
