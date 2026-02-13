Imports System.Data.SQLite
Imports System.IO

''' <summary>
''' SQLite-based statistics database service
''' Replaces JSON-based StatisticsManager with persistent database storage
''' </summary>
Public Class StatisticsDatabase
    Implements IDisposable

    Private ReadOnly connectionString As String
    Private connection As SQLiteConnection

    Public Sub New(Optional databasePath As String = Nothing)
        If String.IsNullOrEmpty(databasePath) Then
            databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statistics.db")
        End If

        connectionString = $"Data Source={databasePath};Version=3;"
        InitializeDatabase()
    End Sub

    ''' <summary>
    ''' Initialize database and create tables if they don't exist
    ''' </summary>
    Private Sub InitializeDatabase()
        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            ' Create Sessions table
            Dim createSessionsTable As String = "
            CREATE TABLE IF NOT EXISTS Sessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionDate TEXT NOT NULL,
                OperationType TEXT NOT NULL,
                GamesProcessed INTEGER DEFAULT 0,
                FilesPatched INTEGER DEFAULT 0,
                FilesSkipped INTEGER DEFAULT 0,
                FilesFailed INTEGER DEFAULT 0,
                DurationSeconds REAL DEFAULT 0,
                Success INTEGER DEFAULT 0,
                PresetUsed TEXT,
                TargetSdk TEXT,
                Notes TEXT
            )"

            ' Create Operations table (aggregate stats)
            Dim createOperationsTable As String = "
            CREATE TABLE IF NOT EXISTS Operations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OperationType TEXT NOT NULL UNIQUE,
                TotalCount INTEGER DEFAULT 0,
                SuccessCount INTEGER DEFAULT 0,
                FailCount INTEGER DEFAULT 0,
                LastUsed TEXT
            )"

            ' Create DailyStats table (for trends)
            Dim createDailyStatsTable As String = "
            CREATE TABLE IF NOT EXISTS DailyStats (
                Date TEXT PRIMARY KEY,
                TotalOperations INTEGER DEFAULT 0,
                SuccessfulOperations INTEGER DEFAULT 0,
                FailedOperations INTEGER DEFAULT 0,
                FilesPatched INTEGER DEFAULT 0,
                GamesProcessed INTEGER DEFAULT 0,
                TotalDurationSeconds REAL DEFAULT 0
            )"

            ' Create AppSettings table (global stats)
            Dim createAppSettingsTable As String = "
            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )"

            Using cmd As New SQLiteCommand(conn)
                cmd.CommandText = createSessionsTable
                cmd.ExecuteNonQuery()

                cmd.CommandText = createOperationsTable
                cmd.ExecuteNonQuery()

                cmd.CommandText = createDailyStatsTable
                cmd.ExecuteNonQuery()

                cmd.CommandText = createAppSettingsTable
                cmd.ExecuteNonQuery()
            End Using

            ' Initialize first use date if not exists
            Dim firstUse = GetSetting("FirstUsedDate")
            If String.IsNullOrEmpty(firstUse) Then
                SetSetting("FirstUsedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            End If
        End Using
    End Sub

#Region "Session Recording"

    ''' <summary>
    ''' Record a new operation session
    ''' </summary>
    Public Function RecordSession(
        operationType As String,
        gamesProcessed As Integer,
        filesPatched As Integer,
        filesSkipped As Integer,
        filesFailed As Integer,
        duration As TimeSpan,
        success As Boolean,
        Optional presetUsed As String = "",
        Optional targetSdk As String = "",
        Optional notes As String = ""
    ) As Long

        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            Dim sessionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")

            ' Insert session
            Dim insertSession As String = "
            INSERT INTO Sessions (SessionDate, OperationType, GamesProcessed, FilesPatched,
                                  FilesSkipped, FilesFailed, DurationSeconds, Success,
                                  PresetUsed, TargetSdk, Notes)
            VALUES (@date, @opType, @games, @patched, @skipped, @failed, @duration, @success,
                    @preset, @sdk, @notes)"

            Dim sessionId As Long

            Using cmd As New SQLiteCommand(insertSession, conn)
                cmd.Parameters.AddWithValue("@date", sessionDate)
                cmd.Parameters.AddWithValue("@opType", operationType)
                cmd.Parameters.AddWithValue("@games", gamesProcessed)
                cmd.Parameters.AddWithValue("@patched", filesPatched)
                cmd.Parameters.AddWithValue("@skipped", filesSkipped)
                cmd.Parameters.AddWithValue("@failed", filesFailed)
                cmd.Parameters.AddWithValue("@duration", duration.TotalSeconds)
                cmd.Parameters.AddWithValue("@success", If(success, 1, 0))
                cmd.Parameters.AddWithValue("@preset", presetUsed)
                cmd.Parameters.AddWithValue("@sdk", targetSdk)
                cmd.Parameters.AddWithValue("@notes", notes)
                cmd.ExecuteNonQuery()

                sessionId = conn.LastInsertRowId
            End Using

            ' Update operation stats
            UpdateOperationStats(conn, operationType, success, sessionDate)

            ' Update daily stats
            Dim dateOnly = DateTime.Now.ToString("yyyy-MM-dd")
            UpdateDailyStats(conn, dateOnly, success, filesPatched, gamesProcessed, duration)

            ' Update last used date
            SetSetting("LastUsedDate", sessionDate, conn)

            Return sessionId
        End Using
    End Function

    ''' <summary>
    ''' Update operation statistics
    ''' </summary>
    Private Sub UpdateOperationStats(conn As SQLiteConnection, operationType As String, success As Boolean, lastUsed As String)
        Dim updateOp As String = "
        INSERT INTO Operations (OperationType, TotalCount, SuccessCount, FailCount, LastUsed)
        VALUES (@opType, 1, @success, @fail, @lastUsed)
        ON CONFLICT(OperationType) DO UPDATE SET
            TotalCount = TotalCount + 1,
            SuccessCount = SuccessCount + @success,
            FailCount = FailCount + @fail,
            LastUsed = @lastUsed"

        Using cmd As New SQLiteCommand(updateOp, conn)
            cmd.Parameters.AddWithValue("@opType", operationType)
            cmd.Parameters.AddWithValue("@success", If(success, 1, 0))
            cmd.Parameters.AddWithValue("@fail", If(success, 0, 1))
            cmd.Parameters.AddWithValue("@lastUsed", lastUsed)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Update daily statistics
    ''' </summary>
    Private Sub UpdateDailyStats(conn As SQLiteConnection, dateOnly As String, success As Boolean,
                                  filesPatched As Integer, gamesProcessed As Integer, duration As TimeSpan)
        Dim updateDaily As String = "
        INSERT INTO DailyStats (Date, TotalOperations, SuccessfulOperations, FailedOperations,
                                FilesPatched, GamesProcessed, TotalDurationSeconds)
        VALUES (@date, 1, @success, @fail, @patched, @games, @duration)
        ON CONFLICT(Date) DO UPDATE SET
            TotalOperations = TotalOperations + 1,
            SuccessfulOperations = SuccessfulOperations + @success,
            FailedOperations = FailedOperations + @fail,
            FilesPatched = FilesPatched + @patched,
            GamesProcessed = GamesProcessed + @games,
            TotalDurationSeconds = TotalDurationSeconds + @duration"

        Using cmd As New SQLiteCommand(updateDaily, conn)
            cmd.Parameters.AddWithValue("@date", dateOnly)
            cmd.Parameters.AddWithValue("@success", If(success, 1, 0))
            cmd.Parameters.AddWithValue("@fail", If(success, 0, 1))
            cmd.Parameters.AddWithValue("@patched", filesPatched)
            cmd.Parameters.AddWithValue("@games", gamesProcessed)
            cmd.Parameters.AddWithValue("@duration", duration.TotalSeconds)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

