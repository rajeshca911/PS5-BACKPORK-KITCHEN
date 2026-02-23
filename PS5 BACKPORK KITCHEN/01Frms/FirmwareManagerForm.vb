Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading
Imports System.IO
Imports System.Diagnostics

''' <summary>
''' Firmware Manager Form - Manage PS5 firmware downloads, extraction, and library organization.
''' Supports firmware versions 1-10 with progress tracking and automated processing.
''' </summary>
Public Class FirmwareManagerForm
    Inherits Form

    ' ===========================
    ' UI CONTROLS
    ' ===========================

    Private toolStrip As ToolStrip
    Private btnDownloadAll As ToolStripButton
    Private btnVerifyAll As ToolStripButton
    Private btnClearCache As ToolStripButton
    Private btnRefresh As ToolStripButton
    Private btnOpenArchive As ToolStripButton
    Private toolStripSeparator1 As ToolStripSeparator
    Private lblTitle As ToolStripLabel

    Private tabControl As TabControl
    Private tabInstalled As TabPage
    Private tabArchive As TabPage

    Private dgvFirmware As DataGridView
    Private dgvArchive As DataGridView
    Private pnlArchiveButtons As Panel
    Private btnDownloadSelected As Button
    Private btnDownloadAllArchive As Button
    Private lblArchiveInfo As Label
    Private pnlDetails As Panel
    Private lblDetailsTitle As Label
    Private txtDetails As TextBox
    Private pnlStatistics As GroupBox
    Private lblTotalFirmwares As Label
    Private lblTotalLibraries As Label
    Private lblTotalStorage As Label

    Private pnlOptions As Panel
    Private chkExtractCommonOnly As CheckBox
    Private chkVerifyChecksums As CheckBox
    Private lblStorageInfo As Label

    Private pnlProgress As Panel
    Private lblProgressMessage As Label
    Private progressBar As ProgressBar
    Private lblProgressDetails As Label

    Private btnPauseResume As Button
    Private btnCancelDownload As Button

    Private statusStrip As StatusStrip
    Private lblStatus As ToolStripStatusLabel
    Private tooltipProvider As ToolTip

    ' ===========================
    ' SERVICES
    ' ===========================

    Private firmwareService As FirmwareManagerService
    Private cancellationTokenSource As CancellationTokenSource

    Private isPaused As Boolean = False
    Private currentDownloadVersion As Integer = 0

    ' ===========================
    ' CONSTRUCTOR
    ' ===========================

    Public Sub New()
        InitializeComponent()
        firmwareService = New FirmwareManagerService()
        LoadFirmwareData()
        LoadArchiveData()
    End Sub

    ' ===========================
    ' INITIALIZATION
    ' ===========================

    Private Sub InitializeComponent()
        Me.Text = "Firmware Manager"
        Me.Size = New Size(900, 650)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(800, 500)
        Me.FormBorderStyle = FormBorderStyle.Sizable

        ' Create ToolStrip
        toolStrip = New ToolStrip With {
            .GripStyle = ToolStripGripStyle.Hidden,
            .Padding = New Padding(5)
        }

        lblTitle = New ToolStripLabel With {
            .Text = "💾 Firmware Manager",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }

        btnRefresh = New ToolStripButton With {
            .Text = "🔄 Refresh",
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Refresh firmware status"
        }
        AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click

        btnDownloadAll = New ToolStripButton With {
            .Text = "⬇ Download Available",
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Automatically download all firmware with available archives"
        }
        AddHandler btnDownloadAll.Click, AddressOf BtnDownloadAll_Click

        btnVerifyAll = New ToolStripButton With {
            .Text = "✓ Verify All",
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Verify integrity of all installed firmware"
        }
        AddHandler btnVerifyAll.Click, AddressOf BtnVerifyAll_Click

        btnClearCache = New ToolStripButton With {
            .Text = "🗑 Clear Cache",
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Clear temporary download cache"
        }
        AddHandler btnClearCache.Click, AddressOf BtnClearCache_Click

        btnOpenArchive = New ToolStripButton With {
            .Text = "📦 Browse Archive",
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Browse Midnight Channel firmware archive"
        }
        AddHandler btnOpenArchive.Click, AddressOf BtnOpenArchive_Click

        toolStripSeparator1 = New ToolStripSeparator()

        toolStrip.Items.AddRange({lblTitle, New ToolStripSeparator(), btnRefresh, btnOpenArchive, btnDownloadAll, btnVerifyAll, toolStripSeparator1, btnClearCache})

        ' Create TabControl
        tabControl = New TabControl With {
            .Dock = DockStyle.Fill
        }

        ' === TAB 1: INSTALLED FIRMWARE ===
        tabInstalled = New TabPage With {
            .Text = "📁 Installed Firmware",
            .Padding = New Padding(5)
        }

        ' Create DataGridView for Installed
        dgvFirmware = New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        ' Add columns for installed firmware
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Version", .HeaderText = "FW", .FillWeight = 8})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "VersionString", .HeaderText = "Ver", .FillWeight = 10})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "BuildDate", .HeaderText = "Build", .FillWeight = 12})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "ReleaseDate", .HeaderText = "Released", .FillWeight = 12})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Status", .HeaderText = "Status", .FillWeight = 15})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Libraries", .HeaderText = "Libs", .FillWeight = 8})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Size", .HeaderText = "Size", .FillWeight = 10})
        dgvFirmware.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Notes", .HeaderText = "Notes", .FillWeight = 20})
        dgvFirmware.Columns.Add(New DataGridViewButtonColumn With {.Name = "Action", .HeaderText = "Action", .FillWeight = 12, .Text = "Import", .UseColumnTextForButtonValue = False})

        AddHandler dgvFirmware.CellContentClick, AddressOf DgvFirmware_CellContentClick
        AddHandler dgvFirmware.CellFormatting, AddressOf DgvFirmware_CellFormatting

        tabInstalled.Controls.Add(dgvFirmware)

        ' === TAB 2: MIDNIGHT CHANNEL ARCHIVE ===
        tabArchive = New TabPage With {
            .Text = "🌐 Midnight Channel Archive",
            .Padding = New Padding(5)
        }

        ' Archive info label
        lblArchiveInfo = New Label With {
            .Text = "⚠️ Downloaded PUPs are ENCRYPTED - Requires decryption on jailbroken PS5 with ps5_pup_decrypt.elf (included in tools/)",
            .Dock = DockStyle.Top,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(10),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb(255, 250, 205),
            .ForeColor = Color.DarkRed
        }

        ' Archive DataGridView
        dgvArchive = New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        ' Add columns for archive
        dgvArchive.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = "Select", .HeaderText = "☑", .FillWeight = 5, .ReadOnly = False})
        dgvArchive.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Version", .HeaderText = "FW", .FillWeight = 8})
        dgvArchive.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "VersionString", .HeaderText = "Version", .FillWeight = 10})
        dgvArchive.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Size", .HeaderText = "Size", .FillWeight = 10})
        dgvArchive.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Status", .HeaderText = "Status", .FillWeight = 15})
        dgvArchive.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Notes", .HeaderText = "Description", .FillWeight = 25})
        dgvArchive.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "SHA256", .HeaderText = "SHA256 (truncated)", .FillWeight = 15})
        dgvArchive.Columns.Add(New DataGridViewButtonColumn With {.Name = "Action", .HeaderText = "Action", .FillWeight = 12, .Text = "Download", .UseColumnTextForButtonValue = False})

        AddHandler dgvArchive.CellContentClick, AddressOf DgvArchive_CellContentClick

        ' Archive buttons panel
        pnlArchiveButtons = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 50,
            .Padding = New Padding(5)
        }

        btnDownloadSelected = New Button With {
            .Text = "⬇ Download Selected",
            .Location = New Point(10, 10),
            .Size = New Size(150, 30),
            .BackColor = ColorPalette.Primary,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnDownloadSelected.Click, AddressOf BtnDownloadSelected_Click

        btnDownloadAllArchive = New Button With {
            .Text = "⬇⬇ Download All (9 FW)",
            .Location = New Point(170, 10),
            .Size = New Size(180, 30),
            .BackColor = ColorPalette.Success,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnDownloadAllArchive.Click, AddressOf BtnDownloadAllArchive_Click

        pnlArchiveButtons.Controls.AddRange({btnDownloadSelected, btnDownloadAllArchive})

        tabArchive.Controls.Add(dgvArchive)
        tabArchive.Controls.Add(pnlArchiveButtons)
        tabArchive.Controls.Add(lblArchiveInfo)

        ' Add tabs to control
        tabControl.TabPages.Add(tabInstalled)
        tabControl.TabPages.Add(tabArchive)

        ' Create Options Panel
        pnlOptions = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 90,
            .BorderStyle = BorderStyle.FixedSingle,
            .Padding = New Padding(10)
        }

        chkExtractCommonOnly = New CheckBox With {
            .Text = "Extract only common libraries (recommended)",
            .Location = New Point(15, 10),
            .AutoSize = True,
            .Checked = True
        }

        chkVerifyChecksums = New CheckBox With {
            .Text = "Verify checksums after extraction",
            .Location = New Point(15, 35),
            .AutoSize = True,
            .Checked = True
        }

        ' Initialize ToolTip
        tooltipProvider = New ToolTip With {
            .AutoPopDelay = 5000,
            .InitialDelay = 500,
            .ReshowDelay = 100,
            .ShowAlways = True
        }

        ' Set tooltips for controls
        tooltipProvider.SetToolTip(chkExtractCommonOnly, "When enabled, only extracts commonly used system libraries (libc, libkernel, libSce*, etc.) to save space and processing time. Uncheck to extract all libraries.")
        tooltipProvider.SetToolTip(chkVerifyChecksums, "When enabled, verifies SHA256 checksums after extraction to ensure file integrity. Recommended for production use.")
        tooltipProvider.SetToolTip(dgvFirmware, "Firmware versions 1-10. Click Action button to download/extract missing firmware or verify/delete existing firmware.")

        lblStorageInfo = New Label With {
            .Text = "Storage: Calculating...",
            .Location = New Point(15, 60),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9, FontStyle.Regular)
        }

        pnlOptions.Controls.AddRange({chkExtractCommonOnly, chkVerifyChecksums, lblStorageInfo})

        ' Create Progress Panel (initially hidden)
        pnlProgress = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 95,
            .BorderStyle = BorderStyle.FixedSingle,
            .Padding = New Padding(10),
            .Visible = False,
            .BackColor = Color.FromArgb(245, 245, 245)
        }

        lblProgressMessage = New Label With {
            .Text = "Progress:",
            .Location = New Point(10, 10),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }

        progressBar = New ProgressBar With {
            .Location = New Point(10, 30),
            .Size = New Size(700, 24),
            .Style = ProgressBarStyle.Continuous,
            .Minimum = 0,
            .Maximum = 100
        }

        lblProgressDetails = New Label With {
            .Text = "",
            .Location = New Point(10, 55),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 8, FontStyle.Regular)
        }

        ' Pause/Resume button
        btnPauseResume = New Button With {
            .Text = "||",
            .Location = New Point(650, 27),
            .Size = New Size(55, 30),
            .BackColor = ColorPalette.Warning,
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Visible = False,
            .Cursor = Cursors.Hand,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold)
        }
        btnPauseResume.FlatAppearance.BorderSize = 1
        AddHandler btnPauseResume.Click, AddressOf BtnPauseResume_Click

        ' Cancel button (larger, more visible)
        btnCancelDownload = New Button With {
            .Text = "✖ Cancel Download",
            .Location = New Point(720, 26),
            .Size = New Size(150, 32),
            .BackColor = ColorPalette.Error,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Visible = False,
            .Cursor = Cursors.Hand,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        btnCancelDownload.FlatAppearance.BorderSize = 1
        AddHandler btnCancelDownload.Click, AddressOf BtnCancelDownload_Click

        ' ToolTip for cancel button
        Dim btnTooltip As New ToolTip()
        btnTooltip.SetToolTip(btnCancelDownload, "Stop the firmware download")

        pnlProgress.Controls.AddRange({lblProgressMessage, progressBar, lblProgressDetails, btnPauseResume, btnCancelDownload})

        ' Create Details Panel (Right side)
        pnlDetails = New Panel With {
            .Dock = DockStyle.Right,
            .Width = 350,
            .BorderStyle = BorderStyle.FixedSingle,
            .Padding = New Padding(10)
        }

        lblDetailsTitle = New Label With {
            .Text = "Firmware Details",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(10, 10),
            .AutoSize = True
        }

        txtDetails = New TextBox With {
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .Location = New Point(10, 35),
            .Size = New Size(320, 200),
            .Font = New Font("Consolas", 9),
            .BackColor = Color.White
        }

        ' Create Statistics GroupBox
        pnlStatistics = New GroupBox With {
            .Text = "Statistics",
            .Location = New Point(10, 245),
            .Size = New Size(320, 120),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }

        lblTotalFirmwares = New Label With {
            .Text = "Total Firmwares: 0/10",
            .Location = New Point(10, 25),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }

        lblTotalLibraries = New Label With {
            .Text = "Total Libraries: 0",
            .Location = New Point(10, 50),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }

        lblTotalStorage = New Label With {
            .Text = "Total Storage: 0 MB",
            .Location = New Point(10, 75),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }

        pnlStatistics.Controls.AddRange({lblTotalFirmwares, lblTotalLibraries, lblTotalStorage})
        pnlDetails.Controls.AddRange({lblDetailsTitle, txtDetails, pnlStatistics})

        ' Create StatusStrip
        statusStrip = New StatusStrip()
        lblStatus = New ToolStripStatusLabel With {
            .Text = "Ready",
            .Spring = True,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        statusStrip.Items.Add(lblStatus)

        ' Add controls to form
        Me.Controls.Add(tabControl)
        Me.Controls.Add(pnlDetails)
        Me.Controls.Add(pnlProgress)
        Me.Controls.Add(pnlOptions)
        Me.Controls.Add(toolStrip)
        Me.Controls.Add(statusStrip)

        ' Add event handlers
        AddHandler dgvFirmware.SelectionChanged, AddressOf DgvFirmware_SelectionChanged

        ' Apply theme
        ThemeManager.ApplyThemeToForm(Me)
    End Sub

    ' ===========================
    ' DATA LOADING
    ' ===========================

    Private Sub LoadFirmwareData()
        Try
            dgvFirmware.Rows.Clear()

            Dim allMetadata = firmwareService.GetAllFirmwareMetadata()

            For Each Fversion In Constants.SupportedFirmwareVersions
                If allMetadata.ContainsKey(Fversion) Then
                    Dim meta = allMetadata(Fversion)
                    Dim row = dgvFirmware.Rows(dgvFirmware.Rows.Add())

                    ' Get firmware info from download module
                    Dim fwInfo = FirmwareDownloadModule.GetFirmwareInfo(Fversion)

                    row.Cells("version").Value = Fversion.ToString()
                    row.Cells("VersionString").Value = If(fwInfo IsNot Nothing, fwInfo.VersionString, $"{Fversion}.00.00")
                    row.Cells("BuildDate").Value = If(fwInfo IsNot Nothing, fwInfo.BuildDate, "Unknown")
                    row.Cells("ReleaseDate").Value = If(fwInfo IsNot Nothing, fwInfo.ReleaseDate, "Unknown")
                    row.Cells("Status").Value = GetStatusText(meta.Status)
                    row.Cells("Libraries").Value = meta.LibraryCount.ToString()
                    row.Cells("Size").Value = FirmwareManagerService.FormatBytes(meta.SizeBytes)
                    row.Cells("Notes").Value = If(fwInfo IsNot Nothing, fwInfo.Notes, "")
                    row.Cells("Action").Value = GetActionText(meta.Status, Fversion)

                    ' Store metadata in row tag
                    row.Tag = meta
                End If
            Next

            UpdateStorageInfo()
            UpdateStatistics()
            UpdateStatus("Firmware data loaded")
        Catch ex As Exception
            Logger.LogToFile($"Error loading firmware data: {ex.Message}", LogLevel.Error)
            UpdateStatus($"Error: {ex.Message}")
        End Try
    End Sub

    Private Function GetStatusText(status As FirmwareManagerService.FirmwareStatus) As String
        Select Case status
            Case FirmwareManagerService.FirmwareStatus.NotFound
                Return "Not Found"
            Case FirmwareManagerService.FirmwareStatus.Downloading
                Return "Downloading..."
            Case FirmwareManagerService.FirmwareStatus.Downloaded
                Return "Downloaded"
            Case FirmwareManagerService.FirmwareStatus.Extracting
                Return "Extracting..."
            Case FirmwareManagerService.FirmwareStatus.Extracted
                Return "Extracted"
            Case FirmwareManagerService.FirmwareStatus.Processing
                Return "Processing..."
            Case FirmwareManagerService.FirmwareStatus.Processed
                Return "✓ Ready"
            Case FirmwareManagerService.FirmwareStatus.Verified
                Return "✓ Verified"
            Case FirmwareManagerService.FirmwareStatus.Error
                Return "✗ Error"
            Case Else
                Return "Unknown"
        End Select
    End Function

    Private Function GetActionText(status As FirmwareManagerService.FirmwareStatus, version As Integer) As String
        Select Case status
            Case FirmwareManagerService.FirmwareStatus.NotFound
                ' Check if automatic download is available
                If FirmwareDownloadModule.IsDownloadAvailable(version) Then
                    Return "Download"
                Else
                    Return "Import PUP"
                End If
            Case FirmwareManagerService.FirmwareStatus.Downloaded
                Return "Extract"
            Case FirmwareManagerService.FirmwareStatus.Processed, FirmwareManagerService.FirmwareStatus.Verified
                Return "Delete"
            Case Else
                Return "..."
        End Select
    End Function

    Private Sub LoadArchiveData()
        Try
            dgvArchive.Rows.Clear()

            ' Load all firmware with available downloads from database
            For Each Fversion In Constants.SupportedFirmwareVersions
                Dim fwInfo = FirmwareDownloadModule.GetFirmwareInfo(Fversion)

                ' Only show firmware that have download URLs available
                If fwInfo IsNot Nothing AndAlso FirmwareDownloadModule.IsDownloadAvailable(Fversion) Then
                    Dim row = dgvArchive.Rows(dgvArchive.Rows.Add())

                    ' Checkbox (unchecked by default)
                    row.Cells("Select").Value = False

                    ' Basic info
                    row.Cells("version").Value = Fversion.ToString()
                    row.Cells("VersionString").Value = fwInfo.VersionString
                    'row.Cells("Size").Value = fwInfo.Notes.Split("("c).LastOrDefault()?.Replace(")", "").Trim() ' Extract size from notes
                    Dim sizeText As String = ""

                    If Not String.IsNullOrWhiteSpace(fwInfo.Notes) Then
                        sizeText = fwInfo.Notes.
        Split("("c).
        LastOrDefault()?.
        Replace(")", "").
        Trim()
                    End If

                    row.Cells("Size").Value = sizeText

                    ' Check if already installed
                    Dim meta = firmwareService.GetFirmwareMetadata(Fversion)
                    If meta IsNot Nothing AndAlso (meta.Status = FirmwareManagerService.FirmwareStatus.Processed OrElse meta.Status = FirmwareManagerService.FirmwareStatus.Verified) Then
                        row.Cells("Status").Value = "✓ Installed"
                        row.Cells("Status").Style.ForeColor = ColorPalette.Success
                    Else
                        row.Cells("Status").Value = "Available"
                        row.Cells("Status").Style.ForeColor = Color.Gray
                    End If

                    row.Cells("Notes").Value = fwInfo.Notes
                    row.Cells("SHA256").Value = If(fwInfo.Checksum?.Length > 16, fwInfo.Checksum.Substring(0, 16) & "...", fwInfo.Checksum)
                    row.Cells("Action").Value = "Download"

                    ' Store Fversion in tag
                    row.Tag = Fversion
                End If
            Next

            Logger.LogToFile($"Loaded {dgvArchive.Rows.Count} firmware from Midnight Channel archive", LogLevel.Debug)
        Catch ex As Exception
            Logger.LogToFile($"Error loading archive data: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    ' ===========================
    ' EVENT HANDLERS
    ' ===========================

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
        LoadFirmwareData()
    End Sub

    Private Async Sub BtnDownloadAll_Click(sender As Object, e As EventArgs)
        ' Count how many firmwares have automatic download available
        Dim availableCount = Constants.SupportedFirmwareVersions.Count(Function(v) FirmwareDownloadModule.IsDownloadAvailable(v))

        Dim result = MessageBox.Show(
            $"This will automatically download and extract all firmware versions with available archives ({availableCount} available)." & vbCrLf & vbCrLf &
            "Firmware versions without automatic download can be imported manually using 'Import PUP'." & vbCrLf & vbCrLf &
            "This may take a long time and use significant disk space. Continue?",
            "Confirm Download All Available",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If result = DialogResult.Yes Then
            Await DownloadAllFirmwaresAsync()
        End If
    End Sub

    Private Async Sub BtnVerifyAll_Click(sender As Object, e As EventArgs)
        Try
            UpdateStatus("Verifying all firmwares...")
            Dim results = firmwareService.VerifyAllFirmwares()

            Dim successCount = results.Values.Where(Function(r) r).Count()
            Dim totalCount = results.Count

            UpdateStatus($"Verification complete: {successCount}/{totalCount} passed")
            LoadFirmwareData()

            ModernUIHelpers.ShowToast(Me, $"Verified {successCount}/{totalCount} firmwares", ToastIcon.Success)
        Catch ex As Exception
            Logger.LogToFile($"Error verifying firmwares: {ex.Message}", LogLevel.Error)
            UpdateStatus($"Verification error: {ex.Message}")
        End Try
    End Sub

    Private Sub BtnClearCache_Click(sender As Object, e As EventArgs)
        Dim result = MessageBox.Show(
            "This will clear the firmware download cache (PUP files and temporary extraction folders)." & vbCrLf & vbCrLf &
            "Extracted libraries will NOT be deleted." & vbCrLf & vbCrLf &
            "Continue?",
            "Confirm Clear Cache",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        )

        If result = DialogResult.Yes Then
            Dim cacheResult = firmwareService.ClearCache()

            If cacheResult.Success Then
                If cacheResult.BytesFreed > 0 Then
                    Dim freedText = FirmwareManagerService.FormatBytes(cacheResult.BytesFreed)
                    UpdateStatus($"Cache cleared - freed {freedText}")
                    ModernUIHelpers.ShowToast(Me, $"Cache cleared - freed {freedText}", ToastIcon.Success)
                Else
                    UpdateStatus("Cache was already empty")
                    ModernUIHelpers.ShowToast(Me, "Cache was already empty", ToastIcon.Info)
                End If

                ' Refresh storage info
                UpdateStorageInfo()
            Else
                MessageBox.Show($"Error clearing cache: {cacheResult.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End If
    End Sub

    Private Async Sub DgvFirmware_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> dgvFirmware.Columns("Action").Index Then
            Return
        End If

        Dim meta = TryCast(dgvFirmware.Rows(e.RowIndex).Tag, FirmwareManagerService.FirmwareMetadata)
        If meta Is Nothing Then
            Return
        End If

        Select Case meta.Status
            Case FirmwareManagerService.FirmwareStatus.NotFound
                ' Check if automatic download is available
                If FirmwareDownloadModule.IsDownloadAvailable(meta.Version) Then
                    ' Automatic download from archive
                    Await DownloadAndExtractFirmwareAsync(meta.Version)
                Else
                    ' Manual import via file dialog
                    ImportPupFile(meta.Version)
                End If

            Case FirmwareManagerService.FirmwareStatus.Downloaded
                ' Extract imported/downloaded PUP
                Await ExtractFirmwareAsync(meta.Version)

            Case FirmwareManagerService.FirmwareStatus.Processed, FirmwareManagerService.FirmwareStatus.Verified
                DeleteFirmware(meta.Version)
        End Select
    End Sub

    Private Sub DgvFirmware_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 Then Return

        Dim meta = TryCast(dgvFirmware.Rows(e.RowIndex).Tag, FirmwareManagerService.FirmwareMetadata)
        If meta Is Nothing Then Return

        ' Color code status
        If e.ColumnIndex = dgvFirmware.Columns("Status").Index Then
            Select Case meta.Status
                Case FirmwareManagerService.FirmwareStatus.Processed, FirmwareManagerService.FirmwareStatus.Verified
                    e.CellStyle.ForeColor = ColorPalette.Success
                Case FirmwareManagerService.FirmwareStatus.Error
                    e.CellStyle.ForeColor = ColorPalette.Error
                Case FirmwareManagerService.FirmwareStatus.Downloading, FirmwareManagerService.FirmwareStatus.Extracting, FirmwareManagerService.FirmwareStatus.Processing
                    e.CellStyle.ForeColor = ColorPalette.Warning
            End Select
        End If
    End Sub

    Private Async Sub DgvArchive_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return

        ' Handle checkbox column
        If e.ColumnIndex = dgvArchive.Columns("Select").Index Then
            ' Toggle checkbox
            Dim cellValue = dgvArchive.Rows(e.RowIndex).Cells("Select").Value
            Dim currentValue As Boolean = If(cellValue IsNot Nothing, CBool(cellValue), False)
            dgvArchive.Rows(e.RowIndex).Cells("Select").Value = Not currentValue
            Return
        End If

        ' Handle action button column
        If e.ColumnIndex = dgvArchive.Columns("Action").Index Then
            Dim version As Integer = CInt(dgvArchive.Rows(e.RowIndex).Tag)
            Await DownloadAndExtractFirmwareAsync(version)

            ' Refresh both tabs after download
            LoadFirmwareData()
            LoadArchiveData()
        End If
    End Sub

    Private Async Sub BtnDownloadSelected_Click(sender As Object, e As EventArgs)
        Try
            ' Get selected firmware versions
            Dim selectedVersions As New List(Of Integer)

            For Each row As DataGridViewRow In dgvArchive.Rows
                Dim cellValue = row.Cells("Select").Value
                Dim isChecked As Boolean = If(cellValue IsNot Nothing, CBool(cellValue), False)
                If isChecked Then
                    selectedVersions.Add(CInt(row.Tag))
                End If
            Next

            If selectedVersions.Count = 0 Then
                MessageBox.Show("Please select at least one firmware to download.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim result = MessageBox.Show(
                $"Download and extract {selectedVersions.Count} selected firmware version(s)?" & vbCrLf & vbCrLf &
                $"Versions: {String.Join(", ", selectedVersions)}" & vbCrLf & vbCrLf &
                "This may take a while depending on your connection speed.",
                "Confirm Download",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            )

            If result = DialogResult.Yes Then
                For Each fversion In selectedVersions
                    Await DownloadAndExtractFirmwareAsync(fversion)
                Next

                LoadFirmwareData()
                LoadArchiveData()
                ModernUIHelpers.ShowToast(Me, $"Downloaded {selectedVersions.Count} firmware version(s)", ToastIcon.Success)
            End If
        Catch ex As Exception
            Logger.LogToFile($"Error downloading selected firmware: {ex.Message}", LogLevel.Error)
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub BtnDownloadAllArchive_Click(sender As Object, e As EventArgs)
        Try
            Dim availableVersions As New List(Of Integer)

            ' Get all available firmware versions from archive
            For Each row As DataGridViewRow In dgvArchive.Rows
                availableVersions.Add(CInt(row.Tag))
            Next

            Dim result = MessageBox.Show(
                $"Download and extract all {availableVersions.Count} firmware versions from Midnight Channel archive?" & vbCrLf & vbCrLf &
                $"Versions: {String.Join(", ", availableVersions)}" & vbCrLf & vbCrLf &
                "This will take a significant amount of time and disk space." & vbCrLf &
                "The process cannot be cancelled once started." & vbCrLf & vbCrLf &
                "Continue?",
                "Confirm Download All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            )

            If result = DialogResult.Yes Then
                Dim successCount = 0
                Dim failCount = 0

                For Each Fversion In availableVersions
                    ' Check if already installed
                    Dim meta = firmwareService.GetFirmwareMetadata(Fversion)
                    If meta IsNot Nothing AndAlso (meta.Status = FirmwareManagerService.FirmwareStatus.Processed OrElse meta.Status = FirmwareManagerService.FirmwareStatus.Verified) Then
                        Logger.LogToFile($"Firmware {Fversion} already installed, skipping", LogLevel.Info)
                        Continue For
                    End If

                    ' Download and extract
                    Try
                        Await DownloadAndExtractFirmwareAsync(Fversion)
                        successCount += 1
                    Catch ex As Exception
                        Logger.LogToFile($"Failed to download firmware {Fversion}: {ex.Message}", LogLevel.Error)
                        failCount += 1
                    End Try
                Next

                LoadFirmwareData()
                LoadArchiveData()

                Dim summaryMsg = $"Download complete!" & vbCrLf & vbCrLf &
                                $"Success: {successCount}" & vbCrLf &
                                $"Failed: {failCount}" & vbCrLf &
                                $"Already Installed: {availableVersions.Count - successCount - failCount}"

                MessageBox.Show(summaryMsg, "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            Logger.LogToFile($"Error downloading all firmware: {ex.Message}", LogLevel.Error)
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnOpenArchive_Click(sender As Object, e As EventArgs)
        ' Switch to archive tab
        tabControl.SelectedTab = tabArchive

        ' Optionally open in browser
        Dim result = MessageBox.Show(
            "View Midnight Channel firmware archive in your web browser?",
            "Open Archive Website",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If result = DialogResult.Yes Then
            Try
                Process.Start(New ProcessStartInfo With {
                    .FileName = "https://archive.midnightchannel.net/SonyPS/Firmware/index.php?cat=PS5REC",
                    .UseShellExecute = True
                })
            Catch ex As Exception
                Logger.LogToFile($"Error opening archive URL: {ex.Message}", LogLevel.Error)
                MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    ' ===========================
    ' FIRMWARE OPERATIONS
    ' ===========================

    Private Sub ImportPupFile(version As Integer)
        Try
            ' Open file dialog
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Title = $"Select Firmware {version} PUP File"

                openFileDialog.Filter = "PS5 Update Files (*.PUP;*.pup.dec)|*.PUP;*.pup.dec|Decrypted PUP (*.pup.dec)|*.pup.dec|Encrypted PUP (*.PUP)|*.PUP|All Files (*.*)|*.*"

                openFileDialog.FilterIndex = 1
                openFileDialog.RestoreDirectory = True
                openFileDialog.Multiselect = False

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    Dim sourcePath = openFileDialog.FileName

                    ' Show info dialog
                    Dim fileInfo As New FileInfo(sourcePath)
                    Dim sizeText = FirmwareManagerService.FormatBytes(fileInfo.Length)

                    Dim isDecrypted = sourcePath.ToLower().EndsWith(".dec")
                    Dim fileType = If(isDecrypted, "Decrypted PUP (.pup.dec)", "Encrypted PUP (.PUP)")

                    Dim confirmMsg = $"Import this PUP file for Firmware {version}?" & vbCrLf & vbCrLf &
                                   $"File: {Path.GetFileName(sourcePath)}" & vbCrLf &
                                   $"Size: {sizeText}" & vbCrLf &
                                   $"Type: {fileType}" & vbCrLf & vbCrLf &
                                   If(isDecrypted,
                                      "✓ Decrypted - Extraction will work automatically!",
                                      "⚠ Encrypted - Extraction will fail. Use .pup.dec instead.")

                    Dim result = MessageBox.Show(confirmMsg, "Confirm Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                    If result = DialogResult.Yes Then
                        ' Import PUP
                        Dim importResult = FirmwareDownloadModule.ImportPupFile(sourcePath, version)

                        If importResult.Success Then
                            ' Update metadata
                            firmwareService.UpdateFirmwareMetadata(version, Sub(meta)
                                                                                meta.Status = FirmwareManagerService.FirmwareStatus.Downloaded
                                                                                meta.DownloadedDate = DateTime.Now
                                                                                meta.SizeBytes = fileInfo.Length
                                                                            End Sub)

                            LoadFirmwareData()
                            UpdateStorageInfo()
                            ModernUIHelpers.ShowToast(Me, $"PUP imported successfully. Click 'Extract' to extract libraries.", ToastIcon.Success)
                        Else
                            MessageBox.Show($"Import failed: {importResult.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End If
                    End If
                End If
            End Using
        Catch ex As Exception
            Logger.LogToFile($"Error importing PUP: {ex.Message}", LogLevel.Error)
            MessageBox.Show($"Error importing PUP: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Function ExtractFirmwareAsync(version As Integer) As Task
        Try
            ' Check storage quota
            Const estimatedLibSize As Long = 400L * 1024 * 1024  ' 400 MB estimate for extracted libs
            Dim quotaCheck = firmwareService.CanDownloadFirmware(estimatedLibSize)

            If Not quotaCheck.Allowed Then
                MessageBox.Show(quotaCheck.Reason, "Storage Quota Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            cancellationTokenSource = New CancellationTokenSource()
            ShowProgress($"Extracting firmware {version}...")

            ' Update status
            firmwareService.UpdateFirmwareMetadata(version, Sub(meta) meta.Status = FirmwareManagerService.FirmwareStatus.Extracting)
            LoadFirmwareData()

            ' Extract with progress
            Dim extractProgress = New Progress(Of FirmwareExtractorModule.ExtractionProgress)(
                Sub(progress)
                    Me.Invoke(Sub()
                                  progressBar.Value = progress.PercentComplete
                                  lblProgressDetails.Text = progress.FormattedProgress
                              End Sub)
                End Sub
            )

            Dim extractResult = Await FirmwareExtractorModule.ExtractFirmwareAsync(version, extractProgress, cancellationTokenSource.Token)

            If Not extractResult.Success Then
                firmwareService.UpdateFirmwareMetadata(version, Sub(meta)
                                                                    meta.Status = FirmwareManagerService.FirmwareStatus.Error
                                                                    meta.ErrorMessage = extractResult.ErrorMessage
                                                                End Sub)
                HideProgress()
                LoadFirmwareData()
                MessageBox.Show($"Extraction failed: {extractResult.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Process libraries if needed
            If chkExtractCommonOnly.Checked Then
                ShowProgress($"Processing libraries for firmware {version}...")
                firmwareService.UpdateFirmwareMetadata(version, Sub(meta) meta.Status = FirmwareManagerService.FirmwareStatus.Processing)

                Dim processProgress = New Progress(Of LibraryProcessorModule.ProcessingProgress)(
                    Sub(progress)
                        Me.Invoke(Sub()
                                      progressBar.Value = progress.PercentComplete
                                      lblProgressDetails.Text = progress.FormattedProgress
                                  End Sub)
                    End Sub
                )

                Dim processResult = Await LibraryProcessorModule.ProcessLibrariesAsync(version, True, processProgress, cancellationTokenSource.Token)

                If Not processResult.Success Then
                    Logger.LogToFile($"Library processing had issues: {processResult.ErrorMessage}", LogLevel.Warning)
                End If
            End If

            ' Update metadata
            firmwareService.UpdateFirmwareMetadata(version, Sub(meta)
                                                                meta.Status = FirmwareManagerService.FirmwareStatus.Processed
                                                                meta.LibraryCount = extractResult.LibraryCount
                                                                meta.ExtractedDate = DateTime.Now
                                                                meta.ProcessedDate = DateTime.Now
                                                            End Sub)

            HideProgress()
            LoadFirmwareData()
            UpdateStorageInfo()
            ModernUIHelpers.ShowToast(Me, $"Firmware {version} ready! {extractResult.LibraryCount} libraries extracted.", ToastIcon.Success)
        Catch ex As Exception
            Logger.LogToFile($"Error extracting firmware {version}: {ex.Message}", LogLevel.Error)
            HideProgress()
            LoadFirmwareData()
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            cancellationTokenSource?.Dispose()
            cancellationTokenSource = Nothing
        End Try
    End Function

    Private Async Function DownloadAndExtractFirmwareAsync(version As Integer) As Task
        Try
            ' Check storage quota before download (estimate 600MB per firmware)
            Const estimatedFirmwareSize As Long = 600L * 1024 * 1024  ' 600 MB
            Dim quotaCheck = firmwareService.CanDownloadFirmware(estimatedFirmwareSize)

            If Not quotaCheck.Allowed Then
                MessageBox.Show(quotaCheck.Reason, "Storage Quota Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            cancellationTokenSource = New CancellationTokenSource()

            currentDownloadVersion = version

            ShowProgress($"Downloading firmware {version}...")

            ' Update status
            firmwareService.UpdateFirmwareMetadata(version, Sub(meta) meta.Status = FirmwareManagerService.FirmwareStatus.Downloading)
            LoadFirmwareData()

            ' Download
            Dim downloadProgress = New Progress(Of FirmwareDownloadModule.DownloadProgress)(
                Sub(progress)
                    Me.Invoke(Sub()
                                  progressBar.Value = progress.PercentComplete
                                  lblProgressDetails.Text = $"{progress.FormattedProgress} | Speed: {progress.FormattedSpeed} | ETA: {progress.FormattedETA}"
                              End Sub)
                End Sub
            )

            Dim downloadResult = Await FirmwareDownloadModule.DownloadFirmwareAsync(version, downloadProgress, cancellationTokenSource.Token)

            If Not downloadResult.Success Then
                firmwareService.UpdateFirmwareMetadata(version, Sub(meta)
                                                                    meta.Status = FirmwareManagerService.FirmwareStatus.Error
                                                                    meta.ErrorMessage = downloadResult.ErrorMessage
                                                                End Sub)
                HideProgress()
                LoadFirmwareData()
                MessageBox.Show($"Download failed: {downloadResult.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Extract
            ShowProgress($"Extracting firmware {version}...")
            firmwareService.UpdateFirmwareMetadata(version, Sub(meta) meta.Status = FirmwareManagerService.FirmwareStatus.Extracting)

            Dim extractProgress = New Progress(Of FirmwareExtractorModule.ExtractionProgress)(
                Sub(progress)
                    Me.Invoke(Sub()
                                  progressBar.Value = progress.PercentComplete
                                  lblProgressDetails.Text = progress.FormattedProgress
                              End Sub)
                End Sub
            )

            Dim extractResult = Await FirmwareExtractorModule.ExtractFirmwareAsync(version, extractProgress, cancellationTokenSource.Token)

            If Not extractResult.Success Then
                firmwareService.UpdateFirmwareMetadata(version, Sub(meta)
                                                                    meta.Status = FirmwareManagerService.FirmwareStatus.Error
                                                                    meta.ErrorMessage = extractResult.ErrorMessage
                                                                End Sub)
                HideProgress()
                LoadFirmwareData()
                MessageBox.Show($"Extraction failed: {extractResult.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Update metadata
            firmwareService.UpdateFirmwareMetadata(version, Sub(meta)
                                                                meta.Status = FirmwareManagerService.FirmwareStatus.Processed
                                                                meta.LibraryCount = extractResult.LibraryCount
                                                                meta.ProcessedDate = DateTime.Now
                                                            End Sub)

            HideProgress()
            LoadFirmwareData()
            ModernUIHelpers.ShowToast(Me, $"Firmware {version} ready! {extractResult.LibraryCount} libraries extracted.", ToastIcon.Success)
        Catch ex As Exception
            Logger.LogToFile($"Error processing firmware {version}: {ex.Message}", LogLevel.Error)
            HideProgress()
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            cancellationTokenSource?.Dispose()
            cancellationTokenSource = Nothing
        End Try
    End Function

    Private Async Function DownloadAllFirmwaresAsync() As Task
        For Each Fversion In Constants.SupportedFirmwareVersions
            Dim meta = firmwareService.GetFirmwareMetadata(Fversion)

            ' Only download if status is NotFound AND automatic download is available
            If meta IsNot Nothing AndAlso meta.Status = FirmwareManagerService.FirmwareStatus.NotFound AndAlso FirmwareDownloadModule.IsDownloadAvailable(Fversion) Then
                Await DownloadAndExtractFirmwareAsync(Fversion)
            End If
        Next

        ModernUIHelpers.ShowToast(Me, "All available firmwares downloaded and extracted", ToastIcon.Success)
    End Function

    Private Sub DeleteFirmware(version As Integer)
        Dim result = MessageBox.Show(
            $"Delete firmware {version} and all its libraries?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        )

        If result = DialogResult.Yes Then
            If firmwareService.DeleteFirmware(version) Then
                LoadFirmwareData()
                ModernUIHelpers.ShowToast(Me, $"Firmware {version} deleted", ToastIcon.Success)
            Else
                MessageBox.Show("Failed to delete firmware", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End If
    End Sub

    ' ===========================
    ' UI HELPERS
    ' ===========================

    Private Sub ShowProgress(message As String)
        pnlProgress.Visible = True
        lblProgressMessage.Text = message
        progressBar.Value = 0
        lblProgressDetails.Text = ""

        ' Show only cancel button (pause/resume coming in v2.3)
        btnPauseResume.Visible = False
        btnCancelDownload.Visible = True
        isPaused = False

    End Sub

    Private Sub HideProgress()
        pnlProgress.Visible = False
        progressBar.Value = 0

        ' Hide pause/cancel buttons
        btnPauseResume.Visible = False
        btnCancelDownload.Visible = False
        currentDownloadVersion = 0
    End Sub

    ''' <summary>
    ''' Handle pause/resume button click
    ''' </summary>
    Private Sub BtnPauseResume_Click(sender As Object, e As EventArgs)
        ' Pause/Resume functionality coming in v2.3
        ' For now, just show informative message without canceling download
        MessageBox.Show(
            "⏸ Pause/Resume Feature Coming Soon!" & vbCrLf & vbCrLf &
            "This feature will allow you to:" & vbCrLf &
            "  • Pause downloads and preserve progress" & vbCrLf &
            "  • Resume from where you left off" & vbCrLf &
            "  • Save bandwidth during peak hours" & vbCrLf & vbCrLf &
            "For now, use the ✖ Cancel button to stop the download." & vbCrLf &
            "You can restart it later (download will start from beginning)." & vbCrLf & vbCrLf &
            "📅 Planned for version 2.3 - Stay tuned!",
            "Feature In Development",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        )
    End Sub

    ''' <summary>
    ''' Handle cancel button click
    ''' </summary>
    Private Sub BtnCancelDownload_Click(sender As Object, e As EventArgs)
        Dim result = MessageBox.Show(
            "Cancel the current download?" & vbCrLf &
            "Downloaded data will be lost.",
            "Cancel Download",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        )

        If result = DialogResult.Yes Then
            cancellationTokenSource?.Cancel()
            lblProgressMessage.Text = "Download cancelled"
            Logger.LogToFile($"Download cancelled for firmware {currentDownloadVersion}", LogLevel.Warning)

            ' Hide progress after short delay
            Task.Delay(1500).ContinueWith(Sub()
                                              Me.Invoke(Sub() HideProgress())
                                          End Sub)
        End If

    End Sub

    Private Sub UpdateStatus(message As String)
        lblStatus.Text = message
        Logger.LogToFile(message, LogLevel.Info)
    End Sub

    Private Sub UpdateStorageInfo()
        Try
            Dim storageStatus = firmwareService.GetStorageStatus()
            lblStorageInfo.Text = firmwareService.GetStorageStatusMessage()

            ' Color code based on storage status
            Select Case storageStatus
                Case FirmwareManagerService.StorageStatus.Normal
                    lblStorageInfo.ForeColor = ColorPalette.Success
                Case FirmwareManagerService.StorageStatus.Warning
                    lblStorageInfo.ForeColor = ColorPalette.Warning
                Case FirmwareManagerService.StorageStatus.Critical
                    lblStorageInfo.ForeColor = Color.OrangeRed
                Case FirmwareManagerService.StorageStatus.OverQuota
                    lblStorageInfo.ForeColor = ColorPalette.Error
                    lblStorageInfo.Font = New Font(lblStorageInfo.Font, FontStyle.Bold)
            End Select
        Catch ex As Exception
            lblStorageInfo.Text = "Storage: Error calculating"
            lblStorageInfo.ForeColor = ColorPalette.Error
        End Try
    End Sub

    Private Sub UpdateStatistics()
        Try
            Dim allMetadata = firmwareService.GetAllFirmwareMetadata()

            ' Count ready firmwares
            Dim readyCount = allMetadata.Values.Where(Function(m) m.Status = FirmwareManagerService.FirmwareStatus.Processed OrElse m.Status = FirmwareManagerService.FirmwareStatus.Verified).Count()

            ' Sum libraries
            Dim totalLibs As Integer = allMetadata.Values.Sum(Function(m) m.LibraryCount)

            ' Sum storage
            Dim totalStorage As Long = allMetadata.Values.Sum(Function(m) m.SizeBytes)

            lblTotalFirmwares.Text = $"Ready Firmwares: {readyCount}/10"
            lblTotalFirmwares.ForeColor = If(readyCount > 0, ColorPalette.Success, Color.Gray)

            lblTotalLibraries.Text = $"Total Libraries: {totalLibs:N0}"
            lblTotalLibraries.ForeColor = If(totalLibs > 0, ColorPalette.Success, Color.Gray)

            lblTotalStorage.Text = $"Total Storage: {FirmwareManagerService.FormatBytes(totalStorage)}"
            lblTotalStorage.ForeColor = If(totalStorage > 0, ColorPalette.Success, Color.Gray)
        Catch ex As Exception
            Logger.LogToFile($"Error updating statistics: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    Private Sub DgvFirmware_SelectionChanged(sender As Object, e As EventArgs)
        Try
            If dgvFirmware.SelectedRows.Count = 0 Then
                txtDetails.Text = "Select a firmware to view details"
                Return
            End If

            Dim selectedRow = dgvFirmware.SelectedRows(0)
            Dim meta = TryCast(selectedRow.Tag, FirmwareManagerService.FirmwareMetadata)

            If meta Is Nothing Then
                txtDetails.Text = "No firmware data available"
                Return
            End If

            ' Get firmware info
            Dim fwInfo = FirmwareDownloadModule.GetFirmwareInfo(meta.Version)

            ' Build detailed info text
            Dim details As New System.Text.StringBuilder()
            details.AppendLine($"=== FIRMWARE {meta.Version} ===")
            details.AppendLine()

            If fwInfo IsNot Nothing Then
                details.AppendLine($"Version String: {fwInfo.VersionString}")
                details.AppendLine($"Build Date:     {fwInfo.BuildDate}")
                details.AppendLine($"Release Date:   {fwInfo.ReleaseDate}")
                details.AppendLine()
                details.AppendLine($"Notes: {fwInfo.Notes}")
                details.AppendLine()
            End If

            details.AppendLine($"Status:         {GetStatusText(meta.Status)}")
            details.AppendLine($"Libraries:      {meta.LibraryCount:N0}")
            details.AppendLine($"Size:           {FirmwareManagerService.FormatBytes(meta.SizeBytes)}")
            details.AppendLine()

            If meta.DownloadedDate.HasValue Then
                details.AppendLine($"Downloaded:     {meta.DownloadedDate.Value:yyyy-MM-dd HH:mm}")
            End If

            If meta.ExtractedDate.HasValue Then
                details.AppendLine($"Extracted:      {meta.ExtractedDate.Value:yyyy-MM-dd HH:mm}")
            End If

            If meta.ProcessedDate.HasValue Then
                details.AppendLine($"Processed:      {meta.ProcessedDate.Value:yyyy-MM-dd HH:mm}")
            End If

            If Not String.IsNullOrEmpty(meta.ErrorMessage) Then
                details.AppendLine()
                details.AppendLine($"ERROR: {meta.ErrorMessage}")
            End If

            ' Check if PUP exists
            Dim pupPath = FirmwareDownloadModule.GetCachedPupPath(meta.Version)
            If File.Exists(pupPath) Then
                Dim pupInfo As New FileInfo(pupPath)
                details.AppendLine()
                details.AppendLine($"PUP File: {Path.GetFileName(pupPath)}")
                details.AppendLine($"PUP Size: {FirmwareManagerService.FormatBytes(pupInfo.Length)}")
            End If

            ' Check fakelib directory
            Dim fakelibDir = Constants.GetFakelibDirectory(meta.Version)
            If Directory.Exists(fakelibDir) Then
                Dim libFiles = Directory.GetFiles(fakelibDir, "*.sprx", SearchOption.TopDirectoryOnly)
                details.AppendLine()
                details.AppendLine($"Library Directory: {fakelibDir}")
                details.AppendLine($"SPRX Files: {libFiles.Length}")
            End If

            txtDetails.Text = details.ToString()
        Catch ex As Exception
            Logger.LogToFile($"Error updating details: {ex.Message}", LogLevel.Error)
            txtDetails.Text = $"Error loading details: {ex.Message}"
        End Try
    End Sub

End Class