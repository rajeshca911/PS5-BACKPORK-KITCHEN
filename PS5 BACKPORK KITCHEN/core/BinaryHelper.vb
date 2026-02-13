Imports System.IO

Public Module BinaryHelper

    Public Function ReadUInt(file As FileStream, offset As Long, size As Integer) As ULong
        Dim buffer(size - 1) As Byte
        file.Seek(offset, SeekOrigin.Begin)
        file.Read(buffer, 0, size)

        Select Case size
            Case 1 : Return buffer(0)
            Case 2 : Return BitConverter.ToUInt16(buffer, 0)
            Case 4 : Return BitConverter.ToUInt32(buffer, 0)
            Case 8 : Return BitConverter.ToUInt64(buffer, 0)
            Case Else
                Throw New ArgumentException("Unsupported integer size")
        End Select
    End Function

    Public Sub WriteUInt(file As FileStream, offset As Long, size As Integer, value As ULong)
        Dim buffer As Byte()

        Select Case size
            Case 1 : buffer = {CByte(value)}
            Case 2 : buffer = BitConverter.GetBytes(CUShort(value))
            Case 4 : buffer = BitConverter.GetBytes(CUInt(value))
            Case 8 : buffer = BitConverter.GetBytes(value)
            Case Else
                Throw New ArgumentException("Unsupported integer size")
        End Select

        file.Seek(offset, SeekOrigin.Begin)
        file.Write(buffer, 0, size)
    End Sub

End Module