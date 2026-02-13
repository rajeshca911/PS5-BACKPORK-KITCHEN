Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports System.Linq

Imports PS5_BACKPORK_KITCHEN.PS5_Payload_Sender

''' <summary>
''' Payload Manager - Manage and send payloads to PS5
''' </summary>
Public Class PayloadManagerForm
    Inherits Form

    ' UI Controls
    Private WithEvents dgvPayloads As DataGridView

    Private WithEvents btnAdd As Button
    Private WithEvents btnEdit As Button
    Private WithEvents btnDelete As Button
    Private WithEvents btnSend As Button
    Private WithEvents btnBatchSend As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnConnect As Button
    Private WithEvents cmbCategoryFilter As ComboBox
    Private lblStatus As Label
    Private lblConnectionStatus As Label
    Private lblStats As Label
    Private progressBar As ProgressBar
    Private txtIP As TextBox
    Private txtPort As TextBox

    ' Data
    Private db As PayloadLibrary

    Private allPayloads As List(Of PayloadInfo)
    Private selectedPayload As PayloadInfo

    'Private Sub loadprofiles()
    '    Try
    '        Dim profiles = FtpManager.LoadProfiles()
    '        ' Add default profile if none exist
    '        If profiles.Count = 0 Then
    '            profiles.Add(New FtpManager.FtpProfile With {
    '            .Name = "My PS5",
    '            .Host = "192.168.1.100",
    '            .Port = 2121,
    '            .tcpPort = 9021,
    '            .Username = "anonymous",
    '            .Password = "",
    '            .UsePassiveMode = True,
    '            .Timeout = 30,
    '            .IsDefault = True
    '        })
    '            FtpManager.SaveProfiles(profiles)
    '        End If
    '        Dim defaultProfile = profiles.FirstOrDefault(Function(p) p.IsDefault)
    '        If profiles IsNot Nothing AndAlso profiles.Count > 0 Then
    '            Dim activeProfile = profiles.FirstOrDefault(Function(p) p.IsDefault)
    '            If activeProfile IsNot Nothing Then
    '                txtIP.Text = activeProfile.Host
    '                txtPort.Text = activeProfile.tcpPort.ToString()
    '            Else
    '                txtIP.Text = profiles(0).Host
    '                txtPort.Text = profiles(0).tcpPort.ToString()
    '            End If
    '        End If
    '    Catch ex As Exception
    '        txtIP.Text = "192.168.29.78"
    '        txtPort.Text = "9021"
    '        Logger.LogToFile($"Error loading FTP profiles: {ex.Message}", LogLevel.Error)

    '    End Try
    'End Sub
    Private Sub LoadProfiles()
        Try
            Dim profiles = FtpManager.LoadProfiles()

            ' Create default profile if none exist
            If profiles Is Nothing OrElse profiles.Count = 0 Then
                Dim defaultProfile As New FtpManager.FtpProfile With {
                .Name = "My PS5",
                .Host = "192.168.1.100",
                .Port = 2121,
                .tcpPort = 9021,
                .Username = "anonymous",
                .Password = "",
                .UsePassiveMode = True,
                .Timeout = 30,
                .IsDefault = True
            }

                profiles = New List(Of FtpManager.FtpProfile) From {defaultProfile}
                FtpManager.SaveProfiles(profiles)
            End If

            ' Pick default or first
            Dim activeProfile = profiles.FirstOrDefault(Function(p) p.IsDefault)
            If activeProfile Is Nothing Then activeProfile = profiles(0)

            txtIP.Text = activeProfile.Host
            txtPort.Text = activeProfile.tcpPort.ToString()
        Catch ex As Exception
            txtIP.Text = "192.168.29.78"
            txtPort.Text = "9021"
            Logger.LogToFile($"Error loading FTP profiles: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    Private Sub SaveOrUpdateProfileFromUI()
        Try
            Dim profiles = FtpManager.LoadProfiles()

            If profiles Is Nothing Then
                profiles = New List(Of FtpManager.FtpProfile)
            End If

            ' Remove existing default flag
            For Each p In profiles
                p.IsDefault = False
            Next

            Dim profile = profiles.FirstOrDefault(Function(p) p.Name = "My PS5")

            If profile Is Nothing Then
                profile = New FtpManager.FtpProfile
                profiles.Add(profile)
            End If

            profile.Name = "My PS5"
            profile.Host = txtIP.Text.Trim()
            profile.tcpPort = Integer.Parse(txtPort.Text)
            profile.Port = 2121
            profile.Username = "anonymous"
            profile.Password = ""
            profile.UsePassiveMode = True
            profile.Timeout = 30
            profile.IsDefault = True

            FtpManager.SaveProfiles(profiles)
        Catch ex As Exception
            Logger.LogToFile($"Profile save failed: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    Public Sub New()
        Try
            InitializeComponent()

            ' Initialize with empty list to prevent NullReference
            allPayloads = New List(Of PayloadInfo)

            db = New PayloadLibrary()
            LoadPayloads()
            UpdateUI()
            LoadStatistics()
            LoadProfiles()
        Catch ex As Exception
            Dim errorDetails = $"=== PAYLOAD MANAGER ERROR ==={vbCrLf}" &
                             $"Message: {ex.Message}{vbCrLf}" &
                             $"Type: {ex.GetType().Name}{vbCrLf}" &
                             $"Stack Trace:{vbCrLf}{ex.StackTrace}{vbCrLf}" &
                             $"==========================={vbCrLf}"

            ' Log to file
            Logger.LogToFile(errorDetails, LogLevel.Error)

            ' Show user-friendly message with log location
            Dim logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log", "app.log")
            MessageBox.Show($"Error initializing Payload Manager.{vbCrLf}{vbCrLf}" &
                          $"Error details saved to:{vbCrLf}{logPath}{vbCrLf}{vbCrLf}" &
                          $"Please send the log file to the developer.",
                          "Payload Manager Initialization Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Initialize with empty data to prevent form crash
            If lblStatus IsNot Nothing Then
                lblStatus.Text = "Error loading payloads"
            End If
        End Try
    End Sub

    Private Sub InitializeComponent()
        ' Form settings
        Me.Text = "Payload Manager"
        Me.Size = New Size(1200, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(1000, 600)
        Me.BackColor = Color.White

        ' Main Layout
        Dim mainLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .Padding = New Padding(10)
        }
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))  ' Header
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Content
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))  ' Progress
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100)) ' Actions & Status

        ' HEADER PANEL
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(45, 45, 48),
            .Padding = New Padding(10)
        }

        Dim lblTitle As New Label With {
            .Text = "🎮 PS5 Payload Manager",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(10, 10)
        }
        pnlHeader.Controls.Add(lblTitle)

        lblConnectionStatus = New Label With {
            .Text = "⚫ Not Connected",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(10, 45)
        }
        pnlHeader.Controls.Add(lblConnectionStatus)

        'btnConnect = New Button With {
        '    .Text = "📡 Connect to PS5",
        '    .Location = New Point(950, 15),
        '    .Size = New Size(200, 50),
        '    .BackColor = Color.FromArgb(0, 122, 204),
        '    .ForeColor = Color.White,
        '    .FlatStyle = FlatStyle.Flat,
        '    .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        '}
        'pnlHeader.Controls.Add(btnConnect)
        ' ===== IP Label =====
        Dim lblIP As New Label With {
    .Text = "IP:",
    .ForeColor = Color.White,
    .AutoSize = True,
    .Location = New Point(800, 18),
    .Font = New Font("Segoe UI", 10, FontStyle.Bold)
}
        pnlHeader.Controls.Add(lblIP)

        txtIP = New TextBox With {
    .Location = New Point(830, 15),
    .Size = New Size(120, 25),
    .Text = "192.168.1.10"   ' default example
}
        pnlHeader.Controls.Add(txtIP)

        ' ===== Port Label =====
        Dim lblPort As New Label With {
    .Text = "Port:",
    .ForeColor = Color.White,
    .AutoSize = True,
    .Location = New Point(960, 18),
    .Font = New Font("Segoe UI", 10, FontStyle.Bold)
}
        pnlHeader.Controls.Add(lblPort)

        txtPort = New TextBox With {
    .Location = New Point(1010, 15),
    .Size = New Size(70, 25),
    .Text = "9020"   ' default payload port
}
        pnlHeader.Controls.Add(txtPort)

        lblStats = New Label With {
            .Text = "Loading statistics...",
            .Location = New Point(250, 45),
            .AutoSize = True,
            .ForeColor = Color.LightGray,
            .Font = New Font("Segoe UI", 9)
        }
        pnlHeader.Controls.Add(lblStats)

        mainLayout.Controls.Add(pnlHeader, 0, 0)

        ' CONTENT PANEL
        Dim pnlContent As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(5)
        }

        ' Toolbar
        Dim pnlToolbar As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .Height = 50,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Padding = New Padding(5)
        }

        Dim lblFilter As New Label With {
            .Text = "Category:",
            .AutoSize = True,
            .Margin = New Padding(5, 12, 5, 0),
            .Font = New Font("Segoe UI", 10)
        }
        pnlToolbar.Controls.Add(lblFilter)

        cmbCategoryFilter = New ComboBox With {
            .Width = 150,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 10),
            .Margin = New Padding(5, 8, 15, 0)
        }
        cmbCategoryFilter.Items.AddRange({"All", "Jailbreak", "Homebrew", "Debug", "Backup", "Custom"})
        cmbCategoryFilter.SelectedIndex = 0
        pnlToolbar.Controls.Add(cmbCategoryFilter)

        btnRefresh = New Button With {
            .Text = "🔄 Refresh",
            .Width = 100,
            .Height = 35,
            .Margin = New Padding(5)
        }
        pnlToolbar.Controls.Add(btnRefresh)

        pnlContent.Controls.Add(pnlToolbar)

        ' DataGridView
        dgvPayloads = New DataGridView With {
            .Dock = DockStyle.Fill,
            .AutoGenerateColumns = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
            .RowHeadersVisible = False,
            .AllowUserToResizeRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .Font = New Font("Segoe UI", 9),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.Fixed3D,
            .ColumnHeadersHeight = 35,
            .RowTemplate = New DataGridViewRow With {.Height = 30},
            .EnableHeadersVisualStyles = False
        }

        ' Style column headers
        dgvPayloads.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48)
        dgvPayloads.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgvPayloads.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        dgvPayloads.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        dgvPayloads.ColumnHeadersDefaultCellStyle.Padding = New Padding(5)

        ' Style cells
        dgvPayloads.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215)
        dgvPayloads.DefaultCellStyle.SelectionForeColor = Color.White
        dgvPayloads.DefaultCellStyle.Padding = New Padding(5, 2, 5, 2)
        dgvPayloads.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)

        ' Configure columns
        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colId",
            .HeaderText = "ID",
            .DataPropertyName = "Id",
            .Width = 50,
            .Visible = False
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colName",
            .HeaderText = "Payload Name",
            .DataPropertyName = "Name",
            .FillWeight = 200
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colDescription",
            .HeaderText = "Description",
            .DataPropertyName = "Description",
            .FillWeight = 250
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colCategory",
            .HeaderText = "Category",
            .DataPropertyName = "Category",
            .Width = 100
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colSize",
            .HeaderText = "Size",
            .DataPropertyName = "FileSizeFormatted",
            .Width = 90
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colSendCount",
            .HeaderText = "Sent",
            .DataPropertyName = "SendCount",
            .Width = 70
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colLastSent",
            .HeaderText = "Last Sent",
            .DataPropertyName = "LastSent",
            .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd HH:mm"}
        })

        dgvPayloads.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colTargetPath",
            .HeaderText = "Target Path",
            .DataPropertyName = "TargetPath",
            .Width = 200
        })

        pnlContent.Controls.Add(dgvPayloads)
        dgvPayloads.BringToFront() ' Ensure visible above toolbar

        mainLayout.Controls.Add(pnlContent, 0, 1)

        ' PROGRESS BAR
        progressBar = New ProgressBar With {
            .Dock = DockStyle.Fill,
            .Visible = False,
            .Style = ProgressBarStyle.Continuous
        }
        mainLayout.Controls.Add(progressBar, 0, 2)

        ' BOTTOM PANEL - Actions & Status
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(240, 240, 240),
            .Padding = New Padding(10)
        }

        ' Status Label
        lblStatus = New Label With {
            .Text = "Ready",
            .Location = New Point(15, 15),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10),
            .ForeColor = Color.DarkGreen
        }
        pnlBottom.Controls.Add(lblStatus)

        ' Action Buttons
        btnAdd = New Button With {
            .Text = "➕ Add Payload",
            .Location = New Point(15, 45),
            .Size = New Size(130, 40),
            .BackColor = Color.FromArgb(76, 175, 80),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10)
        }
        pnlBottom.Controls.Add(btnAdd)

        btnEdit = New Button With {
            .Text = "✏️ Edit",
            .Location = New Point(155, 45),
            .Size = New Size(100, 40),
            .BackColor = Color.FromArgb(33, 150, 243),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Enabled = False
        }
        pnlBottom.Controls.Add(btnEdit)

        btnDelete = New Button With {
            .Text = "🗑️ Delete",
            .Location = New Point(265, 45),
            .Size = New Size(100, 40),
            .BackColor = Color.FromArgb(244, 67, 54),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Enabled = False
        }
        pnlBottom.Controls.Add(btnDelete)

        btnSend = New Button With {
            .Text = "📤 Send Selected",
            .Location = New Point(400, 45),
            .Size = New Size(150, 40),
            .BackColor = Color.FromArgb(156, 39, 176),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Enabled = True
        }
        pnlBottom.Controls.Add(btnSend)

        btnBatchSend = New Button With {
            .Text = "📦 Batch Send All",
            .Location = New Point(560, 45),
            .Size = New Size(150, 40),
            .BackColor = Color.FromArgb(255, 152, 0),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Enabled = True
        }
        pnlBottom.Controls.Add(btnBatchSend)

        mainLayout.Controls.Add(pnlBottom, 0, 3)

        ' Add main layout to form
        Me.Controls.Add(mainLayout)
    End Sub

    Private Sub LoadPayloads()
        If db Is Nothing Then Return ' Not yet initialized

        Try
            Dim category = cmbCategoryFilter.SelectedItem?.ToString()

            If category = "All" OrElse String.IsNullOrEmpty(category) Then
                allPayloads = db.GetAllPayloads()
            Else
                allPayloads = db.GetPayloadsByCategory(category)
            End If

            dgvPayloads.DataSource = Nothing
            dgvPayloads.DataSource = allPayloads

            ' Color-code rows based on file existence
            For Each row As DataGridViewRow In dgvPayloads.Rows
                Dim payload = DirectCast(row.DataBoundItem, PayloadInfo)
                If Not payload.FileExists Then
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235)
                    row.DefaultCellStyle.ForeColor = Color.Gray
                End If
            Next

            lblStatus.Text = $"Loaded {allPayloads.Count} payloads"
            lblStatus.ForeColor = Color.DarkGreen
        Catch ex As Exception
            ToastNotification.ShowError($"Error loading payloads: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadStatistics()
        If db Is Nothing Then Return ' Not yet initialized

        Try
            Dim stats = db.GetStatistics()
            lblStats.Text = $"Total: {stats("TotalPayloads")} | Sent: {stats("TotalSends")} | Success Rate: {stats("SuccessRate"):F1}%"
        Catch ex As Exception
            lblStats.Text = "Statistics unavailable"
        End Try
    End Sub

    Private Sub UpdateUI()
        Dim connected = FtpManager.IsConnected

        ' Update connection status
        'If connected Then
        '    Dim profile = FtpManager.ActiveProfile
        '    lblConnectionStatus.Text = $"🟢 Connected: {profile.Host}:{profile.Port}"
        '    lblConnectionStatus.ForeColor = Color.LightGreen
        '    btnConnect.Text = "🔌 Disconnect"
        '    btnConnect.BackColor = Color.FromArgb(200, 50, 50)
        'Else
        '    lblConnectionStatus.Text = "⚫ Not Connected"
        '    lblConnectionStatus.ForeColor = Color.Gray
        '    btnConnect.Text = "📡 Connect to PS5"
        '    btnConnect.BackColor = Color.FromArgb(0, 122, 204)
        'End If

        ' Enable/disable send buttons based on connection
        'btnSend.Enabled = connected AndAlso dgvPayloads.SelectedRows.Count > 0
        'btnBatchSend.Enabled = connected AndAlso allPayloads IsNot Nothing AndAlso allPayloads.Count > 0

        ' Enable edit/delete based on selection
        btnEdit.Enabled = dgvPayloads.SelectedRows.Count = 1
        btnDelete.Enabled = dgvPayloads.SelectedRows.Count > 0
    End Sub

    Private Async Sub BtnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        If FtpManager.IsConnected Then
            ' Disconnect
            Try
                Await FtpManager.DisconnectAsync()
                ToastNotification.ShowInfo("Disconnected from PS5")
            Catch ex As Exception
                ToastNotification.ShowError($"Disconnect error: {ex.Message}")
            End Try
        Else
            ' Connect
            Using connForm As New FtpConnectionForm()
                connForm.ShowDialog()
            End Using
        End If

        UpdateUI()
    End Sub

    Private Sub BtnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Using addForm As New PayloadEditForm()
            If addForm.ShowDialog() = DialogResult.OK Then
                Try
                    db.AddPayload(addForm.PayloadInfo)
                    LoadPayloads()
                    LoadStatistics()
                    ToastNotification.ShowSuccess("Payload added successfully!")
                Catch ex As Exception
                    ToastNotification.ShowError($"Error adding payload: {ex.Message}")
                End Try
            End If
        End Using
    End Sub

    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        If dgvPayloads.SelectedRows.Count = 0 Then Return

        Dim payload = DirectCast(dgvPayloads.SelectedRows(0).DataBoundItem, PayloadInfo)

        Using editForm As New PayloadEditForm(payload)
            If editForm.ShowDialog() = DialogResult.OK Then
                Try
                    db.UpdatePayload(editForm.PayloadInfo)
                    LoadPayloads()
                    ToastNotification.ShowSuccess("Payload updated successfully!")
                Catch ex As Exception
                    ToastNotification.ShowError($"Error updating payload: {ex.Message}")
                End Try
            End If
        End Using
    End Sub

    Private Sub BtnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If dgvPayloads.SelectedRows.Count = 0 Then Return

        Dim count = dgvPayloads.SelectedRows.Count
        Dim result = MessageBox.Show(
            $"Delete {count} selected payload(s)?{vbCrLf}This will mark them as inactive (soft delete).",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        )

        If result = DialogResult.Yes Then
            Try
                For Each row As DataGridViewRow In dgvPayloads.SelectedRows
                    Dim payload = DirectCast(row.DataBoundItem, PayloadInfo)
                    db.DeletePayload(payload.Id, hardDelete:=False)
                Next

                LoadPayloads()
                LoadStatistics()
                ToastNotification.ShowSuccess($"{count} payload(s) deleted")
            Catch ex As Exception
                ToastNotification.ShowError($"Error deleting payloads: {ex.Message}")
            End Try
        End If
    End Sub

    'Private Async Sub BtnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
    '    If dgvPayloads.SelectedRows.Count = 0 Then Return

    '    'If Not FtpManager.IsConnected Then
    '    '    ToastNotification.ShowWarning("Please connect to PS5 first!")
    '    '    Return
    '    'End If
    '    Dim ip = txtIP.Text.Trim()
    '    Dim port As Integer
    '    If Not Integer.TryParse(txtPort.Text, port) Then
    '        MessageBox.Show("Invalid port")
    '        Return
    '    End If

    '    Dim payloads As New List(Of PayloadInfo)
    '    For Each row As DataGridViewRow In dgvPayloads.SelectedRows
    '        payloads.Add(DirectCast(row.DataBoundItem, PayloadInfo))
    '    Next

    '    ' Disable UI during send
    '    btnSend.Enabled = False
    '    btnBatchSend.Enabled = False
    '    progressBar.Visible = True
    '    progressBar.Style = ProgressBarStyle.Marquee

    '    Try
    '        Dim results As New List(Of PayloadSenderService.SendResult)

    '        For Each payload In payloads
    '            lblStatus.Text = $"Sending {payload.Name}..."
    '            lblStatus.ForeColor = Color.Blue

    '            ' Validate
    '            Dim validation = PayloadSenderService.ValidatePayload(payload)
    '            If Not validation.Valid Then
    '                ToastNotification.ShowWarning(validation.ErrorMessage)
    '                Continue For
    '            End If

    '            ' Send
    '            Dim result = Await PayloadSenderService.SendViaFtpAsync(payload)
    '            results.Add(result)

    '            ' Record in database
    '            db.RecordSend(payload.Id, FtpManager.ActiveProfile.Host, "FTP", result.Success, result.ErrorMessage, result.DurationMs)
    '        Next

    '        ' Show summary
    '        Dim successCount = results.Where(Function(r) r.Success).Count()
    '        Dim summary = $"Sent {successCount}/{results.Count} payload(s) successfully"

    '        If results.Where(Function(r) Not r.Success).Any() Then
    '            summary &= vbCrLf & vbCrLf & "Failed:"
    '            For Each failed In results.Where(Function(r) Not r.Success)
    '                summary &= vbCrLf & $"• {failed.PayloadName}: {failed.ErrorMessage}"
    '            Next
    '        End If

    '        If successCount = results.Count Then
    '            ToastNotification.ShowSuccess(summary, 4000)
    '        Else
    '            ToastNotification.ShowWarning(summary, 5000)
    '        End If

    '        ' Refresh
    '        LoadPayloads()
    '        LoadStatistics()
    '    Catch ex As Exception
    '        ToastNotification.ShowError($"Send error: {ex.Message}")
    '    Finally
    '        progressBar.Visible = False
    '        btnSend.Enabled = True
    '        btnBatchSend.Enabled = True
    '        lblStatus.Text = "Ready"
    '        lblStatus.ForeColor = Color.DarkGreen
    '        UpdateUI()
    '    End Try
    'End Sub
    Private Async Sub BtnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        If dgvPayloads.SelectedRows.Count = 0 Then Return

        Dim ip = txtIP.Text.Trim()

        Dim port As Integer
        If Not Integer.TryParse(txtPort.Text, port) Then
            MessageBox.Show("Invalid port")
            Return
        End If
        SaveOrUpdateProfileFromUI()
        ' Build selected payload list
        Dim payloads As New List(Of PayloadInfo)
        For Each row As DataGridViewRow In dgvPayloads.SelectedRows
            payloads.Add(DirectCast(row.DataBoundItem, PayloadInfo))
        Next

        ' Disable UI
        btnSend.Enabled = False
        btnBatchSend.Enabled = False
        progressBar.Visible = True
        progressBar.Style = ProgressBarStyle.Marquee

        Dim results As New List(Of String)

        Try
            For Each payload In payloads

                lblStatus.Text = $"Sending {payload.Name}..."
                lblStatus.ForeColor = Color.Blue
                Await Task.Delay(50) ' allow UI refresh

                ' Validate file exists
                If Not IO.File.Exists(payload.TargetPath) Then
                    results.Add($"{payload.Name} — file not found")
                    Continue For
                End If

                Using tcp As New TcpPayloadSender()

                    If Not tcp.Connect(ip, port) Then
                        results.Add($"{payload.Name} — connect failed: {tcp.LastError}")
                        Continue For
                    End If

                    If tcp.SendPayload(payload.TargetPath) Then
                        results.Add($"{payload.Name} — OK")

                        ' record DB success
                        db.RecordSend(payload.Id, ip, "TCP", True, "", 0)
                    Else
                        results.Add($"{payload.Name} — failed: {tcp.LastError}")

                        db.RecordSend(payload.Id, ip, "TCP", False, tcp.LastError, 0)
                    End If

                End Using

            Next

            ' ===== Summary =====

            'Dim ok = results.Count(Function(x) x.Contains("OK"))
            Dim ok As Integer = 0

            For Each r In results
                If r.Contains("OK") Then ok += 1
            Next

            Dim msg = $"Sent {ok}/{results.Count} payload(s)" &
                  vbCrLf & vbCrLf &
                  String.Join(vbCrLf, results)

            If ok = results.Count Then
                ToastNotification.ShowSuccess(msg, 4000)
            Else
                ToastNotification.ShowWarning(msg, 5000)
            End If

            LoadPayloads()
            LoadStatistics()
        Catch ex As Exception
            ToastNotification.ShowError($"Send error: {ex.Message}")
        Finally
            progressBar.Visible = False
            btnSend.Enabled = True
            btnBatchSend.Enabled = True
            lblStatus.Text = "Ready"
            lblStatus.ForeColor = Color.DarkGreen
            UpdateUI()
        End Try

    End Sub

    'Private Async Sub BtnBatchSend_Click(sender As Object, e As EventArgs) Handles btnBatchSend.Click
    '    If allPayloads Is Nothing OrElse allPayloads.Count = 0 Then Return

    '    'If Not FtpManager.IsConnected Then
    '    '    ToastNotification.ShowWarning("Please connect to PS5 first!")
    '    '    Return
    '    'End If

    '    Dim result = MessageBox.Show(
    '        $"Send all {allPayloads.Count} payloads?{vbCrLf}This will send all visible payloads in sequence.",
    '        "Batch Send",
    '        MessageBoxButtons.YesNo,
    '        MessageBoxIcon.Question
    '    )

    '    If result <> DialogResult.Yes Then Return

    '    ' Disable UI
    '    btnSend.Enabled = False
    '    btnBatchSend.Enabled = False
    '    progressBar.Visible = True
    '    progressBar.Style = ProgressBarStyle.Continuous

    '    Try
    '        ' Create progress handler
    '        Dim progress As New Progress(Of PayloadSenderService.SendProgress)(
    '            Sub(p)
    '                lblStatus.Text = p.ProgressText
    '                progressBar.Value = CInt(p.PercentComplete)
    '            End Sub
    '        )

    '        ' Send batch
    '        Dim batchResult = Await PayloadSenderService.SendBatchViaFtpAsync(allPayloads, progress)

    '        ' Record each send
    '        For Each sendResult In batchResult.Results
    '            Dim payload = allPayloads.FirstOrDefault(Function(p) p.Name = sendResult.PayloadName)
    '            If payload IsNot Nothing Then
    '                db.RecordSend(payload.Id, FtpManager.ActiveProfile.Host, "FTP", sendResult.Success, sendResult.ErrorMessage, sendResult.DurationMs)
    '            End If
    '        Next

    '        ' Show summary
    '        Dim summaryMsg = PayloadSenderService.FormatBatchResult(batchResult)
    '        If batchResult.SuccessRate = 100 Then
    '            ToastNotification.ShowSuccess(summaryMsg, 5000)
    '        Else
    '            ToastNotification.ShowWarning(summaryMsg, 6000)
    '        End If

    '        ' Refresh
    '        LoadPayloads()
    '        LoadStatistics()
    '    Catch ex As Exception
    '        ToastNotification.ShowError($"Batch send error: {ex.Message}")
    '    Finally
    '        progressBar.Visible = False
    '        progressBar.Value = 0
    '        btnSend.Enabled = True
    '        btnBatchSend.Enabled = True
    '        lblStatus.Text = "Ready"
    '        UpdateUI()
    '    End Try
    'End Sub
    Private Async Sub BtnBatchSend_Click(sender As Object, e As EventArgs) Handles btnBatchSend.Click

        If allPayloads Is Nothing OrElse allPayloads.Count = 0 Then Return

        Dim confirm = MessageBox.Show(
        $"Send all {allPayloads.Count} payloads?" & vbCrLf &
        "Each payload will wait 10 seconds before sending next.",
        "Batch Send",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question)

        If confirm <> DialogResult.Yes Then Return

        ' ===== Validate IP / Port =====
        Dim ip = txtIP.Text.Trim()

        Dim port As Integer
        If Not Integer.TryParse(txtPort.Text, port) Then
            MessageBox.Show("Invalid port")
            Return
        End If
        SaveOrUpdateProfileFromUI()
        ' ===== UI Lock =====
        btnSend.Enabled = False
        btnBatchSend.Enabled = False
        progressBar.Visible = True
        progressBar.Style = ProgressBarStyle.Continuous
        progressBar.Value = 0
        progressBar.Maximum = allPayloads.Count

        Dim results As New List(Of String)
        Dim index As Integer = 0

        Try
            For Each payload In allPayloads

                index += 1

                lblStatus.Text = $"[{index}/{allPayloads.Count}] Sending {payload.Name}..."
                progressBar.Value = index - 1
                Await Task.Delay(50)

                ' ===== Validate file =====
                If Not IO.File.Exists(payload.TargetPath) Then
                    results.Add($"{payload.Name} — file not found")
                    Continue For
                End If

                Dim sw As New Stopwatch
                sw.Start()

                Using tcp As New TcpPayloadSender()

                    If Not tcp.Connect(ip, port) Then
                        results.Add($"{payload.Name} — connect failed: {tcp.LastError}")
                        db.RecordSend(payload.Id, ip, "TCP", False, tcp.LastError, 0)
                        Continue For
                    End If

                    If tcp.SendPayload(payload.TargetPath) Then
                        sw.Stop()
                        results.Add($"{payload.Name} — OK")
                        db.RecordSend(payload.Id, ip, "TCP", True, "", sw.ElapsedMilliseconds)
                    Else
                        sw.Stop()
                        results.Add($"{payload.Name} — failed: {tcp.LastError}")
                        db.RecordSend(payload.Id, ip, "TCP", False, tcp.LastError, sw.ElapsedMilliseconds)
                    End If

                End Using

                progressBar.Value = index

                ' ===== 10 second cooldown =====
                If index < allPayloads.Count Then
                    lblStatus.Text = $"Cooldown 10s before next payload..."
                    Await Task.Delay(10000)
                End If

            Next

            ' ===== Summary =====

            Dim ok As Integer = 0

            For Each r In results
                If r.Contains("OK") Then ok += 1
            Next

            Dim msg =
            $"Batch complete: {ok}/{results.Count} success" &
            vbCrLf & vbCrLf &
            String.Join(vbCrLf, results)

            If ok = results.Count Then
                ToastNotification.ShowSuccess(msg, 6000)
            Else
                ToastNotification.ShowWarning(msg, 7000)
            End If

            LoadPayloads()
            LoadStatistics()
        Catch ex As Exception
            ToastNotification.ShowError($"Batch send error: {ex.Message}")
        Finally
            progressBar.Visible = False
            progressBar.Value = 0
            btnSend.Enabled = True
            btnBatchSend.Enabled = True
            lblStatus.Text = "Ready"
            UpdateUI()
        End Try

    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadPayloads()
        LoadStatistics()
        UpdateUI()
    End Sub

    Private Sub CmbCategoryFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCategoryFilter.SelectedIndexChanged
        LoadPayloads()
    End Sub

    Private Sub DgvPayloads_SelectionChanged(sender As Object, e As EventArgs) Handles dgvPayloads.SelectionChanged
        UpdateUI()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If db IsNot Nothing Then
            db.Dispose()
        End If
    End Sub

End Class