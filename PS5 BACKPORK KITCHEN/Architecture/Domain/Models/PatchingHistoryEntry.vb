Namespace Architecture.Domain.Models
    ''' <summary>
    ''' Represents a single patching operation in history
    ''' </summary>
    Public Class PatchingHistoryEntry
        Public Property Id As Guid
        Public Property Timestamp As DateTime
        Public Property OperationType As OperationType
        Public Property SourcePath As String
        Public Property GameName As String
        Public Property TargetSdk As Long
        Public Property TotalFiles As Integer
        Public Property PatchedFiles As Integer
        Public Property SkippedFiles As Integer
        Public Property FailedFiles As Integer
        Public Property Duration As TimeSpan
        Public Property Success As Boolean
        Public Property BackupPath As String
        Public Property ErrorMessage As String
        Public Property MachineName As String
        Public Property UserName As String
        Public Property AppVersion As String

        Public Sub New()
            Id = Guid.NewGuid()
            Timestamp = DateTime.Now
            MachineName = Environment.MachineName
            UserName = Environment.UserName
            AppVersion = APP_VERSION
        End Sub

        ''' <summary>
        ''' Create from PatchSummary
        ''' </summary>
        Public Shared Function FromPatchSummary(summary As Application.Coordinators.PatchSummary) As PatchingHistoryEntry
            Return New PatchingHistoryEntry With {
                .Id = summary.OperationId,
                .OperationType = OperationType.SinglePatch,
                .SourcePath = summary.SourceFolder,
                .GameName = IO.Path.GetFileName(summary.SourceFolder),
                .TargetSdk = summary.TargetSdk,
                .TotalFiles = summary.TotalFiles,
                .PatchedFiles = summary.PatchedCount,
                .SkippedFiles = summary.SkippedCount,
                .FailedFiles = summary.ErrorCount,
                .Duration = summary.Duration,
                .Success = summary.ErrorCount = 0,
                .BackupPath = summary.BackupPath,
                .ErrorMessage = If(summary.ErrorCount > 0,
                    $"{summary.ErrorCount} files failed to patch", String.Empty)
            }
        End Function

        ''' <summary>
        ''' Create from BatchResult
        ''' </summary>
        Public Shared Function FromBatchResult(result As BatchResult, targetSdk As Long) As PatchingHistoryEntry
            Dim totalPatched = result.GameResults.Sum(Function(g) If(g.Summary IsNot Nothing, g.Summary.PatchedCount, 0))
            Dim totalSkipped = result.GameResults.Sum(Function(g) If(g.Summary IsNot Nothing, g.Summary.SkippedCount, 0))
            Dim totalFailed = result.GameResults.Sum(Function(g) If(g.Summary IsNot Nothing, g.Summary.ErrorCount, 0))
            Dim totalFiles = result.GameResults.Sum(Function(g) If(g.Summary IsNot Nothing, g.Summary.TotalFiles, 0))

            Return New PatchingHistoryEntry With {
                .Id = result.OperationId,
                .OperationType = OperationType.BatchPatch,
                .SourcePath = $"Batch of {result.TotalGames} games",
                .GameName = $"Batch Operation ({result.TotalGames} games)",
                .TargetSdk = targetSdk,
                .TotalFiles = totalFiles,
                .PatchedFiles = totalPatched,
                .SkippedFiles = totalSkipped,
                .FailedFiles = totalFailed,
                .Duration = result.TotalDuration,
                .Success = result.FailedGames = 0,
                .ErrorMessage = If(result.FailedGames > 0,
                    $"{result.FailedGames} games failed", String.Empty)
            }
        End Function

        Public ReadOnly Property SuccessRate As Double
            Get
                If TotalFiles = 0 Then Return 0
                Return (PatchedFiles * 100.0) / TotalFiles
            End Get
        End Property

        Public ReadOnly Property StatusText As String
            Get
                If Success Then Return "Success"
                If FailedFiles > 0 Then Return "Partial Failure"
                Return "Failed"
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Type of patching operation
    ''' </summary>
    Public Enum OperationType
        SinglePatch = 0
        BatchPatch = 1
        Restore = 2
    End Enum

    ''' <summary>
    ''' Statistics aggregated from history
    ''' </summary>
    Public Class PatchingStatistics
        Public Property TotalOperations As Integer
        Public Property SuccessfulOperations As Integer
        Public Property FailedOperations As Integer
        Public Property TotalFilesPatched As Integer
        Public Property TotalFilesSkipped As Integer
        Public Property TotalFilesFailed As Integer
        Public Property AverageDuration As TimeSpan
        Public Property TotalDuration As TimeSpan
        Public Property MostUsedSdk As Long
        Public Property LastOperationDate As DateTime
        Public Property FirstOperationDate As DateTime

        Public ReadOnly Property SuccessRate As Double
            Get
                If TotalOperations = 0 Then Return 0
                Return (SuccessfulOperations * 100.0) / TotalOperations
            End Get
        End Property

        Public ReadOnly Property AverageFilesPerOperation As Double
            Get
                If TotalOperations = 0 Then Return 0
                Return TotalFilesPatched / TotalOperations
            End Get
        End Property
    End Class
End Namespace
