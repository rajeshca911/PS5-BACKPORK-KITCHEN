Imports System.Windows.Forms
Imports System.Drawing

Public Class FtpConnectionForm
    Inherits Form

    ' UI Controls
    Private WithEvents cmbProfiles As ComboBox

    Private WithEvents btnNewProfile As Button
    Private WithEvents btnDeleteProfile As Button
    Private WithEvents txtProfileName As TextBox
    Private WithEvents txtHost As TextBox
    Private WithEvents txtPort As NumericUpDown
    Private WithEvents txtUsername As TextBox
    Private WithEvents txtPassword As TextBox
    Private WithEvents chkShowPassword As CheckBox
    Private WithEvents chkPassiveMode As CheckBox
    Private WithEvents chkDefaultProfile As CheckBox
    Private WithEvents txtTimeout As NumericUpDown
    Private WithEvents btnTestConnection As Button
    Private WithEvents btnConnect As Button
    Private WithEvents btnCancel As Button
    Private lblStatus As Label
    Private lblConnectionStatus As Label

    ' Data
    Private profiles As List(Of FtpManager.FtpProfile)

    Private currentProfile As FtpManager.FtpProfile
    Private isEditing As Boolean = False
    Private selectedProfile As FtpManager.FtpProfile

    Public Sub New()
        InitializeComponent()
        LoadProfiles()
        UpdateConnectionStatus()
        UpdateUI()
    End Sub

    Private Sub InitializeComponent()
        ' Form settings
        Me.Text = "PS5 FTP Connection Manager"
        Me.Size = New Size(600, 550)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Title Panel
        Dim pnlTitle As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = Color.FromArgb(45, 45, 48)
        }

        Dim lblTitle As New Label With {
            .Text = "📡 PS5 FTP Connection Manager",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(20, 18)
        }
        pnlTitle.Controls.Add(lblTitle)
        Me.Controls.Add(pnlTitle)

        ' Main Panel
        Dim pnlMain As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(20)
        }

        Dim yPos = 10

        ' Profile Selection
        Dim lblProfile As New Label With {
            .Text = "Connection Profile:",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblProfile)

        cmbProfiles = New ComboBox With {
            .Location = New Point(20, yPos + 25),
            .Width = 350,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        pnlMain.Controls.Add(cmbProfiles)

        btnNewProfile = New Button With {
            .Text = "➕ New",
            .Location = New Point(380, yPos + 23),
            .Width = 80,
            .Height = 28
        }
        pnlMain.Controls.Add(btnNewProfile)

        btnDeleteProfile = New Button With {
            .Text = "🗑️ Delete",
            .Location = New Point(470, yPos + 23),
            .Width = 80,
            .Height = 28,
            .ForeColor = Color.Red
        }
        pnlMain.Controls.Add(btnDeleteProfile)

        yPos += 70

        ' Profile Name
        Dim lblProfileName As New Label With {
            .Text = "Profile Name:",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblProfileName)

        txtProfileName = New TextBox With {
            .Location = New Point(140, yPos - 3),
            .Width = 410
        }
        pnlMain.Controls.Add(txtProfileName)

        yPos += 40

        ' Host
        Dim lblHost As New Label With {
            .Text = "PS5 IP Address:",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblHost)

        txtHost = New TextBox With {
            .Location = New Point(140, yPos - 3),
            .Width = 410,
            .PlaceholderText = "192.168.1.100"
        }
        pnlMain.Controls.Add(txtHost)

        yPos += 40

        ' Port
        Dim lblPort As New Label With {
            .Text = "Port:",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblPort)

        txtPort = New NumericUpDown With {
            .Location = New Point(140, yPos - 3),
            .Width = 100,
            .Minimum = 1,
            .Maximum = 65535,
            .Value = 2121
        }
        pnlMain.Controls.Add(txtPort)

        yPos += 40

        ' Username
        Dim lblUsername As New Label With {
            .Text = "Username:",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblUsername)

        txtUsername = New TextBox With {
            .Location = New Point(140, yPos - 3),
            .Width = 410,
            .Text = "anonymous"
        }
        pnlMain.Controls.Add(txtUsername)

        yPos += 40

        ' Password
        Dim lblPassword As New Label With {
            .Text = "Password:",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblPassword)

        txtPassword = New TextBox With {
            .Location = New Point(140, yPos - 3),
            .Width = 410,
            .UseSystemPasswordChar = True
        }
        pnlMain.Controls.Add(txtPassword)

        yPos += 40

        ' Show Password Checkbox
        chkShowPassword = New CheckBox With {
            .Text = "Show password",
            .Location = New Point(140, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(chkShowPassword)

        yPos += 35

        ' Timeout
        Dim lblTimeout As New Label With {
            .Text = "Timeout (sec):",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblTimeout)

        txtTimeout = New NumericUpDown With {
            .Location = New Point(140, yPos - 3),
            .Width = 100,
            .Minimum = 5,
            .Maximum = 120,
            .Value = 30
        }
        pnlMain.Controls.Add(txtTimeout)

        yPos += 40

        ' Passive Mode Checkbox
        chkPassiveMode = New CheckBox With {
            .Text = "Use Passive Mode (recommended)",
            .Location = New Point(20, yPos),
            .AutoSize = True,
            .Checked = True
        }
        pnlMain.Controls.Add(chkPassiveMode)

        yPos += 30

        ' Default Profile Checkbox
        chkDefaultProfile = New CheckBox With {
            .Text = "Set as default profile",
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(chkDefaultProfile)

        Me.Controls.Add(pnlMain)

        ' Bottom Panel
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 120,
            .BackColor = Color.FromArgb(240, 240, 240)
        }

        ' Status Label
        lblStatus = New Label With {
            .Text = "Status: Ready",
            .Location = New Point(20, 15),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9, FontStyle.Regular)
        }
        pnlBottom.Controls.Add(lblStatus)

        ' Connection Status
        lblConnectionStatus = New Label With {
            .Location = New Point(20, 40),
            .Size = New Size(540, 20),
            .ForeColor = Color.Gray
        }
        pnlBottom.Controls.Add(lblConnectionStatus)

        ' Buttons
        btnTestConnection = New Button With {
            .Text = "🔍 Test Connection",
            .Location = New Point(20, 75),
            .Width = 150,
            .Height = 35
        }
        pnlBottom.Controls.Add(btnTestConnection)

        btnConnect = New Button With {
            .Text = "🔌 Connect",
            .Location = New Point(320, 75),
            .Width = 110,
            .Height = 35,
            .BackColor = Color.FromArgb(0, 122, 204),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        pnlBottom.Controls.Add(btnConnect)

        btnCancel = New Button With {
            .Text = "❌ Cancel",
            .Location = New Point(440, 75),
            .Width = 110,
            .Height = 35
        }
        pnlBottom.Controls.Add(btnCancel)

        Me.Controls.Add(pnlBottom)
    End Sub

    Private Sub LoadProfiles()
        profiles = FtpManager.LoadProfiles()

        ' Add default profile if none exist
        If profiles.Count = 0 Then
            profiles.Add(New FtpManager.FtpProfile With {
                .Name = "My PS5",
                .Host = "192.168.1.100",
                .Port = 2121,
                .tcpPort = 9021,
                .Username = "anonymous",
                .Password = "",
                .UsePassiveMode = True,
                .Timeout = 30,
                .IsDefault = True
            })
            FtpManager.SaveProfiles(profiles)
        End If

        ' Populate combo box
        cmbProfiles.Items.Clear()
        For Each profile In profiles
            cmbProfiles.Items.Add(profile.Name)
        Next

        ' Select default or first profile
        Dim defaultProfile = profiles.FirstOrDefault(Function(p) p.IsDefault)
        If defaultProfile IsNot Nothing Then
            cmbProfiles.SelectedIndex = profiles.IndexOf(defaultProfile)
        ElseIf cmbProfiles.Items.Count > 0 Then
            cmbProfiles.SelectedIndex = 0
        End If
    End Sub

    Private Sub UpdateUI()
        Dim hasProfile = cmbProfiles.SelectedIndex >= 0
        btnDeleteProfile.Enabled = hasProfile AndAlso profiles.Count > 1
        btnTestConnection.Enabled = hasProfile
        btnConnect.Enabled = hasProfile
    End Sub

    Private Sub UpdateConnectionStatus()
        If FtpManager.IsConnected Then
            Dim profile = FtpManager.ActiveProfile
            lblConnectionStatus.Text = $"🟢 Connected to: {profile.Host}:{profile.Port}"
            lblConnectionStatus.ForeColor = Color.Green
            btnConnect.Text = "🔌 Disconnect"
        Else
            lblConnectionStatus.Text = "⚫ Not connected"
            lblConnectionStatus.ForeColor = Color.Gray
            btnConnect.Text = "🔌 Connect"
        End If
    End Sub

    Private Sub CmbProfiles_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbProfiles.SelectedIndexChanged
        If cmbProfiles.SelectedIndex < 0 Then Return

        currentProfile = profiles(cmbProfiles.SelectedIndex)

        ' Load profile data to controls
        txtProfileName.Text = currentProfile.Name
        txtHost.Text = currentProfile.Host
        txtPort.Value = currentProfile.Port
        txtUsername.Text = currentProfile.Username
        txtPassword.Text = currentProfile.Password
        txtTimeout.Value = currentProfile.Timeout
        chkPassiveMode.Checked = currentProfile.UsePassiveMode
        chkDefaultProfile.Checked = currentProfile.IsDefault

        isEditing = True
        UpdateUI()
    End Sub

    Private Sub BtnNewProfile_Click(sender As Object, e As EventArgs) Handles btnNewProfile.Click
        ' Clear controls for new profile
        txtProfileName.Text = "New Profile"
        txtHost.Text = "192.168.1.100"
        txtPort.Value = 2121
        txtUsername.Text = "anonymous"
        txtPassword.Text = ""
        txtTimeout.Value = 30
        chkPassiveMode.Checked = True
        chkDefaultProfile.Checked = False

        isEditing = False
        cmbProfiles.SelectedIndex = -1
        UpdateUI()
    End Sub

    Private Sub BtnDeleteProfile_Click(sender As Object, e As EventArgs) Handles btnDeleteProfile.Click
        If cmbProfiles.SelectedIndex < 0 Then Return
        If profiles.Count <= 1 Then
            MessageBox.Show("Cannot delete the last profile!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim result = MessageBox.Show(
            $"Are you sure you want to delete profile '{currentProfile.Name}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If result = DialogResult.Yes Then
            profiles.RemoveAt(cmbProfiles.SelectedIndex)
            FtpManager.SaveProfiles(profiles)
            LoadProfiles()
        End If
    End Sub

    Private Sub ChkShowPassword_CheckedChanged(sender As Object, e As EventArgs) Handles chkShowPassword.CheckedChanged
        txtPassword.UseSystemPasswordChar = Not chkShowPassword.Checked
    End Sub

    Private Async Sub BtnTestConnection_Click(sender As Object, e As EventArgs) Handles btnTestConnection.Click
        ' Validate inputs
        If String.IsNullOrWhiteSpace(txtHost.Text) Then
            MessageBox.Show("Please enter PS5 IP address!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Create profile from current inputs
        Dim testProfile As New FtpManager.FtpProfile With {
            .Name = txtProfileName.Text,
            .Host = txtHost.Text.Trim(),
            .Port = CInt(txtPort.Value),
            .Username = txtUsername.Text.Trim(),
            .Password = txtPassword.Text,
            .UsePassiveMode = chkPassiveMode.Checked,
            .Timeout = CInt(txtTimeout.Value)
        }

        ' Test connection
        btnTestConnection.Enabled = False
        lblStatus.Text = "Status: Testing connection..."
        lblStatus.ForeColor = Color.Blue

        Try
            Dim result = Await FtpManager.TestConnectionAsync(testProfile)
            lblStatus.Text = $"Status: {result}"
            lblStatus.ForeColor = If(result.StartsWith("✓"), Color.Green, Color.Red)

            If result.StartsWith("✓") Then
                MessageBox.Show(result, "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show(result, "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            lblStatus.Text = $"Status: Test failed - {ex.Message}"
            lblStatus.ForeColor = Color.Red
            MessageBox.Show($"Test failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnTestConnection.Enabled = True
        End Try
    End Sub

    Private Async Sub BtnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        If FtpManager.IsConnected Then
            ' Disconnect
            Try
                Await FtpManager.DisconnectAsync()
                lblStatus.Text = "Status: Disconnected"
                lblStatus.ForeColor = Color.Gray
                UpdateConnectionStatus()
                MessageBox.Show("Disconnected successfully!", "FTP Connection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"Disconnect error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        Else
            ' Connect
            ' Validate inputs
            If String.IsNullOrWhiteSpace(txtHost.Text) Then
                MessageBox.Show("Please enter PS5 IP address!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Save or update profile
            Dim profileToSave As New FtpManager.FtpProfile With {
                .Name = txtProfileName.Text.Trim(),
                .Host = txtHost.Text.Trim(),
                .Port = CInt(txtPort.Value),
                .Username = txtUsername.Text.Trim(),
                .Password = txtPassword.Text,
                .UsePassiveMode = chkPassiveMode.Checked,
                .Timeout = CInt(txtTimeout.Value),
                .IsDefault = chkDefaultProfile.Checked
            }

            ' Clear other default flags if this is default
            If profileToSave.IsDefault Then
                For Each p In profiles
                    p.IsDefault = False
                Next
            End If

            ' Add or update profile
            If isEditing AndAlso currentProfile IsNot Nothing Then
                Dim idx = profiles.IndexOf(currentProfile)
                profiles(idx) = profileToSave
            Else
                profiles.Add(profileToSave)
            End If

            FtpManager.SaveProfiles(profiles)

            ' Connect
            btnConnect.Enabled = False
            lblStatus.Text = "Status: Connecting..."
            lblStatus.ForeColor = Color.Blue

            Try
                Dim success = Await FtpManager.ConnectAsync(profileToSave)

                If success Then
                    lblStatus.Text = "Status: Connected successfully!"
                    lblStatus.ForeColor = Color.Green
                    UpdateConnectionStatus()
                    selectedProfile = profileToSave
                    Me.DialogResult = DialogResult.OK
                    MessageBox.Show("Connected successfully!", "FTP Connection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Catch ex As Exception
                lblStatus.Text = $"Status: Connection failed - {ex.Message}"
                lblStatus.ForeColor = Color.Red
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnConnect.Enabled = True
            End Try
        End If
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

End Class