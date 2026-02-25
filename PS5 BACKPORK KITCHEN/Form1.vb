Imports System.Configuration
Imports System.Globalization
Imports System.IO
Imports System.Runtime
Imports System.Text
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Coordinators
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Services

Public Class Form1

    ' === NEW v1.2.0 FEATURES INTEGRATION ===
    '=== coded by DroneTechTI ===
    Private currentPreset As String = ""

    Private recentFolders As List(Of String) = New List(Of String)

    ' UI Controls for v1.3.0 features
    Private btnRecentFolders As Button

    Private recentFoldersMenu As ContextMenuStrip
    Private cmbPresets As ComboBox
    Private lblPresets As Label
    Private cmbLanguage As ComboBox
    Private lblLanguage As Label
    Private btnTheme As Button
    Private selectedPs5Sdk As UInteger = &H7000038UI
    Private selectedPs4Sdk As UInteger = 0

    ' Tooltip service
    Private tooltipService As TooltipService

    ' Keyboard shortcuts manager
    Private keyboardShortcuts As KeyboardShortcutManager

    ' === NEW ARCHITECTURE (Refactoring 2026) ===
    Private _coordinator As Architecture.Application.Coordinators.PatchingCoordinator

    Private Sub DungeonLinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles DungeonLinkLabel1.LinkClicked
        OpenURL(mytwitter)
    End Sub

    Private Async Sub BtnStart_Click(sender As Object, e As EventArgs) Handles BtnStart.Click
        '==        ' updated on 26-01-2026
        '==        BtnStart.Enabled = False
        rtbStatus.Clear()
        ResetOperationStats()
        'tempfolder = GetTempFolder()
        TempUnpackFolder = GetTempFolder()
        Dim sw As New Stopwatch()
        Try

            updatestatus("Processing Please Wait...", 2)
            selectedfolder = Txtpath.Text
            If String.IsNullOrEmpty(selectedfolder) OrElse Not Directory.Exists(selectedfolder) Then
                MessageBox.Show("Please select a valid folder containing ELF files to patch.", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Error)
                updatestatus()
                Return
            End If
            Dim selectedItem = TryCast(cmbPs5Sdk.SelectedItem, SdkComboItem)
            If selectedItem Is Nothing Then
                MessageBox.Show("Please select a valid PS5 SDK version.", "Invalid SDK", MessageBoxButtons.OK, MessageBoxIcon.Error)
                updatestatus()
                Return
            End If
            selectedPs5Sdk = selectedItem.Ps5Sdk
            selectedPs4Sdk = selectedItem.Ps4Sdk
            'log sdk
            Logger.Log(rtbStatus, $"Selected PS5 SDK: {cmbPs5Sdk.Text} ({selectedPs5Sdk:X8})", Color.Black)
            Dim result = MessageBox.Show("Are you sure you want to port all  libraries in the selected folder?" & Environment.NewLine & "Make sure you have selected the correct folder before proceeding.", "Confirm Patch", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result <> DialogResult.Yes Then
                updatestatus()
                Return
            End If
            'start stopwatch
            gamename = Path.GetFileName(Txtpath.Text)
            sw.Start()
            ' Add to recent folders
            RecentFoldersManager.AddRecentFolder(selectedfolder)
            'SendToLogFile($"--- Starting patching process for folder: {selectedfolder} ---{Environment.NewLine}")
            updatestatus("Patching files...", 2)
            Logger.Log(rtbStatus, $"Backingup: {selectedfolder}", Color.Black)
            ' Record operation start for statistics
            Dim operationStart = DateTime.Now
            Backupfiles()
            'delete fakelib folder in selectedfolder if exists to avoid patching
            If Directory.Exists(Path.Combine(selectedfolder, "fakelib")) Then
                Directory.Delete(Path.Combine(selectedfolder, "fakelib"), True)
            End If

            updatestatus("Patching files...", 2)

            ' === NEW: Use PatchingCoordinator instead of direct ElfPatcher call ===
            Dim patchOptions = New Architecture.Domain.Models.PatchOptions With {
                .AutoBackup = False, ' Already handled above
                .SkipAlreadyPatched = False,
                .ContinueOnError = True,
                .AutoVerify = False
            }

            ' Progress reporter
            Dim progressReporter = New Progress(Of Architecture.Application.Coordinators.PatchProgress)(
                Sub(p)
                    If p.CurrentFile IsNot Nothing Then
                        'Dim line = $"✔ {Path.GetFileName(p.CurrentFile)} ({p.ProcessedFiles}/{p.TotalFiles})"

                        Logger.Log(rtbStatus, $"  {p.CurrentFile} ({p.ProcessedFiles}/{p.TotalFiles})", Color.Black)
                        'fileStatusList.Add(line)
                    End If
                End Sub)

            ' Execute patching via coordinator
            Dim patchingResult = Await _coordinator.ExecutePatchingAsync(
                selectedfolder,
                CLng(selectedPs5Sdk),
                patchOptions,
                progressReporter,
                Threading.CancellationToken.None)

            ' Handle result
            If Not patchingResult.IsSuccess Then
                Throw New Exception(patchingResult.Error.ToUserMessage())
            End If

            ' Update counters from result
            Dim summary = patchingResult.Value
            patchedcount = summary.PatchedCount
            skippedcount = summary.SkippedCount


            updatestatus("Patching completed.", 1)

            'copy libfolder to selectedfolder
            Dim fakelibfolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cmbPs5Sdk.Text, "fakelib")
            Dim fakelibingame = Path.Combine(selectedfolder, "fakelib")
            If Not Directory.Exists(fakelibingame) Then
                Directory.CreateDirectory(fakelibingame)
            End If
            Logger.Log(rtbStatus, $"Copying fakelibs to: {fakelibingame}", Color.Black)

            CopyRelative(fakelibfolder, fakelibingame)
            'clean folder
            CleanTempFolder()
            'updatestatus($"Skipped:{skippedcount},Patched:{patchedcount}", 1)
            updatestatus("--- Patching process completed.---", 1)
            'SendToLogFile($"--- Patching process completed for folder: {selectedfolder} ---{Environment.NewLine}")
            sw.Stop()
            Dim maxLines As Integer = 50
            Dim totalLines = summary.StatusLines.Count
            Logger.Log(rtbStatus, $"UI list count = {totalLines}", Color.Red)
            Dim shownLines = summary.StatusLines.Take(maxLines)
            Dim detailsBlock = String.Join(Environment.NewLine, shownLines)
            If totalLines > maxLines Then
                detailsBlock &= Environment.NewLine &
                    $"… and {totalLines - maxLines} more files"
            End If

            Dim Rmsg As String =
    "✅ Patching completed!" & Environment.NewLine &
    $"✔ Files patched : {patchedcount}" & Environment.NewLine &
    $"⏭ Files skipped : {skippedcount}" & Environment.NewLine &
    $"📦 Fake libs FW : {fwMajor}" & Environment.NewLine &
    $"📁 Fake libs path : {fakelibfolder}" & Environment.NewLine &
    $"⏱ Duration : {sw.Elapsed.TotalSeconds:F2} seconds" & Environment.NewLine &
    Environment.NewLine &
            "— File Results —" & Environment.NewLine &
            detailsBlock


            ShowNotification(Rmsg, "PS5 BackPork Kitchen", "PS5 BackPork Kitchen")
            Logger.Log(rtbStatus, Rmsg, Color.Green)
            OpenFolder(selectedfolder)
        Catch ex As Exception
            'SendToLogFile($"--- Error during patching process: {ex.Message} ---{Environment.NewLine}")
            updatestatus($"Error:{ex.Message}", 1)

            MessageBox.Show($"An error occurred during the patching process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            BtnStart.Enabled = True
            SaveOperationHistory(
    New OperationHistoryItem With {
        .Timestamp = DateTime.Now,
        .GameName = gamename,
        .SdkVersion = $"{cmbPs5Sdk.Text}-{selectedPs5Sdk:X8}",
        .FilesPatched = patchedcount,
        .TotalFiles = patchedcount,
        .DurationSeconds = sw.Elapsed.TotalSeconds,
        .Success = patchedcount > 0
    }
)
            ' Save to new history database for Dashboard
            Try
                Dim historyRepo As New Architecture.Infrastructure.Repositories.PatchingHistoryRepository()
                Dim historyEntry As New Architecture.Domain.Models.PatchingHistoryEntry With {
                    .Id = Guid.NewGuid(),
                    .Timestamp = DateTime.Now,
                    .OperationType = Architecture.Domain.Models.OperationType.SinglePatch,
                    .SourcePath = selectedfolder,
                    .GameName = gamename,
                    .TargetSdk = CLng(selectedPs5Sdk),
                    .TotalFiles = patchedcount + skippedcount,
                    .PatchedFiles = patchedcount,
                    .SkippedFiles = skippedcount,
                    .FailedFiles = 0,
                    .Duration = sw.Elapsed,
                    .Success = patchedcount > 0,
                    .BackupPath = "",
                    .ErrorMessage = "",
                    .MachineName = Environment.MachineName,
                    .UserName = Environment.UserName,
                    .AppVersion = APP_VERSION
                }
                historyRepo.Add(historyEntry)
            Catch ex As Exception
                ' Silently fail if history tracking fails
                Logger.LogToFile($"Failed to save history: {ex.Message}", LogLevel.Warning)
            End Try
        End Try

    End Sub

    Private Sub Backupfiles()
        Dim createBackup As Boolean = chkBackup.Checked
        If createBackup Then
            Try
                Dim parentDirectory As String = IO.Path.GetDirectoryName(selectedfolder)
                Dim folderName As String = IO.Path.GetFileName(selectedfolder)
                Dim backupFolder As String = IO.Path.Combine(parentDirectory, $"{folderName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}")
                'Dim backupFolder As String = $"{selectedfolder}_backup_{DateTime.Now:yyyyMMdd_HHmmss}" ' not working.lol
                Directory.CreateDirectory(backupFolder)
                CopyFromsource(selectedfolder, backupFolder)
                Logger.Log(rtbStatus, $"Backup created at: {backupFolder}", Color.Green)
            Catch ex As Exception
                Logger.Log(rtbStatus, $"Error creating backup: {ex.Message}", Color.Red)
            End Try
        End If

    End Sub

    Private Sub CleanTempFolder()
        Try

            If Directory.Exists(TempUnpackFolder) Then
                Directory.Delete(TempUnpackFolder, True)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error cleaning temp folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub deletefile(fname As String)
        'delete file if exists
        On Error Resume Next
        If System.IO.File.Exists(fname) Then
            System.IO.File.Delete(fname)
        End If
    End Sub
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'delete applog
        deletefile("app.log")
        deletefile("backpork_log.txt")


        updatestatus("Initializing..", 2)
        InitializeElfLog()
        ' Initialize new v1.2.0 services
        InitializeAdvancedFeatures()

        ' === Initialize new architecture coordinator ===
        Try
            _coordinator = Architecture.Composition.CompositionRoot.Instance().CreatePatchingCoordinator()
            Logger.Log(rtbStatus, "Architecture initialized successfully", Color.Green)
        Catch ex As Exception
            Logger.Log(rtbStatus, $"Warning: Architecture initialization failed: {ex.Message}", Color.Orange, True, Logger.LogLevel.Error)
            Logger.Log(rtbStatus, "Application will continue with legacy mode", Color.Orange)
            ' Continue without coordinator - app will fall back to legacy patching
        End Try

        ' Initialize Library Manager
        Try
            LibraryManager.Initialize()
        Catch ex As Exception
            Logger.Log(rtbStatus, $"Warning: Library Manager initialization failed: {ex.Message}", Color.Orange)
        End Try

        ' Apply modern gradient background
        ApplyModernBackground()

        ' Show v1.2.0 features info
        ShowV12Features()
        chklibcpatch.Checked = False
        'chklibcpatch.Visible = False 'hide for now
        lblexperiment.Visible = False
        updatestatus()
        rtbStatus.Clear()
        TableLayoutPanel1.Visible = False
        lblVer.Text = $"v{Application.ProductVersion}"

        ' Add Telegram support links
        AddTelegramSupport()
#If DEBUG Then
        btnUFS2Image.Visible = True

#End If

    End Sub

    Private Sub LoadComboBoxValues()

        Dim sdkList = VersionProfiles.BuildPs5SdkList()

        cmbPs5Sdk.DropDownStyle = ComboBoxStyle.DropDownList
        cmbPs5Sdk.FormattingEnabled = False

        cmbPs5Sdk.DataSource = sdkList
        cmbPs5Sdk.DisplayMember = "Display"
        cmbPs5Sdk.ValueMember = Nothing   ' IMPORTANT

        cmbPs5Sdk.SelectedIndex =
        sdkList.FindIndex(Function(x) x.Ps5Sdk = &H7000038UI)

    End Sub

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs) Handles BtnBrowse.Click

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select folder containing ELF files to patch"
            If fbd.ShowDialog() <> DialogResult.OK Then Return
            'check Selected folder name starting with "PPSA"
            'Dim folder4letters = Path.GetFileName(fbd.SelectedPath).Substring(0, 4)
            'If Not folder4letters = "PPSA" Then
            '    MessageBox.Show("Selected folder does not appear to be a valid PS5 homebrew application folder (folder name should start with 'PPSA'). Please select the correct folder.", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Error)
            '    updatestatus()
            '    Return
            'End If

            rtbStatus.Clear()
            Logger.Log(rtbStatus, $"Selected folder: {fbd.SelectedPath}", Color.Black)
            Txtpath.Text = fbd.SelectedPath
        End Using
        TableLayoutPanel1.Visible = True
        logparamjson(Txtpath.Text)

    End Sub

    Private Sub logparamjson(selectedfolder As String)
        On Error Resume Next
        Dim paramPath = Path.Combine(selectedfolder, "sce_sys", "param.json")
        Dim gameicon As String = Path.Combine(selectedfolder, "sce_sys", "icon0.png")
        Dim paramInfo = ReadEssentialInfo(paramPath)
        If paramInfo IsNot Nothing Then
            Logger.Log(rtbStatus, "param.json detected", Color.Blue)
            RichGameInfo.Clear()

            If Not String.IsNullOrEmpty(paramInfo.Title) Then
                gamename = paramInfo.Title.Trim()
                Logger.Log(RichGameInfo, $"Title: {gamename}", Color.Purple, False)

            End If

            Logger.Log(RichGameInfo, $"Title ID: {paramInfo.TitleId}", Color.Black, False)
            Logger.Log(RichGameInfo, $"Content ID: {paramInfo.ContentId}", Color.Black, False)
            Logger.Log(RichGameInfo, $"Content Version: {paramInfo.ContentVersion}", Color.Blue, False)
            Logger.Log(RichGameInfo, $"Origin Version: {paramInfo.OriginContentVersion}", Color.Blue, False)
        Else
            Logger.Log(RichGameInfo, "param.json not found", Color.DarkGray)
        End If
        'load icon if exists
        If File.Exists(gameicon) Then
            gamepic.Image = Image.FromFile(gameicon)
        Else
            gamepic.Image = My.Resources.logo
        End If

    End Sub

    Private Sub DungeonLinkLabel2_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles DungeonLinkLabel2.LinkClicked
        Me.Hide()
        'show credits form
        Dim credits As New CreditsForm()
        credits.ShowDialog()
        Me.Show()
    End Sub

    Private Async Sub Form1_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        'checking base library
        Me.Refresh()
        TableLayoutPanel1.Visible = True


        ' Check for updates with new system
        Try
            Await CheckForUpdatesEnhancedAsync()
        Catch ex As Exception
            Debug.WriteLine($"Update check failed: {ex.Message}")
            Logger.Log(rtbStatus, $"Update check failed: {ex.Message}", Color.Orange)
        End Try
        updatestatus("Downloading SelfUtil", 2)
        Logger.Log(RichGameInfo, $"Checking for SelfUtils", Color.Purple, False)
        'Try
        '    Await DownloadSelfUtil()
        'Catch ex As Exception
        '    Logger.Log(rtbStatus, $"SelfUtil download failed: {ex.Message}", Color.Red)
        '    MessageBox.Show("Selfutil downloading failed")
        'End Try
        'Await DownloadAllStandaloneAsync()
        Me.Refresh()
        updatestatus("Downloading Libs", 2)
        Logger.Log(RichGameInfo, $"Checking fakelibs Please wait..", Color.Blue, False)
        Try
            Await DownloadAllArchivesAsync()
        Catch ex As Exception
            Logger.Log(rtbStatus, $"Fakelib download failed: {ex.Message}", Color.Red)
        End Try
        Application.DoEvents()
        Logger.Log(RichGameInfo, $"Checks Done!", Color.Blue, False)
        Await Task.Delay(5000)

        LoadComboBoxValues()
        updatestatus()
        Me.Refresh()
    End Sub

    ''' <summary>
    ''' Enhanced auto-update check with user-friendly UI.
    ''' </summary>
    Public Async Function CheckForUpdatesEnhancedAsync() As Task
        Try
            Logger.Log(RichGameInfo, "Checking for updates...", Color.Blue, False)

            AddHandler UpdateCheckerService.UpdateAvailable,
        AddressOf HandleUpdateAvailable
            ' Check if auto-update is enabled
            'If Not UpdateCheckerService.IsAutoUpdateEnabled() Then
            '    Logger.Log(RichGameInfo, "Auto-update check disabled", Color.Gray, False)
            '    Return
            'End If

            ' Check if we should check (24h interval)
            'If Not UpdateCheckerService.ShouldCheckForUpdates() Then
            '    Return
            'End If


            ' Perform check
            'Dim result = Await UpdateCheckerService.CheckForUpdatesAsync()
            UpdateCheckerService.StartBackgroundUpdateCheck()

            ' Update last check time
            UpdateCheckerService.UpdateLastCheckTime()

            'If Not result.CheckedSuccessfully Then
            '    Logger.Log(RichGameInfo, "Update check failed", Color.Orange, False)
            '    Return
            'End If

            'If result.UpdateAvailable Then
            '    ' Check if version is skipped
            '    If UpdateCheckerService.IsVersionSkipped(result.LatestVersion) Then
            '        Logger.Log(RichGameInfo, $"Update v{result.LatestVersion} skipped by user", Color.Gray, False)
            '        Return
            '    End If

            '    Logger.Log(RichGameInfo, $"Update available: v{result.LatestVersion}", Color.Green, False)

            '    ' Show notification dialog
            '    Using notifyForm As New UpdateNotificationForm(result)
            '        notifyForm.ShowDialog(Me)

            '        Select Case notifyForm.UserDecision
            '            Case UpdateCheckerService.UpdateDecision.DownloadNow
            '                ' Open browser to releases page
            '                OpenURL(result.ReleaseUrl)

            '            Case UpdateCheckerService.UpdateDecision.SkipVersion
            '                ' Mark version as skipped
            '                UpdateCheckerService.SkipVersion(result.LatestVersion)
            '                Logger.Log(RichGameInfo, $"Version v{result.LatestVersion} skipped", Color.Gray, False)

            '            Case UpdateCheckerService.UpdateDecision.RemindLater
            '                ' Do nothing - will check again in 24h
            '                Logger.Log(RichGameInfo, "Reminder set for 24 hours", Color.Blue, False)
            '        End Select
            '    End Using
            'Else
            '    Logger.Log(RichGameInfo, $"Up to date (v{result.CurrentVersion})", Color.Green, False)
            'End If
        Catch ex As Exception
            Debug.WriteLine($"Enhanced update check failed: {ex.Message}")
            Logger.Log(RichGameInfo, "Unable to check for updates", Color.Orange, False)
        End Try
    End Function
    Private Sub HandleUpdateAvailable(result As UpdateCheckerService.UpdateCheckResult)

        If Me.InvokeRequired Then
            Me.Invoke(Sub() HandleUpdateAvailable(result))
            Return
        End If

        ' ----------------------------
        ' Your logic goes here
        ' ----------------------------

        If Not result.CheckedSuccessfully Then
            Logger.Log(RichGameInfo, "Update check failed", Color.Orange, False)
            Return
        End If

        If result.UpdateAvailable Then

            If UpdateCheckerService.IsVersionSkipped(result.LatestVersion) Then
                Logger.Log(RichGameInfo,
                       $"Update v{result.LatestVersion} skipped by user",
                       Color.Gray,
                       False)
                Return
            End If

            Logger.Log(RichGameInfo,
                   $"Update available: v{result.LatestVersion}",
                   Color.Green,
                   False)

            Using notifyForm As New UpdateNotificationForm(result)
                notifyForm.ShowDialog(Me)

                Select Case notifyForm.UserDecision

                    Case UpdateCheckerService.UpdateDecision.DownloadNow
                        OpenURL(result.ReleaseUrl)

                    Case UpdateCheckerService.UpdateDecision.SkipVersion
                        UpdateCheckerService.SkipVersion(result.LatestVersion)
                        Logger.Log(RichGameInfo,
                               $"Version v{result.LatestVersion} skipped",
                               Color.Gray,
                               False)

                    Case UpdateCheckerService.UpdateDecision.RemindLater
                        Logger.Log(RichGameInfo,
                               "Reminder set for 24 hours",
                               Color.Blue,
                               False)

                End Select
            End Using

        Else
            Logger.Log(RichGameInfo,
                   $"Up to date (v{result.CurrentVersion})",
                   Color.Green,
                   False)
        End If

    End Sub

    ' Keep old function for backward compatibility
    Public Async Function CheckUpdatesAsync() As Task
        Dim fallbackToGitHub As Boolean = False
        Try
            Await CheckUpdateSupabaseAsync()
            updatestatus()
        Catch ex As Exception
            Logger.Log(rtbStatus, "Supabase update check failed. Will try GitHub.", Color.Orange)
            fallbackToGitHub = True
        End Try
        If fallbackToGitHub Then
            Await CheckGitHubUpdateAsync()
        End If
    End Function

    Private Sub MoonButton1_Click(sender As Object, e As EventArgs) Handles MoonButton1.Click
        Dim frm As New restoreform
        frm.ShowDialog()

    End Sub

    Private Async Sub Button1_Click(sender As Object, e As EventArgs)

        ''path to fw 7
        'Dim filePath As String = "C:\Users\rajes\OneDrive\Desktop\alex\TEST\origeboot.elf"
        'Dim selectedItem = TryCast(cmbPs5Sdk.SelectedItem, SdkComboItem)
        'If selectedItem Is Nothing Then
        '    MessageBox.Show("Please select a valid PS5 SDK version.", "Invalid SDK", MessageBoxButtons.OK, MessageBoxIcon.Error)
        '    updatestatus()
        '    Return
        'End If
        'selectedPs5Sdk = selectedItem.Ps5Sdk
        'selectedPs4Sdk = selectedItem.Ps4Sdk
        'rtbStatus.Clear()
        'Dim inputfpath As String = "C:\Users\rajes\OneDrive\Desktop\alex\all in one test\libc.elf"
        'Dim outputfpath As String = "C:\Users\rajes\OneDrive\Desktop\alex\all in one test\libCARA.bin"
        'Dim resultMsg As String = ""
        'Dim patched = ElfPatcher.PatchSingleFile(
        '    inputfpath,
        '    selectedPs5Sdk,
        '    selectedPs4Sdk, resultMsg
        ')
        'Logger.Log(rtbStatus, resultMsg, If(patched, Color.Green, Color.Red))
        'sign the patched file
        '#If DEBUG Then
        '        Debug.WriteLine($"PATCHED ELF SHA256 = {ComputeSHA256(inputfpath)}")
        '#End If
        '        DebugSignSingleFile(inputfpath, outputfpath)
        '        Logger.Log(rtbStatus, $"Signed file saved to: {outputfpath}", Color.Green)
        'lib c test
        Dim libcPath = "C:\Users\rajes\OneDrive\Desktop\alex\backupPPSA14677-app_original\sce_module\libc.prx"
        PatchPrxString(libcPath, Encoding.ASCII.GetBytes("4h6F1LLbTiw#A#B"), Encoding.ASCII.GetBytes("IWIBBdTHit4#A#B"))

    End Sub

    Private Sub NightLinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel1.LinkClicked
        LoadLogs()
    End Sub

    ' === v1.2.0 NEW FEATURES METHODS ===
    ' === Coded By DroneTechTI ===

    ''' <summary>
    ''' Initialize v1.2.0 advanced features
    ''' </summary>
    Private Sub InitializeAdvancedFeatures()
        Me.AllowDrop = True
        Try
            ' Initialize Logger with file output
            Logger.InitializeFileLogger()

            ' Load configuration
            Dim config = ConfigurationManager.LoadConfiguration()

            ' Load language preference
            Dim lang = LocalizationService.LoadLanguagePreference()
            LocalizationService.Initialize(lang)

            ' Load and apply theme
            ThemeManager.Initialize()
            ThemeManager.ApplyThemeToForm(Me)

            ' Initialize Tooltip Service
            tooltipService = New TooltipService()
            tooltipService.ConfigureMainFormTooltips(Me)

            ' Initialize Keyboard Shortcuts
            keyboardShortcuts = New KeyboardShortcutManager(Me)
            keyboardShortcuts.ConfigureForm1Shortcuts(Me)

            ' Update button text with keyboard shortcuts hints
            UpdateButtonTextWithShortcuts()

            ' Enable Drag & Drop on path textbox with improved handling
            ' Make sure the control accepts drag & drop
            Txtpath.AllowDrop = True
            DragDropHelper.EnableDragDrop(Txtpath, AddressOf OnFolderDropped)
            DragDropHelper.SetupDragVisualFeedback(Txtpath, Color.LightBlue)

            Logger.Log(rtbStatus, "Drag & Drop enabled on path field", Color.Green, True, Logger.LogLevel.Info)
            AddV13UIControls()
            ' Load recent folders
            LoadRecentFolders()

            ' Load presets
            LoadPresets()

            Log(rtbStatus, "Advanced features initialized (including tooltips)", Color.Green, True, Logger.LogLevel.Info)

            Log(rtbStatus, "Advanced features initialized (incl. keyboard shortcuts)", Color.Green, True, Logger.LogLevel.Info)
        Catch ex As Exception
            ' Don't fail if advanced features can't initialize
            Log(rtbStatus, $"Warning: Some features unavailable: {ex.Message}", Color.Orange, True, Logger.LogLevel.Warning)
        End Try
    End Sub

    ''' <summary>
    ''' Add Telegram support link and menu
    ''' </summary>
    Private Sub AddTelegramSupport()
        Try
            ' === Add Telegram link in credits area (TableLayoutPanel3) ===
            ' Create DungeonLinkLabel to match existing style
            Dim linkTelegram As New ReaLTaiizor.Controls.DungeonLinkLabel With {
                .ActiveLinkColor = Color.FromArgb(0, 136, 204),
                .AutoSize = True,
                .BackColor = Color.Transparent,
                .Font = New Font("Segoe UI", 11.0F),
                .LinkBehavior = LinkBehavior.AlwaysUnderline,
                .LinkColor = Color.Blue,
                .Location = New Point(170, 0),
                .Name = "linkTelegram",
                .Size = New Size(70, 20),
                .TabStop = True,
                .Text = "Telegram Support",
                .VisitedLinkColor = Color.FromArgb(240, 119, 70),
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' Add event handler
            AddHandler linkTelegram.LinkClicked, AddressOf LinkTelegram_Click

            ' Add to TableLayoutPanel3 (credits area)
            TableLayoutPanel3.Controls.Add(linkTelegram)
            linkTelegram.BringToFront()
        Catch ex As Exception
            ' Log error for debugging
            Logger.Log(rtbStatus, $"Telegram integration error: {ex.Message}", Color.Orange, True, Logger.LogLevel.Warning)
        End Try

    End Sub

    ''' <summary>
    ''' Handle Telegram link click in credits
    ''' </summary>
    Private Sub LinkTelegram_Click(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Try
            Process.Start(New ProcessStartInfo(myTelegram) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show($"Could not open Telegram link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Handle Telegram menu click
    ''' </summary>
    Private Sub MenuTelegram_Click(sender As Object, e As EventArgs)
        Try
            Process.Start(New ProcessStartInfo(myTelegram) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show($"Could not open Telegram link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Handle About menu click
    ''' </summary>
    Private Sub MenuAbout_Click(sender As Object, e As EventArgs)
        Dim aboutMsg = $"PS5 BackPork Kitchen{vbCrLf}" &
                      $"Version: {Application.ProductVersion}{vbCrLf}{vbCrLf}" &
                      $"A tool for backporting PS5 games to lower firmware versions.{vbCrLf}{vbCrLf}" &
                      $"Developer: rajeshca911{vbCrLf}" &
                      $"Contributors: DroneTechTI & Community{vbCrLf}{vbCrLf}" &
                      $"Join our Telegram for support and updates!"

        MessageBox.Show(aboutMsg, "About PS5 BackPork Kitchen", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ''' <summary>
    ''' Handle folder dropped via drag & drop
    ''' </summary>
    Private Sub OnFolderDropped(folderPath As String)
        Try
            Logger.Log(rtbStatus, $"Drag & Drop triggered: {folderPath}", Color.Blue, True, Logger.LogLevel.Info)

            If DragDropHelper.ValidateDroppedGameFolder(folderPath) Then
                Txtpath.Text = folderPath
                Logger.Log(rtbStatus, $"📂 Folder loaded via Drag & Drop: {Path.GetFileName(folderPath)}", Color.Green, True, Logger.LogLevel.Success)

                ' Auto-detect SDK if available
                AutoDetectSDK(folderPath)

                ' Add to recent folders
                RecentFoldersManager.AddRecentFolder(folderPath)
            Else
                MessageBox.Show("The dropped folder does not appear to be a valid PS5 game folder." & vbCrLf &
                               "Make sure it contains 'sce_sys' folder or 'eboot.bin' file.",
                               "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Logger.Log(rtbStatus, "Dropped folder is not a valid game folder", Color.Orange, True, Logger.LogLevel.Warning)
            End If
        Catch ex As Exception
            Logger.Log(rtbStatus, $"Drag & Drop error: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
            MessageBox.Show($"Error processing dropped folder: {ex.Message}", "Drag & Drop Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Load recent folders
    ''' </summary>
    Private Sub LoadRecentFolders()
        Try
            recentFolders = RecentFoldersManager.GetRecentFoldersForDisplay()
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Load presets
    ''' </summary>
    Private Sub LoadPresets()
        Try
            Dim presets = PresetManager.LoadAllPresets()
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Auto-detect SDK from folder
    ''' </summary>
    Private Sub AutoDetectSDK(folderPath As String)
        Try
            Dim analysis = SdkDetector.AnalyzeFolderSdkVersions(folderPath)
            If analysis.FilesAnalyzed > 0 AndAlso analysis.RecommendedTargetSdk > 0 Then
                Log(rtbStatus, $"SDK Detected: {SdkDetector.FormatSdkVersion(analysis.RecommendedTargetSdk)} " &
                    $"({analysis.FilesAnalyzed} files)", Color.Green, True, Logger.LogLevel.Info)
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Show v1.2.0 features information
    ''' </summary>
    Private Sub ShowV12Features()

        Log(rtbStatus, $"=== PS5 BackPork Kitchen v{My.Application.Info.Version.ToString} ===", Color.Blue, False)
        Log(rtbStatus, "New features initialized! Try Recent, Presets & Language!", Color.Green, True, Logger.LogLevel.Info)
        Log(rtbStatus, "", Color.Black, False)
        With lblDragDropHint
            .Text = "💡 TIP: Drag & Drop game folders directly onto the path field below!"
            .Font = New Font("Segoe UI", 8.5, FontStyle.Italic)
            .ForeColor = Color.DarkGreen
            .BackColor = Color.Transparent
            .AutoSize = True
        End With
        With btnShowStatistics
            .Text = "📊 Statistics"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = ColorPalette.FeatureBlue
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
        With btnElfInspector
            .Text = "🔍 ELF Inspector"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = ColorPalette.FeatureGreen
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
        With btnBatchProcess
            .Text = "📦 Batch Process"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = ColorPalette.FeatureCoral
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
        With btnAdvancedOps
            .Text = "🔧 Advanced Ops"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = ColorPalette.FeatureGold
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
        With btnPayloadManager
            .Text = "📤 Payload Manager"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = ColorPalette.FeatureTeal
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
        With btnUFS2Image
            .Text = "💿 UFS2 Image"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = Color.FromArgb(180, 210, 255)
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
        With btnPkgManager
            .Text = "📦 PKG Manager"
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .BackColor = Color.FromArgb(200, 230, 180)
            .FlatStyle = FlatStyle.Flat
            .Cursor = Cursors.Hand
        End With
    End Sub

    ''' <summary>
    ''' Add visible UI controls for v1.2.0 features
    ''' </summary>
    '    Private Sub AddV12UIControls()

    '            btnBatchProcess = New Button With {
    '    .Text = "📦 Batch Process",
    '    .Location = New Point(buttonX, buttonY + buttonSpacingY),
    '    .Size = New Size(buttonWidth, buttonHeight),
    '    .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left,
    '    .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '    .BackColor = ColorPalette.FeatureCoral,
    '    .ForeColor = Color.Black,
    '    .FlatStyle = FlatStyle.Flat,
    '    .Cursor = Cursors.Hand
    '}
    '            btnBatchProcess.FlatAppearance.BorderColor = ColorPalette.Error
    '            AddHandler btnBatchProcess.Click, AddressOf BtnBatchProcess_Click
    '            Me.Controls.Add(btnBatchProcess)
    '            btnBatchProcess.BringToFront()

    '            ' Add Advanced Operations button (Row 2, Col 2)
    '            btnAdvancedOps = New Button With {
    '    .Text = "🔧 Advanced Ops",
    '    .Location = New Point(buttonX + buttonSpacingX, buttonY + buttonSpacingY),
    '    .Size = New Size(buttonWidth, buttonHeight),
    '    .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left,
    '    .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '    .BackColor = ColorPalette.FeatureGold,
    '    .ForeColor = Color.Black,
    '    .FlatStyle = FlatStyle.Flat,
    '    .Cursor = Cursors.Hand
    '}
    '            btnAdvancedOps.FlatAppearance.BorderColor = ColorPalette.Warning
    '            AddHandler btnAdvancedOps.Click, AddressOf BtnAdvancedOps_Click
    '            Me.Controls.Add(btnAdvancedOps)
    '            btnAdvancedOps.BringToFront()

    '            ' THIRD ROW - Firmware Manager (Row 3, Col 1)
    '            btnFirmwareManager = New Button With {
    '    .Text = "💾 Firmware Manager",
    '    .Location = New Point(buttonX, buttonY + buttonSpacingY * 2),
    '    .Size = New Size(buttonWidth, buttonHeight),
    '    .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left,
    '    .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '    .BackColor = ColorPalette.FeaturePurple,
    '    .ForeColor = Color.White,
    '    .FlatStyle = FlatStyle.Flat,
    '    .Cursor = Cursors.Hand,
    '    .Visible = False 'hide for now
    '}
    '            btnFirmwareManager.FlatAppearance.BorderColor = ColorPalette.Primary

    '            'AddHandler btnFirmwareManager.Click, AddressOf BtnFirmwareManager_Click
    '            Me.Controls.Add(btnFirmwareManager)
    '            btnFirmwareManager.BringToFront()

    '            ' Add Payload Manager button (Row 3, Col 2)
    '            btnPayloadManager = New Button With {
    '    .Text = "📤 Payload Manager",
    '    .Location = New Point(buttonX + buttonSpacingX, buttonY + buttonSpacingY * 2),
    '    .Size = New Size(buttonWidth, buttonHeight),
    '    .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left,
    '    .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '    .BackColor = ColorPalette.FeatureTeal,
    '    .ForeColor = Color.White,
    '    .FlatStyle = FlatStyle.Flat,
    '    .Cursor = Cursors.Hand
    '}
    '            btnPayloadManager.FlatAppearance.BorderColor = ColorPalette.PrimaryDark
    '            AddHandler btnPayloadManager.Click, AddressOf BtnPayloadManager_Click
    '            Me.Controls.Add(btnPayloadManager)
    '            btnPayloadManager.BringToFront()

    '            ' === v1.3.0 NEW FEATURE: Recent Folders Button ===
    '            AddV13UIControls()

    '            Log(rtbStatus, "✔️ v1.2.0 UI controls added successfully!", Color.Green, True, Logger.LogLevel.Info)
    '        Catch ex As Exception
    '            Log(rtbStatus, $"Warning: Could not add UI controls: {ex.Message}", Color.Orange, True, Logger.LogLevel.Warning)
    '        End Try
    '    End Sub

    ' === v1.3.0 NEW FEATURES METHODS ===
    ' === codded by DronetechTI ===

    ''' <summary>
    ''' Add visible UI controls for v1.3.0 features
    ''' </summary>
    Private Sub AddV13UIControls()
        Try
            ' Add Recent Folders button next to Browse button

            btnRecentFolders = New Button With {
    .Text = "📂",
    .Size = New Size(85, BtnBrowse.Height),
    .Location = New Point(
        BtnBrowse.Left - 90,
        BtnBrowse.Top
    ),
    .Font = BtnBrowse.Font,
    .BackColor = Color.LightSteelBlue,
    .FlatStyle = FlatStyle.Flat,
    .Cursor = Cursors.Hand,
    .TextAlign = ContentAlignment.TopLeft,
    .Anchor = AnchorStyles.Top Or AnchorStyles.Left
}

            btnRecentFolders.FlatAppearance.BorderColor = Color.SteelBlue
            AddHandler btnRecentFolders.Click, AddressOf BtnRecentFolders_Click
            Me.Controls.Add(btnRecentFolders)
            btnRecentFolders.BringToFront()
            lblPresets = New Label With {
    .Text = "Preset:",
    .AutoSize = True,
    .Font = cmbPs5Sdk.Font,
    .Location = New Point(
        cmbPs5Sdk.Right + 15,
        cmbPs5Sdk.Top + 4
    )
}
            'combbox for presets

            '            cmbPresets = New ComboBox With {
            '    .Width = 200,
            '    .DropDownStyle = ComboBoxStyle.DropDownList,
            '    .Font = cmbPs5Sdk.Font,
            '    .Visible = False 'desable for now
            '}

            '            cmbPresets.Location = New Point(
            '    TableLayoutPanel5.Right + 8,
            '    TableLayoutPanel5.Top + (TableLayoutPanel5.Height - cmbPresets.Height) \ 2
            ')

            '            AddHandler cmbPresets.SelectedIndexChanged, AddressOf CmbPresets_SelectedIndexChanged
            '            Me.Controls.Add(cmbPresets)
            '            cmbPresets.BringToFront()

            '            ' Load presets into combobox
            '            LoadPresetsIntoComboBox()

            cmbLanguage = New ComboBox With {
    .Size = New Size(130, 23),
    .Font = New Font("Segoe UI", 8.5),
    .DropDownStyle = ComboBoxStyle.DropDownList,
    .Anchor = AnchorStyles.Top Or AnchorStyles.Right
}

            cmbLanguage.Location = New Point(
    Me.ClientSize.Width - cmbLanguage.Width - 15,
    30
)

            lblLanguage = New Label With {
    .Text = "🌐Language:",
    .AutoSize = True,
    .Font = cmbLanguage.Font,
    .Anchor = AnchorStyles.Top Or AnchorStyles.Right
}

            lblLanguage.Location = New Point(
    cmbLanguage.Left,
    cmbLanguage.Top - lblLanguage.Height - 2
)

            cmbLanguage.Items.AddRange(New String() {"English", "Italiano", "Deutsch"})

            ' Set current language
            Dim currentLang = LocalizationService.GetCurrentLanguage()
            Select Case currentLang
                Case LocalizationService.SupportedLanguage.English
                    cmbLanguage.SelectedIndex = 0
                Case LocalizationService.SupportedLanguage.Italian
                    cmbLanguage.SelectedIndex = 1
                Case LocalizationService.SupportedLanguage.German
                    cmbLanguage.SelectedIndex = 2
                Case Else
                    cmbLanguage.SelectedIndex = 0
            End Select

            AddHandler cmbLanguage.SelectedIndexChanged, AddressOf CmbLanguage_SelectedIndexChanged
            Me.Controls.Add(cmbLanguage)
            cmbLanguage.BringToFront()

            btnTheme = New Button With {
    .Text = "🎨",
    .Size = New Size(40, cmbLanguage.Height),
    .Location = New Point(
        cmbLanguage.Left - 45,
        cmbLanguage.Top
    ),
    .Font = New Font("Segoe UI", 10),
    .BackColor = Color.LightGray,
    .FlatStyle = FlatStyle.Flat,
    .Cursor = Cursors.Hand,
    .Anchor = AnchorStyles.Top Or AnchorStyles.Right
}

            btnTheme.FlatAppearance.BorderColor = Color.Gray
            AddHandler btnTheme.Click, AddressOf BtnTheme_Click
            Me.Controls.Add(btnTheme)
            btnTheme.BringToFront()

            Log(rtbStatus, "✔️ v1.3.0: Recent Folders, Presets, Language & Theme added!", Color.Green, True, Logger.LogLevel.Info)
        Catch ex As Exception
            Log(rtbStatus, $"Warning: Could not add v1.3.0 controls: {ex.Message}", Color.Orange, True, Logger.LogLevel.Warning)
        End Try
    End Sub

    ''' <summary>
    ''' Load presets into combobox
    ''' </summary>
    Private Sub LoadPresetsIntoComboBox()
        Try
            cmbPresets.Items.Clear()

            ' Add default option
            cmbPresets.Items.Add("(No Preset)")

            ' Load all presets
            Dim presets = PresetManager.LoadAllPresets()

            For Each preset In presets
                cmbPresets.Items.Add(preset.PresetName)
            Next

            ' Select default
            cmbPresets.SelectedIndex = 0
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Handle preset selection change
    ''' </summary>
    Private Sub CmbPresets_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            If cmbPresets.SelectedIndex <= 0 Then
                ' No preset selected
                Return
            End If

            Dim presetName = cmbPresets.SelectedItem.ToString()
            Dim allPresets = PresetManager.LoadAllPresets()
            Dim preset = allPresets.FirstOrDefault(Function(p) p.PresetName = presetName)
            Dim presetMap = allPresets.ToDictionary(Function(p) p.PresetName)
            Dim presetDescription = presetMap(presetName).Description

            If Not String.IsNullOrEmpty(preset.PresetName) Then
                ' Apply preset settings
                ' Find matching SDK in combobox
                Dim sdkList = TryCast(cmbPs5Sdk.DataSource, List(Of SdkComboItem))
                If sdkList IsNot Nothing Then
                    Dim matchingIndex = sdkList.FindIndex(Function(x) x.Ps5Sdk = preset.TargetPs5Sdk)
                    If matchingIndex >= 0 Then
                        cmbPs5Sdk.SelectedIndex = matchingIndex
                    End If
                End If

                ' Apply other settings
                chkBackup.Checked = preset.AutoBackup

                Log(rtbStatus, $"✔️ Preset applied: {presetName}", Color.Green, True, Logger.LogLevel.Success)
                Log(rtbStatus, $"✔️ Description: {presetDescription}", Color.Green, True, Logger.LogLevel.Success)

            End If
        Catch ex As Exception
            MessageBox.Show($"Error applying preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Handle language change
    ''' </summary>
    Private Sub CmbLanguage_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            Dim selectedLang As LocalizationService.SupportedLanguage

            Select Case cmbLanguage.SelectedIndex
                Case 0 ' English
                    selectedLang = LocalizationService.SupportedLanguage.English
                Case 1 ' Italiano
                    selectedLang = LocalizationService.SupportedLanguage.Italian
                Case 2 ' Deutsch
                    selectedLang = LocalizationService.SupportedLanguage.German
                Case Else
                    selectedLang = LocalizationService.SupportedLanguage.English
            End Select

            ' Set language
            LocalizationService.SetLanguage(selectedLang)

            ' Apply translations to UI
            ApplyTranslations()

            Logger.Log(rtbStatus, $"✔️ Language changed to: {cmbLanguage.SelectedItem}", Color.Green, True, Logger.LogLevel.Success)
        Catch ex As Exception
            MessageBox.Show($"Error changing language: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Apply translations to UI elements
    ''' </summary>
    Private Sub ApplyTranslations()
        Try
            ' Translate main buttons
            BtnBrowse.Text = LocalizationService.T("btn_browse")
            BtnStart.Text = LocalizationService.T("btn_start")
            btnRecentFolders.Text = LocalizationService.T("btn_recent")

            ' Translate labels
            lblPresets.Text = LocalizationService.T("lbl_preset")
            lblLanguage.Text = LocalizationService.T("lbl_language")
            lblDragDropHint.Text = LocalizationService.T("hint_dragdrop")

            ' Translate status buttons
            btnShowStatistics.Text = LocalizationService.T("btn_statistics")
            btnElfInspector.Text = LocalizationService.T("btn_elf_inspector")
            btnBatchProcess.Text = LocalizationService.T("btn_batch")

            ' Translate placeholders
            Txtpath.PlaceholderText = LocalizationService.T("placeholder_path")
        Catch ex As Exception
            ' Silent fail - translations not critical
        End Try
    End Sub

    ''' <summary>
    ''' Show recent folders menu when Recent button is clicked
    ''' </summary>
    Private Sub BtnRecentFolders_Click(sender As Object, e As EventArgs)
        Try
            'MessageBox.Show("Loading from:" & vbCrLf & ConfigPath)

            ' Create context menu
            recentFoldersMenu = New ContextMenuStrip()
            recentFoldersMenu.Font = New Font("Segoe UI", 9)

            ' Load recent folders
            Dim recentList = RecentFoldersManager.LoadRecentFolders()

            If recentList.Count = 0 Then
                ' No recent folders
                Dim noItemsItem As New ToolStripMenuItem("No recent folders") With {
                    .Enabled = False,
                    .ForeColor = Color.Gray
                }
                recentFoldersMenu.Items.Add(noItemsItem)
            Else
                ' Add each recent folder
                For Each entry In recentList
                    Dim displayText = entry.GameName
                    If displayText.Length > 40 Then
                        displayText = displayText.Substring(0, 37) & "..."
                    End If

                    Dim menuItem As New ToolStripMenuItem(displayText) With {
                        .Tag = entry.FolderPath,
                        .ToolTipText = entry.FolderPath & vbCrLf &
                                      "Last used: " & entry.LastUsed.ToString("dd/MM/yyyy HH:mm")
                    }

                    ' Add SDK version if available
                    If Not String.IsNullOrEmpty(entry.LastSdkVersion) Then
                        menuItem.ToolTipText &= vbCrLf & "Last SDK: " & entry.LastSdkVersion
                    End If

                    AddHandler menuItem.Click, AddressOf RecentFolderMenuItem_Click
                    recentFoldersMenu.Items.Add(menuItem)
                Next

                ' Add separator
                recentFoldersMenu.Items.Add(New ToolStripSeparator())

                ' Add "Clear History" option
                Dim clearItem As New ToolStripMenuItem("Clear History") With {
                    .ForeColor = Color.Red,
                    .Font = New Font("Segoe UI", 9, FontStyle.Bold)
                }
                AddHandler clearItem.Click, AddressOf ClearRecentFolders_Click
                recentFoldersMenu.Items.Add(clearItem)
            End If

            ' Show menu below the Recent button
            recentFoldersMenu.Show(btnRecentFolders, New Point(0, btnRecentFolders.Height))
        Catch ex As Exception
            MessageBox.Show($"Error loading recent folders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Handle recent folder menu item click
    ''' </summary>
    Private Sub RecentFolderMenuItem_Click(sender As Object, e As EventArgs)
        Try
            Dim menuItem = TryCast(sender, ToolStripMenuItem)
            If menuItem IsNot Nothing AndAlso menuItem.Tag IsNot Nothing Then
                Dim folderPath = menuItem.Tag.ToString()

                ' Check if folder still exists
                If Directory.Exists(folderPath) Then
                    Txtpath.Text = folderPath
                    Logger.Log(rtbStatus, $"✔️ Loaded from recent: {Path.GetFileName(folderPath)}", Color.Green, True, Logger.LogLevel.Success)

                    ' Load game info
                    TableLayoutPanel1.Visible = True
                    logparamjson(folderPath)

                    ' Auto-detect SDK
                    AutoDetectSDK(folderPath)
                Else
                    MessageBox.Show("This folder no longer exists." & vbCrLf & "It will be removed from recent folders.",
                                  "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    RecentFoldersManager.RemoveRecentFolder(folderPath)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Clear recent folders history
    ''' </summary>
    Private Sub ClearRecentFolders_Click(sender As Object, e As EventArgs)
        Try
            Dim result = MessageBox.Show("Are you sure you want to clear recent folders history?",
                                        "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                RecentFoldersManager.ClearRecentFolders()
                MessageBox.Show("Recent folders history cleared!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error clearing history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Show statistics window - Opens Dashboard with history and stats
    ''' </summary>
    Private Sub showstatistics()
        Try
            ' Use new Dashboard instead of old StatisticsForm
            Using dashboard As New DashboardForm()
                dashboard.ShowDialog(Me)
            End Using
        Catch ex As Exception
            Logger.LogToFile($"Error showing statistics: {ex.Message}", LogLevel.Warning)
            MessageBox.Show($"Error opening statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Show ELF Inspector for selected folder (now opens dedicated window)
    ''' </summary>
    Private Sub showelfinspector()
        Try
            ' Get folder path
            Dim folderPath = Txtpath.Text

            ' If no folder selected, ask user
            If String.IsNullOrEmpty(folderPath) Then
                Using fbd As New FolderBrowserDialog()
                    fbd.Description = "Select game folder to analyze"
                    If fbd.ShowDialog() = DialogResult.OK Then
                        folderPath = fbd.SelectedPath
                    Else
                        Return
                    End If
                End Using
            End If

            ' Open ELF Inspector Window
            Using frm As New ElfInspectorForm(folderPath)
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"{LocalizationService.T("elf_inspector_error")}: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Show batch processing dialog
    ''' </summary>
    Private Sub batchprocess()
        Try
            ' Ask user to select multiple folders
            Dim result = MessageBox.Show(
                LocalizationService.T("batch_description"),
                LocalizationService.T("batch_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Open folder browser multiple times
                Dim folders As New List(Of String)

                Do While folders.Count < 10
                    Using fbd As New FolderBrowserDialog()
                        fbd.Description = String.Format(LocalizationService.T("batch_select_folder"), folders.Count + 1)
                        If fbd.ShowDialog() = DialogResult.OK Then
                            If Not folders.Contains(fbd.SelectedPath) Then
                                folders.Add(fbd.SelectedPath)
                                Logger.Log(rtbStatus, $"{LocalizationService.T("batch_added")}: {Path.GetFileName(fbd.SelectedPath)}", Color.Blue)
                            End If
                        Else
                            Exit Do
                        End If
                    End Using
                Loop

                If folders.Count > 0 Then
                    ProcessBatchFolders(folders)
                End If
            End If
        Catch ex As Exception
            'MessageBox.Show($"{LocalizationService.T("batch_error_msg")}: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            ShowNotification(ex.Message, "Error", "Error Batch Process")
        End Try
    End Sub

    ''' <summary>
    ''' Process multiple folders in batch
    ''' </summary>
    Private Sub ProcessBatchFolders(folders As List(Of String))
        Try
            Dim selectedItem = TryCast(cmbPs5Sdk.SelectedItem, SdkComboItem)
            If selectedItem Is Nothing Then
                MessageBox.Show("Please select a valid PS5 SDK version first.", "Invalid SDK", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Dim totalSuccess = 0
            Dim totalFailed = 0

            Logger.Log(rtbStatus, $"=== {String.Format(LocalizationService.T("batch_starting"), folders.Count)} ===\", Color.Blue)

            For i = 0 To folders.Count - 1
                Dim folder = folders(i)
                Logger.Log(rtbStatus, $"{String.Format(LocalizationService.T("batch_processing"), i + 1, folders.Count)}: {Path.GetFileName(folder)}", Color.Purple)

                Try
                    ' Validate folder
                    Dim hasSceSys = Directory.Exists(Path.Combine(folder, "sce_sys"))
                    Dim hasEboot = File.Exists(Path.Combine(folder, "eboot.bin"))

                    If Not hasSceSys And Not hasEboot Then
                        Logger.Log(rtbStatus, $"  ✔️ {LocalizationService.T("batch_skipped")}", Color.Orange)
                        totalFailed += 1
                        Continue For
                    End If

                    ' Process folder
                    SelectedGameFolder = folder
                    ResetOperationStats()

                    ' Backup
                    If chkBackup.Checked Then
                        Backupfiles()
                    End If

                    ' Patch
                    ElfPatcher.PatchFolder(folder, selectedItem.Ps5Sdk, selectedItem.Ps4Sdk, False, Sub(msg) Log(rtbStatus, "  " & msg))

                    Logger.Log(rtbStatus, $"  ✔️ {String.Format(LocalizationService.T("batch_completed"), PatchedFilesCount, SkippedFilesCount)}", Color.Green)
                    totalSuccess += 1

                    ' Add to recent folders
                    RecentFoldersManager.AddRecentFolder(folder)
                Catch ex As Exception
                    Logger.Log(rtbStatus, $"  ✖️ {LocalizationService.T("batch_error")}: {ex.Message}", Color.Red)
                    totalFailed += 1
                End Try
            Next

            Logger.Log(rtbStatus, $"=== {String.Format(LocalizationService.T("batch_complete"), totalSuccess, totalFailed)} ===\", Color.Blue)
            'MessageBox.Show(String.Format(LocalizationService.T("batch_complete_msg"), totalSuccess, totalFailed),
            '              LocalizationService.T("batch_title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            ShowNotification(String.Format(LocalizationService.T("batch_complete_msg"), totalSuccess, totalFailed),
                             LocalizationService.T("batch_title"), "Batch Process Complete")
        Catch ex As Exception
            'MessageBox.Show($"{LocalizationService.T("batch_error_msg")}: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            ShowNotification(ex.Message, "Error", "Error Batch Process")
        End Try
    End Sub

    ''' <summary>
    ''' Open Advanced Operations dialog for ELF signing
    ''' </summary>
    Private Sub showadvancedoperations()
        Try
            Using frm As New OperationModeForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening Advanced Operations: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open Firmware Manager for downloading and managing PS5 firmware libraries
    ''' </summary>
    'Private Sub BtnFirmwareManager_Click(sender As Object, e As EventArgs)
    '    Try
    '        Using frm As New FirmwareManagerForm()
    '            frm.ShowDialog()
    '        End Using
    '    Catch ex As Exception
    '        MessageBox.Show($"Error opening Firmware Manager: {ex.Message}",
    '                      "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '    End Try
    'End Sub
    Private Sub FirmwareManagerForm()
        Try
            Using frm As New FirmwareManagerForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening Firmware Manager: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>

    ''' Open Payload Manager for managing and sending payloads to PS5
    ''' </summary>
    Private Sub showpayloadmanager()
        Try
            Using frm As New PayloadManagerForm()
                'Using frm As New PayLoadSender
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening Payload Manager: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>

    ''' Show theme selection menu with background options
    ''' </summary>
    Private Sub BtnTheme_Click(sender As Object, e As EventArgs)
        Try
            Dim themeMenu As New ContextMenuStrip()
            themeMenu.Font = New Font("Segoe UI", 9)

            ' Get current theme
            Dim currentTheme = ThemeManager.GetCurrentTheme()

            ' Add theme options
            Dim lightTheme As New ToolStripMenuItem("☀️ Light Theme") With {
                .Checked = (currentTheme = ThemeManager.AppTheme.Light)
            }
            AddHandler lightTheme.Click, Sub() ApplyTheme(ThemeManager.AppTheme.Light)
            themeMenu.Items.Add(lightTheme)

            Dim darkTheme As New ToolStripMenuItem("🌙 Dark Theme") With {
                .Checked = (currentTheme = ThemeManager.AppTheme.Dark)
            }
            AddHandler darkTheme.Click, Sub() ApplyTheme(ThemeManager.AppTheme.Dark)
            themeMenu.Items.Add(darkTheme)

            Dim systemTheme As New ToolStripMenuItem("💻 System Theme") With {
                .Checked = (currentTheme = ThemeManager.AppTheme.System)
            }
            AddHandler systemTheme.Click, Sub() ApplyTheme(ThemeManager.AppTheme.System)
            themeMenu.Items.Add(systemTheme)

            Dim highContrastTheme As New ToolStripMenuItem("🏁 High Contrast") With {
                .Checked = (currentTheme = ThemeManager.AppTheme.HighContrast)
            }
            AddHandler highContrastTheme.Click, Sub() ApplyTheme(ThemeManager.AppTheme.HighContrast)
            themeMenu.Items.Add(highContrastTheme)

            ' Add separator
            themeMenu.Items.Add(New ToolStripSeparator())

            ' Background Image options
            Dim bgImageItem As New ToolStripMenuItem("🖼️ Set Background Image")
            AddHandler bgImageItem.Click, AddressOf SetBackgroundImage_Click
            themeMenu.Items.Add(bgImageItem)

            Dim clearBgItem As New ToolStripMenuItem("🗑️ Clear Background")
            AddHandler clearBgItem.Click, AddressOf ClearBackgroundImage_Click
            themeMenu.Items.Add(clearBgItem)

            ' Add another separator
            themeMenu.Items.Add(New ToolStripSeparator())

            ' Advanced Settings option
            Dim advSettingsItem As New ToolStripMenuItem("🛠️ Advanced Settings")
            AddHandler advSettingsItem.Click, AddressOf OpenAdvancedSettings
            themeMenu.Items.Add(advSettingsItem)

            ' Operation History option
            Dim historyItem As New ToolStripMenuItem("📜 Operation History")
            AddHandler historyItem.Click, AddressOf OpenOperationHistory
            themeMenu.Items.Add(historyItem)

            ' Game Library option
            Dim gameLibraryItem As New ToolStripMenuItem("🎮 Game Library")
            AddHandler gameLibraryItem.Click, AddressOf OpenGameLibrary
            themeMenu.Items.Add(gameLibraryItem)
            '   AddHandler btnFirmwareManager.Click, AddressOf BtnFirmwareManager_Click
            ' firmware
            Dim firmwareItem As New ToolStripMenuItem("🎮 Firmware")
            AddHandler firmwareItem.Click, AddressOf FirmwareManagerForm
            themeMenu.Items.Add(firmwareItem)

            ' FTP Browser option
            Dim ftpBrowserItem As New ToolStripMenuItem("☁️ FTP Browser")
            AddHandler ftpBrowserItem.Click, AddressOf OpenFtpBrowser
            themeMenu.Items.Add(ftpBrowserItem)

            ' Custom Library Manager option
            Dim libraryManagerItem As New ToolStripMenuItem("📚 Custom Libraries")
            AddHandler libraryManagerItem.Click, AddressOf OpenCustomLibraryManager
            themeMenu.Items.Add(libraryManagerItem)

            ' Game Search option
            Dim gameSearchItem As New ToolStripMenuItem("🔍 Game Search")
            AddHandler gameSearchItem.Click, AddressOf OpenGameSearch
            themeMenu.Items.Add(gameSearchItem)

            ' Remote PKG Install
            Dim remotePkgItem As New ToolStripMenuItem("📡 Remote PKG Install")
            AddHandler remotePkgItem.Click, Sub()
                                                Using frm As New RemotePkgInstallForm()
                                                    frm.ShowDialog(Me)
                                                End Using
                                            End Sub
            themeMenu.Items.Add(remotePkgItem)

            ' Show menu below the button
            themeMenu.Show(btnTheme, New Point(0, btnTheme.Height))
        Catch ex As Exception
            MessageBox.Show($"Error loading themes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Apply selected theme (but preserve background image)
    ''' </summary>
    Private Sub ApplyTheme(theme As ThemeManager.AppTheme)
        Try
            ' Store current background image before theme change
            Dim currentBgImage = Me.BackgroundImage
            Dim currentBgLayout = Me.BackgroundImageLayout

            ' Apply theme
            ThemeManager.SetTheme(theme)
            ThemeManager.ApplyThemeToForm(Me)

            ' Restore background image if it was set
            If currentBgImage IsNot Nothing Then
                Me.BackgroundImage = currentBgImage
                Me.BackgroundImageLayout = currentBgLayout
            Else
                ' If no custom image, apply theme background
                ApplyModernBackground()
            End If

            ' Force UI refresh
            Me.Invalidate(True)
            Me.Update()
            Me.Refresh()

            Logger.Log(rtbStatus, $"✔️ Theme changed to: {theme}", Color.Green, True, Logger.LogLevel.Success)
        Catch ex As Exception
            MessageBox.Show($"Error applying theme: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Apply modern gradient background to form
    ''' </summary>
    Private Sub ApplyModernBackground()
        Try
            ' Check if custom background image is set
            ThemeManager.LoadBackgroundImagePreference()
            Dim bgImagePath = ThemeManager.GetBackgroundImagePath()

            If Not String.IsNullOrEmpty(bgImagePath) AndAlso IO.File.Exists(bgImagePath) Then
                ' Dispose old image if exists
                If Me.BackgroundImage IsNot Nothing Then
                    Dim oldImage = Me.BackgroundImage
                    Me.BackgroundImage = Nothing
                    oldImage.Dispose()
                End If

                ' Load new image
                Using fs As New IO.FileStream(bgImagePath, IO.FileMode.Open, IO.FileAccess.Read)
                    Me.BackgroundImage = Image.FromStream(fs)
                    Me.BackgroundImageLayout = ImageLayout.Stretch
                End Using

                Logger.Log(rtbStatus, $"🖼️ Background image loaded: {IO.Path.GetFileName(bgImagePath)}", Color.Green, True, Logger.LogLevel.Success)
            Else
                ' Clear any existing background image
                If Me.BackgroundImage IsNot Nothing Then
                    Dim oldImage = Me.BackgroundImage
                    Me.BackgroundImage = Nothing
                    oldImage.Dispose()
                End If

                Dim currentTheme = ThemeManager.GetCurrentTheme()

                Select Case currentTheme
                    Case ThemeManager.AppTheme.Light
                        Me.BackColor = Color.FromArgb(240, 248, 255) ' AliceBlue

                    Case ThemeManager.AppTheme.Dark
                        Me.BackColor = Color.FromArgb(25, 28, 45) ' Dark blue-purple

                    Case ThemeManager.AppTheme.HighContrast
                        Me.BackColor = Color.Black

                    Case ThemeManager.AppTheme.System
                        If ThemeManager.IsSystemDarkMode() Then
                            Me.BackColor = Color.FromArgb(25, 28, 45)
                        Else
                            Me.BackColor = Color.FromArgb(240, 248, 255)
                        End If
                End Select
            End If

            ' Enable double buffering for smooth rendering
            Me.DoubleBuffered = True
        Catch ex As Exception
            ' Silent fail - background is not critical
        End Try
    End Sub

    ''' <summary>
    ''' Custom paint event for decorative background pattern
    ''' </summary>
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Try
            ' Skip pattern if custom background image is set
            If Me.BackgroundImage IsNot Nothing Then
                Return
            End If

            Dim currentTheme = ThemeManager.GetCurrentTheme()

            ' Skip pattern for high contrast mode
            If currentTheme = ThemeManager.AppTheme.HighContrast Then
                Return
            End If

            ' Create subtle geometric pattern
            Using pen As New Pen(Color.FromArgb(20, 255, 255, 255), 1)
                Dim spacing = 50

                ' Draw diagonal lines pattern
                For x = -Me.Height To Me.Width Step spacing
                    e.Graphics.DrawLine(pen, x, 0, x + Me.Height, Me.Height)
                Next
            End Using
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Set custom background image (FIXED - WITH EXTENSIVE DEBUG)
    ''' </summary>
    Private Sub SetBackgroundImage_Click(sender As Object, e As EventArgs)
        Try
            Using ofd As New OpenFileDialog()
                ofd.Title = "Select Background Image"
                ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"

                If ofd.ShowDialog() = DialogResult.OK Then
                    Logger.Log(rtbStatus, $"📜 Saving background preference: {ofd.FileName}", Color.Blue, True, Logger.LogLevel.Info)

                    ' Save preference FIRST
                    ThemeManager.SetBackgroundImage(ofd.FileName)

                    Logger.Log(rtbStatus, $"🖼️ Loading image into form...", Color.Blue, True, Logger.LogLevel.Info)

                    ' Dispose old image
                    If Me.BackgroundImage IsNot Nothing Then
                        Dim oldImg = Me.BackgroundImage
                        Me.BackgroundImage = Nothing
                        oldImg.Dispose()
                    End If

                    ' Load new image DIRECTLY
                    Try
                        Me.BackgroundImage = Image.FromFile(ofd.FileName)
                        Me.BackgroundImageLayout = ImageLayout.Stretch
                        Logger.Log(rtbStatus, $"✔️ BackgroundImage SET: {Me.BackgroundImage IsNot Nothing}", Color.Green, True, Logger.LogLevel.Success)
                    Catch imgEx As Exception
                        Logger.Log(rtbStatus, $"✖️ Image load failed: {imgEx.Message}", Color.Red, True, Logger.LogLevel.Error)
                        Throw
                    End Try

                    ' Force complete UI refresh
                    Me.Invalidate(True)
                    Me.Update()
                    Me.Refresh()

                    ' Verify it's still set
                    Logger.Log(rtbStatus, $"✔️ After refresh: {Me.BackgroundImage IsNot Nothing}", Color.Green, True, Logger.LogLevel.Success)
                    Logger.Log(rtbStatus, $"✔️ Background applied: {IO.Path.GetFileName(ofd.FileName)}", Color.Green, True, Logger.LogLevel.Success)

                    MessageBox.Show($"Background image loaded!" & vbCrLf & $"Image null? {Me.BackgroundImage Is Nothing}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            Logger.Log(rtbStatus, $"✖️ Error: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
            MessageBox.Show($"Error: {ex.Message}" & vbCrLf & vbCrLf & ex.StackTrace, LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Clear custom background image (SIMPLIFIED)
    ''' </summary>
    Private Sub ClearBackgroundImage_Click(sender As Object, e As EventArgs)
        Try
            ThemeManager.ClearBackgroundImage()

            ' Dispose and clear image
            If Me.BackgroundImage IsNot Nothing Then
                Dim oldImg = Me.BackgroundImage
                Me.BackgroundImage = Nothing
                oldImg.Dispose()
            End If

            ' Reapply theme background
            ApplyModernBackground()

            ' Force refresh
            Me.Invalidate(True)
            Me.Refresh()

            Logger.Log(rtbStatus, " Background cleared", Color.Green, True, Logger.LogLevel.Success)
            MessageBox.Show("Background cleared!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            Logger.Log(rtbStatus, $"Error: {ex.Message}", Color.Red, True, Logger.LogLevel.Error)
            MessageBox.Show($"Error: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open advanced settings dialog
    ''' </summary>
    Private Sub OpenAdvancedSettings(sender As Object, e As EventArgs)
        Try
            Using frm As New AdvancedSettingsForm()
                If frm.ShowDialog() = DialogResult.OK Then
                    ' Settings applied - refresh UI
                    ThemeManager.ApplyThemeToForm(Me)
                    ApplyModernBackground()
                    ApplyTranslations()
                    Logger.Log(rtbStatus, "🛠️ Settings updated", Color.Green, True, Logger.LogLevel.Success)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening settings: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open operation history viewer
    ''' </summary>
    Private Sub OpenOperationHistory(sender As Object, e As EventArgs)
        Try
            Using frm As New OperationHistoryForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening history: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open Dashboard with history and statistics
    ''' </summary>
    Private Sub OpenDashboard(sender As Object, e As EventArgs)
        Try
            Using frm As New DashboardForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open Game Library window
    ''' </summary>
    Private Sub OpenGameLibrary(sender As Object, e As EventArgs)
        Try
            Using frm As New GameLibraryForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening game library: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open FTP Browser window
    ''' </summary>
    Private Sub OpenFtpBrowser(sender As Object, e As EventArgs)
        Try
            Using frm As New FtpBrowserForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening FTP browser: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open Custom Library Manager window
    ''' </summary>
    Private Sub OpenCustomLibraryManager(sender As Object, e As EventArgs)
        Try
            Using frm As New CustomLibraryForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening Custom Library Manager: {ex.Message}", LocalizationService.T("error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open Game Search window
    ''' </summary>
    Private Sub OpenGameSearch(sender As Object, e As EventArgs)
        Try
            Using frm As New GameSearchForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening Game Search: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Update button text to show keyboard shortcuts.
    ''' </summary>
    Private Sub UpdateButtonTextWithShortcuts()
        Try
            ' Main buttons - add shortcuts to text
            If BtnBrowse IsNot Nothing Then
                Dim currentText = BtnBrowse.Text.Replace(" (Ctrl+O)", "")
                BtnBrowse.Text = $"{currentText} (Ctrl+O)"
            End If

            If BtnStart IsNot Nothing Then
                Dim currentText = BtnStart.Text.Replace(" (Ctrl+S)", "")
                BtnStart.Text = $"{currentText} (Ctrl+S)"
            End If

            ' Update other buttons if they exist
            Dim btnRecent = FindControl(Of Button)(Me, "btnRecentFolders")
            If btnRecent IsNot Nothing Then
                Dim currentText = btnRecent.Text.Replace(" (Ctrl+R)", "")
                btnRecent.Text = $"{currentText} (Ctrl+R)"
            End If

            If MoonButton1 IsNot Nothing Then
                MoonButton1.Text = "📚 Toggle Library (Ctrl+L)"
            End If

            ' Update feature buttons
            Dim btnStats = FindControl(Of Button)(Me, "btnShowStatistics")
            If btnStats IsNot Nothing Then
                Dim currentText = btnStats.Text.Replace(" (Ctrl+I)", "")
                btnStats.Text = $"{currentText} (Ctrl+I)"
            End If

            Dim btnElf = FindControl(Of Button)(Me, "btnElfInspector")
            If btnElf IsNot Nothing Then
                Dim currentText = btnElf.Text.Replace(" (Ctrl+E)", "")
                btnElf.Text = $"{currentText} (Ctrl+E)"
            End If

            Dim btnBatch = FindControl(Of Button)(Me, "btnBatchProcess")
            If btnBatch IsNot Nothing Then
                Dim currentText = btnBatch.Text.Replace(" (Ctrl+B)", "")
                btnBatch.Text = $"{currentText} (Ctrl+B)"
            End If

            Dim btnThemeCtrl = FindControl(Of Button)(Me, "btnTheme")
            If btnThemeCtrl IsNot Nothing Then
                ' Add tooltip for theme button
                Dim tt = New ToolTip()
                tt.SetToolTip(btnThemeCtrl, "Theme Menu (Ctrl+T)")
            End If
        Catch ex As Exception
            ' Silent fail - this is cosmetic
            Debug.WriteLine($"UpdateButtonTextWithShortcuts: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Helper to find control recursively.
    ''' </summary>
    Private Function FindControl(Of T As Control)(parent As Control, controlName As String) As T
        If parent Is Nothing Then Return Nothing
        If TypeOf parent Is T AndAlso parent.Name = controlName Then Return DirectCast(parent, T)

        For Each child As Control In parent.Controls
            Dim found = FindControl(Of T)(child, controlName)
            If found IsNot Nothing Then Return found
        Next

        Return Nothing
    End Function

    Private Sub Form1_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter
        ' Change cursor to 'Copy' if a file/folder is being dragged over
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub Form1_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop
        ' Retrieve the array of paths
        Dim paths As String() = DirectCast(e.Data.GetData(DataFormats.FileDrop), String())

        If paths IsNot Nothing AndAlso paths.Length > 0 Then
            ' Take the first item dropped
            Dim droppedPath As String = paths(0)

            ' Verify it's a directory
            If System.IO.Directory.Exists(droppedPath) Then
                ' Update your path field (replace txtPathField with your actual control name)
                Txtpath.Text = droppedPath

                ' Optional: Provide visual feedback
                Debug.WriteLine("Folder accepted: " & droppedPath)
            Else
                MessageBox.Show("Please drop a folder, not a file.", "Folder Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End If
    End Sub

    Private Sub btnShowStatistics_Click(sender As Object, e As EventArgs) Handles btnShowStatistics.Click
        showstatistics()
    End Sub

    Private Sub btnElfInspector_Click(sender As Object, e As EventArgs) Handles btnElfInspector.Click
        showelfinspector()
    End Sub

    Private Sub btnBatchProcess_Click_1(sender As Object, e As EventArgs) Handles btnBatchProcess.Click
        batchprocess()
    End Sub

    Private Sub btnAdvancedOps_Click(sender As Object, e As EventArgs) Handles btnAdvancedOps.Click
        showadvancedoperations()
    End Sub

    Private Sub btnPayloadManager_Click(sender As Object, e As EventArgs) Handles btnPayloadManager.Click
        showpayloadmanager()
    End Sub

    Private Async Sub cmbPs5Sdk_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPs5Sdk.SelectedIndexChanged
        ' 23-01-2026 parsing gloabalization

        Dim item = TryCast(cmbPs5Sdk.SelectedItem, SdkComboItem)
        If item Is Nothing Then Return

        'fwMajor = GetFirmwareMajor(item.Ps5Sdk)

        ' Use the Key property directly since it's already an Integer
        fwMajor = item.Key
        Logger.Log(rtbStatus, "PS5 SDK: = " & fwMajor.ToString())

        'Logger.Log(rtbStatus, $"PS5 SDK: {ToFirmware(fwMajor)}")

        'If fwMajor < 7 Then
        '    lblfw.Text = "Not Recommended"
        '    lblfw.ForeColor = Color.Red
        'ElseIf fwMajor = 7 Then
        '    lblfw.Text = "Recommended"
        '    lblfw.ForeColor = Color.Black
        'Else
        '    lblfw.Text = ""
        'End If
        'opt for select case instead of nested ifs
        'lblfw.Visible = True
        'Select Case fwMajor
        '    Case 5, 4, 3, 2, 1
        '        lblfw.Text = "Experiment-Mode"
        '        lblfw.ForeColor = Color.Red
        '        lblfw.Visible = True

        '    Case 6, 7

        '        lblfw.Text = "Recommended"
        '        lblfw.ForeColor = Color.Black
        '        lblfw.Visible = True
        '    Case Else
        '        lblfw.Text = "else"
        '        lblfw.Visible = True
        'End Select
        'Downloadsprxlibs()
        If fwMajor = 6 OrElse fwMajor = 7 Then
            'chklibcpatch.Visible = True
            lblexperiment.Visible = True
            lblfw.Text = "Recommended"
            lblfw.ForeColor = Color.Black
        Else
            'chklibcpatch.Visible = False
            lblexperiment.Visible = False
            lblfw.Text = "Experiment-Mode"
            lblfw.ForeColor = Color.Red
            lblfw.Visible = True
        End If
        'default show
        chklibcpatch.Visible = True
        'Dim firmwarefolder As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fwMajor.ToString())
        'If Not FolderHasContents(firmwarefolder) Then
        '    lblfw.Text = "Fakelibs not Found for FW:" & fwMajor.ToString()
        '    lblfw.ForeColor = Color.Red
        '    Logger.Log(rtbStatus, "Fakelibs not Found for FW:" & fwMajor.ToString() & ", place them in the relevent folder", Color.Red)
        'Else
        '    lblfw.Text = ""
        '    lblfw.ForeColor = Color.Black
        'End If
    End Sub

    ' === UFS2 Image & PKG Manager Button Handlers ===

    Private Sub btnUFS2Image_Click(sender As Object, e As EventArgs) Handles btnUFS2Image.Click
        ' OpenUFS2Image()
        Dim exepath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "win-x64-selfcontained", "UFS2Tool.GUI.exe")

        ' Start if exists
        If File.Exists(exepath) Then
            Try
                Dim startInfo As New ProcessStartInfo()
                startInfo.FileName = exepath
                ' Set the working directory to the folder containing the exe
                startInfo.WorkingDirectory = Path.GetDirectoryName(exepath)

                Process.Start(startInfo)
            Catch ex As Exception
                MessageBox.Show("Error starting UFS2Tool: " & ex.Message)
            End Try
        Else
            MessageBox.Show("Executable not found: " & exepath)
        End If

    End Sub

    Private Sub btnPkgManager_Click(sender As Object, e As EventArgs) Handles btnPkgManager.Click
        OpenPackageManager()
    End Sub

    ''' <summary>
    ''' Open UFS2 Image Converter window
    ''' </summary>
    Private Sub OpenUFS2Image()
        Try
            Using frm As New UFS2ImageForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening UFS2 Image Converter: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Open PKG/FPKG Package Manager window
    ''' </summary>
    Private Sub OpenPackageManager()
        Try
            Using frm As New PackageManagerForm()
                frm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error opening Package Manager: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


End Class