Imports System.IO
Imports System.Net.Http
Imports System.Threading

Imports System.Windows.Forms
Imports System.Diagnostics

''' <summary>
''' Module for extracting PS5 firmware PUP files using ps5-pup-unpacker tool.
''' Handles tool auto-download, PUP extraction, library discovery, and organization into versioned folders.
''' </summary>
Public Class FirmwareExtractorModule

    ' ===========================
    ' PROGRESS TRACKING
    ' ===========================

    ''' <summary>Extraction progress information</summary>
    Public Class ExtractionProgress
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
    ' TOOL MANAGEMENT
    ' ===========================

    ''' <summary>Ensure ps5-pup-unpacker tool is available (auto-download if needed)</summary>

    Public Shared Async Function EnsureToolAvailableAsync(cancellationToken As CancellationToken) As Task(Of (Success As Boolean, ErrorMessage As String, OpenDownloadPage As Boolean))

        Try
            ' Check if tool already exists
            If File.Exists(Constants.PupUnpackerPath) Then
                Logger.LogToFile("ps5-pup-unpacker tool found", LogLevel.Debug)

                Return (True, "", False)
            End If

            Logger.LogToFile("ps5-pup-unpacker not found, attempting download...", LogLevel.Info)

            ' Ensure tools directory exists
            If Not Directory.Exists(Constants.ToolsDirectory) Then
                Directory.CreateDirectory(Constants.ToolsDirectory)
            End If

            ' Try download
            Dim result = Await DownloadToolAsync(cancellationToken)
            If Not result.Success Then
                ' Offer to open download page
                Return (False, result.ErrorMessage, True)

            End If

            ' Verify tool exists after download
            If Not File.Exists(Constants.PupUnpackerPath) Then

                Return (False, "Tool download completed but file not found", True)
            End If

            Logger.LogToFile("ps5-pup-unpacker tool downloaded successfully", LogLevel.Success)
            Return (True, "", False)
        Catch ex As Exception
            Logger.LogToFile($"Error ensuring tool availability: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message, False)

        End Try
    End Function

    ''' <summary>Download ps5-pup-unpacker tool from GitHub</summary>
    Private Shared Async Function DownloadToolAsync(cancellationToken As CancellationToken) As Task(Of (Success As Boolean, ErrorMessage As String))
        Dim urls() As String = {
            Constants.PupUnpackerDownloadUrl,
            Constants.PupUnpackerDownloadUrlAlt,
            Constants.PupUnpackerDownloadUrlBackup
        }

        For Each url In urls
            Try
                Using client As New HttpClient()
                    client.Timeout = TimeSpan.FromMinutes(5)

                    client.DefaultRequestHeaders.UserAgent.ParseAdd("PS5-BACKPORK-KITCHEN/2.2")

                    Logger.LogToFile($"Attempting download from {url}", LogLevel.Debug)

                    Using response = Await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        If response.IsSuccessStatusCode Then

                            ' Ensure directory exists
                            If Not Directory.Exists(Constants.ToolsDirectory) Then
                                Directory.CreateDirectory(Constants.ToolsDirectory)
                            End If

                            Using fileStream As New FileStream(Constants.PupUnpackerPath, FileMode.Create, FileAccess.Write, FileShare.None)
                                Await response.Content.CopyToAsync(fileStream, cancellationToken)
                            End Using

                            ' Verify file was created and has content
                            Dim fileInfo As New FileInfo(Constants.PupUnpackerPath)
                            If fileInfo.Exists AndAlso fileInfo.Length > 0 Then
                                Logger.LogToFile($"Tool downloaded successfully from {url} ({FirmwareManagerService.FormatBytes(fileInfo.Length)})", LogLevel.Success)
                                Return (True, "")
                            Else
                                Logger.LogToFile($"Downloaded file is empty or invalid from {url}", LogLevel.Warning)
                                ' Try next URL
                            End If
                        Else
                            Logger.LogToFile($"HTTP {response.StatusCode} from {url}", LogLevel.Warning)

                        End If
                    End Using
                End Using
            Catch ex As OperationCanceledException
                Logger.LogToFile("Tool download cancelled by user", LogLevel.Warning)
                Return (False, "Download cancelled")
            Catch ex As Exception
                Logger.LogToFile($"Error downloading from {url}: {ex.Message}", LogLevel.Warning)
            End Try
        Next

        ' All download attempts failed - provide enhanced fallback instructions
        Dim toolsDir = Constants.ToolsDirectory
        Dim errorMsg = "⚠ ps5-pup-unpacker tool not found - Manual download required" & vbCrLf & vbCrLf &
                      "📥 DOWNLOAD OPTIONS:" & vbCrLf & vbCrLf &
                      "Option 1 (Recommended):" & vbCrLf &
                      "  • Visit: https://www.psx-place.com/resources/ps5-pup-decrypter-and-unpacker.1449/" & vbCrLf &
                      "  • Download the .zip file (177KB)" & vbCrLf &
                      "  • Extract ps5-pup-unpacker.exe" & vbCrLf & vbCrLf &
                      "Option 2 (GitHub Source):" & vbCrLf &
                      "  • Visit: https://github.com/zecoxao/ps5-pup-unpacker" & vbCrLf &
                      "  • Download source and compile (requires C++ build tools)" & vbCrLf & vbCrLf &
                      "📂 INSTALLATION:" & vbCrLf &
                      $"  1. Create folder: {toolsDir}" & vbCrLf &
                      $"  2. Place exe in: {Constants.PupUnpackerPath}" & vbCrLf &
                      "  3. Retry firmware extraction" & vbCrLf & vbCrLf &
                      "💡 TIP: After placing the tool, click 'Refresh' in Firmware Manager"

        Logger.LogToFile("All download attempts failed for ps5-pup-unpacker", LogLevel.Error)
        Return (False, errorMsg)
    End Function

    ' ===========================
    ' EXTRACTION METHODS
    ' ===========================

    ''' <summary>Extract firmware PUP file and organize libraries</summary>
    Public Shared Async Function ExtractFirmwareAsync(
        version As Integer,
        progressCallback As IProgress(Of ExtractionProgress),
        cancellationToken As CancellationToken
    ) As Task(Of (Success As Boolean, ErrorMessage As String, LibraryCount As Integer))

        Try
            ' Ensure tool is available
            Dim toolResult = Await EnsureToolAvailableAsync(cancellationToken)
            If Not toolResult.Success Then

                ' If download failed, offer to open download page
                If toolResult.OpenDownloadPage Then
                    Dim openPage = MessageBox.Show(
                        toolResult.ErrorMessage & vbCrLf & vbCrLf &
                        "Open download page in browser now?",
                        "Tool Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information
                    )

                    If openPage = DialogResult.Yes Then
                        Try
                            Process.Start(New ProcessStartInfo With {
                                .FileName = "https://www.psx-place.com/resources/ps5-pup-decrypter-and-unpacker.1449/",
                                .UseShellExecute = True
                            })
                        Catch
                            ' Ignore browser open errors
                        End Try
                    End If
                End If

                Return (False, toolResult.ErrorMessage, 0)
            End If

            ' Get PUP file path
            Dim pupPath = FirmwareDownloadModule.GetCachedPupPath(version)
            If Not File.Exists(pupPath) Then
                Return (False, "PUP file not found. Please download firmware first.", 0)
            End If

            ' Create temp extraction directory
            Dim tempExtractDir = Path.Combine(Constants.FirmwareCacheDirectory, $"extract_fw{version}_temp")
            If Directory.Exists(tempExtractDir) Then
                Directory.Delete(tempExtractDir, True)
            End If
            Directory.CreateDirectory(tempExtractDir)

            Logger.LogToFile($"Extracting firmware {version} from PUP file...", LogLevel.Info)

            ' Report initial progress
            progressCallback?.Report(New ExtractionProgress With {
                .Stage = "Unpacking PUP",
                .CurrentFile = Path.GetFileName(pupPath),
                .FilesProcessed = 0,
                .TotalFiles = 1,
                .PercentComplete = 0,
                .IsComplete = False
            })

            ' Execute ps5-pup-unpacker
            Dim extractResult = Await ExtractPupFileAsync(pupPath, tempExtractDir, cancellationToken)
            If Not extractResult.Success Then
                CleanupTempDirectory(tempExtractDir)
                Return (False, extractResult.ErrorMessage, 0)
            End If

            progressCallback?.Report(New ExtractionProgress With {
                .Stage = "Finding libraries",
                .CurrentFile = "",
                .FilesProcessed = 1,
                .TotalFiles = 2,
                .PercentComplete = 50,
                .IsComplete = False
            })

            ' Find all SPRX/PRX libraries in extracted files
            Dim libraries = FindLibraryFiles(tempExtractDir)
            Logger.LogToFile($"Found {libraries.Count} libraries in firmware {version}", LogLevel.Info)

            If libraries.Count = 0 Then
                CleanupTempDirectory(tempExtractDir)
                Return (False, "No library files found in firmware", 0)
            End If

            ' Organize libraries into target directory
            Dim targetDir = Constants.GetFakelibDirectory(version)
            If Not Directory.Exists(targetDir) Then
                Directory.CreateDirectory(targetDir)
            End If

            progressCallback?.Report(New ExtractionProgress With {
                .Stage = "Copying libraries",
                .CurrentFile = "",
                .FilesProcessed = 0,
                .TotalFiles = libraries.Count,
                .PercentComplete = 75,
                .IsComplete = False
            })

            ' Copy libraries to target directory
            Dim copiedCount = 0
            For Each libPath In libraries
                cancellationToken.ThrowIfCancellationRequested()

                Dim fileName = Path.GetFileName(libPath)
                Dim targetPath = Path.Combine(targetDir, fileName)

                File.Copy(libPath, targetPath, True)
                copiedCount += 1

                progressCallback?.Report(New ExtractionProgress With {
                    .Stage = "Copying libraries",
                    .CurrentFile = fileName,
                    .FilesProcessed = copiedCount,
                    .TotalFiles = libraries.Count,
                    .PercentComplete = 75 + CInt((copiedCount * 25.0) / libraries.Count),
                    .IsComplete = False
                })
            Next

            ' Cleanup temp directory
            CleanupTempDirectory(tempExtractDir)

            ' Report completion
            progressCallback?.Report(New ExtractionProgress With {
                .Stage = "Complete",
                .CurrentFile = "",
                .FilesProcessed = libraries.Count,
                .TotalFiles = libraries.Count,
                .PercentComplete = 100,
                .IsComplete = True
            })

            Logger.LogToFile($"Successfully extracted {copiedCount} libraries for firmware {version}", LogLevel.Success)
            Return (True, "", copiedCount)
        Catch ex As OperationCanceledException
            Logger.LogToFile($"Extraction cancelled for firmware {version}", LogLevel.Warning)
            Return (False, "Extraction cancelled by user", 0)
        Catch ex As Exception
            Logger.LogToFile($"Error extracting firmware {version}: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message, 0)
        End Try
    End Function

    ''' <summary>Execute ps5-pup-unpacker tool to extract PUP file</summary>
    Private Shared Async Function ExtractPupFileAsync(
        pupPath As String,
        outputDir As String,
        cancellationToken As CancellationToken
    ) As Task(Of (Success As Boolean, ErrorMessage As String))

        Try
            Dim startInfo As New ProcessStartInfo With {
                .FileName = Constants.PupUnpackerPath,
                .Arguments = $"""{pupPath}"" ""{outputDir}""",
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True
            }

            Using process As Process = Process.Start(startInfo)
                ' Wait for process with cancellation support
                Await Task.Run(Sub() process.WaitForExit(), cancellationToken)

                Dim stdout = Await process.StandardOutput.ReadToEndAsync()
                Dim stderr = Await process.StandardError.ReadToEndAsync()

                If Not String.IsNullOrWhiteSpace(stdout) Then
                    Logger.LogToFile($"Unpacker output: {stdout}", LogLevel.Debug)
                End If

                If Not String.IsNullOrWhiteSpace(stderr) Then
                    Logger.LogToFile($"Unpacker errors: {stderr}", LogLevel.Warning)
                End If

                If process.ExitCode <> 0 Then

                    ' Check if error is due to encrypted PUP file
                    Dim errorDetails = If(Not String.IsNullOrWhiteSpace(stderr), stderr, stdout)

                    If process.ExitCode = 148 OrElse errorDetails.Contains("Usage") OrElse errorDetails.Contains(".dec") Then
                        ' PUP file is encrypted - provide decryption instructions
                        Dim helpMsg = "⚠️ Firmware PUP file is ENCRYPTED - Decryption Required" & vbCrLf & vbCrLf &
                                     "📋 DECRYPTION PROCESS (On Jailbroken PS5):" & vbCrLf & vbCrLf &
                                     "1️⃣ Prepare USB Drive:" & vbCrLf &
                                     "   • Format USB as exFAT/FAT32" & vbCrLf &
                                     "   • Copy downloaded PUP to: USB:\PS5\UPDATE\PS5UPDATE.PUP" & vbCrLf &
                                     "   • Copy ps5_pup_decrypt.elf to USB root" & vbCrLf &
                                     "   (Tool location: tools\ps5_pup_decrypt.elf)" & vbCrLf & vbCrLf &
                                     "2️⃣ On PS5 (requires jailbreak):" & vbCrLf &
                                     "   • Load ps5_pup_decrypt.elf payload" & vbCrLf &
                                     "   • Plug in USB drive" & vbCrLf &
                                     "   • Tool will decrypt PUP automatically" & vbCrLf &
                                     "   • Result: PS5UPDATE.PUP.dec on USB" & vbCrLf & vbCrLf &
                                     "3️⃣ Import Decrypted File:" & vbCrLf &
                                     "   • Copy .PUP.dec from USB to PC" & vbCrLf &
                                     "   • In Firmware Manager: click firmware row" & vbCrLf &
                                     "   • Click 'Import PUP' → select .PUP.dec file" & vbCrLf &
                                     "   • Extraction will proceed automatically ✓" & vbCrLf & vbCrLf &
                                     "ℹ️ GitHub: https://github.com/zecoxao/ps5-pup-decrypt"
                        Return (False, helpMsg)
                    Else
                        ' Other error
                        Return (False, $"Unpacker exited with code {process.ExitCode}" & vbCrLf & errorDetails)
                    End If

                End If
            End Using

            Return (True, "")
        Catch ex As OperationCanceledException
            Throw  ' Re-throw for caller to handle
        Catch ex As Exception
            Logger.LogToFile($"Error running unpacker: {ex.Message}", LogLevel.Error)
            Return (False, ex.Message)
        End Try
    End Function

    ' ===========================
    ' LIBRARY DISCOVERY
    ' ===========================

    ''' <summary>Find all PRX/SPRX library files in directory recursively</summary>
    Private Shared Function FindLibraryFiles(searchDir As String) As List(Of String)
        Dim libraries As New List(Of String)

        Try
            ' Search for .sprx files (most common)
            libraries.AddRange(Directory.GetFiles(searchDir, "*.sprx", SearchOption.AllDirectories))

            ' Search for .prx files
            libraries.AddRange(Directory.GetFiles(searchDir, "*.prx", SearchOption.AllDirectories))

            ' Filter to only common system libraries (optional - can be configured)
            ' For now, include all found libraries
            Logger.LogToFile($"Found {libraries.Count} library files", LogLevel.Debug)
        Catch ex As Exception
            Logger.LogToFile($"Error finding library files: {ex.Message}", LogLevel.Error)
        End Try

        Return libraries
    End Function

    ''' <summary>Check if file is a valid library file</summary>
    Private Shared Function IsValidLibraryFile(filePath As String) As Boolean
        Try
            ' Check file size (too small = not valid)
            Dim fileInfo As New FileInfo(filePath)
            If fileInfo.Length < 1024 Then
                Return False
            End If

            ' Check file extension
            Dim extension = Path.GetExtension(filePath).ToLowerInvariant()
            If extension <> ".sprx" AndAlso extension <> ".prx" Then
                Return False
            End If

            ' Could add magic number check here if needed
            Return True
        Catch
            Return False
        End Try
    End Function

    ' ===========================
    ' UTILITIES
    ' ===========================

    ''' <summary>Cleanup temporary extraction directory</summary>
    Private Shared Sub CleanupTempDirectory(tempDir As String)
        Try
            If Directory.Exists(tempDir) Then
                Directory.Delete(tempDir, True)
                Logger.LogToFile("Cleaned up temporary extraction directory", LogLevel.Debug)
            End If
        Catch ex As Exception
            Logger.LogToFile($"Error cleaning up temp directory: {ex.Message}", LogLevel.Warning)
        End Try
    End Sub

    ''' <summary>Get extraction status for firmware version</summary>
    Public Shared Function GetExtractionStatus(version As Integer) As (IsExtracted As Boolean, LibraryCount As Integer)
        Try
            Dim fakelibDir = Constants.GetFakelibDirectory(version)
            If Not Directory.Exists(fakelibDir) Then
                Return (False, 0)
            End If

            Dim libraries = Directory.GetFiles(fakelibDir, "*.sprx", SearchOption.TopDirectoryOnly)
            Return (libraries.Length > 0, libraries.Length)
        Catch
            Return (False, 0)
        End Try
    End Function

End Class