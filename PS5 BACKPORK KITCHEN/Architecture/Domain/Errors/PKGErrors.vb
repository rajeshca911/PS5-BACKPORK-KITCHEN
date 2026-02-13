Namespace Architecture.Domain.Errors

    ''' <summary>
    ''' Error when a file is not a valid PKG/FPKG package.
    ''' </summary>
    Public Class InvalidPKGError
        Inherits DomainError

        Private ReadOnly _filePath As String
        Private ReadOnly _reason As String

        Public Sub New(filePath As String, Optional reason As String = "")
            _filePath = filePath
            _reason = reason
        End Sub

        Public Overrides ReadOnly Property Code As String = "INVALID_PKG"

        Public Overrides ReadOnly Property Message As String
            Get
                Dim detail = If(String.IsNullOrEmpty(_reason), "", $" - {_reason}")
                Return $"Invalid PKG file: {_filePath}{detail}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Il file non e' un pacchetto PKG valido o e' corrotto."
        End Function
    End Class

    ''' <summary>
    ''' Error when a PKG is encrypted and cannot be extracted.
    ''' </summary>
    Public Class PKGEncryptedError
        Inherits DomainError

        Private ReadOnly _filePath As String
        Private ReadOnly _drmType As UInteger

        Public Sub New(filePath As String, drmType As UInteger)
            _filePath = filePath
            _drmType = drmType
        End Sub

        Public Overrides ReadOnly Property Code As String = "PKG_ENCRYPTED"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"PKG is encrypted (DRM type 0x{_drmType:X}): {_filePath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Il pacchetto PKG e' crittografato. Solo i pacchetti FPKG possono essere estratti."
        End Function
    End Class

End Namespace
