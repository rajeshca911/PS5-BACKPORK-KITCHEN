Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

''' <summary>
''' Handles remote PKG installation to PS5 using etaHEN DPI v1 protocol.
''' 1. Starts a local HTTP server to serve the PKG file
''' 2. Sends a TCP JSON command to PS5 port 9090 with the PKG URL
''' 3. PS5 fetches and installs the PKG over HTTP
''' </summary>
Public Class RemotePkgInstallService
    Implements IDisposable

    Private _httpListener As HttpListener
    Private _listenerThread As Thread
    Private _cancellationTokenSource As CancellationTokenSource
    Private _isServing As Boolean = False
    Private _filePath As String
    Private _totalBytes As Long
    Private _servedBytes As Long

    Public Event StatusChanged(message As String)
    Public Event FileServeProgress(servedBytes As Long, totalBytes As Long)
    Public Event FileServeCompleted()

    Public ReadOnly Property IsServing As Boolean
        Get
            Return _isServing
        End Get
    End Property

    Public ReadOnly Property ServedBytes As Long
        Get
            Return _servedBytes
        End Get
    End Property

    ''' <summary>
    ''' Tests TCP connection to PS5 on port 9090 (etaHEN DPI).
    ''' </summary>
    Public Async Function TestConnectionAsync(ps5Ip As String) As Task(Of Boolean)
        Try
            Using client As New TcpClient()
                Dim connectTask = client.ConnectAsync(ps5Ip, 9090)
                Dim timeoutTask = Task.Delay(5000)

                Dim completed = Await Task.WhenAny(connectTask, timeoutTask)
                If completed Is timeoutTask Then
                    Return False
                End If

                If connectTask.IsFaulted Then Return False
                Return client.Connected
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Sends the install command to PS5 via TCP JSON on port 9090.
    ''' Returns True if PS5 accepted the request.
    ''' </summary>
    Public Async Function SendInstallCommandAsync(ps5Ip As String, pkgUrl As String) As Task(Of String)
        Try
            Using client As New TcpClient()
                Await client.ConnectAsync(ps5Ip, 9090)

                Dim json = $"{{""type"":""http"",""url"":""{pkgUrl}""}}"
                Dim data = Encoding.UTF8.GetBytes(json)

                Dim stream = client.GetStream()
                Await stream.WriteAsync(data, 0, data.Length)
                Await stream.FlushAsync()

                ' Read response with timeout
                client.ReceiveTimeout = 10000
                Dim buffer(1023) As Byte
                Dim bytesRead = Await stream.ReadAsync(buffer, 0, buffer.Length)

                If bytesRead > 0 Then
                    Return Encoding.UTF8.GetString(buffer, 0, bytesRead)
                End If

                Return ""
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to send install command: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Starts an HTTP server that serves a single PKG file.
    ''' </summary>
    Public Sub StartFileServer(filePath As String, port As Integer)
        If _isServing Then
            Throw New InvalidOperationException("Server is already running.")
        End If

        If Not File.Exists(filePath) Then
            Throw New FileNotFoundException("PKG file not found.", filePath)
        End If

        _filePath = filePath
        _totalBytes = New FileInfo(filePath).Length
        _servedBytes = 0
        _cancellationTokenSource = New CancellationTokenSource()

        ' Try to start HttpListener
        _httpListener = New HttpListener()

        ' Use wildcard binding - may need admin rights
        Dim prefix = $"http://*:{port}/"
        _httpListener.Prefixes.Add(prefix)

        Try
            _httpListener.Start()
        Catch ex As HttpListenerException
            ' Fall back to localhost binding
            _httpListener.Close()
            _httpListener = New HttpListener()
            prefix = $"http://+:{port}/"
            _httpListener.Prefixes.Add(prefix)
            Try
                _httpListener.Start()
            Catch
                ' Last resort: localhost only
                _httpListener.Close()
                _httpListener = New HttpListener()
                prefix = $"http://localhost:{port}/"
                _httpListener.Prefixes.Add(prefix)
                _httpListener.Start()
            End Try
        End Try

        _isServing = True
        RaiseEvent StatusChanged($"HTTP server started on port {port}")

        _listenerThread = New Thread(AddressOf ListenerLoop)
        _listenerThread.IsBackground = True
        _listenerThread.Start()
    End Sub

    Private Sub ListenerLoop()
        While _isServing AndAlso Not _cancellationTokenSource.IsCancellationRequested
            Try
                Dim context = _httpListener.GetContext()
                ThreadPool.QueueUserWorkItem(Sub() HandleRequest(context))
            Catch ex As HttpListenerException
                If _isServing Then
                    RaiseEvent StatusChanged($"Listener error: {ex.Message}")
                End If
            Catch ex As ObjectDisposedException
                Exit While
            End Try
        End While
    End Sub

    Private Sub HandleRequest(context As HttpListenerContext)
        Try
            Dim request = context.Request
            Dim response = context.Response

            RaiseEvent StatusChanged($"Request: {request.HttpMethod} {request.Url.AbsolutePath}")

            ' Serve the PKG file for any path
            If Not File.Exists(_filePath) Then
                response.StatusCode = 404
                response.Close()
                Return
            End If

            Dim fileInfo As New FileInfo(_filePath)
            response.ContentType = "application/octet-stream"
            response.ContentLength64 = fileInfo.Length
            response.AddHeader("Content-Disposition", $"attachment; filename=""{fileInfo.Name}""")
            response.StatusCode = 200

            ' Handle Range requests (PS5 may use partial downloads)
            Dim rangeHeader = request.Headers("Range")
            Dim startByte As Long = 0
            Dim endByte As Long = fileInfo.Length - 1

            If Not String.IsNullOrEmpty(rangeHeader) AndAlso rangeHeader.StartsWith("bytes=") Then
                Dim range = rangeHeader.Substring(6).Split("-"c)
                If range.Length >= 1 AndAlso Long.TryParse(range(0), startByte) Then
                    If range.Length >= 2 AndAlso Not String.IsNullOrEmpty(range(1)) Then
                        Long.TryParse(range(1), endByte)
                    End If
                End If

                response.StatusCode = 206
                response.ContentLength64 = endByte - startByte + 1
                response.AddHeader("Content-Range", $"bytes {startByte}-{endByte}/{fileInfo.Length}")
            End If

            ' Stream file
            Using fs As New FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                fs.Seek(startByte, SeekOrigin.Begin)

                Dim buffer(65535) As Byte
                Dim remaining = endByte - startByte + 1

                While remaining > 0 AndAlso Not _cancellationTokenSource.IsCancellationRequested
                    Dim toRead = CInt(Math.Min(buffer.Length, remaining))
                    Dim bytesRead = fs.Read(buffer, 0, toRead)
                    If bytesRead = 0 Then Exit While

                    Try
                        response.OutputStream.Write(buffer, 0, bytesRead)
                        response.OutputStream.Flush()
                    Catch
                        Exit While
                    End Try

                    remaining -= bytesRead
                    _servedBytes += bytesRead
                    RaiseEvent FileServeProgress(_servedBytes, _totalBytes)
                End While
            End Using

            response.Close()

            If _servedBytes >= _totalBytes Then
                RaiseEvent StatusChanged("File transfer complete")
                RaiseEvent FileServeCompleted()
            End If

        Catch ex As Exception
            RaiseEvent StatusChanged($"Request error: {ex.Message}")
            Try
                context.Response.StatusCode = 500
                context.Response.Close()
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Stops the HTTP file server.
    ''' </summary>
    Public Sub StopFileServer()
        _isServing = False
        _cancellationTokenSource?.Cancel()

        Try
            _httpListener?.Stop()
            _httpListener?.Close()
        Catch
        End Try

        RaiseEvent StatusChanged("HTTP server stopped")
    End Sub

    ''' <summary>
    ''' Auto-detects local LAN IP address.
    ''' </summary>
    Public Shared Function GetLocalIpAddress() As String
        Try
            Dim host = Dns.GetHostEntry(Dns.GetHostName())
            For Each addr In host.AddressList
                If addr.AddressFamily = AddressFamily.InterNetwork Then
                    Dim ip = addr.ToString()
                    If ip.StartsWith("192.168.") OrElse
                       ip.StartsWith("10.") OrElse
                       ip.StartsWith("172.") Then
                        Return ip
                    End If
                End If
            Next

            ' Fallback: return first IPv4
            For Each addr In host.AddressList
                If addr.AddressFamily = AddressFamily.InterNetwork AndAlso
                   addr.ToString() <> "127.0.0.1" Then
                    Return addr.ToString()
                End If
            Next
        Catch
        End Try

        Return "127.0.0.1"
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        StopFileServer()
        _cancellationTokenSource?.Dispose()
    End Sub

End Class
