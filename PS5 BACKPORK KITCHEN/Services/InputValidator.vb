Imports System.IO

Public Module InputValidator

    Public Enum ValidationResult
        Valid
        InvalidPath
        PathNotExists
        NoPermissions
        EmptyFolder
        NoElfFiles
        InvalidFolderStructure
    End Enum

    Public Structure ValidationReport
        Public Result As ValidationResult
        Public Message As String
        Public Details As String
        Public ElfFilesFound As Integer
        Public TotalSize As Long
    End Structure

    ''' <summary>
    ''' Validate game folder for patching
    ''' </summary>
    Public Function ValidateGameFolder(folderPath As String) As ValidationReport
        Dim report As New ValidationReport With {
            .Result = ValidationResult.Valid,
            .Message = "Validation passed",
            .ElfFilesFound = 0,
            .TotalSize = 0
        }

        ' Check if path is null or empty
        If String.IsNullOrWhiteSpace(folderPath) Then
            report.Result = ValidationResult.InvalidPath
            report.Message = "Folder path is empty or invalid"
            Return report
        End If

        ' Check if directory exists
        If Not Directory.Exists(folderPath) Then
            report.Result = ValidationResult.PathNotExists
            report.Message = "The specified folder does not exist"
            report.Details = folderPath
            Return report
        End If

        ' Check read/write permissions
        Try
            Dim testFile = Path.Combine(folderPath, $".test_write_{Guid.NewGuid()}.tmp")
            File.WriteAllText(testFile, "test")
            File.Delete(testFile)
        Catch ex As UnauthorizedAccessException
            report.Result = ValidationResult.NoPermissions
            report.Message = "No read/write permissions on the selected folder"
            report.Details = ex.Message
            Return report
        Catch ex As Exception
            report.Result = ValidationResult.NoPermissions
            report.Message = "Cannot access folder"
            report.Details = ex.Message
            Return report
        End Try

        ' Check if folder is empty
        Try
            Dim files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            If files.Length = 0 Then
                report.Result = ValidationResult.EmptyFolder
                report.Message = "The selected folder is empty"
                Return report
            End If
        Catch ex As Exception
            report.Result = ValidationResult.NoPermissions
            report.Message = "Cannot scan folder contents"
            report.Details = ex.Message
            Return report
        End Try

        ' Check for patchable ELF files
        Try
            Dim elfFiles = GetPatchableFiles(folderPath)
            report.ElfFilesFound = elfFiles.Count

            If elfFiles.Count = 0 Then
                report.Result = ValidationResult.NoElfFiles
                report.Message = "No patchable ELF files found (eboot.bin, .prx, .sprx)"
                report.Details = "Make sure you selected the correct game folder"
                Return report
            End If

            ' Calculate total size
            For Each file In elfFiles
                Dim fileInfo As New FileInfo(file)
                report.TotalSize += fileInfo.Length
            Next

            report.Message = $"Found {report.ElfFilesFound} patchable file(s)"
            report.Details = $"Total size: {FormatBytes(report.TotalSize)}"
        Catch ex As Exception
            report.Result = ValidationResult.InvalidFolderStructure
            report.Message = "Error scanning folder structure"
            report.Details = ex.Message
            Return report
        End Try

        Return report
    End Function

    ''' <summary>
    ''' Get all patchable files in folder
    ''' </summary>
    Private Function GetPatchableFiles(folderPath As String) As List(Of String)
        Dim patchableFiles As New List(Of String)

        Try
            Dim allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)

            For Each file In allFiles
                Dim fileName = Path.GetFileName(file).ToLower()
                Dim ext = Path.GetExtension(file).ToLower()

                If fileName = EBOOT_FILENAME OrElse PATCHABLE_EXTENSIONS.Contains(ext) Then
                    patchableFiles.Add(file)
                End If
            Next
        Catch ex As Exception
            ' Return empty list on error
        End Try

        Return patchableFiles
    End Function

    ''' <summary>
    ''' Validate SDK version format
    ''' </summary>
    Public Function ValidateSdkVersion(version As UInteger) As Boolean
        ' SDK version should be reasonable (not 0, not too high)
        Return version > 0 AndAlso version < &HFFFFFFFFUI
    End Function

    ''' <summary>
    ''' Format bytes to human readable size
    ''' </summary>
    Private Function FormatBytes(bytes As Long) As String
        Dim sizes() As String = {"B", "KB", "MB", "GB", "TB"}
        Dim order As Integer = 0
        Dim size As Double = bytes

        While size >= 1024 AndAlso order < sizes.Length - 1
            order += 1
            size /= 1024
        End While

        Return $"{size:F2} {sizes(order)}"
    End Function

End Module