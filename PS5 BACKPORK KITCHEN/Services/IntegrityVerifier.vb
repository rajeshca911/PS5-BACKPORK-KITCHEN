Imports System.IO
Imports System.Security.Cryptography

Public Module IntegrityVerifier

    Public Structure VerificationResult
        Public IsValid As Boolean
        Public Message As String
        Public FilesChecked As Integer
        Public FilesValid As Integer
        Public FilesFailed As Integer
        Public FailedFiles As List(Of String)
    End Structure

    ''' <summary>
    ''' Verify patched files integrity
    ''' </summary>
    Public Function VerifyPatchedFiles(folderPath As String) As VerificationResult
        Dim result As New VerificationResult With {
            .IsValid = True,
            .FilesChecked = 0,
            .FilesValid = 0,
            .FilesFailed = 0,
            .FailedFiles = New List(Of String)
        }

        Try
            Dim files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)

            For Each file In files
                Dim fileName = Path.GetFileName(file).ToLower()
                Dim ext = Path.GetExtension(file).ToLower()

                ' Check only patchable files
                If fileName = EBOOT_FILENAME OrElse PATCHABLE_EXTENSIONS.Contains(ext) Then
                    result.FilesChecked += 1

                    If VerifyElfFile(file) Then
                        result.FilesValid += 1
                    Else
                        result.FilesFailed += 1
                        result.FailedFiles.Add(Path.GetFileName(file))
                        result.IsValid = False
                    End If
                End If
            Next

            If result.IsValid Then
                result.Message = $"All {result.FilesChecked} file(s) verified successfully"
            Else
                result.Message = $"Verification failed: {result.FilesFailed} file(s) corrupted"
            End If
        Catch ex As Exception
            result.IsValid = False
            result.Message = $"Verification error: {ex.Message}"
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Verify single ELF file structure
    ''' </summary>
    Private Function VerifyElfFile(filePath As String) As Boolean
        Try
            If Not File.Exists(filePath) Then Return False

            ' Check file size (must be > 0)
            Dim fileInfo As New FileInfo(filePath)
            If fileInfo.Length = 0 Then Return False

            ' Try to read ELF info (basic structure validation)
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                ' Check minimum size for ELF header
                If fs.Length < 64 Then Return False

                ' Read ELF magic
                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' Verify it's still a valid ELF
                If Not magic.SequenceEqual(ElfConstants.ELF_MAGIC) Then
                    ' Could also be a signed SELF, check for that
                    fs.Seek(0, SeekOrigin.Begin)
                    Dim selfMagic(3) As Byte
                    fs.Read(selfMagic, 0, 4)

                    If Not (selfMagic.SequenceEqual(ElfConstants.PS4_FSELF_MAGIC) OrElse
                           selfMagic.SequenceEqual(ElfConstants.PS5_FSELF_MAGIC)) Then
                        Return False
                    End If
                End If
            End Using

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Calculate file checksum (SHA256)
    ''' </summary>
    Public Function CalculateFileChecksum(filePath As String) As String
        Try
            Using sha256 As SHA256 = SHA256.Create()
                Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                    Dim hash = sha256.ComputeHash(fs)
                    Return BitConverter.ToString(hash).Replace("-", "").ToLower()
                End Using
            End Using
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Create backup manifest with checksums
    ''' </summary>
    Public Function CreateBackupManifest(folderPath As String) As Dictionary(Of String, String)
        Dim manifest As New Dictionary(Of String, String)

        Try
            Dim files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)

            For Each file In files
                Dim fileName = Path.GetFileName(file).ToLower()
                Dim ext = Path.GetExtension(file).ToLower()

                If fileName = EBOOT_FILENAME OrElse PATCHABLE_EXTENSIONS.Contains(ext) Then
                    Dim relativePath = file.Replace(folderPath, "").TrimStart("\"c)
                    Dim checksum = CalculateFileChecksum(file)
                    If Not String.IsNullOrEmpty(checksum) Then
                        manifest(relativePath) = checksum
                    End If
                End If
            Next
        Catch ex As Exception
            ' Return partial manifest
        End Try

        Return manifest
    End Function

End Module