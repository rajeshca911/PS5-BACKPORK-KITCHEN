Namespace Architecture.Domain.Errors

    ''' <summary>
    ''' Error when FPKG configuration is invalid.
    ''' </summary>
    Public Class InvalidFPKGConfigError
        Inherits DomainError

        Private ReadOnly _details As String

        Public Sub New(details As String)
            _details = details
        End Sub

        Public Overrides ReadOnly Property Code As String = "INVALID_FPKG_CONFIG"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Invalid FPKG configuration: {_details}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return $"Configurazione FPKG non valida: {_details}"
        End Function
    End Class

    ''' <summary>
    ''' Error when FPKG build process fails.
    ''' </summary>
    Public Class FPKGBuildError
        Inherits DomainError

        Private ReadOnly _message As String
        Private ReadOnly _innerException As Exception

        Public Sub New(message As String, Optional innerException As Exception = Nothing)
            _message = message
            _innerException = innerException
        End Sub

        Public Overrides ReadOnly Property Code As String = "FPKG_BUILD_FAILED"

        Public Overrides ReadOnly Property Message As String
            Get
                If _innerException IsNot Nothing Then
                    Return $"FPKG build failed: {_message} - {_innerException.Message}"
                End If
                Return $"FPKG build failed: {_message}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return $"Impossibile creare il pacchetto FPKG: {_message}"
        End Function

        Public Overrides Function ToLogMessage() As String
            Dim log = $"[{Code}] {Message}"
            If _innerException IsNot Nothing Then
                log &= Environment.NewLine & _innerException.StackTrace
            End If
            Return log
        End Function
    End Class

End Namespace
