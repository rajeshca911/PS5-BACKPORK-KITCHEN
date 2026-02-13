Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' Unified interface for application configuration access
    ''' </summary>
    Public Interface IConfigurationStore
        ' SDK Settings
        Function GetDefaultPs5SdkAsync() As Task(Of Long)
        Function SetDefaultPs5SdkAsync(sdk As Long) As Task
        Function GetDefaultPs4SdkAsync() As Task(Of Long)
        Function SetDefaultPs4SdkAsync(sdk As Long) As Task

        ' Application Settings
        Function GetAutoBackupAsync() As Task(Of Boolean)
        Function SetAutoBackupAsync(enabled As Boolean) As Task
        Function GetAutoVerifyAsync() As Task(Of Boolean)
        Function SetAutoVerifyAsync(enabled As Boolean) As Task
        Function GetEnableFileLoggingAsync() As Task(Of Boolean)
        Function SetEnableFileLoggingAsync(enabled As Boolean) As Task
        Function GetLogLevelAsync() As Task(Of String)
        Function SetLogLevelAsync(level As String) As Task

        ' UI Settings
        Function GetThemeAsync() As Task(Of String)
        Function SetThemeAsync(theme As String) As Task

        ' Paths
        Function GetLastUsedFolderAsync() As Task(Of String)
        Function SetLastUsedFolderAsync(folder As String) As Task

        ' Updates
        Function GetCheckForUpdatesOnStartupAsync() As Task(Of Boolean)
        Function SetCheckForUpdatesOnStartupAsync(enabled As Boolean) As Task
        Function GetLastUpdateCheckAsync() As Task(Of DateTime)
        Function SetLastUpdateCheckAsync(lastCheck As DateTime) As Task
        Function GetSkippedUpdateVersionAsync() As Task(Of String)
        Function SetSkippedUpdateVersionAsync(version As String) As Task

        ' Export
        Function GetExportReportsAutomaticallyAsync() As Task(Of Boolean)
        Function SetExportReportsAutomaticallyAsync(enabled As Boolean) As Task
        Function GetReportOutputDirectoryAsync() As Task(Of String)
        Function SetReportOutputDirectoryAsync(directory As String) As Task

        ' Language & Localization
        Function GetLanguageAsync() As Task(Of String)
        Function SetLanguageAsync(language As String) As Task

        ' Advanced
        Function GetMaxConcurrentOperationsAsync() As Task(Of Integer)
        Function SetMaxConcurrentOperationsAsync(max As Integer) As Task
        Function GetTempFolderCleanupOnExitAsync() As Task(Of Boolean)
        Function SetTempFolderCleanupOnExitAsync(enabled As Boolean) As Task

        ' Bulk Operations
        Function GetConfigurationAsync() As Task(Of Domain.Models.AppConfiguration)
        Function SetConfigurationAsync(config As Domain.Models.AppConfiguration) As Task

        ' Persistence
        Function SaveAsync() As Task
        Function ReloadAsync() As Task
    End Interface
End Namespace
