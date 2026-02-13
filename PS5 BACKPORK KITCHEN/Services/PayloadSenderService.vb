Imports System.IO
Imports System.Threading.Tasks
Imports System.Diagnostics

''' <summary>
''' Service for sending payloads to PS5 via various protocols
''' </summary>
Public Class PayloadSenderService

#Region "Data Models"

    ''' <summary>
    ''' Send operation result
    ''' </summary>
    Public Class SendResult
        Public Property Success As Boolean
        Public Property PayloadName As String
        Public Property Protocol As String
        Public Property TargetHost As String
        Public Property DurationMs As Integer
        Public Property ErrorMessage As String
        Public Property BytesTransferred As Long
    End Class

    ''' <summary>
    ''' Batch send result
    ''' </summary>
    Public Class BatchSendResult
        Public Property TotalPayloads As Integer
        Public Property SuccessCount As Integer
        Public Property FailedCount As Integer
        Public Property TotalDurationMs As Integer
        Public Property Results As List(Of SendResult) = New List(Of SendResult)

        Public ReadOnly Property SuccessRate As Double
            Get
                Return If(TotalPayloads > 0, (SuccessCount / TotalPayloads) * 100, 0)
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Send progress information
    ''' </summary>
    Public Class SendProgress
        Public Property PayloadName As String
        Public Property CurrentPayload As Integer
        Public Property TotalPayloads As Integer
        Public Property Status As String
        Public Property PercentComplete As Double

        Public ReadOnly Property ProgressText As String
            Get
                Return $"[{CurrentPayload}/{TotalPayloads}] {PayloadName}: {Status}"
            End Get
        End Property

    End Class

#End Region

#Region "FTP Send"

    ''' <summary>
    ''' Send single payload via FTP
    ''' </summary>
    Public Shared Async Function SendViaFtpAsync(payload As PayloadInfo, Optional progressCallback As IProgress(Of String) = Nothing) As Task(Of SendResult)
        Dim result As New SendResult With {
            .PayloadName = payload.Name,
            .Protocol = "FTP",
            .TargetHost = If(FtpManager.ActiveProfile?.Host, "Unknown")
        }

        Dim stopwatch = Diagnostics.Stopwatch.StartNew()

        Try
            ' Check if connected
            If Not FtpManager.IsConnected Then
                Throw New Exception("Not connected to FTP server. Please connect first.")
            End If

            ' Check if local file exists
            If Not File.Exists(payload.LocalPath) Then
                Throw New Exception($"Payload file not found: {payload.LocalPath}")
            End If

            ' Get file size
            Dim fileInfo As New FileInfo(payload.LocalPath)
            result.BytesTransferred = fileInfo.Length

            progressCallback?.Report($"Uploading {payload.Name} ({FtpManager.FormatFileSize(fileInfo.Length)})...")

            ' Upload to PS5
            Dim success = Await FtpManager.UploadFileAsync(payload.LocalPath, payload.TargetPath)

            If Not success Then
                Throw New Exception("Upload failed (FTP operation returned false)")
            End If

            stopwatch.Stop()
            result.Success = True
            result.DurationMs = CInt(stopwatch.ElapsedMilliseconds)
            result.ErrorMessage = ""

            progressCallback?.Report($"✓ {payload.Name} sent successfully in {result.DurationMs} ms")
        Catch ex As Exception
            stopwatch.Stop()
            result.Success = False
            result.DurationMs = CInt(stopwatch.ElapsedMilliseconds)
            result.ErrorMessage = ex.Message

            progressCallback?.Report($"✗ Failed to send {payload.Name}: {ex.Message}")
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Send multiple payloads via FTP (batch operation)
    ''' </summary>
    Public Shared Async Function SendBatchViaFtpAsync(payloads As List(Of PayloadInfo), Optional progressCallback As IProgress(Of SendProgress) = Nothing) As Task(Of BatchSendResult)
        Dim batchResult As New BatchSendResult With {
            .TotalPayloads = payloads.Count
        }

        Dim stopwatch = Diagnostics.Stopwatch.StartNew()

        Try
            ' Check if connected
            If Not FtpManager.IsConnected Then
                Throw New Exception("Not connected to FTP server. Please connect first.")
            End If

            Dim currentIndex = 0

            For Each payload In payloads
                currentIndex += 1

                ' Report progress
                progressCallback?.Report(New SendProgress With {
                    .PayloadName = payload.Name,
                    .CurrentPayload = currentIndex,
                    .TotalPayloads = payloads.Count,
                    .Status = "Uploading...",
                    .PercentComplete = ((currentIndex - 1) / payloads.Count) * 100
                })

                ' Send payload
                Dim sendProgress As New Progress(Of String)(Sub(status)
                                                                progressCallback?.Report(New SendProgress With {
                                                                    .PayloadName = payload.Name,
                                                                    .CurrentPayload = currentIndex,
                                                                    .TotalPayloads = payloads.Count,
                                                                    .Status = status,
                                                                    .PercentComplete = ((currentIndex - 1) / payloads.Count) * 100
                                                                })
                                                            End Sub)

                Dim result = Await SendViaFtpAsync(payload, sendProgress)

                batchResult.Results.Add(result)

                If result.Success Then
                    batchResult.SuccessCount += 1
                Else
                    batchResult.FailedCount += 1
                End If

                ' Report completion of this payload
                progressCallback?.Report(New SendProgress With {
                    .PayloadName = payload.Name,
                    .CurrentPayload = currentIndex,
                    .TotalPayloads = payloads.Count,
                    .Status = If(result.Success, "✓ Completed", $"✗ Failed: {result.ErrorMessage}"),
                    .PercentComplete = (currentIndex / payloads.Count) * 100
                })
            Next

            stopwatch.Stop()
            batchResult.TotalDurationMs = CInt(stopwatch.ElapsedMilliseconds)
        Catch ex As Exception
            stopwatch.Stop()
            batchResult.TotalDurationMs = CInt(stopwatch.ElapsedMilliseconds)

            ' Add error result for any remaining payloads
            For i = batchResult.Results.Count To payloads.Count - 1
                batchResult.Results.Add(New SendResult With {
                    .Success = False,
                    .PayloadName = payloads(i).Name,
                    .Protocol = "FTP",
                    .ErrorMessage = $"Batch operation aborted: {ex.Message}"
                })
                batchResult.FailedCount += 1
            Next
        End Try

        Return batchResult
    End Function

