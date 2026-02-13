Imports System.IO
Imports System.Threading.Tasks
Imports PS5_BACKPORK_KITCHEN.Architecture.Composition
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Coordinators

''' <summary>
''' LEGACY WRAPPER - Now uses new architecture internally
''' This class maintains backward compatibility while delegating to BatchPatchingCoordinator
''' </summary>
Public Class BatchProcessor

    ' Legacy structure definitions (kept for backward compatibility)
    Public Structure BatchJob
        Public GameFolders As List(Of String)
        Public TargetPs5Sdk As UInteger
        Public TargetPs4Sdk As UInteger
        Public CreateBackup As Boolean
        Public VerifyIntegrity As Boolean
        Public ConcurrentProcessing As Boolean
    End Structure

    Public Structure BatchResult
        Public TotalGames As Integer
        Public SuccessfulGames As Integer
        Public FailedGames As Integer
        Public SkippedGames As Integer
        Public TotalDuration As TimeSpan
        Public Results As List(Of SingleGameResult)
    End Structure

    Public Structure SingleGameResult
        Public GameFolder As String
        Public GameName As String
        Public Success As Boolean
        Public FilesPatched As Integer
        Public FilesSkipped As Integer
        Public FilesFailed As Integer
        Public ErrorMessage As String
        Public Duration As TimeSpan
    End Structure

    Public Event GameProcessed(gameIndex As Integer, total As Integer, result As SingleGameResult)
    Public Event BatchProgress(percentage As Integer, message As String)
    Public Event BatchCompleted(result As BatchResult)

    ''' <summary>
    ''' Process multiple game folders in batch
    ''' Now uses new architecture internally
    ''' </summary>
    Public Async Function ProcessBatchAsync(job As BatchJob) As Task(Of BatchResult)
        Try
            ' Convert legacy job to new architecture job
            Dim newJob = ConvertToNewJob(job)

            ' Create coordinator via composition root
            Dim coordinator = CompositionRoot.Instance().CreateBatchPatchingCoordinator()

            ' Create progress handler
            Dim progressHandler = New Progress(Of Architecture.Application.Coordinators.BatchProgress)(
                Sub(p)
                    RaiseEvent BatchProgress(p.Percentage, p.Message)

                    ' Raise GameProcessed event when a game completes
                    If p.CurrentGame IsNot Nothing Then
                        Dim gameResultLegacy = ConvertToLegacyGameResult(p.CurrentGame)
                        RaiseEvent GameProcessed(p.ProcessedGames, p.TotalGames, gameResultLegacy)
                    End If
                End Sub)

            ' Execute batch using new architecture
            Dim result = Await coordinator.ExecuteBatchAsync(newJob, progressHandler, Threading.CancellationToken.None)

            ' Convert result back to legacy format
            Dim legacyResult As BatchResult
            If result.IsSuccess Then
                legacyResult = ConvertToLegacyResult(result.Value)
            Else
                ' Create error result
                legacyResult = New BatchResult With {
                    .TotalGames = job.GameFolders.Count,
                    .SuccessfulGames = 0,
                    .FailedGames = job.GameFolders.Count,
                    .SkippedGames = 0,
                    .TotalDuration = TimeSpan.Zero,
                    .Results = New List(Of SingleGameResult)
                }
            End If

            RaiseEvent BatchCompleted(legacyResult)
            Return legacyResult

        Catch ex As Exception
            RaiseEvent BatchProgress(100, $"Batch processing failed: {ex.Message}")

            Dim errorResult = New BatchResult With {
                .TotalGames = job.GameFolders.Count,
                .SuccessfulGames = 0,
                .FailedGames = job.GameFolders.Count,
                .SkippedGames = 0,
                .TotalDuration = TimeSpan.Zero,
                .Results = New List(Of SingleGameResult)
            }

            RaiseEvent BatchCompleted(errorResult)
            Return errorResult
        End Try
    End Function

    ''' <summary>
    ''' Convert legacy BatchJob to new architecture
    ''' </summary>
    Private Function ConvertToNewJob(legacyJob As BatchJob) As Architecture.Domain.Models.BatchJob
        Return New Architecture.Domain.Models.BatchJob With {
            .GameFolders = legacyJob.GameFolders,
            .TargetSdk = CLng(legacyJob.TargetPs5Sdk),
            .Options = New PatchOptions With {
                .AutoBackup = legacyJob.CreateBackup,
                .SkipAlreadyPatched = True,
                .ContinueOnError = True
            },
            .ConcurrentProcessing = legacyJob.ConcurrentProcessing
        }
    End Function

    ''' <summary>
    ''' Convert new architecture result to legacy format
    ''' </summary>
    Private Function ConvertToLegacyResult(newResult As Architecture.Domain.Models.BatchResult) As BatchResult
        Dim legacyResults As New List(Of SingleGameResult)

        For Each gameResult In newResult.GameResults
            legacyResults.Add(ConvertToLegacyGameResult(gameResult))
        Next

        Return New BatchResult With {
            .TotalGames = newResult.TotalGames,
            .SuccessfulGames = newResult.SuccessfulGames,
            .FailedGames = newResult.FailedGames,
            .SkippedGames = newResult.SkippedGames,
            .TotalDuration = newResult.TotalDuration,
            .Results = legacyResults
        }
    End Function

    ''' <summary>
    ''' Convert new architecture game result to legacy format
    ''' </summary>
    Private Function ConvertToLegacyGameResult(gameResult As GamePatchResult) As SingleGameResult
        Return New SingleGameResult With {
            .GameFolder = gameResult.GameFolder,
            .GameName = gameResult.GameName,
            .Success = gameResult.IsSuccess,
            .FilesPatched = gameResult.FilesPatched,
            .FilesSkipped = gameResult.FilesSkipped,
            .FilesFailed = gameResult.FilesFailed,
            .ErrorMessage = If(gameResult.ErrorMessage, String.Empty),
            .Duration = gameResult.Duration
        }
    End Function

    ''' <summary>
    ''' Generate batch report (unchanged)
    ''' </summary>
    Public Function GenerateBatchReport(result As BatchResult) As String
        Dim sb As New Text.StringBuilder()

        sb.AppendLine("=======================================================")
        sb.AppendLine($"  BATCH PROCESSING REPORT")
        sb.AppendLine("=======================================================")
        sb.AppendLine()
        sb.AppendLine($"Total Games Processed: {result.TotalGames}")
        sb.AppendLine($"Successful: {result.SuccessfulGames}")
        sb.AppendLine($"Failed: {result.FailedGames}")
        sb.AppendLine($"Skipped: {result.SkippedGames}")
        sb.AppendLine($"Total Duration: {result.TotalDuration.TotalSeconds:F2}s")

        If result.TotalGames > 0 Then
            Dim successRate = (result.SuccessfulGames * 100.0) / result.TotalGames
            sb.AppendLine($"Success Rate: {successRate:F1}%")
        End If

        sb.AppendLine()
        sb.AppendLine("=== DETAILED RESULTS ===")

        For Each gameResult In result.Results
            sb.AppendLine()
            sb.AppendLine($"Game: {gameResult.GameName}")
            sb.AppendLine($"  Status: {If(gameResult.Success, "+ SUCCESS", "x FAILED")}")
            sb.AppendLine($"  Patched: {gameResult.FilesPatched}, Skipped: {gameResult.FilesSkipped}, Failed: {gameResult.FilesFailed}")
            sb.AppendLine($"  Duration: {gameResult.Duration.TotalSeconds:F2}s")
            If Not String.IsNullOrEmpty(gameResult.ErrorMessage) Then
                sb.AppendLine($"  Error: {gameResult.ErrorMessage}")
            End If
        Next

        Return sb.ToString()
    End Function

End Class
