Imports System.IO

Public Class ProgressTracker

    Public Event ProgressChanged(percentage As Integer, currentFile As String, message As String)

    Public Event StageChanged(stageName As String, stageNumber As Integer, totalStages As Integer)

    Private _totalFiles As Integer
    Private _processedFiles As Integer
    Private _currentStage As Integer
    Private _totalStages As Integer

    Public ReadOnly Property TotalFiles As Integer
        Get
            Return _totalFiles
        End Get
    End Property

    Public ReadOnly Property ProcessedFiles As Integer
        Get
            Return _processedFiles
        End Get
    End Property

    Public ReadOnly Property CurrentPercentage As Integer
        Get
            If _totalFiles = 0 Then Return 0
            Return CInt((_processedFiles * 100.0) / _totalFiles)
        End Get
    End Property

    Public Sub New(totalStages As Integer)
        _totalStages = totalStages
        _currentStage = 0
        _totalFiles = 0
        _processedFiles = 0
    End Sub

    ''' <summary>
    ''' Initialize tracking for a specific stage
    ''' </summary>
    Public Sub InitializeStage(stageName As String, totalFiles As Integer)
        _currentStage += 1
        _totalFiles = totalFiles
        _processedFiles = 0
        RaiseEvent StageChanged(stageName, _currentStage, _totalStages)
    End Sub

    ''' <summary>
    ''' Update progress for current file
    ''' </summary>
    Public Sub UpdateProgress(currentFile As String, Optional message As String = "")
        _processedFiles += 1
        Dim percentage = CurrentPercentage
        RaiseEvent ProgressChanged(percentage, currentFile, message)
    End Sub

    ''' <summary>
    ''' Report progress with custom percentage
    ''' </summary>
    Public Sub ReportProgress(percentage As Integer, message As String)
        RaiseEvent ProgressChanged(percentage, "", message)
    End Sub

    ''' <summary>
    ''' Complete current stage
    ''' </summary>
    Public Sub CompleteStage()
        _processedFiles = _totalFiles
        RaiseEvent ProgressChanged(100, "", "Stage completed")
    End Sub

    ''' <summary>
    ''' Reset tracker
    ''' </summary>
    Public Sub Reset()
        _currentStage = 0
        _totalFiles = 0
        _processedFiles = 0
    End Sub

End Class

''' <summary>
''' Static helper for quick progress updates
''' </summary>
Public Module ProgressHelper

    Public Structure ProgressInfo
        Public CurrentStep As Integer
        Public TotalSteps As Integer
        Public Message As String
        Public Percentage As Integer
    End Structure

    ''' <summary>
    ''' Calculate progress percentage
    ''' </summary>
    Public Function CalculateProgress(current As Integer, total As Integer) As Integer
        If total = 0 Then Return 0
        Return CInt((current * 100.0) / total)
    End Function

    ''' <summary>
    ''' Generate progress message
    ''' </summary>
    Public Function FormatProgressMessage(current As Integer, total As Integer, itemName As String) As String
        Return $"Processing {current}/{total}: {itemName}"
    End Function

    ''' <summary>
    ''' Get estimated time remaining
    ''' </summary>
    Public Function EstimateTimeRemaining(startTime As DateTime, current As Integer, total As Integer) As TimeSpan
        If current = 0 OrElse total = 0 Then Return TimeSpan.Zero

        Dim elapsed = DateTime.Now - startTime
        Dim avgTimePerItem = elapsed.TotalSeconds / current
        Dim remaining = total - current

        Return TimeSpan.FromSeconds(avgTimePerItem * remaining)
    End Function

End Module