#End Region

#Region "Quick Send"

    ''' <summary>
    ''' Quick send - connect, send payload, optionally disconnect
    ''' </summary>
    Public Shared Async Function QuickSendAsync(payload As PayloadInfo, ftpProfile As FtpManager.FtpProfile, Optional autoDisconnect As Boolean = False, Optional progressCallback As IProgress(Of String) = Nothing) As Task(Of SendResult)
        Dim wasConnected = FtpManager.IsConnected
        Dim originalProfile = FtpManager.ActiveProfile
        Dim needsCleanupDisconnect As Boolean = False
        Dim result As SendResult = Nothing

        Try
            ' Connect if needed
            If Not wasConnected OrElse originalProfile?.Host <> ftpProfile.Host Then
                progressCallback?.Report("Connecting to PS5...")
                Await FtpManager.ConnectAsync(ftpProfile)
            End If

            ' Send payload
            result = Await SendViaFtpAsync(payload, progressCallback)

            ' Disconnect if requested and we connected in this call
            If autoDisconnect AndAlso Not wasConnected Then
                progressCallback?.Report("Disconnecting...")
                Await FtpManager.DisconnectAsync()
            End If
        Catch ex As Exception
            ' Mark for cleanup disconnect
            If Not wasConnected AndAlso FtpManager.IsConnected Then
                needsCleanupDisconnect = True
            End If

            result = New SendResult With {
                .Success = False,
                .PayloadName = payload.Name,
                .Protocol = "FTP",
                .TargetHost = ftpProfile.Host,
                .ErrorMessage = ex.Message
            }
        End Try

        ' Cleanup disconnect outside Catch block
        If needsCleanupDisconnect Then
            Try
                Await FtpManager.DisconnectAsync()
            Catch
                ' Ignore disconnect errors
            End Try
        End If

        Return result
    End Function

#End Region

#Region "HTTP Send (Future)"

    ''' <summary>
    ''' Send payload via HTTP (planned for future implementation)
    ''' </summary>
    Public Shared Async Function SendViaHttpAsync(payload As PayloadInfo, targetUrl As String) As Task(Of SendResult)
        ' Placeholder for HTTP implementation
        Await Task.Delay(100)

        Return New SendResult With {
            .Success = False,
            .PayloadName = payload.Name,
            .Protocol = "HTTP",
            .ErrorMessage = "HTTP protocol not yet implemented. Coming in v2.3!"
        }
    End Function

