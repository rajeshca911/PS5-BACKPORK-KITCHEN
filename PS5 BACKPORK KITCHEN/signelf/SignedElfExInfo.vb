Imports System.IO

Public Class SignedElfExInfo
    Public Const PTYPE_FAKE As ULong = &H1
    Public Const PTYPE_NPDRM_EXEC As ULong = &H4
    Public Const PTYPE_NPDRM_DYNLIB As ULong = &H5
    Public Const PTYPE_SYSTEM_EXEC As ULong = &H8
    Public Const PTYPE_SYSTEM_DYNLIB As ULong = &H9
    Public Const PTYPE_HOST_KERNEL As ULong = &HC
    Public Const PTYPE_SECURE_MODULE As ULong = &HE
    Public Const PTYPE_SECURE_KERNEL As ULong = &HF

    Public Paid As ULong
    Public PType As ULong
    Public AppVersion As ULong
    Public FwVersion As ULong
    Public Digest As Byte()

    Public Sub Save(bw As BinaryWriter)
        bw.Write(Paid)
        bw.Write(PType)
        bw.Write(AppVersion)
        bw.Write(FwVersion)
        bw.Write(Digest)
#If DEBUG Then
        Debug.WriteLine("ExInfo OK")
#End If
    End Sub

End Class