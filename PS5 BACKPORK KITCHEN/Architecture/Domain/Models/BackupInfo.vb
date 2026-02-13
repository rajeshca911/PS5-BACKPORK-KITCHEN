Namespace Architecture.Domain.Models
    ''' <summary>
    ''' Information about a backup
    ''' </summary>
    Public Class BackupInfo
        Public Property BackupPath As String
        Public Property OriginalPath As String
        Public Property CreatedAt As DateTime
        Public Property SizeBytes As Long
    End Class
End Namespace
