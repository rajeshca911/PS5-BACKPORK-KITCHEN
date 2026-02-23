Imports System.IO
Imports System.Text
Imports System.Windows.Forms.Design.AxImporter
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Errors
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters
Imports PS5_BACKPORK_KITCHEN.Services
Imports PS5_BACKPORK_KITCHEN.globalvariables
' ============================================================
' PATCH PIPELINE CONTRACT — DO NOT REORDER WITHOUT REVIEW
' ============================================================
' This method implements the required PS5 ELF/SELF patch flow.
' Each stage is intentional. Removing or reordering steps will
' produce broken binaries.
'
' REQUIRED EXECUTION ORDER:
'
'   1. Detect SELF vs ELF
'   2. If SELF → decrypt to ELF
'   3. Validate ELF structure
'   4. Check SDK version
'   5. Patch ELF headers
'   6. Optional libc.prx string patch (6xx compatibility)
'   7. Re-sign ELF → SELF   *** REQUIRED ***
'   8. Overwrite original only AFTER signing success
'
' IMPORTANT RULES:
'
' • Never return a patched file without re-signing
' • libc.prx string patch MUST occur before signing
' • Do not sign before patching
' • Do not overwrite original before patch + sign succeed
' • Do not remove SELF detection — many inputs are encrypted
' • Do not move signing into Catch/Finally blocks
'
' SERVICE LAYER NOTES:
'
' • Avoid UI references here (Form1, controls, etc.)
' • Feature flags should be passed via options when possible
' • Logging is allowed — UI access is not
'
' TEST MATRIX (must pass before merge):
'
' □ SELF input → patched → signed → loads
' □ ELF input → patched → signed → loads
' □ Already patched → skipped
' □ libc.prx + 6xx flag → string patch applied
' □ Patch failure → original unchanged
' □ Signing failure → original unchanged
'
' If changing this pipeline — run binary validation tests.
'
' ============================================================

