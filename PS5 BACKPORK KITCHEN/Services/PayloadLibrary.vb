Imports System.IO
Imports System.Data.SQLite
Imports Newtonsoft.Json

''' <summary>
''' Payload Library Manager for storing and managing PS5 payloads
''' </summary>
Public Class PayloadLibrary
    Implements IDisposable

    Private ReadOnly connectionString As String
    Private connection As SQLiteConnection

    Public Sub New()
        Dim appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PS5BackporkKitchen"
        )
        If Not Directory.Exists(appDataPath) Then
            Directory.CreateDirectory(appDataPath)
        End If

        Dim dbPath = Path.Combine(appDataPath, "payloads.db")
        connectionString = $"Data Source={dbPath};Version=3;"
        connection = New SQLiteConnection(connectionString)
        connection.Open()

        InitializeDatabase()
    End Sub

    Private Sub InitializeDatabase()
        Dim createTableSql = "
        CREATE TABLE IF NOT EXISTS Payloads (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Description TEXT,
            Category TEXT NOT NULL,
            LocalPath TEXT NOT NULL,
            TargetPath TEXT NOT NULL,
            FileSize INTEGER DEFAULT 0,
            Version TEXT,
            Author TEXT,
            Added TEXT NOT NULL,
            LastSent TEXT,
            SendCount INTEGER DEFAULT 0,
            IsActive INTEGER DEFAULT 1,
            Tags TEXT
        );

        CREATE TABLE IF NOT EXISTS SendHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PayloadId INTEGER NOT NULL,
            SentDate TEXT NOT NULL,
            TargetHost TEXT NOT NULL,
            Protocol TEXT NOT NULL,
            Success INTEGER NOT NULL,
            ErrorMessage TEXT,
            DurationMs INTEGER,
            FOREIGN KEY (PayloadId) REFERENCES Payloads(Id)
        );

        CREATE TABLE IF NOT EXISTS PayloadProfiles (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Description TEXT,
            PayloadIds TEXT NOT NULL,
            TargetHost TEXT,
            AutoSend INTEGER DEFAULT 0,
            Created TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_payloads_category ON Payloads(Category);
        CREATE INDEX IF NOT EXISTS idx_payloads_active ON Payloads(IsActive);
        CREATE INDEX IF NOT EXISTS idx_history_payload ON SendHistory(PayloadId);
        "

        Using cmd As New SQLiteCommand(createTableSql, connection)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

#Region "Payload Management"

    ''' <summary>
    ''' Add new payload to library
    ''' </summary>
    Public Function AddPayload(payload As PayloadInfo) As Integer
        Dim sql = "INSERT INTO Payloads (Name, Description, Category, LocalPath, TargetPath, FileSize, Version, Author, Added, Tags, IsActive)
                   VALUES (@Name, @Description, @Category, @LocalPath, @TargetPath, @FileSize, @Version, @Author, @Added, @Tags, @IsActive);
                   SELECT last_insert_rowid();"

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@Name", payload.Name)
            cmd.Parameters.AddWithValue("@Description", If(payload.Description, ""))
            cmd.Parameters.AddWithValue("@Category", payload.Category)
            cmd.Parameters.AddWithValue("@LocalPath", payload.LocalPath)
            cmd.Parameters.AddWithValue("@TargetPath", payload.TargetPath)
            cmd.Parameters.AddWithValue("@FileSize", payload.FileSize)
            cmd.Parameters.AddWithValue("@Version", If(payload.Version, ""))
            cmd.Parameters.AddWithValue("@Author", If(payload.Author, ""))
            cmd.Parameters.AddWithValue("@Added", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            cmd.Parameters.AddWithValue("@Tags", JsonConvert.SerializeObject(payload.Tags))
            cmd.Parameters.AddWithValue("@IsActive", If(payload.IsActive, 1, 0))

            Return Convert.ToInt32(cmd.ExecuteScalar())
        End Using
    End Function

    ''' <summary>
    ''' Update existing payload
    ''' </summary>
    Public Sub UpdatePayload(payload As PayloadInfo)
        Dim sql = "UPDATE Payloads SET
                   Name = @Name,
                   Description = @Description,
                   Category = @Category,
                   LocalPath = @LocalPath,
                   TargetPath = @TargetPath,
                   FileSize = @FileSize,
                   Version = @Version,
                   Author = @Author,
                   Tags = @Tags,
                   IsActive = @IsActive
                   WHERE Id = @Id"

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@Id", payload.Id)
            cmd.Parameters.AddWithValue("@Name", payload.Name)
            cmd.Parameters.AddWithValue("@Description", If(payload.Description, ""))
            cmd.Parameters.AddWithValue("@Category", payload.Category)
            cmd.Parameters.AddWithValue("@LocalPath", payload.LocalPath)
            cmd.Parameters.AddWithValue("@TargetPath", payload.TargetPath)
            cmd.Parameters.AddWithValue("@FileSize", payload.FileSize)
            cmd.Parameters.AddWithValue("@Version", If(payload.Version, ""))
            cmd.Parameters.AddWithValue("@Author", If(payload.Author, ""))
            cmd.Parameters.AddWithValue("@Tags", JsonConvert.SerializeObject(payload.Tags))
            cmd.Parameters.AddWithValue("@IsActive", If(payload.IsActive, 1, 0))

            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Delete payload (mark as inactive)
    ''' </summary>
    Public Sub DeletePayload(id As Integer, Optional hardDelete As Boolean = False)
        Dim sql As String
        If hardDelete Then
            sql = "DELETE FROM Payloads WHERE Id = @Id"
        Else
            sql = "UPDATE Payloads SET IsActive = 0 WHERE Id = @Id"
        End If

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@Id", id)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Get all active payloads
    ''' </summary>
    Public Function GetAllPayloads(Optional includeInactive As Boolean = False) As List(Of PayloadInfo)
        Dim sql = "SELECT * FROM Payloads"
        If Not includeInactive Then
            sql &= " WHERE IsActive = 1"
        End If
        sql &= " ORDER BY Name"

        Dim payloads As New List(Of PayloadInfo)

        Using cmd As New SQLiteCommand(sql, connection)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    payloads.Add(ReadPayloadFromReader(reader))
                End While
            End Using
        End Using

        Return payloads
    End Function

    ''' <summary>
    ''' Get payloads by category
    ''' </summary>
    Public Function GetPayloadsByCategory(category As String) As List(Of PayloadInfo)
        Dim sql = "SELECT * FROM Payloads WHERE Category = @Category AND IsActive = 1 ORDER BY Name"

        Dim payloads As New List(Of PayloadInfo)

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@Category", category)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    payloads.Add(ReadPayloadFromReader(reader))
                End While
            End Using
        End Using

        Return payloads
    End Function

    ''' <summary>
    ''' Get payload by ID
    ''' </summary>
    Public Function GetPayloadById(id As Integer) As PayloadInfo
        Dim sql = "SELECT * FROM Payloads WHERE Id = @Id"

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@Id", id)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    Return ReadPayloadFromReader(reader)
                End If
            End Using
        End Using

        Return Nothing
    End Function

    Private Function ReadPayloadFromReader(reader As SQLiteDataReader) As PayloadInfo
        Dim tags As New List(Of String)
        Try
            Dim tagsJson = reader("Tags").ToString()
            If Not String.IsNullOrEmpty(tagsJson) Then
                tags = JsonConvert.DeserializeObject(Of List(Of String))(tagsJson)
            End If
        Catch
            ' Ignore invalid JSON
        End Try

        Return New PayloadInfo With {
            .Id = Convert.ToInt32(reader("Id")),
            .Name = reader("Name").ToString(),
            .Description = reader("Description").ToString(),
            .Category = reader("Category").ToString(),
            .LocalPath = reader("LocalPath").ToString(),
            .TargetPath = reader("TargetPath").ToString(),
            .FileSize = Convert.ToInt64(reader("FileSize")),
            .Version = reader("Version").ToString(),
            .Author = reader("Author").ToString(),
            .Added = DateTime.Parse(reader("Added").ToString()),
            .LastSent = If(IsDBNull(reader("LastSent")), Nothing, DateTime.Parse(reader("LastSent").ToString())),
            .SendCount = Convert.ToInt32(reader("SendCount")),
            .IsActive = Convert.ToInt32(reader("IsActive")) = 1,
            .Tags = tags
        }
    End Function

#End Region

#Region "Send History"

    ''' <summary>
    ''' Record payload send operation
    ''' </summary>
    Public Sub RecordSend(payloadId As Integer, targetHost As String, protocol As String, success As Boolean, Optional errorMessage As String = "", Optional durationMs As Integer = 0)
        Dim sql = "INSERT INTO SendHistory (PayloadId, SentDate, TargetHost, Protocol, Success, ErrorMessage, DurationMs)
                   VALUES (@PayloadId, @SentDate, @TargetHost, @Protocol, @Success, @ErrorMessage, @DurationMs)"

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@PayloadId", payloadId)
            cmd.Parameters.AddWithValue("@SentDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            cmd.Parameters.AddWithValue("@TargetHost", targetHost)
            cmd.Parameters.AddWithValue("@Protocol", protocol)
            cmd.Parameters.AddWithValue("@Success", If(success, 1, 0))
            cmd.Parameters.AddWithValue("@ErrorMessage", If(errorMessage, ""))
            cmd.Parameters.AddWithValue("@DurationMs", durationMs)

            cmd.ExecuteNonQuery()
        End Using

        ' Update payload send stats
        If success Then
            Dim updateSql = "UPDATE Payloads SET LastSent = @LastSent, SendCount = SendCount + 1 WHERE Id = @Id"
            Using cmd As New SQLiteCommand(updateSql, connection)
                cmd.Parameters.AddWithValue("@LastSent", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                cmd.Parameters.AddWithValue("@Id", payloadId)
                cmd.ExecuteNonQuery()
            End Using
        End If
    End Sub

    ''' <summary>
    ''' Get send history for payload
    ''' </summary>
    Public Function GetSendHistory(payloadId As Integer, Optional limit As Integer = 50) As List(Of SendHistoryRecord)
        Dim sql = "SELECT * FROM SendHistory WHERE PayloadId = @PayloadId ORDER BY SentDate DESC LIMIT @Limit"

        Dim history As New List(Of SendHistoryRecord)

        Using cmd As New SQLiteCommand(sql, connection)
            cmd.Parameters.AddWithValue("@PayloadId", payloadId)
            cmd.Parameters.AddWithValue("@Limit", limit)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    history.Add(New SendHistoryRecord With {
                        .Id = Convert.ToInt32(reader("Id")),
                        .PayloadId = Convert.ToInt32(reader("PayloadId")),
                        .SentDate = DateTime.Parse(reader("SentDate").ToString()),
                        .TargetHost = reader("TargetHost").ToString(),
                        .Protocol = reader("Protocol").ToString(),
                        .Success = Convert.ToInt32(reader("Success")) = 1,
                        .ErrorMessage = reader("ErrorMessage").ToString(),
                        .DurationMs = Convert.ToInt32(reader("DurationMs"))
                    })
                End While
            End Using
        End Using

        Return history
    End Function

#End Region

#Region "Statistics"

    ''' <summary>
    ''' Get payload statistics
    ''' </summary>
    Public Function GetStatistics() As Dictionary(Of String, Object)
        Dim stats As New Dictionary(Of String, Object)

        ' Total payloads
        Using cmd As New SQLiteCommand("SELECT COALESCE(COUNT(*), 0) FROM Payloads WHERE IsActive = 1", connection)
            Dim result = cmd.ExecuteScalar()
            stats("TotalPayloads") = If(result IsNot Nothing AndAlso Not IsDBNull(result), Convert.ToInt32(result), 0)
        End Using

        ' Total sends
        Using cmd As New SQLiteCommand("SELECT COALESCE(COUNT(*), 0) FROM SendHistory", connection)
            Dim result = cmd.ExecuteScalar()
            stats("TotalSends") = If(result IsNot Nothing AndAlso Not IsDBNull(result), Convert.ToInt32(result), 0)
        End Using

        ' Success rate
        Using cmd As New SQLiteCommand("SELECT COALESCE(COUNT(*), 0) FROM SendHistory WHERE Success = 1", connection)
            Dim result = cmd.ExecuteScalar()
            Dim successCount = If(result IsNot Nothing AndAlso Not IsDBNull(result), Convert.ToInt32(result), 0)
            Dim totalSends = CInt(stats("TotalSends"))
            stats("SuccessRate") = If(totalSends > 0, (successCount / totalSends) * 100, 0)
        End Using

        ' Most sent payload
        Using cmd As New SQLiteCommand("SELECT Name, SendCount FROM Payloads WHERE IsActive = 1 ORDER BY SendCount DESC LIMIT 1", connection)
            Using reader = cmd.ExecuteReader()
                If reader.Read() AndAlso Not IsDBNull(reader("Name")) Then
                    stats("MostSentPayload") = reader("Name").ToString()
                    stats("MostSentCount") = If(Not IsDBNull(reader("SendCount")), Convert.ToInt32(reader("SendCount")), 0)
                Else
                    stats("MostSentPayload") = "N/A"
                    stats("MostSentCount") = 0
                End If
            End Using
        End Using

        ' Category breakdown
        Using cmd As New SQLiteCommand("SELECT Category, COUNT(*) as Count FROM Payloads WHERE IsActive = 1 GROUP BY Category", connection)
            Dim categoryStats As New Dictionary(Of String, Integer)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    categoryStats(reader("Category").ToString()) = Convert.ToInt32(reader("Count"))
                End While
            End Using
            stats("CategoryBreakdown") = categoryStats
        End Using

        Return stats
    End Function

#End Region

#Region "IDisposable"

    Private disposed As Boolean = False

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposed Then
            If disposing Then
                If connection IsNot Nothing Then
                    connection.Close()
                    connection.Dispose()
                    connection = Nothing
                End If
            End If
            disposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region

End Class

#Region "Data Models"

''' <summary>
''' Payload information model
''' </summary>
Public Class PayloadInfo
    Public Property Id As Integer
    Public Property Name As String
    Public Property Description As String
    Public Property Category As String
    Public Property LocalPath As String
    Public Property TargetPath As String
    Public Property FileSize As Long
    Public Property Version As String
    Public Property Author As String
    Public Property Added As DateTime
    Public Property LastSent As DateTime?
    Public Property SendCount As Integer
    Public Property IsActive As Boolean
    Public Property Tags As List(Of String) = New List(Of String)

    Public ReadOnly Property FileSizeFormatted As String
        Get
            Return FtpManager.FormatFileSize(FileSize)
        End Get
    End Property

    Public ReadOnly Property FileExists As Boolean
        Get
            Return File.Exists(LocalPath)
        End Get
    End Property

End Class

''' <summary>
''' Send history record
''' </summary>
Public Class SendHistoryRecord
    Public Property Id As Integer
    Public Property PayloadId As Integer
    Public Property SentDate As DateTime
    Public Property TargetHost As String
    Public Property Protocol As String
    Public Property Success As Boolean
    Public Property ErrorMessage As String
    Public Property DurationMs As Integer

    Public ReadOnly Property DurationFormatted As String
        Get
            If DurationMs < 1000 Then
                Return $"{DurationMs} ms"
            Else
                Return $"{DurationMs / 1000.0:F2} s"
            End If
        End Get
    End Property

End Class

''' <summary>
''' Payload categories enum
''' </summary>
Public Enum PayloadCategory
    Jailbreak
    Homebrew
    Debug
    Backup
    Custom
End Enum

#End Region