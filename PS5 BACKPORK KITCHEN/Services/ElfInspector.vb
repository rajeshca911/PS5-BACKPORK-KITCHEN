Imports System.IO
Imports System.Text

Public Module ElfInspectorService

    Public Structure ElfInspectionReport
        Public FileName As String
        Public FilePath As String
        Public FileSize As Long
        Public IsValidElf As Boolean
        Public ElfType As String
        Public IsSelfSigned As Boolean
        Public Ps5SdkVersion As UInteger
        Public Ps4SdkVersion As UInteger
        Public Ps5SdkFormatted As String
        Public Ps4SdkFormatted As String
        Public ProgramHeaders As Integer
        Public SectionHeaders As Integer
        Public HasProcParam As Boolean
        Public HasModuleParam As Boolean
        Public LibraryDependencies As List(Of String)
        Public Warnings As List(Of String)
        Public ErrorMessage As String
    End Structure

    ''' <summary>
    ''' Inspect ELF file and generate detailed report
    ''' </summary>
    Public Function InspectElfFile(filePath As String) As ElfInspectionReport
        Dim report As New ElfInspectionReport With {
            .FileName = Path.GetFileName(filePath),
            .FilePath = filePath,
            .IsValidElf = False,
            .LibraryDependencies = New List(Of String),
            .Warnings = New List(Of String)
        }

        Try
            If Not File.Exists(filePath) Then
                report.ErrorMessage = "File not found"
                Return report
            End If

            Dim fileInfo As New FileInfo(filePath)
            report.FileSize = fileInfo.Length

            ' Basic ELF validation
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                If fs.Length < 64 Then
                    report.ErrorMessage = "File too small to be a valid ELF"
                    Return report
                End If

                ' Read ELF header magic
                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' Check if it's ELF
                If magic.SequenceEqual(ElfConstants.ELF_MAGIC) Then
                    report.IsValidElf = True
                    report.ElfType = "ELF (Unencrypted)"
                    report.IsSelfSigned = False
                Else
                    ' Check if it's SELF (signed)
                    fs.Seek(0, SeekOrigin.Begin)
                    Dim selfMagic(3) As Byte
                    fs.Read(selfMagic, 0, 4)

                    If selfMagic.SequenceEqual(ElfConstants.PS4_FSELF_MAGIC) Then
                        report.IsValidElf = True
                        report.ElfType = "FSELF (PS4 Signed)"
                        report.IsSelfSigned = True
                    ElseIf selfMagic.SequenceEqual(ElfConstants.PS5_FSELF_MAGIC) Then
                        report.IsValidElf = True
                        report.ElfType = "FSELF (PS5 Signed)"
                        report.IsSelfSigned = True
                    Else
                        report.ErrorMessage = "Unknown file format (not ELF or FSELF)"
                        Return report
                    End If
                End If

                ' Read program header count
                fs.Seek(&H38, SeekOrigin.Begin)
                Dim phCount(1) As Byte
                fs.Read(phCount, 0, 2)
                report.ProgramHeaders = BitConverter.ToUInt16(phCount, 0)

                ' Read section header count
                fs.Seek(&H3C, SeekOrigin.Begin)
                Dim shCount(1) As Byte
                fs.Read(shCount, 0, 2)
                report.SectionHeaders = BitConverter.ToUInt16(shCount, 0)
            End Using

            ' Get SDK versions using existing ElfInspector class
            Dim elfInfo = ElfInspector.ReadInfo(filePath)
            If elfInfo IsNot Nothing Then
                If elfInfo.Ps5SdkVersion.HasValue Then
                    report.Ps5SdkVersion = elfInfo.Ps5SdkVersion.Value
                    report.Ps5SdkFormatted = SdkDetector.FormatSdkVersion(elfInfo.Ps5SdkVersion.Value)
                End If

                If elfInfo.Ps4SdkVersion.HasValue Then
                    report.Ps4SdkVersion = elfInfo.Ps4SdkVersion.Value
                    report.Ps4SdkFormatted = SdkDetector.FormatSdkVersion(elfInfo.Ps4SdkVersion.Value)
                End If

                ' Note: HasProcParam and HasModuleParam not available in ElfInfo
                ' These fields remain false in the report
                report.HasProcParam = False
                report.HasModuleParam = False
            End If

            ' Extract library dependencies (basic implementation)
            report.LibraryDependencies = ExtractLibraryDependencies(filePath)

            ' Generate warnings
            GenerateWarnings(report)
        Catch ex As Exception
            report.ErrorMessage = ex.Message
            report.IsValidElf = False
        End Try

        Return report
    End Function

    ''' <summary>
    ''' Extract library dependencies from ELF
    ''' </summary>
    Private Function ExtractLibraryDependencies(filePath As String) As List(Of String)
        Dim libs As New List(Of String)

        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                Using br As New BinaryReader(fs)
                    ' Simple heuristic: search for common library patterns
                    Dim content = br.ReadBytes(CInt(Math.Min(fs.Length, 1024 * 1024))) ' Read first 1MB

                    ' Convert to string for pattern matching
                    Dim text = Encoding.ASCII.GetString(content)

                    ' Common PS5 libraries
                    Dim libPatterns = {"libSce", "libkernel", "libc.prx", ".sprx", ".prx"}

                    For Each pattern In libPatterns
                        Dim index = 0
                        While True
                            index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)
                            If index = -1 Then Exit While

                            ' Extract library name (simple approach)
                            Dim endIndex = text.IndexOfAny({Chr(0), " "c, Chr(10), Chr(13)}, index)
                            If endIndex > index Then
                                Dim libName = text.Substring(index, Math.Min(50, endIndex - index))
                                If libName.Contains(".prx") OrElse libName.Contains(".sprx") Then
                                    If Not libs.Contains(libName) Then
                                        libs.Add(libName.Trim())
                                    End If
                                End If
                            End If

                            index += 1
                        End While
                    Next
                End Using
            End Using
        Catch ex As Exception
            ' Return partial results
        End Try

        Return libs.Distinct().Take(20).ToList() ' Limit to 20 unique libraries
    End Function

    ''' <summary>
    ''' Generate warnings based on inspection
    ''' </summary>
    Private Sub GenerateWarnings(ByRef report As ElfInspectionReport)
        ' Check SDK version
        If report.Ps5SdkVersion > &H8000000UI Then
            report.Warnings.Add($"High SDK version detected ({report.Ps5SdkFormatted}). May not be compatible with older firmware.")
        End If

        If report.Ps5SdkVersion = 0 AndAlso report.IsValidElf Then
            report.Warnings.Add("No PS5 SDK version found. File may not be patchable.")
        End If

        ' Check file size
        If report.FileSize > 100 * 1024 * 1024 Then ' > 100MB
            report.Warnings.Add($"Large file size ({FormatFileSize(report.FileSize)}). Patching may take longer.")
        End If

        If report.FileSize < 1024 Then ' < 1KB
            report.Warnings.Add("Unusually small file. May be corrupted or incomplete.")
        End If

        ' Check structure
        If report.ProgramHeaders = 0 Then
            report.Warnings.Add("No program headers found. File structure may be invalid.")
        End If

        If Not report.HasProcParam AndAlso Not report.HasModuleParam Then
            report.Warnings.Add("No SCE parameters found. May not be a valid PS5 executable.")
        End If
    End Sub

    ''' <summary>
    ''' Generate detailed inspection report as text
    ''' </summary>
    Public Function GenerateInspectionReport(report As ElfInspectionReport) As String
        Dim sb As New StringBuilder()

        sb.AppendLine("===========================================================")
        sb.AppendLine($"  ELF INSPECTION REPORT: {report.FileName}")
        sb.AppendLine("===========================================================")
        sb.AppendLine()

        If Not report.IsValidElf Then
            sb.AppendLine($"x ERROR: {report.ErrorMessage}")
            Return sb.ToString()
        End If

        sb.AppendLine("=== FILE INFORMATION ===")
        sb.AppendLine($"File Path: {report.FilePath}")
        sb.AppendLine($"File Size: {FormatFileSize(report.FileSize)}")
        sb.AppendLine($"File Type: {report.ElfType}")
        sb.AppendLine($"Is Signed: {If(report.IsSelfSigned, "Yes (FSELF)", "No (Raw ELF)")}")
        sb.AppendLine()

        sb.AppendLine("=== SDK VERSIONS ===")
        If report.Ps5SdkVersion > 0 Then
            sb.AppendLine($"PS5 SDK: {report.Ps5SdkFormatted} (0x{report.Ps5SdkVersion:X8})")
        Else
            sb.AppendLine("PS5 SDK: Not found")
        End If

        If report.Ps4SdkVersion > 0 Then
            sb.AppendLine($"PS4 SDK: {report.Ps4SdkFormatted} (0x{report.Ps4SdkVersion:X8})")
        Else
            sb.AppendLine("PS4 SDK: Not found")
        End If
        sb.AppendLine()

        sb.AppendLine("=== STRUCTURE ===")
        sb.AppendLine($"Program Headers: {report.ProgramHeaders}")
        sb.AppendLine($"Section Headers: {report.SectionHeaders}")
        sb.AppendLine($"Has Process Param: {If(report.HasProcParam, "Yes", "No")}")
        sb.AppendLine($"Has Module Param: {If(report.HasModuleParam, "Yes", "No")}")
        sb.AppendLine()

        If report.LibraryDependencies.Count > 0 Then
            sb.AppendLine("=== LIBRARY DEPENDENCIES ===")
            Dim i As Integer = 0
            While i < report.LibraryDependencies.Count
                sb.AppendLine("  - " + report.LibraryDependencies(i))
                i += 1
            End While
            sb.AppendLine()
        End If

        If report.Warnings.Count > 0 Then
            sb.AppendLine("=== WARNINGS ===")
            Dim j As Integer = 0
            While j < report.Warnings.Count
                sb.AppendLine("  ! " + report.Warnings(j))
                j += 1
            End While
            sb.AppendLine()
        End If

        sb.AppendLine("===========================================================")

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Format file size to human readable
    ''' </summary>
    Private Function FormatFileSize(bytes As Long) As String
        Dim sizes() As String = {"B", "KB", "MB", "GB"}
        Dim order As Integer = 0
        Dim size As Double = bytes

        While size >= 1024 AndAlso order < sizes.Length - 1
            order += 1
            size /= 1024
        End While

        Return $"{size:F2} {sizes(order)}"
    End Function

    ''' <summary>
    ''' Batch inspect multiple files
    ''' </summary>
    Public Function InspectFolder(folderPath As String) As List(Of ElfInspectionReport)
        Dim reports As New List(Of ElfInspectionReport)

        Try
            ' Find all ELF files
            Dim files As New List(Of String)

            Dim ebotPath = Path.Combine(folderPath, EBOOT_FILENAME)
            If File.Exists(ebotPath) Then
                files.Add(ebotPath)
            End If

            For Each ext In PATCHABLE_EXTENSIONS
                files.AddRange(Directory.GetFiles(folderPath, $"*{ext}", SearchOption.AllDirectories))
            Next

            ' Inspect each file
            For Each file In files
                reports.Add(InspectElfFile(file))
            Next
        Catch ex As Exception
            ' Return partial results
        End Try

        Return reports
    End Function

    ''' <summary>
    ''' Compare two ELF files
    ''' </summary>
    Public Function CompareElfFiles(file1Path As String, file2Path As String) As String
        Dim report1 = InspectElfFile(file1Path)
        Dim report2 = InspectElfFile(file2Path)

        Dim sb As New StringBuilder()

        sb.AppendLine("===========================================================")
        sb.AppendLine("  ELF COMPARISON REPORT")
        sb.AppendLine("===========================================================")
        sb.AppendLine()

        sb.AppendLine($"File 1: {report1.FileName}")
        sb.AppendLine($"File 2: {report2.FileName}")
        sb.AppendLine()

        sb.AppendLine("=== SDK VERSION COMPARISON ===")
        sb.AppendLine($"File 1 PS5 SDK: {report1.Ps5SdkFormatted}")
        sb.AppendLine($"File 2 PS5 SDK: {report2.Ps5SdkFormatted}")

        If report1.Ps5SdkVersion > report2.Ps5SdkVersion Then
            sb.AppendLine($"> File 1 has HIGHER SDK version (+{report1.Ps5SdkVersion - report2.Ps5SdkVersion})")
        ElseIf report1.Ps5SdkVersion < report2.Ps5SdkVersion Then
            sb.AppendLine($"> File 2 has HIGHER SDK version (+{report2.Ps5SdkVersion - report1.Ps5SdkVersion})")
        Else
            sb.AppendLine("> Both files have SAME SDK version")
        End If

        sb.AppendLine()
        sb.AppendLine("=== SIZE COMPARISON ===")
        sb.AppendLine($"File 1: {FormatFileSize(report1.FileSize)}")
        sb.AppendLine($"File 2: {FormatFileSize(report2.FileSize)}")

        Dim sizeDiff = report1.FileSize - report2.FileSize
        If sizeDiff > 0 Then
            sb.AppendLine($"> File 1 is LARGER (+{FormatFileSize(sizeDiff)})")
        ElseIf sizeDiff < 0 Then
            sb.AppendLine($"> File 2 is LARGER (+{FormatFileSize(-sizeDiff)})")
        Else
            sb.AppendLine("> Both files are SAME SIZE")
        End If

        Return sb.ToString()
    End Function

End Module