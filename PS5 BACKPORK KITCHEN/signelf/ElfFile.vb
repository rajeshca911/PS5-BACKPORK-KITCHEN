Imports System.IO

Public Class ElfFile

    Public Header As ElfHeaderport
    Public ProgramHeaders As New List(Of ElfProgramHeader)
    Public Segments As New List(Of Byte())

    Public FileSize As Long
    Public Digest As Byte()

    Public Sub Load(path As String, Optional ignoreSections As Boolean = True)

        ' ---- Read full file (digest depends on this) ----
        Dim fullData = File.ReadAllBytes(path)
        FileSize = fullData.Length
        Digest = BinaryHelpers.Sha256(fullData)

        Using fs As New FileStream(path, FileMode.Open, FileAccess.Read)
            Using br As New BinaryReader(fs)

                ' ---- ELF Header ----
                Header = New ElfHeaderport()
                Header.Load(br)

                ' ---- Ignore section headers (Python behavior) ----
                If ignoreSections Then
                    Header.ShNum = 0
                End If

                ' ---- Program headers & segments ----
                If Header.HasProgramHeaders() Then

                    For i = 0 To Header.PhNum - 1

                        fs.Seek(Header.Phoff + CLng(i) * Header.PhEntSize, SeekOrigin.Begin)

                        Dim ph = New ElfProgramHeader(i)
                        ph.Load(br)
                        ProgramHeaders.Add(ph)

                        ' ---- Segment data ----
                        If ph.FileSize > 0 Then
                            fs.Seek(ph.Offset, SeekOrigin.Begin)
                            Segments.Add(br.ReadBytes(CInt(ph.FileSize)))
                        Else
                            Segments.Add(New Byte() {})
                        End If

                    Next
                End If

            End Using
        End Using
    End Sub

End Class