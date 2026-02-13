Imports System.IO
Imports Newtonsoft.Json

Public Module StatisticsManager

    Public Structure AppStatistics
        Public TotalOperations As Integer
        Public SuccessfulOperations As Integer
        Public FailedOperations As Integer
        Public TotalGamesPatched As Integer
        Public TotalFilesPatched As Integer
        Public TotalFilesSkipped As Integer
        Public TotalFilesFailed As Integer
        Public TotalBackupsCreated As Integer
        Public TotalReportsExported As Integer
        Public FirstUsedDate As DateTime
        Public LastUsedDate As DateTime
        Public TotalTimeSpent As TimeSpan
        Public MostUsedPreset As String
        Public MostPatchedGame As String
        Public AverageFilesPerOperation As Double
        Public SuccessRate As Double
        Public FavoriteSdkVersion As UInteger
        Public SessionHistory As List(Of SessionRecord)
    End Structure

    Public Structure SessionRecord
        Public SessionDate As DateTime
        Public OperationType As String
        Public GamesProcessed As Integer
        Public FilesPatched As Integer
        Public FilesSkipped As Integer
        Public FilesFailed As Integer
        Public Duration As TimeSpan
        Public Success As Boolean
        Public PresetUsed As String
        Public TargetSdk As String
    End Structure

    Private ReadOnly StatsPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statistics.json")
    Private ReadOnly MaxSessionHistory As Integer = 100

    ''' <summary>
    ''' Load statistics from file
    ''' </summary>
    Public Function LoadStatistics() As AppStatistics
        Try
            If File.Exists(StatsPath) Then
                Dim json = File.ReadAllText(StatsPath)
                Return JsonConvert.DeserializeObject(Of AppStatistics)(json)
            End If
        Catch ex As Exception
            ' Return new stats on error
        End Try

        Return New AppStatistics With {
            .SessionHistory = New List(Of SessionRecord),
            .FirstUsedDate = DateTime.Now
        }
    End Function

    ''' <summary>
    ''' Save statistics to file
    ''' </summary>
    Private Sub SaveStatistics(stats As AppStatistics)
        Try
            Dim json = JsonConvert.SerializeObject(stats, Formatting.Indented)
            File.WriteAllText(StatsPath, json)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Record new operation
    ''' </summary>
    Public Sub RecordOperation(operationType As String, gamesProcessed As Integer, filesPatched As Integer,
                               filesSkipped As Integer, filesFailed As Integer, duration As TimeSpan,
                               success As Boolean, Optional presetUsed As String = "", Optional targetSdk As String = "")
        Try
            Dim stats = LoadStatistics()

            ' Update totals
            stats.TotalOperations += 1
            If success Then
                stats.SuccessfulOperations += 1
            Else
                stats.FailedOperations += 1
            End If

            stats.TotalGamesPatched += gamesProcessed
            stats.TotalFilesPatched += filesPatched
            stats.TotalFilesSkipped += filesSkipped
            stats.TotalFilesFailed += filesFailed
            stats.LastUsedDate = DateTime.Now
            stats.TotalTimeSpent = stats.TotalTimeSpent.Add(duration)

            ' Calculate success rate
            If stats.TotalOperations > 0 Then
                stats.SuccessRate = (stats.SuccessfulOperations * 100.0) / stats.TotalOperations
            End If

            ' Calculate average files per operation
            If stats.TotalOperations > 0 Then
                stats.AverageFilesPerOperation = stats.TotalFilesPatched / stats.TotalOperations
            End If

            ' Add session record
            Dim session As New SessionRecord With {
                .SessionDate = DateTime.Now,
                .OperationType = operationType,
                .GamesProcessed = gamesProcessed,
                .FilesPatched = filesPatched,
                .FilesSkipped = filesSkipped,
                .FilesFailed = filesFailed,
                .Duration = duration,
                .Success = success,
                .PresetUsed = presetUsed,
                .TargetSdk = targetSdk
            }

            stats.SessionHistory.Insert(0, session)

            ' Keep only recent sessions
            If stats.SessionHistory.Count > MaxSessionHistory Then
                stats.SessionHistory = stats.SessionHistory.Take(MaxSessionHistory).ToList()
            End If

            SaveStatistics(stats)
        Catch ex As Exception
            ' Silent fail - statistics are not critical
        End Try
    End Sub

    ''' <summary>
    ''' Record backup creation
    ''' </summary>
    Public Sub RecordBackupCreated()
        Try
            Dim stats = LoadStatistics()
            stats.TotalBackupsCreated += 1
            SaveStatistics(stats)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Record report export
    ''' </summary>
    Public Sub RecordReportExported()
        Try
            Dim stats = LoadStatistics()
            stats.TotalReportsExported += 1
            SaveStatistics(stats)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Generate statistics dashboard report
    ''' </summary>
    Public Function GenerateDashboardReport() As String
        Dim stats = LoadStatistics()
        Dim sb As New Text.StringBuilder()

        sb.AppendLine("+===============================================================+")
        sb.AppendLine("|              PS5 BACKPORK KITCHEN - STATISTICS                |")
        sb.AppendLine("+===============================================================+")
        sb.AppendLine()

        sb.AppendLine("=== USAGE STATISTICS ===")
        sb.AppendLine($"First Used: {stats.FirstUsedDate:yyyy-MM-dd HH:mm}")
        sb.AppendLine($"Last Used: {stats.LastUsedDate:yyyy-MM-dd HH:mm}")
        sb.AppendLine($"Total Time Spent: {FormatTimeSpan(stats.TotalTimeSpent)}")
        sb.AppendLine()

        sb.AppendLine("=== OPERATIONS ===")
        sb.AppendLine($"Total Operations: {stats.TotalOperations}")
        sb.AppendLine($"Successful: {stats.SuccessfulOperations} ({stats.SuccessRate:F1}%)")
        sb.AppendLine($"Failed: {stats.FailedOperations}")
        sb.AppendLine()

        sb.AppendLine("=== PATCH STATISTICS ===")
        sb.AppendLine($"Total Games Patched: {stats.TotalGamesPatched}")
        sb.AppendLine($"Total Files Patched: {stats.TotalFilesPatched}")
        sb.AppendLine($"Total Files Skipped: {stats.TotalFilesSkipped}")
        sb.AppendLine($"Total Files Failed: {stats.TotalFilesFailed}")
        sb.AppendLine($"Average Files/Operation: {stats.AverageFilesPerOperation:F1}")
        sb.AppendLine()

        sb.AppendLine("=== OTHER STATISTICS ===")
        sb.AppendLine($"Backups Created: {stats.TotalBackupsCreated}")
        sb.AppendLine($"Reports Exported: {stats.TotalReportsExported}")
        sb.AppendLine()

        If stats.SessionHistory.Count > 0 Then
            sb.AppendLine("=== RECENT SESSIONS (Last 10) ===")
            For i = 0 To Math.Min(9, stats.SessionHistory.Count - 1)
                Dim session = stats.SessionHistory(i)
                sb.AppendLine($"[{session.SessionDate:yyyy-MM-dd HH:mm}] {session.OperationType}")
                sb.AppendLine($"  Games: {session.GamesProcessed}, Patched: {session.FilesPatched}, " &
                            $"Skipped: {session.FilesSkipped}, Duration: {session.Duration.TotalSeconds:F1}s")
                sb.AppendLine($"  Status: {If(session.Success, "+ Success", "x Failed")}")
                If Not String.IsNullOrEmpty(session.PresetUsed) Then
                    sb.AppendLine($"  Preset: {session.PresetUsed}")
                End If
                sb.AppendLine()
            Next
        End If

        sb.AppendLine("===============================================================")

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Get statistics for UI display
    ''' </summary>
    Public Function GetQuickStats() As Dictionary(Of String, String)
        Dim stats = LoadStatistics()
        Dim quickStats As New Dictionary(Of String, String)

        quickStats("TotalOperations") = stats.TotalOperations.ToString()
        quickStats("SuccessRate") = $"{stats.SuccessRate:F1}%"
        quickStats("TotalGamesPatched") = stats.TotalGamesPatched.ToString()
        quickStats("TotalFilesPatched") = stats.TotalFilesPatched.ToString()
        quickStats("TotalTimeSpent") = FormatTimeSpan(stats.TotalTimeSpent)
        quickStats("LastUsed") = stats.LastUsedDate.ToString("yyyy-MM-dd HH:mm")

        Return quickStats
    End Function

    ''' <summary>
    ''' Clear all statistics
    ''' </summary>
    Public Sub ClearStatistics()
        Try
            If File.Exists(StatsPath) Then
                File.Delete(StatsPath)
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Export statistics to file
    ''' </summary>
    Public Function ExportStatistics(filePath As String) As Boolean
        Try
            Dim stats = LoadStatistics()
            Dim report = GenerateDashboardReport()
            File.WriteAllText(filePath, report)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Format timespan to readable string
    ''' </summary>
    Private Function FormatTimeSpan(ts As TimeSpan) As String
        If ts.TotalHours >= 1 Then
            Return $"{ts.TotalHours:F1}h"
        ElseIf ts.TotalMinutes >= 1 Then
            Return $"{ts.TotalMinutes:F1}m"
        Else
            Return $"{ts.TotalSeconds:F1}s"
        End If
    End Function

    ''' <summary>
    ''' Get statistics summary for specific time period
    ''' </summary>
    Public Function GetPeriodStatistics(days As Integer) As Dictionary(Of String, Integer)
        Dim stats = LoadStatistics()
        Dim cutoffDate = DateTime.Now.AddDays(-days)

        Dim periodStats As New Dictionary(Of String, Integer)
        periodStats("Operations") = 0
        periodStats("GamesPatched") = 0
        periodStats("FilesPatched") = 0
        periodStats("Successful") = 0
        periodStats("Failed") = 0

        For Each session In stats.SessionHistory
            If session.SessionDate >= cutoffDate Then
                periodStats("Operations") += 1
                periodStats("GamesPatched") += session.GamesProcessed
                periodStats("FilesPatched") += session.FilesPatched
                If session.Success Then
                    periodStats("Successful") += 1
                Else
                    periodStats("Failed") += 1
                End If
            End If
        Next

        Return periodStats
    End Function

    ''' <summary>
    ''' Get top 5 most productive days
    ''' </summary>
    Public Function GetTopProductiveDays() As List(Of Tuple(Of DateTime, Integer))
        Dim stats = LoadStatistics()
        Dim dayStats As New Dictionary(Of DateTime, Integer)

        For Each session In stats.SessionHistory
            Dim dateOnly = session.SessionDate.Date
            If dayStats.ContainsKey(dateOnly) Then
                dayStats(dateOnly) += session.FilesPatched
            Else
                dayStats(dateOnly) = session.FilesPatched
            End If
        Next

        Return dayStats.OrderByDescending(Function(kvp) kvp.Value) _
                       .Take(5) _
                       .Select(Function(kvp) New Tuple(Of DateTime, Integer)(kvp.Key, kvp.Value)) _
                       .ToList()
    End Function

End Module