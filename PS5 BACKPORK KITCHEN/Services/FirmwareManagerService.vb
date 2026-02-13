Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization

''' <summary>
''' Main orchestration service for firmware management operations.
''' Handles metadata storage, firmware detection, state tracking, and coordination of download/extraction/processing.
''' </summary>
Public Class FirmwareManagerService

    ' ===========================
    ' ENUMS
    ' ===========================

    ''' <summary>Firmware processing status</summary>
    Public Enum FirmwareStatus
        NotFound
        Downloading
        Downloaded
        Extracting
        Extracted
        Processing
        Processed
        Verified
        [Error]
    End Enum

    ' ===========================
    ' METADATA CLASSES
    ' ===========================

    ''' <summary>Metadata for a single firmware version</summary>
    Public Class FirmwareMetadata

        <JsonPropertyName("version")>
        Public Property Version As Integer

        <JsonPropertyName("status")>
        Public Property Status As FirmwareStatus

        <JsonPropertyName("downloadUrl")>
        Public Property DownloadUrl As String

        <JsonPropertyName("sha256")>
        Public Property Sha256 As String

        <JsonPropertyName("downloadedDate")>
        Public Property DownloadedDate As DateTime?

        <JsonPropertyName("extractedDate")>
        Public Property ExtractedDate As DateTime?

        <JsonPropertyName("processedDate")>
        Public Property ProcessedDate As DateTime?

        <JsonPropertyName("libraryCount")>
        Public Property LibraryCount As Integer

        <JsonPropertyName("sizeBytes")>
        Public Property SizeBytes As Long

        <JsonPropertyName("errorMessage")>
        Public Property ErrorMessage As String

        Public Sub New()
            Status = FirmwareStatus.NotFound
        End Sub

    End Class

    ''' <summary>Root metadata container</summary>
    Public Class FirmwareMetadataContainer

        <JsonPropertyName("firmwares")>
        Public Property Firmwares As Dictionary(Of Integer, FirmwareMetadata)

        <JsonPropertyName("lastUpdated")>
        Public Property LastUpdated As DateTime

        Public Sub New()
            Firmwares = New Dictionary(Of Integer, FirmwareMetadata)()
            LastUpdated = DateTime.Now
        End Sub

    End Class

    ' ===========================
    ' PROPERTIES
    ' ===========================

    Private _metadata As FirmwareMetadataContainer
    Private _metadataPath As String

    ' ===========================
    ' CONSTRUCTOR
    ' ===========================

    Public Sub New()
        _metadataPath = Constants.FirmwareMetadataPath
        LoadMetadata()
        EnsureDirectoriesExist()
    End Sub

    ' ===========================
    ' METADATA MANAGEMENT
    ' ===========================

    ''' <summary>Load metadata from JSON file or create new if not exists</summary>
    Private Sub LoadMetadata()
        Try
            If File.Exists(_metadataPath) Then
                Dim json = File.ReadAllText(_metadataPath)
                _metadata = JsonSerializer.Deserialize(Of FirmwareMetadataContainer)(json)
                Logger.LogToFile($"Loaded firmware metadata: {_metadata.Firmwares.Count} entries", LogLevel.Debug)
            Else
                _metadata = New FirmwareMetadataContainer()
                Logger.LogToFile("Created new firmware metadata container", LogLevel.Debug)
            End If

            ' Initialize metadata for all supported versions
            For Each version In Constants.SupportedFirmwareVersions
                If Not _metadata.Firmwares.ContainsKey(version) Then
                    _metadata.Firmwares(version) = New FirmwareMetadata With {.Version = version}
                End If
            Next

            ' Detect existing installations
            DetectExistingFirmwares()
            SaveMetadata()
        Catch ex As Exception
            Logger.LogToFile($"Error loading firmware metadata: {ex.Message}", LogLevel.Error)
            _metadata = New FirmwareMetadataContainer()
        End Try
    End Sub

    ''' <summary>Save metadata to JSON file</summary>
    Private Sub SaveMetadata()
        Try
            _metadata.LastUpdated = DateTime.Now
            Dim options = New JsonSerializerOptions With {
                .WriteIndented = True,
                .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }
            Dim json = JsonSerializer.Serialize(_metadata, options)
            File.WriteAllText(_metadataPath, json)
            Logger.LogToFile("Saved firmware metadata", LogLevel.Debug)
        Catch ex As Exception
            Logger.LogToFile($"Error saving firmware metadata: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    ''' <summary>Update metadata for specific firmware version</summary>
    Public Sub UpdateFirmwareMetadata(version As Integer, updateAction As Action(Of FirmwareMetadata))
        If _metadata.Firmwares.ContainsKey(version) Then
            updateAction(_metadata.Firmwares(version))
            SaveMetadata()
        End If
    End Sub

    ' ===========================
    ' FIRMWARE DETECTION
    ' ===========================

    ''' <summary>Detect existing firmware installations and update metadata</summary>
    Private Sub DetectExistingFirmwares()
        For Each version In Constants.SupportedFirmwareVersions
            Dim fakelibDir = Constants.GetFakelibDirectory(version)

            If Directory.Exists(fakelibDir) Then
                Dim libFiles = Directory.GetFiles(fakelibDir, "*.sprx", SearchOption.TopDirectoryOnly)
                Dim metadata = _metadata.Firmwares(version)

                If libFiles.Length > 0 Then
                    ' Firmware exists and has libraries
                    metadata.Status = FirmwareStatus.Processed
                    metadata.LibraryCount = libFiles.Length
                    metadata.SizeBytes = CalculateDirectorySize(fakelibDir)

                    If Not metadata.ProcessedDate.HasValue Then
                        metadata.ProcessedDate = Directory.GetLastWriteTime(fakelibDir)
                    End If

                    Logger.LogToFile($"Detected FW{version}: {libFiles.Length} libraries, {FormatBytes(metadata.SizeBytes)}", LogLevel.Info)
                End If
            End If
        Next
    End Sub

    ''' <summary>Calculate total size of directory</summary>
    Private Function CalculateDirectorySize(directoryPath As String) As Long
        Try
            Dim dirInfo As New DirectoryInfo(directoryPath)
            Return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(Function(file) file.Length)
        Catch
            Return 0
        End Try
    End Function

    ' ===========================
    ' DIRECTORY MANAGEMENT
    ' ===========================

    ''' <summary>Ensure all required directories exist</summary>
    Private Sub EnsureDirectoriesExist()
        Try
            ' Create tools directory
            If Not Directory.Exists(Constants.ToolsDirectory) Then
                Directory.CreateDirectory(Constants.ToolsDirectory)
            End If

            ' Create firmware cache directory
            If Not Directory.Exists(Constants.FirmwareCacheDirectory) Then
                Directory.CreateDirectory(Constants.FirmwareCacheDirectory)
            End If

            ' Create fakelib directories for all versions
            For Each version In Constants.SupportedFirmwareVersions
                Dim fakelibDir = Constants.GetFakelibDirectory(version)
                If Not Directory.Exists(fakelibDir) Then
                    Directory.CreateDirectory(fakelibDir)
                End If
            Next

            Logger.LogToFile("Firmware directories initialized", LogLevel.Debug)
        Catch ex As Exception
            Logger.LogToFile($"Error creating firmware directories: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    ''' <summary>Clear firmware cache directory</summary>

    ''' <summary>Delete specific firmware version</summary>
    Public Function DeleteFirmware(version As Integer) As Boolean
        Try
            Dim fakelibDir = Constants.GetFakelibDirectory(version)
            If Directory.Exists(fakelibDir) Then
                Directory.Delete(fakelibDir, True)
                Directory.CreateDirectory(fakelibDir)
            End If

            ' Reset metadata
            UpdateFirmwareMetadata(version, Sub(meta)
                                                meta.Status = FirmwareStatus.NotFound
                                                meta.LibraryCount = 0
                                                meta.SizeBytes = 0
                                                meta.DownloadedDate = Nothing
                                                meta.ExtractedDate = Nothing
                                                meta.ProcessedDate = Nothing
                                                meta.ErrorMessage = Nothing
                                            End Sub)

            Logger.LogToFile($"Deleted firmware {version}", LogLevel.Info)
            Return True
        Catch ex As Exception
            Logger.LogToFile($"Error deleting firmware {version}: {ex.Message}", LogLevel.Error)
            Return False
        End Try
    End Function

    ' ===========================
    ' CACHE MANAGEMENT
    ' ===========================

    ''' <summary>Clear temporary cache files (PUP downloads and extraction temps)</summary>
    Public Function ClearCache() As (Success As Boolean, BytesFreed As Long, ErrorMessage As String)
        Try
            Dim bytesFreed As Long = 0
            Dim cacheDir = Constants.FirmwareCacheDirectory

            If Not Directory.Exists(cacheDir) Then
                Return (True, 0, "")
            End If

            ' Delete PUP files
            Dim pupFiles = Directory.GetFiles(cacheDir, "*.PUP", SearchOption.TopDirectoryOnly)
            For Each pupFile In pupFiles
                Try
                    Dim fileInfo As New FileInfo(pupFile)
                    bytesFreed += fileInfo.Length
                    File.Delete(pupFile)
                    Logger.LogToFile($"Deleted cached PUP: {Path.GetFileName(pupFile)}", LogLevel.Debug)
                Catch ex As Exception
                    Logger.LogToFile($"Error deleting PUP {Path.GetFileName(pupFile)}: {ex.Message}", LogLevel.Warning)
                End Try
            Next

            ' Delete temporary extraction directories
            Dim tempDirs = Directory.GetDirectories(cacheDir, "extract_fw*_temp", SearchOption.TopDirectoryOnly)
            For Each tempDir In tempDirs
                Try
                    Dim dirInfo As New DirectoryInfo(tempDir)
                    bytesFreed += GetDirectorySize(dirInfo)
                    Directory.Delete(tempDir, True)
                    Logger.LogToFile($"Deleted temp extraction dir: {Path.GetFileName(tempDir)}", LogLevel.Debug)
                Catch ex As Exception
                    Logger.LogToFile($"Error deleting temp dir {Path.GetFileName(tempDir)}: {ex.Message}", LogLevel.Warning)
                End Try
            Next

            Logger.LogToFile($"Cache cleared - freed {FormatBytes(bytesFreed)}", LogLevel.Success)
            Return (True, bytesFreed, "")
        Catch ex As Exception
            Logger.LogToFile($"Error clearing cache: {ex.Message}", LogLevel.Error)
            Return (False, 0, ex.Message)
        End Try
    End Function

    ''' <summary>Delete old firmware versions keeping only the most recent N versions</summary>
    Public Function CleanupOldFirmwares(keepMostRecent As Integer) As (Success As Boolean, DeletedCount As Integer, BytesFreed As Long)
        Try
            ' Get all firmwares sorted by version (descending)
            Dim sortedFirmwares = _metadata.Firmwares.Values _
                .Where(Function(meta) meta.Status = FirmwareStatus.Processed OrElse meta.Status = FirmwareStatus.Verified) _
                .OrderByDescending(Function(meta) meta.Version) _
                .ToList()

            If sortedFirmwares.Count <= keepMostRecent Then
                Logger.LogToFile($"No old firmwares to cleanup ({sortedFirmwares.Count} <= {keepMostRecent})", LogLevel.Info)
                Return (True, 0, 0)
            End If

            ' Delete old versions beyond keepMostRecent
            Dim toDelete = sortedFirmwares.Skip(keepMostRecent).ToList()
            Dim deletedCount As Integer = 0
            Dim bytesFreed As Long = 0

            For Each meta In toDelete
                Dim sizeBeforeDelete = meta.SizeBytes
                If DeleteFirmware(meta.Version) Then
                    deletedCount += 1
                    bytesFreed += sizeBeforeDelete
                End If
            Next

            Logger.LogToFile($"Cleanup complete - deleted {deletedCount} old firmware versions, freed {FormatBytes(bytesFreed)}", LogLevel.Success)
            Return (True, deletedCount, bytesFreed)
        Catch ex As Exception
            Logger.LogToFile($"Error during cleanup: {ex.Message}", LogLevel.Error)
            Return (False, 0, 0)
        End Try
    End Function

    ''' <summary>Calculate directory size recursively</summary>
    Private Function GetDirectorySize(dirInfo As DirectoryInfo) As Long
        Try
            Dim size As Long = 0

            ' Add file sizes
            For Each fileInfo In dirInfo.GetFiles()
                size += fileInfo.Length
            Next

            ' Add subdirectory sizes
            For Each subDirInfo In dirInfo.GetDirectories()
                size += GetDirectorySize(subDirInfo)
            Next

            Return size
        Catch
            Return 0
        End Try
    End Function

    ' ===========================
    ' GETTERS
    ' ===========================

    ''' <summary>Get metadata for specific firmware version</summary>
    Public Function GetFirmwareMetadata(version As Integer) As FirmwareMetadata
        If _metadata.Firmwares.ContainsKey(version) Then
            Return _metadata.Firmwares(version)
        End If
        Return Nothing
    End Function

    ''' <summary>Get all firmware metadata</summary>
    Public Function GetAllFirmwareMetadata() As Dictionary(Of Integer, FirmwareMetadata)
        Return _metadata.Firmwares
    End Function

    ''' <summary>Get total storage used by all firmwares</summary>
    Public Function GetTotalStorageUsed() As Long
        Return _metadata.Firmwares.Values.Sum(Function(meta) meta.SizeBytes)
    End Function

    ' ===========================
    ' STORAGE QUOTA MANAGEMENT
    ' ===========================

    ''' <summary>Storage quota levels in bytes</summary>
    Public Const StorageQuotaWarning As Long = 8L * 1024 * 1024 * 1024    ' 8 GB

    Public Const StorageQuotaCritical As Long = 10L * 1024 * 1024 * 1024  ' 10 GB

    ''' <summary>Storage status levels</summary>
    Public Enum StorageStatus
        Normal
        Warning
        Critical
        OverQuota
    End Enum

    ''' <summary>Check current storage status against quota</summary>
    Public Function GetStorageStatus() As StorageStatus
        Dim used = GetTotalStorageUsed()

        If used >= StorageQuotaCritical Then
            Return StorageStatus.OverQuota
        ElseIf used >= StorageQuotaWarning Then
            Return StorageStatus.Critical
        ElseIf used >= (StorageQuotaWarning * 0.75) Then
            Return StorageStatus.Warning
        Else
            Return StorageStatus.Normal
        End If
    End Function

    ''' <summary>Get storage usage percentage</summary>
    Public Function GetStorageUsagePercent() As Integer
        Dim used = GetTotalStorageUsed()
        Return CInt((used * 100.0) / StorageQuotaCritical)
    End Function

    ''' <summary>Check if storage quota allows new firmware download</summary>
    Public Function CanDownloadFirmware(estimatedSizeBytes As Long) As (Allowed As Boolean, Reason As String)
        Dim currentUsed = GetTotalStorageUsed()
        Dim projectedUsed = currentUsed + estimatedSizeBytes

        If projectedUsed > StorageQuotaCritical Then
            Dim overage = FormatBytes(projectedUsed - StorageQuotaCritical)
            Return (False, $"Download would exceed storage quota by {overage}. Please clear cache or delete old firmware versions.")
        End If

        Return (True, "")
    End Function

    ''' <summary>Get storage status message for UI display</summary>
    Public Function GetStorageStatusMessage() As String
        Dim used = GetTotalStorageUsed()
        Dim percent = GetStorageUsagePercent()
        Dim status = GetStorageStatus()

        Dim message = $"Storage: {FormatBytes(used)} / {FormatBytes(StorageQuotaCritical)} ({percent}%)"

        Select Case status
            Case StorageStatus.Warning
                message &= " ⚠ Approaching limit"
            Case StorageStatus.Critical
                message &= " ⚠ Critical - Clear cache soon"
            Case StorageStatus.OverQuota
                message &= " 🔴 Over quota - Clear cache required"
        End Select

        Return message
    End Function

    ' ===========================
    ' UTILITIES
    ' ===========================

    ''' <summary>Format bytes to human-readable string</summary>
    Public Shared Function FormatBytes(bytes As Long) As String
        If bytes < 1024 Then
            Return $"{bytes} B"
        ElseIf bytes < 1024 * 1024 Then
            Return $"{bytes / 1024:F2} KB"
        ElseIf bytes < 1024 * 1024 * 1024 Then
            Return $"{bytes / (1024 * 1024):F2} MB"
        Else
            Return $"{bytes / (1024L * 1024 * 1024):F2} GB"
        End If
    End Function

    ''' <summary>Format download speed</summary>
    Public Shared Function FormatSpeed(bytesPerSecond As Double) As String
        Return $"{FormatBytes(CLng(bytesPerSecond))}/s"
    End Function

    ''' <summary>Format time span to readable string</summary>
    Public Shared Function FormatTimeSpan(span As TimeSpan) As String
        If span.TotalSeconds < 60 Then
            Return $"{span.TotalSeconds:F0}s"
        ElseIf span.TotalMinutes < 60 Then
            Return $"{span.TotalMinutes:F0}m {span.Seconds}s"
        Else
            Return $"{span.TotalHours:F0}h {span.Minutes}m"
        End If
    End Function

    ' ===========================
    ' VERIFICATION
    ' ===========================

    ''' <summary>Verify integrity of firmware installation</summary>
    Public Function VerifyFirmware(version As Integer) As Boolean
        Try
            Dim metadata = GetFirmwareMetadata(version)
            If metadata Is Nothing Then
                Return False
            End If

            Dim fakelibDir = Constants.GetFakelibDirectory(version)
            If Not Directory.Exists(fakelibDir) Then
                metadata.Status = FirmwareStatus.NotFound
                SaveMetadata()
                Return False
            End If

            Dim libFiles = Directory.GetFiles(fakelibDir, "*.sprx", SearchOption.TopDirectoryOnly)
            If libFiles.Length = 0 Then
                metadata.Status = FirmwareStatus.NotFound
                SaveMetadata()
                Return False
            End If

            ' Update metadata with current state
            metadata.LibraryCount = libFiles.Length
            metadata.SizeBytes = CalculateDirectorySize(fakelibDir)
            If metadata.Status = FirmwareStatus.NotFound Then
                metadata.Status = FirmwareStatus.Processed
            End If
            SaveMetadata()

            Logger.LogToFile($"Verified FW{version}: {libFiles.Length} libraries OK", LogLevel.Info)
            Return True
        Catch ex As Exception
            Logger.LogToFile($"Error verifying firmware {version}: {ex.Message}", LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>Verify all installed firmwares</summary>
    Public Function VerifyAllFirmwares() As Dictionary(Of Integer, Boolean)
        Dim results As New Dictionary(Of Integer, Boolean)
        For Each version In Constants.SupportedFirmwareVersions
            results(version) = VerifyFirmware(version)
        Next
        Return results
    End Function

End Class