'Module Logger
'    Public Sub Log(rtb As RichTextBox, text As String, Optional color As Color = Nothing, Optional Showtime As Boolean = True)
'        If rtb.InvokeRequired Then
'            rtb.Invoke(New Action(Of RichTextBox, String, Color)(AddressOf Log), rtb, text, color)
'            Return
'        End If

'        If color = Nothing Then color = Color.Black

'        rtb.SelectionStart = rtb.TextLength
'        rtb.SelectionLength = 0
'        rtb.SelectionColor = color
'        If Showtime Then
'            rtb.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}")
'        Else
'            rtb.AppendText($"{text}{Environment.NewLine}")
'        End If
'        'rtb.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}")
'        rtb.SelectionColor = rtb.ForeColor
'        rtb.ScrollToCaret()
'    End Sub

'End Module
Imports System.IO

Module Logger
    Private logFilePath As String = ""
    Private logWriter As StreamWriter = Nothing
    Private logLock As New Object()

    Public Enum LogLevel
        Info
        Success
        Warning
        [Error]
        Debug
    End Enum

    ''' <summary>
    ''' Initialize the file logger
    ''' </summary>
    Public Sub InitializeFileLogger(Optional customPath As String = "")
        Try
            If String.IsNullOrEmpty(customPath) Then
                logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILENAME)
            Else
                logFilePath = customPath
            End If

            ' Check if log file exceeds max size, rotate if needed
            If File.Exists(logFilePath) Then
                Dim fileInfo As New FileInfo(logFilePath)
                If fileInfo.Length > MAX_LOG_FILE_SIZE Then
                    Dim archivePath = Path.Combine(
                        Path.GetDirectoryName(logFilePath),
                        $"{Path.GetFileNameWithoutExtension(logFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.log"
                    )
                    File.Move(logFilePath, archivePath)
                End If
            End If

            logWriter = New StreamWriter(logFilePath, True) With {.AutoFlush = True}
            LogToFile($"=== {APP_NAME} v{APP_VERSION} - Session Started ===", LogLevel.Info)
        Catch ex As Exception
            ' Silent fail - logging to file is optional
        End Try
    End Sub

    ''' <summary>
    ''' Close the file logger
    ''' </summary>
    Public Sub CloseFileLogger()
        SyncLock logLock
            If logWriter IsNot Nothing Then
                Try
                    LogToFile("=== Session Ended ===", LogLevel.Info)
                    logWriter.Close()
                    logWriter.Dispose()
                    logWriter = Nothing
                Catch
                    ' Silent fail
                End Try
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Log to both RichTextBox and file
    ''' </summary>
    Public Sub Log(rtb As RichTextBox, text As String, Optional color As Color = Nothing, Optional showTime As Boolean = True, Optional level As LogLevel = LogLevel.Info)
        If rtb?.InvokeRequired Then
            rtb.Invoke(New Action(Of RichTextBox, String, Color, Boolean, LogLevel)(AddressOf Log), rtb, text, color, showTime, level)
            Return
        End If

        If color = Nothing Then color = Color.Black

        ' Log to RichTextBox
        If rtb IsNot Nothing Then
            rtb.SelectionStart = rtb.TextLength
            rtb.SelectionLength = 0
            rtb.SelectionColor = color
            If showTime Then
                rtb.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}")
            Else
                rtb.AppendText($"{text}{Environment.NewLine}")
            End If
            rtb.SelectionColor = rtb.ForeColor
            rtb.ScrollToCaret()
        End If

        ' Log to file
        LogToFile(text, level)
    End Sub

    ''' <summary>
    ''' Log only to file (no UI update) - Public for use by services
    ''' </summary>
    Public Sub LogToFile(text As String, level As LogLevel)
        If logWriter Is Nothing Then Return

        SyncLock logLock
            Try
                Dim levelStr = $"[{level.ToString().ToUpper()}]".PadRight(10)
                logWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {levelStr} {text}")
            Catch
                ' Silent fail
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Export current session logs to a specific file
    ''' </summary>
    Public Function ExportSessionLog(destinationPath As String, sessionData As String) As Boolean
        Try
            File.WriteAllText(destinationPath, sessionData)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

End Module