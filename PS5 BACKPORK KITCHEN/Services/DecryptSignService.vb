Imports System.IO

''' <summary>
''' Service for decrypt + sign workflows
''' Handles full round-trip: Decrypt SELF → Modify ELF → Sign back to SELF
''' </summary>
Public Class DecryptSignService

    ''' <summary>
    ''' Result of decrypt + sign workflow
    ''' </summary>
    Public Class DecryptSignResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property InputPath As String
        Public Property OutputPath As String
        Public Property IntermediateElfPath As String
        Public Property FileSize As Long
        Public Property ElapsedMs As Long
        Public Property StepsFailed As List(Of String)

        Public Sub New()
            StepsFailed = New List(Of String)()
        End Sub

    End Class

    ''' <summary>
    ''' Options for workflow operations
    ''' </summary>
    Public Class WorkflowOptions

        ''' <summary>Signing type to use</summary>
        Public Property SigningType As SigningService.SigningType = SigningService.SigningType.FreeFakeSign

        ''' <summary>Signing options (SDK versions, PAID, etc.)</summary>
        Public Property SigningOptions As SigningService.SigningOptions

        ''' <summary>Keep intermediate ELF file after signing</summary>
        Public Property KeepIntermediateFiles As Boolean = False

        ''' <summary>Create backup of original SELF</summary>
        Public Property CreateBackup As Boolean = True

        ''' <summary>Output directory for intermediate files (if null, uses temp)</summary>
        Public Property IntermediateDirectory As String = Nothing

        Public Sub New()
            SigningOptions = New SigningService.SigningOptions()
        End Sub

    End Class

    ''' <summary>
    ''' Execute full decrypt + sign workflow
    ''' </summary>
    Public Shared Function ExecuteDecryptSignWorkflow(
        inputSelfPath As String,
        outputSelfPath As String,
        options As WorkflowOptions,
        Optional progressCallback As Action(Of Integer, String) = Nothing
    ) As DecryptSignResult

        Dim result As New DecryptSignResult With {
            .Success = False,
            .InputPath = inputSelfPath,
            .OutputPath = outputSelfPath
        }

        Dim sw As New Stopwatch()
        sw.Start()

        Dim tempElfPath As String = Nothing

        Try
            ' Validate inputs
            Dim validationError = ValidateWorkflowInputs(inputSelfPath, outputSelfPath, options)
            If Not String.IsNullOrEmpty(validationError) Then
                result.Message = validationError
                result.StepsFailed.Add("Validation")
                Return result
            End If

            ' Create backup if requested
            If options.CreateBackup Then
                Try
                    CreateBackupFile(inputSelfPath)
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus, $"Warning: Backup creation failed: {ex.Message}", Color.Orange)
                End Try
            End If

            ' ===========================
            ' STEP 1: Decrypt SELF to ELF
            ' ===========================
            progressCallback?.Invoke(10, "Decrypting SELF file...")

            ' Determine intermediate ELF path
            If String.IsNullOrEmpty(options.IntermediateDirectory) Then
                tempElfPath = Path.Combine(Path.GetTempPath(), $"decrypted_{Guid.NewGuid():N}.elf")
            Else
                Directory.CreateDirectory(options.IntermediateDirectory)
                tempElfPath = Path.Combine(options.IntermediateDirectory, Path.GetFileNameWithoutExtension(inputSelfPath) & "_decrypted.elf")
            End If

            result.IntermediateElfPath = tempElfPath

            Logger.Log(Form1.rtbStatus, $"Step 1/3: Decrypting SELF to ELF...", Color.Blue)
            Logger.Log(Form1.rtbStatus, $"   Intermediate ELF: {Path.GetFileName(tempElfPath)}", Color.DarkGray)

            Dim decryptSuccess = selfutilmodule.unpackfile(inputSelfPath, tempElfPath)

            If Not decryptSuccess OrElse Not File.Exists(tempElfPath) Then
                result.Message = "Failed to decrypt SELF file"
                result.StepsFailed.Add("Decrypt")
                Logger.Log(Form1.rtbStatus, $"✗ Step 1 failed: Decryption error", Color.Red)
                Return result
            End If

            progressCallback?.Invoke(20, "Decryption completed")
            Logger.Log(Form1.rtbStatus, $"✓ Step 1 completed: ELF extracted ({FormatFileSize(New FileInfo(tempElfPath).Length)})", Color.Green)

            ' ===========================
            ' STEP 2: Placeholder for user modifications
            ' ===========================
            progressCallback?.Invoke(40, "Processing ELF...")
            Logger.Log(Form1.rtbStatus, $"Step 2/3: ELF ready for modifications (if needed)", Color.Blue)

            ' In the future, this is where you could:
            ' - Patch SDK version
            ' - Modify sections
            ' - Inject code
            ' - etc.

            progressCallback?.Invoke(50, "ELF processing completed")
            Logger.Log(Form1.rtbStatus, $"✓ Step 2 completed: ELF ready", Color.Green)

            ' ===========================
            ' STEP 3: Sign ELF to SELF
            ' ===========================
            progressCallback?.Invoke(60, "Signing ELF to SELF...")
            Logger.Log(Form1.rtbStatus, $"Step 3/3: Signing ELF back to SELF...", Color.Blue)
            Logger.Log(Form1.rtbStatus, $"   Signing Type: {options.SigningType}", Color.DarkGray)

            Dim signResult = SigningService.SignElf(
                tempElfPath,
                outputSelfPath,
                options.SigningType,
                options.SigningOptions
            )

            If Not signResult.Success Then
                result.Message = $"Failed to sign ELF: {signResult.Message}"
                result.StepsFailed.Add("Sign")
                Logger.Log(Form1.rtbStatus, $"✗ Step 3 failed: {signResult.Message}", Color.Red)
                Return result
            End If

            progressCallback?.Invoke(90, "Signing completed")
            Logger.Log(Form1.rtbStatus, $"✓ Step 3 completed: SELF signed ({FormatFileSize(signResult.FileSize)})", Color.Green)

            ' ===========================
            ' CLEANUP
            ' ===========================
            progressCallback?.Invoke(95, "Cleaning up...")

            If Not options.KeepIntermediateFiles AndAlso File.Exists(tempElfPath) Then
                Try
                    File.Delete(tempElfPath)
                    Logger.Log(Form1.rtbStatus, $"   Intermediate ELF deleted", Color.DarkGray)
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus, $"   Warning: Could not delete temp file: {ex.Message}", Color.Orange)
                End Try
            End If

            ' ===========================
            ' SUCCESS
            ' ===========================
            progressCallback?.Invoke(100, "Workflow completed")

            sw.Stop()
            result.Success = True
            result.Message = "Decrypt + Sign workflow completed successfully"
            result.FileSize = signResult.FileSize
            result.ElapsedMs = sw.ElapsedMilliseconds

            Logger.Log(Form1.rtbStatus, $"✓ Workflow completed in {result.ElapsedMs} ms", Color.LightGreen)
        Catch ex As Exception
            result.Success = False
            result.Message = $"Workflow error: {ex.Message}"
            result.StepsFailed.Add("Exception")
            Logger.Log(Form1.rtbStatus, $"✗ Workflow exception: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)

            ' Cleanup on error
            If Not String.IsNullOrEmpty(tempElfPath) AndAlso File.Exists(tempElfPath) Then
                Try
                    File.Delete(tempElfPath)
                Catch
                    ' Silent fail
                End Try
            End If
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Validate workflow inputs before execution
    ''' </summary>
    Public Shared Function ValidateWorkflowInputs(
        inputSelfPath As String,
        outputSelfPath As String,
        options As WorkflowOptions
    ) As String

        ' Check input file exists
        If String.IsNullOrWhiteSpace(inputSelfPath) Then
            Return "Input SELF path is required"
        End If

        If Not File.Exists(inputSelfPath) Then
            Return $"Input file not found: {inputSelfPath}"
        End If

        ' Check input is a SELF file
        If Not SigningService.IsSelfFile(inputSelfPath) Then
            Return "Input file is not a valid SELF/BIN file"
        End If

        ' Check output path is specified
        If String.IsNullOrWhiteSpace(outputSelfPath) Then
            Return "Output SELF path is required"
        End If

        ' Check output directory exists or can be created
        Dim outputDir = Path.GetDirectoryName(outputSelfPath)
        If Not String.IsNullOrEmpty(outputDir) AndAlso Not Directory.Exists(outputDir) Then
            Try
                Directory.CreateDirectory(outputDir)
            Catch ex As Exception
                Return $"Cannot create output directory: {ex.Message}"
            End Try
        End If

        ' Validate NPDRM Content ID if needed
        If options.SigningType = SigningService.SigningType.Npdrm Then
            If String.IsNullOrWhiteSpace(options.SigningOptions.ContentId) Then
                Return "NPDRM signing requires a Content ID"
            End If
        End If

        ' Validate Custom Keys auth info if needed
        If options.SigningType = SigningService.SigningType.CustomKeys Then
            If options.SigningOptions.AuthInfo Is Nothing OrElse options.SigningOptions.AuthInfo.Length = 0 Then
                Return "Custom signing requires AuthInfo data"
            End If
        End If

        ' All validation passed
        Return Nothing
    End Function

    ''' <summary>
    ''' Create backup of file
    ''' </summary>
    Private Shared Sub CreateBackupFile(filePath As String)
        Try
            Dim backupPath = $"{filePath}.bak_{DateTime.Now:yyyyMMdd_HHmmss}"
            If Not File.Exists(backupPath) Then
                File.Copy(filePath, backupPath)
                Logger.Log(Form1.rtbStatus, $"   Backup created: {Path.GetFileName(backupPath)}", Color.DarkGray)
            End If
        Catch ex As Exception
            Throw New Exception($"Backup creation failed: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Format file size for display
    ''' </summary>
    Private Shared Function FormatFileSize(bytes As Long) As String
        If bytes < 1024 Then
            Return $"{bytes} bytes"
        ElseIf bytes < 1024 * 1024 Then
            Return $"{bytes / 1024.0:F2} KB"
        Else
            Return $"{bytes / (1024.0 * 1024.0):F2} MB"
        End If
    End Function

End Class