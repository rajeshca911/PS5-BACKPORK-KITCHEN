Imports System.IO
Imports System.Runtime.InteropServices.JavaScript.JSType

Module ElfLogger

    Public elfLogPath As String

    Public Sub InitializeElfLog()
        Dim baseOutputDir = Application.StartupPath
        elfLogPath = Path.Combine(baseOutputDir, "elf.log")

        If File.Exists(elfLogPath) Then
            File.Delete(elfLogPath)
        End If
    End Sub

    Public Sub WriteLine(text As String)
        If String.IsNullOrEmpty(elfLogPath) Then Return
        File.AppendAllText(elfLogPath, text & Environment.NewLine)
    End Sub

    Public Sub LogElfDetailsToFile(data As Byte(),
                               Optional Sourcefilename As String = "")

        If Not String.IsNullOrEmpty(Sourcefilename) Then
            ElfLogger.WriteLine($"Lib:{Sourcefilename}")
        End If

        If data Is Nothing OrElse data.Length < 64 Then
            ElfLogger.WriteLine("Invalid ELF (too small).")
            Return
        End If

        Dim hasMagic As Boolean =
        (data(0) = &H7F AndAlso
         data(1) = &H45 AndAlso
         data(2) = &H4C AndAlso
         data(3) = &H46)

        Dim header As Elf64Header

        Try
            header = BinaryReaderUtil.ToStructure(Of Elf64Header)(data, 0)
        Catch
            ElfLogger.WriteLine("Failed to read ELF header structure.")
            Return
        End Try

        ' ------------------------------------------------
        ' Determine Type
        ' ------------------------------------------------
        If hasMagic Then
            ElfLogger.WriteLine("Type: Standard ELF")
        ElseIf LooksLikeStrippedElf(data) Then
            ElfLogger.WriteLine("Type: Stripped / Zeroed ELF")
        Else
            ElfLogger.WriteLine("Not a valid ELF structure.")
            Return
        End If

        ElfLogger.WriteLine("----------------------------------------")
        ElfLogger.WriteLine("General Information")
        ElfLogger.WriteLine("----------------------------------------")

        ElfLogger.WriteLine($"Entry Point        : 0x{header.e_entry:X}")
        ElfLogger.WriteLine($"Program Headers    : {header.e_phnum}")
        ElfLogger.WriteLine($"PH Entry Size      : {header.e_phentsize}")
        ElfLogger.WriteLine($"Section Headers    : {header.e_shnum}")
        ElfLogger.WriteLine($"Header Size        : {header.e_ehsize}")
        ElfLogger.WriteLine("")

        ' ------------------------------------------------
        ' Program Headers
        ' ------------------------------------------------

        Dim phOffset As Integer = CInt(header.e_phoff)
        Dim phSize As Integer = header.e_phentsize

        ElfLogger.WriteLine("Program Header Table")
        ElfLogger.WriteLine("Idx | Type       | Offset     | FileSize   | MemSize")

        For i As Integer = 0 To header.e_phnum - 1

            Dim currentPhOffset As Integer =
            phOffset + (i * phSize)

            If currentPhOffset + phSize > data.Length Then
                ElfLogger.WriteLine($"Program header {i} out of bounds.")
                Exit For
            End If

            Dim ph As Elf64ProgramHeader =
            BinaryReaderUtil.ToStructure(Of Elf64ProgramHeader)(
                data, currentPhOffset)

            ElfLogger.WriteLine(
            $"{i.ToString().PadRight(3)} | " &
            $"0x{ph.p_type:X8} | " &
            $"0x{ph.p_offset:X8} | " &
            $"0x{ph.p_filesz:X8} | " &
            $"0x{ph.p_memsz:X8}"
        )
        Next

        ElfLogger.WriteLine("")
        ElfLogger.WriteLine("========================================")
        ElfLogger.WriteLine("")

    End Sub

End Module