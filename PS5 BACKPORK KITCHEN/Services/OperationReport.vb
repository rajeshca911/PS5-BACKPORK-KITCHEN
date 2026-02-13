Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Public Module OperationReport

    Public Structure PatchOperation
        Public StartTime As DateTime
        Public EndTime As DateTime
        Public Duration As TimeSpan
        Public GameFolder As String
        Public TargetPs5Sdk As String
        Public TargetPs4Sdk As String
        Public FilesScanned As Integer
        Public FilesPatched As Integer
        Public FilesSkipped As Integer
        Public FilesFailed As Integer
        Public BackupCreated As Boolean
        Public BackupPath As String
        Public VerificationPassed As Boolean
        Public Success As Boolean
        Public ErrorMessage As String
        Public PatchedFilesList As List(Of String)
        Public SkippedFilesList As List(Of String)
        Public FailedFilesList As List(Of String)
    End Structure

    ''' <summary>
    ''' Generate comprehensive operation report
    ''' </summary>
    Public Function GenerateReport(operation As PatchOperation) As String
        Dim sb As New StringBuilder()

        ' Header
        sb.AppendLine("+=======================================================================+")
        sb.AppendLine($"| {APP_NAME} - Operation Report".PadRight(71) & "|")
        sb.AppendLine($"| Version {APP_VERSION}".PadRight(71) & "|")
        sb.AppendLine("+=======================================================================+")
        sb.AppendLine()

        ' Operation Summary
        sb.AppendLine("=== OPERATION SUMMARY ===")
        sb.AppendLine($"Status: {If(operation.Success, "+ SUCCESS", "x FAILED")}")
        sb.AppendLine($"Start Time: {operation.StartTime:yyyy-MM-dd HH:mm:ss}")
        sb.AppendLine($"End Time: {operation.EndTime:yyyy-MM-dd HH:mm:ss}")
        sb.AppendLine($"Duration: {operation.Duration.TotalSeconds:F2}s")
        sb.AppendLine()

        ' Configuration
        sb.AppendLine("=== CONFIGURATION ===")
        sb.AppendLine($"Game Folder: {operation.GameFolder}")
        sb.AppendLine($"Target PS5 SDK: {operation.TargetPs5Sdk}")
        sb.AppendLine($"Target PS4 SDK: {operation.TargetPs4Sdk}")
        sb.AppendLine($"Backup Created: {If(operation.BackupCreated, "Yes", "No")}")
        If operation.BackupCreated Then
            sb.AppendLine($"Backup Location: {operation.BackupPath}")
        End If
        sb.AppendLine()

        ' Statistics
        sb.AppendLine("=== STATISTICS ===")
        sb.AppendLine($"Files Scanned: {operation.FilesScanned}")
        sb.AppendLine($"Files Patched: {operation.FilesPatched}")
        sb.AppendLine($"Files Skipped: {operation.FilesSkipped}")
        sb.AppendLine($"Files Failed: {operation.FilesFailed}")

        If operation.FilesScanned > 0 Then
            Dim successRate = (operation.FilesPatched * 100.0) / operation.FilesScanned
            sb.AppendLine($"Success Rate: {successRate:F1}%")
        End If
        sb.AppendLine()

        ' Patched Files
        If operation.PatchedFilesList IsNot Nothing AndAlso operation.PatchedFilesList.Count > 0 Then
            sb.AppendLine("=== PATCHED FILES ===")
            For Each file In operation.PatchedFilesList
                sb.AppendLine($"  + {file}")
            Next
            sb.AppendLine()
        End If

        ' Skipped Files
        If operation.SkippedFilesList IsNot Nothing AndAlso operation.SkippedFilesList.Count > 0 Then
            sb.AppendLine("=== SKIPPED FILES ===")
            For Each file In operation.SkippedFilesList
                sb.AppendLine($"  ○ {file}")
            Next
            sb.AppendLine()
        End If

        ' Failed Files
        If operation.FailedFilesList IsNot Nothing AndAlso operation.FailedFilesList.Count > 0 Then
            sb.AppendLine("=== FAILED FILES ===")
            For Each file In operation.FailedFilesList
                sb.AppendLine($"  x {file}")
            Next
            sb.AppendLine()
        End If

        ' Verification
        sb.AppendLine("=== VERIFICATION ===")
        sb.AppendLine($"Integrity Check: {If(operation.VerificationPassed, "+ PASSED", "x FAILED")}")
        sb.AppendLine()

        ' Errors
        If Not String.IsNullOrEmpty(operation.ErrorMessage) Then
            sb.AppendLine("=== ERRORS ===")
            sb.AppendLine(operation.ErrorMessage)
            sb.AppendLine()
        End If

        ' Footer
        sb.AppendLine("=======================================================================")
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
        sb.AppendLine("=======================================================================")

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Export report to file (TXT format)
    ''' </summary>
    Public Function ExportToText(operation As PatchOperation, outputPath As String) As Boolean
        Try
            Dim report = GenerateReport(operation)
            File.WriteAllText(outputPath, report, Encoding.UTF8)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Export report to JSON format
    ''' </summary>
    Public Function ExportToJson(operation As PatchOperation, outputPath As String) As Boolean
        Try
            Dim jsonData = New With {
                .AppName = APP_NAME,
                .AppVersion = APP_VERSION,
                .ReportGenerated = DateTime.Now,
                .Operation = operation
            }

            Dim json = JsonConvert.SerializeObject(jsonData, Formatting.Indented)
            File.WriteAllText(outputPath, json, Encoding.UTF8)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Generate filename for report
    ''' </summary>
    Public Function GenerateReportFileName(extension As String) As String
        Return $"BackPork_Report_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}"
    End Function

End Module