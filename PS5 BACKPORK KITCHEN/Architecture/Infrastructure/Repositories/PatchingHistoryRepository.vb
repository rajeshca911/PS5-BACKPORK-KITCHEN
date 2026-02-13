Imports System.Data.SQLite
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models

Namespace Architecture.Infrastructure.Repositories
    ''' <summary>
    ''' SQLite-based repository for patching history
    ''' </summary>
    Public Class PatchingHistoryRepository
        Implements IDisposable

        Private ReadOnly _dbPath As String
        Private ReadOnly _connectionString As String

        Public Sub New(Optional dbPath As String = Nothing)
            _dbPath = If(dbPath, IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "patching_history.db"))
            _connectionString = $"Data Source={_dbPath};Version=3;"
            InitializeDatabase()
        End Sub

        ''' <summary>
        ''' Initialize database schema
        ''' </summary>
        Private Sub InitializeDatabase()
            Using conn As New SQLiteConnection(_connectionString)
                conn.Open()

                Dim createTableSql = "
CREATE TABLE IF NOT EXISTS PatchingHistory (
    Id TEXT PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    OperationType INTEGER NOT NULL,
    SourcePath TEXT,
    GameName TEXT,
    TargetSdk INTEGER NOT NULL,
    TotalFiles INTEGER DEFAULT 0,
    PatchedFiles INTEGER DEFAULT 0,
    SkippedFiles INTEGER DEFAULT 0,
    FailedFiles INTEGER DEFAULT 0,
    Duration TEXT,
    Success INTEGER DEFAULT 0,
    BackupPath TEXT,
    ErrorMessage TEXT,
    MachineName TEXT,
    UserName TEXT,
    AppVersion TEXT
)"

                Using cmd As New SQLiteCommand(createTableSql, conn)
                    cmd.ExecuteNonQuery()
                End Using

                ' Create indexes for better query performance
                Dim indexSql1 = "CREATE INDEX IF NOT EXISTS idx_timestamp ON PatchingHistory(Timestamp DESC)"
                Dim indexSql2 = "CREATE INDEX IF NOT EXISTS idx_game ON PatchingHistory(GameName)"
                Dim indexSql3 = "CREATE INDEX IF NOT EXISTS idx_sdk ON PatchingHistory(TargetSdk)"

                Using cmd As New SQLiteCommand(indexSql1, conn)
                    cmd.ExecuteNonQuery()
                End Using
                Using cmd As New SQLiteCommand(indexSql2, conn)
                    cmd.ExecuteNonQuery()
                End Using
                Using cmd As New SQLiteCommand(indexSql3, conn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Add new history entry
        ''' </summary>
        Public Function Add(entry As PatchingHistoryEntry) As Boolean
            Try
                Using conn As New SQLiteConnection(_connectionString)
                    conn.Open()

                    Dim sql = "INSERT INTO PatchingHistory (Id, Timestamp, OperationType, SourcePath, GameName, " &
                             "TargetSdk, TotalFiles, PatchedFiles, SkippedFiles, FailedFiles, Duration, Success, " &
                             "BackupPath, ErrorMessage, MachineName, UserName, AppVersion) " &
                             "VALUES (@Id, @Timestamp, @OperationType, @SourcePath, @GameName, " &
                             "@TargetSdk, @TotalFiles, @PatchedFiles, @SkippedFiles, @FailedFiles, @Duration, @Success, " &
                             "@BackupPath, @ErrorMessage, @MachineName, @UserName, @AppVersion)"

                    Using cmd As New SQLiteCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@Id", entry.Id.ToString())
                        cmd.Parameters.AddWithValue("@Timestamp", entry.Timestamp.ToString("O"))
                        cmd.Parameters.AddWithValue("@OperationType", CInt(entry.OperationType))
                        cmd.Parameters.AddWithValue("@SourcePath", If(entry.SourcePath, String.Empty))
                        cmd.Parameters.AddWithValue("@GameName", If(entry.GameName, String.Empty))
                        cmd.Parameters.AddWithValue("@TargetSdk", entry.TargetSdk)
                        cmd.Parameters.AddWithValue("@TotalFiles", entry.TotalFiles)
                        cmd.Parameters.AddWithValue("@PatchedFiles", entry.PatchedFiles)
                        cmd.Parameters.AddWithValue("@SkippedFiles", entry.SkippedFiles)
                        cmd.Parameters.AddWithValue("@FailedFiles", entry.FailedFiles)
                        cmd.Parameters.AddWithValue("@Duration", entry.Duration.ToString())
                        cmd.Parameters.AddWithValue("@Success", If(entry.Success, 1, 0))
                        cmd.Parameters.AddWithValue("@BackupPath", If(entry.BackupPath, String.Empty))
                        cmd.Parameters.AddWithValue("@ErrorMessage", If(entry.ErrorMessage, String.Empty))
                        cmd.Parameters.AddWithValue("@MachineName", If(entry.MachineName, String.Empty))
                        cmd.Parameters.AddWithValue("@UserName", If(entry.UserName, String.Empty))
                        cmd.Parameters.AddWithValue("@AppVersion", If(entry.AppVersion, String.Empty))

                        cmd.ExecuteNonQuery()
                    End Using
                End Using
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Get all history entries
        ''' </summary>
        Public Function GetAll(Optional limit As Integer = 100) As List(Of PatchingHistoryEntry)
            Dim entries As New List(Of PatchingHistoryEntry)

            Try
                Using conn As New SQLiteConnection(_connectionString)
                    conn.Open()

                    Dim sql = $"SELECT * FROM PatchingHistory ORDER BY Timestamp DESC LIMIT {limit}"

                    Using cmd As New SQLiteCommand(sql, conn)
                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                entries.Add(MapReaderToEntry(reader))
                            End While
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                ' Return empty list on error
            End Try

            Return entries
        End Function

        ''' <summary>
        ''' Get history for specific game
        ''' </summary>
        Public Function GetByGame(gameName As String) As List(Of PatchingHistoryEntry)
            Dim entries As New List(Of PatchingHistoryEntry)

            Try
                Using conn As New SQLiteConnection(_connectionString)
                    conn.Open()

                    Dim sql = "SELECT * FROM PatchingHistory WHERE GameName = @GameName ORDER BY Timestamp DESC"

                    Using cmd As New SQLiteCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@GameName", gameName)

                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                entries.Add(MapReaderToEntry(reader))
                            End While
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                ' Return empty list on error
            End Try

            Return entries
        End Function

        ''' <summary>
        ''' Get statistics from history
        ''' </summary>
        Public Function GetStatistics() As PatchingStatistics
            Dim stats As New PatchingStatistics()

            Try
                Using conn As New SQLiteConnection(_connectionString)
                    conn.Open()

                    Dim sql = "SELECT COUNT(*) as Total, " &
                             "SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as Successful, " &
                             "SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as Failed, " &
                             "SUM(PatchedFiles) as TotalPatched, " &
                             "SUM(SkippedFiles) as TotalSkipped, " &
                             "SUM(FailedFiles) as TotalFailed, " &
                             "AVG(CAST((julianday(Duration) - 2440587.5)*86400.0 AS REAL)) as AvgDurationSeconds, " &
                             "MIN(Timestamp) as FirstOp, " &
                             "MAX(Timestamp) as LastOp " &
                             "FROM PatchingHistory"

                    Using cmd As New SQLiteCommand(sql, conn)
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                stats.TotalOperations = If(Not reader.IsDBNull(0), reader.GetInt32(0), 0)
                                stats.SuccessfulOperations = If(Not reader.IsDBNull(1), reader.GetInt32(1), 0)
                                stats.FailedOperations = If(Not reader.IsDBNull(2), reader.GetInt32(2), 0)
                                stats.TotalFilesPatched = If(Not reader.IsDBNull(3), reader.GetInt32(3), 0)
                                stats.TotalFilesSkipped = If(Not reader.IsDBNull(4), reader.GetInt32(4), 0)
                                stats.TotalFilesFailed = If(Not reader.IsDBNull(5), reader.GetInt32(5), 0)

                                If Not reader.IsDBNull(6) Then
                                    stats.AverageDuration = TimeSpan.FromSeconds(reader.GetDouble(6))
                                End If

                                If Not reader.IsDBNull(7) Then
                                    stats.FirstOperationDate = DateTime.Parse(reader.GetString(7))
                                End If

                                If Not reader.IsDBNull(8) Then
                                    stats.LastOperationDate = DateTime.Parse(reader.GetString(8))
                                End If
                            End If
                        End Using
                    End Using

                    ' Get most used SDK
                    Dim sdkSql = "SELECT TargetSdk, COUNT(*) as Count FROM PatchingHistory GROUP BY TargetSdk ORDER BY Count DESC LIMIT 1"
                    Using cmd As New SQLiteCommand(sdkSql, conn)
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() AndAlso Not reader.IsDBNull(0) Then
                                stats.MostUsedSdk = reader.GetInt64(0)
                            End If
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                ' Return empty stats on error
            End Try

            Return stats
        End Function

        ''' <summary>
        ''' Delete old entries (cleanup)
        ''' </summary>
        Public Function DeleteOlderThan(days As Integer) As Integer
            Try
                Using conn As New SQLiteConnection(_connectionString)
                    conn.Open()

                    Dim cutoffDate = DateTime.Now.AddDays(-days).ToString("O")
                    Dim sql = "DELETE FROM PatchingHistory WHERE Timestamp < @CutoffDate"

                    Using cmd As New SQLiteCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@CutoffDate", cutoffDate)
                        Return cmd.ExecuteNonQuery()
                    End Using
                End Using
            Catch ex As Exception
                Return 0
            End Try
        End Function

        ''' <summary>
        ''' Clear all history
        ''' </summary>
        Public Function Clear() As Boolean
            Try
                Using conn As New SQLiteConnection(_connectionString)
                    conn.Open()

                    Dim sql = "DELETE FROM PatchingHistory"

                    Using cmd As New SQLiteCommand(sql, conn)
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function MapReaderToEntry(reader As SQLiteDataReader) As PatchingHistoryEntry
            Return New PatchingHistoryEntry With {
                .Id = Guid.Parse(reader("Id").ToString()),
                .Timestamp = DateTime.Parse(reader("Timestamp").ToString()),
                .OperationType = CType(reader.GetInt32(reader.GetOrdinal("OperationType")), OperationType),
                .SourcePath = reader("SourcePath").ToString(),
                .GameName = reader("GameName").ToString(),
                .TargetSdk = reader.GetInt64(reader.GetOrdinal("TargetSdk")),
                .TotalFiles = reader.GetInt32(reader.GetOrdinal("TotalFiles")),
                .PatchedFiles = reader.GetInt32(reader.GetOrdinal("PatchedFiles")),
                .SkippedFiles = reader.GetInt32(reader.GetOrdinal("SkippedFiles")),
                .FailedFiles = reader.GetInt32(reader.GetOrdinal("FailedFiles")),
                .Duration = TimeSpan.Parse(reader("Duration").ToString()),
                .Success = reader.GetInt32(reader.GetOrdinal("Success")) = 1,
                .BackupPath = reader("BackupPath").ToString(),
                .ErrorMessage = reader("ErrorMessage").ToString(),
                .MachineName = reader("MachineName").ToString(),
                .UserName = reader("UserName").ToString(),
                .AppVersion = reader("AppVersion").ToString()
            }
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Cleanup if needed
        End Sub
    End Class
End Namespace
