Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Errors
Imports PS5_BACKPORK_KITCHEN.Services
Imports System.IO

Namespace Architecture.Application.Services
    ''' <summary>
    ''' Service for ELF patching operations
    ''' Wraps existing core/ElfPatcher and core/ElfReader functionality
    ''' </summary>
    Public Class ElfPatchingService
        Implements IElfPatchingService

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
        ' continue success flow

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
