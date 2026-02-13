Imports System.IO

Public Class SignedElfFile
    Public Elf As ElfFile
    Private VersionData As Byte()

    ' ---- SELF header fields ----
    Public Magic As Byte()

    Public Version As Byte
    Public Mode As Byte
    Public Endian As Byte
    Public Attribs As Byte
    Public KeyType As UInteger

    Public HeaderSize As UInteger
    Public MetaSize As UInteger
    Public FileSize As ULong
    Public NumEntries As UInteger
    Public Flags As UInteger

    ' ---- SELF contents ----
    Public Entries As New List(Of SignedElfEntry)

    Public ExInfo As SignedElfExInfo
    Public Npdrm As NpdrmControlBlock
    Public MetaBlocks As New List(Of SignedElfMetaBlock)
    Public MetaFooter As SignedElfMetaFooter
    Public Signature As Byte()

    ' ---- parameters ----
    Public Paid As ULong

    Public PType As ULong
    Public AppVersion As ULong
    Public FwVersion As ULong
    Public AuthInfo As Byte()

    Public Sub New(elfFile As ElfFile,
                   Optional paid As ULong = &H3100000000000002UL,
                   Optional ptype As ULong = SignedElfExInfo.PTYPE_FAKE,
                   Optional appVersion As ULong = 0,
                   Optional fwVersion As ULong = 0,
                   Optional authInfo As Byte() = Nothing)

        Elf = elfFile
        Me.Paid = paid
        Me.PType = ptype
        Me.AppVersion = appVersion
        Me.FwVersion = fwVersion
        Me.AuthInfo = authInfo
    End Sub

    Public Sub Prepare()

        ' ---- SELF fixed header ----
        Magic = SelfConstants.SELF_MAGIC
        Version = SelfConstants.SELF_VERSION
        Mode = SelfConstants.SELF_MODE
        Endian = SelfConstants.SELF_ENDIAN
        Attribs = SelfConstants.SELF_ATTRIBS
        KeyType = SelfConstants.SELF_KEY_TYPE

        ' ---- Flags ----
        Flags = &H2UI
        Dim signedBlockCount As Integer = 2
        Flags = Flags Or (signedBlockCount << SelfConstants.FLAGS_SEGMENT_SIGNED_SHIFT)

        ' ---- Build entries ----
        Entries.Clear()
        Dim entryIndex As Integer = 0

        For i = 0 To Elf.ProgramHeaders.Count - 1

            Dim ph = Elf.ProgramHeaders(i)

            If ph.Type <> PT_LOAD AndAlso
               ph.Type <> PT_SCE_RELRO AndAlso
               ph.Type <> PT_SCE_DYNLIBDATA AndAlso
               ph.Type <> PT_SCE_COMMENT Then
                Continue For
            End If

            ' ---- META entry ----
            Dim metaEntry As New SignedElfEntry(entryIndex)
            metaEntry.Signed = True
            metaEntry.HasDigests = True
            metaEntry.SegmentIndex = entryIndex + 1
            Entries.Add(metaEntry)

            ' ---- DATA entry ----
            Dim dataEntry As New SignedElfEntry(entryIndex + 1)
            dataEntry.Signed = True
            dataEntry.HasBlocks = True
            dataEntry.BlockSize = SelfConstants.BLOCK_SIZE
            dataEntry.SegmentIndex = i
            Entries.Add(dataEntry)

            entryIndex += 2
        Next

        NumEntries = CUInt(Entries.Count)
#If DEBUG Then
        Debug.WriteLine("===== ENTRY PROPS =====")
        For i = 0 To Entries.Count - 1
            Debug.WriteLine($"Entry[{i}] Props=0x{Entries(i).Props:X}")
        Next
        Debug.WriteLine("=======================")
