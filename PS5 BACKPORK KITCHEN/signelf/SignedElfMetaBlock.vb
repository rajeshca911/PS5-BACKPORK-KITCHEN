Imports System.IO

Public Class SignedElfMetaBlock

    Public Sub Save(bw As BinaryWriter)
        bw.Write(New Byte(79) {}) ' 80 bytes padding
#If DEBUG Then
        Debug.WriteLine("MetaBlock OK")
#End If

    End Sub

End Class

Public Class SignedElfMetaFooter

    Public Unknown1 As UInteger

    Public Sub Save(bw As BinaryWriter)
        bw.Write(New Byte(47) {}) ' 48 bytes padding
        bw.Write(Unknown1)
        bw.Write(New Byte(27) {}) ' 28 bytes padding
#If DEBUG Then
        Debug.WriteLine("MetaFooter OK")
#End If
    End Sub

End Class