#End Region

#Region "USB Send (Future)"

    ''' <summary>
    ''' Send payload via USB (planned for future implementation)
    ''' </summary>
    Public Shared Async Function SendViaUsbAsync(payload As PayloadInfo) As Task(Of SendResult)
        ' Placeholder for USB implementation
        Await Task.Delay(100)

        Return New SendResult With {
            .Success = False,
            .PayloadName = payload.Name,
            .Protocol = "USB",
            .ErrorMessage = "USB protocol not yet implemented. Coming in v2.3!"
        }
    End Function

#End Region

#Region "Validation"

    ''' <summary>
    ''' Validate payload before sending
    ''' </summary>
    Public Shared Function ValidatePayload(payload As PayloadInfo) As (Valid As Boolean, ErrorMessage As String)
        ' Check file exists
        If Not File.Exists(payload.LocalPath) Then
            Return (False, $"Payload file not found: {payload.LocalPath}")
        End If

        ' Check file size
        Dim fileInfo As New FileInfo(payload.LocalPath)
        If fileInfo.Length = 0 Then
            Return (False, "Payload file is empty")
        End If

        ' Check file size is reasonable (< 100MB for safety)
        If fileInfo.Length > 100 * 1024 * 1024 Then
            Return (False, $"Payload file is very large ({FtpManager.FormatFileSize(fileInfo.Length)}). Are you sure this is a payload?")
        End If

        ' Check target path is valid
        If String.IsNullOrWhiteSpace(payload.TargetPath) Then
            Return (False, "Target path is not specified")
        End If

        Return (True, "")
    End Function

    ''' <summary>
    ''' Check if ready to send (FTP connected)
    ''' </summary>
    Public Shared Function IsReadyToSend() As (Ready As Boolean, ErrorMessage As String)
        If Not FtpManager.IsConnected Then
            Return (False, "Not connected to PS5 via FTP. Please connect first.")
        End If

        Return (True, "")
    End Function

#End Region

#Region "Helper Methods"

    ''' <summary>
    ''' Format send result as readable text
    ''' </summary>
    Public Shared Function FormatSendResult(result As SendResult) As String
        Dim status = If(result.Success, "✓ SUCCESS", "✗ FAILED")
        Dim duration = $"{result.DurationMs} ms"
        Dim size = If(result.BytesTransferred > 0, $" ({FtpManager.FormatFileSize(result.BytesTransferred)})", "")

        If result.Success Then
            Return $"{status} | {result.PayloadName} | {result.Protocol} → {result.TargetHost} | {duration}{size}"
        Else
            Return $"{status} | {result.PayloadName} | {result.ErrorMessage}"
        End If
    End Function

    ''' <summary>
    ''' Format batch result as readable text
    ''' </summary>
    Public Shared Function FormatBatchResult(result As BatchSendResult) As String
        Dim summary = $"Batch Send Complete: {result.SuccessCount}/{result.TotalPayloads} successful ({result.SuccessRate:F1}%)"
        summary &= vbCrLf & $"Duration: {result.TotalDurationMs / 1000.0:F2} seconds"

        If result.FailedCount > 0 Then
            summary &= vbCrLf & vbCrLf & "Failed payloads:"
            For Each failed In result.Results.Where(Function(r) Not r.Success)
                summary &= vbCrLf & $"  • {failed.PayloadName}: {failed.ErrorMessage}"
            Next
        End If

        Return summary
    End Function

#End Region

End Class