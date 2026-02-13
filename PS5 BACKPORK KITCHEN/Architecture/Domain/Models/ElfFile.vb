Namespace Architecture.Domain.Models
    ''' <summary>
    ''' Represents a parsed ELF file (immutable)
    ''' </summary>
    Public Class ElfFile
        Public Sub New(header As ElfHeader,
                      programHeaders As IReadOnlyList(Of ProgramHeader),
                      rawBytes As Byte())
            Me.Header = header
            Me.ProgramHeaders = programHeaders
            Me.RawBytes = rawBytes
        End Sub

        Public ReadOnly Property Header As ElfHeader
        Public ReadOnly Property ProgramHeaders As IReadOnlyList(Of ProgramHeader)
        Public ReadOnly Property RawBytes As Byte()

        Public ReadOnly Property SdkVersion As Long
            Get
                Return Header.SdkVersion
            End Get
        End Property

        Public ReadOnly Property IsEncrypted As Boolean
            Get
                ' Check ELF magic vs SELF magic
                Return RawBytes.Length >= 4 AndAlso
                       RawBytes(0) = &H4F AndAlso
                       RawBytes(1) = &H15 ' SELF magic
            End Get
        End Property

        Public Function WithPatchedSdk(newSdk As Long) As ElfFile
            ' Return new instance with patched header (immutable)
            Dim newHeader = Header.WithSdkVersion(newSdk)
            Return New ElfFile(newHeader, ProgramHeaders, RawBytes)
        End Function
    End Class

    ''' <summary>
    ''' ELF header information
    ''' </summary>
    Public Class ElfHeader
        Public Property SdkVersion As Long
        Public Property EntryPoint As Long
        Public Property ProgramHeaderOffset As Long
        Public Property ProgramHeaderCount As Integer

        Public Function WithSdkVersion(newSdk As Long) As ElfHeader
            ' Return copy with new SDK
            Return New ElfHeader With {
                .SdkVersion = newSdk,
                .EntryPoint = Me.EntryPoint,
                .ProgramHeaderOffset = Me.ProgramHeaderOffset,
                .ProgramHeaderCount = Me.ProgramHeaderCount
            }
        End Function
    End Class

    ''' <summary>
    ''' ELF program header
    ''' </summary>
    Public Class ProgramHeader
        Public Property Type As UInteger
        Public Property Offset As Long
        Public Property VirtualAddress As Long
        Public Property FileSize As Long
        Public Property MemorySize As Long
        Public Property Flags As UInteger
    End Class
End Namespace
