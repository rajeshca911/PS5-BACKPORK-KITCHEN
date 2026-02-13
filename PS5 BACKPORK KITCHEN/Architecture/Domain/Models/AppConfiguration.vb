Namespace Architecture.Domain.Models
    ''' <summary>
    ''' Application configuration settings
    ''' </summary>
    Public Class AppConfiguration
        ' Patching defaults
        Public Property DefaultPs5Sdk As Long
        Public Property DefaultPs4Sdk As Long
        Public Property AutoBackup As Boolean
        Public Property AutoVerify As Boolean

        ' Logging
        Public Property EnableFileLogging As Boolean
        Public Property LogLevel As String

        ' UI/UX
        Public Property LastUsedFolder As String
        Public Property Theme As String
        Public Property Language As String

        ' Reporting
        Public Property ExportReportsAutomatically As Boolean
        Public Property ReportOutputDirectory As String

        ' Updates
        Public Property CheckForUpdatesOnStartup As Boolean
        Public Property LastUpdateCheck As DateTime
        Public Property SkippedUpdateVersion As String

        ' Advanced
        Public Property MaxConcurrentOperations As Integer
        Public Property TempFolderCleanupOnExit As Boolean

        Public Shared Function CreateDefault() As AppConfiguration
            Return New AppConfiguration With {
                .DefaultPs5Sdk = &H7000038L,
                .DefaultPs4Sdk = 0,
                .AutoBackup = True,
                .AutoVerify = True,
                .EnableFileLogging = True,
                .LogLevel = "Info",
                .LastUsedFolder = String.Empty,
                .Theme = "Default",
                .Language = "en",
                .ExportReportsAutomatically = False,
                .ReportOutputDirectory = String.Empty,
                .CheckForUpdatesOnStartup = True,
                .LastUpdateCheck = DateTime.MinValue,
                .SkippedUpdateVersion = String.Empty,
                .MaxConcurrentOperations = 4,
                .TempFolderCleanupOnExit = True
            }
        End Function

        ''' <summary>
        ''' Validate configuration values
        ''' </summary>
        Public Function Validate() As (IsValid As Boolean, Errors As List(Of String))
            Dim errors As New List(Of String)

            If DefaultPs5Sdk < 0 Then
                errors.Add("DefaultPs5Sdk must be non-negative")
            End If

            If DefaultPs4Sdk < 0 Then
                errors.Add("DefaultPs4Sdk must be non-negative")
            End If

            If Not {"Trace", "Debug", "Info", "Warning", "Error"}.Contains(LogLevel) Then
                errors.Add("Invalid LogLevel. Must be: Trace, Debug, Info, Warning, or Error")
            End If

            If MaxConcurrentOperations < 1 OrElse MaxConcurrentOperations > 16 Then
                errors.Add("MaxConcurrentOperations must be between 1 and 16")
            End If

            Return (errors.Count = 0, errors)
        End Function
    End Class
End Namespace
