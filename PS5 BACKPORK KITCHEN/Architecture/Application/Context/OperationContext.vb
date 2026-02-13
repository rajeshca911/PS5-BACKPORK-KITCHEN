Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models

Namespace Architecture.Application.Context
    ''' <summary>
    ''' Context for a patching operation (replaces global variables)
    ''' </summary>
    Public Class OperationContext
        Public Property OperationId As Guid
        Public Property StartTime As DateTime
        Public Property SourceFolder As String
        Public Property TargetSdk As Long
        Public Property TotalFiles As Integer
        Public Property ProcessedFiles As Integer
        Public Property PatchedCount As Integer
        Public Property SkippedCount As Integer
        Public Property ErrorCount As Integer
        Public Property BackupPath As String
        Public Property Options As PatchOptions

        Public Shared Function Create(sourceFolder As String, targetSdk As Long, options As PatchOptions) As OperationContext
            Return New OperationContext With {
                .OperationId = Guid.NewGuid(),
                .StartTime = DateTime.Now,
                .SourceFolder = sourceFolder,
                .TargetSdk = targetSdk,
                .Options = options,
                .TotalFiles = 0,
                .ProcessedFiles = 0,
                .PatchedCount = 0,
                .SkippedCount = 0,
                .ErrorCount = 0
            }
        End Function

        Public ReadOnly Property Duration As TimeSpan
            Get
                Return DateTime.Now - StartTime
            End Get
        End Property

        Public ReadOnly Property ProgressPercentage As Double
            Get
                If TotalFiles = 0 Then Return 0
                Return (ProcessedFiles / TotalFiles) * 100
            End Get
        End Property
    End Class
End Namespace
