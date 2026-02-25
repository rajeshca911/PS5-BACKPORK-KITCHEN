Imports System
Imports System.Runtime
Imports System.Runtime.InteropServices
<StructLayout(LayoutKind.Sequential, Pack:=1)>
Public Structure Elf64ProgramHeader
    Public p_type As UInteger
    Public p_flags As UInteger
    Public p_offset As ULong
    Public p_vaddr As ULong
    Public p_paddr As ULong
    Public p_filesz As ULong
    Public p_memsz As ULong
    Public p_align As ULong
End Structure
<StructLayout(LayoutKind.Sequential, Pack:=1)>
Public Structure SelfHeader
    Public magic As UInteger
    Public version As Byte
    Public mode As Byte
    Public endian As Byte
    Public attribs As Byte
    Public key_type As UInteger
    Public header_size As UShort
    Public meta_size As UShort
    Public file_size As ULong
    Public num_entries As UShort
    Public flags As UShort
    <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)>
    Public pad() As Byte
End Structure
<StructLayout(LayoutKind.Sequential, Pack:=1)>
Public Structure Elf64Header

    <MarshalAs(UnmanagedType.ByValArray, SizeConst:=16)>
    Public e_ident() As Byte

    Public e_type As UShort
    Public e_machine As UShort
    Public e_version As UInteger
    Public e_entry As ULong
    Public e_phoff As ULong
    Public e_shoff As ULong
    Public e_flags As UInteger
    Public e_ehsize As UShort
    Public e_phentsize As UShort
    Public e_phnum As UShort
    Public e_shentsize As UShort
    Public e_shnum As UShort
    Public e_shstrndx As UShort

End Structure
<StructLayout(LayoutKind.Sequential, Pack:=1)>
Public Structure SelfEntry
    Public props As ULong
    Public offs As ULong
    Public fileSz As ULong
    Public memSz As ULong
