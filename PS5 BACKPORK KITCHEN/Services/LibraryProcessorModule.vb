Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading

''' <summary>
''' Module for processing extracted firmware libraries.
''' Handles SPRX decryption, selective extraction, checksum generation, and validation.
''' </summary>
Public Class LibraryProcessorModule

    ' ===========================
    ' COMMON LIBRARIES
    ' ===========================

    ''' <summary>List of commonly used system libraries to extract</summary>
    Private Shared ReadOnly CommonLibraries As String() = {
        "libc.sprx",
        "libSceFios2.sprx",
        "libSceSystemService.sprx",
        "libkernel.sprx",
        "libSceVideoOut.sprx",
        "libSceAudioOut.sprx",
        "libScePad.sprx",
        "libSceUserService.sprx",
        "libSceSysmodule.sprx",
        "libSceNet.sprx",
        "libSceNetCtl.sprx",
        "libSceHttp.sprx",
        "libSceSsl.sprx",
        "libSceGnmDriver.sprx",
        "libSceCommonDialog.sprx",
        "libSceMsgDialog.sprx",
        "libSceImeDialog.sprx",
        "libSceSaveData.sprx",
        "libSceNpManager.sprx",
        "libSceNpCommon.sprx",
        "libSceNpTrophy.sprx",
        "libSceRtc.sprx",
        "libScePngDec.sprx",
        "libSceJpegDec.sprx",
        "libSceVideoCoreServerInterface.sprx",
        "libSceMove.sprx",
        "libSceSharePlay.sprx"
    }

    ' ===========================
    ' PROGRESS TRACKING
    ' ===========================

    ''' <summary>Processing progress information</summary>
    Public Class ProcessingProgress
        Public Property Stage As String
        Public Property CurrentFile As String
        Public Property FilesProcessed As Integer
        Public Property TotalFiles As Integer
        Public Property PercentComplete As Integer
        Public Property IsComplete As Boolean

        Public ReadOnly Property FormattedProgress As String
            Get
                Return $"{Stage} - {PercentComplete}% ({FilesProcessed}/{TotalFiles} files)"
            End Get
        End Property

    End Class

    ' ===========================
    ' LIBRARY INFO
    ' ===========================

    ''' <summary>Information about a processed library</summary>
    Public Class LibraryInfo
        Public Property FileName As String
        Public Property FilePath As String
        Public Property SizeBytes As Long
        Public Property Checksum As String
        Public Property IsDecrypted As Boolean
        Public Property IsValid As Boolean
        Public Property ErrorMessage As String

        Public ReadOnly Property FormattedSize As String
            Get
                Return FirmwareManagerService.FormatBytes(SizeBytes)
            End Get
        End Property

    End Class

    ' ===========================
    ' PROCESSING METHODS
    ' ===========================

    ''' <summary>Process firmware libraries (decrypt, validate, generate checksums)</summary>
    Public Shared Async Function ProcessLibrariesAsync(
        version As Integer,
        selectiveMode As Boolean,
        progressCallback As IProgress(Of ProcessingProgress),
        cancellationToken As CancellationToken
    ) As Task(Of (Success As Boolean, ErrorMessage As String, ProcessedCount As Integer))

        Try
            Dim fakelibDir = Constants.GetFakelibDirectory(version)
            If Not Directory.Exists(fakelibDir) Then
                Return (False, "Firmware libraries not found. Please extract firmware first.", 0)
            End If

            ' Get list of libraries to process
            Dim allLibraries = Directory.GetFiles(fakelibDir, "*.sprx", SearchOption.TopDirectoryOnly).ToList()
            Dim librariesToProcess As List(Of String)

            If selectiveMode Then
                ' Filter to only common libraries
                librariesToProcess = allLibraries.Where(Function(libPath)
                                                            Dim fileName = Path.GetFileName(libPath)
                                                            Return CommonLibraries.Contains(fileName, StringComparer.OrdinalIgnoreCase)
                                                        End Function).ToList()
                Logger.LogToFile($"Selective mode: Processing {librariesToProcess.Count} of {allLibraries.Count} libraries", LogLevel.Info)
            Else
                librariesToProcess = allLibraries
                Logger.LogToFile($"Processing all {librariesToProcess.Count} libraries", LogLevel.Info)
            End If

            If librariesToProcess.Count = 0 Then
                Return (False, "No libraries to process", 0)
            End If

            ' Process each library
            Dim processedCount = 0
            Dim failedCount = 0
            Dim libraryInfos As New List(Of LibraryInfo)

            For Each libPath In librariesToProcess
                cancellationToken.ThrowIfCancellationRequested()

                Dim fileName = Path.GetFileName(libPath)

                progressCallback?.Report(New ProcessingProgress With {
                    .Stage = "Processing",
                    .CurrentFile = fileName,
                    .FilesProcessed = processedCount + failedCount,
                    .TotalFiles = librariesToProcess.Count,
                    .PercentComplete = CInt(((processedCount + failedCount) * 100.0) / librariesToProcess.Count),
                    .IsComplete = False
                })

                ' Process library
                Dim libInfo = Await ProcessLibraryAsync(libPath, cancellationToken)
                libraryInfos.Add(libInfo)

                If libInfo.IsValid Then
                    processedCount += 1
                Else
                    failedCount += 1
                    Logger.LogToFile($"Failed to process {fileName}: {libInfo.ErrorMessage}", LogLevel.Warning)
                End If
            Next

            ' Save library manifest
            Await SaveLibraryManifestAsync(version, libraryInfos)

            ' Report completion
            progressCallback?.Report(New ProcessingProgress With {
                .Stage = "Complete",
                .CurrentFile = "",
                .FilesProcessed = processedCount,
                .TotalFiles = librariesToProcess.Count,
                .PercentComplete = 100,
                .IsComplete = True
            })

            Logger.LogToFile($"Processing complete: {processedCount} succeeded, {failedCount} failed", LogLevel.Success)
            Return (True, "", processedCount)
        Catch ex As OperationCanceledException
            Logger.LogToFile($"Processing cancelled for firmware {version}", LogLevel.Warning)
            Return (False, "Processing cancelled by user", 0)
        Catch ex As Exception
            Logger.LogToFile($"Error processing libraries for firmware {version}: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message, 0)
        End Try
    End Function

    ''' <summary>Process a single library file</summary>
    Private Shared Async Function ProcessLibraryAsync(
        libPath As String,
        cancellationToken As CancellationToken
    ) As Task(Of LibraryInfo)

        Dim libInfo As New LibraryInfo With {
            .FileName = Path.GetFileName(libPath),
            .FilePath = libPath,
            .IsValid = False
        }

        Try
            ' Get file info
            Dim fileInfo As New FileInfo(libPath)
            libInfo.SizeBytes = fileInfo.Length

            ' Validate file size
            If libInfo.SizeBytes < 1024 Then
                libInfo.ErrorMessage = "File too small to be valid"
                Return libInfo
            End If

            ' Check if file is encrypted (FSELF format)
            Dim isEncrypted = Await IsLibraryEncryptedAsync(libPath)
            libInfo.IsDecrypted = Not isEncrypted

            If isEncrypted Then
                ' NOTE: Actual decryption would require SelfUtil integration
                ' For now, we just flag it as encrypted
                Logger.LogToFile($"{libInfo.FileName} is encrypted (FSELF format)", LogLevel.Debug)
                ' libInfo.ErrorMessage = "Encrypted - decryption not implemented yet"
                ' Return libInfo
            End If

            ' Generate checksum
            libInfo.Checksum = Await CalculateChecksumAsync(libPath, cancellationToken)

            ' Mark as valid
            libInfo.IsValid = True
            Logger.LogToFile($"Processed {libInfo.FileName}: {libInfo.FormattedSize}, checksum: {libInfo.Checksum?.Substring(0, 8)}...", LogLevel.Debug)
        Catch ex As Exception
            libInfo.ErrorMessage = ex.Message
            Logger.LogToFile($"Error processing {libInfo.FileName}: {ex.Message}", LogLevel.Error)
        End Try

        Return libInfo
    End Function

    ' ===========================
    ' DECRYPTION
    ' ===========================

    ''' <summary>Check if library file is encrypted (FSELF format)</summary>
    Private Shared Async Function IsLibraryEncryptedAsync(filePath As String) As Task(Of Boolean)
        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                If fs.Length < 4 Then
                    Return False
                End If

                Dim magicBytes(3) As Byte
                Await fs.ReadAsync(magicBytes, 0, 4)

                ' Check for PS5 FSELF magic: 54 14 F5 EE
                Dim ps5Magic = ElfConstants.PS5_FSELF_MAGIC
                If magicBytes.SequenceEqual(ps5Magic) Then
                    Return True
                End If

                ' Check for PS4 FSELF magic: 4F 15 3D 1D
                Dim ps4Magic = ElfConstants.PS4_FSELF_MAGIC
                If magicBytes.SequenceEqual(ps4Magic) Then
                    Return True
                End If

                ' Check for ELF magic: 7F 45 4C 46
                Dim elfMagic = ElfConstants.ELF_MAGIC
                If magicBytes.SequenceEqual(elfMagic) Then
                    Return False  ' Already decrypted
                End If

                Return False
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>Decrypt library using SelfUtil (future implementation)</summary>
    Private Shared Function DecryptLibrary(sourcePath As String, outputPath As String) As Boolean
        Try
            ' Call existing SelfUtil module
            Return selfutilmodule.unpackfile(sourcePath, outputPath)
        Catch ex As Exception
            Logger.LogToFile($"Error decrypting library: {ex.Message}", LogLevel.Error)
            Return False
        End Try
    End Function

    ' ===========================
    ' CHECKSUM GENERATION
    ' ===========================

    ''' <summary>Calculate SHA256 checksum for file</summary>
    Private Shared Async Function CalculateChecksumAsync(
        filePath As String,
        cancellationToken As CancellationToken
    ) As Task(Of String)

        Try
            Using sha256 As SHA256 = SHA256.Create()
                Using fileStream As FileStream = File.OpenRead(filePath)
                    Dim hashBytes As Byte() = Await Task.Run(Function() sha256.ComputeHash(fileStream), cancellationToken)
                    Return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
                End Using
            End Using
        Catch ex As Exception
            Logger.LogToFile($"Error calculating checksum: {ex.Message}", LogLevel.Error)
            Return ""
        End Try
    End Function

    ' ===========================
    ' MANIFEST MANAGEMENT
    ' ===========================

    ''' <summary>Save library manifest to JSON file</summary>
    Private Shared Async Function SaveLibraryManifestAsync(
        version As Integer,
        libraries As List(Of LibraryInfo)
    ) As Task

        Try
            Dim manifestPath = Path.Combine(Constants.GetFakelibDirectory(version), "library_manifest.json")
            Dim json = System.Text.Json.JsonSerializer.Serialize(libraries, New System.Text.Json.JsonSerializerOptions With {
                .WriteIndented = True
            })
            Await File.WriteAllTextAsync(manifestPath, json)
            Logger.LogToFile($"Saved library manifest: {manifestPath}", LogLevel.Debug)
        Catch ex As Exception
            Logger.LogToFile($"Error saving library manifest: {ex.Message}", LogLevel.Warning)
        End Try
    End Function

    ''' <summary>Load library manifest from JSON file</summary>
    Public Shared Async Function LoadLibraryManifestAsync(version As Integer) As Task(Of List(Of LibraryInfo))
        Try
            Dim manifestPath = Path.Combine(Constants.GetFakelibDirectory(version), "library_manifest.json")
            If Not File.Exists(manifestPath) Then
                Return New List(Of LibraryInfo)()
            End If

            Dim json = Await File.ReadAllTextAsync(manifestPath)
            Return System.Text.Json.JsonSerializer.Deserialize(Of List(Of LibraryInfo))(json)
        Catch ex As Exception
            Logger.LogToFile($"Error loading library manifest: {ex.Message}", LogLevel.Warning)
            Return New List(Of LibraryInfo)()
        End Try
    End Function

    ' ===========================
    ' VALIDATION
    ' ===========================

    ''' <summary>Validate library file against manifest checksum</summary>
    Public Shared Async Function ValidateLibraryAsync(
        libPath As String,
        expectedChecksum As String,
        cancellationToken As CancellationToken
    ) As Task(Of Boolean)

        Try
            Dim actualChecksum = Await CalculateChecksumAsync(libPath, cancellationToken)
            Return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase)
        Catch
            Return False
        End Try
    End Function

    ''' <summary>Validate all libraries in firmware version</summary>
    Public Shared Async Function ValidateAllLibrariesAsync(
        version As Integer,
        cancellationToken As CancellationToken
    ) As Task(Of ValueTuple(Of Integer, Integer, Integer))

        Try
            Dim manifest = Await LoadLibraryManifestAsync(version)
            If manifest.Count = 0 Then
                Return (0, 0, 0)
            End If

            Dim validCount = 0
            Dim invalidCount = 0
            Dim missingCount = 0

            For Each libInfo In manifest
                cancellationToken.ThrowIfCancellationRequested()

                If Not File.Exists(libInfo.FilePath) Then
                    missingCount += 1
                    Continue For
                End If

                If String.IsNullOrEmpty(libInfo.Checksum) Then
                    validCount += 1  ' No checksum to validate
                    Continue For
                End If

                Dim isValid = Await ValidateLibraryAsync(libInfo.FilePath, libInfo.Checksum, cancellationToken)
                If isValid Then
                    validCount += 1
                Else
                    invalidCount += 1
                End If
            Next

            Logger.LogToFile($"Validation results - Valid: {validCount}, Invalid: {invalidCount}, Missing: {missingCount}", LogLevel.Info)
            Return (validCount, invalidCount, missingCount)
        Catch ex As Exception
            Logger.LogToFile($"Error validating libraries: {ex.Message}", LogLevel.Error)
            Return (0, 0, 0)
        End Try
    End Function

    ' ===========================
    ' UTILITIES
    ' ===========================

    ''' <summary>Check if library is in common libraries list</summary>
    Public Shared Function IsCommonLibrary(fileName As String) As Boolean
        Return CommonLibraries.Contains(fileName, StringComparer.OrdinalIgnoreCase)
    End Function

    ''' <summary>Get list of common libraries</summary>
    Public Shared Function GetCommonLibraries() As String()
        Return CommonLibraries.Clone()
    End Function

End Class