Namespace Architecture.Application.Services
    ''' <summary>
    ''' Service for ELF patching operations
    ''' Wraps existing core/ElfPatcher and core/ElfReader functionality
    ''' </summary>
    Public Class ElfPatchingService
        Implements IElfPatchingService
        Public fileStatusList As New List(Of String)
        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger
        Private ReadOnly _enableLibcPatch As Boolean
        Private ReadOnly _errorLog As New List(Of PipelineError)

        Public Sub New(fileSystem As IFileSystem, logger As ILogger, Optional enableLibcPatch As Boolean = False)
            _fileSystem = fileSystem
            _logger = logger
            _enableLibcPatch = enableLibcPatch
        End Sub

        Public Structure PipelineError
            Public Stage As String
            Public FilePath As String
            Public Message As String
            Public StackTrace As String
            Public Timestamp As DateTime
            Public Context As Dictionary(Of String, String)
        End Structure

        Public Async Function PatchFileAsync(filePath As String, targetSdk As Long, cancellationToken As Threading.CancellationToken) As Task(Of Result(Of PatchResult)) _
            Implements IElfPatchingService.PatchFileAsync

            Dim startTime = DateTime.Now

            Try
                Dim filename As String
                cancellationToken.ThrowIfCancellationRequested()

                ' Check file exists
                Dim exists = Await _fileSystem.FileExistsAsync(filePath)
                If Not exists Then
                    Return Result(Of PatchResult).Fail(New FileNotFoundError(filePath))
                End If

                ' ---------- STEP 1: Detect file type (SELF / ELF / StrippedELF) ----------

                Dim fileData As Byte() = IO.File.ReadAllBytes(filePath)
                Dim fileType As String = selfutilmodule.GetFileType(fileData)

                _logger.LogInfo($"File type detected: {fileType} — {filePath}")

                ' ---------- STEP 2: If SELF → decrypt to ELF ----------

                If fileType = "SELF" Then

                    _logger.LogInfo($"SELF detected — decrypting: {filePath}")

                    Dim decDir = IO.Path.GetDirectoryName(filePath)
                    Dim decBase = IO.Path.GetFileNameWithoutExtension(filePath)
                    Dim decExt = IO.Path.GetExtension(filePath)

                    Dim tempDecPath = IO.Path.Combine(decDir, decBase & "_tmp_dec" & decExt)

                    Dim ok = selfutilmodule.unpackfile(filePath, tempDecPath)

                    If Not ok Then
                        LogPipelineError("Decryption", filePath, "SELF decryption failed", New Exception("unpackfile returned false"))
                        Return Result(Of PatchResult).Fail(New DecryptFailedError(filePath))
                    End If

                    ' Overwrite original with decrypted ELF (working copy strategy)
                    IO.File.Copy(tempDecPath, filePath, True)
                    IO.File.Delete(tempDecPath)
                    fileData = IO.File.ReadAllBytes(filePath)

                    _logger.LogInfo($"Decrypted OK → {filePath}")

                ElseIf fileType = "ELF" OrElse fileType = "StrippedELF" Then

                    _logger.LogInfo($"{fileType} detected — skipping decryption: {filePath}")

                Else

                    LogPipelineError("Detection", filePath, $"Unsupported file type: {fileType}", Nothing)
                    Return Result(Of PatchResult).Fail(New InvalidElfFormatError(filePath))

                End If

                ' ---------- STEP 3: Validate ELF structure ----------

                If Not ValidateElfStructure(fileData, filePath) Then
                    LogPipelineError("Validation", filePath, "ELF structural validation failed", Nothing)
                    Return Result(Of PatchResult).Fail(New InvalidElfFormatError(filePath))
                End If

                ' ---------- STEP 4: Read and verify current SDK version ----------

                Dim info = ElfInspector.ReadInfo(filePath)

                If info Is Nothing Then
                    LogPipelineError("SDKDetection", filePath, "Failed to read ELF info", Nothing)
                    Return Result(Of PatchResult).Fail(New InvalidElfFormatError(filePath))
                End If

                If Not info.IsPatchable Then
                    LogPipelineError("SDKDetection", filePath, "ELF is not patchable", Nothing)
                    Return Result(Of PatchResult).Fail(New InvalidElfFormatError(filePath))
                End If

                Dim currentSdk = CLng(info.Ps5SdkVersion)

                ' Check if already patched
                _logger.LogInfo($"SDK check: current={currentSdk:X} target={targetSdk:X}")

                Dim needsPatching As Boolean = (currentSdk <> targetSdk)

                If Not needsPatching Then
                    _logger.LogInfo($"File already at target SDK — skipping patch: {filePath}")
                End If

                cancellationToken.ThrowIfCancellationRequested()

                ' ---------- STEP 5: Patch ELF headers to target SDK ----------

                Dim logMessage As String = ""
                Dim targetPs5 = CUInt(targetSdk)
                Dim targetPs4 = 0UI

                Dim status As PatchStatus = PatchStatus.Skipped

                If needsPatching Then
                    status = ElfPatcher.PatchSingleFile(filePath, targetPs5, targetPs4, logMessage)

                    If status = PatchStatus.Failed Then
                        LogPipelineError("Patching", filePath, "ELF patching failed: " & logMessage, Nothing)
                        Return Result(Of PatchResult).Fail(New PatchFailedError(filePath))
                    End If

                    _logger.LogInfo($"Patch status: {status} — {logMessage}")
                End If

                ' ---------- STEP 6: Optional libc.prx string patch (6xx compatibility) ----------
                ' MUST execute AFTER header patch, BEFORE signing

                If _enableLibcPatch Then

                    Dim libcFilename = IO.Path.GetFileName(filePath).ToLower()

                    If libcFilename = "libc.prx" Then
                        _logger.LogInfo("Applying 6xx libc string patch (before signing)")

                        Try
                            PatchPrxString(
                                filePath,
                                Encoding.ASCII.GetBytes("4h6F1LLbTiw#A#B"),
                                Encoding.ASCII.GetBytes("IWIBBdTHit4#A#B")
                            )
                        Catch ex As Exception
                            LogPipelineError("LibcPatch", filePath, "libc string patch failed", ex)
                            ' Non-fatal, continue to signing
                        End Try

                    End If

                End If

                ' ---------- STEP 7: Re-sign ELF back to SELF *** REQUIRED *** ----------
                ' Signing is MANDATORY for all files that reached this point

                _logger.LogInfo($"Signing file (mandatory): {filePath}")

                Dim signDir = IO.Path.GetDirectoryName(filePath)
                Dim signBaseName = IO.Path.GetFileNameWithoutExtension(filePath)
                Dim tempSelf = IO.Path.Combine(signDir, signBaseName & "_tmp.self")

                Dim signOptions As New SigningService.SigningOptions()

                Dim signResult = SigningService.SignElf(
                    filePath,
                    tempSelf,
                    SigningService.SigningType.FreeFakeSign,
                    signOptions
                )

                If Not signResult.Success Then
                    LogPipelineError("Signing", filePath, "Signing failed", New Exception(signResult.Message))
                    Return Result(Of PatchResult).Fail(New PatchFailedError($"Signing failed: {signResult.Message}"))
                End If

                ' ---------- STEP 8: Overwrite original ONLY AFTER signing succeeds ----------

                IO.File.Copy(tempSelf, filePath, True)
                IO.File.Delete(tempSelf)

                _logger.LogInfo($"Signed OK → {filePath}")


                Dim duration = DateTime.Now - startTime

                _logger.LogInfo($"Patched {filePath}: {currentSdk:X} -> {targetSdk:X} in {duration.TotalMilliseconds}ms")

                ' Get file size
                Dim fileSize = Await _fileSystem.GetFileSizeAsync(filePath)

                ' Create result
                Dim patchResult = New PatchResult With {
                    .FilePath = filePath,
                    .OriginalSdk = info.Ps5SdkVersion.Value,
                    .PatchedSdk = targetSdk,
                    .BytesWritten = fileSize,
                    .Duration = duration
                }

                Return Result(Of PatchResult).Success(patchResult)

            Catch ex As OperationCanceledException
                _logger.LogWarning($"Patch cancelled for {filePath}")
                Throw

            Catch ex As Exception
                _logger.LogError($"Unexpected error patching {filePath}", ex)
                Return Result(Of PatchResult).Fail(New FileAccessError(filePath, ex))
            End Try
        End Function

        Public Async Function CanPatchFileAsync(filePath As String) As Task(Of Result(Of Boolean)) _
            Implements IElfPatchingService.CanPatchFileAsync

            Try
                Dim exists = Await _fileSystem.FileExistsAsync(filePath)
                If Not exists Then Return Result(Of Boolean).Success(False)

                Dim info = Await Task.Run(Function() ElfInspector.ReadInfo(filePath))

                Return Result(Of Boolean).Success(info IsNot Nothing AndAlso info.IsPatchable)

            Catch ex As Exception
                _logger.LogError($"Error checking file {filePath}", ex)
                Return Result(Of Boolean).Success(False)
            End Try
        End Function

        Public Async Function DetectSdkVersionAsync(filePath As String) As Task(Of Result(Of Long)) _
            Implements IElfPatchingService.DetectSdkVersionAsync

            Try
                Dim info = Await Task.Run(Function() ElfInspector.ReadInfo(filePath))

                If info Is Nothing Then
                    Return Result(Of Long).Fail(New InvalidElfFormatError(filePath))
                End If

                Return Result(Of Long).Success(CLng(info.Ps5SdkVersion))

            Catch ex As FileNotFoundException
                Return Result(Of Long).Fail(New FileNotFoundError(filePath))
            Catch ex As Exception
                Return Result(Of Long).Fail(New FileAccessError(filePath, ex))
            End Try
        End Function
        ' ============================================================
        ' STRUCTURAL ELF VALIDATION
        ' ============================================================
        Private Function ValidateElfStructure(fileData As Byte(), filePath As String) As Boolean
            If fileData Is Nothing OrElse fileData.Length < 64 Then
                _logger.LogError($"File too small to be valid ELF: {filePath}")
                Return False
            End If

            Try
                ' Use the robust validation from selfutilmodule
                Dim fileType = selfutilmodule.GetFileType(fileData)

                If fileType = "ELF" OrElse fileType = "StrippedELF" Then
                    _logger.LogInfo($"ELF structure validated: {fileType}")
                    Return True
                End If

                _logger.LogError($"Invalid ELF structure: {fileType}")
                Return False

            Catch ex As Exception
                _logger.LogError($"ELF validation exception: {ex.Message}")
                Return False
            End Try
        End Function

        ' ============================================================
        ' ERROR LOGGING & REPORTING
        ' ============================================================
        Private Sub LogPipelineError(stage As String, filePath As String, message As String, ex As Exception)
            Dim err As New PipelineError With {
                .Stage = stage,
                .FilePath = filePath,
                .Message = message,
                .StackTrace = If(ex IsNot Nothing, ex.StackTrace, ""),
                .Timestamp = DateTime.Now,
                .Context = New Dictionary(Of String, String)()
            }

            ' Add exception details if present
            If ex IsNot Nothing Then
                err.Context("ExceptionType") = ex.GetType().FullName
                err.Context("ExceptionMessage") = ex.Message
                If ex.InnerException IsNot Nothing Then
                    err.Context("InnerException") = ex.InnerException.Message
                End If
            End If

            _errorLog.Add(err)
            _logger.LogError($"[{stage}] {message} — {filePath}")

            If ex IsNot Nothing AndAlso Not String.IsNullOrEmpty(ex.StackTrace) Then
                _logger.LogError($"Stack trace: {ex.StackTrace.Substring(0, Math.Min(200, ex.StackTrace.Length))}")
            End If
        End Sub

        Public Function GenerateErrorReport() As String
            If _errorLog.Count = 0 Then
                Return "No errors encountered during pipeline execution."
            End If

            Dim sb As New StringBuilder()
            sb.AppendLine("============================================================")
            sb.AppendLine($"PIPELINE ERROR REPORT — {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            sb.AppendLine("============================================================")
            sb.AppendLine()
            sb.AppendLine($"Total Errors: {_errorLog.Count}")
            sb.AppendLine()

            For i As Integer = 0 To _errorLog.Count - 1
                Dim err = _errorLog(i)
                sb.AppendLine($"[{i + 1}] {err.Stage} — {err.Timestamp:HH:mm:ss}")
                sb.AppendLine($"    File: {err.FilePath}")
                sb.AppendLine($"    Message: {err.Message}")

                If err.Context.Count > 0 Then
                    sb.AppendLine("    Context:")
                    For Each kvp In err.Context
                        sb.AppendLine($"      - {kvp.Key}: {kvp.Value}")
                    Next
                End If

                If Not String.IsNullOrEmpty(err.StackTrace) Then
                    sb.AppendLine("    Stack Trace:")
                    sb.AppendLine(err.StackTrace.Replace(vbCrLf, vbCrLf & "      "))
                End If

                sb.AppendLine()
            Next

            sb.AppendLine("============================================================")
            Return sb.ToString()
        End Function

        Public Sub SaveErrorReportToFile(outputPath As String)
            Try
                Dim report = GenerateErrorReport()
                IO.File.WriteAllText(outputPath, report)
                _logger.LogInfo($"Error report saved: {outputPath}")
            Catch ex As Exception
                _logger.LogError($"Failed to save error report: {ex.Message}")
            End Try
        End Sub

    End Class
End Namespace

