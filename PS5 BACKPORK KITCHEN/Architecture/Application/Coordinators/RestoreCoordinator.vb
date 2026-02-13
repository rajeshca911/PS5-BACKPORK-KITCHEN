Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Services
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Errors
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters

Namespace Architecture.Application.Coordinators
    ''' <summary>
    ''' Coordinates restore operations from backups
    ''' </summary>
    Public Class RestoreCoordinator
        Private ReadOnly _backupService As IBackupService
        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger

        Public Sub New(backupService As IBackupService,
                      fileSystem As IFileSystem,
                      logger As ILogger)
            _backupService = backupService
            _fileSystem = fileSystem
            _logger = logger
        End Sub

        ''' <summary>
        ''' Execute restore operation with validation
        ''' </summary>
        Public Async Function ExecuteRestoreAsync(
            backupPath As String,
            targetPath As String,
            options As RestoreOptions,
            progress As IProgress(Of RestoreProgress),
            cancellationToken As Threading.CancellationToken) As Task(Of Result(Of RestoreSummary))

            Dim operationId = Guid.NewGuid()
            Dim startTime = DateTime.Now

            _logger.LogInfo($"Starting restore operation {operationId} from {backupPath} to {targetPath}")

            Try
                ' Step 1: Validate backup exists
                progress?.Report(New RestoreProgress With {
                    .Stage = "Validating",
                    .Message = "Validating backup...",
                    .Percentage = 10
                })

                Dim backupExists = Await _fileSystem.DirectoryExistsAsync(backupPath)
                If Not backupExists Then
                    Return Result(Of RestoreSummary).Fail(New DirectoryNotFoundError(backupPath))
                End If

                ' Step 2: Create target backup if requested
                Dim preRestoreBackupPath As String = Nothing
                If options.CreateBackupBeforeRestore AndAlso Await _fileSystem.DirectoryExistsAsync(targetPath) Then
                    progress?.Report(New RestoreProgress With {
                        .Stage = "PreBackup",
                        .Message = "Creating pre-restore backup...",
                        .Percentage = 25
                    })

                    _logger.LogInfo("Creating pre-restore backup")
                    Dim preBackupResult = Await _backupService.CreateBackupAsync(targetPath)

                    If preBackupResult.IsSuccess Then
                        preRestoreBackupPath = preBackupResult.Value
                        _logger.LogInfo($"Pre-restore backup created: {preRestoreBackupPath}")
                    ElseIf Not options.ContinueOnError Then
                        Return Result(Of RestoreSummary).Fail(preBackupResult.Error)
                    End If
                End If

                cancellationToken.ThrowIfCancellationRequested()

                ' Step 3: Restore from backup
                progress?.Report(New RestoreProgress With {
                    .Stage = "Restoring",
                    .Message = "Restoring files...",
                    .Percentage = 50
                })

                _logger.LogInfo($"Restoring from {backupPath}")
                Dim restoreResult = Await _backupService.RestoreBackupAsync(backupPath, targetPath)

                If Not restoreResult.IsSuccess Then
                    _logger.LogError($"Restore failed: {restoreResult.Error.Message}")
                    Return Result(Of RestoreSummary).Fail(restoreResult.Error)
                End If

                ' Step 4: Verify integrity if requested
                If options.VerifyIntegrity Then
                    progress?.Report(New RestoreProgress With {
                        .Stage = "Verifying",
                        .Message = "Verifying restored files...",
                        .Percentage = 75
                    })

                    _logger.LogInfo("Verifying restored files integrity")
                    Dim verifyResult = IntegrityVerifier.VerifyPatchedFiles(targetPath)

                    If Not verifyResult.IsValid AndAlso Not options.ContinueOnError Then
                        Return Result(Of RestoreSummary).Fail(
                            New FileAccessError(targetPath, New Exception($"Integrity verification failed: {verifyResult.Message}")))
                    End If
                End If

                ' Step 5: Cleanup old backup if requested
                If options.DeleteBackupAfterRestore Then
                    progress?.Report(New RestoreProgress With {
                        .Stage = "Cleanup",
                        .Message = "Cleaning up backup...",
                        .Percentage = 90
                    })

                    _logger.LogInfo($"Deleting backup: {backupPath}")
                    Await _backupService.DeleteBackupAsync(backupPath)
                End If

                ' Final progress
                progress?.Report(New RestoreProgress With {
                    .Stage = "Completed",
                    .Message = "Restore completed successfully",
                    .Percentage = 100
                })

                Dim duration = DateTime.Now - startTime

                Dim summary = New RestoreSummary With {
                    .OperationId = operationId,
                    .BackupPath = backupPath,
                    .TargetPath = targetPath,
                    .PreRestoreBackupPath = preRestoreBackupPath,
                    .Success = True,
                    .Duration = duration
                }

                _logger.LogInfo($"Restore operation completed successfully in {duration.TotalSeconds:F2}s")

                Return Result(Of RestoreSummary).Success(summary)

            Catch ex As OperationCanceledException
                _logger.LogWarning($"Restore operation {operationId} cancelled")
                Throw

            Catch ex As Exception
                _logger.LogError("Unexpected error during restore", ex)
                Return Result(Of RestoreSummary).Fail(New UnexpectedError(ex))
            End Try
        End Function

        ''' <summary>
        ''' List available backups for a source path
        ''' </summary>
        Public Async Function ListAvailableBackupsAsync(sourcePath As String) As Task(Of Result(Of List(Of BackupInfo)))
            Try
                _logger.LogInfo($"Listing available backups for {sourcePath}")
                Return Await _backupService.ListBackupsAsync(sourcePath)
            Catch ex As Exception
                _logger.LogError($"Failed to list backups for {sourcePath}", ex)
                Return Result(Of List(Of BackupInfo)).Fail(New FileAccessError(sourcePath, ex))
            End Try
        End Function
    End Class

    ''' <summary>
    ''' Options for restore operations
    ''' </summary>
    Public Class RestoreOptions
        Public Property CreateBackupBeforeRestore As Boolean = True
        Public Property VerifyIntegrity As Boolean = True
        Public Property DeleteBackupAfterRestore As Boolean = False
        Public Property ContinueOnError As Boolean = False
    End Class

    ''' <summary>
    ''' Progress reporting for restore operations
    ''' </summary>
    Public Class RestoreProgress
        Public Property Stage As String
        Public Property Message As String
        Public Property Percentage As Integer
    End Class

    ''' <summary>
    ''' Summary of a restore operation
    ''' </summary>
    Public Class RestoreSummary
        Public Property OperationId As Guid
        Public Property BackupPath As String
        Public Property TargetPath As String
        Public Property PreRestoreBackupPath As String
        Public Property Success As Boolean
        Public Property Duration As TimeSpan
    End Class
End Namespace
