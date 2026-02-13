Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Services
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Errors
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports System.IO

Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' Adapter that wraps the existing BackupService module
    ''' </summary>
    Public Class BackupServiceAdapter
        Implements IBackupService

        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger

        Public Sub New(fileSystem As IFileSystem, logger As ILogger)
            _fileSystem = fileSystem
            _logger = logger
        End Sub

        Public Async Function CreateBackupAsync(sourcePath As String) As Task(Of Result(Of String)) _
            Implements IBackupService.CreateBackupAsync

            Try
                _logger.LogInfo($"Creating backup for: {sourcePath}")

                ' Use existing BackupService module (legacy)
                Dim backupInfo = Await Task.Run(Function() BackupService.CreateBackupWithManifest(sourcePath))

                If backupInfo.Success Then
                    _logger.LogInfo($"Backup created successfully: {backupInfo.BackupPath} ({backupInfo.FilesCount} files, {backupInfo.TotalSize} bytes)")
                    Return Result(Of String).Success(backupInfo.BackupPath)
                Else
                    _logger.LogError($"Backup failed: {backupInfo.ErrorMessage}")
                    Return Result(Of String).Fail(New BackupFailedError(sourcePath, New Exception(backupInfo.ErrorMessage)))
                End If

            Catch ex As Exception
                _logger.LogError($"Backup failed for {sourcePath}", ex)
                Return Result(Of String).Fail(New BackupFailedError(sourcePath, ex))
            End Try
        End Function

        Public Async Function RestoreBackupAsync(backupPath As String, targetPath As String) As Task(Of Result(Of Unit)) _
            Implements IBackupService.RestoreBackupAsync

            Try
                _logger.LogInfo($"Restoring backup from {backupPath} to {targetPath}")

                ' Validate backup exists
                If Not Await _fileSystem.DirectoryExistsAsync(backupPath) Then
                    _logger.LogError($"Backup directory not found: {backupPath}")
                    Return Result(Of Unit).Fail(New DirectoryNotFoundError(backupPath))
                End If

                ' Use existing BackupService module (legacy)
                Dim success = Await Task.Run(Function() BackupService.RestoreFromBackup(backupPath, targetPath))

                If success Then
                    _logger.LogInfo($"Backup restored successfully to {targetPath}")
                    Return Result(Of Unit).Success(Unit.Value)
                Else
                    _logger.LogError($"Restore failed for {backupPath}")
                    Return Result(Of Unit).Fail(New RestoreFailedError(backupPath, New Exception("Restore operation failed")))
                End If

            Catch ex As Exception
                _logger.LogError("Restore failed", ex)
                Return Result(Of Unit).Fail(New RestoreFailedError(backupPath, ex))
            End Try
        End Function

        Public Async Function ListBackupsAsync(sourcePath As String) As Task(Of Result(Of List(Of BackupInfo))) _
            Implements IBackupService.ListBackupsAsync

            Try
                _logger.LogInfo($"Listing backups for: {sourcePath}")

                ' Use existing BackupService module (legacy)
                Dim backupPaths = Await Task.Run(Function() BackupService.ListBackups(sourcePath))

                Dim backupInfos As New List(Of BackupInfo)

                For Each backupPath In backupPaths
                    Dim manifestPath = Path.Combine(backupPath, "backup_manifest.json")

                    If File.Exists(manifestPath) Then
                        Try
                            Dim manifestJson = File.ReadAllText(manifestPath)
                            Dim manifestData = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(manifestJson)

                            Dim backupInfo As New BackupInfo With {
                                .BackupPath = backupPath,
                                .OriginalPath = If(manifestData.ContainsKey("OriginalPath"), CStr(manifestData("OriginalPath")), sourcePath),
                                .CreatedDate = If(manifestData.ContainsKey("BackupDate"), CDate(manifestData("BackupDate")), DateTime.MinValue),
                                .FilesCount = If(manifestData.ContainsKey("FilesCount"), CInt(manifestData("FilesCount")), 0),
                                .TotalSize = If(manifestData.ContainsKey("TotalSize"), CLng(manifestData("TotalSize")), 0L)
                            }

                            backupInfos.Add(backupInfo)
                        Catch ex As Exception
                            _logger.LogWarning($"Could not read manifest for backup: {backupPath}")
                        End Try
                    Else
                        ' Backup without manifest - create basic info
                        Dim dirInfo As New DirectoryInfo(backupPath)
                        Dim backupInfo As New BackupInfo With {
                            .BackupPath = backupPath,
                            .OriginalPath = sourcePath,
                            .CreatedDate = dirInfo.CreationTime,
                            .FilesCount = Directory.GetFiles(backupPath, "*.*", SearchOption.AllDirectories).Length,
                            .TotalSize = 0
                        }
                        backupInfos.Add(backupInfo)
                    End If
                Next

                _logger.LogInfo($"Found {backupInfos.Count} backup(s)")
                Return Result(Of List(Of BackupInfo)).Success(backupInfos)

            Catch ex As Exception
                _logger.LogError($"Failed to list backups for {sourcePath}", ex)
                Return Result(Of List(Of BackupInfo)).Fail(New FileAccessError(sourcePath, ex))
            End Try
        End Function

        Public Async Function DeleteBackupAsync(backupPath As String) As Task(Of Result(Of Unit)) _
            Implements IBackupService.DeleteBackupAsync

            Try
                _logger.LogInfo($"Deleting backup: {backupPath}")

                If Await _fileSystem.DirectoryExistsAsync(backupPath) Then
                    Await Task.Run(Sub() Directory.Delete(backupPath, True))
                    _logger.LogInfo($"Backup deleted successfully: {backupPath}")
                Else
                    _logger.LogWarning($"Backup directory not found: {backupPath}")
                End If

                Return Result(Of Unit).Success(Unit.Value)

            Catch ex As Exception
                _logger.LogError($"Failed to delete backup {backupPath}", ex)
                Return Result(Of Unit).Fail(New FileAccessError(backupPath, ex))
            End Try
        End Function
    End Class
End Namespace
