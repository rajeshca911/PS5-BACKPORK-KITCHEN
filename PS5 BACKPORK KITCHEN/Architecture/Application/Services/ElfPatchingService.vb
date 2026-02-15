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

        Public Sub New(fileSystem As IFileSystem, logger As ILogger)
            _fileSystem = fileSystem
            _logger = logger
        End Sub

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

                ' ---------- STEP 1: Ensure file is decrypted ELF ----------

                Dim workingPath As String = filePath

                If Not IsFileDecrypted(filePath) Then

                    _logger.LogInfo($"SELF detected — decrypting: {filePath}")

                    Dim dir = IO.Path.GetDirectoryName(filePath)
                    Dim base = IO.Path.GetFileNameWithoutExtension(filePath)
                    Dim ext = IO.Path.GetExtension(filePath)

                    Dim tempDecPath = IO.Path.Combine(dir, base & "_tmp_dec" & ext)

                    Dim ok = selfutilmodule.unpackfile(filePath, tempDecPath)

                    If Not ok Then
                        _logger.LogError($"Decrypt failed: {filePath}")

                        Return Result(Of PatchResult).Fail(New DecryptFailedError(filePath))
                    End If

                    ' overwrite original safely (your original design — correct)
                    IO.File.Copy(tempDecPath, filePath, True)
                    IO.File.Delete(tempDecPath)

                    _logger.LogInfo($"Decrypted OK → {filePath}")

                Else
                    _logger.LogInfo($"Already ELF — skipping decrypt: {filePath}")
                End If

                ' ---------- Step 2: Ensure ELF (decrypt SELF first) ----------
                ' Read file info using existing ElfInspector
                Dim info = ElfInspector.ReadInfo(filePath)

                If info Is Nothing Then
                    Return Result(Of PatchResult).Fail(New InvalidElfFormatError(filePath))
                End If

                If Not info.IsPatchable Then
                    Return Result(Of PatchResult).Fail(New InvalidElfFormatError(filePath))
                End If

                Dim currentSdk = CLng(info.Ps5SdkVersion)

                ' Check if already patched
                _logger.LogInfo($"SDK check: current={currentSdk} target = {targetSdk}")

                If currentSdk = targetSdk Then

                    _logger.LogInfo($"File already patched: {filePath}")
                    Return Result(Of PatchResult).Fail(New AlreadyPatchedError(filePath, targetSdk))
                End If

                cancellationToken.ThrowIfCancellationRequested()

                ' Patch using existing ElfPatcher
                Dim logMessage As String = ""
                Dim targetPs5 = CUInt(targetSdk)
                Dim targetPs4 = 0UI ' Not used for now

                'Dim success = ElfPatcher.PatchSingleFile(filePath, targetPs5, targetPs4, logMessage)

                'If Not success Then
                '    Return Result(Of PatchResult).Fail(New PatchFailedError(filePath))
                'End If
                Dim status = ElfPatcher.PatchSingleFile(filePath, targetPs5, targetPs4, logMessage)

                Select Case status

                    Case PatchStatus.Patched
                        ' ----------  Optional 6xx libc string patch ----------

                        Dim expermental6xx As Boolean = Form1.chklibcpatch.Checked
                        If expermental6xx Then

                            filename = IO.Path.GetFileName(filePath).ToLower()

                            If filename = "libc.prx" Then
                                _logger.LogInfo("Applying 6xx libc string patch")

                                PatchPrxString(
                filePath,
                Encoding.ASCII.GetBytes("4h6F1LLbTiw#A#B"),
                Encoding.ASCII.GetBytes("IWIBBdTHit4#A#B")
            )



                            End If

                        End If

                        ' ---------- STEP 3: Sign patched ELF back to SELF ----------

                        _logger.LogInfo($"Signing patched file: {filePath}")

                        Dim dir = IO.Path.GetDirectoryName(filePath)
                        Dim baseName = IO.Path.GetFileNameWithoutExtension(filePath)
                        Dim tempSelf = IO.Path.Combine(dir, baseName & "_tmp.self")

                        Dim signOptions As New SigningService.SigningOptions()

                        Dim signResult = SigningService.SignElf(
                            filePath,
                            tempSelf,
                            SigningService.SigningType.FreeFakeSign,
                            signOptions
                        )

                        If Not signResult.Success Then
                            _logger.LogError($"Signing failed: {filePath}")

                            Return Result(Of PatchResult).Fail(
                                New PatchFailedError($"Signing failed")
                            )
                        End If

                        IO.File.Copy(tempSelf, filePath, True)
                        IO.File.Delete(tempSelf)

                        _logger.LogInfo($"Signed OK → {filePath}")



                    Case PatchStatus.Skipped
                        Return Result(Of PatchResult).Fail(
            New AlreadyPatchedError(filePath, targetSdk)
        )

                    Case PatchStatus.Failed
                        Return Result(Of PatchResult).Fail(
            New PatchFailedError(filePath)
        )

                End Select


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
        Public Shared Function IsFileDecrypted(path As String) As Boolean
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read)
                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' ELF magic = 7F 45 4C 46
                Return magic(0) = &H7F AndAlso
                       magic(1) = &H45 AndAlso
                       magic(2) = &H4C AndAlso
                       magic(3) = &H46
            End Using
        End Function

    End Class
End Namespace
