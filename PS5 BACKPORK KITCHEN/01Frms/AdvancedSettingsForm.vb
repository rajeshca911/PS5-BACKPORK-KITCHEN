Imports System.IO
Imports System.Windows.Forms

Public Class AdvancedSettingsForm
    Inherits Form

    ' UI Controls
    Private tabControl As TabControl

    Private tabGeneral As TabPage
    Private tabAppearance As TabPage
    Private tabAdvanced As TabPage

    ' General Tab
    Private grpBackup As GroupBox

    Private chkAutoBackup As CheckBox
    Private chkBackupTimestamp As CheckBox
    Private txtBackupLocation As TextBox
    Private btnBrowseBackup As Button

    ' Appearance Tab
    Private grpTheme As GroupBox

    Private rbLight As RadioButton
    Private rbDark As RadioButton
    Private rbSystem As RadioButton
    Private rbHighContrast As RadioButton

    Private grpBackground As GroupBox
    Private lblCurrentBg As Label
    Private btnSetBackground As Button
    Private btnClearBackground As Button
    Private picPreview As PictureBox

    Private grpLanguage As GroupBox
    Private cmbLanguage As ComboBox

    ' Advanced Tab
    Private grpLogging As GroupBox

    Private chkEnableLogging As CheckBox
    Private chkVerboseLogging As CheckBox
    Private btnOpenLogFolder As Button
    Private btnClearLogs As Button

    Private grpPerformance As GroupBox
    Private chkEnableCache As CheckBox
    Private chkParallelProcessing As CheckBox

    ' Bottom buttons
    Private btnOK As Button

    Private btnCancel As Button
    Private btnApply As Button

    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)

        InitializeComponent()
        LoadCurrentSettings()
    End Sub

    Private Sub InitializeComponent()

        Me.Text = "Advanced Settings"
        Me.MinimumSize = New Size(620, 520)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' ===== ROOT LAYOUT =====
        Dim root As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 2,
        .Padding = New Padding(10)
    }

        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Me.Controls.Add(root)

        ' ===== TAB CONTROL =====
        tabControl = New TabControl With {
        .Dock = DockStyle.Fill
    }

        tabGeneral = New TabPage("General")
        tabAppearance = New TabPage("Appearance")
        tabAdvanced = New TabPage("Advanced")

        tabControl.TabPages.AddRange({tabGeneral, tabAppearance, tabAdvanced})

        root.Controls.Add(tabControl, 0, 0)

        ' build tabs
        InitializeGeneralTab()
        InitializeAppearanceTab()
        InitializeAdvancedTab()

        ' ===== BUTTON BAR =====
        Dim buttonPanel As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.RightToLeft,
        .AutoSize = True
    }

        btnOK = New Button With {.Text = "OK", .Width = 90, .Height = 32}
        btnCancel = New Button With {.Text = "Cancel", .Width = 90, .Height = 32}
        btnApply = New Button With {.Text = "Apply", .Width = 90, .Height = 32}

        btnOK.DialogResult = DialogResult.OK
        btnCancel.DialogResult = DialogResult.Cancel

        AddHandler btnOK.Click, AddressOf BtnOK_Click
        AddHandler btnApply.Click, AddressOf BtnApply_Click

        buttonPanel.Controls.AddRange({btnOK, btnCancel, btnApply})
        root.Controls.Add(buttonPanel, 0, 1)

        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel

    End Sub

    Private Sub InitializeGeneralTab()

        Dim layout As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 1,
        .Padding = New Padding(8)
    }

        tabGeneral.Controls.Add(layout)

        grpBackup = New GroupBox With {
        .Text = "Backup Settings",
        .Dock = DockStyle.Fill
    }

        Dim flow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.TopDown,
        .AutoSize = True
    }

        chkAutoBackup = New CheckBox With {
        .Text = "Automatically create backup before patching",
        .AutoSize = True
    }

        chkBackupTimestamp = New CheckBox With {
        .Text = "Add timestamp to backup folder name",
        .AutoSize = True
    }

        Dim row As New FlowLayoutPanel With {.AutoSize = True}

        txtBackupLocation = New TextBox With {
        .Width = 320,
        .Text = "(Same directory as game)"
    }

        btnBrowseBackup = New Button With {.Text = "Browse...", .Width = 100}

        row.Controls.AddRange({
        New Label With {.Text = "Backup Location:", .AutoSize = True},
        txtBackupLocation,
        btnBrowseBackup
    })

        flow.Controls.AddRange({chkAutoBackup, chkBackupTimestamp, row})
        grpBackup.Controls.Add(flow)
        layout.Controls.Add(grpBackup)

    End Sub

    Private Sub InitializeAppearanceTab()

        Dim root As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.TopDown,
        .AutoScroll = True,
        .Padding = New Padding(8)
    }

        tabAppearance.Controls.Add(root)

        ' ===== THEME =====
        grpTheme = New GroupBox With {.Text = "Theme", .AutoSize = True, .Width = 520}

        Dim themeFlow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        rbLight = New RadioButton With {.Text = "☀️ Light Theme", .AutoSize = True}
        rbDark = New RadioButton With {.Text = "🌙 Dark Theme", .AutoSize = True}
        rbSystem = New RadioButton With {.Text = "💻 System Theme", .AutoSize = True}
        rbHighContrast = New RadioButton With {.Text = "🔲 High Contrast", .AutoSize = True}

        themeFlow.Controls.AddRange({rbLight, rbDark, rbSystem, rbHighContrast})
        grpTheme.Controls.Add(themeFlow)

        ' ===== BACKGROUND =====
        grpBackground = New GroupBox With {.Text = "Background Image", .AutoSize = True, .Width = 520}

        Dim bgFlow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        lblCurrentBg = New Label With {.Text = "Current: None", .AutoSize = True}

        btnSetBackground = New Button With {.Text = "🖼️ Set Background Image"}
        btnClearBackground = New Button With {.Text = "🗑️ Clear Background"}

        AddHandler btnSetBackground.Click, AddressOf BtnSetBackground_Click
        AddHandler btnClearBackground.Click, AddressOf BtnClearBackground_Click

        picPreview = New PictureBox With {
        .Size = New Size(120, 70),
        .SizeMode = PictureBoxSizeMode.Zoom,
        .BorderStyle = BorderStyle.FixedSingle
    }

        bgFlow.Controls.AddRange({lblCurrentBg, btnSetBackground, btnClearBackground, picPreview})
        grpBackground.Controls.Add(bgFlow)

        ' ===== LANGUAGE =====
        grpLanguage = New GroupBox With {.Text = "Language", .AutoSize = True, .Width = 520}

        Dim langFlow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        cmbLanguage = New ComboBox With {.Width = 180, .DropDownStyle = ComboBoxStyle.DropDownList}
        cmbLanguage.Items.AddRange({"English", "Italiano", "Deutsch"})

        langFlow.Controls.AddRange({
        New Label With {.Text = "Interface Language:", .AutoSize = True},
        cmbLanguage
    })

        grpLanguage.Controls.Add(langFlow)

        root.Controls.AddRange({grpTheme, grpBackground, grpLanguage})

    End Sub

    Private Sub InitializeAdvancedTab()

        Dim root As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.TopDown,
        .AutoScroll = True,
        .Padding = New Padding(8)
    }

        tabAdvanced.Controls.Add(root)

        ' ===== LOGGING =====
        grpLogging = New GroupBox With {.Text = "Logging", .AutoSize = True, .Width = 520}
        Dim logFlow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        chkEnableLogging = New CheckBox With {.Text = "Enable file logging", .AutoSize = True}
        chkVerboseLogging = New CheckBox With {.Text = "Verbose logging", .AutoSize = True}

        btnOpenLogFolder = New Button With {.Text = "📁 Open Log Folder"}
        btnClearLogs = New Button With {.Text = "🗑️ Clear Old Logs"}

        AddHandler btnOpenLogFolder.Click, AddressOf BtnOpenLogFolder_Click

        logFlow.Controls.AddRange({
        chkEnableLogging,
        chkVerboseLogging,
        btnOpenLogFolder,
        btnClearLogs
    })

        grpLogging.Controls.Add(logFlow)

        ' ===== PERFORMANCE =====
        grpPerformance = New GroupBox With {.Text = "Performance", .AutoSize = True, .Width = 520}
        Dim perfFlow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        chkEnableCache = New CheckBox With {.Text = "Enable caching", .AutoSize = True}
        chkParallelProcessing = New CheckBox With {.Text = "Enable parallel processing", .AutoSize = True}

        perfFlow.Controls.AddRange({chkEnableCache, chkParallelProcessing})
        grpPerformance.Controls.Add(perfFlow)

        root.Controls.AddRange({grpLogging, grpPerformance})

    End Sub

    Private Sub LoadCurrentSettings()
        ' Load theme
        Dim currentTheme = ThemeManager.GetCurrentTheme()
        Select Case currentTheme
            Case ThemeManager.AppTheme.Light
                rbLight.Checked = True
            Case ThemeManager.AppTheme.Dark
                rbDark.Checked = True
            Case ThemeManager.AppTheme.System
                rbSystem.Checked = True
            Case ThemeManager.AppTheme.HighContrast
                rbHighContrast.Checked = True
        End Select

        ' Load language
        Dim currentLang = LocalizationService.GetCurrentLanguage()
        Select Case currentLang
            Case LocalizationService.SupportedLanguage.English
                cmbLanguage.SelectedIndex = 0
            Case LocalizationService.SupportedLanguage.Italian
                cmbLanguage.SelectedIndex = 1
            Case LocalizationService.SupportedLanguage.German
                cmbLanguage.SelectedIndex = 2
        End Select

        ' Load background image
        ThemeManager.LoadBackgroundImagePreference()
        Dim bgPath = ThemeManager.GetBackgroundImagePath()
        If Not String.IsNullOrEmpty(bgPath) AndAlso IO.File.Exists(bgPath) Then
            lblCurrentBg.Text = $"Current: {IO.Path.GetFileName(bgPath)}"
            Try
                picPreview.Image = Image.FromFile(bgPath)
            Catch
                ' Preview failed
            End Try
        End If
    End Sub

    Private Sub BtnSetBackground_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Title = "Select Background Image"
            ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif"

            If ofd.ShowDialog() = DialogResult.OK Then
                ThemeManager.SetBackgroundImage(ofd.FileName)
                lblCurrentBg.Text = $"Current: {IO.Path.GetFileName(ofd.FileName)}"

                Try
                    If picPreview.Image IsNot Nothing Then
                        picPreview.Image.Dispose()
                    End If
                    picPreview.Image = Image.FromFile(ofd.FileName)
                Catch
                    ' Preview failed
                End Try

                MessageBox.Show("Background will be applied when you click OK or Apply.", "Background Image", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Private Sub BtnClearBackground_Click(sender As Object, e As EventArgs)
        ThemeManager.ClearBackgroundImage()
        lblCurrentBg.Text = "Current: None"
        If picPreview.Image IsNot Nothing Then
            picPreview.Image.Dispose()
            picPreview.Image = Nothing
        End If
        MessageBox.Show("Background will be cleared when you click OK or Apply.", "Background Image", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub BtnOpenLogFolder_Click(sender As Object, e As EventArgs)
        'Anti virus triggering - commented out for now
        'Try
        '    Dim logPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
        '    If Not IO.Directory.Exists(logPath) Then
        '        IO.Directory.CreateDirectory(logPath)
        '    End If
        '    Process.Start("explorer.exe", logPath)
        'Catch ex As Exception
        '    MessageBox.Show($"Error opening log folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'End Try
        Dim logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
        OpenFolder(logPath)
    End Sub

    Private Sub BtnApply_Click(sender As Object, e As EventArgs)
        ApplySettings()
    End Sub

    Private Sub BtnOK_Click(sender As Object, e As EventArgs)
        ApplySettings()
    End Sub

    Private Sub ApplySettings()
        Try
            ' Apply theme
            If rbLight.Checked Then
                ThemeManager.SetTheme(ThemeManager.AppTheme.Light)
            ElseIf rbDark.Checked Then
                ThemeManager.SetTheme(ThemeManager.AppTheme.Dark)
            ElseIf rbSystem.Checked Then
                ThemeManager.SetTheme(ThemeManager.AppTheme.System)
            ElseIf rbHighContrast.Checked Then
                ThemeManager.SetTheme(ThemeManager.AppTheme.HighContrast)
            End If

            ' Apply language
            Dim selectedLang As LocalizationService.SupportedLanguage
            Select Case cmbLanguage.SelectedIndex
                Case 0
                    selectedLang = LocalizationService.SupportedLanguage.English
                Case 1
                    selectedLang = LocalizationService.SupportedLanguage.Italian
                Case 2
                    selectedLang = LocalizationService.SupportedLanguage.German
                Case Else
                    selectedLang = LocalizationService.SupportedLanguage.English
            End Select
            LocalizationService.SetLanguage(selectedLang)

            MessageBox.Show("Settings applied successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show($"Error applying settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Class