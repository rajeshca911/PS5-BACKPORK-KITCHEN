Imports System.IO
Imports System.Security.Cryptography

Public Module BinaryHelpers

    ' ----------------------------
    ' Alignment helpers
    ' ----------------------------
    Public Function AlignUp(value As Long, alignment As Long) As Long
        Return (value + alignment - 1) And Not (alignment - 1)
    End Function

    Public Function AlignDown(value As Long, alignment As Long) As Long
        Return value And Not (alignment - 1)
    End Function

    Public Function ILog2(value As Long) As Integer
        If value <= 0 Then Throw New ArgumentException("math domain error")
        Return CInt(Math.Log(value, 2))
    End Function

    ' ----------------------------
    ' Hash helpers
    ' ----------------------------
    Public Function Sha256(data As Byte()) As Byte()
        Using sha = System.Security.Cryptography.SHA256.Create()
            Return sha.ComputeHash(data)
        End Using
    End Function

    Public Function HmacSha256(key As Byte(), data As Byte()) As Byte()
        Using h = New HMACSHA256(key)
            Return h.ComputeHash(data)
        End Using
    End Function

    ' ----------------------------
    ' File helpers
    ' ----------------------------
    Public Function ReadAllBytesAt(fs As FileStream, offset As Long, size As Integer) As Byte()
        fs.Seek(offset, SeekOrigin.Begin)
        Dim buffer(size - 1) As Byte
        fs.Read(buffer, 0, size)
        Return buffer
    End Function

    Public Function CheckFileMagic(br As BinaryReader, expected As Byte()) As Boolean
        Dim oldPos = br.BaseStream.Position
        Dim data = br.ReadBytes(expected.Length)
        br.BaseStream.Seek(oldPos, SeekOrigin.Begin)
        Return data.SequenceEqual(expected)
    End Function

End Module