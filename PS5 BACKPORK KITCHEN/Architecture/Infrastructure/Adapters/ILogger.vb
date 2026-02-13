Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' Abstraction for logging to enable dependency injection and testing
    ''' </summary>
    Public Interface ILogger
        Sub LogInfo(message As String)
        Sub LogWarning(message As String)
        Sub LogError(message As String)
        Sub LogError(message As String, exception As Exception)
        Sub LogDebug(message As String)

        Function IsEnabled(level As LogLevel) As Boolean
    End Interface

    Public Enum LogLevel
        Debug = 0
        Info = 1
        Warning = 2
        [Error] = 3
    End Enum
End Namespace
