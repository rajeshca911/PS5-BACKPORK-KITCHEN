Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Services
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Coordinators

Namespace Architecture.Composition
    ''' <summary>
    ''' Poor Man's DI: creates and wires all dependencies
    ''' </summary>
    Public Class CompositionRoot
        Private Shared _instance As CompositionRoot
        Private Shared ReadOnly _lock As New Object()

        ' Singletons
        Private ReadOnly _fileSystem As IFileSystem
        Private ReadOnly _logger As ILogger
        Private ReadOnly _configStore As IConfigurationStore

        Private Sub New()
            ' Infrastructure adapters (singletons)
            _fileSystem = New FileSystemAdapter()
            _logger = New LoggerAdapter()

            Dim configPath = IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "backpork_config.json")
            _configStore = New JsonConfigurationStore(configPath)

            ' Log initialization
            _logger.LogInfo("CompositionRoot initialized")
        End Sub

        Public Shared Function Instance() As CompositionRoot
            SyncLock _lock
                If _instance Is Nothing Then
                    _instance = New CompositionRoot()
                End If
                Return _instance
            End SyncLock
        End Function

        ''' <summary>
        ''' Creates a new PatchingCoordinator with all dependencies wired
        ''' </summary>
        Public Function CreatePatchingCoordinator() As PatchingCoordinator
            ' Domain services (transient - new instance each time)
            Dim elfService As IElfPatchingService = New ElfPatchingService(_fileSystem, _logger)
            Dim backupService As IBackupService = New BackupServiceAdapter(_fileSystem, _logger)

            ' Application coordinator
            Return New PatchingCoordinator(elfService, backupService, _fileSystem, _logger)
        End Function

        ''' <summary>
        ''' Creates a new BatchPatchingCoordinator with all dependencies wired
        ''' </summary>
        Public Function CreateBatchPatchingCoordinator() As BatchPatchingCoordinator
            Dim patchingCoordinator = CreatePatchingCoordinator()
            Return New BatchPatchingCoordinator(patchingCoordinator, _fileSystem, _logger)
        End Function

        ''' <summary>
        ''' Creates a new RestoreCoordinator with all dependencies wired
        ''' </summary>
        Public Function CreateRestoreCoordinator() As RestoreCoordinator
            Dim backupService As IBackupService = New BackupServiceAdapter(_fileSystem, _logger)
            Return New RestoreCoordinator(backupService, _fileSystem, _logger)
        End Function

        ''' <summary>
        ''' Gets the shared file system adapter
        ''' </summary>
        Public Function GetFileSystem() As IFileSystem
            Return _fileSystem
        End Function

        ''' <summary>
        ''' Gets the shared logger
        ''' </summary>
        Public Function GetLogger() As ILogger
            Return _logger
        End Function

        ''' <summary>
        ''' Gets the shared configuration store
        ''' </summary>
        Public Function GetConfigurationStore() As IConfigurationStore
            Return _configStore
        End Function

        ''' <summary>
        ''' Releases resources (call on application shutdown)
        ''' </summary>
        Public Shared Sub Shutdown()
            SyncLock _lock
                If _instance IsNot Nothing Then
                    _instance._logger.LogInfo("CompositionRoot shutting down")
                    _instance = Nothing
                End If
            End SyncLock
        End Sub
    End Class
End Namespace
