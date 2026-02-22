Imports System.Runtime.InteropServices

Public Class BinaryReaderUtil

    Public Shared Function ToStructure(Of T)(data As Byte(), offset As Integer) As T
        Dim size As Integer = Marshal.SizeOf(GetType(T))
        Dim ptr As IntPtr = Marshal.AllocHGlobal(size)

        Marshal.Copy(data, offset, ptr, size)
        Dim obj As T = Marshal.PtrToStructure(Of T)(ptr)

        Marshal.FreeHGlobal(ptr)
        Return obj
    End Function

End Class
