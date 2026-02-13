Imports System.IO

''' <summary>
''' Service for signing ELF files with various signing types
''' Supports Free Fake Sign, NPDRM, and Custom Keys
''' </summary>
Public Class SigningService

    ''' <summary>
    ''' Available signing types for ELF files
    ''' </summary>
    Public Enum SigningType

        ''' <summary>Free Fake Sign - unsigned SELF for homebrew</summary>
        FreeFakeSign = 0

        ''' <summary>NPDRM - with Content ID and encryption</summary>
        Npdrm = 1

        ''' <summary>Custom Keys - advanced signing with user-provided keys</summary>
        CustomKeys = 2

    End Enum

    ''' <summary>
    ''' Options for signing operations
    ''' </summary>
    Public Class SigningOptions

        ''' <summary>Target PS5 SDK version</summary>
        Public Property Ps5SdkVersion As UInteger = &H7000038UI

        ''' <summary>Target PS4 SDK version</summary>
        Public Property Ps4SdkVersion As UInteger = 0

        ''' <summary>Program Auth ID (PAID)</summary>
        Public Property Paid As ULong = &H3100000000000002UL

        ''' <summary>Program Type (PTYPE)</summary>
        Public Property PType As ULong = SignedElfExInfo.PTYPE_FAKE

        ''' <summary>Application version</summary>
        Public Property AppVersion As ULong = 0

        ''' <summary>Firmware version</summary>
        Public Property FwVersion As ULong = 0

        ''' <summary>NPDRM Content ID (for NPDRM signing)</summary>
        Public Property ContentId As String = ""

        ''' <summary>Custom auth info (for advanced signing)</summary>
        Public Property AuthInfo As Byte() = Nothing

        ''' <summary>Create backup before signing</summary>
        Public Property CreateBackup As Boolean = True

        ''' <summary>Overwrite existing output file</summary>
        Public Property OverwriteOutput As Boolean = False

    End Class

    ''' <summary>
    ''' Result of signing operation
    ''' </summary>
    Public Class SigningResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property OutputPath As String
        Public Property FileSize As Long
        Public Property ElapsedMs As Long
    End Class

    ''' <summary>
    ''' Sign an ELF file with specified signing type
    ''' </summary>
    Public Shared Function SignElf(
        elfPath As String,
        outputPath As String,
        signingType As SigningType,
        options As SigningOptions
    ) As SigningResult

        Dim result As New SigningResult With {
            .Success = False,
            .OutputPath = outputPath
        }

        Dim sw As New Stopwatch()
        sw.Start()

        Try
            ' Validate input file
            If Not File.Exists(elfPath) Then
                result.Message = $"Input file not found: {elfPath}"
                Return result
            End If

            ' Check if it's a valid ELF
            If Not IsValidElfFile(elfPath) Then
                result.Message = "Input file is not a valid ELF file"
                Return result
            End If

            ' Check output path
            If File.Exists(outputPath) AndAlso Not options.OverwriteOutput Then
                result.Message = $"Output file already exists: {outputPath}"
                Return result
            End If

            ' Create backup if requested
            If options.CreateBackup Then
                CreateBackupFile(elfPath)
            End If

            ' Perform signing based on type
            Select Case signingType
                Case SigningType.FreeFakeSign
                    SignWithFreeFake(elfPath, outputPath, options, result)

                Case SigningType.Npdrm
                    SignWithNpdrm(elfPath, outputPath, options, result)

                Case SigningType.CustomKeys
                    SignWithCustomKeys(elfPath, outputPath, options, result)

                Case Else
                    result.Message = "Unknown signing type"
                    Return result
            End Select

            ' Get file size
            If File.Exists(outputPath) Then
                result.FileSize = New FileInfo(outputPath).Length
            End If

            sw.Stop()
            result.ElapsedMs = sw.ElapsedMilliseconds
        Catch ex As Exception
            result.Success = False
            result.Message = $"Signing error: {ex.Message}"
            Logger.Log(Form1.rtbStatus, $"Signing failed: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Sign with Free Fake Sign (unsigned SELF for homebrew)
    ''' </summary>
    Private Shared Sub SignWithFreeFake(
        elfPath As String,
        outputPath As String,
        options As SigningOptions,
        result As SigningResult
    )
        Try
            Logger.Log(Form1.rtbStatus, $"Signing with Free Fake Sign...", Color.Blue)

            ' Load ELF file
            Dim elf As New ElfFile()
            elf.Load(elfPath, ignoreSections:=True)

            ' Create SELF with fake signing
            Dim selfFile As New SignedElfFile(
                elf,
                paid:=options.Paid,
                ptype:=SignedElfExInfo.PTYPE_FAKE,
                appVersion:=options.AppVersion,
                fwVersion:=options.FwVersion,
                authInfo:=Nothing
            )

            ' Save signed file
            selfFile.Save(outputPath)

            result.Success = True
            result.Message = $"Successfully signed with Free Fake Sign"
            Logger.Log(Form1.rtbStatus, $"✓ Free Fake Sign completed: {Path.GetFileName(outputPath)}", Color.Green)
        Catch ex As Exception
            result.Success = False
            result.Message = $"Free Fake Sign failed: {ex.Message}"
            Logger.Log(Form1.rtbStatus, $"✗ Free Fake Sign error: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Sign with NPDRM (with Content ID)
    ''' </summary>
    Private Shared Sub SignWithNpdrm(
        elfPath As String,
        outputPath As String,
        options As SigningOptions,
        result As SigningResult
    )
        Try
            Logger.Log(Form1.rtbStatus, $"Signing with NPDRM...", Color.Blue)

            ' Validate Content ID
            If String.IsNullOrEmpty(options.ContentId) Then
                result.Message = "NPDRM signing requires a Content ID"
                result.Success = False
                Return
            End If

            ' Validate Content ID format (should be like "UP0000-CUSA00000_00-GAMEID0000000000")
            If Not ValidateContentId(options.ContentId) Then
                result.Message = "Invalid Content ID format"
                result.Success = False
                Return
            End If

            ' Load ELF file
            Dim elf As New ElfFile()
            elf.Load(elfPath, ignoreSections:=True)

            ' Create SELF with NPDRM
            Dim selfFile As New SignedElfFile(
                elf,
                paid:=options.Paid,
                ptype:=SignedElfExInfo.PTYPE_NPDRM_EXEC,
                appVersion:=options.AppVersion,
                fwVersion:=options.FwVersion,
                authInfo:=Nothing
            )

            ' Set Content ID in NPDRM block
            If selfFile.Npdrm IsNot Nothing Then
                Dim contentIdBytes = Text.Encoding.ASCII.GetBytes(options.ContentId.PadRight(&H13, ControlChars.NullChar))
                Array.Resize(contentIdBytes, &H13)
                selfFile.Npdrm.ContentId = contentIdBytes
            End If

            ' Save signed file
            selfFile.Save(outputPath)

            result.Success = True
            result.Message = $"Successfully signed with NPDRM (Content ID: {options.ContentId})"
            Logger.Log(Form1.rtbStatus, $"✓ NPDRM signing completed: {Path.GetFileName(outputPath)}", Color.Green)
        Catch ex As Exception
            result.Success = False
            result.Message = $"NPDRM signing failed: {ex.Message}"
            Logger.Log(Form1.rtbStatus, $"✗ NPDRM error: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Sign with custom keys (advanced)
    ''' </summary>
    Private Shared Sub SignWithCustomKeys(
        elfPath As String,
        outputPath As String,
        options As SigningOptions,
        result As SigningResult
    )
        Try
            Logger.Log(Form1.rtbStatus, $"Signing with Custom Keys...", Color.Blue)

            ' Validate auth info
            If options.AuthInfo Is Nothing OrElse options.AuthInfo.Length = 0 Then
                result.Message = "Custom signing requires AuthInfo data"
                result.Success = False
                Return
            End If

            ' Load ELF file
            Dim elf As New ElfFile()
            elf.Load(elfPath, ignoreSections:=True)

            ' Create SELF with custom auth info
            Dim selfFile As New SignedElfFile(
                elf,
                paid:=options.Paid,
                ptype:=options.PType,
                appVersion:=options.AppVersion,
                fwVersion:=options.FwVersion,
                authInfo:=options.AuthInfo
            )

            ' Save signed file
            selfFile.Save(outputPath)

            result.Success = True
            result.Message = $"Successfully signed with Custom Keys"
            Logger.Log(Form1.rtbStatus, $"✓ Custom signing completed: {Path.GetFileName(outputPath)}", Color.Green)
        Catch ex As Exception
            result.Success = False
            result.Message = $"Custom signing failed: {ex.Message}"
            Logger.Log(Form1.rtbStatus, $"✗ Custom signing error: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Check if file is a valid ELF
    ''' </summary>
    Public Shared Function IsValidElfFile(filePath As String) As Boolean
        Try
            If Not File.Exists(filePath) Then Return False

            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                If fs.Length < 4 Then Return False

                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' Check ELF magic: 0x7F 'E' 'L' 'F'
                Return magic(0) = &H7F AndAlso
                       magic(1) = &H45 AndAlso
                       magic(2) = &H4C AndAlso
                       magic(3) = &H46
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Check if file is a SELF (signed ELF)
    ''' </summary>
    Public Shared Function IsSelfFile(filePath As String) As Boolean
        Try
            If Not File.Exists(filePath) Then Return False

            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                If fs.Length < 4 Then Return False

                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' Check PS4 SELF magic: 0x4F 0x15 0x3D 0x1D
                If magic(0) = &H4F AndAlso magic(1) = &H15 AndAlso
                   magic(2) = &H3D AndAlso magic(3) = &H1D Then
                    Return True
                End If

                ' Check PS5 SELF magic: 0x54 0x14 0xF5 0xEE
                If magic(0) = &H54 AndAlso magic(1) = &H14 AndAlso
                   magic(2) = &HF5 AndAlso magic(3) = &HEE Then
                    Return True
                End If

                Return False
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Validate Content ID format
    ''' </summary>
    Private Shared Function ValidateContentId(contentId As String) As Boolean
        If String.IsNullOrEmpty(contentId) Then Return False

        ' Content ID should be in format: UP0000-CUSA00000_00-GAMEID0000000000
        ' Or similar patterns
        If contentId.Length < 20 OrElse contentId.Length > 36 Then Return False

        ' Basic validation - should contain hyphen and underscore
        Return contentId.Contains("-") AndAlso contentId.Contains("_")
    End Function

    ''' <summary>
    ''' Create backup of file
    ''' </summary>
    Private Shared Sub CreateBackupFile(filePath As String)
        Try
            Dim backupPath = $"{filePath}.bak_{DateTime.Now:yyyyMMdd_HHmmss}"
            If Not File.Exists(backupPath) Then
                File.Copy(filePath, backupPath)
                Logger.Log(Form1.rtbStatus, $"Backup created: {Path.GetFileName(backupPath)}", Color.DarkGray)
            End If
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, $"Warning: Could not create backup: {ex.Message}", Color.Orange)
        End Try
    End Sub

    ''' <summary>
    ''' Get file type description
    ''' </summary>
    Public Shared Function GetFileTypeDescription(filePath As String) As String
        If IsValidElfFile(filePath) Then
            Return "ELF (Executable and Linkable Format)"
        ElseIf IsSelfFile(filePath) Then
            Return "SELF (Signed ELF)"
        Else
            Return "Unknown format"
        End If
    End Function

    ''' <summary>
    ''' Get recommended signing type based on file name/extension
    ''' </summary>
    Public Shared Function GetRecommendedSigningType(filePath As String) As SigningType
        Dim fileName = Path.GetFileName(filePath).ToLower()
        Dim ext = Path.GetExtension(filePath).ToLower()

        ' Homebrew files typically use Free Fake Sign
        If fileName = "eboot.bin" OrElse ext = ".elf" Then
            Return SigningType.FreeFakeSign
        End If

        ' System libraries might need NPDRM
        If ext = ".prx" OrElse ext = ".sprx" Then
            Return SigningType.FreeFakeSign
        End If

        Return SigningType.FreeFakeSign
    End Function

End Class