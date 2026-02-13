Imports System.Windows.Forms
Imports System.IO
Imports System.ComponentModel

''' <summary>
''' Dialog for configuring signing options
''' Allows selection of signing type and related parameters
''' </summary>
Public Class SigningOptionsDialog
    Inherits Form

    ' UI Controls
    Private grpSigningType As GroupBox

    Private rdoFreeFakeSign As RadioButton
    Private rdoNpdrm As RadioButton
    Private rdoCustomKeys As RadioButton

    Private grpOptions As GroupBox
    Private lblContentId As Label
    Private txtContentId As TextBox
    Private lblContentIdHelp As Label

    Private lblPaid As Label
    Private txtPaid As TextBox
    Private lblPaidHelp As Label

    Private lblPType As Label
    Private cmbPType As ComboBox

    Private lblAppVersion As Label
    Private txtAppVersion As TextBox

    Private lblFwVersion As Label
    Private txtFwVersion As TextBox

    Private chkCreateBackup As CheckBox
    Private chkOverwrite As CheckBox

    Private btnOK As Button
    Private btnCancel As Button

    Private lblFileInfo As Label

    ' Properties
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property SelectedSigningType As SigningService.SigningType = SigningService.SigningType.FreeFakeSign

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property SigningOpts As SigningService.SigningOptions

    Private _inputFilePath As String

    Public Sub New(Optional inputFilePath As String = "")
        InitializeComponent()
        _inputFilePath = inputFilePath
        SigningOpts = New SigningService.SigningOptions()
        LoadDefaults()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Signing Options"
        Me.Size = New Size(550, 600)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent

        Dim yPos = 15

        ' File info label
        lblFileInfo = New Label With {
            .Location = New Point(15, yPos),
            .Size = New Size(510, 40),
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.DarkBlue,
            .Text = "Select signing type and configure options"
        }
        Me.Controls.Add(lblFileInfo)
        yPos += 50

        ' Signing Type Group
        grpSigningType = New GroupBox With {
            .Location = New Point(15, yPos),
            .Size = New Size(510, 120),
            .Text = "Signing Type"
        }

        rdoFreeFakeSign = New RadioButton With {
            .Location = New Point(15, 25),
            .Size = New Size(470, 25),
            .Text = "🎮 Free Fake Sign (Unsigned SELF for Homebrew)",
            .Checked = True
        }
        AddHandler rdoFreeFakeSign.CheckedChanged, AddressOf SigningType_Changed

        rdoNpdrm = New RadioButton With {
            .Location = New Point(15, 55),
            .Size = New Size(470, 25),
            .Text = "🔐 NPDRM (with Content ID - for licensed content)"
        }
        AddHandler rdoNpdrm.CheckedChanged, AddressOf SigningType_Changed

        rdoCustomKeys = New RadioButton With {
            .Location = New Point(15, 85),
            .Size = New Size(470, 25),
            .Text = "⚙️ Custom Keys (Advanced - requires auth info)",
            .Enabled = False
        }
        AddHandler rdoCustomKeys.CheckedChanged, AddressOf SigningType_Changed

        grpSigningType.Controls.AddRange({rdoFreeFakeSign, rdoNpdrm, rdoCustomKeys})
        Me.Controls.Add(grpSigningType)
        yPos += 130

        ' Options Group
        grpOptions = New GroupBox With {
            .Location = New Point(15, yPos),
            .Size = New Size(510, 280),
            .Text = "Options"
        }

        Dim optYPos = 25

        ' Content ID (for NPDRM)
        lblContentId = New Label With {
            .Location = New Point(15, optYPos),
            .Size = New Size(100, 20),
            .Text = "Content ID:"
        }
        txtContentId = New TextBox With {
            .Location = New Point(120, optYPos),
            .Size = New Size(365, 23),
            .PlaceholderText = "UP0000-CUSA00000_00-GAMEID0000000000",
            .Enabled = False
        }
        lblContentIdHelp = New Label With {
            .Location = New Point(120, optYPos + 25),
            .Size = New Size(365, 35),
            .Font = New Font("Segoe UI", 8, FontStyle.Italic),
            .ForeColor = Color.Gray,
            .Text = "Required for NPDRM signing. Format: UP0000-CUSA00000_00-GAMEID0000000000"
        }
        optYPos += 65

        ' PAID
        lblPaid = New Label With {
            .Location = New Point(15, optYPos),
            .Size = New Size(100, 20),
            .Text = "PAID:"
        }
        txtPaid = New TextBox With {
            .Location = New Point(120, optYPos),
            .Size = New Size(200, 23),
            .Text = "3100000000000002"
        }
        lblPaidHelp = New Label With {
            .Location = New Point(330, optYPos),
            .Size = New Size(155, 40),
            .Font = New Font("Segoe UI", 8, FontStyle.Italic),
            .ForeColor = Color.Gray,
            .Text = "Program Auth ID (hex)"
        }
        optYPos += 50

        ' PType
        lblPType = New Label With {
            .Location = New Point(15, optYPos),
            .Size = New Size(100, 20),
            .Text = "Program Type:"
        }
        cmbPType = New ComboBox With {
            .Location = New Point(120, optYPos),
            .Size = New Size(200, 23),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbPType.Items.AddRange(New String() {
            "Fake (Homebrew)",
            "NPDRM Exec",
            "NPDRM Dynlib",
            "System Exec",
            "System Dynlib"
        })
        cmbPType.SelectedIndex = 0
        optYPos += 35

        ' App Version
        lblAppVersion = New Label With {
            .Location = New Point(15, optYPos),
            .Size = New Size(100, 20),
            .Text = "App Version:"
        }
        txtAppVersion = New TextBox With {
            .Location = New Point(120, optYPos),
            .Size = New Size(200, 23),
            .Text = "0",
            .PlaceholderText = "0"
        }
        optYPos += 35

        ' FW Version
        lblFwVersion = New Label With {
            .Location = New Point(15, optYPos),
            .Size = New Size(100, 20),
            .Text = "FW Version:"
        }
        txtFwVersion = New TextBox With {
            .Location = New Point(120, optYPos),
            .Size = New Size(200, 23),
            .Text = "0",
            .PlaceholderText = "0"
        }
        optYPos += 40

        ' Checkboxes
        chkCreateBackup = New CheckBox With {
            .Location = New Point(15, optYPos),
            .Size = New Size(200, 25),
            .Text = "Create backup before signing",
            .Checked = True
        }
        chkOverwrite = New CheckBox With {
            .Location = New Point(250, optYPos),
            .Size = New Size(230, 25),
            .Text = "Overwrite existing output file",
            .Checked = False
        }

        grpOptions.Controls.AddRange({
            lblContentId, txtContentId, lblContentIdHelp,
            lblPaid, txtPaid, lblPaidHelp,
            lblPType, cmbPType,
            lblAppVersion, txtAppVersion,
            lblFwVersion, txtFwVersion,
            chkCreateBackup, chkOverwrite
        })
        Me.Controls.Add(grpOptions)
        yPos += 290

        ' Buttons
        btnOK = New Button With {
            .Location = New Point(300, yPos),
            .Size = New Size(100, 30),
            .Text = "OK",
            .DialogResult = DialogResult.OK,
            .BackColor = Color.FromArgb(144, 238, 144),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnOK.Click, AddressOf BtnOK_Click

        btnCancel = New Button With {
            .Location = New Point(410, yPos),
            .Size = New Size(100, 30),
            .Text = "Cancel",
            .DialogResult = DialogResult.Cancel,
            .BackColor = Color.FromArgb(240, 128, 128),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }

        Me.Controls.AddRange({btnOK, btnCancel})
        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel
    End Sub

    Private Sub LoadDefaults()
        ' Load default values
        If Not String.IsNullOrEmpty(_inputFilePath) Then
            Dim fileType = SigningService.GetFileTypeDescription(_inputFilePath)
            lblFileInfo.Text = $"File: {Path.GetFileName(_inputFilePath)}" & vbCrLf & $"Type: {fileType}"

            ' Set recommended signing type
            Dim recommended = SigningService.GetRecommendedSigningType(_inputFilePath)
            Select Case recommended
                Case SigningService.SigningType.FreeFakeSign
                    rdoFreeFakeSign.Checked = True
                Case SigningService.SigningType.Npdrm
                    rdoNpdrm.Checked = True
            End Select
        End If
    End Sub

    Private Sub SigningType_Changed(sender As Object, e As EventArgs)
        ' Enable/disable Content ID based on NPDRM selection
        Dim isNpdrm = rdoNpdrm.Checked

        txtContentId.Enabled = isNpdrm
        lblContentId.Enabled = isNpdrm
        lblContentIdHelp.Enabled = isNpdrm

        ' Update PType combo based on selection
        If rdoFreeFakeSign.Checked Then
            cmbPType.SelectedIndex = 0 ' Fake
        ElseIf rdoNpdrm.Checked Then
            cmbPType.SelectedIndex = 1 ' NPDRM Exec
        End If
    End Sub

    Private Sub BtnOK_Click(sender As Object, e As EventArgs)
        ' Validate inputs
        If rdoNpdrm.Checked AndAlso String.IsNullOrWhiteSpace(txtContentId.Text) Then
            MessageBox.Show("Content ID is required for NPDRM signing.", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.DialogResult = DialogResult.None
            Return
        End If

        ' Build options
        SigningOpts = New SigningService.SigningOptions()

        ' Parse PAID
        Try
            SigningOpts.Paid = Convert.ToUInt64(txtPaid.Text.Trim(), 16)
        Catch
            SigningOpts.Paid = &H3100000000000002UL
        End Try

        ' Set PType based on combo selection
        Select Case cmbPType.SelectedIndex
            Case 0 ' Fake
                SigningOpts.PType = SignedElfExInfo.PTYPE_FAKE
            Case 1 ' NPDRM Exec
                SigningOpts.PType = SignedElfExInfo.PTYPE_NPDRM_EXEC
            Case 2 ' NPDRM Dynlib
                SigningOpts.PType = SignedElfExInfo.PTYPE_NPDRM_DYNLIB
            Case 3 ' System Exec
                SigningOpts.PType = SignedElfExInfo.PTYPE_SYSTEM_EXEC
            Case 4 ' System Dynlib
                SigningOpts.PType = SignedElfExInfo.PTYPE_SYSTEM_DYNLIB
            Case Else
                SigningOpts.PType = SignedElfExInfo.PTYPE_FAKE
        End Select

        ' Parse versions
        Try
            SigningOpts.AppVersion = Convert.ToUInt64(txtAppVersion.Text.Trim())
        Catch
            SigningOpts.AppVersion = 0
        End Try

        Try
            SigningOpts.FwVersion = Convert.ToUInt64(txtFwVersion.Text.Trim())
        Catch
            SigningOpts.FwVersion = 0
        End Try

        ' Content ID
        SigningOpts.ContentId = txtContentId.Text.Trim()

        ' Checkboxes
        SigningOpts.CreateBackup = chkCreateBackup.Checked
        SigningOpts.OverwriteOutput = chkOverwrite.Checked

        ' Set signing type
        If rdoFreeFakeSign.Checked Then
            SelectedSigningType = SigningService.SigningType.FreeFakeSign
        ElseIf rdoNpdrm.Checked Then
            SelectedSigningType = SigningService.SigningType.Npdrm
        ElseIf rdoCustomKeys.Checked Then
            SelectedSigningType = SigningService.SigningType.CustomKeys
        End If
    End Sub

End Class