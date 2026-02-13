Imports System.Net
Imports System.Net.Sockets
Imports System.IO

Namespace PS5_Payload_Sender

    Public Class TcpPayloadSender
        Implements IDisposable

        Private _socket As Socket
        Private _lastError As String = String.Empty

        Public ReadOnly Property IsConnected As Boolean
            Get
                Return _socket IsNot Nothing AndAlso _socket.Connected
            End Get
        End Property

        Public ReadOnly Property LastError As String
            Get
                Return _lastError
            End Get
        End Property

        ' -------------------------------
        ' Connect
        ' -------------------------------
        Public Function Connect(ip As String, port As Integer) As Boolean

            Try
                Disconnect()

                Dim ipAddress As IPAddress
                If Not IPAddress.TryParse(ip, ipAddress) Then
                    SetError("Invalid IP address.")
                    Return False
                End If

                _socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) With {
                    .ReceiveTimeout = 5000,
                    .SendTimeout = 5000,
                    .NoDelay = True 'reduces latency
                }

                _socket.Connect(New IPEndPoint(ipAddress, port))

                Return True
            Catch ex As SocketException
                SetError(MapSocketError(ex))
                Disconnect()
                Return False
            Catch ex As Exception
                SetError(ex.Message)
                Disconnect()
                Return False
            End Try

        End Function

        ' -------------------------------
        ' Send payload
        ' -------------------------------
        Public Function SendPayload(filePath As String) As Boolean

            Try
                If Not IsConnected Then
                    SetError("Not connected to PS5.")
                    Return False
                End If

                If Not File.Exists(filePath) Then
                    SetError("Payload file not found.")
                    Return False
                End If

                _socket.SendFile(filePath)

                Return True
            Catch ex As SocketException
                SetError(MapSocketError(ex))
                Disconnect()
                Return False
            Catch ex As Exception
                SetError(ex.Message)
                Disconnect()
                Return False
            End Try

        End Function

        ' -------------------------------
        ' Disconnect
        ' -------------------------------
        Public Sub Disconnect()

            If _socket Is Nothing Then Return

            Try
                If _socket.Connected Then
                    _socket.Shutdown(SocketShutdown.Both)
                End If
            Catch
                ' ignore shutdown errors
            Finally
                _socket.Close()
                _socket.Dispose()
                _socket = Nothing
            End Try

        End Sub

        ' -------------------------------
        ' Helpers
        ' -------------------------------
        Private Sub SetError(message As String)
            _lastError = message
        End Sub

        Private Function MapSocketError(ex As SocketException) As String

            Select Case ex.SocketErrorCode

                Case SocketError.TimedOut
                    Return "Connection timed out. PS5 not responding."

                Case SocketError.ConnectionRefused
                    Return "Connection refused. Payload server not running."

                Case SocketError.HostUnreachable
                    Return "Host unreachable. Check IP address."

                Case SocketError.NetworkUnreachable
                    Return "Network unreachable."

                Case Else
                    Return $"Socket error: {ex.Message}"

            End Select

        End Function

        ' -------------------------------
        ' IDisposable
        ' -------------------------------
        Public Sub Dispose() Implements IDisposable.Dispose
            Disconnect()
        End Sub

    End Class

End Namespace