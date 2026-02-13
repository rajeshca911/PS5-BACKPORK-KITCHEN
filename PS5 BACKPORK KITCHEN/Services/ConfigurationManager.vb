Imports System.IO
Imports Newtonsoft.Json
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models

''' <summary>
''' LEGACY WRAPPER - Synchronous configuration management
''' Uses direct file I/O instead of async ConfigStore to avoid UI thread deadlocks
''' </summary>
Public Module ConfigurationManager

    ' Legacy structure definition (kept for backward compatibility)
    Public Structure AppConfig
        Public DefaultPs5Sdk As UInteger
        Public DefaultPs4Sdk As UInteger
        Public AutoBackup As Boolean
        Public AutoVerify As Boolean
        Public EnableFileLogging As Boolean
        Public LogLevel As String
        Public LastUsedFolder As String
        Public ExportReportsAutomatically As Boolean
        Public Theme As String
        Public CheckForUpdatesOnStartup As Boolean
        Public LastUpdateCheck As DateTime
        Public SkippedUpdateVersion As String
    End Structure

    Private ReadOnly ConfigFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backpork_config.json")
    Private _currentConfig As AppConfig

    ''' <summary>
    ''' Load configuration from file or create default
    ''' SYNCHRONOUS - safe for UI thread
    ''' </summary>
    Public Function LoadConfiguration() As AppConfig
        Try
            If File.Exists(ConfigFilePath) Then
                Dim json = File.ReadAllText(ConfigFilePath)
                Dim domainConfig = JsonConvert.DeserializeObject(Of AppConfiguration)(json)

                If domainConfig Is Nothing Then
                    _currentConfig = GetDefaultConfiguration()
                Else
                    _currentConfig = ConvertToLegacy(domainConfig)
                End If
            Else
                _currentConfig = GetDefaultConfiguration()
                SaveConfiguration(_currentConfig)
            End If
        Catch ex As Exception
            _currentConfig = GetDefaultConfiguration()
        End Try

        Return _currentConfig
    End Function

    ''' <summary>
    ''' Save configuration to file
    ''' SYNCHRONOUS - safe for UI thread
    ''' </summary>
    Public Function SaveConfiguration(config As AppConfig) As Boolean
        Try
            Dim domainConfig = ConvertFromLegacy(config)
            Dim json = JsonConvert.SerializeObject(domainConfig, Formatting.Indented)
            File.WriteAllText(ConfigFilePath, json)
            _currentConfig = config
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Get default configuration
    ''' </summary>
    Public Function GetDefaultConfiguration() As AppConfig
        Return New AppConfig With {
            .DefaultPs5Sdk = &H7000038UI,
            .DefaultPs4Sdk = 0,
            .AutoBackup = True,
            .AutoVerify = True,
            .EnableFileLogging = True,
            .LogLevel = "Info",
            .LastUsedFolder = "",
            .ExportReportsAutomatically = False,
            .Theme = "Default",
            .CheckForUpdatesOnStartup = True,
            .LastUpdateCheck = DateTime.MinValue,
            .SkippedUpdateVersion = ""
        }
    End Function

    ''' <summary>
    ''' Get current configuration
    ''' </summary>
    Public Function GetCurrentConfiguration() As AppConfig
        Return _currentConfig
    End Function

    ''' <summary>
    ''' Update specific setting
    ''' SYNCHRONOUS - safe for UI thread
    ''' </summary>
    Public Function UpdateSetting(key As String, value As Object) As Boolean
        Try
            Select Case key.ToLower()
                Case "defaultps5sdk"
                    _currentConfig.DefaultPs5Sdk = CUInt(value)
                Case "defaultps4sdk"
                    _currentConfig.DefaultPs4Sdk = CUInt(value)
                Case "autobackup"
                    _currentConfig.AutoBackup = CBool(value)
                Case "autoverify"
                    _currentConfig.AutoVerify = CBool(value)
                Case "enablefilelogging"
                    _currentConfig.EnableFileLogging = CBool(value)
                Case "loglevel"
                    _currentConfig.LogLevel = CStr(value)
                Case "lastusedfolder"
                    _currentConfig.LastUsedFolder = CStr(value)
                Case "exportreportsautomatically"
                    _currentConfig.ExportReportsAutomatically = CBool(value)
                Case "theme"
                    _currentConfig.Theme = CStr(value)
                Case "checkforupdatesonstartup"
                    _currentConfig.CheckForUpdatesOnStartup = CBool(value)
                Case "lastupdatecheck"
                    _currentConfig.LastUpdateCheck = CDate(value)
                Case "skippedupdateversion"
                    _currentConfig.SkippedUpdateVersion = CStr(value)
                Case Else
                    Return False
            End Select

            Return SaveConfiguration(_currentConfig)
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Reset to default configuration
    ''' </summary>
    Public Function ResetToDefaults() As Boolean
        Try
            _currentConfig = GetDefaultConfiguration()
            Return SaveConfiguration(_currentConfig)
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Export configuration to backup file
    ''' </summary>
    Public Function ExportConfiguration(destinationPath As String) As Boolean
        Try
            If File.Exists(ConfigFilePath) Then
                File.Copy(ConfigFilePath, destinationPath, True)
                Return True
            End If
            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Import configuration from file
    ''' </summary>
    Public Function ImportConfiguration(sourcePath As String) As Boolean
        Try
            If Not File.Exists(sourcePath) Then Return False

            Dim json = File.ReadAllText(sourcePath)
            Dim importedConfig = JsonConvert.DeserializeObject(Of AppConfiguration)(json)

            _currentConfig = ConvertToLegacy(importedConfig)
            Return SaveConfiguration(_currentConfig)
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Convert new architecture config to legacy format
    ''' </summary>
    Private Function ConvertToLegacy(config As AppConfiguration) As AppConfig
        Return New AppConfig With {
            .DefaultPs5Sdk = CUInt(config.DefaultPs5Sdk),
            .DefaultPs4Sdk = CUInt(config.DefaultPs4Sdk),
            .AutoBackup = config.AutoBackup,
            .AutoVerify = config.AutoVerify,
            .EnableFileLogging = config.EnableFileLogging,
            .LogLevel = config.LogLevel,
            .LastUsedFolder = config.LastUsedFolder,
            .ExportReportsAutomatically = config.ExportReportsAutomatically,
            .Theme = config.Theme,
            .CheckForUpdatesOnStartup = config.CheckForUpdatesOnStartup,
            .LastUpdateCheck = config.LastUpdateCheck,
            .SkippedUpdateVersion = config.SkippedUpdateVersion
        }
    End Function

    ''' <summary>
    ''' Convert legacy config to new architecture format
    ''' </summary>
    Private Function ConvertFromLegacy(legacyConfig As AppConfig) As AppConfiguration
        Return New AppConfiguration With {
            .DefaultPs5Sdk = CLng(legacyConfig.DefaultPs5Sdk),
            .DefaultPs4Sdk = CLng(legacyConfig.DefaultPs4Sdk),
            .AutoBackup = legacyConfig.AutoBackup,
            .AutoVerify = legacyConfig.AutoVerify,
            .EnableFileLogging = legacyConfig.EnableFileLogging,
            .LogLevel = legacyConfig.LogLevel,
            .LastUsedFolder = legacyConfig.LastUsedFolder,
            .ExportReportsAutomatically = legacyConfig.ExportReportsAutomatically,
            .Theme = legacyConfig.Theme,
            .CheckForUpdatesOnStartup = legacyConfig.CheckForUpdatesOnStartup,
            .LastUpdateCheck = legacyConfig.LastUpdateCheck,
            .SkippedUpdateVersion = legacyConfig.SkippedUpdateVersion,
            .Language = "en",
            .ReportOutputDirectory = String.Empty,
            .MaxConcurrentOperations = 4,
            .TempFolderCleanupOnExit = True
        }
    End Function

End Module
