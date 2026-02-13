Imports System.Windows.Forms
Imports System.IO

''' <summary>
''' Advanced operations form for signing ELF files
''' Supports multiple signing types and custom library management
''' </summary>
Public Class OperationModeForm
    Inherits Form

    ' UI Controls
    Private grpOperationMode As GroupBox

    Private rdoSignOnly As RadioButton
    Private rdoDecryptSign As RadioButton
    Private rdoFullWorkflow As RadioButton

    Private grpFileSelection As GroupBox
    Private lblInputFile As Label
    Private txtInputFile As TextBox
    Private btnBrowseInput As Button
    Private lblOutputFile As Label
    Private txtOutputFile As TextBox
    Private btnBrowseOutput As Button

    Private grpSigningSettings As GroupBox
    Private lblSigningType As Label
    Private cmbSigningType As ComboBox
    Private btnAdvancedOptions As Button

    Private grpProgress As GroupBox
    Private rtbLog As RichTextBox
    Private progressBar As ProgressBar

    Private btnStart As Button
    Private btnClose As Button

    Private lblFileInfo As Label

    ' Properties
    Private _signingOptions As SigningService.SigningOptions

    Private _selectedSigningType As SigningService.SigningType = SigningService.SigningType.FreeFakeSign

    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)

        InitializeComponent()
        _signingOptions = New SigningService.SigningOptions()
    End Sub

    'Private Sub InitializeComponent()
    '    Me.Text = "Advanced Operations - ELF Signing"
    '    Me.Size = New Size(700, 750)
    '    Me.FormBorderStyle = FormBorderStyle.Sizable
    '    Me.MinimumSize = New Size(700, 750)
    '    Me.StartPosition = FormStartPosition.CenterParent
    '    Me.Icon = Form1.Icon

    '    Dim yPos = 15

    '    ' Title label
    '    Dim lblTitle As New Label With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(650, 30),
    '        .Text = "🔧 Advanced Signing Operations",
    '        .Font = New Font("Segoe UI", 14, FontStyle.Bold),
    '        .ForeColor = Color.DarkBlue
    '    }
    '    Me.Controls.Add(lblTitle)
    '    yPos += 40

    '    ' Operation Mode Group
    '    grpOperationMode = New GroupBox With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(655, 100),
    '        .Text = "Operation Mode"
    '    }

    '    rdoSignOnly = New RadioButton With {
    '        .Location = New Point(15, 25),
    '        .Size = New Size(620, 22),
    '        .Text = "🟢 Sign Only - Sign an ELF file to create SELF",
    '        .Checked = True
    '    }
    '    AddHandler rdoSignOnly.CheckedChanged, AddressOf OperationMode_Changed

    '    rdoDecryptSign = New RadioButton With {
    '        .Location = New Point(15, 50),
    '        .Size = New Size(620, 22),
    '        .Text = "🟣 Decrypt Only - Extract ELF from SELF/BIN file",
    '        .Enabled = True
    '    }
    '    AddHandler rdoDecryptSign.CheckedChanged, AddressOf OperationMode_Changed

    '    rdoFullWorkflow = New RadioButton With {
    '        .Location = New Point(15, 75),
    '        .Size = New Size(620, 22),
    '        .Text = "🔵 Full Workflow - Decrypt SELF, then re-sign back to SELF",
    '        .Enabled = True
    '    }
    '    AddHandler rdoFullWorkflow.CheckedChanged, AddressOf OperationMode_Changed

    '    grpOperationMode.Controls.AddRange({rdoSignOnly, rdoDecryptSign, rdoFullWorkflow})
    '    Me.Controls.Add(grpOperationMode)
    '    yPos += 110

    '    ' File Selection Group
    '    grpFileSelection = New GroupBox With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(655, 120),
    '        .Text = "File Selection"
    '    }

    '    Dim fileYPos = 25

    '    ' Input file
    '    lblInputFile = New Label With {
    '        .Location = New Point(15, fileYPos),
    '        .Size = New Size(80, 20),
    '        .Text = "Input File:"
    '    }
    '    txtInputFile = New TextBox With {
    '        .Location = New Point(100, fileYPos),
    '        .Size = New Size(440, 23),
    '        .PlaceholderText = "Select ELF file to sign..."
    '    }
    '    AddHandler txtInputFile.TextChanged, AddressOf InputFile_Changed

    '    btnBrowseInput = New Button With {
    '        .Location = New Point(550, fileYPos - 2),
    '        .Size = New Size(90, 27),
    '        .Text = "Browse...",
    '        .BackColor = Color.LightSteelBlue,
    '        .FlatStyle = FlatStyle.Flat
    '    }
    '    AddHandler btnBrowseInput.Click, AddressOf BtnBrowseInput_Click
    '    fileYPos += 40

    '    ' Output file
    '    lblOutputFile = New Label With {
    '        .Location = New Point(15, fileYPos),
    '        .Size = New Size(80, 20),
    '        .Text = "Output File:"
    '    }
    '    txtOutputFile = New TextBox With {
    '        .Location = New Point(100, fileYPos),
    '        .Size = New Size(440, 23),
    '        .PlaceholderText = "Output SELF file path..."
    '    }
    '    AddHandler txtOutputFile.TextChanged, AddressOf ValidateInputs

    '    btnBrowseOutput = New Button With {
    '        .Location = New Point(550, fileYPos - 2),
    '        .Size = New Size(90, 27),
    '        .Text = "Browse...",
    '        .BackColor = Color.LightSteelBlue,
    '        .FlatStyle = FlatStyle.Flat
    '    }
    '    AddHandler btnBrowseOutput.Click, AddressOf BtnBrowseOutput_Click
    '    fileYPos += 40

    '    ' File info label
    '    lblFileInfo = New Label With {
    '        .Location = New Point(100, fileYPos),
    '        .Size = New Size(540, 20),
    '        .Font = New Font("Segoe UI", 8, FontStyle.Italic),
    '        .ForeColor = Color.DarkGray,
    '        .Text = "No file selected"
    '    }

    '    grpFileSelection.Controls.AddRange({
    '        lblInputFile, txtInputFile, btnBrowseInput,
    '        lblOutputFile, txtOutputFile, btnBrowseOutput,
    '        lblFileInfo
    '    })
    '    Me.Controls.Add(grpFileSelection)
    '    yPos += 130

    '    ' Signing Settings Group
    '    grpSigningSettings = New GroupBox With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(655, 80),
    '        .Text = "Signing Settings"
    '    }

    '    lblSigningType = New Label With {
    '        .Location = New Point(15, 30),
    '        .Size = New Size(90, 20),
    '        .Text = "Signing Type:"
    '    }
    '    cmbSigningType = New ComboBox With {
    '        .Location = New Point(110, 27),
    '        .Size = New Size(300, 23),
    '        .DropDownStyle = ComboBoxStyle.DropDownList
    '    }
    '    cmbSigningType.Items.AddRange(New String() {
    '        "Free Fake Sign (Homebrew)",
    '        "NPDRM (Content ID)",
    '        "Custom Keys (Advanced)"
    '    })
    '    cmbSigningType.SelectedIndex = 0

    '    btnAdvancedOptions = New Button With {
    '        .Location = New Point(430, 25),
    '        .Size = New Size(210, 28),
    '        .Text = "⚙️ Advanced Options...",
    '        .BackColor = Color.FromArgb(255, 215, 0),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 9, FontStyle.Bold)
    '    }
    '    AddHandler btnAdvancedOptions.Click, AddressOf BtnAdvancedOptions_Click

    '    grpSigningSettings.Controls.AddRange({lblSigningType, cmbSigningType, btnAdvancedOptions})
    '    Me.Controls.Add(grpSigningSettings)
    '    yPos += 90

    '    ' Progress Group
    '    grpProgress = New GroupBox With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(655, 280),
    '        .Text = "Progress & Log"
    '    }

    '    rtbLog = New RichTextBox With {
    '        .Location = New Point(10, 25),
    '        .Size = New Size(635, 210),
    '        .ReadOnly = True,
    '        .BackColor = Color.FromArgb(30, 30, 30),
    '        .ForeColor = Color.LightGreen,
    '        .Font = New Font("Consolas", 9),
    '        .BorderStyle = BorderStyle.Fixed3D
    '    }

    '    progressBar = New ProgressBar With {
    '        .Location = New Point(10, 245),
    '        .Size = New Size(635, 25),
    '        .Style = ProgressBarStyle.Continuous
    '    }

    '    grpProgress.Controls.AddRange({rtbLog, progressBar})
    '    Me.Controls.Add(grpProgress)
    '    yPos += 290

    '    ' Action Buttons
    '    btnStart = New Button With {
    '        .Location = New Point(450, yPos),
    '        .Size = New Size(110, 35),
    '        .Text = "▶️ Start",
    '        .BackColor = Color.FromArgb(144, 238, 144),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 10, FontStyle.Bold),
    '        .Enabled = False
    '    }
    '    AddHandler btnStart.Click, AddressOf BtnStart_Click

    '    btnClose = New Button With {
    '        .Location = New Point(570, yPos),
    '        .Size = New Size(100, 35),
    '        .Text = "Close",
    '        .BackColor = Color.FromArgb(240, 128, 128),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 10, FontStyle.Bold)
    '    }
    '    AddHandler btnClose.Click, Sub() Me.Close()

    '    Me.Controls.AddRange({btnStart, btnClose})

    '    ' Initial log message
    '    LogMessage("🔧 Advanced Signing Operations ready. Select an ELF file to begin.", Color.Cyan)
    'End Sub
    Private Sub InitializeComponent()

        Me.Text = "Advanced Operations - ELF Signing"
        Me.MinimumSize = New Size(720, 720)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Icon = Form1.Icon

        ' ===== ROOT =====
        Dim root As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 6,
        .Padding = New Padding(12)
    }

        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' title
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' mode
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' files
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' signing
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' log
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' buttons

        Me.Controls.Add(root)

        ' ===== TITLE =====
        Dim lblTitle As New Label With {
        .Text = "🔧 Advanced Signing Operations",
        .Font = New Font("Segoe UI", 14, FontStyle.Bold),
        .AutoSize = True,
        .ForeColor = Color.DarkBlue
    }

        root.Controls.Add(lblTitle)

        ' ===== MODE GROUP =====
        grpOperationMode = BuildModeGroup()
        root.Controls.Add(grpOperationMode)

        ' ===== FILE GROUP =====
        grpFileSelection = BuildFileGroup()
        root.Controls.Add(grpFileSelection)

        ' ===== SIGNING GROUP =====
        grpSigningSettings = BuildSigningGroup()
        root.Controls.Add(grpSigningSettings)

        ' ===== PROGRESS GROUP =====
        grpProgress = BuildProgressGroup()
        root.Controls.Add(grpProgress)

        ' ===== BUTTON BAR =====
        Dim buttonFlow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.RightToLeft,
        .AutoSize = True
    }

        btnStart = New Button With {.Text = "▶️ Start", .Width = 120, .Height = 36, .Enabled = False}
        btnClose = New Button With {.Text = "Close", .Width = 100, .Height = 36}

        AddHandler btnStart.Click, AddressOf BtnStart_Click
        AddHandler btnClose.Click, Sub() Me.Close()

        buttonFlow.Controls.AddRange({btnStart, btnClose})
        root.Controls.Add(buttonFlow)

        LogMessage("🔧 Advanced Signing Operations ready. Select an ELF file to begin.", Color.Cyan)

    End Sub

    Private Function BuildModeGroup() As GroupBox

        grpOperationMode = New GroupBox With {.Text = "Operation Mode", .AutoSize = True}

        Dim flow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .AutoSize = True}

        rdoSignOnly = New RadioButton With {.Text = "🟢 Sign Only - Sign ELF → SELF", .Checked = True, .AutoSize = True}
        rdoDecryptSign = New RadioButton With {.Text = "🟣 Decrypt Only - SELF → ELF", .AutoSize = True}
        rdoFullWorkflow = New RadioButton With {.Text = "🔵 Full Workflow - Decrypt + Re-Sign", .AutoSize = True}

        AddHandler rdoSignOnly.CheckedChanged, AddressOf OperationMode_Changed
        AddHandler rdoDecryptSign.CheckedChanged, AddressOf OperationMode_Changed
        AddHandler rdoFullWorkflow.CheckedChanged, AddressOf OperationMode_Changed

        flow.Controls.AddRange({rdoSignOnly, rdoDecryptSign, rdoFullWorkflow})
        grpOperationMode.Controls.Add(flow)

        Return grpOperationMode

    End Function

    Private Function BuildFileGroup() As GroupBox

        grpFileSelection = New GroupBox With {.Text = "File Selection", .AutoSize = True}

        Dim grid As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 3,
        .AutoSize = True
    }

        grid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        lblInputFile = New Label With {.Text = "Input File:", .AutoSize = True}
        txtInputFile = New TextBox With {.Dock = DockStyle.Fill}
        btnBrowseInput = New Button With {.Text = "Browse...", .Width = 100}

        lblOutputFile = New Label With {.Text = "Output File:", .AutoSize = True}
        txtOutputFile = New TextBox With {.Dock = DockStyle.Fill}
        btnBrowseOutput = New Button With {.Text = "Browse...", .Width = 100}

        lblFileInfo = New Label With {.Text = "No file selected", .AutoSize = True, .ForeColor = Color.Gray}

        AddHandler txtInputFile.TextChanged, AddressOf InputFile_Changed
        AddHandler txtOutputFile.TextChanged, AddressOf ValidateInputs
        AddHandler btnBrowseInput.Click, AddressOf BtnBrowseInput_Click
        AddHandler btnBrowseOutput.Click, AddressOf BtnBrowseOutput_Click

        grid.Controls.Add(lblInputFile, 0, 0)
        grid.Controls.Add(txtInputFile, 1, 0)
        grid.Controls.Add(btnBrowseInput, 2, 0)

        grid.Controls.Add(lblOutputFile, 0, 1)
        grid.Controls.Add(txtOutputFile, 1, 1)
        grid.Controls.Add(btnBrowseOutput, 2, 1)

        grid.Controls.Add(lblFileInfo, 1, 2)

        grpFileSelection.Controls.Add(grid)
        Return grpFileSelection

    End Function

    Private Function BuildSigningGroup() As GroupBox

        grpSigningSettings = New GroupBox With {.Text = "Signing Settings", .AutoSize = True}

        Dim flow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        lblSigningType = New Label With {.Text = "Signing Type:", .AutoSize = True}

        cmbSigningType = New ComboBox With {.Width = 260, .DropDownStyle = ComboBoxStyle.DropDownList}
        cmbSigningType.Items.AddRange({
        "Free Fake Sign (Homebrew)",
        "NPDRM (Content ID)",
        "Custom Keys (Advanced)"
    })
        cmbSigningType.SelectedIndex = 0

        btnAdvancedOptions = New Button With {.Text = "⚙️ Advanced Options..."}

        AddHandler btnAdvancedOptions.Click, AddressOf BtnAdvancedOptions_Click

        flow.Controls.AddRange({lblSigningType, cmbSigningType, btnAdvancedOptions})
        grpSigningSettings.Controls.Add(flow)

        Return grpSigningSettings

    End Function

    Private Function BuildProgressGroup() As GroupBox

        grpProgress = New GroupBox With {.Text = "Progress & Log", .Dock = DockStyle.Fill}

        Dim grid As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 2}
        grid.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        grid.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        rtbLog = New RichTextBox With {
        .Dock = DockStyle.Fill,
        .ReadOnly = True,
        .BackColor = Color.FromArgb(30, 30, 30),
        .ForeColor = Color.LightGreen,
        .Font = New Font("Consolas", 9)
    }

        progressBar = New ProgressBar With {
        .Dock = DockStyle.Fill,
        .Height = 24
    }

        grid.Controls.Add(rtbLog, 0, 0)
        grid.Controls.Add(progressBar, 0, 1)

        grpProgress.Controls.Add(grid)
        Return grpProgress

    End Function

    Private Sub OperationMode_Changed(sender As Object, e As EventArgs)
        ' Update UI based on selected mode
        If rdoSignOnly.Checked Then
            lblInputFile.Text = "Input File:"
            txtInputFile.PlaceholderText = "Select ELF file to sign..."
            lblOutputFile.Text = "Output File:"
            txtOutputFile.PlaceholderText = "Output SELF file path..."
            grpSigningSettings.Visible = True
        ElseIf rdoDecryptSign.Checked Then
            lblInputFile.Text = "SELF/BIN File:"
            txtInputFile.PlaceholderText = "Select SELF/BIN file to decrypt..."
            lblOutputFile.Text = "Output ELF:"
            grpSigningSettings.Visible = True

        End If

        ' Clear file paths when switching modes
        txtInputFile.Clear()
        txtOutputFile.Clear()
        lblFileInfo.Text = "No file selected"
    End Sub

    Private Sub BtnBrowseInput_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ' Adjust filter based on operation mode
            If rdoDecryptSign.Checked OrElse rdoFullWorkflow.Checked Then
                ofd.Title = "Select SELF/BIN File"
                ofd.Filter = "SELF/BIN Files|*.bin;*.self;eboot.bin|All Files|*.*"
            Else
                ofd.Title = "Select ELF File"
                ofd.Filter = "ELF Files|*.elf;eboot.bin;*.prx;*.sprx|All Files|*.*"
            End If

            If ofd.ShowDialog() = DialogResult.OK Then
                txtInputFile.Text = ofd.FileName

                ' Auto-suggest output filename based on mode
                If String.IsNullOrEmpty(txtOutputFile.Text) Then
                    Dim baseName = Path.GetFileNameWithoutExtension(ofd.FileName)
                    Dim outputName As String

                    If rdoDecryptSign.Checked Then
                        ' Decrypt mode: output .elf
                        outputName = baseName & ".elf"
                    ElseIf rdoFullWorkflow.Checked Then
                        ' Full workflow: output _signed.bin
                        outputName = baseName & "_signed.bin"
                    Else
                        ' Sign only: output _signed.bin
                        outputName = baseName & "_signed.bin"
                    End If

                    txtOutputFile.Text = Path.Combine(Path.GetDirectoryName(ofd.FileName), outputName)
                End If
            End If
        End Using
    End Sub

    Private Sub BtnBrowseOutput_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog()
            ' Adjust filter and default based on operation mode
            If rdoDecryptSign.Checked Then
                sfd.Title = "Save ELF File As"
                sfd.Filter = "ELF Files|*.elf|All Files|*.*"
                sfd.DefaultExt = "elf"
            Else
                sfd.Title = "Save Signed File As"
                sfd.Filter = "SELF Files|*.bin;*.self|All Files|*.*"
                sfd.DefaultExt = "bin"
            End If

            If Not String.IsNullOrEmpty(txtInputFile.Text) Then
                sfd.InitialDirectory = Path.GetDirectoryName(txtInputFile.Text)

                Dim baseName = Path.GetFileNameWithoutExtension(txtInputFile.Text)
                If rdoDecryptSign.Checked Then
                    sfd.FileName = baseName & ".elf"
                Else
                    sfd.FileName = baseName & "_signed.bin"
                End If
            End If

            If sfd.ShowDialog() = DialogResult.OK Then
                txtOutputFile.Text = sfd.FileName
            End If
        End Using
    End Sub

    Private Sub InputFile_Changed(sender As Object, e As EventArgs)
        ValidateInputs()

        If Not String.IsNullOrEmpty(txtInputFile.Text) AndAlso File.Exists(txtInputFile.Text) Then
            ' Show file info
            Dim fileType = SigningService.GetFileTypeDescription(txtInputFile.Text)
            Dim fileSize = New FileInfo(txtInputFile.Text).Length
            lblFileInfo.Text = $"Type: {fileType} | Size: {FormatFileSize(fileSize)}"

            ' Recommend signing type
            Dim recommended = SigningService.GetRecommendedSigningType(txtInputFile.Text)
            cmbSigningType.SelectedIndex = CInt(recommended)

            LogMessage($"📄 File selected: {Path.GetFileName(txtInputFile.Text)}", Color.White)
            LogMessage($"   Type: {fileType} | Size: {FormatFileSize(fileSize)}", Color.LightGray)
        Else
            lblFileInfo.Text = "No file selected"
        End If
    End Sub

    Private Function ValidateInputs() As Boolean
        ' Enable Start button only if inputs are valid
        Dim isValid = Not String.IsNullOrWhiteSpace(txtInputFile.Text) AndAlso
                     File.Exists(txtInputFile.Text) AndAlso
                     Not String.IsNullOrWhiteSpace(txtOutputFile.Text)

        btnStart.Enabled = isValid
        Return isValid
    End Function

    Private Sub BtnAdvancedOptions_Click(sender As Object, e As EventArgs)
        Using dlg As New SigningOptionsDialog(txtInputFile.Text)
            ' Pre-populate current settings
            dlg.SigningOpts = _signingOptions

            If dlg.ShowDialog() = DialogResult.OK Then
                _signingOptions = dlg.SigningOpts
                _selectedSigningType = dlg.SelectedSigningType

                ' Update combo to match
                cmbSigningType.SelectedIndex = CInt(_selectedSigningType)

                LogMessage("✓ Advanced options configured", Color.LightGreen)
            End If
        End Using
    End Sub

    Private Async Sub BtnStart_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtInputFile.Text) Then
            MessageBox.Show("Please select an input file.", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrWhiteSpace(txtOutputFile.Text) Then
            MessageBox.Show("Please specify an output file.", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Disable UI during operation
        SetUIEnabled(False)
        progressBar.Value = 0
        rtbLog.Clear()

        Try
            ' Route to appropriate operation based on mode
            If rdoDecryptSign.Checked Then
                Await PerformDecryptOperation()
            ElseIf rdoFullWorkflow.Checked Then
                Await PerformFullWorkflowOperation()
            Else ' rdoSignOnly.Checked
                Await PerformSignOnlyOperation()
            End If
        Catch ex As Exception
            LogMessage($"✗ Error: {ex.Message}", Color.Red)
            MessageBox.Show($"Error during operation:" & vbCrLf & vbCrLf & ex.Message,
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            SetUIEnabled(True)
        End Try
    End Sub

    ''' <summary>
    ''' Perform Sign Only operation
    ''' </summary>
    Private Async Function PerformSignOnlyOperation() As Task
        LogMessage("🚀 Starting signing operation...", Color.Cyan)
        progressBar.Value = 10

        ' Get signing type from combo
        Dim signingType As SigningService.SigningType = CType(cmbSigningType.SelectedIndex, SigningService.SigningType)

        LogMessage($"   Signing Type: {cmbSigningType.SelectedItem}", Color.White)
        LogMessage($"   Input: {Path.GetFileName(txtInputFile.Text)}", Color.White)
        LogMessage($"   Output: {Path.GetFileName(txtOutputFile.Text)}", Color.White)
        progressBar.Value = 20

        ' Perform signing
        Await Task.Run(Sub()
                           Dim result = SigningService.SignElf(
                               txtInputFile.Text,
                               txtOutputFile.Text,
                               signingType,
                               _signingOptions
                           )

                           Me.Invoke(Sub()
                                         progressBar.Value = 80

                                         If result.Success Then
                                             LogMessage($"✓ Signing completed successfully!", Color.LightGreen)
                                             LogMessage($"   Output: {result.OutputPath}", Color.White)
                                             LogMessage($"   Size: {FormatFileSize(result.FileSize)}", Color.White)
                                             LogMessage($"   Time: {result.ElapsedMs} ms", Color.White)
                                             progressBar.Value = 100

                                             MessageBox.Show($"Signing completed successfully!" & vbCrLf & vbCrLf &
                                                           $"Output: {Path.GetFileName(result.OutputPath)}" & vbCrLf &
                                                           $"Size: {FormatFileSize(result.FileSize)}" & vbCrLf &
                                                           $"Time: {result.ElapsedMs} ms",
                                                           "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                         Else
                                             LogMessage($"✗ Signing failed: {result.Message}", Color.Red)
                                             progressBar.Value = 0

                                             MessageBox.Show($"Signing failed:" & vbCrLf & vbCrLf & result.Message,
                                                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                         End If
                                     End Sub)
                       End Sub)
    End Function

    ''' <summary>
    ''' Perform Decrypt Only operation
    ''' </summary>
    Private Async Function PerformDecryptOperation() As Task
        LogMessage("🚀 Starting decrypt operation...", Color.Cyan)
        progressBar.Value = 10

        ' Validate input is a SELF file
        If Not SigningService.IsSelfFile(txtInputFile.Text) Then
            LogMessage($"✗ Input file is not a valid SELF/BIN file", Color.Red)
            MessageBox.Show("The input file does not appear to be a valid SELF/BIN file." & vbCrLf &
                          "Please select a valid SELF (Signed ELF) file.",
                          "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error)
            progressBar.Value = 0
            Return
        End If

        LogMessage($"   Input: {Path.GetFileName(txtInputFile.Text)}", Color.White)
        LogMessage($"   Output: {Path.GetFileName(txtOutputFile.Text)}", Color.White)
        progressBar.Value = 20

        Dim sw As New Stopwatch()
        sw.Start()

        ' Perform decryption
        Await Task.Run(Sub()
                           Me.Invoke(Sub()
                                         LogMessage($"🔓 Decrypting SELF file...", Color.Blue)
                                     End Sub)

                           Dim success = selfutilmodule.unpackfile(txtInputFile.Text, txtOutputFile.Text)

                           Me.Invoke(Sub()
                                         progressBar.Value = 80

                                         If success Then
                                             sw.Stop()

                                             Dim fileSize As Long = 0
                                             If File.Exists(txtOutputFile.Text) Then
                                                 fileSize = New FileInfo(txtOutputFile.Text).Length
                                             End If

                                             LogMessage($"✓ Decryption completed successfully!", Color.LightGreen)
                                             LogMessage($"   Output: {txtOutputFile.Text}", Color.White)
                                             LogMessage($"   Size: {FormatFileSize(fileSize)}", Color.White)
                                             LogMessage($"   Time: {sw.ElapsedMilliseconds} ms", Color.White)
                                             progressBar.Value = 100

                                             MessageBox.Show($"Decryption completed successfully!" & vbCrLf & vbCrLf &
                                                           $"Output ELF: {Path.GetFileName(txtOutputFile.Text)}" & vbCrLf &
                                                           $"Size: {FormatFileSize(fileSize)}" & vbCrLf &
                                                           $"Time: {sw.ElapsedMilliseconds} ms",
                                                           "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                         Else
                                             LogMessage($"✗ Decryption failed", Color.Red)
                                             progressBar.Value = 0

                                             MessageBox.Show($"Decryption failed. Check the log for details.",
                                                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                         End If
                                     End Sub)
                       End Sub)
    End Function

    ''' <summary>
    ''' Perform Full Workflow operation (Decrypt + Sign)
    ''' </summary>
    Private Async Function PerformFullWorkflowOperation() As Task

        LogMessage("🚀 Starting full workflow (Decrypt + Sign)...", Color.Cyan)
        progressBar.Value = 5

        ' Validate input is a SELF file
        If Not SigningService.IsSelfFile(txtInputFile.Text) Then
            LogMessage($"✗ Input file is not a valid SELF/BIN file", Color.Red)
            MessageBox.Show("The input file does not appear to be a valid SELF/BIN file." & vbCrLf &
                          "Please select a valid SELF (Signed ELF) file.",
                          "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error)
            progressBar.Value = 0
            Return
        End If

        ' Create workflow options
        Dim workflowOpts As New DecryptSignService.WorkflowOptions With {
            .SigningType = CType(cmbSigningType.SelectedIndex, SigningService.SigningType),
            .SigningOptions = _signingOptions,
            .KeepIntermediateFiles = False,
            .CreateBackup = True
        }

        ' Validate workflow inputs
        Dim validationError = DecryptSignService.ValidateWorkflowInputs(txtInputFile.Text, txtOutputFile.Text, workflowOpts)
        If Not String.IsNullOrEmpty(validationError) Then
            LogMessage($"✗ Validation failed: {validationError}", Color.Red)
            MessageBox.Show($"Validation error:" & vbCrLf & vbCrLf & validationError,
                          "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            progressBar.Value = 0
            Return
        End If

        LogMessage($"   Signing Type: {cmbSigningType.SelectedItem}", Color.White)
        LogMessage($"   Input: {Path.GetFileName(txtInputFile.Text)}", Color.White)
        LogMessage($"   Output: {Path.GetFileName(txtOutputFile.Text)}", Color.White)

        ' Execute workflow with progress callback
        Await Task.Run(Sub()
                           Dim result = DecryptSignService.ExecuteDecryptSignWorkflow(
                               txtInputFile.Text,
                               txtOutputFile.Text,
                               workflowOpts,
                               Sub(progress As Integer, message As String)
                                   Me.Invoke(Sub()
                                                 progressBar.Value = progress
                                                 If Not String.IsNullOrEmpty(message) Then
                                                     LogMessage($"   {message}", Color.LightBlue)
                                                 End If
                                             End Sub)
                               End Sub
                           )

                           Me.Invoke(Sub()
                                         If result.Success Then
                                             LogMessage($"✓ Full workflow completed successfully!", Color.LightGreen)
                                             LogMessage($"   Output: {result.OutputPath}", Color.White)
                                             LogMessage($"   Size: {FormatFileSize(result.FileSize)}", Color.White)
                                             LogMessage($"   Time: {result.ElapsedMs} ms", Color.White)

                                             If Not String.IsNullOrEmpty(result.IntermediateElfPath) AndAlso File.Exists(result.IntermediateElfPath) Then
                                                 LogMessage($"   Intermediate ELF kept: {Path.GetFileName(result.IntermediateElfPath)}", Color.DarkGray)
                                             End If

                                             progressBar.Value = 100

                                             Dim successMsg = $"Full workflow completed successfully!" & vbCrLf & vbCrLf &
                                                            $"Steps:" & vbCrLf &
                                                            $"  1. Decrypted SELF to ELF ✓" & vbCrLf &
                                                            $"  2. Processed ELF ✓" & vbCrLf &
                                                            $"  3. Signed ELF to SELF ✓" & vbCrLf & vbCrLf &
                                                            $"Output: {Path.GetFileName(result.OutputPath)}" & vbCrLf &
                                                            $"Size: {FormatFileSize(result.FileSize)}" & vbCrLf &
                                                            $"Time: {result.ElapsedMs} ms"

                                             MessageBox.Show(successMsg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                         Else
                                             LogMessage($"✗ Workflow failed: {result.Message}", Color.Red)

                                             If result.StepsFailed.Count > 0 Then
                                                 LogMessage($"   Failed steps: {String.Join(", ", result.StepsFailed)}", Color.Red)
                                             End If

                                             progressBar.Value = 0

                                             Dim errorMsg = $"Workflow failed:" & vbCrLf & vbCrLf & result.Message

                                             If result.StepsFailed.Count > 0 Then
                                                 errorMsg &= vbCrLf & vbCrLf & $"Failed steps: {String.Join(", ", result.StepsFailed)}"
                                             End If

                                             MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                         End If
                                     End Sub)
                       End Sub)

    End Function

    Private Sub SetUIEnabled(enabled As Boolean)
        grpOperationMode.Enabled = enabled
        grpFileSelection.Enabled = enabled
        grpSigningSettings.Enabled = enabled
        btnStart.Enabled = enabled AndAlso ValidateInputs()
    End Sub

    Private Sub LogMessage(message As String, Optional color As Color = Nothing)
        If color = Nothing Then color = Color.White

        If rtbLog.InvokeRequired Then
            rtbLog.Invoke(Sub() LogMessage(message, color))
        Else
            rtbLog.SelectionStart = rtbLog.TextLength
            rtbLog.SelectionLength = 0
            rtbLog.SelectionColor = color
            rtbLog.AppendText(message & vbCrLf)
            rtbLog.SelectionColor = rtbLog.ForeColor
            rtbLog.ScrollToCaret()
        End If
    End Sub

    Private Function FormatFileSize(bytes As Long) As String
        If bytes < 1024 Then
            Return $"{bytes} bytes"
        ElseIf bytes < 1024 * 1024 Then
            Return $"{bytes / 1024.0:F2} KB"
        Else
            Return $"{bytes / (1024.0 * 1024.0):F2} MB"
        End If
    End Function

End Class