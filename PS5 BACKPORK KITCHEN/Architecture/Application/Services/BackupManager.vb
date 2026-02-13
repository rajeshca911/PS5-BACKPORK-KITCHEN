Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters
Imports System.IO

Namespace Architecture.Application.Services
    ''' <summary>
    ''' Advanced backup management with rotation, monitoring, and cleanup
    ''' Feature #4: Advanced Backup System
    ''' </summary>
    Public Class BackupManager
        Private ReadOnly _backupService As IBackupService
        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger
        Private ReadOnly _maxBackupsToKeep As Integer
        Private ReadOnly _minDiskSpaceGb As Double

        Public Sub New(backupService As IBackupService,
                      fileSystem As IFileSystem,
                      logger As ILogger,
                      Optional maxBackupsToKeep As Integer = 5,
                      Optional minDiskSpaceGb As Double = 10.0)
            _backupService = backupService
            _fileSystem = fileSystem
            _logger = logger
            _maxBackupsToKeep = maxBackupsToKeep
            _minDiskSpaceGb = minDiskSpaceGb
        End Sub

        ''' <summary>
        ''' Create backup with automatic rotation and space monitoring
        ''' </summary>
        Public Async Function CreateManagedBackupAsync(sourcePath As String) As Task(Of Result(Of String))
            Try
                ' Check disk space before creating backup
                Dim spaceCheck = CheckDiskSpace(sourcePath)
                If Not spaceCheck.HasEnoughSpace Then
                    _logger.LogWarning($"Low disk space: {spaceCheck.AvailableSpaceGb:F2}GB available, {_minDiskSpaceGb}GB recommended")

                    ' Try cleanup to free space
                    Await CleanupOldBackupsAsync(sourcePath, keepCount:=2)

                    ' Re-check
                    spaceCheck = CheckDiskSpace(sourcePath)
                    If Not spaceCheck.HasEnoughSpace Then
                        Return Result(Of String).Fail(New Domain.Errors.UnexpectedError(
                            New Exception($"Insufficient disk space: {spaceCheck.AvailableSpaceGb:F2}GB available, {_minDiskSpaceGb}GB required")))
                    End If
                End If

                ' Create backup
                Dim backupResult = Await _backupService.CreateBackupAsync(sourcePath)

                If backupResult.IsSuccess Then
                    ' Apply rotation policy
                    Await ApplyRotationPolicyAsync(sourcePath)
                End If

                Return backupResult

            Catch ex As Exception
                _logger.LogError("Failed to create managed backup", ex)
                Return Result(Of String).Fail(New Domain.Errors.UnexpectedError(ex))
            End Try
        End Function

        ''' <summary>
        ''' Apply backup rotation policy (keep last N backups)
        ''' </summary>
        Public Async Function ApplyRotationPolicyAsync(sourcePath As String) As Task(Of Integer)
            Try
                Dim backupsResult = Await _backupService.ListBackupsAsync(sourcePath)

                If Not backupsResult.IsSuccess Then
                    Return 0
                End If

                Dim backups = backupsResult.Value.OrderByDescending(Function(b) b.CreatedDate).ToList()

                If backups.Count <= _maxBackupsToKeep Then
                    _logger.LogDebug($"Backup count ({backups.Count}) within limit ({_maxBackupsToKeep})")
                    Return 0
                End If

                ' Delete old backups
                Dim backupsToDelete = backups.Skip(_maxBackupsToKeep).ToList()
                Dim deletedCount = 0

                For Each backup In backupsToDelete
                    Dim deleteResult = Await _backupService.DeleteBackupAsync(backup.BackupPath)
                    If deleteResult.IsSuccess Then
                        deletedCount += 1
                        _logger.LogInfo($"Deleted old backup: {backup.BackupPath} (created {backup.CreatedDate})")
                    End If
                Next

                _logger.LogInfo($"Rotation policy applied: deleted {deletedCount} old backup(s), kept {_maxBackupsToKeep}")
                Return deletedCount

            Catch ex As Exception
                _logger.LogError("Failed to apply rotation policy", ex)
                Return 0
            End Try
        End Function

        ''' <summary>
        ''' Cleanup old backups (older than N days or beyond count limit)
        ''' </summary>
        Public Async Function CleanupOldBackupsAsync(sourcePath As String,
                                                     Optional olderThanDays As Integer? = Nothing,
                                                     Optional keepCount As Integer? = Nothing) As Task(Of Integer)
            Try
                Dim backupsResult = Await _backupService.ListBackupsAsync(sourcePath)

                If Not backupsResult.IsSuccess Then
                    Return 0
                End If

                Dim backups = backupsResult.Value.OrderByDescending(Function(b) b.CreatedDate).ToList()
                Dim backupsToDelete As New List(Of BackupInfo)

                ' Filter by age
                If olderThanDays.HasValue Then
                    Dim cutoffDate = DateTime.Now.AddDays(-olderThanDays.Value)
                    backupsToDelete.AddRange(backups.Where(Function(b) b.CreatedDate < cutoffDate))
                End If

                ' Filter by count (keep only N most recent)
                If keepCount.HasValue Then
                    Dim excessBackups = backups.Skip(keepCount.Value)
                    For Each backup In excessBackups
                        If Not backupsToDelete.Contains(backup) Then
                            backupsToDelete.Add(backup)
                        End If
                    Next
                End If

                ' Delete selected backups
                Dim deletedCount = 0
                For Each backup In backupsToDelete
                    Dim deleteResult = Await _backupService.DeleteBackupAsync(backup.BackupPath)
                    If deleteResult.IsSuccess Then
                        deletedCount += 1
                        _logger.LogInfo($"Cleaned up backup: {backup.BackupPath}")
                    End If
                Next

                _logger.LogInfo($"Cleanup completed: removed {deletedCount} backup(s)")
                Return deletedCount

            Catch ex As Exception
                _logger.LogError("Failed to cleanup old backups", ex)
                Return 0
            End Try
        End Function

        ''' <summary>
        ''' Compare two backups and return differences
        ''' </summary>
        Public Async Function CompareBackupsAsync(backup1Path As String, backup2Path As String) As Task(Of BackupComparison)
            Dim comparison As New BackupComparison With {
                .Backup1Path = backup1Path,
                .Backup2Path = backup2Path
            }

            Try
                ' Read manifests
                Dim manifest1 = Await ReadManifestAsync(backup1Path)
                Dim manifest2 = Await ReadManifestAsync(backup2Path)

                If manifest1 Is Nothing OrElse manifest2 Is Nothing Then
                    comparison.ComparisonFailed = True
                    comparison.ErrorMessage = "One or both backups missing manifest"
                    Return comparison
                End If

                ' Compare checksums
                Dim allFiles = manifest1.Keys.Union(manifest2.Keys).ToList()

                For Each filePath In allFiles
                    Dim in1 = manifest1.ContainsKey(filePath)
                    Dim in2 = manifest2.ContainsKey(filePath)

                    If in1 And in2 Then
                        ' File exists in both
                        If manifest1(filePath) <> manifest2(filePath) Then
                            comparison.ModifiedFiles.Add(filePath)
                        Else
                            comparison.IdenticalFiles.Add(filePath)
                        End If
                    ElseIf in1 And Not in2 Then
                        comparison.OnlyInBackup1.Add(filePath)
                    Else
                        comparison.OnlyInBackup2.Add(filePath)
                    End If
                Next

                comparison.ComparisonFailed = False
                _logger.LogInfo($"Backup comparison completed: {comparison.TotalDifferences} difference(s) found")

            Catch ex As Exception
                comparison.ComparisonFailed = True
                comparison.ErrorMessage = ex.Message
                _logger.LogError("Backup comparison failed", ex)
            End Try

            Return comparison
        End Function

        ''' <summary>
        ''' Check available disk space
        ''' </summary>
        Public Function CheckDiskSpace(path As String) As DiskSpaceInfo
            Try
                Dim drive = New DriveInfo(IO.Path.GetPathRoot(path))

                Dim info As New DiskSpaceInfo With {
                    .DriveName = drive.Name,
                    .TotalSpaceBytes = drive.TotalSize,
                    .AvailableSpaceBytes = drive.AvailableFreeSpace,
                    .UsedSpaceBytes = drive.TotalSize - drive.AvailableFreeSpace
                }

                _logger.LogDebug($"Disk space: {info.AvailableSpaceGb:F2}GB available of {info.TotalSpaceGb:F2}GB")
                Return info

            Catch ex As Exception
                _logger.LogError("Failed to check disk space", ex)
                Return New DiskSpaceInfo With {.CheckFailed = True, .ErrorMessage = ex.Message}
            End Try
        End Function

        ''' <summary>
        ''' Get backup statistics for a source path
        ''' </summary>
        Public Async Function GetBackupStatisticsAsync(sourcePath As String) As Task(Of BackupStatistics)
            Dim stats As New BackupStatistics With {
                .SourcePath = sourcePath
            }

            Try
                Dim backupsResult = Await _backupService.ListBackupsAsync(sourcePath)

                If backupsResult.IsSuccess Then
                    stats.TotalBackups = backupsResult.Value.Count
                    stats.TotalSizeBytes = backupsResult.Value.Sum(Function(b) b.TotalSize)
                    stats.OldestBackupDate = If(backupsResult.Value.Any(), backupsResult.Value.Min(Function(b) b.CreatedDate), Nothing)
                    stats.NewestBackupDate = If(backupsResult.Value.Any(), backupsResult.Value.Max(Function(b) b.CreatedDate), Nothing)
                    stats.AverageSizeBytes = If(stats.TotalBackups > 0, CLng(stats.TotalSizeBytes / stats.TotalBackups), 0)
                End If

            Catch ex As Exception
                _logger.LogError("Failed to get backup statistics", ex)
            End Try

            Return stats
        End Function

        ''' <summary>
        ''' Read backup manifest
        ''' </summary>
        Private Async Function ReadManifestAsync(backupPath As String) As Task(Of Dictionary(Of String, String))
            Try
                Dim manifestPath = Path.Combine(backupPath, "backup_manifest.json")

                If Not File.Exists(manifestPath) Then
                    Return Nothing
                End If

                Dim json = Await Task.Run(Function() File.ReadAllText(manifestPath))
                Dim manifestData = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(json)

                If manifestData.ContainsKey("Checksums") Then
                    Dim checksums = CType(manifestData("Checksums"), Newtonsoft.Json.Linq.JObject)
                    Return checksums.ToObject(Of Dictionary(Of String, String))()
                End If

                Return New Dictionary(Of String, String)()

            Catch ex As Exception
                _logger.LogError($"Failed to read manifest: {backupPath}", ex)
                Return Nothing
            End Try
        End Function
    End Class

    ''' <summary>
    ''' Disk space information
    ''' </summary>
    Public Class DiskSpaceInfo
        Public Property DriveName As String
        Public Property TotalSpaceBytes As Long
        Public Property AvailableSpaceBytes As Long
        Public Property UsedSpaceBytes As Long
        Public Property CheckFailed As Boolean
        Public Property ErrorMessage As String

        Public ReadOnly Property TotalSpaceGb As Double
            Get
                Return TotalSpaceBytes / 1024.0 / 1024.0 / 1024.0
            End Get
        End Property

        Public ReadOnly Property AvailableSpaceGb As Double
            Get
                Return AvailableSpaceBytes / 1024.0 / 1024.0 / 1024.0
            End Get
        End Property

        Public ReadOnly Property UsedSpaceGb As Double
            Get
                Return UsedSpaceBytes / 1024.0 / 1024.0 / 1024.0
            End Get
        End Property

        Public ReadOnly Property UsagePercentage As Double
            Get
                If TotalSpaceBytes = 0 Then Return 0
                Return (UsedSpaceBytes * 100.0) / TotalSpaceBytes
            End Get
        End Property

        Public ReadOnly Property HasEnoughSpace As Boolean
            Get
                Return AvailableSpaceGb >= 10.0 ' 10GB minimum
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Backup comparison result
    ''' </summary>
    Public Class BackupComparison
        Public Property Backup1Path As String
        Public Property Backup2Path As String
        Public Property IdenticalFiles As New List(Of String)
        Public Property ModifiedFiles As New List(Of String)
        Public Property OnlyInBackup1 As New List(Of String)
        Public Property OnlyInBackup2 As New List(Of String)
        Public Property ComparisonFailed As Boolean
        Public Property ErrorMessage As String

        Public ReadOnly Property TotalDifferences As Integer
            Get
                Return ModifiedFiles.Count + OnlyInBackup1.Count + OnlyInBackup2.Count
            End Get
        End Property

        Public ReadOnly Property AreIdentical As Boolean
            Get
                Return TotalDifferences = 0 AndAlso Not ComparisonFailed
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Backup statistics
    ''' </summary>
    Public Class BackupStatistics
        Public Property SourcePath As String
        Public Property TotalBackups As Integer
        Public Property TotalSizeBytes As Long
        Public Property AverageSizeBytes As Long
        Public Property OldestBackupDate As DateTime?
        Public Property NewestBackupDate As DateTime?

        Public ReadOnly Property TotalSizeMb As Double
            Get
                Return TotalSizeBytes / 1024.0 / 1024.0
            End Get
        End Property

        Public ReadOnly Property TotalSizeGb As Double
            Get
                Return TotalSizeBytes / 1024.0 / 1024.0 / 1024.0
            End Get
        End Property

        Public ReadOnly Property AverageSizeMb As Double
            Get
                Return AverageSizeBytes / 1024.0 / 1024.0
            End Get
        End Property
    End Class
End Namespace
