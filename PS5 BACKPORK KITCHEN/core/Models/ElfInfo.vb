Imports System.IO

Public Class ElfInfo
    Public Property FilePath As String
    Public Property FileType As String
    Public Property IsElf As Boolean
    Public Property IsSigned As Boolean
    Public Property IsPatchable As Boolean

    Public Property ProgramHeaderCount As UInteger
    Public Property Ps5SdkVersion As UInteger?
    Public Property Ps4SdkVersion As UInteger?

    Public Property Message As String
End Class

Public Class ElfInspector

    Public Shared Function ReadInfo(filePath As String) As ElfInfo

        Dim info As New ElfInfo With {
            .FilePath = filePath,
            .FileType = "Unknown",
            .IsElf = False,
            .IsSigned = False,
            .IsPatchable = False
        }

        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)

                ' ---- Read magic ----
                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' ---- Detect signed SELF ----
                If magic.SequenceEqual(PS4_FSELF_MAGIC) Then
                    info.FileType = "PS4 Signed SELF"
                    info.IsSigned = True
                    info.Message = "Signed PS4 SELF – cannot patch"
                    Return info
                End If

                If magic.SequenceEqual(PS5_FSELF_MAGIC) Then
                    info.FileType = "PS5 Signed SELF"
                    info.IsSigned = True
                    info.Message = "Signed PS5 SELF – cannot patch"
                    Return info
                End If

                ' ---- ELF check ----
                If Not magic.SequenceEqual(ELF_MAGIC) Then
                    info.Message = "Not a valid ELF file"
                    Return info
                End If

                info.IsElf = True
                info.FileType = "ELF"

                ' ---- Read program header info ----
                info.ProgramHeaderCount = CUInt(ReadUInt(fs, PHT_COUNT_OFFSET, PHT_COUNT_SIZE))
                Dim phtOffset = ReadUInt(fs, PHT_OFFSET_OFFSET, PHT_OFFSET_SIZE)

                ' ---- Scan program headers ----
                For i As Integer = 0 To CInt(info.ProgramHeaderCount) - 1

                    Dim entryBase = CLng(phtOffset) + i * PHDR_ENTRY_SIZE
                    Dim segType = ReadUInt(fs, entryBase + PHDR_TYPE_OFFSET, PHDR_TYPE_SIZE)

                    If segType <> PT_SCE_PROCPARAM AndAlso segType <> PT_SCE_MODULE_PARAM Then Continue For

                    Dim segOffset = ReadUInt(fs, entryBase + PHDR_OFFSET_OFFSET, PHDR_OFFSET_SIZE)
                    Dim paramSize = ReadUInt(fs, CLng(segOffset), 4)

                    If paramSize < SCE_PARAM_PS5_SDK_OFFSET + SCE_PARAM_PS_VERSION_SIZE Then
                        info.Message = "Invalid param structure"
                        Return info
                    End If

                    Dim magicParam = ReadUInt(fs, CLng(segOffset) + SCE_PARAM_MAGIC_OFFSET, 4)

                    If segType = PT_SCE_PROCPARAM AndAlso magicParam <> SCE_PROCESS_PARAM_MAGIC Then Continue For
                    If segType = PT_SCE_MODULE_PARAM AndAlso magicParam <> SCE_MODULE_PARAM_MAGIC Then Continue For

                    ' ---- Extract versions ----
                    info.Ps5SdkVersion = CUInt(ReadUInt(fs, CLng(segOffset) + SCE_PARAM_PS5_SDK_OFFSET, 4))
                    info.Ps4SdkVersion = CUInt(ReadUInt(fs, CLng(segOffset) + SCE_PARAM_PS4_SDK_OFFSET, 4))
                    info.IsPatchable = True
                    info.Message = "Patchable ELF detected"
                    Return info
                Next

                info.Message = "No patchable param segment found"

            End Using
        Catch ex As Exception
            info.Message = $"Error: {ex.Message}"
        End Try

        Return info
    End Function

End Class