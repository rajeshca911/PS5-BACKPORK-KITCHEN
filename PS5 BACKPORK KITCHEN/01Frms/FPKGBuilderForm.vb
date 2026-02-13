Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' FPKG configuration and building form.
''' Layout: ToolStrip (Browse Source, Browse Output, Build) + SplitContainer (config panel | preview/log) + StatusStrip.
''' Constructor overloads: New() for standalone, New(sourceFolder, config) for UFS2 conversion pre-fill.
''' </summary>
Public Class FPKGBuilderForm
    Inherits Form

    ' ---- UI Controls ----
    Private toolStrip As ToolStrip
    Private btnBrowseSource As ToolStripButton
    Private btnBrowseOutput As ToolStripButton
    Private btnBuild As ToolStripButton

    Private splitContainer As SplitContainer

    ' Left panel: configuration
    Private configPanel As Panel
    Private grpConfig As GroupBox
    Private txtContentId As TextBox
    Private txtTitle As TextBox
    Private txtTitleId As TextBox
    Private cmbContentType As ComboBox
    Private txtAppVersion As TextBox
    Private txtVersion As TextBox
    Private txtCategory As TextBox
    Private btnSelectIcon As Button
    Private picIcon As PictureBox
    Private btnSelectBackground As Button
    Private picBackground As PictureBox
    Private txtSourceFolder As TextBox
    Private txtOutputPath As TextBox

    ' Right panel: preview + log
    Private rightPanel As TableLayoutPanel
    Private dgvPreview As DataGridView
    Private txtBuildLog As RichTextBox

    Private statusStrip As StatusStrip
    Private lblStatus As ToolStripStatusLabel
    Private progressBar As ToolStripProgressBar

    ' ---- State ----
    Private _sourceFolder As String = ""
    Private _outputPath As String = ""
    Private _iconPath As String = ""
    Private _backgroundPath As String = ""

    ''' <summary>
    ''' True after a successful build.
    ''' </summary>
    Public Property BuildSucceeded As Boolean

    ''' <summary>
    ''' Path to the built FPKG file.
    ''' </summary>
    Public Property OutputFilePath As String = ""

    ''' <summary>
    ''' Standalone constructor.
    ''' </summary>
    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)
        InitializeFormLayout()
    End Sub

    ''' <summary>
    ''' Pre-filled constructor for UFS2 conversion pipeline.
    ''' </summary>
    Public Sub New(sourceFolder As String, config As FPKGConfig)
        Me.New()

        _sourceFolder = sourceFolder
        txtSourceFolder.Text = sourceFolder

        If config IsNot Nothing Then
            txtContentId.Text = config.ContentId
            txtTitle.Text = config.Title
            txtTitleId.Text = config.TitleId
            txtAppVersion.Text = config.AppVersion
            txtVersion.Text = config.Version
            txtCategory.Text = config.Category

            Select Case config.ContentType
                Case PKGConstants.CONTENT_TYPE_GD : cmbContentType.SelectedIndex = 0
                Case PKGConstants.CONTENT_TYPE_AC : cmbContentType.SelectedIndex = 1
                Case PKGConstants.CONTENT_TYPE_DP : cmbContentType.SelectedIndex = 2
                Case Else : cmbContentType.SelectedIndex = 0
            End Select

            If Not String.IsNullOrEmpty(config.IconPath) AndAlso File.Exists(config.IconPath) Then
                _iconPath = config.IconPath
                LoadIconPreview(config.IconPath)
            End If
            If Not String.IsNullOrEmpty(config.BackgroundPath) AndAlso File.Exists(config.BackgroundPath) Then
                _backgroundPath = config.BackgroundPath
                LoadBackgroundPreview(config.BackgroundPath)
            End If
        End If

        RefreshFilePreview()
    End Sub

    Private Sub InitializeFormLayout()
        Me.Text = "FPKG Builder"
        Me.MinimumSize = New Size(1000, 700)
        Me.Size = New Size(1150, 780)
        Me.StartPosition = FormStartPosition.CenterParent

        ' Root layout
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3
        }
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Me.Controls.Add(root)

        ' ---- ToolStrip ----
        toolStrip = New ToolStrip With {.GripStyle = ToolStripGripStyle.Hidden, .Dock = DockStyle.Fill}

        btnBrowseSource = New ToolStripButton("Browse Source") With {.DisplayStyle = ToolStripItemDisplayStyle.Text}
        AddHandler btnBrowseSource.Click, AddressOf BtnBrowseSource_Click
        toolStrip.Items.Add(btnBrowseSource)

        btnBrowseOutput = New ToolStripButton("Browse Output") With {.DisplayStyle = ToolStripItemDisplayStyle.Text}
        AddHandler btnBrowseOutput.Click, AddressOf BtnBrowseOutput_Click
        toolStrip.Items.Add(btnBrowseOutput)

        toolStrip.Items.Add(New ToolStripSeparator())

        btnBuild = New ToolStripButton("Build FPKG") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .Font = New Font(toolStrip.Font, FontStyle.Bold)
        }
        AddHandler btnBuild.Click, AddressOf BtnBuild_Click
        toolStrip.Items.Add(btnBuild)

        root.Controls.Add(toolStrip, 0, 0)

        ' ---- SplitContainer ----
        splitContainer = New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterDistance = 380
        }
        root.Controls.Add(splitContainer, 0, 1)

        ' ---- Left Panel: Configuration ----
        BuildConfigPanel()

        ' ---- Right Panel: Preview + Log ----
        rightPanel = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2
        }
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 55))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 45))
        splitContainer.Panel2.Controls.Add(rightPanel)

        ' DataGridView for file preview
        dgvPreview = New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        dgvPreview.Columns.Add("FileName", "File Name")
        dgvPreview.Columns.Add("Size", "Size")
        dgvPreview.Columns.Add("Type", "Type")
        dgvPreview.Columns("Size").FillWeight = 20
        dgvPreview.Columns("Type").FillWeight = 15
        rightPanel.Controls.Add(dgvPreview, 0, 0)

        ' Build log
        txtBuildLog = New RichTextBox With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .Font = New Font("Consolas", 9),
            .BackColor = Color.FromArgb(250, 250, 250)
        }
        rightPanel.Controls.Add(txtBuildLog, 0, 1)

        ' ---- StatusStrip ----
        statusStrip = New StatusStrip()
        lblStatus = New ToolStripStatusLabel("Ready") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        progressBar = New ToolStripProgressBar With {.Visible = False, .Width = 200}
        statusStrip.Items.AddRange({lblStatus, progressBar})
        root.Controls.Add(statusStrip, 0, 2)
    End Sub

    Private Sub BuildConfigPanel()
        configPanel = New Panel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True
        }
        splitContainer.Panel1.Controls.Add(configPanel)

        grpConfig = New GroupBox With {
            .Text = "FPKG Configuration",
            .Dock = DockStyle.Fill,
            .Padding = New Padding(10)
        }
        configPanel.Controls.Add(grpConfig)

        Dim tbl As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .AutoSize = True
        }
        tbl.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 110))
        tbl.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

        Dim row = 0

        ' Source Folder
        AddLabel(tbl, "Source Folder:", row)
        txtSourceFolder = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
        tbl.Controls.Add(txtSourceFolder, 1, row)
        row += 1

        ' Output Path
        AddLabel(tbl, "Output Path:", row)
        txtOutputPath = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
        tbl.Controls.Add(txtOutputPath, 1, row)
        row += 1

        ' Content ID
        AddLabel(tbl, "Content ID:", row)
        txtContentId = New TextBox With {.Dock = DockStyle.Fill, .MaxLength = 36}
        tbl.Controls.Add(txtContentId, 1, row)
        row += 1

        ' Title
        AddLabel(tbl, "Title:", row)
        txtTitle = New TextBox With {.Dock = DockStyle.Fill}
        tbl.Controls.Add(txtTitle, 1, row)
        row += 1

        ' Title ID
        AddLabel(tbl, "Title ID:", row)
        txtTitleId = New TextBox With {.Dock = DockStyle.Fill, .MaxLength = 9}
        tbl.Controls.Add(txtTitleId, 1, row)
        row += 1

        ' Content Type
        AddLabel(tbl, "Content Type:", row)
        cmbContentType = New ComboBox With {
            .Dock = DockStyle.Fill,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbContentType.Items.AddRange({"Game Data", "DLC (Additional Content)", "Delta Patch"})
        cmbContentType.SelectedIndex = 0
        tbl.Controls.Add(cmbContentType, 1, row)
        row += 1

        ' App Version
        AddLabel(tbl, "App Version:", row)
        txtAppVersion = New TextBox With {.Dock = DockStyle.Fill, .Text = "01.00"}
        tbl.Controls.Add(txtAppVersion, 1, row)
        row += 1

        ' Version
        AddLabel(tbl, "Version:", row)
        txtVersion = New TextBox With {.Dock = DockStyle.Fill, .Text = "01.00"}
        tbl.Controls.Add(txtVersion, 1, row)
        row += 1

        ' Category
        AddLabel(tbl, "Category:", row)
        txtCategory = New TextBox With {.Dock = DockStyle.Fill, .Text = "gd"}
        tbl.Controls.Add(txtCategory, 1, row)
        row += 1

        ' Icon
        AddLabel(tbl, "Icon:", row)
        Dim iconPanel As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}
        btnSelectIcon = New Button With {.Text = "Select Icon...", .AutoSize = True}
        AddHandler btnSelectIcon.Click, AddressOf BtnSelectIcon_Click
        picIcon = New PictureBox With {
            .Size = New Size(64, 64),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BorderStyle = BorderStyle.FixedSingle
        }
        iconPanel.Controls.AddRange({btnSelectIcon, picIcon})
        tbl.Controls.Add(iconPanel, 1, row)
        row += 1

        ' Background
        AddLabel(tbl, "Background:", row)
        Dim bgPanel As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}
        btnSelectBackground = New Button With {.Text = "Select Background...", .AutoSize = True}
        AddHandler btnSelectBackground.Click, AddressOf BtnSelectBackground_Click
        picBackground = New PictureBox With {
            .Size = New Size(100, 56),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BorderStyle = BorderStyle.FixedSingle
        }
        bgPanel.Controls.AddRange({btnSelectBackground, picBackground})
        tbl.Controls.Add(bgPanel, 1, row)

        grpConfig.Controls.Add(tbl)
    End Sub

    Private Sub AddLabel(panel As TableLayoutPanel, text As String, row As Integer)
        Dim lbl As New Label With {
            .Text = text,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        }
        panel.Controls.Add(lbl, 0, row)
    End Sub

    ' ===== EVENT HANDLERS =====

    Private Sub BtnBrowseSource_Click(sender As Object, e As EventArgs)
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select source folder containing files to package"
            If fbd.ShowDialog() = DialogResult.OK Then
                _sourceFolder = fbd.SelectedPath
                txtSourceFolder.Text = _sourceFolder
                RefreshFilePreview()
            End If
        End Using
    End Sub

    Private Sub BtnBrowseOutput_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog()
            sfd.Title = "Save FPKG As"
            sfd.Filter = "PKG Files|*.pkg|All Files|*.*"
            sfd.FileName = If(Not String.IsNullOrEmpty(txtTitleId.Text),
                              $"{txtTitleId.Text}.pkg", "output.pkg")
            If sfd.ShowDialog() = DialogResult.OK Then
                _outputPath = sfd.FileName
                txtOutputPath.Text = _outputPath
            End If
        End Using
    End Sub

    Private Sub BtnSelectIcon_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Title = "Select Icon (icon0.png)"
            ofd.Filter = "PNG Images|*.png|All Files|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                _iconPath = ofd.FileName
                LoadIconPreview(ofd.FileName)
            End If
        End Using
    End Sub

    Private Sub BtnSelectBackground_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Title = "Select Background (pic1.png)"
            ofd.Filter = "PNG Images|*.png|All Files|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                _backgroundPath = ofd.FileName
                LoadBackgroundPreview(ofd.FileName)
            End If
        End Using
    End Sub

    Private Sub BtnBuild_Click(sender As Object, e As EventArgs)
        ' Validate
        If String.IsNullOrEmpty(_sourceFolder) OrElse Not Directory.Exists(_sourceFolder) Then
            MessageBox.Show("Please select a valid source folder.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrEmpty(_outputPath) Then
            ' Auto-generate output path
            BtnBrowseOutput_Click(Nothing, Nothing)
            If String.IsNullOrEmpty(_outputPath) Then Return
        End If

        ' Build config
        Dim config As New FPKGConfig With {
            .ContentId = txtContentId.Text.Trim(),
            .Title = txtTitle.Text.Trim(),
            .TitleId = txtTitleId.Text.Trim(),
            .ContentType = GetSelectedContentType(),
            .AppVersion = txtAppVersion.Text.Trim(),
            .Version = txtVersion.Text.Trim(),
            .Category = txtCategory.Text.Trim(),
            .IconPath = _iconPath,
            .BackgroundPath = _backgroundPath
        }

        ' Validate config
        Dim validationError = FPKGBuilderService.ValidateConfig(config)
        If Not String.IsNullOrEmpty(validationError) Then
            MessageBox.Show(validationError, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Build
        txtBuildLog.Clear()
        LogMessage("Starting FPKG build...", Color.Blue)
        LogMessage($"Source: {_sourceFolder}")
        LogMessage($"Output: {_outputPath}")
        LogMessage($"Content ID: {config.ContentId}")
        LogMessage($"Title: {config.Title}")
        LogMessage("")

        lblStatus.Text = "Building..."
        progressBar.Visible = True
        progressBar.Value = 0
        progressBar.Maximum = 100
        btnBuild.Enabled = False
        Application.DoEvents()

        Dim progressReporter = New Progress(Of BuildProgress)(
            Sub(bp)
                progressBar.Value = Math.Min(bp.PercentComplete, 100)
                lblStatus.Text = bp.Stage
                If Not String.IsNullOrEmpty(bp.CurrentFile) Then
                    LogMessage($"  {bp.CurrentFile}")
                End If
                Application.DoEvents()
            End Sub)

        Dim result = FPKGBuilderService.BuildFromFolder(_sourceFolder, _outputPath, config, progressReporter)

        progressBar.Visible = False
        btnBuild.Enabled = True

        If result.Success Then
            BuildSucceeded = True
            OutputFilePath = result.OutputPath

            LogMessage("")
            LogMessage("BUILD SUCCESSFUL", Color.Green)
            LogMessage($"Output: {result.OutputPath}")
            LogMessage($"Files: {result.FileCount}")
            LogMessage($"Size: {FormatSize(result.TotalSize)}")

            lblStatus.Text = "Build complete"
            MessageBox.Show($"FPKG built successfully!" & vbCrLf &
                            $"Output: {result.OutputPath}" & vbCrLf &
                            $"Size: {FormatSize(result.TotalSize)}",
                            "Build Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            LogMessage("")
            LogMessage($"BUILD FAILED: {result.ErrorMessage}", Color.Red)
            lblStatus.Text = "Build failed"
            MessageBox.Show(result.ErrorMessage, "Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    ' ===== HELPERS =====

    Private Sub RefreshFilePreview()
        dgvPreview.Rows.Clear()
        If String.IsNullOrEmpty(_sourceFolder) OrElse Not Directory.Exists(_sourceFolder) Then Return

        Try
            Dim files = Directory.GetFiles(_sourceFolder, "*", SearchOption.AllDirectories)
            For Each filePath In files.OrderBy(Function(f) f)
                Dim fInfo As New FileInfo(filePath)
                Dim relativePath = filePath.Substring(_sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar)
                dgvPreview.Rows.Add(
                    relativePath,
                    FormatSize(fInfo.Length),
                    fInfo.Extension.ToLowerInvariant()
                )
            Next

            lblStatus.Text = $"Source: {files.Length} file(s)"
        Catch ex As Exception
            lblStatus.Text = $"Error scanning source: {ex.Message}"
        End Try
    End Sub

    Private Sub LoadIconPreview(path As String)
        Try
            If picIcon.Image IsNot Nothing Then
                Dim old = picIcon.Image
                picIcon.Image = Nothing
                old.Dispose()
            End If
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                picIcon.Image = Drawing.Image.FromStream(fs)
            End Using
        Catch
        End Try
    End Sub

    Private Sub LoadBackgroundPreview(path As String)
        Try
            If picBackground.Image IsNot Nothing Then
                Dim old = picBackground.Image
                picBackground.Image = Nothing
                old.Dispose()
            End If
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                picBackground.Image = Drawing.Image.FromStream(fs)
            End Using
        Catch
        End Try
    End Sub

    Private Function GetSelectedContentType() As UInteger
        Select Case cmbContentType.SelectedIndex
            Case 0 : Return PKGConstants.CONTENT_TYPE_GD
            Case 1 : Return PKGConstants.CONTENT_TYPE_AC
            Case 2 : Return PKGConstants.CONTENT_TYPE_DP
            Case Else : Return PKGConstants.CONTENT_TYPE_GD
        End Select
    End Function

    Private Sub LogMessage(text As String, Optional color As Color = Nothing)
        txtBuildLog.SelectionStart = txtBuildLog.TextLength
        txtBuildLog.SelectionLength = 0
        txtBuildLog.SelectionColor = If(color = Nothing, txtBuildLog.ForeColor, color)
        txtBuildLog.AppendText(text & vbCrLf)
        txtBuildLog.SelectionColor = txtBuildLog.ForeColor
        txtBuildLog.ScrollToCaret()
    End Sub

    Private Shared Function FormatSize(bytes As Long) As String
        If bytes < 1024 Then Return $"{bytes} B"
        If bytes < 1024 * 1024 Then Return $"{bytes / 1024.0:F1} KB"
        If bytes < 1024L * 1024 * 1024 Then Return $"{bytes / (1024.0 * 1024):F1} MB"
        Return $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    End Function

End Class
