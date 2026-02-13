Namespace Architecture.Domain.Errors
    ''' <summary>
    ''' Error when a file is not found
    ''' </summary>
    Public Class FileNotFoundError
        Inherits DomainError

        Private ReadOnly _filePath As String

        Public Sub New(filePath As String)
            _filePath = filePath
        End Sub

        Public Overrides ReadOnly Property Code As String = "FILE_NOT_FOUND"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"File not found: {_filePath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return $"Il file non è stato trovato: {IO.Path.GetFileName(_filePath)}"
        End Function
    End Class

    ''' <summary>
    ''' Error when a directory is not found
    ''' </summary>
    Public Class DirectoryNotFoundError
        Inherits DomainError

        Private ReadOnly _directoryPath As String

        Public Sub New(directoryPath As String)
            _directoryPath = directoryPath
        End Sub

        Public Overrides ReadOnly Property Code As String = "DIRECTORY_NOT_FOUND"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Directory not found: {_directoryPath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "La cartella specificata non esiste."
        End Function
    End Class

    ''' <summary>
    ''' Error when an ELF file has invalid format
    ''' </summary>
    Public Class InvalidElfFormatError
        Inherits DomainError

        Private ReadOnly _filePath As String

        Public Sub New(filePath As String)
            _filePath = filePath
        End Sub

        Public Overrides ReadOnly Property Code As String = "INVALID_ELF_FORMAT"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Invalid ELF format: {_filePath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Il file non è un ELF valido o è corrotto."
        End Function
    End Class

    ''' <summary>
    ''' Error when a file is already patched to the target SDK
    ''' </summary>
    Public Class AlreadyPatchedError
        Inherits DomainError

        Private ReadOnly _filePath As String
        Private ReadOnly _sdkVersion As Long

        Public Sub New(filePath As String, sdkVersion As Long)
            _filePath = filePath
            _sdkVersion = sdkVersion
        End Sub

        Public Overrides ReadOnly Property Code As String = "ALREADY_PATCHED"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"File already patched to SDK {_sdkVersion:X}: {_filePath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return $"Il file è già patchato alla versione SDK {_sdkVersion:X}."
        End Function
    End Class

    ''' <summary>
    ''' Error when file access fails
    ''' </summary>
    Public Class FileAccessError
        Inherits DomainError

        Private ReadOnly _filePath As String
        Private ReadOnly _innerException As Exception

        Public Sub New(filePath As String, innerException As Exception)
            _filePath = filePath
            _innerException = innerException
        End Sub

        Public Overrides ReadOnly Property Code As String = "FILE_ACCESS_ERROR"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Cannot access file: {_filePath} - {_innerException.Message}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Impossibile accedere al file. Controlla i permessi."
        End Function
    End Class

    ''' <summary>
    ''' Error when backup operation fails
    ''' </summary>
    Public Class BackupFailedError
        Inherits DomainError

        Private ReadOnly _sourcePath As String
        Private ReadOnly _innerException As Exception

        Public Sub New(sourcePath As String, Optional innerException As Exception = Nothing)
            _sourcePath = sourcePath
            _innerException = innerException
        End Sub

        Public Overrides ReadOnly Property Code As String = "BACKUP_FAILED"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Backup failed for {_sourcePath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Impossibile creare il backup. Verifica lo spazio disponibile su disco."
        End Function
    End Class

    ''' <summary>
    ''' Error when restore operation fails
    ''' </summary>
    Public Class RestoreFailedError
        Inherits DomainError

        Private ReadOnly _backupPath As String
        Private ReadOnly _innerException As Exception

        Public Sub New(backupPath As String, Optional innerException As Exception = Nothing)
            _backupPath = backupPath
            _innerException = innerException
        End Sub

        Public Overrides ReadOnly Property Code As String = "RESTORE_FAILED"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Restore failed from {_backupPath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Impossibile ripristinare il backup."
        End Function
    End Class

    ''' <summary>
    ''' Error when patch operation fails
    ''' </summary>
    Public Class PatchFailedError
        Inherits DomainError

        Private ReadOnly _filePath As String

        Public Sub New(filePath As String)
            _filePath = filePath
        End Sub

        Public Overrides ReadOnly Property Code As String = "PATCH_FAILED"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Failed to patch {_filePath}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Impossibile applicare la patch al file."
        End Function
    End Class

    ''' <summary>
    ''' Error when no files are found to patch
    ''' </summary>
    Public Class NoFilesFoundError
        Inherits DomainError

        Private ReadOnly _directory As String

        Public Sub New(directory As String)
            _directory = directory
        End Sub

        Public Overrides ReadOnly Property Code As String = "NO_FILES_FOUND"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"No patchable files found in {_directory}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Nessun file ELF trovato nella cartella selezionata."
        End Function
    End Class

    ''' <summary>
    ''' Error for unexpected exceptions
    ''' </summary>
    Public Class UnexpectedError
        Inherits DomainError

        Private ReadOnly _innerException As Exception

        Public Sub New(innerException As Exception)
            _innerException = innerException
        End Sub

        Public Overrides ReadOnly Property Code As String = "UNEXPECTED_ERROR"

        Public Overrides ReadOnly Property Message As String
            Get
                Return $"Unexpected error: {_innerException.Message}"
            End Get
        End Property

        Public Overrides Function ToUserMessage() As String
            Return "Si è verificato un errore imprevisto."
        End Function

        Public Overrides Function ToLogMessage() As String
            Return $"[{Code}] {Message}{Environment.NewLine}{_innerException.StackTrace}"
        End Function
    End Class
End Namespace
