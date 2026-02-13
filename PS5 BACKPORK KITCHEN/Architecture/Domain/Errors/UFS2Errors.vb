Namespace Architecture.Domain.Errors

    ''' <summary>
    ''' Error when a file is not a valid UFS2 image.
    ''' </summary>
    Public Class InvalidUFS2ImageError
        Inherits DomainError

        Private ReadOnly _filePath As String
        Private ReadOnly _reason As String

        Public Sub New(filePath As String, Optional reason As String = "")
            _filePath = filePath
            _reason = reason
        End Sub

        Public Overrides ReadOnly Property Code As String = "INVALID_UFS2_IMAGE"

        Public Overrides ReadOnly Property Message As String
            Get
                Dim detail = If(String.IsNullOrEmpty(_reason), "", $" - {_reason}")
                Return $"Invalid UFS2 image: {_filePath}{detail}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Il file non e' un'immagine UFS2 valida o e' corrotto."
        End Function
    End Class

    ''' <summary>
    ''' Error when reading data from a UFS2 image fails.
    ''' </summary>
    Public Class UFS2ReadError
        Inherits DomainError

        Private ReadOnly _filePath As String
        Private ReadOnly _innerException As Exception

        Public Sub New(filePath As String, innerException As Exception)
            _filePath = filePath
            _innerException = innerException
        End Sub

        Public Overrides ReadOnly Property Code As String = "UFS2_READ_ERROR"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Error reading UFS2 image: {_filePath} - {_innerException.Message}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Errore durante la lettura dell'immagine UFS2."
        End Function

        Public Overrides Function ToLogMessage() As String
            Return $"[{Code}] {Message}{Environment.NewLine}{_innerException.StackTrace}"
        End Function
    End Class

End Namespace
