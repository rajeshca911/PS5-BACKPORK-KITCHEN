Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters
Imports System.Threading.Tasks

Namespace Architecture.Application.Coordinators
    ''' <summary>
    ''' Coordinates batch patching operations across multiple games
    ''' </summary>
    Public Class BatchPatchingCoordinator
        Private ReadOnly _patchingCoordinator As PatchingCoordinator
        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger

        Public Sub New(patchingCoordinator As PatchingCoordinator,
                      fileSystem As IFileSystem,
                      logger As ILogger)
            _patchingCoordinator = patchingCoordinator
            _fileSystem = fileSystem
            _logger = logger
        End Sub

        ''' <summary>
        ''' Execute batch patching operation
        ''' </summary>
        Public Async Function ExecuteBatchAsync(
            job As BatchJob,
            progress As IProgress(Of BatchProgress),
            cancellationToken As Threading.CancellationToken) As Task(Of Result(Of BatchResult))

            Dim batchResult = New BatchResult With {
                .TotalGames = job.GameFolders.Count
            }

            Dim startTime = DateTime.Now

            _logger.LogInfo($"Starting batch operation {batchResult.OperationId} with {job.GameFolders.Count} game(s)")

            Try
                progress?.Report(New BatchProgress With {
                    .Message = $"Starting batch processing of {job.GameFolders.Count} game(s)...",
                    .Percentage = 0,
                    .ProcessedGames = 0,
                    .TotalGames = job.GameFolders.Count
                })

                If job.ConcurrentProcessing Then
                    Await ProcessConcurrentlyAsync(job, batchResult, progress, cancellationToken)
                Else
                    Await ProcessSequentiallyAsync(job, batchResult, progress, cancellationToken)
                End If

                batchResult.TotalDuration = DateTime.Now - startTime

                _logger.LogInfo($"Batch operation completed: {batchResult.SuccessfulGames} successful, " &
                              $"{batchResult.FailedGames} failed, {batchResult.SkippedGames} skipped in {batchResult.TotalDuration.TotalSeconds:F2}s")

                Return Result(Of BatchResult).Success(batchResult)

            Catch ex As OperationCanceledException
                _logger.LogWarning($"Batch operation {batchResult.OperationId} cancelled")
                Throw

            Catch ex As Exception
                _logger.LogError("Batch operation failed", ex)
                Return Result(Of BatchResult).Fail(New Domain.Errors.UnexpectedError(ex))
            End Try
        End Function

        ''' <summary>
        ''' Process games sequentially (safer, easier to debug)
        ''' </summary>
        Private Async Function ProcessSequentiallyAsync(
            job As BatchJob,
            batchResult As BatchResult,
            progress As IProgress(Of BatchProgress),
            cancellationToken As Threading.CancellationToken) As Task

            For i As Integer = 0 To job.GameFolders.Count - 1
                cancellationToken.ThrowIfCancellationRequested()

                Dim gameFolder = job.GameFolders(i)
                Dim gameResult = Await ProcessSingleGameAsync(gameFolder, job, i + 1, job.GameFolders.Count, cancellationToken)

                batchResult.GameResults.Add(gameResult)
                UpdateBatchCounts(batchResult, gameResult)

                Dim percentage = CInt(((i + 1) * 100.0) / job.GameFolders.Count)
                progress?.Report(New BatchProgress With {
                    .Message = $"Processed {i + 1}/{job.GameFolders.Count} games",
                    .Percentage = percentage,
                    .ProcessedGames = i + 1,
                    .TotalGames = job.GameFolders.Count,
                    .CurrentGame = gameResult
                })
            Next
        End Function

        ''' <summary>
        ''' Process games in parallel (faster but more resource intensive)
        ''' </summary>
        Private Async Function ProcessConcurrentlyAsync(
            job As BatchJob,
            batchResult As BatchResult,
            progress As IProgress(Of BatchProgress),
            cancellationToken As Threading.CancellationToken) As Task

            Dim maxConcurrency = Math.Min(Environment.ProcessorCount, 4)
            Dim semaphore As New Threading.SemaphoreSlim(maxConcurrency)
            Dim tasks As New List(Of Task(Of GamePatchResult))

            _logger.LogInfo($"Processing batch concurrently with max concurrency: {maxConcurrency}")

            For i As Integer = 0 To job.GameFolders.Count - 1
                Dim index = i
                Dim gameFolder = job.GameFolders(i)

                Dim task As Task(Of GamePatchResult) = Task.Run(Async Function()
                                        Await semaphore.WaitAsync(cancellationToken)
                                        Try
                                            Return Await ProcessSingleGameAsync(gameFolder, job, index + 1, job.GameFolders.Count, cancellationToken)
                                        Finally
                                            semaphore.Release()
                                        End Try
                                    End Function)
                tasks.Add(task)
            Next

            Dim results = Await Task.WhenAll(tasks)

            For Each gameResult In results
                batchResult.GameResults.Add(gameResult)
                UpdateBatchCounts(batchResult, gameResult)
            Next

            progress?.Report(New BatchProgress With {
                .Message = $"Completed processing {batchResult.TotalGames} games",
                .Percentage = 100,
                .ProcessedGames = batchResult.TotalGames,
                .TotalGames = batchResult.TotalGames
            })
        End Function

        ''' <summary>
        ''' Process a single game folder
        ''' </summary>
        Private Async Function ProcessSingleGameAsync(
            gameFolder As String,
            job As BatchJob,
            currentIndex As Integer,
            totalGames As Integer,
            cancellationToken As Threading.CancellationToken) As Task(Of GamePatchResult)

            Dim gameResult = New GamePatchResult With {
                .GameFolder = gameFolder,
                .GameName = IO.Path.GetFileName(gameFolder),
                .IsSuccess = False
            }

            Dim startTime = DateTime.Now

            Try
                _logger.LogInfo($"[{currentIndex}/{totalGames}] Processing game: {gameResult.GameName}")

                ' Validate folder exists
                Dim folderExists = Await _fileSystem.DirectoryExistsAsync(gameFolder)
                If Not folderExists Then
                    gameResult.ErrorMessage = $"Directory not found: {gameFolder}"
                    gameResult.Duration = DateTime.Now - startTime
                    _logger.LogError($"[{currentIndex}/{totalGames}] {gameResult.ErrorMessage}")
                    Return gameResult
                End If

                ' Execute patching using PatchingCoordinator
                Dim patchProgress = New Progress(Of PatchProgress)(
                    Sub(p)
                        _logger.LogDebug($"[{gameResult.GameName}] {p.CurrentFile} ({p.Percentage:F0}%)")
                    End Sub)

                Dim patchResult = Await _patchingCoordinator.ExecutePatchingAsync(
                    gameFolder,
                    job.TargetSdk,
                    job.Options,
                    patchProgress,
                    cancellationToken)

                If patchResult.IsSuccess Then
                    gameResult.IsSuccess = True
                    gameResult.Summary = patchResult.Value
                    _logger.LogInfo($"[{currentIndex}/{totalGames}] Success: {gameResult.GameName} - " &
                                  $"{gameResult.FilesPatched} patched, {gameResult.FilesSkipped} skipped")
                Else
                    gameResult.IsSuccess = False
                    gameResult.ErrorMessage = patchResult.Error.Message
                    _logger.LogError($"[{currentIndex}/{totalGames}] Failed: {gameResult.GameName} - {gameResult.ErrorMessage}")
                End If

                gameResult.Duration = DateTime.Now - startTime

            Catch ex As OperationCanceledException
                gameResult.ErrorMessage = "Operation cancelled"
                gameResult.Duration = DateTime.Now - startTime
                _logger.LogWarning($"[{currentIndex}/{totalGames}] Cancelled: {gameResult.GameName}")
                Throw

            Catch ex As Exception
                gameResult.IsSuccess = False
                gameResult.ErrorMessage = ex.Message
                gameResult.Duration = DateTime.Now - startTime
                _logger.LogError($"[{currentIndex}/{totalGames}] Error: {gameResult.GameName}", ex)
            End Try

            Return gameResult
        End Function

        ''' <summary>
        ''' Update batch result counts based on game result
        ''' </summary>
        Private Sub UpdateBatchCounts(batchResult As BatchResult, gameResult As GamePatchResult)
            If gameResult.IsSuccess Then
                batchResult.SuccessfulGames += 1
            ElseIf String.IsNullOrEmpty(gameResult.ErrorMessage) OrElse
                   gameResult.ErrorMessage.Contains("already patched") Then
                batchResult.SkippedGames += 1
            Else
                batchResult.FailedGames += 1
            End If
        End Sub
    End Class

    ''' <summary>
    ''' Progress reporting for batch operations
    ''' </summary>
    Public Class BatchProgress
        Public Property Message As String
        Public Property Percentage As Integer
        Public Property ProcessedGames As Integer
        Public Property TotalGames As Integer
        Public Property CurrentGame As GamePatchResult
    End Class
End Namespace
