Imports System.IO
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Context
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Services
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Errors
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters

Namespace Architecture.Application.Coordinators
    ''' <summary>
    ''' Coordinates complete patching workflows
    ''' </summary>
    Public Class PatchingCoordinator
        Private ReadOnly _elfService As IElfPatchingService
        Private ReadOnly _backupService As IBackupService
        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger
        'Public Shared fileStatusList As New List(Of String)

        Public Sub New(elfService As IElfPatchingService,
                      backupService As IBackupService,
                      fileSystem As IFileSystem,
                      logger As ILogger)
            _elfService = elfService
            _backupService = backupService
            _fileSystem = fileSystem
            _logger = logger
        End Sub

        Public Async Function ExecutePatchingAsync(
            sourceFolder As String,
            targetSdk As Long,
            options As PatchOptions,
            progress As IProgress(Of PatchProgress),
            cancellationToken As Threading.CancellationToken) As Task(Of Result(Of PatchSummary))

            ' Create operation context
            Dim context = OperationContext.Create(sourceFolder, targetSdk, options)

            _logger.LogInfo($"Starting patching operation {context.OperationId} for {sourceFolder}")

            Try
                ' Step 1: Validate source folder
                Dim folderExists = Await _fileSystem.DirectoryExistsAsync(sourceFolder)
                If Not folderExists Then
                    Return Result(Of PatchSummary).Fail(New DirectoryNotFoundError(sourceFolder))
                End If

                ' Step 2: Backup (if enabled)
                If options.AutoBackup Then
                    _logger.LogInfo("Creating backup...")
                    Dim backupResult = Await _backupService.CreateBackupAsync(sourceFolder)

                    If Not backupResult.IsSuccess Then
                        _logger.LogError($"Backup failed: {backupResult.Error.Message}")
                        If Not options.ContinueOnError Then
                            Return Result(Of PatchSummary).Fail(backupResult.Error)
                        End If
                    Else
                        context.BackupPath = backupResult.Value
                        _logger.LogInfo($"Backup created: {context.BackupPath}")
                    End If
                End If

                cancellationToken.ThrowIfCancellationRequested()

                ' Step 3: Find files to patch
                _logger.LogInfo("Scanning for ELF files...")
                Dim binFiles = Await _fileSystem.GetFilesAsync(sourceFolder, "*.bin", IO.SearchOption.AllDirectories)
                Dim prxFiles = Await _fileSystem.GetFilesAsync(sourceFolder, "*.prx", IO.SearchOption.AllDirectories)
                Dim sprxFiles = Await _fileSystem.GetFilesAsync(sourceFolder, "*.sprx", IO.SearchOption.AllDirectories)

                Dim allFiles = binFiles.Concat(prxFiles).Concat(sprxFiles).ToList()
                context.TotalFiles = allFiles.Count

                _logger.LogInfo($"Found {context.TotalFiles} files to process")

                If context.TotalFiles = 0 Then
                    Return Result(Of PatchSummary).Fail(New NoFilesFoundError(sourceFolder))
                End If

                ' Step 4: Patch each file
                Dim summary = New PatchSummary With {
                    .OperationId = context.OperationId,
                    .SourceFolder = sourceFolder,
                    .TargetSdk = targetSdk,
                    .BackupPath = context.BackupPath
                }

                For Each filePath In allFiles
                    cancellationToken.ThrowIfCancellationRequested()

                    ' Report progress
                    progress?.Report(New PatchProgress With {
                        .CurrentFile = IO.Path.GetFileName(filePath),
                        .CurrentFilePath = filePath,
                        .ProcessedFiles = context.ProcessedFiles,
                        .TotalFiles = context.TotalFiles,
                        .Percentage = context.ProgressPercentage
                    })

                    ' Patch file

                    Dim patchResult = Await _elfService.PatchFileAsync(filePath, targetSdk, cancellationToken)

                    context.ProcessedFiles += 1

                    If patchResult.IsSuccess Then
                        context.PatchedCount += 1
                        summary.PatchedFiles.Add(patchResult.Value)
                        summary.StatusLines.Add($"Patched: {IO.Path.GetFileName(filePath)}")
                        _logger.LogInfo($"✔ Patched: {filePath}")

                    ElseIf TypeOf patchResult.Error Is AlreadyPatchedError Then
                        context.SkippedCount += 1
                        summary.SkippedFiles.Add(filePath)
                        summary.StatusLines.Add($"⏭ {Path.GetFileName(filePath)} — already at target SDK")
                        _logger.LogInfo($"Skipped (already patched): {filePath}")

                    Else
                        context.ErrorCount += 1
                        summary.FailedFiles.Add((filePath, patchResult.Error))
                        summary.StatusLines.Add(
        $"❌ {Path.GetFileName(filePath)} — {patchResult.Error.Message}")
                        _logger.LogError($"Failed: {filePath} - {patchResult.Error.Message}")

                        If Not options.ContinueOnError Then
                            Return Result(Of PatchSummary).Fail(patchResult.Error)
                        End If
                    End If
                Next

                ' Final progress
                progress?.Report(New PatchProgress With {
                    .CurrentFile = "Completed",
                    .ProcessedFiles = context.TotalFiles,
                    .TotalFiles = context.TotalFiles,
                    .Percentage = 100
                })

                ' Step 5: Finalize summary
                summary.TotalFiles = context.TotalFiles
                summary.PatchedCount = context.PatchedCount
                summary.SkippedCount = context.SkippedCount
                summary.ErrorCount = context.ErrorCount
                summary.Duration = context.Duration

                _logger.LogInfo($"Patching completed: {context.PatchedCount} patched, {context.SkippedCount} skipped, {context.ErrorCount} errors in {context.Duration.TotalSeconds}s")

                Return Result(Of PatchSummary).Success(summary)

            Catch ex As OperationCanceledException
                _logger.LogWarning($"Operation {context.OperationId} cancelled by user")
                Throw

            Catch ex As Exception
                _logger.LogError("Unexpected error in patching operation", ex)
                Return Result(Of PatchSummary).Fail(New UnexpectedError(ex))
            End Try
        End Function
    End Class

    ''' <summary>
    ''' Progress reporting for patching operation
    ''' </summary>
    Public Class PatchProgress
        Public Property CurrentFile As String
        Public Property CurrentFilePath As String
        Public Property ProcessedFiles As Integer
        Public Property TotalFiles As Integer
        Public Property Percentage As Double
    End Class

    ''' <summary>
    ''' Summary of a completed patching operation
    ''' </summary>
    Public Class PatchSummary
        Public Property OperationId As Guid
        Public Property SourceFolder As String
        Public Property TargetSdk As Long
        Public Property BackupPath As String
        Public Property TotalFiles As Integer
        Public Property PatchedCount As Integer
        Public Property SkippedCount As Integer
        Public Property ErrorCount As Integer
        Public Property Duration As TimeSpan
        Public Property PatchedFiles As New List(Of PatchResult)
        Public Property SkippedFiles As New List(Of String)
        Public Property FailedFiles As New List(Of (String, DomainError))
        Public Property StatusLines As New List(Of String)

    End Class
End Namespace
