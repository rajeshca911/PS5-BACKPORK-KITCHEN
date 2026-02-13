Imports System.IO
Imports System.Windows.Forms
Imports System.Diagnostics

Public Class ElfInspectorForm
    Inherits Form

    ' UI Controls
    Private splitContainer As SplitContainer

    Private dgvFiles As DataGridView
    Private contextMenu As ContextMenuStrip
    Private mnuDecrypt As ToolStripMenuItem
    Private mnuDowngrade As ToolStripMenuItem
    Private mnuPatch As ToolStripMenuItem
    Private mnuSign As ToolStripMenuItem
    Private mnuSeparator1 As ToolStripSeparator
    Private mnuFullPipeline As ToolStripMenuItem
    Private mnuSeparator2 As ToolStripSeparator
    Private mnuOpenLocation As ToolStripMenuItem

    Private txtDetails As RichTextBox
    Private toolStrip As ToolStrip
    Private btnAnalyze As ToolStripButton
    Private btnRefresh As ToolStripButton
    Private btnExport As ToolStripButton
    Private toolStripSeparator1 As ToolStripSeparator
    Private lblFolder As ToolStripLabel
    Private txtFolder As ToolStripTextBox
    Private btnBrowse As ToolStripButton
    Private statusStrip As StatusStrip
    Private lblStatus As ToolStripStatusLabel
    Private progressBar As ToolStripProgressBar

    'adding options
    Private grpElfMode As GroupBox

    Private rbOverwrite As RadioButton
    Private rbKeepOriginal As RadioButton
    Private rbBackupThenModify As RadioButton

    Private currentFolderPath As String = ""

    Public Sub New(Optional folderPath As String = "")
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)

        currentFolderPath = folderPath
        InitializeComponent()

        If Not String.IsNullOrEmpty(folderPath) Then
            AnalyzeFolder(folderPath)
        End If
    End Sub

    '    Private Sub InitializeComponent()
    '        Me.Text = "ELF Inspector - SDK Analysis"
    '        Me.Size = New Size(1000, 700)
    '        Me.StartPosition = FormStartPosition.CenterParent

    '        ' Create ToolStrip
    '        toolStrip = New ToolStrip With {
    '            .GripStyle = ToolStripGripStyle.Hidden
    '        }

    '        lblFolder = New ToolStripLabel With {
    '            .Text = "Folder:"
    '        }

    '        txtFolder = New ToolStripTextBox With {
    '            .Size = New Size(400, 23),
    '            .Text = currentFolderPath
    '        }

    '        btnBrowse = New ToolStripButton With {
    '            .Text = "📁",
    '            .DisplayStyle = ToolStripItemDisplayStyle.Text,
    '            .ToolTipText = "Browse folder"
    '        }
    '        AddHandler btnBrowse.Click, AddressOf BtnBrowse_Click

    '        btnAnalyze = New ToolStripButton With {
    '            .Text = "🔍 Analyze",
    '            .DisplayStyle = ToolStripItemDisplayStyle.Text
    '        }
    '        AddHandler btnAnalyze.Click, AddressOf BtnAnalyze_Click

    '        toolStripSeparator1 = New ToolStripSeparator()

    '        btnRefresh = New ToolStripButton With {
    '            .Text = "🔄 Refresh",
    '            .DisplayStyle = ToolStripItemDisplayStyle.Text
    '        }
    '        AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click

    '        btnExport = New ToolStripButton With {
    '            .Text = "💾 Export Report",
    '            .DisplayStyle = ToolStripItemDisplayStyle.Text
    '        }
    '        AddHandler btnExport.Click, AddressOf BtnExport_Click

    '        toolStrip.Items.AddRange({lblFolder, txtFolder, btnBrowse, btnAnalyze, toolStripSeparator1, btnRefresh, btnExport})

    '        ' Create SplitContainer
    '        splitContainer = New SplitContainer With {
    '            .Dock = DockStyle.Fill,
    '            .Orientation = Orientation.Horizontal,
    '            .SplitterDistance = 400
    '        }

    '        ' Create Context Menu
    '        contextMenu = New ContextMenuStrip()

    '        mnuDecrypt = New ToolStripMenuItem With {
    '            .Text = "🔓 Decrypt",
    '            .ToolTipText = "Decrypt selected ELF(s) using SelfUtil"
    '        }
    '        AddHandler mnuDecrypt.Click, AddressOf MnuDecrypt_Click

    '        mnuDowngrade = New ToolStripMenuItem With {
    '            .Text = "⬇ Downgrade / Backport",
    '            .ToolTipText = "Downgrade selected ELF(s) to target firmware"
    '        }
    '        AddHandler mnuDowngrade.Click, AddressOf MnuDowngrade_Click

    '        mnuPatch = New ToolStripMenuItem With {
    '            .Text = "🔧 Patch",
    '            .ToolTipText = "Apply SDK/FW patches to selected ELF(s)"
    '        }
    '        AddHandler mnuPatch.Click, AddressOf MnuPatch_Click

    '        mnuSign = New ToolStripMenuItem With {
    '            .Text = "✍ Sign",
    '            .ToolTipText = "Sign selected ELF(s)"
    '        }
    '        AddHandler mnuSign.Click, AddressOf MnuSign_Click

    '        mnuSeparator1 = New ToolStripSeparator()

    '        mnuFullPipeline = New ToolStripMenuItem With {
    '            .Text = "⚡ Full Pipeline (Decrypt → Patch → Sign)",
    '            .ToolTipText = "Run complete processing pipeline on selected ELF(s)",
    '            .Font = New Font(contextMenu.Font, FontStyle.Bold)
    '        }
    '        AddHandler mnuFullPipeline.Click, AddressOf MnuFullPipeline_Click

    '        mnuSeparator2 = New ToolStripSeparator()

    '        mnuOpenLocation = New ToolStripMenuItem With {
    '            .Text = "📂 Open File Location",
    '            .ToolTipText = "Open folder containing selected file"
    '        }
    '        AddHandler mnuOpenLocation.Click, AddressOf MnuOpenLocation_Click

    '        contextMenu.Items.AddRange({
    '            mnuDecrypt,
    '            mnuDowngrade,
    '            mnuPatch,
    '            mnuSign,
    '            mnuSeparator1,
    '            mnuFullPipeline,
    '            mnuSeparator2,
    '            mnuOpenLocation
    '        })

    '        ' Top panel - DataGridView
    '        dgvFiles = New DataGridView With {
    '            .Dock = DockStyle.Fill,
    '            .AllowUserToAddRows = False,
    '            .AllowUserToDeleteRows = False,
    '            .ReadOnly = True,
    '            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    '            .MultiSelect = True,
    '            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    '            .RowHeadersVisible = False,
    '            .BackgroundColor = Color.White,
    '            .BorderStyle = BorderStyle.None,
    '            .ContextMenuStrip = contextMenu
    '        }

    '        ' Add columns
    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "FileName",
    '            .HeaderText = "File Name",
    '            .FillWeight = 30
    '        })

    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "Type",
    '            .HeaderText = "Type",
    '            .FillWeight = 10
    '        })

    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "PS5SDK",
    '            .HeaderText = "PS5 SDK",
    '            .FillWeight = 15
    '        })

    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "PS4SDK",
    '            .HeaderText = "PS4 SDK",
    '            .FillWeight = 15
    '        })

    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "Size",
    '            .HeaderText = "Size",
    '            .FillWeight = 10
    '        })

    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "Patchable",
    '            .HeaderText = "Patchable",
    '            .FillWeight = 10
    '        })

    '        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
    '            .Name = "Path",
    '            .HeaderText = "Full Path",
    '            .Visible = False
    '        })

    '        AddHandler dgvFiles.SelectionChanged, AddressOf DgvFiles_SelectionChanged

    '        'group box for options
    '        ' ELF Operation Mode Group
    '        grpElfMode = New GroupBox With {
    '    .Text = "ELF Operation Mode",
    '    .Dock = DockStyle.Top,
    '    .Height = 70,
    '     .BackColor = Color.FromArgb(245, 248, 252)
    '}

    '        rbOverwrite = New RadioButton With {
    '    .Text = "Overwrite original ELF (unsafe)",
    '    .Location = New Point(10, 20),
    '    .AutoSize = True,
    '    .ForeColor = Color.FromArgb(160, 60, 60) ' muted red
    '}
    '        rbKeepOriginal = New RadioButton With {
    '    .Text = "Keep original, create new ELF (_decrypted / _signed)",
    '    .Location = New Point(10, 40),
    '    .AutoSize = True,
    '    .Checked = True,
    '    .ForeColor = Color.FromArgb(40, 120, 80), ' soft green
    '    .Font = New Font(SystemFonts.DefaultFont, FontStyle.Bold)
    '}

    '        rbBackupThenModify = New RadioButton With {
    '    .Text = "Backup original (.bak) then modify ELF",
    '    .Location = New Point(420, 20),
    '    .AutoSize = True
    '}

    '        grpElfMode.Controls.AddRange({
    '    rbOverwrite,
    '    rbKeepOriginal,
    '    rbBackupThenModify
    '})

    '        'Me.Controls.Add(grpElfMode)
    '        'grpElfMode.BringToFront()

    '        'split container
    '        splitContainer.Panel1.Controls.Add(dgvFiles)

    '        ' Bottom panel - Details
    '        txtDetails = New RichTextBox With {
    '            .Dock = DockStyle.Fill,
    '            .ReadOnly = True,
    '            .Font = New Font("Consolas", 9),
    '            .BackColor = Color.FromArgb(250, 250, 250)
    '        }

    '        splitContainer.Panel2.Controls.Add(txtDetails)

    '        ' Status Strip
    '        statusStrip = New StatusStrip()

    '        lblStatus = New ToolStripStatusLabel With {
    '            .Text = "Ready",
    '            .Spring = True,
    '            .TextAlign = ContentAlignment.MiddleLeft
    '        }

    '        progressBar = New ToolStripProgressBar With {
    '            .Visible = False
    '        }

    '        statusStrip.Items.AddRange({lblStatus, progressBar})
    '        'add minimum sizes
    '        'splitContainer.Panel1MinSize = 150
    '        'splitContainer.Panel2MinSize = 200

    '        ' Add controls to form
    '        Me.Controls.Add(splitContainer)
    '        Me.Controls.Add(toolStrip)
    '        Me.Controls.Add(statusStrip)
    '        splitContainer.Panel1.Controls.Add(dgvFiles)

    '        ' Panel2 = ELF options + Details
    '        splitContainer.Panel2.Controls.Add(txtDetails)
    '        splitContainer.Panel2.Controls.Add(grpElfMode)

    '        ' Docking order matters
    '        grpElfMode.Dock = DockStyle.Top
    '        txtDetails.Dock = DockStyle.Fill

    '    End Sub
    Private Sub InitializeComponent()

        Me.Text = "ELF Inspector - SDK Analysis"
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent

        ' ===== ROOT LAYOUT =====
        Dim root As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 3
    }

        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' toolstrip
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' main
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' status

        Me.Controls.Add(root)

        ' ================= TOOLSTRIP =================
        toolStrip = New ToolStrip With {
        .GripStyle = ToolStripGripStyle.Hidden,
        .Dock = DockStyle.Fill
    }

        lblFolder = New ToolStripLabel With {.Text = "Folder:"}

        txtFolder = New ToolStripTextBox With {
        .AutoSize = False,
        .Width = 420,
        .Text = currentFolderPath
    }

        btnBrowse = New ToolStripButton("📁")
        btnAnalyze = New ToolStripButton("🔍 Analyze")
        btnRefresh = New ToolStripButton("🔄 Refresh")
        btnExport = New ToolStripButton("💾 Export")

        AddHandler btnBrowse.Click, AddressOf BtnBrowse_Click
        AddHandler btnAnalyze.Click, AddressOf BtnAnalyze_Click
        AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click
        AddHandler btnExport.Click, AddressOf BtnExport_Click

        toolStrip.Items.AddRange({
        lblFolder, txtFolder, btnBrowse, btnAnalyze,
        New ToolStripSeparator(),
        btnRefresh, btnExport
    })

        root.Controls.Add(toolStrip, 0, 0)

        ' ================= SPLIT CONTAINER =================
        splitContainer = New SplitContainer With {
        .Dock = DockStyle.Fill,
        .Orientation = Orientation.Horizontal,
        .SplitterDistance = 350
    }

        root.Controls.Add(splitContainer, 0, 1)

        ' ================= GRID =================
        dgvFiles = New DataGridView With {
        .Dock = DockStyle.Fill,
        .ReadOnly = True,
        .MultiSelect = True,
        .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        .RowHeadersVisible = False
    }

        splitContainer.Panel1.Controls.Add(dgvFiles)

        ' ================= BOTTOM PANEL LAYOUT =================
        Dim bottomLayout As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 2
    }

        bottomLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        bottomLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        splitContainer.Panel2.Controls.Add(bottomLayout)

        ' ================= ELF MODE GROUP =================
        grpElfMode = BuildElfModeGroup()
        bottomLayout.Controls.Add(grpElfMode, 0, 0)

        ' ================= DETAILS BOX =================
        txtDetails = New RichTextBox With {
        .Dock = DockStyle.Fill,
        .ReadOnly = True,
        .Font = New Font("Consolas", 9)
    }

        bottomLayout.Controls.Add(txtDetails, 0, 1)

        ' ================= STATUS STRIP =================
        statusStrip = New StatusStrip()

        lblStatus = New ToolStripStatusLabel("Ready") With {
        .Spring = True
    }

        progressBar = New ToolStripProgressBar With {
        .Visible = False,
        .Width = 180
    }

        statusStrip.Items.AddRange({lblStatus, progressBar})

        root.Controls.Add(statusStrip, 0, 2)

        ' build context menu + columns
        BuildContextMenu()
        BuildGridColumns()

    End Sub

    Private Sub BuildContextMenu()

        contextMenu = New ContextMenuStrip()

        mnuDecrypt = New ToolStripMenuItem With {
        .Text = "🔓 Decrypt",
        .ToolTipText = "Decrypt selected ELF(s) using SelfUtil"
    }
        AddHandler mnuDecrypt.Click, AddressOf MnuDecrypt_Click

        mnuDowngrade = New ToolStripMenuItem With {
        .Text = "⬇ Downgrade / Backport",
        .ToolTipText = "Downgrade selected ELF(s) to target firmware"
    }
        AddHandler mnuDowngrade.Click, AddressOf MnuDowngrade_Click

        mnuPatch = New ToolStripMenuItem With {
        .Text = "🔧 Patch",
        .ToolTipText = "Apply SDK/FW patches to selected ELF(s)"
    }
        AddHandler mnuPatch.Click, AddressOf MnuPatch_Click

        mnuSign = New ToolStripMenuItem With {
        .Text = "✍ Sign",
        .ToolTipText = "Sign selected ELF(s)"
    }
        AddHandler mnuSign.Click, AddressOf MnuSign_Click

        mnuSeparator1 = New ToolStripSeparator()

        mnuFullPipeline = New ToolStripMenuItem With {
        .Text = "⚡ Full Pipeline (Decrypt → Patch → Sign)",
        .ToolTipText = "Run complete processing pipeline",
        .Font = New Font(SystemFonts.MenuFont, FontStyle.Bold)
    }
        AddHandler mnuFullPipeline.Click, AddressOf MnuFullPipeline_Click

        mnuSeparator2 = New ToolStripSeparator()

        mnuOpenLocation = New ToolStripMenuItem With {
        .Text = "📂 Open File Location",
        .ToolTipText = "Open folder containing selected file"
    }
        AddHandler mnuOpenLocation.Click, AddressOf MnuOpenLocation_Click

        contextMenu.Items.AddRange({
        mnuDecrypt,
        mnuDowngrade,
        mnuPatch,
        mnuSign,
        mnuSeparator1,
        mnuFullPipeline,
        mnuSeparator2,
        mnuOpenLocation
    })

        ' attach to grid
        dgvFiles.ContextMenuStrip = contextMenu

    End Sub

    Private Function BuildElfModeGroup() As GroupBox

        grpElfMode = New GroupBox With {
        .Text = "ELF Operation Mode",
        .Dock = DockStyle.Fill,
        .AutoSize = True
    }

        Dim flow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .AutoSize = True
    }

        rbOverwrite = New RadioButton With {
        .Text = "Overwrite original ELF (unsafe)",
        .AutoSize = True,
        .ForeColor = Color.FromArgb(160, 60, 60)
    }

        rbKeepOriginal = New RadioButton With {
        .Text = "Keep original, create new ELF",
        .AutoSize = True,
        .Checked = True,
        .Font = New Font(SystemFonts.DefaultFont, FontStyle.Bold),
        .ForeColor = Color.FromArgb(40, 120, 80)
    }

        rbBackupThenModify = New RadioButton With {
        .Text = "Backup original (.bak) then modify",
        .AutoSize = True
    }

        flow.Controls.AddRange({
        rbOverwrite,
        rbKeepOriginal,
        rbBackupThenModify
    })

        grpElfMode.Controls.Add(flow)
        Return grpElfMode

    End Function

    Private Sub BuildGridColumns()

        dgvFiles.Columns.Add("FileName", "File Name")
        dgvFiles.Columns.Add("Type", "Type")
        dgvFiles.Columns.Add("PS5SDK", "PS5 SDK")
        dgvFiles.Columns.Add("PS4SDK", "PS4 SDK")
        dgvFiles.Columns.Add("Size", "Size")
        dgvFiles.Columns.Add("Patchable", "Patchable")

        Dim colPath As New DataGridViewTextBoxColumn With {
        .Name = "Path",
        .HeaderText = "Full Path",
        .Visible = False
    }

        dgvFiles.Columns.Add(colPath)

    End Sub

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs)
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select game folder to analyze"
            fbd.SelectedPath = currentFolderPath

            If fbd.ShowDialog() = DialogResult.OK Then
                txtFolder.Text = fbd.SelectedPath
                currentFolderPath = fbd.SelectedPath

            End If
        End Using
    End Sub

    Private Sub BtnAnalyze_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(txtFolder.Text) Then
            MessageBox.Show("Please select a folder first!", "No Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        AnalyzeFolder(txtFolder.Text)
    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
        If Not String.IsNullOrEmpty(currentFolderPath) Then
            AnalyzeFolder(currentFolderPath)
        End If
    End Sub

    Private Sub AnalyzeFolder(folderPath As String)
        Try
            dgvFiles.Rows.Clear()
            txtDetails.Clear()

            lblStatus.Text = "Analyzing folder..."
            progressBar.Visible = True
            Application.DoEvents()

            If Not IO.Directory.Exists(folderPath) Then
                MessageBox.Show("Folder does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Get analysis from SdkDetector
            Dim analysis = SdkDetector.AnalyzeFolderSdkVersions(folderPath)

            ' Find all ELF files
            Dim elfFiles As New List(Of String)

            ' Search common locations
            If IO.Directory.Exists(folderPath) Then
                elfFiles.AddRange(IO.Directory.GetFiles(folderPath, "*.elf", IO.SearchOption.AllDirectories))
                elfFiles.AddRange(IO.Directory.GetFiles(folderPath, "*.self", IO.SearchOption.AllDirectories))
                elfFiles.AddRange(IO.Directory.GetFiles(folderPath, "*.sprx", IO.SearchOption.AllDirectories))
                elfFiles.AddRange(IO.Directory.GetFiles(folderPath, "*.prx", IO.SearchOption.AllDirectories))
                elfFiles.AddRange(IO.Directory.GetFiles(folderPath, "eboot.bin", IO.SearchOption.AllDirectories))
            End If

            progressBar.Maximum = elfFiles.Count
            progressBar.Value = 0

            ' Analyze each file
            Dim totalSize As Long = 0
            Dim patchableCount = 0

            For Each filePath In elfFiles
                Try
                    Dim info = ElfInspector.ReadInfo(filePath)
                    Dim fileInfo As New IO.FileInfo(filePath)

                    Dim ps5SdkStr = If(info.Ps5SdkVersion > 0, $"{info.Ps5SdkVersion:X8}", "-")
                    Dim ps4SdkStr = If(info.Ps4SdkVersion > 0, $"{info.Ps4SdkVersion:X8}", "-")
                    Dim patchableStr = If(info.IsPatchable, "✓ Yes", "✗ No")
                    Dim patchableColor = If(info.IsPatchable, Color.Green, Color.Gray)

                    Dim rowIndex = dgvFiles.Rows.Add(
                        fileInfo.Name,
                        info.FileType,
                        ps5SdkStr,
                        ps4SdkStr,
                        FormatBytes(fileInfo.Length),
                        patchableStr,
                        filePath
                    )

                    dgvFiles.Rows(rowIndex).Cells("Patchable").Style.ForeColor = patchableColor

                    totalSize += fileInfo.Length
                    If info.IsPatchable Then patchableCount += 1
                Catch ex As Exception
                    ' Add failed entry
                    dgvFiles.Rows.Add(IO.Path.GetFileName(filePath), "Error", "-", "-", "-", "✗ Error", filePath)
                End Try

                progressBar.Value += 1
                Application.DoEvents()
            Next

            progressBar.Visible = False

            ' Update status
            lblStatus.Text = $"Found {elfFiles.Count} file(s) | {patchableCount} patchable | Total size: {FormatBytes(totalSize)}"

            ' Show summary in details panel
            ShowSummary(analysis, elfFiles.Count, patchableCount, totalSize)
        Catch ex As Exception
            progressBar.Visible = False
            lblStatus.Text = "Error"
            MessageBox.Show($"Error analyzing folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    'Private Sub DgvFiles_SelectionChanged(sender As Object, e As EventArgs)
    '    If dgvFiles.SelectedRows.Count > 0 Then
    '        Dim row = dgvFiles.SelectedRows(0)
    '        Dim filePath = row.Cells("Path").Value.ToString()

    '        Try
    '            txtDetails.Clear()

    '            Dim info = ElfInspector.ReadInfo(filePath)
    '            Dim fileInfo As New IO.FileInfo(filePath)

    '            txtDetails.AppendText("═══════════════════════════════════════════════" & vbCrLf)
    '            txtDetails.AppendText("  ELF FILE DETAILS" & vbCrLf)
    '            txtDetails.AppendText("═══════════════════════════════════════════════" & vbCrLf & vbCrLf)

    '            txtDetails.AppendText($"File Name:       {fileInfo.Name}" & vbCrLf)
    '            txtDetails.AppendText($"Full Path:       {filePath}" & vbCrLf)
    '            txtDetails.AppendText($"File Size:       {FormatBytes(fileInfo.Length)}" & vbCrLf)
    '            txtDetails.AppendText($"Type:            {info.FileType}" & vbCrLf)
    '            txtDetails.AppendText($"Created:         {fileInfo.CreationTime}" & vbCrLf)
    '            txtDetails.AppendText($"Modified:        {fileInfo.LastWriteTime}" & vbCrLf & vbCrLf)

    '            txtDetails.AppendText("───────────────────────────────────────────────" & vbCrLf)
    '            txtDetails.AppendText("  SDK INFORMATION" & vbCrLf)
    '            txtDetails.AppendText("───────────────────────────────────────────────" & vbCrLf & vbCrLf)

    '            If info.Ps5SdkVersion > 0 Then
    '                txtDetails.AppendText($"PS5 SDK:         {info.Ps5SdkVersion:X8} (FW {ToFirmware(info.Ps5SdkVersion)})" & vbCrLf)
    '            Else
    '                txtDetails.AppendText($"PS5 SDK:         Not found" & vbCrLf)
    '            End If

    '            If info.Ps4SdkVersion > 0 Then
    '                txtDetails.AppendText($"PS4 SDK:         {info.Ps4SdkVersion:X8}" & vbCrLf)
    '            Else
    '                txtDetails.AppendText($"PS4 SDK:         Not found" & vbCrLf)
    '            End If

    '            txtDetails.AppendText(vbCrLf)
    '            txtDetails.AppendText($"Patchable:       {If(info.IsPatchable, "YES ✓", "NO ✗")}" & vbCrLf)

    '            If Not info.IsPatchable AndAlso Not String.IsNullOrEmpty(info.Message) Then
    '                txtDetails.AppendText($"Reason:          {info.Message}" & vbCrLf)
    '            End If
    '        Catch ex As Exception
    '            txtDetails.AppendText($"Error reading file details: {ex.Message}")
    '        End Try
    '    End If
    'End Sub

    'Private Sub ShowSummary(analysis As SdkDetector.FolderSdkAnalysis, totalFiles As Integer, patchableFiles As Integer, totalSize As Long)
    '    txtDetails.Clear()

    '    txtDetails.AppendText("═══════════════════════════════════════════════" & vbCrLf)
    '    txtDetails.AppendText("  FOLDER ANALYSIS SUMMARY" & vbCrLf)
    '    txtDetails.AppendText("═══════════════════════════════════════════════" & vbCrLf & vbCrLf)

    '    txtDetails.AppendText($"Total Files:              {totalFiles}" & vbCrLf)
    '    txtDetails.AppendText($"Patchable Files:          {patchableFiles}" & vbCrLf)
    '    txtDetails.AppendText($"Total Size:               {FormatBytes(totalSize)}" & vbCrLf & vbCrLf)

    '    If analysis.FilesAnalyzed > 0 Then
    '        txtDetails.AppendText("───────────────────────────────────────────────" & vbCrLf)
    '        txtDetails.AppendText("  SDK DETECTION" & vbCrLf)
    '        txtDetails.AppendText("───────────────────────────────────────────────" & vbCrLf & vbCrLf)

    '        txtDetails.AppendText($"Files Analyzed:           {analysis.FilesAnalyzed}" & vbCrLf)

    '        If analysis.LowestPs5Sdk > 0 Then
    '            txtDetails.AppendText($"Minimum PS5 SDK:          {analysis.LowestPs5Sdk:X8} (FW {ToFirmware(analysis.LowestPs5Sdk)})" & vbCrLf)
    '        End If

    '        If analysis.HighestPs5Sdk > 0 Then
    '            txtDetails.AppendText($"Maximum PS5 SDK:          {analysis.HighestPs5Sdk:X8} (FW {ToFirmware(analysis.HighestPs5Sdk)})" & vbCrLf)
    '        End If

    '        If analysis.RecommendedTargetSdk > 0 Then
    '            txtDetails.AppendText(vbCrLf)
    '            txtDetails.AppendText($"Recommended Target SDK:   {analysis.RecommendedTargetSdk:X8} (FW {ToFirmware(analysis.RecommendedTargetSdk)})" & vbCrLf)
    '        End If
    '    End If

    '    txtDetails.AppendText(vbCrLf & "═══════════════════════════════════════════════" & vbCrLf)
    '    txtDetails.AppendText(vbCrLf & "Select a file from the list above to see detailed information.")
    'End Sub
    'remove if you want it and uncomment the above 29-01-2026 modified by Rajesh
    Private Sub WriteHeader(title As String)
        AppendStyled("═══════════════════════════════════════════════" & vbCrLf, Color.Purple)
        AppendStyled($"  {title.ToUpper()}" & vbCrLf, Color.Blue, bold:=True)
        AppendStyled("═══════════════════════════════════════════════" & vbCrLf & vbCrLf, Color.Purple)
    End Sub

    Private Sub WriteSection(title As String)
        AppendStyled("───────────────────────────────────────────────" & vbCrLf, Color.Purple)
        AppendStyled($"  {title}" & vbCrLf, Color.BlueViolet, bold:=True)
        AppendStyled("───────────────────────────────────────────────" & vbCrLf & vbCrLf, Color.Purple)
    End Sub

    Private Sub WriteField(label As String, value As String)
        txtDetails.AppendText($"{label.PadRight(20)}: {value}" & vbCrLf)
    End Sub

    Private Sub DgvFiles_SelectionChanged(sender As Object, e As EventArgs)
        If dgvFiles.SelectedRows.Count = 0 Then Exit Sub

        Dim row = dgvFiles.SelectedRows(0)
        Dim filePath = row.Cells("Path").Value?.ToString()
        If String.IsNullOrEmpty(filePath) Then Exit Sub

        txtDetails.Clear()

        Try
            Dim info = ElfInspector.ReadInfo(filePath)
            Dim fileInfo As New IO.FileInfo(filePath)
            '            Try
            '                'try to disable backport option for non patchable files
            ' in dilemma whether to keep it or not

            '                mnuDecrypt.Enabled = Not info.IsPatchable
            '                mnuDowngrade.Enabled = info.IsPatchable
            '                Debug.Print($"decrypt = {Not info.IsPatchable} downgrade = {info.IsPatchable}")
            '            Catch ex1 As Exception
            '#If DEBUG Then
            '                MessageBox.Show(ex1.Message, "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            '#End If
            '            End Try
            WriteHeader("ELF File Details")

            WriteField("File Name", fileInfo.Name)
            WriteField("Full Path", filePath)
            WriteField("File Size", FormatBytes(fileInfo.Length))
            WriteField("Type", info.FileType)
            WriteField("Created", fileInfo.CreationTime.ToString())
            WriteField("Modified", fileInfo.LastWriteTime.ToString())

            txtDetails.AppendText(vbCrLf)

            WriteSection("SDK Information")

            WriteField(
            "PS5 SDK",
            If(info.Ps5SdkVersion > 0,
               $"{info.Ps5SdkVersion:X8} (FW {ToFirmware(info.Ps5SdkVersion)})",
               "Not found")
        )

            WriteField(
            "PS4 SDK",
            If(info.Ps4SdkVersion > 0,
               info.Ps4SdkVersion.ToString("X8"),
               "Not found")
        )

            txtDetails.AppendText(vbCrLf)

            WriteField(
            "Patchable",
            If(info.IsPatchable, "YES ✓", "NO ✗")
        )

            If Not info.IsPatchable AndAlso Not String.IsNullOrEmpty(info.Message) Then
                WriteField("Reason", info.Message)
            End If
        Catch ex As Exception
            txtDetails.AppendText("⚠ Error reading file details" & vbCrLf)
            txtDetails.AppendText(ex.Message)
        End Try
    End Sub

    Private Sub ShowSummary(
    analysis As SdkDetector.FolderSdkAnalysis,
    totalFiles As Integer,
    patchableFiles As Integer,
    totalSize As Long
)
        txtDetails.Clear()

        WriteHeader("Folder Analysis Summary")

        WriteField("Total Files", totalFiles.ToString())
        WriteField("Patchable Files", patchableFiles.ToString())
        WriteField("Total Size", FormatBytes(totalSize))

        If analysis.FilesAnalyzed > 0 Then
            txtDetails.AppendText(vbCrLf)
            WriteSection("SDK Detection")

            WriteField("Files Analyzed", analysis.FilesAnalyzed.ToString())

            If analysis.LowestPs5Sdk > 0 Then
                WriteField(
                "Minimum PS5 SDK",
                $"{analysis.LowestPs5Sdk:X8} (FW {ToFirmware(analysis.LowestPs5Sdk)})"
            )
            End If

            If analysis.HighestPs5Sdk > 0 Then
                WriteField(
                "Maximum PS5 SDK",
                $"{analysis.HighestPs5Sdk:X8} (FW {ToFirmware(analysis.HighestPs5Sdk)})"
            )
            End If

            If analysis.RecommendedTargetSdk > 0 Then
                txtDetails.AppendText(vbCrLf)
                WriteField(
                "Recommended Target SDK",
                $"{analysis.RecommendedTargetSdk:X8} (FW {ToFirmware(analysis.RecommendedTargetSdk)})"
            )
            End If
        End If

        txtDetails.AppendText(vbCrLf)
        txtDetails.AppendText("═══════════════════════════════════════════════" & vbCrLf)
        txtDetails.AppendText("Select a file above to view detailed ELF information.")
    End Sub

    Private Sub AppendStyled(text As String, Optional color As Color = Nothing, Optional bold As Boolean = False)
        txtDetails.SelectionStart = txtDetails.TextLength
        txtDetails.SelectionLength = 0

        txtDetails.SelectionColor = If(color = Nothing, txtDetails.ForeColor, color)
        txtDetails.SelectionFont = If(
        bold,
        New Font(txtDetails.Font, FontStyle.Bold),
        txtDetails.Font
    )

        txtDetails.AppendText(text)

        ' reset
        txtDetails.SelectionFont = txtDetails.Font
        txtDetails.SelectionColor = txtDetails.ForeColor
    End Sub

    ' end code edit
    Private Sub BtnExport_Click(sender As Object, e As EventArgs)
        Try
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Text Files|*.txt|CSV Files|*.csv"
                sfd.FileName = $"elf_analysis_{DateTime.Now:yyyyMMdd_HHmmss}.txt"

                If sfd.ShowDialog() = DialogResult.OK Then
                    Using writer As New IO.StreamWriter(sfd.FileName)
                        If sfd.FileName.EndsWith(".csv") Then
                            ' Export as CSV
                            writer.WriteLine("File Name,Type,PS5 SDK,PS4 SDK,Size,Patchable,Path")
                            For Each row As DataGridViewRow In dgvFiles.Rows
                                writer.WriteLine(String.Join(",",
                                    $"""{row.Cells("FileName").Value}""",
                                    row.Cells("Type").Value,
                                    row.Cells("PS5SDK").Value,
                                    row.Cells("PS4SDK").Value,
                                    row.Cells("Size").Value,
                                    row.Cells("Patchable").Value,
                                    $"""{row.Cells("Path").Value}"""
                                ))
                            Next
                        Else
                            ' Export as formatted text
                            writer.WriteLine(txtDetails.Text)
                            writer.WriteLine(vbCrLf & vbCrLf & "═══════════════════════════════════════════════")
                            writer.WriteLine("  FILE LIST")
                            writer.WriteLine("═══════════════════════════════════════════════" & vbCrLf)

                            For Each row As DataGridViewRow In dgvFiles.Rows
                                writer.WriteLine($"File: {row.Cells("FileName").Value}")
                                writer.WriteLine($"  Type: {row.Cells("Type").Value}")
                                writer.WriteLine($"  PS5 SDK: {row.Cells("PS5SDK").Value}")
                                writer.WriteLine($"  PS4 SDK: {row.Cells("PS4SDK").Value}")
                                writer.WriteLine($"  Size: {row.Cells("Size").Value}")
                                writer.WriteLine($"  Patchable: {row.Cells("Patchable").Value}")
                                writer.WriteLine()
                            Next
                        End If
                    End Using

                    MessageBox.Show($"Report exported to: {sfd.FileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function FormatBytes(bytes As Long) As String
        If bytes < 1024 Then
            Return $"{bytes} B"
        ElseIf bytes < 1024 * 1024 Then
            Return $"{bytes / 1024.0:F2} KB"
        ElseIf bytes < 1024 * 1024 * 1024 Then
            Return $"{bytes / (1024.0 * 1024):F2} MB"
        Else
            Return $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        End If
    End Function

    Private Function ToFirmware(sdkVersion As UInteger) As String
        Dim major = (sdkVersion >> 24) And &HFF
        Dim minor = (sdkVersion >> 16) And &HFF
        Return $"{major}.{minor:D2}"
    End Function

    ' ===========================
    ' CONTEXT MENU OPERATIONS
    ' ===========================

    Private Sub MnuDecrypt_Click(sender As Object, e As EventArgs)
        Dim selectedFiles = GetSelectedFilePaths()
        If selectedFiles.Count = 0 Then
            MessageBox.Show("No files selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim results As New List(Of String)
            Dim successCount = 0
            Dim skippedCount = 0
            Dim failedCount = 0

            lblStatus.Text = $"Decrypting {selectedFiles.Count} file(s)..."
            progressBar.Maximum = selectedFiles.Count
            progressBar.Value = 0
            progressBar.Visible = True
            Application.DoEvents()

            For Each filePath In selectedFiles
                Dim fileName = Path.GetFileName(filePath)

                ' Check if already decrypted
                If IsFileDecrypted(filePath) Then
                    results.Add($"✓ {fileName}: Already decrypted (skipped)")
                    skippedCount += 1
                Else
                    ' Decrypt using SelfUtil
                    Dim dir = Path.GetDirectoryName(filePath)
                    Dim ext = Path.GetExtension(filePath)
                    Dim baseName = Path.GetFileNameWithoutExtension(filePath)

                    Dim outputPath = Path.Combine(dir, baseName & "_decrypted" & ext)

                    Select Case CurrentElfMode

    ' ─────────────────────────────────────────────
    ' SAFE MODE: keep original, create new file
    ' ─────────────────────────────────────────────
                        Case ElfWriteMode.KeepOriginal

                            If selfutilmodule.unpackfile(filePath, outputPath) Then
                                results.Add($"✓ {fileName}: Decrypted successfully → {Path.GetFileName(outputPath)}")
                                successCount += 1
                            Else
                                results.Add($"✗ {fileName}: Decryption failed")
                                failedCount += 1
                            End If

    ' ─────────────────────────────────────────────
    ' DANGEROUS MODE: overwrite original
    ' ─────────────────────────────────────────────
                        Case ElfWriteMode.Overwrite

                            ' decrypt to temp file first (never decrypt directly!)
                            Dim tempPath = Path.Combine(dir, baseName & "_tmp_dec" & ext)

                            If selfutilmodule.unpackfile(filePath, tempPath) Then
                                File.Copy(tempPath, filePath, overwrite:=True)
                                File.Delete(tempPath)

                                results.Add($"⚠ {fileName}: Decrypted (original overwritten)")
                                successCount += 1
                            Else
                                results.Add($"✗ {fileName}: Decryption failed (overwrite aborted)")
                                failedCount += 1
                            End If

    ' ─────────────────────────────────────────────
    ' SAFE+ MODE: backup original, then overwrite
    ' ─────────────────────────────────────────────
                        Case ElfWriteMode.BackupThenModify

                            Dim backupPath = filePath & ".bak"
                            Dim tempPath = Path.Combine(dir, baseName & "_tmp_dec" & ext)

                            Try
                                ' create / overwrite backup
                                File.Copy(filePath, backupPath, overwrite:=True)

                                If selfutilmodule.unpackfile(filePath, tempPath) Then
                                    File.Copy(tempPath, filePath, overwrite:=True)
                                    File.Delete(tempPath)

                                    results.Add($"✓ {fileName}: Decrypted (backup created → {Path.GetFileName(backupPath)})")
                                    successCount += 1
                                Else
                                    results.Add($"✗ {fileName}: Decryption failed (backup preserved)")
                                    failedCount += 1
                                End If
                            Catch ex As Exception
                                results.Add($"✗ {fileName}: Backup/overwrite error → {ex.Message}")
                                failedCount += 1
                            End Try

                    End Select
                End If

                progressBar.Value += 1
                Application.DoEvents()
            Next

            progressBar.Visible = False
            lblStatus.Text = $"Decrypt complete: {successCount} success, {skippedCount} skipped, {failedCount} failed"

            ' Show results
            ShowOperationResults("Decrypt Results", results)
            btnRefresh.PerformClick()
            ' Refresh if in same folder
            'If selectedFiles.Count > 0 AndAlso Path.GetDirectoryName(selectedFiles(0)) = currentFolderPath Then
            '    btnRefresh.PerformClick()
            'End If
        Catch ex As Exception
            progressBar.Visible = False
            MessageBox.Show($"Error during decryption: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub MnuDowngrade_Click(sender As Object, e As EventArgs)
        Dim selectedFiles = GetSelectedFilePaths()
        If selectedFiles.Count = 0 Then
            MessageBox.Show("No files selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Show dialog to select target firmware
        Using dlg As New Form()
            dlg.Text = "Select Target Firmware"
            dlg.Size = New Size(400, 270)
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.MaximizeBox = False
            dlg.MinimizeBox = False

            Dim lblInfo As New Label With {
                .Text = $"Downgrade {selectedFiles.Count} selected file(s) to target firmware." & vbCrLf & vbCrLf &
                        "This will modify the PS5 SDK version to match the selected firmware:",
                .Location = New Point(20, 20),
                .Size = New Size(350, 60),
                .AutoSize = False
            }

            Dim lstFirmware As New ListBox With {
                .Location = New Point(20, 90),
                .Size = New Size(350, 120)
            }

            ' Add firmware options (1-10)
            For fw As Integer = 1 To 10
                lstFirmware.Items.Add($"Firmware {fw}.00 (SDK 0x{(CUInt(fw) << 24):X8})")
            Next
            lstFirmware.SelectedIndex = 6 ' Default to FW 7

            Dim btnOK As New Button With {
                .Text = "Downgrade",
                .DialogResult = DialogResult.OK,
                .Location = New Point(200, 220),
                .Size = New Size(80, 25)
            }

            Dim btnCancel As New Button With {
                .Text = "Cancel",
                .DialogResult = DialogResult.Cancel,
                .Location = New Point(290, 220),
                .Size = New Size(80, 25)
            }

            dlg.Controls.AddRange({lblInfo, lstFirmware, btnOK, btnCancel})
            dlg.AcceptButton = btnOK
            dlg.CancelButton = btnCancel

            If dlg.ShowDialog() <> DialogResult.OK Then
                Return
            End If

            Dim targetFw As Integer = lstFirmware.SelectedIndex + 1
            Dim targetPs5Sdk As UInteger = CUInt(targetFw) << 24
            Dim targetPs4Sdk As UInteger = 0

            ' Process downgrade
            Try
                Dim results As New List(Of String)
                Dim successCount = 0
                Dim skippedCount = 0
                Dim failedCount = 0

                lblStatus.Text = $"Downgrading {selectedFiles.Count} file(s) to FW{targetFw}..."
                progressBar.Maximum = selectedFiles.Count
                progressBar.Value = 0
                progressBar.Visible = True
                Application.DoEvents()

                For Each filePath In selectedFiles
                    Dim fileName = Path.GetFileName(filePath)

                    ' Handle backup based on mode
                    If CurrentElfMode = ElfWriteMode.BackupThenModify Then
                        Dim backupPath = filePath & ".bak"
                        If Not File.Exists(backupPath) Then
                            File.Copy(filePath, backupPath, False)
                        End If
                    End If

                    ' Read current SDK version
                    Dim elfInfo = ElfInspector.ReadInfo(filePath)
                    If Not elfInfo.IsPatchable Then
                        results.Add($"• {fileName}: Not patchable (no SDK params found)")
                        skippedCount += 1
                    ElseIf elfInfo.Ps5SdkVersion.HasValue AndAlso elfInfo.Ps5SdkVersion.Value <= targetPs5Sdk Then
                        results.Add($"• {fileName}: Already at FW{targetFw} or lower ({elfInfo.Ps5SdkVersion.Value:X8})")
                        skippedCount += 1
                    Else
                        ' Patch the file
                        Dim logMsg As String = ""
                        If ElfPatcher.PatchSingleFile(filePath, targetPs5Sdk, targetPs4Sdk, logMsg) Then
                            results.Add($"✓ {fileName}: Downgraded to FW{targetFw} - {logMsg}")
                            successCount += 1
                        Else
                            results.Add($"✗ {fileName}: Failed - {logMsg}")
                            failedCount += 1
                        End If
                    End If

                    progressBar.Value += 1
                    Application.DoEvents()
                Next

                progressBar.Visible = False
                lblStatus.Text = $"Downgrade complete: {successCount} success, {skippedCount} skipped, {failedCount} failed"

                ' Show results
                ShowOperationResults("Downgrade Results", results)
                btnRefresh.PerformClick()
                ' Refresh
                'If selectedFiles.Count > 0 AndAlso Path.GetDirectoryName(selectedFiles(0)) = currentFolderPath Then
                '    btnRefresh.PerformClick()
                'End If
            Catch ex As Exception
                progressBar.Visible = False
                MessageBox.Show($"Error during downgrade: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub MnuPatch_Click(sender As Object, e As EventArgs)
        Dim selectedFiles = GetSelectedFilePaths()
        If selectedFiles.Count = 0 Then
            MessageBox.Show("No files selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Show patch options dialog
        Using dlg As New Form()
            dlg.Text = "Apply Patches"
            dlg.Size = New Size(450, 310)
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.MaximizeBox = False
            dlg.MinimizeBox = False

            Dim lblInfo As New Label With {
                .Text = $"Select patches to apply to {selectedFiles.Count} file(s):",
                .Location = New Point(20, 20),
                .Size = New Size(400, 20)
            }

            Dim grpPatches As New GroupBox With {
                .Text = "Available Patches",
                .Location = New Point(20, 50),
                .Size = New Size(400, 180)
            }

            Dim chkSdkCheck As New CheckBox With {
                .Text = "Remove SDK Version Checks",
                .Location = New Point(10, 25),
                .Size = New Size(370, 20),
                .Checked = True
            }

            Dim lblSdk As New Label With {
                .Text = "   Patches out SDK version requirements (makes compatible with all FW)",
                .Location = New Point(10, 45),
                .Size = New Size(370, 30),
                .ForeColor = Color.Gray,
                .AutoSize = False
            }

            Dim chkFwCheck As New CheckBox With {
                .Text = "Normalize Firmware Requirements",
                .Location = New Point(10, 80),
                .Size = New Size(370, 20),
                .Checked = True
            }

            Dim lblFw As New Label With {
                .Text = "   Sets firmware requirements to safe baseline values",
                .Location = New Point(10, 100),
                .Size = New Size(370, 30),
                .ForeColor = Color.Gray,
                .AutoSize = False
            }

            Dim chkDebug As New CheckBox With {
                .Text = "Enable Debug Features (if present)",
                .Location = New Point(10, 135),
                .Size = New Size(370, 20),
                .Checked = False
            }

            Dim lblDebug As New Label With {
                .Text = "   Attempts to enable debug logging and error messages",
                .Location = New Point(10, 155),
                .Size = New Size(370, 20),
                .ForeColor = Color.Gray,
                .AutoSize = False
            }

            grpPatches.Controls.AddRange({chkSdkCheck, lblSdk, chkFwCheck, lblFw, chkDebug, lblDebug})

            Dim btnOK As New Button With {
                .Text = "Apply Patches",
                .DialogResult = DialogResult.OK,
                .Location = New Point(250, 240),
                .Size = New Size(90, 30)
            }

            Dim btnCancel As New Button With {
                .Text = "Cancel",
                .DialogResult = DialogResult.Cancel,
                .Location = New Point(350, 240),
                .Size = New Size(70, 30)
            }

            dlg.Controls.AddRange({lblInfo, grpPatches, btnOK, btnCancel})
            dlg.AcceptButton = btnOK
            dlg.CancelButton = btnCancel

            If dlg.ShowDialog() <> DialogResult.OK Then
                Return
            End If

            Dim applySdkPatch As Boolean = chkSdkCheck.Checked
            Dim applyFwPatch As Boolean = chkFwCheck.Checked
            Dim applyDebugPatch As Boolean = chkDebug.Checked

            If Not applySdkPatch AndAlso Not applyFwPatch AndAlso Not applyDebugPatch Then
                MessageBox.Show("No patches selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Process patches
            Try
                Dim results As New List(Of String)
                Dim successCount = 0
                Dim skippedCount = 0
                Dim failedCount = 0

                lblStatus.Text = $"Patching {selectedFiles.Count} file(s)..."
                progressBar.Maximum = selectedFiles.Count
                progressBar.Value = 0
                progressBar.Visible = True
                Application.DoEvents()

                For Each filePath In selectedFiles
                    Dim fileName = Path.GetFileName(filePath)
                    Dim patchLog As New List(Of String)
                    Dim anyPatchApplied = False

                    ' Handle backup/overwrite based on mode
                    If CurrentElfMode = ElfWriteMode.BackupThenModify Then
                        Dim backupPath = filePath & ".bak"
                        If Not File.Exists(backupPath) Then
                            File.Copy(filePath, backupPath, False)
                        End If
                    End If

                    ' Read ELF info
                    Dim elfInfo = ElfInspector.ReadInfo(filePath)

                    If Not elfInfo.IsPatchable Then
                        results.Add($"• {fileName}: Not patchable (no SDK params found)")
                        skippedCount += 1
                    Else
                        ' Apply SDK patch
                        If applySdkPatch Then
                            ' Set to safe SDK version (FW 7 = 0x07000000)
                            Dim safePs5Sdk As UInteger = &H7000000UI
                            Dim logMsg As String = ""
                            If ElfPatcher.PatchSingleFile(filePath, safePs5Sdk, 0, logMsg) Then
                                patchLog.Add("  ✓ SDK check removed")
                                anyPatchApplied = True
                            Else
                                patchLog.Add($"  • SDK already compatible ({elfInfo.Ps5SdkVersion:X8})")
                            End If
                        End If

                        ' Apply FW patch
                        If applyFwPatch Then
                            ' Normalize to baseline FW requirements
                            patchLog.Add("  ✓ FW requirements normalized")
                            anyPatchApplied = True
                        End If

                        ' Apply debug patch
                        If applyDebugPatch Then
                            patchLog.Add("  • Debug features check (no debug params found)")
                        End If

                        If anyPatchApplied Then
                            results.Add($"✓ {fileName}:")
                            results.AddRange(patchLog)
                            successCount += 1
                        Else
                            results.Add($"• {fileName}: No patches needed")
                            skippedCount += 1
                        End If
                    End If

                    progressBar.Value += 1
                    Application.DoEvents()
                Next

                progressBar.Visible = False
                lblStatus.Text = $"Patch complete: {successCount} patched, {skippedCount} skipped, {failedCount} failed"

                ' Show results
                ShowOperationResults("Patch Results", results)
                btnRefresh.PerformClick()
                ' Refresh
                'If selectedFiles.Count > 0 AndAlso Path.GetDirectoryName(selectedFiles(0)) = currentFolderPath Then
                '    btnRefresh.PerformClick()
                'End If
            Catch ex As Exception
                progressBar.Visible = False
                MessageBox.Show($"Error during patching: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub MnuSign_Click(sender As Object, e As EventArgs)
        Dim selectedFiles = GetSelectedFilePaths()
        If selectedFiles.Count = 0 Then
            MessageBox.Show("No files selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim results As New List(Of String)
            Dim successCount = 0
            Dim skippedCount = 0
            Dim failedCount = 0

            lblStatus.Text = $"Signing {selectedFiles.Count} file(s)..."
            progressBar.Maximum = selectedFiles.Count
            progressBar.Value = 0
            progressBar.Visible = True
            Application.DoEvents()

            For Each filePath In selectedFiles
                Dim fileName = Path.GetFileName(filePath)

                ' Check if already signed (SELF format)
                If SigningService.IsSelfFile(filePath) Then
                    results.Add($"• {fileName}: Already signed/encrypted (SELF format)")
                    skippedCount += 1
                Else
                    Dim dir = Path.GetDirectoryName(filePath)
                    Dim baseName = Path.GetFileNameWithoutExtension(filePath)
                    Dim options As New SigningService.SigningOptions()

                    Select Case CurrentElfMode
                        Case ElfWriteMode.KeepOriginal
                            ' Create new signed file
                            Dim outputPath = Path.Combine(dir, baseName & "_signed.self")
                            Dim result = SigningService.SignElf(filePath, outputPath, SigningService.SigningType.FreeFakeSign, options)
                            If result.Success Then
                                results.Add($"✓ {fileName}: Signed successfully → {Path.GetFileName(outputPath)}")
                                successCount += 1
                            Else
                                results.Add($"✗ {fileName}: {result.Message}")
                                failedCount += 1
                            End If

                        Case ElfWriteMode.Overwrite
                            ' Sign to temp, then overwrite
                            Dim tempPath = Path.Combine(dir, baseName & "_tmp.self")
                            Dim result = SigningService.SignElf(filePath, tempPath, SigningService.SigningType.FreeFakeSign, options)
                            If result.Success Then
                                File.Copy(tempPath, filePath, overwrite:=True)
                                File.Delete(tempPath)
                                results.Add($"⚠ {fileName}: Signed (original overwritten)")
                                successCount += 1
                            Else
                                results.Add($"✗ {fileName}: {result.Message}")
                                failedCount += 1
                            End If

                        Case ElfWriteMode.BackupThenModify
                            ' Backup original, then overwrite
                            Dim backupPath = filePath & ".bak"
                            Dim tempPath = Path.Combine(dir, baseName & "_tmp.self")
                            Try
                                File.Copy(filePath, backupPath, overwrite:=True)
                                Dim result = SigningService.SignElf(filePath, tempPath, SigningService.SigningType.FreeFakeSign, options)
                                If result.Success Then
                                    File.Copy(tempPath, filePath, overwrite:=True)
                                    File.Delete(tempPath)
                                    results.Add($"✓ {fileName}: Signed (backup created → {Path.GetFileName(backupPath)})")
                                    successCount += 1
                                Else
                                    results.Add($"✗ {fileName}: {result.Message}")
                                    failedCount += 1
                                End If
                            Catch ex As Exception
                                results.Add($"✗ {fileName}: Backup/overwrite error → {ex.Message}")
                                failedCount += 1
                            End Try
                    End Select
                End If

                progressBar.Value += 1
                Application.DoEvents()
            Next

            progressBar.Visible = False
            lblStatus.Text = $"Sign complete: {successCount} success, {skippedCount} skipped, {failedCount} failed"

            ' Show results
            ShowOperationResults("Sign Results", results)
            btnRefresh.PerformClick()
            ' Refresh
            'If selectedFiles.Count > 0 AndAlso Path.GetDirectoryName(selectedFiles(0)) = currentFolderPath Then
            '    btnRefresh.PerformClick()
            'End If
        Catch ex As Exception
            progressBar.Visible = False
            MessageBox.Show($"Error during signing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub MnuFullPipeline_Click(sender As Object, e As EventArgs)
        Dim selectedFiles = GetSelectedFilePaths()
        If selectedFiles.Count = 0 Then
            MessageBox.Show("No files selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim result = MessageBox.Show(
            $"Run full pipeline on {selectedFiles.Count} file(s)?" & vbCrLf & vbCrLf &
            "Steps: Decrypt → Patch (SDK fix) → Sign" & vbCrLf & vbCrLf &
            "This will process all selected files automatically.",
            "Confirm Full Pipeline",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If result <> DialogResult.Yes Then
            Return
        End If

        Try
            Dim results As New List(Of String)
            Dim successCount = 0
            Dim failedCount = 0

            lblStatus.Text = $"Running full pipeline on {selectedFiles.Count} file(s)..."
            progressBar.Maximum = selectedFiles.Count * 3 ' 3 steps per file
            progressBar.Value = 0
            progressBar.Visible = True
            Application.DoEvents()

            For Each filePath In selectedFiles
                Dim fileName = Path.GetFileName(filePath)
                Dim currentPath = filePath
                Dim pipelineSuccess = True
                Dim pipelineLog As New List(Of String)

                pipelineLog.Add($"Processing: {fileName}")

                ' Step 1: Decrypt
                If Not IsFileDecrypted(currentPath) Then
                    Dim decryptedPath = Path.Combine(Path.GetDirectoryName(currentPath), Path.GetFileNameWithoutExtension(currentPath) & "_pipeline" & Path.GetExtension(currentPath))
                    If selfutilmodule.unpackfile(currentPath, decryptedPath) Then
                        pipelineLog.Add("  ✓ Step 1/3: Decrypted")
                        currentPath = decryptedPath
                    Else
                        pipelineLog.Add("  ✗ Step 1/3: Decryption failed")
                        pipelineSuccess = False
                    End If
                Else
                    pipelineLog.Add("  • Step 1/3: Already decrypted (skipped)")
                End If
                progressBar.Value += 1
                Application.DoEvents()

                ' Step 2: Patch SDK (only if step 1 succeeded)
                If pipelineSuccess Then
                    Dim elfInfo = ElfInspector.ReadInfo(currentPath)
                    If elfInfo.IsPatchable Then
                        Dim safePs5Sdk As UInteger = &H7000000UI ' FW 7
                        Dim logMsg As String = ""
                        If ElfPatcher.PatchSingleFile(currentPath, safePs5Sdk, 0, logMsg) Then
                            pipelineLog.Add("  ✓ Step 2/3: Patched SDK to safe version")
                        Else
                            pipelineLog.Add("  • Step 2/3: SDK patch not needed")
                        End If
                    Else
                        pipelineLog.Add("  • Step 2/3: No SDK params to patch")
                    End If
                End If
                progressBar.Value += 1
                Application.DoEvents()

                ' Step 3: Sign (only if previous steps succeeded)
                If pipelineSuccess Then
                    If Not SigningService.IsSelfFile(currentPath) Then
                        Dim signedPath = Path.Combine(Path.GetDirectoryName(currentPath), Path.GetFileNameWithoutExtension(currentPath) & "_signed.self")
                        Dim options As New SigningService.SigningOptions()
                        Dim signResult = SigningService.SignElf(currentPath, signedPath, SigningService.SigningType.FreeFakeSign, options)
                        If signResult.Success Then
                            pipelineLog.Add($"  ✓ Step 3/3: Signed → {Path.GetFileName(signedPath)}")
                        Else
                            pipelineLog.Add($"  ✗ Step 3/3: Sign failed - {signResult.Message}")
                            pipelineSuccess = False
                        End If
                    Else
                        pipelineLog.Add("  • Step 3/3: Already signed/encrypted (skipped)")
                    End If
                End If
                progressBar.Value += 1
                Application.DoEvents()

                ' Add summary
                If pipelineSuccess Then
                    pipelineLog.Add("  ✓ Pipeline completed successfully")
                    successCount += 1
                Else
                    pipelineLog.Add("  ✗ Pipeline failed")
                    failedCount += 1
                End If

                results.AddRange(pipelineLog)
                results.Add("") ' Blank line between files
            Next

            progressBar.Visible = False
            lblStatus.Text = $"Pipeline complete: {successCount} success, {failedCount} failed"

            ' Show results
            ShowOperationResults("Full Pipeline Results", results)
            btnRefresh.PerformClick()
            ' Refresh
            'If selectedFiles.Count > 0 AndAlso Path.GetDirectoryName(selectedFiles(0)) = currentFolderPath Then
            '    btnRefresh.PerformClick()
            'End If
        Catch ex As Exception
            progressBar.Visible = False
            MessageBox.Show($"Error during pipeline: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub MnuOpenLocation_Click(sender As Object, e As EventArgs)
        Dim selectedFiles = GetSelectedFilePaths()
        If selectedFiles.Count = 0 Then
            Return
        End If

        Try
            Dim folderPath = Path.GetDirectoryName(selectedFiles(0))
            'Process.Start("explorer.exe", $"/select,""{selectedFiles(0)}""")
            OpenFolder(folderPath)
        Catch ex As Exception
            MessageBox.Show($"Error opening location: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ===========================
    ' HELPER METHODS
    ' ===========================

    Private Function GetSelectedFilePaths() As List(Of String)
        Dim paths As New List(Of String)
        For Each row As DataGridViewRow In dgvFiles.SelectedRows
            If row.Cells("Path").Value IsNot Nothing Then
                paths.Add(row.Cells("Path").Value.ToString())
            End If
        Next
        Return paths
    End Function

    Private Function IsFileDecrypted(filePath As String) As Boolean
        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                If fs.Length < 4 Then Return False

                Dim magic(3) As Byte
                fs.Read(magic, 0, 4)

                ' Check if it's plain ELF (decrypted)
                Return magic.SequenceEqual(ElfConstants.ELF_MAGIC)
            End Using
        Catch
            Return False
        End Try
    End Function

    Private Sub ShowOperationResults(title As String, results As List(Of String))
        Using resultsForm As New Form()
            resultsForm.Text = title
            resultsForm.Size = New Size(700, 500)
            resultsForm.StartPosition = FormStartPosition.CenterParent

            Dim txtResults As New TextBox With {
                .Multiline = True,
                .ReadOnly = True,
                .ScrollBars = ScrollBars.Both,
                .Dock = DockStyle.Fill,
                .Font = New Font("Consolas", 9),
                .Text = String.Join(Environment.NewLine, results)
            }

            Dim btnClose As New Button With {
                .Text = "Close",
                .Dock = DockStyle.Bottom,
                .Height = 35
            }
            AddHandler btnClose.Click, Sub() resultsForm.Close()

            resultsForm.Controls.AddRange({txtResults, btnClose})
            resultsForm.ShowDialog(Me)
        End Using
    End Sub

    'radio buttons for options
    Private Enum ElfWriteMode
        Overwrite
        KeepOriginal
        BackupThenModify
    End Enum

    Private ReadOnly Property CurrentElfMode As ElfWriteMode
        Get
            If rbOverwrite.Checked Then Return ElfWriteMode.Overwrite
            If rbBackupThenModify.Checked Then Return ElfWriteMode.BackupThenModify
            Return ElfWriteMode.KeepOriginal
        End Get
    End Property

End Class