End Structure
Public Class SelfFile

    Private ReadOnly _data As Byte()
    Private ReadOnly _header As SelfHeader
    Private ReadOnly _logger As ISelfLogger
    Private _ElfHeaderOffset As Integer = -1
    Public ReadOnly Property ElfHeaderOffset As Integer
        Get
            Return _ElfHeaderOffset
        End Get
    End Property
    Public Sub New(data As Byte(), logger As ISelfLogger)
        _data = data
        _logger = logger

        If Not ValidateMagic() Then
            Throw New Exception("Invalid SELF magic.")
        End If

        _header = BinaryReaderUtil.ToStructure(Of SelfHeader)(_data, 0)

        _logger.Debug("SELF header parsed.")
    End Sub
    'Public Sub New(data As Byte())
    '    _data = data

    '    If Not ValidateMagic() Then
    '        Throw New Exception("Invalid SELF magic.")
    '    End If

    '    _header = BinaryReaderUtil.ToStructure(Of SelfHeader)(_data, 0)
    'End Sub

    Private Function ValidateMagic() As Boolean
        If _data.Length < 4 Then Return False

        Dim magic As UInteger = BitConverter.ToUInt32(_data, 0)

        Const PS4_SELF_MAGIC As UInteger = &H1D3D154FUI
        Const PS5_SELF_MAGIC As UInteger = &HEEF51454UI

        Return (magic = PS4_SELF_MAGIC OrElse magic = PS5_SELF_MAGIC)
    End Function


    Public Function ExtractElf() As Byte()

        Const ELF_MAGIC As UInteger = &H464C457FUI

        ' -------------------------------------------------------
        ' 1. Locate embedded ELF header inside SELF
        ' -------------------------------------------------------

        Dim searchStart As Integer = (1 + _header.num_entries) * &H20
        _ElfHeaderOffset = -1

        For i As Integer = searchStart To _data.Length - 4
            If BitConverter.ToUInt32(_data, i) = ELF_MAGIC Then
                _ElfHeaderOffset = i
                Exit For
            End If
        Next
        If _ElfHeaderOffset = -1 Then
            Throw New Exception("Embedded ELF not found inside SELF container.")
        End If

        ' ✅ Now it's safe to print header bytes
        Logger.LogToFile("ELF header bytes:", LogLevel.Info)

        For i = 0 To 15
            Logger.LogToFile($"{_data(_ElfHeaderOffset + i):X2} ", LogLevel.Info)
        Next

        If _ElfHeaderOffset = -1 Then
            Throw New Exception("Embedded ELF not found inside SELF container.")
        End If
        'Logger.Log(Form1.rtbStatus, "", LogLevel.Info)
        'Logger.Log($"[ExtractElf] SELF entries: {_header.num_entries}")
        Logger.LogToFile($"[ExtractElf] SELF entries: {_header.num_entries}", LogLevel.Info)
        Logger.LogToFile($"[ExtractElf] Calculated ELF header offset: 0x{_ElfHeaderOffset:X}", LogLevel.Info)
        Logger.LogToFile("[ExtractElf] ELF magic verified at that offset", LogLevel.Info)



        ' -------------------------------------------------------
        ' 2. Parse ELF header
        ' -------------------------------------------------------

        Dim elfHeader As Elf64Header =
        ReadStructure(Of Elf64Header)(_data, _ElfHeaderOffset)

        Dim programHeaderTableOffset As Integer =
        CInt(_ElfHeaderOffset + elfHeader.e_phoff)
        Logger.LogToFile($"[ExtractElf] Program Header Table Offset: 0x{programHeaderTableOffset:X}", LogLevel.Info)
        Logger.LogToFile($"[ExtractElf] Program Header Count: {elfHeader.e_phnum}", LogLevel.Info)

        Debug.Print($"ELF e_phoff: 0x{elfHeader.e_phoff:X}", LogLevel.Info)
        Debug.Print($"ELF e_phentsize: {elfHeader.e_phentsize}", LogLevel.Info)
        Debug.Print($"ELF e_phnum: {elfHeader.e_phnum}", LogLevel.Info)

        If elfHeader.e_phentsize <> Marshal.SizeOf(GetType(Elf64ProgramHeader)) Then
            Debug.Print("Invalid program header entry size.")
        End If

        If elfHeader.e_phnum > 200 Then
            Debug.Print("Unrealistic program header count.")
        End If

        If elfHeader.e_phoff + (CLng(elfHeader.e_phentsize) * elfHeader.e_phnum) > _data.Length Then
            Debug.Print("Program header table outside file bounds.")
        End If

        ' -------------------------------------------------------
        ' 3. Determine first & last segments (exact C++ logic)
        ' -------------------------------------------------------

        Dim firstOffset As ULong = 0
        Dim lastOffset As ULong = 0
        Dim lastFileSize As ULong = 0
        Dim firstFound As Boolean = False

        For i As Integer = 0 To elfHeader.e_phnum - 1

            Dim currentPhOffset As Integer =
            programHeaderTableOffset + (i * elfHeader.e_phentsize)

            Dim ph As Elf64ProgramHeader =
            ReadStructure(Of Elf64ProgramHeader)(_data, currentPhOffset)


            Logger.LogToFile($"[ExtractElf] PH[{i}] Offset: 0x{ph.p_offset:X} Size: 0x{ph.p_filesz:X} Align: 0x{ph.p_align:X}", LogLevel.Info)
            Logger.LogToFile($"PH[{i}] Type: 0x{ph.p_type:X8} Offset: 0x{ph.p_offset:X}", LogLevel.Info)
            Logger.Log(Form1.rtbStatus, $"PH[{i}] Type: 0x{ph.p_type:X8} Offset: 0x{ph.p_offset:X}")

            ' --- FIRST segment (smallest non-zero offset)
            'If ph.p_offset > 0 Then
            If ph.p_type = 1UI AndAlso ph.p_offset > 0 Then ' PT_LOAD
                If Not firstFound OrElse ph.p_offset < firstOffset Then
                    firstOffset = ph.p_offset
                    firstFound = True
                End If
            End If

            ' --- LAST segment (largest offset)
            If ph.p_type = 1UI AndAlso ph.p_offset >= lastOffset Then
                lastOffset = ph.p_offset
                lastFileSize = ph.p_filesz
            End If

        Next

        Dim saveSize As ULong = lastOffset + lastFileSize
        Debug.Print($"saveSize: 0x{saveSize:X}", LogLevel.Info)
        '_logger.Debug($"[ExtractElf] First Segment Offset: 0x{firstOffset:X}")
        '_logger.Debug($"[ExtractElf] Last Segment Offset: 0x{lastOffset:X}")
        '_logger.Debug($"[ExtractElf] Calculated Save Size: 0x{saveSize:X}")

        Logger.LogToFile($"First PT_LOAD offset should be 0x4000 → actual 0x{firstOffset:X}", LogLevel.Info)
        Logger.LogToFile($"[ExtractElf] First Segment Offset: 0x{firstOffset:X}", LogLevel.Info)
        Logger.LogToFile($"[ExtractElf] Last Segment Offset: 0x{lastOffset:X}", LogLevel.Info)
        Logger.LogToFile($"[ExtractElf] Calculated Save Size: 0x{saveSize:X}", LogLevel.Info)

        ' -------------------------------------------------------
        ' 4. Allocate output buffer (C++: save.resize)
        ' -------------------------------------------------------
        If saveSize > Integer.MaxValue Then
            Throw New Exception($"saveSize too large: 0x{saveSize:X}")
        End If

        If saveSize = 0 Then
            Throw New Exception("saveSize is zero.")
        End If

        Dim outputSize As Integer = CInt(saveSize)
        Dim output(outputSize - 1) As Byte
        ' VB arrays are zeroed automatically (equivalent to memset)
        ' -------------------------------------------------------
        ' 5. Copy ELF header region (C++: memcpy(pd, eHead, first))
        ' -------------------------------------------------------

        'Array.Copy(_data, _ElfHeaderOffset, output, 0, CInt(firstOffset))
        'Logger.LogToFile($"[ExtractElf] Header region copied: 0x{firstOffset:X} bytes", LogLevel.Info)
        Debug.Print($"Header copy check: srcEnd=0x{_ElfHeaderOffset + CInt(firstOffset):X} dataLen=0x{_data.Length:X}")

        Dim headerCopySize As Integer = CInt(firstOffset)

        If _ElfHeaderOffset + headerCopySize > _data.Length Then
            headerCopySize = _data.Length - _ElfHeaderOffset
        End If

        If headerCopySize > output.Length Then
            headerCopySize = output.Length
        End If

        If headerCopySize > 0 Then
            Debug.Print($"HEADER COPY size=0x{headerCopySize:X}")
            Array.Copy(_data, _ElfHeaderOffset, output, 0, headerCopySize)
        End If

        ' -------------------------------------------------------
        ' 6. Copy segments using SELF entries (exact author logic)
        ' -------------------------------------------------------

        For entryIndex As Integer = 0 To _header.num_entries - 1

            Dim entryOffset As Integer = &H20 * (1 + entryIndex)
            Dim se As SelfEntry =
            ReadStructure(Of SelfEntry)(_data, entryOffset)

            ' Only process loadable entries
            If (se.props And &H800UL) = 0 Then Continue For
            If se.fileSz = 0 Then Continue For

            Dim phIndex As Integer =
            CInt((se.props >> 20) And &HFFFUL)

            If phIndex < 0 OrElse phIndex >= elfHeader.e_phnum Then Continue For

            Dim phOffset As Integer =
            programHeaderTableOffset + (phIndex * elfHeader.e_phentsize)

            Dim ph As Elf64ProgramHeader =
            ReadStructure(Of Elf64ProgramHeader)(_data, phOffset)


            ' --- SAFE COPY BLOCK ---

            If se.offs > Integer.MaxValue Then Continue For
            If ph.p_offset > Integer.MaxValue Then Continue For
            If se.fileSz > Integer.MaxValue Then Continue For

            Dim srcOffset As Integer = CInt(se.offs)
            Dim dstOffset As Integer = CInt(ph.p_offset)
            Dim copySize As Integer = CInt(se.fileSz)

            If srcOffset < 0 OrElse dstOffset < 0 Then Continue For
            If copySize <= 0 Then Continue For

            If srcOffset > _data.Length Then Continue For
            If dstOffset > output.Length Then Continue For

            If srcOffset + copySize > _data.Length Then Continue For
            If dstOffset + copySize > output.Length Then Continue For

            Debug.Print($"COPY src=0x{srcOffset:X} dst=0x{dstOffset:X} size=0x{copySize:X}")

            Array.Copy(_data, srcOffset, output, dstOffset, copySize)



        Next

        ' -------------------------------------------------------
        ' 7. Patch PT_SCE_VERSION (exact C++ behaviour)
        ' -------------------------------------------------------

        For i As Integer = 0 To elfHeader.e_phnum - 1

            Dim phOffset As Integer =
            programHeaderTableOffset + (i * elfHeader.e_phentsize)

            Dim ph As Elf64ProgramHeader =
            ReadStructure(Of Elf64ProgramHeader)(_data, phOffset)

            If ph.p_type = &H6FFFFF01UI Then

                Dim fileSize As ULong = ph.p_filesz
                Dim destOffsetUL As ULong = ph.p_offset

                If fileSize > Integer.MaxValue Then Exit For
                If destOffsetUL > Integer.MaxValue Then Exit For

                Dim copySize As Integer = CInt(fileSize)
                Dim dstOffset As Integer = CInt(destOffsetUL)
                Dim srcOffset As Integer = _data.Length - copySize

                If srcOffset < 0 Then Exit For
                If dstOffset < 0 Then Exit For

                If srcOffset + copySize > _data.Length Then Exit For
                If dstOffset + copySize > output.Length Then Exit For

                Debug.Print($"PATCH VERSION src=0x{srcOffset:X} dst=0x{dstOffset:X} size=0x{copySize:X}")

                Array.Copy(_data, srcOffset, output, dstOffset, copySize)
                Logger.LogToFile($"segment address: 0x{srcOffset:X}", LogLevel.Info)
                Logger.LogToFile($"segment size: 0x{ph.p_filesz:X}", LogLevel.Info)
                Logger.LogToFile("patched version segment", LogLevel.Info)
                Exit For
            End If

        Next
        Logger.LogToFile($"Marshal SelfEntry Size:{Marshal.SizeOf(GetType(SelfEntry))}", LogLevel.Info)
        Logger.LogToFile($"[ExtractElf] Output buffer size: 0x{outputSize:X}", LogLevel.Info)

        ' -------------------------------------------------------
        ' 8. Patch first segment duplicate (correct logic)
        ' -------------------------------------------------------

        Dim duplicateOffset As Integer = -1
        Dim compareSize As Integer = &HC0
        Dim safetyPercent As Integer = 2 ' same as C++ default

        Dim maxScan As Integer =
    CInt(firstOffset * (100 - safetyPercent) / 100)

        If firstOffset >= compareSize Then

            For i As Integer = 0 To maxScan

                If firstOffset - i < compareSize Then Exit For

                Dim match As Boolean = True

                For j As Integer = 0 To compareSize - 1
                    If output(i + j) <> output(CInt(firstOffset) + j) Then
                        match = False
                        Exit For
                    End If
                Next

                If match Then
                    duplicateOffset = i
                    Exit For
                End If

            Next

        End If

        If duplicateOffset <> -1 Then

            Dim wipeSize As Integer =
        CInt(firstOffset - duplicateOffset)

            Logger.LogToFile("", LogLevel.Info)
            Logger.LogToFile("patching first segment duplicate", LogLevel.Info)
            Logger.LogToFile($"address: 0x{duplicateOffset:X}", LogLevel.Info)
            Logger.LogToFile($"size: 0x{wipeSize:X}", LogLevel.Info)

            For i As Integer = duplicateOffset To CInt(firstOffset - 1)
                output(i) = 0
            Next

            Logger.LogToFile("patched first segment duplicate", LogLevel.Info)
            Logger.Log(Form1.rtbStatus, "patched first segment duplicate")
        End If

        Return output

    End Function
    ' Helper to read structures from the byte array
    Private Function ReadStructure(Of T As Structure)(buffer As Byte(), offset As Integer) As T
        Dim size As Integer = Marshal.SizeOf(GetType(T))
        Dim ptr As IntPtr = Marshal.AllocHGlobal(size)
        Try
            Marshal.Copy(buffer, offset, ptr, size)
            Return DirectCast(Marshal.PtrToStructure(ptr, GetType(T)), T)
        Finally
            Marshal.FreeHGlobal(ptr)
        End Try
    End Function


End Class
