Imports System.IO

Public Class ElfProgramHeader

    ' ---- STRUCT FORMAT <2I6Q ----
    Public Type As UInteger

    Public Flags As UInteger
    Public Offset As ULong
    Public VAddr As ULong
    Public PAddr As ULong
    Public FileSize As ULong
    Public MemSize As ULong
    Public Align As ULong

    Public ReadOnly Index As Integer

    Public Sub New(idx As Integer)
        Index = idx
    End Sub

    Public Sub Load(br As BinaryReader)
        Type = br.ReadUInt32()
        Flags = br.ReadUInt32()
        Offset = br.ReadUInt64()
        VAddr = br.ReadUInt64()
        PAddr = br.ReadUInt64()
        FileSize = br.ReadUInt64()
        MemSize = br.ReadUInt64()
        Align = br.ReadUInt64()
    End Sub

    Public Sub Save(bw As BinaryWriter)
        bw.Write(Type)
        bw.Write(Flags)
        bw.Write(Offset)
        bw.Write(VAddr)
        bw.Write(PAddr)
        bw.Write(FileSize)
        bw.Write(MemSize)
        bw.Write(Align)
    End Sub

    Public Function SegmentName() As String
        If Type = PT_LOAD Then
            If (Flags And PF_READ_EXEC) = PF_READ_EXEC Then
                Return ".text"
            ElseIf (Flags And PF_READ_WRITE) = PF_READ_WRITE Then
                Return ".data"
            Else
                Return $".load_{Index:D2}"
            End If
        End If

        Select Case Type
            Case PT_DYNAMIC : Return ".dynamic"
            Case PT_INTERP : Return ".interp"
            Case PT_TLS : Return ".tls"
            Case PT_GNU_EH_FRAME : Return ".eh_frame_hdr"
            Case PT_SCE_DYNLIBDATA : Return ".sce_dynlib_data"
            Case PT_SCE_PROCPARAM : Return ".sce_process_param"
            Case PT_SCE_MODULE_PARAM : Return ".sce_module_param"
            Case PT_SCE_COMMENT : Return ".sce_comment"
            Case Else : Return Nothing
        End Select
    End Function

    Public Function SegmentClass() As String
        If (Flags And PF_EXEC) <> 0 Then
            Return "CODE"
        Else
            Return "DATA"
        End If
    End Function

End Class