Imports System.IO
Imports System.Threading.Tasks
Imports FluentFTP
Imports Newtonsoft.Json

''' <summary>
''' FTP Manager for PS5 connection and file transfer operations
''' </summary>
Public Class FtpManager

    Private Shared client As AsyncFtpClient = Nothing
    Private Shared currentProfile As FtpProfile = Nothing

#Region "Data Models"

    ''' <summary>
    ''' FTP Connection Profile
    ''' </summary>
    Public Class FtpProfile
        Public Property Name As String
        Public Property Host As String
        Public Property Port As Integer = 2121
        Public Property tcpPort As Integer = 9021
        Public Property Username As String = "anonymous"
        Public Property Password As String = ""
        Public Property UsePassiveMode As Boolean = True
        Public Property Timeout As Integer = 30
        Public Property IsDefault As Boolean = False
    End Class

    ''' <summary>
    ''' Transfer progress information
    ''' </summary>
    Public Class TransferProgress
        Public Property FileName As String
        Public Property TotalBytes As Long
        Public Property TransferredBytes As Long
        Public Property PercentComplete As Double
        Public Property SpeedBytesPerSecond As Double
        Public Property EstimatedTimeRemaining As TimeSpan
        Public Property Status As TransferStatus
    End Class

    Public Enum TransferStatus
        Idle = 0
        Connecting = 1
        Transferring = 2
        Completed = 3
        Failed = 4
        Cancelled = 5
    End Enum

    ''' <summary>
    ''' Remote file/directory information
    ''' </summary>
    Public Class RemoteFileInfo
        Public Property Name As String
        Public Property FullPath As String
        Public Property Size As Long
        Public Property ModifiedDate As DateTime
        Public Property IsDirectory As Boolean
        Public Property Type As String
    End Class

#End Region

