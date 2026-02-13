Imports System.IO

Imports System.Net

Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Threading

''' <summary>
''' Module for downloading PS5 firmware PUP files from Sony CDN.
''' Supports progress tracking, resume capability, SHA256 verification, and cancellation.
''' </summary>
Public Class FirmwareDownloadModule

    ' ===========================

    ' FIRMWARE METADATA DATABASE
    ' ===========================

    ''' <summary>Firmware version metadata</summary>
    Public Class FirmwareInfo
        Public Property Version As Integer
        Public Property VersionString As String
        Public Property BuildDate As String
        Public Property ReleaseDate As String
        Public Property DownloadUrl As String
        Public Property Checksum As String
        Public Property Notes As String
        Public Property ArchiveSHA256 As String      ' SHA256 for archive.midnightchannel.net download
        Public Property ArchiveFilename As String    ' Filename in archive
    End Class

    ''' <summary>Archive base URL for automatic downloads</summary>
    Private Const ArchiveBaseUrl As String = "https://archive.midnightchannel.net/SonyPS/Firmware"

    ''' <summary>Known PS5 firmware versions with metadata and archive download info from Midnight Channel</summary>
    Private Shared ReadOnly FirmwareDatabase As New Dictionary(Of Integer, FirmwareInfo) From {
        {1, New FirmwareInfo With {
            .Version = 1,
            .VersionString = "01.00.00",
            .BuildDate = "2020_0521",
            .ReleaseDate = "November 2020",
            .DownloadUrl = "",
            .Checksum = "",
            .ArchiveSHA256 = "",
            .ArchiveFilename = "",
            .Notes = "Launch firmware - Not available in archive"
        }},
        {2, New FirmwareInfo With {
            .Version = 2,
            .VersionString = "02.20",
            .BuildDate = "2021_0215",
            .ReleaseDate = "March 2021",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/AB1645473541B1938660F43252C88144AE1EEBA643DE16736AD0ED0B1D2BFF62/PS5REC_CRC[832DE527]_PS5UPDATE.PUP",
            .Checksum = "AB1645473541B1938660F43252C88144AE1EEBA643DE16736AD0ED0B1D2BFF62",
            .ArchiveSHA256 = "AB1645473541B1938660F43252C88144AE1EEBA643DE16736AD0ED0B1D2BFF62",
            .ArchiveFilename = "PS5REC_CRC[832DE527]_PS5UPDATE.PUP",
            .Notes = "Early system update (959.54MB)"
        }},
        {3, New FirmwareInfo With {
            .Version = 3,
            .VersionString = "03.00",
            .BuildDate = "2021_0406",
            .ReleaseDate = "April 2021",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/1EE2F8313AFAA731488DCFC0153D20C5F3C66CFD38C5519F02AFBC7F3DD2D8BB/PS5REC_CRC[EFDBE2F2]_PS5UPDATE.PUP",
            .Checksum = "1EE2F8313AFAA731488DCFC0153D20C5F3C66CFD38C5519F02AFBC7F3DD2D8BB",
            .ArchiveSHA256 = "1EE2F8313AFAA731488DCFC0153D20C5F3C66CFD38C5519F02AFBC7F3DD2D8BB",
            .ArchiveFilename = "PS5REC_CRC[EFDBE2F2]_PS5UPDATE.PUP",
            .Notes = "USB storage support (1000.29MB)"
        }},
        {4, New FirmwareInfo With {
            .Version = 4,
            .VersionString = "04.00",
            .BuildDate = "2021_0908",
            .ReleaseDate = "September 2021",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/9E003E8AB7CE8CEF61CA0124E962E5F3C9CD8CD5E2681B0C8E1FBD0EB201A43A/PS5REC_CRC[8F082255]_PS5UPDATE.PUP",
            .Checksum = "9E003E8AB7CE8CEF61CA0124E962E5F3C9CD8CD5E2681B0C8E1FBD0EB201A43A",
            .ArchiveSHA256 = "9E003E8AB7CE8CEF61CA0124E962E5F3C9CD8CD5E2681B0C8E1FBD0EB201A43A",
            .ArchiveFilename = "PS5REC_CRC[8F082255]_PS5UPDATE.PUP",
            .Notes = "Major system update (1011.17MB)"
        }},
        {5, New FirmwareInfo With {
            .Version = 5,
            .VersionString = "05.00",
            .BuildDate = "2022_0309",
            .ReleaseDate = "March 2022",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/028896220519726F78007EF3B9C7CD2E4DF67F87BABE533F61E908569220084F/PS5REC_CRC[6FBCBF0A]_PS5UPDATE.PUP",
            .Checksum = "028896220519726F78007EF3B9C7CD2E4DF67F87BABE533F61E908569220084F",
            .ArchiveSHA256 = "028896220519726F78007EF3B9C7CD2E4DF67F87BABE533F61E908569220084F",
            .ArchiveFilename = "PS5REC_CRC[6FBCBF0A]_PS5UPDATE.PUP",
            .Notes = "Variable refresh rate support (1.12GB)"
        }},
        {6, New FirmwareInfo With {
            .Version = 6,
            .VersionString = "06.00",
            .BuildDate = "2022_0914",
            .ReleaseDate = "September 2022",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/57DBF8D0682930DA5B77096D8F321A55F5F7AABB8ED8FFDF8FEF1FEED6A15DF3/PS5REC_CRC[D6A5579A]_PS5UPDATE.PUP",
            .Checksum = "57DBF8D0682930DA5B77096D8F321A55F5F7AABB8ED8FFDF8FEF1FEED6A15DF3",
            .ArchiveSHA256 = "57DBF8D0682930DA5B77096D8F321A55F5F7AABB8ED8FFDF8FEF1FEED6A15DF3",
            .ArchiveFilename = "PS5REC_CRC[D6A5579A]_PS5UPDATE.PUP",
            .Notes = "1440p HDMI support (1.15GB)"
        }},
        {7, New FirmwareInfo With {
            .Version = 7,
            .VersionString = "07.00",
            .BuildDate = "2023_0308",
            .ReleaseDate = "March 2023",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/C76A98D0421D7487F7FFB6ED865CBB30CD7656F4EB951669BDB365FA4008C99C/PS5REC_CRC[103521FA]_PS5UPDATE.PUP",
            .Checksum = "C76A98D0421D7487F7FFB6ED865CBB30CD7656F4EB951669BDB365FA4008C99C",
            .ArchiveSHA256 = "C76A98D0421D7487F7FFB6ED865CBB30CD7656F4EB951669BDB365FA4008C99C",
            .ArchiveFilename = "PS5REC_CRC[103521FA]_PS5UPDATE.PUP",
            .Notes = "Enhanced features (1.2GB)"
        }},
        {8, New FirmwareInfo With {
            .Version = 8,
            .VersionString = "08.00",
            .BuildDate = "2023_0913",
            .ReleaseDate = "September 2023",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/B9A50DAE3C064E523893FF3C8DB7D43B27366054E8BC9D1E962758B9B1E45B8D/PS5REC_CRC[16FC713C]_PS5UPDATE.PUP",
            .Checksum = "B9A50DAE3C064E523893FF3C8DB7D43B27366054E8BC9D1E962758B9B1E45B8D",
            .ArchiveSHA256 = "B9A50DAE3C064E523893FF3C8DB7D43B27366054E8BC9D1E962758B9B1E45B8D",
            .ArchiveFilename = "PS5REC_CRC[16FC713C]_PS5UPDATE.PUP",
            .Notes = "Discord voice chat integration (1.23GB)"
        }},
        {9, New FirmwareInfo With {
            .Version = 9,
            .VersionString = "09.00",
            .BuildDate = "2024_0313",
            .ReleaseDate = "March 2024",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/9A4BE7BEADB702F5DCF65FD4F360AF985D61B938F8D2C11BF0F689B8868325E6/PS5REC_CRC[2E382FAB]_PS5UPDATE.PUP",
            .Checksum = "9A4BE7BEADB702F5DCF65FD4F360AF985D61B938F8D2C11BF0F689B8868325E6",
            .ArchiveSHA256 = "9A4BE7BEADB702F5DCF65FD4F360AF985D61B938F8D2C11BF0F689B8868325E6",
            .ArchiveFilename = "PS5REC_CRC[2E382FAB]_PS5UPDATE.PUP",
            .Notes = "Adaptive charging features (1.25GB)"
        }},
        {10, New FirmwareInfo With {
            .Version = 10,
            .VersionString = "10.00",
            .BuildDate = "2024_0903",
            .ReleaseDate = "September 2024",
            .DownloadUrl = $"{ArchiveBaseUrl}/download/86D0AEDF9512596BBE30A312165FED4CAAC37109469A63CD99D20514D5FBCB7F/PS5REC_CRC[0D82E943]_PS5UPDATE.PUP",
            .Checksum = "86D0AEDF9512596BBE30A312165FED4CAAC37109469A63CD99D20514D5FBCB7F",
            .ArchiveSHA256 = "86D0AEDF9512596BBE30A312165FED4CAAC37109469A63CD99D20514D5FBCB7F",
            .ArchiveFilename = "PS5REC_CRC[0D82E943]_PS5UPDATE.PUP",
            .Notes = "Latest stable release (1.35GB)"
        }}
    }

    ''' <summary>CDN mirror URLs for firmware downloads</summary>
    Private Shared ReadOnly CdnMirrors As String() = {
        "https://dus01.ps5.update.playstation.net",
        "https://duk01.ps5.update.playstation.net",
        "https://djp01.ps5.update.playstation.net"
    }

    ' ===========================
    ' PROGRESS TRACKING
    ' ===========================

    ''' <summary>Progress information during download</summary>
    Public Class DownloadProgress
        Public Property BytesDownloaded As Long
        Public Property TotalBytes As Long
        Public Property PercentComplete As Integer
        Public Property SpeedBytesPerSecond As Double
        Public Property EstimatedTimeRemaining As TimeSpan
        Public Property IsComplete As Boolean

        Public ReadOnly Property FormattedProgress As String
            Get
                Return $"{PercentComplete}% ({FirmwareManagerService.FormatBytes(BytesDownloaded)} / {FirmwareManagerService.FormatBytes(TotalBytes)})"
            End Get
        End Property

        Public ReadOnly Property FormattedSpeed As String
            Get
                Return FirmwareManagerService.FormatSpeed(SpeedBytesPerSecond)
            End Get
        End Property

        Public ReadOnly Property FormattedETA As String
            Get
                Return FirmwareManagerService.FormatTimeSpan(EstimatedTimeRemaining)
            End Get
        End Property

    End Class

    ' ===========================
    ' DOWNLOAD METHODS
    ' ===========================

    ''' <summary>Download firmware PUP file with progress tracking and resume support</summary>
    Public Shared Async Function DownloadFirmwareAsync(
        version As Integer,
        progressCallback As IProgress(Of DownloadProgress),
        cancellationToken As CancellationToken
    ) As Task(Of (Success As Boolean, ErrorMessage As String))

        Try
            ' Validate version

            Dim firmwareInfo = GetFirmwareInfo(version)
            If firmwareInfo Is Nothing Then

                Return (False, $"Firmware version {version} is not supported")
            End If

            ' Get URL and destination path

            Dim url = GetFirmwareUrl(version)
            If String.IsNullOrEmpty(url) Then
                Return (False, $"Firmware {version} does not have an automatic download URL. Please use Import PUP to manually add this firmware.")
            End If

            Dim cachePath = Constants.FirmwareCacheDirectory
            Dim destinationPath = Path.Combine(cachePath, $"PS5UPDATE_FW{version}.PUP")

            ' Ensure cache directory exists
            If Not Directory.Exists(cachePath) Then
                Directory.CreateDirectory(cachePath)
            End If

            Logger.LogToFile($"Starting download of firmware {version} ({firmwareInfo.VersionString})", LogLevel.Info)
            Logger.LogToFile($"URL: {url}", LogLevel.Debug)

            ' Download with progress and resume
            Dim result = Await DownloadFileWithProgressAsync(
                url,
                destinationPath,
                progressCallback,
                cancellationToken
            )

            If Not result.Success Then
                Return (False, result.ErrorMessage)
            End If

            ' Verify checksum if available

            Dim checksum = GetFirmwareChecksum(version)
            If Not String.IsNullOrEmpty(checksum) Then
                Logger.LogToFile($"Verifying checksum for firmware {version}...", LogLevel.Info)
                Dim isValid = Await VerifyChecksumAsync(destinationPath, checksum, cancellationToken)

                If Not isValid Then
                    File.Delete(destinationPath)
                    Return (False, "Checksum verification failed - file may be corrupted")
                End If
                Logger.LogToFile($"Checksum verification passed for firmware {version}", LogLevel.Info)
            End If

            Logger.LogToFile($"Successfully downloaded firmware {version} ({firmwareInfo.VersionString})", LogLevel.Success)
            Return (True, "")
        Catch ex As OperationCanceledException
            Logger.LogToFile($"Download cancelled for firmware {version}", LogLevel.Warning)
            Return (False, "Download cancelled by user")
        Catch ex As Exception
            Logger.LogToFile($"Error downloading firmware {version}: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message)
        End Try
    End Function

    ''' <summary>Download file with progress tracking and resume capability</summary>
    Private Shared Async Function DownloadFileWithProgressAsync(
        url As String,
        destinationPath As String,
        progressCallback As IProgress(Of DownloadProgress),
        cancellationToken As CancellationToken
    ) As Task(Of (Success As Boolean, ErrorMessage As String))

        Try
            ' Check if partial file exists (resume support)
            Dim startPosition As Long = 0
            If File.Exists(destinationPath) Then
                Dim fileInfo As New FileInfo(destinationPath)
                startPosition = fileInfo.Length
                Logger.LogToFile($"Resuming download from byte {startPosition}", LogLevel.Debug)
            End If

            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromMinutes(30)
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PS5-BACKPORK-KITCHEN/1.0")

                ' Set range header for resume

                If startPosition > 0 Then
                    client.DefaultRequestHeaders.Range = New Headers.RangeHeaderValue(startPosition, Nothing)
                End If

                ' Try to get response, handle 416 error outside Catch block
                Dim response As HttpResponseMessage = Nothing
                Dim needsRetry As Boolean = False

                Try
                    response = Await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    response.EnsureSuccessStatusCode()
                Catch ex As HttpRequestException When ex.StatusCode = HttpStatusCode.RequestedRangeNotSatisfiable
                    ' HTTP 416 - Range not satisfiable, need to retry without range
                    Logger.LogToFile($"Resume failed (416 Range Not Satisfiable), will restart from beginning", LogLevel.Warning)
                    needsRetry = True
                End Try

                ' Handle retry outside Catch block
                If needsRetry Then
                    If File.Exists(destinationPath) Then
                        File.Delete(destinationPath)
                    End If

                    startPosition = 0
                    client.DefaultRequestHeaders.Range = Nothing

                    ' Retry without range header
                    response = Await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    response.EnsureSuccessStatusCode()
                End If

                Using response

                    Dim totalBytes As Long = If(response.Content.Headers.ContentLength, -1L)
                    If startPosition > 0 Then
                        totalBytes += startPosition  ' Add already downloaded bytes
                    End If

                    Dim totalRead As Long = startPosition
                    Dim buffer(81920) As Byte  ' 80KB buffer
                    Dim lastProgressReport = DateTime.Now
                    Dim startTime = DateTime.Now
                    Dim lastBytesRead As Long = totalRead

                    ' Open file in append mode if resuming, create new otherwise
                    Dim fileMode As FileMode = If(startPosition > 0, FileMode.Append, FileMode.Create)

                    Using contentStream = Await response.Content.ReadAsStreamAsync(),
                          fileStream As New FileStream(destinationPath, fileMode, FileAccess.Write, FileShare.None)

                        While True
                            cancellationToken.ThrowIfCancellationRequested()

                            Dim bytesRead = Await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                            If bytesRead = 0 Then Exit While

                            Await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken)
                            totalRead += bytesRead

                            ' Report progress every 500ms
                            If (DateTime.Now - lastProgressReport).TotalMilliseconds >= 500 Then
                                Dim elapsedSeconds = (DateTime.Now - startTime).TotalSeconds
                                Dim speed = If(elapsedSeconds > 0, (totalRead - startPosition) / elapsedSeconds, 0)
                                Dim remainingBytes = totalBytes - totalRead
                                Dim eta = If(speed > 0, TimeSpan.FromSeconds(remainingBytes / speed), TimeSpan.Zero)

                                Dim progress As New DownloadProgress With {
                                    .BytesDownloaded = totalRead,
                                    .TotalBytes = totalBytes,
                                    .PercentComplete = If(totalBytes > 0, CInt((totalRead * 100L) / totalBytes), 0),
                                    .SpeedBytesPerSecond = speed,
                                    .EstimatedTimeRemaining = eta,
                                    .IsComplete = False
                                }

                                progressCallback?.Report(progress)
                                lastProgressReport = DateTime.Now
                            End If
                        End While

                        ' Report completion
                        Dim finalProgress As New DownloadProgress With {
                            .BytesDownloaded = totalRead,
                            .TotalBytes = totalBytes,
                            .PercentComplete = 100,
                            .SpeedBytesPerSecond = 0,
                            .EstimatedTimeRemaining = TimeSpan.Zero,
                            .IsComplete = True
                        }
                        progressCallback?.Report(finalProgress)

                    End Using
                End Using
            End Using

            Return (True, "")
        Catch ex As OperationCanceledException
            Throw  ' Re-throw cancellation to be handled by caller
        Catch ex As Exception
            Logger.LogToFile($"Download error: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message)
        End Try
    End Function

    ' ===========================
    ' CHECKSUM VERIFICATION
    ' ===========================

    ''' <summary>Verify SHA256 checksum of downloaded file</summary>
    Private Shared Async Function VerifyChecksumAsync(
        filePath As String,
        expectedChecksum As String,
        cancellationToken As CancellationToken
    ) As Task(Of Boolean)

        Try
            Using sha256 As SHA256 = SHA256.Create()
                Using fileStream As FileStream = File.OpenRead(filePath)
                    Dim hashBytes As Byte() = Await Task.Run(Function() sha256.ComputeHash(fileStream), cancellationToken)
                    Dim actualChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
                    Dim expectedChecksumLower = expectedChecksum.ToLowerInvariant()

                    Logger.LogToFile($"Checksum - Expected: {expectedChecksumLower}", LogLevel.Debug)
                    Logger.LogToFile($"Checksum - Actual:   {actualChecksum}", LogLevel.Debug)

                    Return actualChecksum = expectedChecksumLower
                End Using
            End Using
        Catch ex As Exception
            Logger.LogToFile($"Error verifying checksum: {ex.Message}", LogLevel.Error)
            Return False
        End Try
    End Function

    ' ===========================
    ' UTILITIES
    ' ===========================

    ''' <summary>Get firmware metadata</summary>
    Public Shared Function GetFirmwareInfo(version As Integer) As FirmwareInfo
        If FirmwareDatabase.ContainsKey(version) Then
            Return FirmwareDatabase(version)

        End If
        Return Nothing
    End Function

    ''' <summary>Get download URL for firmware version</summary>
    Public Shared Function GetFirmwareUrl(version As Integer) As String
        Dim info = GetFirmwareInfo(version)
        If info IsNot Nothing AndAlso Not String.IsNullOrEmpty(info.DownloadUrl) Then
            Return info.DownloadUrl
        End If
        Return ""  ' No automatic download available
    End Function

    ''' <summary>Check if automatic download is available for this firmware version</summary>
    Public Shared Function IsDownloadAvailable(version As Integer) As Boolean
        Dim info = GetFirmwareInfo(version)
        Return info IsNot Nothing AndAlso Not String.IsNullOrEmpty(info.DownloadUrl)
    End Function

    ''' <summary>Import manually downloaded PUP file into cache</summary>
    Public Shared Function ImportPupFile(sourcePupPath As String, version As Integer) As (Success As Boolean, ErrorMessage As String)
        Try
            If Not File.Exists(sourcePupPath) Then
                Return (False, "Source PUP file not found")
            End If

            ' Validate it's a PUP file
            Dim fileInfo As New FileInfo(sourcePupPath)
            If fileInfo.Length < 100 * 1024 * 1024 Then ' Less than 100MB
                Return (False, "File too small to be a valid firmware PUP")
            End If

            Dim extension = Path.GetExtension(sourcePupPath).ToLower()
            If extension <> ".pup" Then
                Return (False, "File must have .PUP extension")
            End If

            ' Copy to cache directory
            Dim cachePath = Constants.FirmwareCacheDirectory
            If Not Directory.Exists(cachePath) Then
                Directory.CreateDirectory(cachePath)
            End If

            Dim destinationPath = Path.Combine(cachePath, $"PS5UPDATE_FW{version}.PUP")

            ' Copy file
            File.Copy(sourcePupPath, destinationPath, True)

            Logger.LogToFile($"Imported PUP file for firmware {version}: {fileInfo.Length} bytes", LogLevel.Success)
            Return (True, "")
        Catch ex As Exception
            Logger.LogToFile($"Error importing PUP file: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message)
        End Try
    End Function

    ''' <summary>Get expected checksum for firmware version</summary>
    Public Shared Function GetFirmwareChecksum(version As Integer) As String
        Dim info = GetFirmwareInfo(version)
        If info IsNot Nothing Then
            Return info.Checksum
        End If
        Return ""
    End Function

    ''' <summary>Get firmware version string</summary>
    Public Shared Function GetFirmwareVersionString(version As Integer) As String
        Dim info = GetFirmwareInfo(version)
        If info IsNot Nothing Then
            Return info.VersionString
        End If
        Return $"{version}.00.00"
    End Function

    ''' <summary>Get firmware build date</summary>
    Public Shared Function GetFirmwareBuildDate(version As Integer) As String
        Dim info = GetFirmwareInfo(version)
        If info IsNot Nothing Then
            Return info.BuildDate
        End If
        Return ""
    End Function

    ''' <summary>Get firmware release notes</summary>
    Public Shared Function GetFirmwareNotes(version As Integer) As String
        Dim info = GetFirmwareInfo(version)
        If info IsNot Nothing Then
            Return info.Notes
        End If
        Return ""
    End Function

    ''' <summary>Get all available firmware versions</summary>
    Public Shared Function GetAllFirmwareVersions() As List(Of Integer)
        Return FirmwareDatabase.Keys.OrderBy(Function(v) v).ToList()

    End Function

    ''' <summary>Get cached PUP file path for firmware version</summary>
    Public Shared Function GetCachedPupPath(version As Integer) As String
        Return Path.Combine(Constants.FirmwareCacheDirectory, $"PS5UPDATE_FW{version}.PUP")
    End Function

    ''' <summary>Check if firmware PUP is already downloaded</summary>
    Public Shared Function IsFirmwareDownloaded(version As Integer) As Boolean
        Dim pupPath = GetCachedPupPath(version)
        Return File.Exists(pupPath)
    End Function

    ''' <summary>Delete cached firmware PUP file</summary>
    Public Shared Function DeleteCachedFirmware(version As Integer) As Boolean
        Try
            Dim pupPath = GetCachedPupPath(version)
            If File.Exists(pupPath) Then
                File.Delete(pupPath)
                Logger.LogToFile($"Deleted cached firmware {version}", LogLevel.Info)
                Return True
            End If
            Return False
        Catch ex As Exception
            Logger.LogToFile($"Error deleting cached firmware {version}: {ex.Message}", LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>Try multiple CDN mirrors for download</summary>
    Private Shared Function GetCdnMirrorUrls(buildDate As String, hash As String) As List(Of String)
        Dim urls As New List(Of String)

        ' Try all known CDN mirrors
        For Each mirror In CdnMirrors
            Dim url = $"{mirror}/update/ps5/official/tJMRE80IbXnE9YuG0jzTXgKEjIMoabr6/image/{buildDate}/sys_{hash}/PS5UPDATE.PUP"
            urls.Add(url)
        Next

        Return urls
    End Function

End Class