Namespace Architecture.Domain.Errors
    ''' <summary>
    ''' Base class for all domain-specific errors.
    ''' Provides structured error information for both user display and logging.
    ''' </summary>
    Public MustInherit Class DomainError
        ''' <summary>
        ''' Machine-readable error code (e.g., "FILE_NOT_FOUND")
        ''' </summary>
        Public MustOverride ReadOnly Property Code As String

        ''' <summary>
        ''' Technical error message for logging
        ''' </summary>
        Public MustOverride ReadOnly Property Message As String

        ''' <summary>
        ''' User-friendly error message (localized)
        ''' </summary>
        Public Overridable Function ToUserMessage() As String
            Return Message
        End Function

        ''' <summary>
        ''' Detailed error message for logging
        ''' </summary>
        Public Overridable Function ToLogMessage() As String
            Return $"[{Code}] {Message}"
        End Function
    End Class
End Namespace
