Imports System.IO

Public Class NpdrmControlBlock
    Public Const TYPE_NPDRM As UShort = &H3

    Public ContentId As Byte()    ' 0x13
    Public RandomPad As Byte()    ' 0x0D

    Public Sub Save(bw As BinaryWriter)
        bw.Write(TYPE_NPDRM)
        bw.Write(New Byte(13) {}) ' padding
        bw.Write(ContentId)
        bw.Write(RandomPad)
#If DEBUG Then
        Debug.WriteLine("NPDRM OK")
#End If
    End Sub

End Class