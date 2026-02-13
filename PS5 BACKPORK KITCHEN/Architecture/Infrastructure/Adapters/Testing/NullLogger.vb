Namespace Architecture.Infrastructure.Adapters.Testing
    ''' <summary>
    ''' Logger that does nothing - for unit testing
    ''' </summary>
    Public Class NullLogger
        Implements ILogger

        Public Sub LogInfo(message As String) Implements ILogger.LogInfo
            ' Do nothing
        End Sub

        Public Sub LogWarning(message As String) Implements ILogger.LogWarning
            ' Do nothing
        End Sub

        Public Sub LogError(message As String) Implements ILogger.LogError
            ' Do nothing
        End Sub

        Public Sub LogError(message As String, exception As Exception) Implements ILogger.LogError
            ' Do nothing
        End Sub

        Public Sub LogDebug(message As String) Implements ILogger.LogDebug
            ' Do nothing
        End Sub

        Public Function IsEnabled(level As LogLevel) As Boolean Implements ILogger.IsEnabled
            Return False
        End Function
    End Class
End Namespace
