Imports System.IO
Imports System.Text.Json
Imports System.Data.SQLite

''' <summary>
''' Manages the game library database - tracks all processed games
''' </summary>
Public Class GameLibraryManager

    Private Shared ReadOnly DbPath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PS5BackporkKitchen",
        "gamelibrary.db"
    )

    Private Shared ReadOnly ConnectionString As String = $"Data Source={DbPath};Version=3;"

#Region "Data Models"

    ''' <summary>
    ''' Represents a game entry in the library
    ''' </summary>
    Public Class GameEntry
        Public Property Id As Integer
        Public Property GameTitle As String
        Public Property GameId As String ' PPSA12345
        Public Property FolderPath As String
        Public Property OriginalSdk As String
        Public Property TargetSdk As String
        Public Property PatchDate As DateTime
        Public Property FileCount As Integer
        Public Property TotalSize As Long
        Public Property BackupPath As String
        Public Property Notes As String
        Public Property Status As GameStatus
        Public Property LastAccessed As DateTime
    End Class

    Public Enum GameStatus
        Success = 0
        Failed = 1
        Warning = 2
        Pending = 3
    End Enum

#End Region

#Region "Initialization"

    ''' <summary>
    ''' Initialize database - create if not exists
    ''' </summary>
    Public Shared Sub Initialize()
        Try
            ' Ensure directory exists
            Dim dbDir = Path.GetDirectoryName(DbPath)
            If Not Directory.Exists(dbDir) Then
                Directory.CreateDirectory(dbDir)
            End If

            ' Create database if not exists
            If Not File.Exists(DbPath) Then
                SQLiteConnection.CreateFile(DbPath)
            End If

            ' Create tables
            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim createTableSql As String = "
                CREATE TABLE IF NOT EXISTS Games (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GameTitle TEXT NOT NULL,
                    GameId TEXT NOT NULL,
                    FolderPath TEXT NOT NULL,
                    OriginalSdk TEXT,
                    TargetSdk TEXT,
                    PatchDate TEXT NOT NULL,
                    FileCount INTEGER DEFAULT 0,
                    TotalSize INTEGER DEFAULT 0,
                    BackupPath TEXT,
                    Notes TEXT,
                    Status INTEGER DEFAULT 0,
                    LastAccessed TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_game_id ON Games(GameId);
                CREATE INDEX IF NOT EXISTS idx_patch_date ON Games(PatchDate);
                CREATE INDEX IF NOT EXISTS idx_status ON Games(Status);
                "

                Using cmd As New SQLiteCommand(createTableSql, conn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to initialize game library database: {ex.Message}", ex)
        End Try
    End Sub

#End Region

#Region "CRUD Operations"

    ''' <summary>
    ''' Add a new game to the library
    ''' </summary>
    Public Shared Function AddGame(game As GameEntry) As Integer
        Try
            Initialize() ' Ensure DB exists

            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "
                INSERT INTO Games (GameTitle, GameId, FolderPath, OriginalSdk, TargetSdk,
                                   PatchDate, FileCount, TotalSize, BackupPath, Notes, Status, LastAccessed)
                VALUES (@title, @id, @path, @origSdk, @targSdk, @date, @fileCount, @size, @backup, @notes, @status, @accessed);
                SELECT last_insert_rowid();
                "

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@title", If(game.GameTitle, "Unknown"))
                    cmd.Parameters.AddWithValue("@id", If(game.GameId, ""))
                    cmd.Parameters.AddWithValue("@path", If(game.FolderPath, ""))
                    cmd.Parameters.AddWithValue("@origSdk", If(game.OriginalSdk, ""))
                    cmd.Parameters.AddWithValue("@targSdk", If(game.TargetSdk, ""))
                    cmd.Parameters.AddWithValue("@date", game.PatchDate.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.Parameters.AddWithValue("@fileCount", game.FileCount)
                    cmd.Parameters.AddWithValue("@size", game.TotalSize)
                    cmd.Parameters.AddWithValue("@backup", If(game.BackupPath, ""))
                    cmd.Parameters.AddWithValue("@notes", If(game.Notes, ""))
                    cmd.Parameters.AddWithValue("@status", CInt(game.Status))
                    cmd.Parameters.AddWithValue("@accessed", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to add game to library: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Update existing game entry
    ''' </summary>
    Public Shared Sub UpdateGame(game As GameEntry)
        Try
            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "
                UPDATE Games SET
                    GameTitle = @title,
                    GameId = @id,
                    FolderPath = @path,
                    OriginalSdk = @origSdk,
                    TargetSdk = @targSdk,
                    PatchDate = @date,
                    FileCount = @fileCount,
                    TotalSize = @size,
                    BackupPath = @backup,
                    Notes = @notes,
                    Status = @status,
                    LastAccessed = @accessed
                WHERE Id = @gameId
                "

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@gameId", game.Id)
                    cmd.Parameters.AddWithValue("@title", If(game.GameTitle, "Unknown"))
                    cmd.Parameters.AddWithValue("@id", If(game.GameId, ""))
                    cmd.Parameters.AddWithValue("@path", If(game.FolderPath, ""))
                    cmd.Parameters.AddWithValue("@origSdk", If(game.OriginalSdk, ""))
                    cmd.Parameters.AddWithValue("@targSdk", If(game.TargetSdk, ""))
                    cmd.Parameters.AddWithValue("@date", game.PatchDate.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.Parameters.AddWithValue("@fileCount", game.FileCount)
                    cmd.Parameters.AddWithValue("@size", game.TotalSize)
                    cmd.Parameters.AddWithValue("@backup", If(game.BackupPath, ""))
                    cmd.Parameters.AddWithValue("@notes", If(game.Notes, ""))
                    cmd.Parameters.AddWithValue("@status", CInt(game.Status))
                    cmd.Parameters.AddWithValue("@accessed", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to update game: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Delete game from library
    ''' </summary>
    Public Shared Sub DeleteGame(gameId As Integer)
        Try
            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "DELETE FROM Games WHERE Id = @id"

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@id", gameId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to delete game: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Get all games from library
    ''' </summary>
    Public Shared Function GetAllGames() As List(Of GameEntry)
        Try
            Initialize() ' Ensure DB exists

            Dim games As New List(Of GameEntry)

            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "SELECT * FROM Games ORDER BY PatchDate DESC"

                Using cmd As New SQLiteCommand(sql, conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            games.Add(ReadGameFromReader(reader))
                        End While
                    End Using
                End Using
            End Using

            Return games
        Catch ex As Exception
            Throw New Exception($"Failed to get games: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Get game by ID
    ''' </summary>
    Public Shared Function GetGameById(gameId As Integer) As GameEntry
        Try
            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "SELECT * FROM Games WHERE Id = @id"

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@id", gameId)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return ReadGameFromReader(reader)
                        End If
                    End Using
                End Using
            End Using

            Return Nothing
        Catch ex As Exception
            Throw New Exception($"Failed to get game: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Search games by title or ID
    ''' </summary>
    Public Shared Function SearchGames(searchTerm As String) As List(Of GameEntry)
        Try
            Dim games As New List(Of GameEntry)

            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "
                SELECT * FROM Games
                WHERE GameTitle LIKE @search OR GameId LIKE @search
                ORDER BY PatchDate DESC
                "

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%")

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            games.Add(ReadGameFromReader(reader))
                        End While
                    End Using
                End Using
            End Using

            Return games
        Catch ex As Exception
            Throw New Exception($"Failed to search games: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Filter games by status
    ''' </summary>
    Public Shared Function FilterByStatus(status As GameStatus) As List(Of GameEntry)
        Try
            Dim games As New List(Of GameEntry)

            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "SELECT * FROM Games WHERE Status = @status ORDER BY PatchDate DESC"

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@status", CInt(status))

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            games.Add(ReadGameFromReader(reader))
                        End While
                    End Using
                End Using
            End Using

            Return games
        Catch ex As Exception
            Throw New Exception($"Failed to filter games: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Update last accessed timestamp
    ''' </summary>
    Public Shared Sub UpdateLastAccessed(gameId As Integer)
        Try
            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                Dim sql As String = "UPDATE Games SET LastAccessed = @time WHERE Id = @id"

                Using cmd As New SQLiteCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@id", gameId)
                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            ' Silent fail - not critical
        End Try
    End Sub

#End Region

#Region "Export Functions"

    ''' <summary>
    ''' Export library to CSV
    ''' </summary>
    Public Shared Sub ExportToCsv(filePath As String)
        Try
            Dim games = GetAllGames()

            Using writer As New StreamWriter(filePath, False, Text.Encoding.UTF8)
                ' Header
                writer.WriteLine("ID,Title,GameID,FolderPath,OriginalSDK,TargetSDK,PatchDate,FileCount,TotalSize,BackupPath,Status,Notes")

                ' Data
                For Each game In games
                    writer.WriteLine($"{game.Id},""{EscapeCsv(game.GameTitle)}"",""{game.GameId}"",""{EscapeCsv(game.FolderPath)}"",""{game.OriginalSdk}"",""{game.TargetSdk}"",""{game.PatchDate:yyyy-MM-dd HH:mm:ss}"",{game.FileCount},{game.TotalSize},""{EscapeCsv(game.BackupPath)}"",""{game.Status}"",""{EscapeCsv(game.Notes)}""")
                Next
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to export to CSV: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Export library to JSON
    ''' </summary>
    Public Shared Sub ExportToJson(filePath As String)
        Try
            Dim games = GetAllGames()

            Dim options As New JsonSerializerOptions With {
                .WriteIndented = True
            }

            Dim json = JsonSerializer.Serialize(games, options)
            File.WriteAllText(filePath, json, Text.Encoding.UTF8)
        Catch ex As Exception
            Throw New Exception($"Failed to export to JSON: {ex.Message}", ex)
        End Try
    End Sub

    Private Shared Function EscapeCsv(value As String) As String
        If String.IsNullOrEmpty(value) Then Return ""
        Return value.Replace("""", """""")
    End Function

#End Region

#Region "Statistics"

    ''' <summary>
    ''' Get library statistics
    ''' </summary>
    Public Shared Function GetStatistics() As Dictionary(Of String, Object)
        Try
            Dim stats As New Dictionary(Of String, Object)

            Using conn As New SQLiteConnection(ConnectionString)
                conn.Open()

                ' Total games
                Using cmd As New SQLiteCommand("SELECT COUNT(*) FROM Games", conn)
                    stats("TotalGames") = Convert.ToInt32(cmd.ExecuteScalar())
                End Using

                ' Total size
                Using cmd As New SQLiteCommand("SELECT SUM(TotalSize) FROM Games", conn)
                    Dim result = cmd.ExecuteScalar()
                    stats("TotalSize") = If(IsDBNull(result), 0L, Convert.ToInt64(result))
                End Using

                ' Status counts
                For Each status In [Enum].GetValues(GetType(GameStatus))
                    Using cmd As New SQLiteCommand($"SELECT COUNT(*) FROM Games WHERE Status = {CInt(status)}", conn)
                        stats($"Status{status}") = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using
                Next

                ' Most recent patch
                Using cmd As New SQLiteCommand("SELECT MAX(PatchDate) FROM Games", conn)
                    Dim result = cmd.ExecuteScalar()
                    If Not IsDBNull(result) Then
                        stats("LastPatchDate") = DateTime.Parse(result.ToString())
                    End If
                End Using
            End Using

            Return stats
        Catch ex As Exception
            Return New Dictionary(Of String, Object)
        End Try
    End Function

#End Region

#Region "Helper Methods"

    Private Shared Function ReadGameFromReader(reader As SQLiteDataReader) As GameEntry
        Return New GameEntry With {
            .Id = Convert.ToInt32(reader("Id")),
            .GameTitle = reader("GameTitle").ToString(),
            .GameId = reader("GameId").ToString(),
            .FolderPath = reader("FolderPath").ToString(),
            .OriginalSdk = reader("OriginalSdk").ToString(),
            .TargetSdk = reader("TargetSdk").ToString(),
            .PatchDate = DateTime.Parse(reader("PatchDate").ToString()),
            .FileCount = Convert.ToInt32(reader("FileCount")),
            .TotalSize = Convert.ToInt64(reader("TotalSize")),
            .BackupPath = reader("BackupPath").ToString(),
            .Notes = reader("Notes").ToString(),
            .Status = CType(Convert.ToInt32(reader("Status")), GameStatus),
            .LastAccessed = DateTime.Parse(reader("LastAccessed").ToString())
        }
    End Function

#End Region

End Class