Imports System.IO
Imports Newtonsoft.Json

''' <summary>
''' Service for managing custom PS5/PS4 libraries (.prx, .sprx)
''' Allows developers to add, organize, and manage custom libraries by SDK version
''' </summary>
Public Class LibraryManager

    ' Storage paths
    Private Shared ReadOnly AppDataFolder As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PS5BackporkKitchen"
    )

    Private Shared ReadOnly LibrariesFolder As String = Path.Combine(AppDataFolder, "libraries")
    Private Shared ReadOnly ConfigFilePath As String = Path.Combine(AppDataFolder, "custom_libraries.json")

    ''' <summary>
    ''' Represents a custom library entry
    ''' </summary>
    Public Class CustomLibrary
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property FileName As String
        Public Property FilePath As String
        Public Property SdkVersion As Integer
        Public Property LibraryType As String ' "PRX" or "SPRX"
        Public Property Description As String = ""
        Public Property IsEnabled As Boolean = True
        Public Property IsBuiltIn As Boolean = False
        Public Property FileSize As Long
        Public Property AddedDate As DateTime = DateTime.Now
        Public Property LastUsed As DateTime = DateTime.Now
        Public Property Category As String = "Custom" ' Built-in, Custom, System, etc.
    End Class

    ''' <summary>
    ''' Configuration structure for import/export
    ''' </summary>
    Public Class LibraryConfiguration
        Public Property Libraries As List(Of CustomLibrary) = New List(Of CustomLibrary)()
        Public Property ExportDate As DateTime = DateTime.Now
        Public Property ExportVersion As String = "1.0"
        Public Property SdkVersions As List(Of Integer) = New List(Of Integer)()
    End Class

    ''' <summary>
    ''' Initialize the library management system
    ''' </summary>
    Public Shared Sub Initialize()
        Try
            ' Create AppData directories
            If Not Directory.Exists(AppDataFolder) Then
                Directory.CreateDirectory(AppDataFolder)
            End If

            If Not Directory.Exists(LibrariesFolder) Then
                Directory.CreateDirectory(LibrariesFolder)
            End If

            ' Create SDK version subfolders (3-9)
            For sdk As Integer = 3 To 9
                Dim sdkFolder = Path.Combine(LibrariesFolder, sdk.ToString())
                If Not Directory.Exists(sdkFolder) Then
                    Directory.CreateDirectory(sdkFolder)
                End If
            Next

            ' Create config file if it doesn't exist
            If Not File.Exists(ConfigFilePath) Then
                Dim emptyConfig As New LibraryConfiguration()
                SaveConfiguration(emptyConfig)
            End If

            Logger.Log(Form1.rtbStatus, "Library Manager initialized", Color.Green, True, Logger.LogLevel.Info)
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Library Manager initialization error: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Load all libraries (built-in + custom)
    ''' </summary>
    Public Shared Function LoadAllLibraries() As List(Of CustomLibrary)
        Try
            Dim allLibraries As New List(Of CustomLibrary)()

            ' Load built-in libraries from SDK folders
            allLibraries.AddRange(LoadBuiltInLibraries())

            ' Load custom libraries from config
            Dim config = LoadConfiguration()
            allLibraries.AddRange(config.Libraries)

            Return allLibraries
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error loading libraries: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Return New List(Of CustomLibrary)()
        End Try
    End Function

    ''' <summary>
    ''' Load built-in libraries from SDK fakelib folders (SDK 3-9)
    ''' </summary>
    Public Shared Function LoadBuiltInLibraries() As List(Of CustomLibrary)
        Dim builtInLibs As New List(Of CustomLibrary)()

        Try
            Dim baseDir = AppDomain.CurrentDomain.BaseDirectory

            ' Scan SDK folders 3-9
            For sdk As Integer = 3 To 9
                Dim sdkFolder = Path.Combine(baseDir, sdk.ToString(), "fakelib")

                If Directory.Exists(sdkFolder) Then
                    ' Recursively find all .prx and .sprx files
                    Dim libFiles = Directory.GetFiles(sdkFolder, "*.*", SearchOption.AllDirectories) _
                        .Where(Function(f) f.EndsWith(".prx", StringComparison.OrdinalIgnoreCase) OrElse
                                         f.EndsWith(".sprx", StringComparison.OrdinalIgnoreCase)) _
                        .ToList()

                    For Each libFile In libFiles
                        Dim fileInfo As New FileInfo(libFile)
                        Dim library As New CustomLibrary()
                        library.Id = "builtin_" & sdk.ToString() & "_" & Path.GetFileName(libFile)
                        library.FileName = Path.GetFileName(libFile)
                        library.FilePath = libFile
                        library.SdkVersion = sdk
                        library.LibraryType = If(libFile.EndsWith(".sprx", StringComparison.OrdinalIgnoreCase), "SPRX", "PRX")
                        library.Description = "Built-in library"
                        library.IsEnabled = True
                        library.IsBuiltIn = True
                        library.FileSize = fileInfo.Length
                        library.AddedDate = fileInfo.CreationTime
                        library.Category = "Built-in"
                        builtInLibs.Add(library)
                    Next
                End If
            Next
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error loading built-in libraries: " & ex.Message, Color.Orange, True, Logger.LogLevel.Warning)
        End Try

        Return builtInLibs
    End Function

    ''' <summary>
    ''' Add a new custom library
    ''' </summary>
    Public Shared Function AddLibrary(
        sourceFilePath As String,
        sdkVersion As Integer,
        Optional description As String = ""
    ) As CustomLibrary

        Try
            ' Validate input
            If Not File.Exists(sourceFilePath) Then
                Throw New FileNotFoundException("Library file not found: " & sourceFilePath)
            End If

            Dim ext = Path.GetExtension(sourceFilePath).ToLower()
            If ext <> ".prx" AndAlso ext <> ".sprx" Then
                Throw New ArgumentException("Only .prx and .sprx files are supported")
            End If

            If sdkVersion < 3 OrElse sdkVersion > 9 Then
                Throw New ArgumentException("SDK version must be between 3 and 9")
            End If

            ' Create SDK folder if needed
            Dim sdkFolder = Path.Combine(LibrariesFolder, sdkVersion.ToString())
            If Not Directory.Exists(sdkFolder) Then
                Directory.CreateDirectory(sdkFolder)
            End If

            ' Copy library to storage
            Dim fileName = Path.GetFileName(sourceFilePath)
            Dim destPath = Path.Combine(sdkFolder, fileName)

            ' Handle duplicate names
            Dim counter = 1
            While File.Exists(destPath)
                Dim baseName = Path.GetFileNameWithoutExtension(fileName)
                Dim extension = Path.GetExtension(fileName)
                destPath = Path.Combine(sdkFolder, baseName & "_" & counter.ToString() & extension)
                counter += 1
            End While

            File.Copy(sourceFilePath, destPath, overwrite:=False)

            ' Create library entry
            Dim fileInfo As New FileInfo(destPath)
            Dim newLib As New CustomLibrary()
            newLib.Id = Guid.NewGuid().ToString("N")
            newLib.FileName = Path.GetFileName(destPath)
            newLib.FilePath = destPath
            newLib.SdkVersion = sdkVersion
            newLib.LibraryType = If(ext = ".sprx", "SPRX", "PRX")
            newLib.Description = description
            newLib.IsEnabled = True
            newLib.IsBuiltIn = False
            newLib.FileSize = fileInfo.Length
            newLib.AddedDate = DateTime.Now
            newLib.LastUsed = DateTime.Now
            newLib.Category = "Custom"

            ' Save to config
            Dim config = LoadConfiguration()
            config.Libraries.Add(newLib)
            SaveConfiguration(config)

            Logger.Log(Form1.rtbStatus, "Library added: " & newLib.FileName & " (SDK " & sdkVersion.ToString() & ")", Color.Green, True, Logger.LogLevel.Success)

            Return newLib
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error adding library: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Remove a custom library (built-in libraries cannot be removed)
    ''' </summary>
    Public Shared Function RemoveLibrary(libraryId As String) As Boolean
        Try
            Dim config = LoadConfiguration()
            Dim library = config.Libraries.FirstOrDefault(Function(l) l.Id = libraryId)

            If library Is Nothing Then
                Throw New Exception("Library not found")
            End If

            If library.IsBuiltIn Then
                Throw New Exception("Cannot remove built-in libraries")
            End If

            ' Delete file
            If File.Exists(library.FilePath) Then
                File.Delete(library.FilePath)
            End If

            ' Remove from config
            config.Libraries.Remove(library)
            SaveConfiguration(config)

            Logger.Log(Form1.rtbStatus, "Library removed: " & library.FileName, Color.Green, True, Logger.LogLevel.Success)

            Return True
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error removing library: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Toggle library enabled state
    ''' </summary>
    Public Shared Function ToggleLibraryEnabled(libraryId As String, enabled As Boolean) As Boolean
        Try
            Dim config = LoadConfiguration()
            Dim library = config.Libraries.FirstOrDefault(Function(l) l.Id = libraryId)

            If library IsNot Nothing Then
                library.IsEnabled = enabled
                SaveConfiguration(config)
                Logger.Log(Form1.rtbStatus, "Library " & If(enabled, "enabled", "disabled") & ": " & library.FileName, Color.Blue)
                Return True
            End If

            Return False
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error toggling library: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Get libraries for specific SDK version
    ''' </summary>
    Public Shared Function GetLibrariesForSdk(sdkVersion As Integer, Optional includeDisabled As Boolean = False) As List(Of CustomLibrary)
        Try
            Dim allLibs = LoadAllLibraries()
            Return allLibs.Where(Function(l)
                                     l.SdkVersion = sdkVersion AndAlso
                                     (includeDisabled OrElse l.IsEnabled)
                                 End Function).ToList()
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error getting libraries for SDK " & sdkVersion.ToString() & ": " & ex.Message, Color.Red)
            Return New List(Of CustomLibrary)()
        End Try
    End Function

    ''' <summary>
    ''' Export library configuration to JSON file
    ''' </summary>
    Public Shared Function ExportConfiguration(exportPath As String) As Boolean
        Try
            Dim config = LoadConfiguration()
            config.ExportDate = DateTime.Now
            config.ExportVersion = "1.0"

            ' Get unique SDK versions
            config.SdkVersions = config.Libraries.Select(Function(l) l.SdkVersion).Distinct().ToList()

            Dim json = JsonConvert.SerializeObject(config, Formatting.Indented)
            File.WriteAllText(exportPath, json)

            Logger.Log(Form1.rtbStatus, "Configuration exported to: " & exportPath, Color.Green, True, Logger.LogLevel.Success)
            Return True
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error exporting configuration: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Import library configuration from JSON file
    ''' </summary>
    Public Shared Function ImportConfiguration(importPath As String, Optional mergeWithExisting As Boolean = True) As Boolean
        Try
            If Not File.Exists(importPath) Then
                Throw New FileNotFoundException("Import file not found: " & importPath)
            End If

            Dim json = File.ReadAllText(importPath)
            Dim importedConfig = JsonConvert.DeserializeObject(Of LibraryConfiguration)(json)

            If importedConfig Is Nothing OrElse importedConfig.Libraries Is Nothing Then
                Throw New Exception("Invalid configuration file format")
            End If

            Dim currentConfig = If(mergeWithExisting, LoadConfiguration(), New LibraryConfiguration())

            ' Add imported libraries (skip built-in, only import custom)
            Dim importedCount = 0
            For Each library In importedConfig.Libraries.Where(Function(l) Not l.IsBuiltIn)
                ' Check if library file exists
                If File.Exists(library.FilePath) Then
                    ' Check if already exists
                    If Not currentConfig.Libraries.Any(Function(l) l.FilePath = library.FilePath) Then
                        currentConfig.Libraries.Add(library)
                        importedCount += 1
                    End If
                Else
                    Logger.Log(Form1.rtbStatus, "Skipped missing library: " & library.FileName, Color.Orange)
                End If
            Next

            SaveConfiguration(currentConfig)

            Logger.Log(Form1.rtbStatus, "Configuration imported: " & importedCount.ToString() & " libraries added", Color.Green, True, Logger.LogLevel.Success)
            Return True
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error importing configuration: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Update library description
    ''' </summary>
    Public Shared Function UpdateLibraryDescription(libraryId As String, description As String) As Boolean
        Try
            Dim config = LoadConfiguration()
            Dim library = config.Libraries.FirstOrDefault(Function(l) l.Id = libraryId)

            If library IsNot Nothing Then
                library.Description = description
                SaveConfiguration(config)
                Logger.Log(Form1.rtbStatus, "Description updated for: " & library.FileName, Color.Blue)
                Return True
            End If

            Return False
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error updating description: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Load configuration from JSON file
    ''' </summary>
    Private Shared Function LoadConfiguration() As LibraryConfiguration
        Try
            If Not File.Exists(ConfigFilePath) Then
                Return New LibraryConfiguration()
            End If

            Dim json = File.ReadAllText(ConfigFilePath)
            Dim config = JsonConvert.DeserializeObject(Of LibraryConfiguration)(json)

            Return If(config, New LibraryConfiguration())
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error loading library config: " & ex.Message, Color.Orange)
            Return New LibraryConfiguration()
        End Try
    End Function

    ''' <summary>
    ''' Save configuration to JSON file
    ''' </summary>
    Private Shared Sub SaveConfiguration(config As LibraryConfiguration)
        Try
            Dim json = JsonConvert.SerializeObject(config, Formatting.Indented)
            File.WriteAllText(ConfigFilePath, json)
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, "Error saving library config: " & ex.Message, Color.Red, True, Logger.LogLevel.Error)
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Get storage paths for debugging/info
    ''' </summary>
    Public Shared Function GetStorageInfo() As Dictionary(Of String, String)
        Return New Dictionary(Of String, String) From {
            {"AppData", AppDataFolder},
            {"Libraries", LibrariesFolder},
            {"Config", ConfigFilePath}
        }
    End Function

End Class