Imports System.IO

''' <summary>
''' Big-endian binary write helpers for building PKG/FPKG files.
''' Symmetric to ReadUInt32BE/ReadUInt64BE in PKGHeader.vb.
''' </summary>
Public Class PKGBinaryWriter
    Implements IDisposable

    Private ReadOnly _stream As Stream
    Private _disposed As Boolean

    Public Sub New(stream As Stream)
        _stream = stream
    End Sub

    Public ReadOnly Property BaseStream As Stream
        Get
            Return _stream
        End Get
    End Property

    ''' <summary>
    ''' Writes a UInt32 in big-endian byte order.
    ''' </summary>
    Public Sub WriteUInt32BE(value As UInteger)
        Dim bytes = BitConverter.GetBytes(value)
        Array.Reverse(bytes)
        _stream.Write(bytes, 0, 4)
    End Sub

    ''' <summary>
    ''' Writes a UInt64 in big-endian byte order.
    ''' </summary>
    Public Sub WriteUInt64BE(value As ULong)
        Dim bytes = BitConverter.GetBytes(value)
        Array.Reverse(bytes)
        _stream.Write(bytes, 0, 8)
    End Sub

    ''' <summary>
    ''' Writes a UInt16 in big-endian byte order.
    ''' </summary>
    Public Sub WriteUInt16BE(value As UShort)
        Dim bytes = BitConverter.GetBytes(value)
        Array.Reverse(bytes)
        _stream.Write(bytes, 0, 2)
    End Sub

    ''' <summary>
    ''' Writes raw bytes to the stream.
    ''' </summary>
    Public Sub WriteBytes(data As Byte())
        _stream.Write(data, 0, data.Length)
    End Sub

    ''' <summary>
    ''' Writes a specified number of zero bytes.
    ''' </summary>
    Public Sub WritePadding(count As Integer)
        If count <= 0 Then Return
        Dim zeros(count - 1) As Byte
        _stream.Write(zeros, 0, count)
    End Sub

    ''' <summary>
    ''' Advances the stream position to the next multiple of the specified boundary,
    ''' writing zero bytes as padding.
    ''' </summary>
    Public Sub AlignTo(boundary As Integer)
        If boundary <= 0 Then Return
        Dim pos = _stream.Position
        Dim remainder = CInt(pos Mod boundary)
        If remainder > 0 Then
            WritePadding(boundary - remainder)
        End If
    End Sub

    ''' <summary>
    ''' Seeks to an absolute position in the stream.
    ''' </summary>
    Public Sub SeekTo(position As Long)
        _stream.Seek(position, SeekOrigin.Begin)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If Not _disposed Then
            _stream?.Flush()
            _disposed = True
        End If
    End Sub

End Class
