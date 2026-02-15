Namespace Architecture.Domain.Models
    ''' <summary>
    ''' Represents a patching operation (Value Object)
    ''' </summary>
    Public Class PatchOperation
        Public Sub New(filePath As String,
                      currentSdk As Long,
                      targetSdk As Long)
            Me.FilePath = filePath
            Me.CurrentSdk = currentSdk
            Me.TargetSdk = targetSdk
        End Sub

        Public ReadOnly Property FilePath As String
        Public ReadOnly Property CurrentSdk As Long
        Public ReadOnly Property TargetSdk As Long

        Public ReadOnly Property RequiresPatching As Boolean
            Get
                Return CurrentSdk <> TargetSdk
            End Get
        End Property

        Public ReadOnly Property IsDowngrade As Boolean
            Get
                Return TargetSdk < CurrentSdk
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Result of a patch operation
    ''' </summary>
    Public Class PatchResult
        Public Property FilePath As String
        Public Property OriginalSdk As Long
        Public Property PatchedSdk As Long
        Public Property BytesWritten As Long
        Public Property Duration As TimeSpan
        Public Property StatusMessage As String

    End Class

    ''' <summary>
    ''' Options for patching operations
    ''' </summary>
    Public Class PatchOptions
        Public Property AutoBackup As Boolean = True
        Public Property AutoVerify As Boolean = True
        Public Property SkipAlreadyPatched As Boolean = True
        Public Property ContinueOnError As Boolean = True
    End Class
End Namespace