#Region "Connection Management"

    ''' <summary>
    ''' Check if connected to FTP server
    ''' </summary>
    Public Shared ReadOnly Property IsConnected As Boolean
        Get
            Return client IsNot Nothing AndAlso client.IsConnected
        End Get
    End Property

    ''' <summary>
    ''' Get current connection profile
    ''' </summary>
    Public Shared ReadOnly Property ActiveProfile As FtpProfile
        Get
            Return currentProfile
        End Get
    End Property

    ''' <summary>
    ''' Connect to FTP server using profile
    ''' </summary>
    Public Shared Async Function ConnectAsync(profile As FtpProfile) As Task(Of Boolean)
        Try
            ' Disconnect if already connected
            If IsConnected Then
                Await DisconnectAsync()
            End If

            ' Create new client
            client = New AsyncFtpClient(
                profile.Host,
                profile.Username,
                profile.Password,
                profile.Port
            )

            ' Configure client
            client.Config.ConnectTimeout = profile.Timeout * 1000
            client.Config.DataConnectionType = If(profile.UsePassiveMode, FtpDataConnectionType.PASV, FtpDataConnectionType.PORT)
            client.Config.EncryptionMode = FtpEncryptionMode.None ' PS5 doesn't use SSL/TLS
            client.Config.ValidateAnyCertificate = True

            ' Connect
            Await client.Connect()

            ' Store current profile
            currentProfile = profile

            Return True
        Catch ex As Exception
            client = Nothing
            currentProfile = Nothing
            Throw New Exception($"Failed to connect to FTP server: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Disconnect from FTP server
    ''' </summary>
    Public Shared Async Function DisconnectAsync() As Task
        Try
            If client IsNot Nothing Then
                If client.IsConnected Then
                    Await client.Disconnect()
                End If
                client.Dispose()
                client = Nothing
            End If
            currentProfile = Nothing
        Catch ex As Exception
            ' Silent fail on disconnect
            client = Nothing
            currentProfile = Nothing
        End Try
    End Function

    ''' <summary>
    ''' Test connection to FTP server
    ''' </summary>
    Public Shared Async Function TestConnectionAsync(profile As FtpProfile) As Task(Of String)
        Dim testClient As AsyncFtpClient = Nothing
        Try
            testClient = New AsyncFtpClient(
                profile.Host,
                profile.Username,
                profile.Password,
                profile.Port
            )

            testClient.Config.ConnectTimeout = profile.Timeout * 1000
            testClient.Config.DataConnectionType = If(profile.UsePassiveMode, FtpDataConnectionType.PASV, FtpDataConnectionType.PORT)
            testClient.Config.EncryptionMode = FtpEncryptionMode.None

            Await testClient.Connect()

            Dim workingDir = Await testClient.GetWorkingDirectory()

            Await testClient.Disconnect()
            testClient.Dispose()

            Return $"✓ Connection successful! Working directory: {workingDir}"
        Catch ex As Exception
            Return $"✗ Connection failed: {ex.Message}"
        Finally
            If testClient IsNot Nothing Then
                testClient.Dispose()
            End If
        End Try
    End Function

#End Region

#Region "Directory Operations"

    ''' <summary>
    ''' List files and directories in remote path
    ''' </summary>
    Public Shared Async Function ListDirectoryAsync(remotePath As String) As Task(Of List(Of RemoteFileInfo))
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Try
            Dim items = Await client.GetListing(remotePath)
            Dim result As New List(Of RemoteFileInfo)

            For Each item In items
                ' Skip parent directory entry
                If item.Name = ".." OrElse item.Name = "." Then
                    Continue For
                End If

                result.Add(New RemoteFileInfo With {
                    .Name = item.Name,
                    .FullPath = item.FullName,
                    .Size = item.Size,
                    .ModifiedDate = item.Modified,
                    .IsDirectory = item.Type = FtpObjectType.Directory,
                    .Type = If(item.Type = FtpObjectType.Directory, "Folder", GetFileTypeDescription(item.Name))
                })
            Next

            Return result
        Catch ex As Exception
            Throw New Exception($"Failed to list directory: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Get current working directory
    ''' </summary>
    Public Shared Async Function GetWorkingDirectoryAsync() As Task(Of String)
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Return Await client.GetWorkingDirectory()
    End Function

    ''' <summary>
    ''' Change working directory
    ''' </summary>
    Public Shared Async Function ChangeDirectoryAsync(remotePath As String) As Task
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Await client.SetWorkingDirectory(remotePath)
    End Function

    ''' <summary>
    ''' Check if directory exists
    ''' </summary>
    Public Shared Async Function DirectoryExistsAsync(remotePath As String) As Task(Of Boolean)
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Return Await client.DirectoryExists(remotePath)
    End Function

#End Region

#Region "File Transfer Operations"

    ''' <summary>
    ''' Download file from PS5 to PC
    ''' </summary>
    Public Shared Async Function DownloadFileAsync(remotePath As String, localPath As String, Optional progress As IProgress(Of FtpProgress) = Nothing) As Task(Of Boolean)
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Try
            ' Ensure local directory exists
            Dim localDir = Path.GetDirectoryName(localPath)
            If Not Directory.Exists(localDir) Then
                Directory.CreateDirectory(localDir)
            End If

            ' Download file
            Dim status = Await client.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, FtpVerify.Retry, progress)

            Return status = FtpStatus.Success
        Catch ex As Exception
            Throw New Exception($"Failed to download file: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Download directory recursively from PS5 to PC
    ''' </summary>
    Public Shared Async Function DownloadDirectoryAsync(remotePath As String, localPath As String, Optional progress As IProgress(Of FtpProgress) = Nothing) As Task(Of Integer)
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Try
            ' Ensure local directory exists
            If Not Directory.Exists(localPath) Then
                Directory.CreateDirectory(localPath)
            End If

            ' Download directory
            Dim results = Await client.DownloadDirectory(localPath, remotePath, FtpFolderSyncMode.Update, FtpLocalExists.Overwrite, FtpVerify.Retry, Nothing, progress)

            ' Count successful downloads
            Dim successCount = results.Where(Function(r) r.IsSuccess).Count()

            Return successCount
        Catch ex As Exception
            Throw New Exception($"Failed to download directory: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Upload file from PC to PS5
    ''' </summary>
    Public Shared Async Function UploadFileAsync(localPath As String, remotePath As String, Optional progress As IProgress(Of FtpProgress) = Nothing) As Task(Of Boolean)
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Try
            ' Check if local file exists
            If Not File.Exists(localPath) Then
                Throw New Exception($"Local file not found: {localPath}")
            End If

            ' Upload file
            Dim status = Await client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, False, FtpVerify.Retry, progress)

            Return status = FtpStatus.Success
        Catch ex As Exception
            Throw New Exception($"Failed to upload file: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Upload directory recursively from PC to PS5
    ''' </summary>
    Public Shared Async Function UploadDirectoryAsync(localPath As String, remotePath As String, Optional progress As IProgress(Of FtpProgress) = Nothing) As Task(Of Integer)
        If Not IsConnected Then
            Throw New Exception("Not connected to FTP server")
        End If

        Try
            ' Check if local directory exists
            If Not Directory.Exists(localPath) Then
                Throw New Exception($"Local directory not found: {localPath}")
            End If

            ' Upload directory
            Dim results = Await client.UploadDirectory(localPath, remotePath, FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite, FtpVerify.Retry, Nothing, progress)

            ' Count successful uploads
            Dim successCount = results.Where(Function(r) r.IsSuccess).Count()

            Return successCount
        Catch ex As Exception
            Throw New Exception($"Failed to upload directory: {ex.Message}", ex)
        End Try
    End Function

#End Region

#Region "Profile Management"

    Private Shared ReadOnly ProfilesPath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PS5BackporkKitchen",
        "ftp_profiles.json"
    )

    ''' <summary>
    ''' Save FTP profiles to disk
    ''' </summary>
    Public Shared Sub SaveProfiles(profiles As List(Of FtpProfile))
        Try
            Dim dir = Path.GetDirectoryName(ProfilesPath)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim json = JsonConvert.SerializeObject(profiles, Formatting.Indented)
            File.WriteAllText(ProfilesPath, json)
        Catch ex As Exception
            Throw New Exception($"Failed to save profiles: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Load FTP profiles from disk
    ''' </summary>
    Public Shared Function LoadProfiles() As List(Of FtpProfile)
        Try
            If Not File.Exists(ProfilesPath) Then
                Return New List(Of FtpProfile)()
            End If

            Dim json = File.ReadAllText(ProfilesPath)
            Dim profiles = JsonConvert.DeserializeObject(Of List(Of FtpProfile))(json)

            Return If(profiles, New List(Of FtpProfile)())
        Catch ex As Exception
            Return New List(Of FtpProfile)()
        End Try
    End Function

    ''' <summary>
    ''' Get default profile
    ''' </summary>
    Public Shared Function GetDefaultProfile() As FtpProfile
        Dim profiles = LoadProfiles()
        Return profiles.FirstOrDefault(Function(p) p.IsDefault)
    End Function

#End Region

#Region "Helper Methods"

    ''' <summary>
    ''' Get file type description from extension
    ''' </summary>
    Private Shared Function GetFileTypeDescription(fileName As String) As String
        Dim ext = Path.GetExtension(fileName).ToLower()

        Select Case ext
            Case ".prx", ".sprx"
                Return "PS5 Library"
            Case ".elf"
                Return "ELF Executable"
            Case ".sfo"
                Return "System File"
            Case ".json"
                Return "JSON File"
            Case ".txt"
                Return "Text File"
            Case ".bin"
                Return "Binary File"
            Case ".dat"
                Return "Data File"
            Case Else
                Return "File"
        End Select
    End Function

    ''' <summary>
    ''' Format file size for display
    ''' </summary>
    Public Shared Function FormatFileSize(bytes As Long) As String
        Dim suffixes() As String = {"B", "KB", "MB", "GB", "TB"}
        Dim counter As Integer = 0
        Dim number As Decimal = bytes

        While number >= 1024 AndAlso counter < suffixes.Length - 1
            number /= 1024
            counter += 1
        End While

        Return $"{number:N2} {suffixes(counter)}"
    End Function

#End Region

End Class