Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Coordinators

Namespace Architecture.Domain.Models
    ''' <summary>
    ''' Configuration for batch patching operation
    ''' </summary>
    Public Class BatchJob
        Public Property GameFolders As List(Of String)
        Public Property TargetSdk As Long
        Public Property Options As PatchOptions
        Public Property ConcurrentProcessing As Boolean

        Public Sub New()
            GameFolders = New List(Of String)
            Options = New PatchOptions()
            ConcurrentProcessing = False
        End Sub
    End Class

    ''' <summary>
    ''' Result of a batch patching operation
    ''' </summary>
    Public Class BatchResult
        Public Property OperationId As Guid
        Public Property TotalGames As Integer
        Public Property SuccessfulGames As Integer
        Public Property FailedGames As Integer
        Public Property SkippedGames As Integer
        Public Property TotalDuration As TimeSpan
        Public Property GameResults As List(Of GamePatchResult)

        Public Sub New()
            OperationId = Guid.NewGuid()
            GameResults = New List(Of GamePatchResult)
        End Sub

        Public ReadOnly Property SuccessRate As Double
            Get
                If TotalGames = 0 Then Return 0
                Return (SuccessfulGames * 100.0) / TotalGames
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Result of patching a single game in a batch
    ''' </summary>
    Public Class GamePatchResult
        Public Property GameFolder As String
        Public Property GameName As String
        Public Property IsSuccess As Boolean
        Public Property Summary As PatchSummary
        Public Property ErrorMessage As String
        Public Property Duration As TimeSpan

        Public ReadOnly Property FilesPatched As Integer
            Get
                Return If(Summary IsNot Nothing, Summary.PatchedCount, 0)
            End Get
        End Property

        Public ReadOnly Property FilesSkipped As Integer
            Get
                Return If(Summary IsNot Nothing, Summary.SkippedCount, 0)
            End Get
        End Property

        Public ReadOnly Property FilesFailed As Integer
            Get
                Return If(Summary IsNot Nothing, Summary.ErrorCount, 0)
            End Get
        End Property
    End Class
End Namespace
