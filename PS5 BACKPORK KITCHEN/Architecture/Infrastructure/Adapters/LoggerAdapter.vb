Imports System.IO

Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' Simple file-based logger implementation
    ''' TODO: Wire to existing Services.Logger when namespace issues resolved
    ''' </summary>
    Public Class LoggerAdapter
        Implements ILogger

        Private Shared ReadOnly _lock As New Object()
        Private Shared ReadOnly _logFile As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log")

        Private Sub WriteLog(level As String, message As String)
            Try
                SyncLock _lock
                    Dim timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    Dim logEntry = $"[{timestamp}] [{level}] {message}{Environment.NewLine}"
                    File.AppendAllText(_logFile, logEntry)
                End SyncLock
            Catch
                ' Ignore logging errors
            End Try
        End Sub

        Public Sub LogInfo(message As String) Implements ILogger.LogInfo
            WriteLog("INFO", message)
        End Sub

        Public Sub LogWarning(message As String) Implements ILogger.LogWarning
            WriteLog("WARNING", message)
        End Sub

        Public Sub LogError(message As String) Implements ILogger.LogError
            WriteLog("ERROR", message)
        End Sub

        Public Sub LogError(message As String, exception As Exception) Implements ILogger.LogError
            WriteLog("ERROR", $"{message}{Environment.NewLine}{exception}")
        End Sub

        Public Sub LogDebug(message As String) Implements ILogger.LogDebug
            WriteLog("DEBUG", message)
        End Sub

        Public Function IsEnabled(level As LogLevel) As Boolean Implements ILogger.IsEnabled
            Return True
        End Function
    End Class
End Namespace