#End If

        ' ---- ExInfo ----
        ExInfo = New SignedElfExInfo() With {
            .Paid = Paid,
            .PType = PType,
            .AppVersion = AppVersion,
            .FwVersion = FwVersion,
            .Digest = Elf.Digest
        }

        ' ---- NPDRM ----
        Npdrm = New NpdrmControlBlock() With {
            .ContentId = New Byte(&H12) {},
            .RandomPad = New Byte(&HC) {}
        }

        'HeaderSize =
        '    CUInt(4 + 4 + ' common header
        '    Entries.Count * 32 + ' SignedElfEntry (4Q)
        '    Math.Max(Elf.Header.EhSize,
        '             Elf.Header.Phoff + Elf.Header.PhEntSize * Elf.Header.PhNum))

        'HeaderSize = CUInt(BinaryHelpers.AlignUp(HeaderSize, 16))

        'HeaderSize += 64 ' ExInfo
        'HeaderSize += 48 ' NPDRM
        Dim COMMON_HEADER_SIZE As Integer = 8      ' <4s4B
        Dim EXT_HEADER_SIZE As Integer = 20        ' <I2HQ2H4x>

        HeaderSize =
    CUInt(COMMON_HEADER_SIZE +
          EXT_HEADER_SIZE +
          Entries.Count * 32 +
          Math.Max(
              Elf.Header.EhSize,
              Elf.Header.Phoff + Elf.Header.PhEntSize * Elf.Header.PhNum))

        HeaderSize = CUInt(BinaryHelpers.AlignUp(HeaderSize, 16))

        HeaderSize += 64  ' ExInfo
        HeaderSize += 48  ' NPDRM

        ' ---- Meta blocks ----
        MetaBlocks.Clear()
        For i = 0 To Entries.Count - 1
            MetaBlocks.Add(New SignedElfMetaBlock())
        Next

        MetaFooter = New SignedElfMetaFooter() With {
            .Unknown1 = &H10000UI
        }

        MetaSize = CUInt(
            Entries.Count * 80 + ' meta blocks
            80 +                ' footer
            SelfConstants.SIGNATURE_SIZE)

        ' ---- Signature ----
        If AuthInfo IsNot Nothing Then
            Signature = New Byte(SelfConstants.SIGNATURE_SIZE - 1) {}
        Else
            Signature = SelfConstants.EMPTY_SIGNATURE
        End If

        ' ---- Offsets & data ----
        Dim offset As ULong = HeaderSize + MetaSize
        Dim eIdx As Integer = 0

        For i = 0 To Elf.ProgramHeaders.Count - 1

            Dim ph = Elf.ProgramHeaders(i)
            ' ---- Capture version segment (OUTSIDE filter) ----
            If ph.Type = PT_SCE_VERSION Then
                VersionData = Elf.Segments(i)
                '#If DEBUG Then
                '                Debug.WriteLine($"FOUND VERSION SEGMENT: {VersionData.Length} bytes")
                '#End If
            End If
            '#If DEBUG Then
            '            Debug.WriteLine($"VersionData MD5 = {ComputeMD5(VersionData)}")
            '#End If

            If ph.Type <> PT_LOAD AndAlso
               ph.Type <> PT_SCE_RELRO AndAlso
               ph.Type <> PT_SCE_DYNLIBDATA AndAlso
               ph.Type <> PT_SCE_COMMENT Then
                Continue For
            End If

            Dim metaEntry = Entries(eIdx)
            Dim dataEntry = Entries(eIdx + 1)

            Dim numBlocks = BinaryHelpers.AlignUp(ph.FileSize, SelfConstants.BLOCK_SIZE) \ SelfConstants.BLOCK_SIZE

            metaEntry.Data = Enumerable.Repeat(Of Byte)(0, numBlocks * SelfConstants.DIGEST_SIZE).ToArray()
            metaEntry.Offset = offset
            metaEntry.FileSize = CULng(metaEntry.Data.Length)
            metaEntry.MemSize = metaEntry.FileSize

            offset = BinaryHelpers.AlignUp(offset + metaEntry.FileSize, 16)

            dataEntry.Data = Elf.Segments(i)
            dataEntry.Offset = offset
            dataEntry.FileSize = CULng(ph.FileSize)
            dataEntry.MemSize = dataEntry.FileSize

            offset = BinaryHelpers.AlignUp(offset + dataEntry.FileSize, 16)

            eIdx += 2
            If ph.Type = PT_SCE_VERSION Then
                VersionData = Elf.Segments(i)

                Debug.WriteLine($"FOUND VERSION SEGMENT: {VersionData.Length} bytes")

            End If

        Next

        FileSize = offset
        Debug.WriteLine("===== SIGN PREPARE =====")
        Debug.WriteLine($"Entries.Count   = {Entries.Count}")
        Debug.WriteLine($"HeaderSize      = {HeaderSize}")
        Debug.WriteLine($"MetaSize        = {MetaSize}")
        Debug.WriteLine($"FileSize        = {FileSize}")
        Debug.WriteLine($"SignatureSize   = {Signature.Length}")
        Debug.WriteLine($"FW Version      = {FwVersion}")
        Debug.WriteLine("========================")

    End Sub

    Public Sub Save(outputPath As String)

        Using fs As New FileStream(outputPath, FileMode.Create, FileAccess.Write)
            Using bw As New BinaryWriter(fs)

                ' ---- ensure prepared ----
                Prepare()

                Dim baseOffset As Long = fs.Position

                ' COMMON SELF HEADER

                bw.Write(Magic)
                bw.Write(Version)
                bw.Write(Mode)
                bw.Write(Endian)
                bw.Write(Attribs)
                Debug.WriteLine($"FLAGS (hex) = 0x{Flags:X}")

                ' EXTENDED HEADER

                'bw.Write(KeyType)
                'bw.Write(HeaderSize)
                'bw.Write(MetaSize)
                'bw.Write(FileSize)
                'bw.Write(NumEntries)
                'bw.Write(Flags)
                'bw.Write(New Byte(3) {}) ' padding (4x)
                bw.Write(KeyType)                     ' uint32
                bw.Write(CUShort(HeaderSize))         ' uint16
                bw.Write(CUShort(MetaSize))           ' uint16
                bw.Write(FileSize)                    ' uint64
                bw.Write(CUShort(NumEntries))         ' uint16
                bw.Write(CUShort(Flags))              ' uint16
                bw.Write(New Byte(3) {})              ' 4 bytes padding

                ' ENTRIES

                For Each e In Entries
                    e.Save(bw)
                Next

                ' ELF HEADERS

                Dim elfHeaderStart = fs.Position

                Elf.Header.Save(bw)

                For Each ph In Elf.ProgramHeaders
                    ph.Save(bw)
                Next

                Dim elfHeaderSize =
                Math.Max(
                    Elf.Header.EhSize,
                    Elf.Header.Phoff + Elf.Header.PhEntSize * Elf.Header.PhNum
                )

                elfHeaderSize = BinaryHelpers.AlignUp(elfHeaderSize, 16)
                fs.Seek(elfHeaderStart + elfHeaderSize, SeekOrigin.Begin)

                ' EXTENDED INFO

                ExInfo.Save(bw)

                ' NPDRM CONTROL BLOCK

                Npdrm.Save(bw)
