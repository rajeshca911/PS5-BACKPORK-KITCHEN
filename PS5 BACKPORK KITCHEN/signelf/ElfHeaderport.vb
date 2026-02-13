Imports System.IO

Public Class ElfHeaderport

    ' ---- IDENT ----
    Public Magic As Byte()            ' 4 bytes

    Public EiClass As Byte            ' 1
    Public EiData As Byte             ' 1
    Public EiVersion As Byte          ' 1
    Public EiOsAbi As Byte            ' 1
    Public EiAbiVersion As Byte       ' 1
    Public EiPad As Byte              ' 1 (last byte of ident we care about)

    ' ---- ELF HEADER ----
    Public Type As UShort

    Public Machine As UShort
    Public Version As UInteger
    Public Entry As ULong
    Public Phoff As ULong
    Public Shoff As ULong
    Public Flags As UInteger
    Public EhSize As UShort
    Public PhEntSize As UShort
    Public PhNum As UShort
    Public ShEntSize As UShort
    Public ShNum As UShort
    Public ShStrIdx As UShort

    Public Sub Load(br As BinaryReader)

        ' ---- Magic check ----
        Magic = br.ReadBytes(4)
        If Not Magic.SequenceEqual(ElfConstants.ELF_MAGIC) Then
            Throw New ElfError("Invalid ELF magic")
        End If

        ' ---- e_ident ----
        EiClass = br.ReadByte()
        EiData = br.ReadByte()
        EiVersion = br.ReadByte()
        EiOsAbi = br.ReadByte()
        EiAbiVersion = br.ReadByte()

        ' skip 6 padding bytes
        br.ReadBytes(6)

        ' last ident byte
        EiPad = br.ReadByte()

        ' ---- Validate ----
        If EiClass <> ELFCLASS64 Then
            Throw New ElfError("Unsupported ELF class (not 64-bit)")
        End If

        If EiData <> ELFDATA2LSB Then
            Throw New ElfError("Unsupported ELF endian (not little-endian)")
        End If

        ' ---- Main header ----
        Type = br.ReadUInt16()
        Machine = br.ReadUInt16()
        Version = br.ReadUInt32()
        Entry = br.ReadUInt64()
        Phoff = br.ReadUInt64()
        Shoff = br.ReadUInt64()
        Flags = br.ReadUInt32()
        EhSize = br.ReadUInt16()
        PhEntSize = br.ReadUInt16()
        PhNum = br.ReadUInt16()
        ShEntSize = br.ReadUInt16()
        ShNum = br.ReadUInt16()
        ShStrIdx = br.ReadUInt16()

        ' ---- Validate machine ----
        If Machine <> EM_X86_64 Then
            Throw New ElfError("Unsupported machine type")
        End If

        If Version <> EV_CURRENT Then
            Throw New ElfError("Unsupported ELF version")
        End If

        ' ---- Validate type ----
        If Not {
            ET_EXEC,
            ET_SCE_EXEC,
            ET_SCE_EXEC_ASLR,
            ET_SCE_DYNAMIC
        }.Contains(Type) Then
            Throw New ElfError("Unsupported ELF type")
        End If
    End Sub

    Public Sub Save(bw As BinaryWriter)

        bw.Write(Magic)
        bw.Write(EiClass)
        bw.Write(EiData)
        bw.Write(EiVersion)
        bw.Write(EiOsAbi)
        bw.Write(EiAbiVersion)

        ' padding
        bw.Write(New Byte(5) {}) ' 6 bytes
        bw.Write(EiPad)

        bw.Write(Type)
        bw.Write(Machine)
        bw.Write(Version)
        bw.Write(Entry)
        bw.Write(Phoff)
        bw.Write(Shoff)
        bw.Write(Flags)
        bw.Write(EhSize)
        bw.Write(PhEntSize)
        bw.Write(PhNum)
        bw.Write(ShEntSize)
        bw.Write(ShNum)
        bw.Write(ShStrIdx)

    End Sub

    Public Function HasProgramHeaders() As Boolean
        Return PhEntSize > 0 AndAlso PhNum > 0
    End Function

    Public Function HasSectionHeaders() As Boolean
        Return ShEntSize > 0 AndAlso ShNum > 0
    End Function

End Class