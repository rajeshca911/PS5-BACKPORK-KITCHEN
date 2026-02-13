Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Errors
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models

Namespace Architecture.Application.Services
    ''' <summary>
    ''' Service for backup and restore operations
    ''' </summary>
    Public Interface IBackupService
        ''' <summary>
        ''' Creates a backup of a folder
        ''' </summary>
        Function CreateBackupAsync(sourcePath As String) As Task(Of Result(Of String))

        ''' <summary>
        ''' Restores from a backup
        ''' </summary>
        Function RestoreBackupAsync(backupPath As String, targetPath As String) As Task(Of Result(Of Unit))

        ''' <summary>
        ''' Lists available backups for a source path
        ''' </summary>
        Function ListBackupsAsync(sourcePath As String) As Task(Of Result(Of List(Of BackupInfo)))

        ''' <summary>
        ''' Deletes a backup
        ''' </summary>
        Function DeleteBackupAsync(backupPath As String) As Task(Of Result(Of Unit))
    End Interface
End Namespace