#If DEBUG Then
                Debug.WriteLine($"NPDRM block written = {fs.Position}")
#End If

                ' META BLOCKS

                For Each m In MetaBlocks
                    m.Save(bw)
                Next

                ' META FOOTER

                MetaFooter.Save(bw)

                ' SIGNATURE

                bw.Write(Signature)

                ' SEGMENT DATA

                For Each e In Entries
                    fs.Seek(baseOffset + CLng(e.Offset), SeekOrigin.Begin)
                    bw.Write(e.Data)
                Next

                ' FINAL PAD

                If fs.Length < CLng(FileSize) Then
                    fs.Seek(CLng(FileSize) - 1, SeekOrigin.Begin)
                    bw.Write(CByte(0))
                End If
                '
                '' APPEND VERSION DATA (PYTHON COMPAT)
                '
                'If VersionData IsNot Nothing AndAlso VersionData.Length > 0 Then
                '    bw.Write(VersionData)

                '    Debug.WriteLine($"APPENDED VERSION DATA: {VersionData.Length} bytes")

                'End If

                ' VERSION DATA (AFTER FILESIZE)

                If VersionData IsNot Nothing Then
                    fs.Seek(0, SeekOrigin.End)
                    bw.Write(VersionData)
                    Debug.WriteLine($"APPENDED VERSION DATA: {VersionData.Length} bytes")
                End If

            End Using
        End Using

    End Sub

End Class