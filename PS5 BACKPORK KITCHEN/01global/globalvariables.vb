Imports System.IO

''' <summary>
''' LEGACY MODULE - Gradual deprecation in progress
'''
''' NEW ARCHITECTURE ALTERNATIVES:
''' - For operation state: Use Architecture.Application.Context.OperationContext
''' - For configuration: Use Architecture.Infrastructure.Adapters.IConfigurationStore
''' - For patching: Use Architecture.Application.Coordinators.PatchingCoordinator
'''
''' KEEPING FOR NOW:
''' - Constants (APP_NAME, URLs, file extensions) - These are fine
''' - Used by legacy code (batch processing, etc.) - To be migrated gradually
'''
''' MIGRATION STATUS:
''' - Main patching flow (BtnStart_Click): ✓ MIGRATED to new architecture
''' - Batch processing: TODO - Still uses legacy
''' - Other features: TODO - To be reviewed
''' </summary>
Module globalvariables
    Public mytwitter As String = "https://x.com/rajeshca911"
    Public myTelegram As String = "https://t.me/+Wt7jrpth4DplNDg0"

    'github links
    Public backpork As String = "https://github.com/BestPig/BackPork"

    Public selfutilurl As String = "https://github.com/CyB1K/SelfUtil-Patched"
    Public Idlesauce As String = "https://gist.github.com/idlesauce/2ded24b7b5ff296f21792a8202542aaa"
    Public makeself As String = "https://github.com/ps5-payload-dev/sdk/blob/master/samples/install_app/make_fself.py"
    Public fakelibJsonUrl As String = "https://raw.githubusercontent.com/rajeshca911/PS5-BACKPORK-KITCHEN/refs/heads/main/fakelibs.json"
    Public selectedfolder As String = ""
    Public selfutilpath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SelfUtil")
    Public backupDir As String = ""
    Public skippedcount As Integer = 0
    Public patchedcount As Integer = 0
    Public gamename As String = ""

    'Public tempfolder As String = ""
    Public fwMajor As Integer = 7

    Public k1 As String = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"
    Public k2 As String = "eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJ1d2Nld2xrcmp5aWx0Z251ZGFmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTY0ODAzNTksImV4cCI6MjA3MjA1NjM1OX0"
    Public k3 As String = "5JpzjmMPB190xJjTgsn8OqOCnyASvz6osl4Nbj9PVT0"

    ' Application constants
    Public Const APP_NAME As String = "PS5 BackPork Kitchen"

    Public Const APP_VERSION As String = "1.2.0"
    Public Const LOG_FILENAME As String = "backpork_log.txt"
    Public Const MAX_LOG_FILE_SIZE As Long = 10 * 1024 * 1024 ' 10MB

    ' Backup constants
    Public Const BACKUP_PREFIX As String = "Backup-"

    Public Const BACKUP_DATE_FORMAT As String = "dd-MMM-yy-HH-mm-ss"

    ' Temp folder constants
    Public Const TEMP_FOLDER_NAME As String = "temp_unpack"

    Public Const FAKELIB_FOLDER_NAME As String = "fakelib"

    ' File extensions
    Public ReadOnly PATCHABLE_EXTENSIONS As String() = {".prx", ".sprx"}

    Public Const EBOOT_FILENAME As String = "eboot.bin"

    ' Runtime Paths
    Public SelectedGameFolder As String = ""

    Public BackupDirectory As String = ""
    Public TempUnpackFolder As String = ""

    ' Statistics
    Public SkippedFilesCount As Integer = 0

    Public PatchedFilesCount As Integer = 0
    Public FailedFilesCount As Integer = 0

    ' Operation tracking
    Public CurrentOperationStartTime As DateTime

    Public PatchedFilesList As New List(Of String)
    Public SkippedFilesList As New List(Of String)
    Public FailedFilesList As New List(Of String)

    ''' <summary>
    ''' Get or create temp unpacking folder
    ''' </summary>
    Public Function GetTempFolder() As String
        If String.IsNullOrEmpty(Form1.Txtpath.Text) Then
            Return ""
        End If

        Dim tempFolder As String = Path.Combine(Form1.Txtpath.Text, TEMP_FOLDER_NAME)

        If Not Directory.Exists(tempFolder) Then
            Try
                Directory.CreateDirectory(tempFolder)
            Catch ex As Exception
                Logger.Log(Form1.rtbStatus, $"Failed to create temp folder: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
                Return ""
            End Try
        End If

        TempUnpackFolder = tempFolder
        Return tempFolder
    End Function

    ''' <summary>
    ''' Reset operation statistics
    ''' </summary>
    Public Sub ResetOperationStats()
        SkippedFilesCount = 0
        PatchedFilesCount = 0
        FailedFilesCount = 0
        PatchedFilesList.Clear()
        SkippedFilesList.Clear()
        FailedFilesList.Clear()
        CurrentOperationStartTime = DateTime.Now
    End Sub

    ''' <summary>
    ''' Increment patched count and track file
    ''' </summary>
    Public Sub IncrementPatchedCount(fileName As String)
        PatchedFilesCount += 1
        PatchedFilesList.Add(fileName)
    End Sub

    ''' <summary>
    ''' Increment skipped count and track file
    ''' </summary>
    Public Sub IncrementSkippedCount(fileName As String)
        SkippedFilesCount += 1
        SkippedFilesList.Add(fileName)
    End Sub

    ''' <summary>
    ''' Increment failed count and track file
    ''' </summary>
    Public Sub IncrementFailedCount(fileName As String)
        FailedFilesCount += 1
        FailedFilesList.Add(fileName)
    End Sub

    'Public Function GetTempFolder() As String
    '    If Form1.Txtpath.Text = "" Then
    '        Return ""
    '    End If
    '    Dim tempfolder As String = Path.Combine(Form1.Txtpath.Text, "temp_unpack")
    '    If Not Directory.Exists(tempfolder) Then
    '        Directory.CreateDirectory(tempfolder)
    '    End If
    '    Return tempfolder
    'End Function

End Module