#End Region

#Region "Query Methods"

    ''' <summary>
    ''' Get recent sessions with optional filters
    ''' </summary>
    Public Function GetRecentSessions(
        Optional limit As Integer = 100,
        Optional operationType As String = Nothing,
        Optional startDate As DateTime? = Nothing,
        Optional endDate As DateTime? = Nothing
    ) As List(Of SessionRecord)

        Dim sessions As New List(Of SessionRecord)

        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            Dim query As String = "SELECT * FROM Sessions WHERE 1=1"

            If Not String.IsNullOrEmpty(operationType) Then
                query &= " AND OperationType = @opType"
            End If

            If startDate.HasValue Then
                query &= " AND SessionDate >= @startDate"
            End If

            If endDate.HasValue Then
                query &= " AND SessionDate <= @endDate"
            End If

            query &= " ORDER BY SessionDate DESC LIMIT @limit"

            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@limit", limit)
                If Not String.IsNullOrEmpty(operationType) Then
                    cmd.Parameters.AddWithValue("@opType", operationType)
                End If
                If startDate.HasValue Then
                    cmd.Parameters.AddWithValue("@startDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"))
                End If
                If endDate.HasValue Then
                    cmd.Parameters.AddWithValue("@endDate", endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"))
                End If

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        sessions.Add(New SessionRecord With {
                            .SessionDate = DateTime.Parse(reader("SessionDate").ToString()),
                            .OperationType = reader("OperationType").ToString(),
                            .GamesProcessed = Convert.ToInt32(reader("GamesProcessed")),
                            .FilesPatched = Convert.ToInt32(reader("FilesPatched")),
                            .FilesSkipped = Convert.ToInt32(reader("FilesSkipped")),
                            .FilesFailed = Convert.ToInt32(reader("FilesFailed")),
                            .Duration = TimeSpan.FromSeconds(Convert.ToDouble(reader("DurationSeconds"))),
                            .Success = Convert.ToInt32(reader("Success")) = 1,
                            .PresetUsed = reader("PresetUsed").ToString(),
                            .TargetSdk = reader("TargetSdk").ToString()
                        })
                    End While
                End Using
            End Using
        End Using

        Return sessions
    End Function

    ''' <summary>
    ''' Get overall statistics summary
    ''' </summary>
    Public Function GetOverallStats() As Dictionary(Of String, Object)
        Dim stats As New Dictionary(Of String, Object)

        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            ' Total sessions
            stats("TotalSessions") = ExecuteScalar(Of Long)("SELECT COALESCE(COUNT(*), 0) FROM Sessions", conn)

            ' Success/fail counts
            stats("SuccessfulSessions") = ExecuteScalar(Of Long)("SELECT COALESCE(COUNT(*), 0) FROM Sessions WHERE Success = 1", conn)
            stats("FailedSessions") = ExecuteScalar(Of Long)("SELECT COALESCE(COUNT(*), 0) FROM Sessions WHERE Success = 0", conn)

            ' Files totals
            stats("TotalFilesPatched") = ExecuteScalar(Of Long)("SELECT COALESCE(SUM(FilesPatched), 0) FROM Sessions", conn)
            stats("TotalFilesSkipped") = ExecuteScalar(Of Long)("SELECT COALESCE(SUM(FilesSkipped), 0) FROM Sessions", conn)
            stats("TotalFilesFailed") = ExecuteScalar(Of Long)("SELECT COALESCE(SUM(FilesFailed), 0) FROM Sessions", conn)

            ' Games total
            stats("TotalGamesProcessed") = ExecuteScalar(Of Long)("SELECT COALESCE(SUM(GamesProcessed), 0) FROM Sessions", conn)

            ' Duration
            Dim totalSeconds = ExecuteScalar(Of Double)("SELECT COALESCE(SUM(DurationSeconds), 0.0) FROM Sessions", conn)
            stats("TotalDuration") = TimeSpan.FromSeconds(If(totalSeconds > 0, totalSeconds, 0))

            ' Dates
            stats("FirstUsedDate") = GetSetting("FirstUsedDate", conn)
            stats("LastUsedDate") = GetSetting("LastUsedDate", conn)

            ' Success rate
            Dim total = Convert.ToDouble(stats("TotalSessions"))
            Dim successful = Convert.ToDouble(stats("SuccessfulSessions"))
            stats("SuccessRate") = If(total > 0, (successful / total) * 100, 0)
        End Using

        Return stats
    End Function

    ''' <summary>
    ''' Get daily statistics for charting
    ''' </summary>
    Public Function GetDailyStats(Optional days As Integer = 30) As List(Of DailyStatRecord)
        Dim dailyStats As New List(Of DailyStatRecord)
        Dim startDate = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd")

        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            Dim query = "SELECT * FROM DailyStats WHERE Date >= @startDate ORDER BY Date ASC"

            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@startDate", startDate)

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        dailyStats.Add(New DailyStatRecord With {
                            .Date = DateTime.Parse(reader("Date").ToString()),
                            .TotalOperations = Convert.ToInt32(reader("TotalOperations")),
                            .SuccessfulOperations = Convert.ToInt32(reader("SuccessfulOperations")),
                            .FailedOperations = Convert.ToInt32(reader("FailedOperations")),
                            .FilesPatched = Convert.ToInt32(reader("FilesPatched")),
                            .GamesProcessed = Convert.ToInt32(reader("GamesProcessed")),
                            .TotalDuration = TimeSpan.FromSeconds(Convert.ToDouble(reader("TotalDurationSeconds")))
                        })
                    End While
                End Using
            End Using
        End Using

        Return dailyStats
    End Function

    ''' <summary>
    ''' Get operation type statistics
    ''' </summary>
    Public Function GetOperationTypeStats() As List(Of OperationStat)
        Dim opStats As New List(Of OperationStat)

        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            Dim query = "SELECT * FROM Operations ORDER BY TotalCount DESC"

            Using cmd As New SQLiteCommand(query, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim total = Convert.ToInt32(reader("TotalCount"))
                        Dim success = Convert.ToInt32(reader("SuccessCount"))

                        opStats.Add(New OperationStat With {
                            .OperationType = reader("OperationType").ToString(),
                            .TotalCount = total,
                            .SuccessCount = success,
                            .FailCount = Convert.ToInt32(reader("FailCount")),
                            .SuccessRate = If(total > 0, (success / total) * 100, 0),
                            .LastUsed = DateTime.Parse(reader("LastUsed").ToString())
                        })
                    End While
                End Using
            End Using
        End Using

        Return opStats
    End Function

#End Region

#Region "Settings Management"

    Private Function GetSetting(key As String, Optional conn As SQLiteConnection = Nothing) As String
        Dim shouldClose = (conn Is Nothing)
        If conn Is Nothing Then
            conn = New SQLiteConnection(connectionString)
            conn.Open()
        End If

        Try
            Dim query = "SELECT Value FROM AppSettings WHERE Key = @key"
            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@key", key)
                Dim result = cmd.ExecuteScalar()
                Return If(result IsNot Nothing, result.ToString(), "")
            End Using
        Finally
            If shouldClose AndAlso conn IsNot Nothing Then
                conn.Close()
                conn.Dispose()
            End If
        End Try
    End Function

    Private Sub SetSetting(key As String, value As String, Optional conn As SQLiteConnection = Nothing)
        Dim shouldClose = (conn Is Nothing)
        If conn Is Nothing Then
            conn = New SQLiteConnection(connectionString)
            conn.Open()
        End If

        Try
            Dim query = "INSERT OR REPLACE INTO AppSettings (Key, Value) VALUES (@key, @value)"
            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@key", key)
                cmd.Parameters.AddWithValue("@value", value)
                cmd.ExecuteNonQuery()
            End Using
        Finally
            If shouldClose AndAlso conn IsNot Nothing Then
                conn.Close()
                conn.Dispose()
            End If
        End Try
    End Sub

    Private Function ExecuteScalar(Of T)(query As String, conn As SQLiteConnection) As T
        Using cmd As New SQLiteCommand(query, conn)
            Dim result = cmd.ExecuteScalar()
            If result Is Nothing OrElse IsDBNull(result) Then
                ' Return default value instead of Nothing to prevent NullReferenceException
                Return If(GetType(T).IsValueType, CType(Activator.CreateInstance(GetType(T)), T), Nothing)
            End If
            Return CType(result, T)
        End Using
    End Function

#End Region

#Region "Export Methods"

    ''' <summary>
    ''' Export statistics to CSV
    ''' </summary>
    Public Function ExportToCSV(filePath As String) As Boolean
        Try
            Dim sessions = GetRecentSessions(10000) ' Export all

            Using writer As New StreamWriter(filePath)
                ' Header
                writer.WriteLine("Date,Operation Type,Games,Patched,Skipped,Failed,Duration (s),Success,Preset,SDK")

                ' Data
                For Each session In sessions
                    writer.WriteLine($"{session.SessionDate:yyyy-MM-dd HH:mm:ss}," &
                                   $"{session.OperationType}," &
                                   $"{session.GamesProcessed}," &
                                   $"{session.FilesPatched}," &
                                   $"{session.FilesSkipped}," &
                                   $"{session.FilesFailed}," &
                                   $"{session.Duration.TotalSeconds:F1}," &
                                   $"{session.Success}," &
                                   $"{session.PresetUsed}," &
                                   $"{session.TargetSdk}")
                Next
            End Using

            Return True
        Catch
            Return False
        End Try
    End Function

#End Region

    Public Sub Dispose() Implements IDisposable.Dispose
        If connection IsNot Nothing Then
            connection.Close()
            connection.Dispose()
        End If
    End Sub

End Class

#Region "Data Structures"

Public Structure DailyStatRecord
    Public [Date] As DateTime
    Public TotalOperations As Integer
    Public SuccessfulOperations As Integer
    Public FailedOperations As Integer
    Public FilesPatched As Integer
    Public GamesProcessed As Integer
    Public TotalDuration As TimeSpan
End Structure

Public Structure OperationStat
    Public OperationType As String
    Public TotalCount As Integer
    Public SuccessCount As Integer
    Public FailCount As Integer
    Public SuccessRate As Double
    Public LastUsed As DateTime
End Structure

#End Region