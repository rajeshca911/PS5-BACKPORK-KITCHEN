Imports System.Windows.Forms
Imports System.Drawing
Imports System.IO

''' <summary>
''' Dialog form for adding or editing payload information
''' </summary>
Public Class PayloadEditForm
    Inherits Form

    ' UI Controls
    Private WithEvents txtName As TextBox

    Private WithEvents txtDescription As TextBox
    Private WithEvents cmbCategory As ComboBox
    Private WithEvents txtLocalPath As TextBox
    Private WithEvents btnBrowseLocal As Button
    Private WithEvents txtTargetPath As TextBox
    Private WithEvents txtVersion As TextBox
    Private WithEvents txtAuthor As TextBox
    Private WithEvents txtTags As TextBox
    Private WithEvents chkActive As CheckBox
    Private WithEvents btnOK As Button
    Private WithEvents btnCancel As Button
    Private lblFileSize As Label

    ' Data
    Private _payloadInfo As PayloadInfo

    Private isEditMode As Boolean

    ''' <summary>
    ''' Get the payload information after dialog closes
    ''' </summary>
    Public ReadOnly Property PayloadInfo As PayloadInfo
        Get
            Return _payloadInfo
        End Get
    End Property

    ''' <summary>
    ''' Create new payload
    ''' </summary>
    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)

        isEditMode = False
        _payloadInfo = New PayloadInfo With {
            .IsActive = True,
            .Category = "Custom"
        }
        InitializeComponent()
        PopulateFields()
    End Sub

    ''' <summary>
    ''' Edit existing payload
    ''' </summary>
    Public Sub New(payload As PayloadInfo)
        isEditMode = True
        _payloadInfo = payload
        InitializeComponent()
        PopulateFields()
    End Sub

    Private Sub InitializeComponent()

        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.Text = If(isEditMode, "Edit Payload", "Add New Payload")
        Me.MinimumSize = New Size(640, 560)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog

        Dim root As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 2,
        .Padding = New Padding(12),
        .AutoScroll = True
    }

        root.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

        Me.Controls.Add(root)

        Dim row As Integer = 0

        ' ===== Payload File Row =====
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.Controls.Add(New Label With {.Text = "Payload File:", .AutoSize = True, .Font = BoldFont()}, 0, row)

        Dim fileRow As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}

        txtLocalPath = New TextBox With {.Width = 340, .ReadOnly = True}
        btnBrowseLocal = New Button With {.Text = "Browse...", .Width = 100}
        lblFileSize = New Label With {.AutoSize = True, .ForeColor = Color.Gray}

        fileRow.Controls.AddRange({txtLocalPath, btnBrowseLocal, lblFileSize})
        root.Controls.Add(fileRow, 1, row)

        row += 1

        ' ===== Standard Rows =====
        txtName = New TextBox()
        AddRow(root, row, "Payload Name:", txtName) : row += 1

        txtDescription = New TextBox With {.Multiline = True, .Height = 70}
        AddRow(root, row, "Description:", txtDescription) : row += 1

        cmbCategory = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList}
        cmbCategory.Items.AddRange({"Jailbreak", "Homebrew", "Debug", "Backup", "Custom"})
        AddRow(root, row, "Category:", cmbCategory) : row += 1

        txtVersion = New TextBox()
        AddRow(root, row, "Version:", txtVersion) : row += 1

        txtAuthor = New TextBox()
        AddRow(root, row, "Author:", txtAuthor) : row += 1

        txtTags = New TextBox()
        AddRow(root, row, "Tags:", txtTags) : row += 1

        ' ===== Tags Hint =====
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.Controls.Add(New Label(), 0, row)
        root.Controls.Add(New Label With {
        .Text = "Separate tags with commas (goldhen, debug, utility)",
        .AutoSize = True,
        .ForeColor = Color.Gray
    }, 1, row)
        row += 1

        ' Hidden target
        txtTargetPath = New TextBox With {.Visible = False}
        root.Controls.Add(txtTargetPath, 1, row)
        row += 1

        ' Active checkbox
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        chkActive = New CheckBox With {.Text = "Active (visible in library)", .AutoSize = True, .Checked = True}
        root.Controls.Add(New Label(), 0, row)
        root.Controls.Add(chkActive, 1, row)
        row += 1

        ' ===== Buttons =====
        Dim buttonFlow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.RightToLeft,
        .AutoSize = True
    }

        btnOK = New Button With {.Text = "Save", .Width = 100, .Height = 36}
        btnCancel = New Button With {.Text = "Cancel", .Width = 100, .Height = 36}

        btnOK.DialogResult = DialogResult.OK
        btnCancel.DialogResult = DialogResult.Cancel

        buttonFlow.Controls.AddRange({btnOK, btnCancel})

        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.Controls.Add(New Label(), 0, row)
        root.Controls.Add(buttonFlow, 1, row)

        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel

    End Sub

    Private Sub AddRow(layout As TableLayoutPanel, row As Integer, labelText As String, ctrl As Control)

        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lbl As New Label With {
        .Text = labelText,
        .AutoSize = True,
        .Font = BoldFont(),
        .Anchor = AnchorStyles.Left
    }

        ctrl.Dock = DockStyle.Fill

        layout.Controls.Add(lbl, 0, row)
        layout.Controls.Add(ctrl, 1, row)

    End Sub

    Private Function BoldFont() As Font
        Return New Font("Segoe UI", 10, FontStyle.Bold)
    End Function

    Private Sub AddRow(layout As TableLayoutPanel, labelText As String, ctrl As Control)
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lbl As New Label With {
        .Text = labelText,
        .AutoSize = True,
        .Font = BoldFont(),
        .Anchor = AnchorStyles.Left
    }

        ctrl.Dock = DockStyle.Fill

        layout.Controls.Add(lbl, 0, layout.RowCount)
        layout.Controls.Add(ctrl, 1, layout.RowCount - 1)
    End Sub

    Private Sub AddLabel(text As String, x As Integer, y As Integer)
        Dim lbl As New Label With {
            .Text = text,
            .Location = New Point(x, y + 3),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        Me.Controls.Add(lbl)
    End Sub

    Private Sub PopulateFields()
        If _payloadInfo IsNot Nothing Then
            txtName.Text = _payloadInfo.Name
            txtDescription.Text = _payloadInfo.Description
            cmbCategory.SelectedItem = _payloadInfo.Category
            If cmbCategory.SelectedIndex = -1 Then
                cmbCategory.SelectedIndex = 4 ' Default to "Custom"
            End If
            txtLocalPath.Text = _payloadInfo.LocalPath
            txtTargetPath.Text = _payloadInfo.TargetPath
            txtVersion.Text = _payloadInfo.Version
            txtAuthor.Text = _payloadInfo.Author
            txtTags.Text = If(_payloadInfo.Tags?.Count > 0, String.Join(", ", _payloadInfo.Tags), "")
            chkActive.Checked = _payloadInfo.IsActive

            UpdateFileSize()
        Else
            cmbCategory.SelectedIndex = 4 ' Default to "Custom"
        End If
    End Sub

    Private Sub UpdateFileSize()
        If Not String.IsNullOrEmpty(txtLocalPath.Text) AndAlso File.Exists(txtLocalPath.Text) Then
            Dim fileInfo As New FileInfo(txtLocalPath.Text)
            lblFileSize.Text = $"Size: {FtpManager.FormatFileSize(fileInfo.Length)}"
            lblFileSize.ForeColor = Color.DarkGreen
        Else
            lblFileSize.Text = "File not found"
            lblFileSize.ForeColor = Color.Red
        End If
    End Sub

    Private Sub BtnBrowseLocal_Click(sender As Object, e As EventArgs) Handles btnBrowseLocal.Click
        Using ofd As New OpenFileDialog With {
            .Title = "Select Payload File",
            .Filter = "All Files (*.*)|*.*|Binary Files (*.bin)|*.bin|ELF Files (*.elf)|*.elf",
            .CheckFileExists = True
        }
            If ofd.ShowDialog() = DialogResult.OK Then
                txtLocalPath.Text = ofd.FileName

                ' Auto-fill name if empty
                If String.IsNullOrWhiteSpace(txtName.Text) Then
                    txtName.Text = Path.GetFileNameWithoutExtension(ofd.FileName)
                End If

                ' Auto-fill target path with filename
                If txtTargetPath.Text.EndsWith("/") Then
                    txtTargetPath.Text &= Path.GetFileName(ofd.FileName)
                End If

                UpdateFileSize()
            End If
        End Using
    End Sub

    Private Sub BtnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        ' Validate
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show("Please enter a payload name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Me.DialogResult = DialogResult.None
            Return
        End If

        If String.IsNullOrWhiteSpace(txtLocalPath.Text) Then
            MessageBox.Show("Please select a local file.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            btnBrowseLocal.Focus()
            Me.DialogResult = DialogResult.None
            Return
        End If

        If Not File.Exists(txtLocalPath.Text) Then
            Dim result = MessageBox.Show(
                "The selected file does not exist. Save anyway?",
                "File Not Found",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            )
            If result <> DialogResult.Yes Then
                Me.DialogResult = DialogResult.None
                Return
            End If
        End If
        txtTargetPath.Text = txtLocalPath.Text.Trim
        If String.IsNullOrWhiteSpace(txtTargetPath.Text) Then
            MessageBox.Show("Please enter a target path on PS5.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtTargetPath.Focus()
            Me.DialogResult = DialogResult.None
            Return
        End If

        ' Save data
        _payloadInfo.Name = txtName.Text.Trim()
        _payloadInfo.Description = txtDescription.Text.Trim()
        _payloadInfo.Category = cmbCategory.SelectedItem.ToString()
        _payloadInfo.LocalPath = txtLocalPath.Text
        _payloadInfo.TargetPath = txtTargetPath.Text.Trim()
        _payloadInfo.Version = txtVersion.Text.Trim()
        _payloadInfo.Author = txtAuthor.Text.Trim()
        _payloadInfo.IsActive = chkActive.Checked

        ' Parse tags
        _payloadInfo.Tags.Clear()
        If Not String.IsNullOrWhiteSpace(txtTags.Text) Then
            Dim tags = txtTags.Text.Split(","c).Select(Function(t) t.Trim()).Where(Function(t) Not String.IsNullOrEmpty(t))
            _payloadInfo.Tags.AddRange(tags)
        End If

        ' Get file size
        If File.Exists(_payloadInfo.LocalPath) Then
            Dim fileInfo As New FileInfo(_payloadInfo.LocalPath)
            _payloadInfo.FileSize = fileInfo.Length
        End If
    End Sub

End Class