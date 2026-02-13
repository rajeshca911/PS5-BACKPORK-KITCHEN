Imports Newtonsoft.Json
Imports System.IO
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models

Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' JSON-based configuration store implementation
    ''' </summary>
    Public Class JsonConfigurationStore
        Implements IConfigurationStore

        Private _config As AppConfiguration
        Private ReadOnly _filePath As String
        Private ReadOnly _lock As New Object()

        Public Sub New(filePath As String)
            _filePath = filePath
            LoadConfig()
        End Sub

        Private Sub LoadConfig()
            SyncLock _lock
                If File.Exists(_filePath) Then
                    Try
                        Dim json = File.ReadAllText(_filePath)
                        _config = JsonConvert.DeserializeObject(Of AppConfiguration)(json)

                        ' Ensure config is valid - if null or missing required properties, use defaults
                        If _config Is Nothing Then
                            _config = AppConfiguration.CreateDefault()
                        End If

                        ' Validate and fix any missing/invalid properties
                        If String.IsNullOrEmpty(_config.LogLevel) Then _config.LogLevel = "Info"
                        If String.IsNullOrEmpty(_config.Theme) Then _config.Theme = "Default"
                        If String.IsNullOrEmpty(_config.Language) Then _config.Language = "en"
                        If _config.MaxConcurrentOperations < 1 Then _config.MaxConcurrentOperations = 4

                    Catch ex As Exception
                        ' Fallback to defaults if config corrupted or format incompatible
                        _config = AppConfiguration.CreateDefault()

                        ' Try to save defaults to fix the corrupted file
                        Try
                            Dim json = JsonConvert.SerializeObject(_config, Formatting.Indented)
                            File.WriteAllText(_filePath, json)
                        Catch
                            ' Ignore save errors
                        End Try
                    End Try
                Else
                    ' Create default config and save it
                    _config = AppConfiguration.CreateDefault()
                    Try
                        Dim json = JsonConvert.SerializeObject(_config, Formatting.Indented)
                        File.WriteAllText(_filePath, json)
                    Catch
                        ' Ignore save errors
                    End Try
                End If
            End SyncLock
        End Sub

        Public Async Function SaveAsync() As Task Implements IConfigurationStore.SaveAsync
            Await Task.Run(Sub()
                              SyncLock _lock
                                  Dim json = JsonConvert.SerializeObject(_config, Formatting.Indented)
                                  File.WriteAllText(_filePath, json)
                              End SyncLock
                          End Sub)
        End Function

        Public Function ReloadAsync() As Task Implements IConfigurationStore.ReloadAsync
            Return Task.Run(Sub() LoadConfig())
        End Function

        ' SDK Settings
        Public Function GetDefaultPs5SdkAsync() As Task(Of Long) Implements IConfigurationStore.GetDefaultPs5SdkAsync
            Return Task.FromResult(Of Long)(_config.DefaultPs5Sdk)
        End Function

        Public Function SetDefaultPs5SdkAsync(sdk As Long) As Task Implements IConfigurationStore.SetDefaultPs5SdkAsync
            _config.DefaultPs5Sdk = sdk
            Return Task.CompletedTask
        End Function

        Public Function GetDefaultPs4SdkAsync() As Task(Of Long) Implements IConfigurationStore.GetDefaultPs4SdkAsync
            Return Task.FromResult(Of Long)(_config.DefaultPs4Sdk)
        End Function

        Public Function SetDefaultPs4SdkAsync(sdk As Long) As Task Implements IConfigurationStore.SetDefaultPs4SdkAsync
            _config.DefaultPs4Sdk = sdk
            Return Task.CompletedTask
        End Function

        ' Application Settings
        Public Function GetAutoBackupAsync() As Task(Of Boolean) Implements IConfigurationStore.GetAutoBackupAsync
            Return Task.FromResult(Of Boolean)(_config.AutoBackup)
        End Function

        Public Function SetAutoBackupAsync(enabled As Boolean) As Task Implements IConfigurationStore.SetAutoBackupAsync
            _config.AutoBackup = enabled
            Return Task.CompletedTask
        End Function

        Public Function GetAutoVerifyAsync() As Task(Of Boolean) Implements IConfigurationStore.GetAutoVerifyAsync
            Return Task.FromResult(Of Boolean)(_config.AutoVerify)
        End Function

        Public Function SetAutoVerifyAsync(enabled As Boolean) As Task Implements IConfigurationStore.SetAutoVerifyAsync
            _config.AutoVerify = enabled
            Return Task.CompletedTask
        End Function

        Public Function GetEnableFileLoggingAsync() As Task(Of Boolean) Implements IConfigurationStore.GetEnableFileLoggingAsync
            Return Task.FromResult(Of Boolean)(_config.EnableFileLogging)
        End Function

        Public Function SetEnableFileLoggingAsync(enabled As Boolean) As Task Implements IConfigurationStore.SetEnableFileLoggingAsync
            _config.EnableFileLogging = enabled
            Return Task.CompletedTask
        End Function

        Public Function GetLogLevelAsync() As Task(Of String) Implements IConfigurationStore.GetLogLevelAsync
            Return Task.FromResult(Of String)(_config.LogLevel)
        End Function

        Public Function SetLogLevelAsync(level As String) As Task Implements IConfigurationStore.SetLogLevelAsync
            _config.LogLevel = level
            Return Task.CompletedTask
        End Function

        ' UI Settings
        Public Function GetThemeAsync() As Task(Of String) Implements IConfigurationStore.GetThemeAsync
            Return Task.FromResult(Of String)(_config.Theme)
        End Function

        Public Function SetThemeAsync(theme As String) As Task Implements IConfigurationStore.SetThemeAsync
            _config.Theme = theme
            Return Task.CompletedTask
        End Function

        ' Paths
        Public Function GetLastUsedFolderAsync() As Task(Of String) Implements IConfigurationStore.GetLastUsedFolderAsync
            Return Task.FromResult(Of String)(_config.LastUsedFolder)
        End Function

        Public Function SetLastUsedFolderAsync(folder As String) As Task Implements IConfigurationStore.SetLastUsedFolderAsync
            _config.LastUsedFolder = folder
            Return Task.CompletedTask
        End Function

        ' Updates
        Public Function GetCheckForUpdatesOnStartupAsync() As Task(Of Boolean) Implements IConfigurationStore.GetCheckForUpdatesOnStartupAsync
            Return Task.FromResult(Of Boolean)(_config.CheckForUpdatesOnStartup)
        End Function

        Public Function SetCheckForUpdatesOnStartupAsync(enabled As Boolean) As Task Implements IConfigurationStore.SetCheckForUpdatesOnStartupAsync
            _config.CheckForUpdatesOnStartup = enabled
            Return Task.CompletedTask
        End Function

        Public Function GetLastUpdateCheckAsync() As Task(Of DateTime) Implements IConfigurationStore.GetLastUpdateCheckAsync
            Return Task.FromResult(Of DateTime)(_config.LastUpdateCheck)
        End Function

        Public Function SetLastUpdateCheckAsync(lastCheck As DateTime) As Task Implements IConfigurationStore.SetLastUpdateCheckAsync
            _config.LastUpdateCheck = lastCheck
            Return Task.CompletedTask
        End Function

        Public Function GetSkippedUpdateVersionAsync() As Task(Of String) Implements IConfigurationStore.GetSkippedUpdateVersionAsync
            Return Task.FromResult(Of String)(_config.SkippedUpdateVersion)
        End Function

        Public Function SetSkippedUpdateVersionAsync(version As String) As Task Implements IConfigurationStore.SetSkippedUpdateVersionAsync
            _config.SkippedUpdateVersion = version
            Return Task.CompletedTask
        End Function

        Public Function GetExportReportsAutomaticallyAsync() As Task(Of Boolean) Implements IConfigurationStore.GetExportReportsAutomaticallyAsync
            Return Task.FromResult(Of Boolean)(_config.ExportReportsAutomatically)
        End Function

        Public Function SetExportReportsAutomaticallyAsync(enabled As Boolean) As Task Implements IConfigurationStore.SetExportReportsAutomaticallyAsync
            _config.ExportReportsAutomatically = enabled
            Return Task.CompletedTask
        End Function

        Public Function GetReportOutputDirectoryAsync() As Task(Of String) Implements IConfigurationStore.GetReportOutputDirectoryAsync
            Return Task.FromResult(Of String)(_config.ReportOutputDirectory)
        End Function

        Public Function SetReportOutputDirectoryAsync(directory As String) As Task Implements IConfigurationStore.SetReportOutputDirectoryAsync
            _config.ReportOutputDirectory = directory
            Return Task.CompletedTask
        End Function

        ' Language
        Public Function GetLanguageAsync() As Task(Of String) Implements IConfigurationStore.GetLanguageAsync
            Return Task.FromResult(Of String)(_config.Language)
        End Function

        Public Function SetLanguageAsync(language As String) As Task Implements IConfigurationStore.SetLanguageAsync
            _config.Language = language
            Return Task.CompletedTask
        End Function

        ' Advanced
        Public Function GetMaxConcurrentOperationsAsync() As Task(Of Integer) Implements IConfigurationStore.GetMaxConcurrentOperationsAsync
            Return Task.FromResult(Of Integer)(_config.MaxConcurrentOperations)
        End Function

        Public Function SetMaxConcurrentOperationsAsync(max As Integer) As Task Implements IConfigurationStore.SetMaxConcurrentOperationsAsync
            _config.MaxConcurrentOperations = max
            Return Task.CompletedTask
        End Function

        Public Function GetTempFolderCleanupOnExitAsync() As Task(Of Boolean) Implements IConfigurationStore.GetTempFolderCleanupOnExitAsync
            Return Task.FromResult(Of Boolean)(_config.TempFolderCleanupOnExit)
        End Function

        Public Function SetTempFolderCleanupOnExitAsync(enabled As Boolean) As Task Implements IConfigurationStore.SetTempFolderCleanupOnExitAsync
            _config.TempFolderCleanupOnExit = enabled
            Return Task.CompletedTask
        End Function

        ' Bulk Operations
        Public Function GetConfigurationAsync() As Task(Of AppConfiguration) Implements IConfigurationStore.GetConfigurationAsync
            Return Task.FromResult(_config)
        End Function

        Public Function SetConfigurationAsync(config As AppConfiguration) As Task Implements IConfigurationStore.SetConfigurationAsync
            _config = config
            Return Task.CompletedTask
        End Function
    End Class
End Namespace
