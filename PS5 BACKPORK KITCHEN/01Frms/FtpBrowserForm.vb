Imports System.Windows.Forms
Imports System.Drawing
Imports System.IO

Public Class FtpBrowserForm
    Inherits Form

    ' UI Controls
    Private WithEvents btnConnect As Button

    Private WithEvents txtCurrentPath As TextBox
    Private WithEvents btnRefresh As Button
    Private WithEvents btnUp As Button
    Private WithEvents btnHome As Button
    Private WithEvents dgvFiles As DataGridView
    Private WithEvents btnDownload As Button
    Private WithEvents btnUpload As Button
    Private WithEvents btnCreateFolder As Button
    Private WithEvents btnDelete As Button
    Private lblStatus As Label
    Private lblConnectionInfo As Label
    Private progressBar As ProgressBar

    ' Data
    Private currentPath As String = "/"

    Private fileList As List(Of FtpManager.RemoteFileInfo)
    Private selectedFiles As List(Of FtpManager.RemoteFileInfo)

    Public Sub New()
        InitializeComponent()
        UpdateUI()
    End Sub

    Private Sub InitializeComponent()
        ' Form settings
        Me.Text = "PS5 FTP File Browser"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(800, 600)

        ' Top Panel - Connection & Navigation
        Dim pnlTop As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 120,
            .BackColor = Color.FromArgb(45, 45, 48),
            .Padding = New Padding(10)
        }

        ' Connection Info
        lblConnectionInfo = New Label With {
            .Location = New Point(15, 10),
            .AutoSize = True,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        pnlTop.Controls.Add(lblConnectionInfo)

        btnConnect = New Button With {
            .Text = "📡 Connect to PS5",
            .Location = New Point(15, 35),
            .Width = 150,
            .Height = 35,
            .BackColor = Color.FromArgb(0, 122, 204),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        pnlTop.Controls.Add(btnConnect)

        ' Path Navigation
        Dim lblPath As New Label With {
            .Text = "Current Path:",
            .Location = New Point(180, 15),
            .AutoSize = True,
            .ForeColor = Color.White
        }
        pnlTop.Controls.Add(lblPath)

        txtCurrentPath = New TextBox With {
            .Location = New Point(180, 40),
            .Width = 650,
            .ReadOnly = True,
            .Font = New Font("Consolas", 10),
            .BackColor = Color.FromArgb(30, 30, 30),
            .ForeColor = Color.White
        }
        pnlTop.Controls.Add(txtCurrentPath)

        btnHome = New Button With {
            .Text = "🏠",
            .Location = New Point(840, 38),
            .Width = 40,
            .Height = 27
        }
        pnlTop.Controls.Add(btnHome)

        btnUp = New Button With {
            .Text = "⬆️",
            .Location = New Point(890, 38),
            .Width = 40,
            .Height = 27
        }
        pnlTop.Controls.Add(btnUp)

        btnRefresh = New Button With {
            .Text = "🔄",
            .Location = New Point(940, 38),
            .Width = 40,
            .Height = 27
        }
        pnlTop.Controls.Add(btnRefresh)

        ' Progress Bar
        progressBar = New ProgressBar With {
            .Location = New Point(15, 80),
            .Width = 965,
            .Height = 25,
            .Visible = False
        }
        pnlTop.Controls.Add(progressBar)

        Me.Controls.Add(pnlTop)

        ' DataGridView for files
        dgvFiles = New DataGridView With {
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
            .BackgroundColor = Color.White
        }

        ' Configure columns
        dgvFiles.Columns.Add(New DataGridViewImageColumn With {
            .Name = "colIcon",
            .HeaderText = "",
            .Width = 40,
            .ImageLayout = DataGridViewImageCellLayout.Zoom
        })

        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colName",
            .HeaderText = "Name",
            .DataPropertyName = "Name",
            .FillWeight = 300
        })

        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colType",
            .HeaderText = "Type",
            .DataPropertyName = "Type",
            .Width = 120
        })

        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colSize",
            .HeaderText = "Size",
            .DataPropertyName = "Size",
            .Width = 100
        })

        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colDate",
            .HeaderText = "Modified",
            .DataPropertyName = "ModifiedDate",
            .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd HH:mm:ss"}
        })

        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colPath",
            .HeaderText = "FullPath",
            .DataPropertyName = "FullPath",
            .Visible = False
        })

        Me.Controls.Add(dgvFiles)

        ' Bottom Panel - Actions & Status
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = Color.FromArgb(240, 240, 240),
            .Padding = New Padding(10)
        }

        ' Status
        lblStatus = New Label With {
            .Text = "Status: Not connected",
            .Location = New Point(15, 15),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }
        pnlBottom.Controls.Add(lblStatus)

        ' Action Buttons
        btnDownload = New Button With {
            .Text = "📥 Download Selected",
            .Location = New Point(15, 40),
            .Width = 150,
            .Height = 30,
            .Enabled = False
        }
        pnlBottom.Controls.Add(btnDownload)

        btnUpload = New Button With {
            .Text = "📤 Upload Files",
            .Location = New Point(175, 40),
            .Width = 130,
            .Height = 30,
            .Enabled = False
        }
        pnlBottom.Controls.Add(btnUpload)

        btnCreateFolder = New Button With {
            .Text = "📁 New Folder",
            .Location = New Point(315, 40),
            .Width = 120,
            .Height = 30,
            .Enabled = False
        }
        pnlBottom.Controls.Add(btnCreateFolder)

        btnDelete = New Button With {
            .Text = "🗑️ Delete",
            .Location = New Point(445, 40),
            .Width = 100,
            .Height = 30,
            .ForeColor = Color.Red,
            .Enabled = False
        }
        pnlBottom.Controls.Add(btnDelete)

        Me.Controls.Add(pnlBottom)
    End Sub

    Private Sub UpdateUI()
        Dim connected = FtpManager.IsConnected

        If connected Then
            Dim profile = FtpManager.ActiveProfile
            lblConnectionInfo.Text = $"🟢 Connected: {profile.Host}:{profile.Port}"
            lblConnectionInfo.ForeColor = Color.LightGreen
            btnConnect.Text = "🔌 Disconnect"
            btnConnect.BackColor = Color.FromArgb(200, 50, 50)

            ' Enable navigation
            txtCurrentPath.Enabled = True
            btnRefresh.Enabled = True
            btnUp.Enabled = True
            btnHome.Enabled = True
            btnUpload.Enabled = True
            btnCreateFolder.Enabled = True
        Else
            lblConnectionInfo.Text = "⚫ Not Connected"
            lblConnectionInfo.ForeColor = Color.Gray
            btnConnect.Text = "📡 Connect to PS5"
            btnConnect.BackColor = Color.FromArgb(0, 122, 204)

            ' Disable navigation
            txtCurrentPath.Enabled = False
            btnRefresh.Enabled = False
            btnUp.Enabled = False
            btnHome.Enabled = False
            btnDownload.Enabled = False
            btnUpload.Enabled = False
            btnCreateFolder.Enabled = False
            btnDelete.Enabled = False

            dgvFiles.DataSource = Nothing
        End If
    End Sub

    Private Async Sub BtnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        If FtpManager.IsConnected Then
            ' Disconnect
            Try
                Await FtpManager.DisconnectAsync()
                lblStatus.Text = "Status: Disconnected"
                UpdateUI()
                MessageBox.Show("Disconnected from PS5", "FTP", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"Disconnect error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        Else
            ' Open connection dialog
            Using connForm As New FtpConnectionForm()
                If connForm.ShowDialog() = DialogResult.OK Then
                    UpdateUI()
                    ' Load initial directory
                    Try
                        currentPath = Await FtpManager.GetWorkingDirectoryAsync()
                        Await LoadDirectory(currentPath)
                    Catch ex As Exception
                        MessageBox.Show($"Failed to load directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End If
    End Sub

    Private Async Function LoadDirectory(path As String) As Task
        If Not FtpManager.IsConnected Then Return

        Try
            lblStatus.Text = $"Status: Loading {path}..."
            lblStatus.ForeColor = Color.Blue
            progressBar.Visible = True
            progressBar.Style = ProgressBarStyle.Marquee

            ' List directory
            fileList = Await FtpManager.ListDirectoryAsync(path)

            ' Update UI
            currentPath = path
            txtCurrentPath.Text = currentPath

            ' Bind to grid
            dgvFiles.DataSource = Nothing
            dgvFiles.DataSource = fileList

            ' Format size column
            For Each row As DataGridViewRow In dgvFiles.Rows
                If row.Cells("colSize").Value IsNot Nothing Then
                    Dim fileInfo = DirectCast(row.DataBoundItem, FtpManager.RemoteFileInfo)
                    row.Cells("colSize").Value = If(fileInfo.IsDirectory, "", FtpManager.FormatFileSize(fileInfo.Size))
                End If

                ' Set icon (simplified - you can add actual icons)
                Dim fileInfo2 = DirectCast(row.DataBoundItem, FtpManager.RemoteFileInfo)
                If fileInfo2.IsDirectory Then
                    row.Cells("colIcon").Value = Nothing ' Placeholder for folder icon
                End If
            Next

            lblStatus.Text = $"Status: Loaded {fileList.Count} items from {path}"
            lblStatus.ForeColor = Color.Green
        Catch ex As Exception
            lblStatus.Text = $"Status: Error loading directory"
            lblStatus.ForeColor = Color.Red
            MessageBox.Show($"Failed to load directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            progressBar.Visible = False
        End Try
    End Function

    Private Async Sub BtnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        Await LoadDirectory(currentPath)
    End Sub

    Private Async Sub BtnUp_Click(sender As Object, e As EventArgs) Handles btnUp.Click
        If currentPath = "/" Then Return

        Dim parentPath = Path.GetDirectoryName(currentPath.Replace("\"c, "/"c))
        If String.IsNullOrEmpty(parentPath) Then parentPath = "/"

        Await LoadDirectory(parentPath)
    End Sub

    Private Async Sub BtnHome_Click(sender As Object, e As EventArgs) Handles btnHome.Click
        Await LoadDirectory("/")
    End Sub

    Private Async Sub DgvFiles_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvFiles.CellDoubleClick
        If e.RowIndex < 0 Then Return

        Dim fileInfo = DirectCast(dgvFiles.Rows(e.RowIndex).DataBoundItem, FtpManager.RemoteFileInfo)

        If fileInfo.IsDirectory Then
            ' Navigate into directory
            Await LoadDirectory(fileInfo.FullPath)
        Else
            ' Show file info or download dialog
            MessageBox.Show($"File: {fileInfo.Name}{vbCrLf}Size: {FtpManager.FormatFileSize(fileInfo.Size)}{vbCrLf}Modified: {fileInfo.ModifiedDate}", "File Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub DgvFiles_SelectionChanged(sender As Object, e As EventArgs) Handles dgvFiles.SelectionChanged
        btnDownload.Enabled = dgvFiles.SelectedRows.Count > 0
        btnDelete.Enabled = dgvFiles.SelectedRows.Count > 0
    End Sub

    Private Async Sub BtnDownload_Click(sender As Object, e As EventArgs) Handles btnDownload.Click
        If dgvFiles.SelectedRows.Count = 0 Then Return

        ' Select destination folder
        Using fbd As New FolderBrowserDialog With {
            .Description = "Select destination folder for downloads",
            .ShowNewFolderButton = True
        }
            If fbd.ShowDialog() <> DialogResult.OK Then Return

            Dim destFolder = fbd.SelectedPath
            btnDownload.Enabled = False
            progressBar.Visible = True
            progressBar.Style = ProgressBarStyle.Blocks

            Try
                Dim totalFiles = dgvFiles.SelectedRows.Count
                Dim completed = 0

                For Each row As DataGridViewRow In dgvFiles.SelectedRows
                    Dim fileInfo = DirectCast(row.DataBoundItem, FtpManager.RemoteFileInfo)

                    lblStatus.Text = $"Status: Downloading {fileInfo.Name}... ({completed + 1}/{totalFiles})"
                    lblStatus.ForeColor = Color.Blue

                    Dim localPath = Path.Combine(destFolder, fileInfo.Name)

                    If fileInfo.IsDirectory Then
                        ' Download directory
                        Dim count = Await FtpManager.DownloadDirectoryAsync(fileInfo.FullPath, localPath)
                        lblStatus.Text = $"Status: Downloaded folder {fileInfo.Name} ({count} files)"
                    Else
                        ' Download file
                        Dim success = Await FtpManager.DownloadFileAsync(fileInfo.FullPath, localPath)
                        If Not success Then
                            MessageBox.Show($"Failed to download {fileInfo.Name}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End If
                    End If

                    completed += 1
                    progressBar.Value = CInt((completed / totalFiles) * 100)
                Next

                lblStatus.Text = $"Status: Downloaded {completed} items successfully"
                lblStatus.ForeColor = Color.Green
                MessageBox.Show($"Downloaded {completed} items to {destFolder}", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                lblStatus.Text = "Status: Download failed"
                lblStatus.ForeColor = Color.Red
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnDownload.Enabled = True
                progressBar.Visible = False
                progressBar.Value = 0
            End Try
        End Using
    End Sub

    Private Async Sub BtnUpload_Click(sender As Object, e As EventArgs) Handles btnUpload.Click
        Using ofd As New OpenFileDialog With {
            .Multiselect = True,
            .Title = "Select files to upload to PS5"
        }
            If ofd.ShowDialog() <> DialogResult.OK Then Return

            btnUpload.Enabled = False
            progressBar.Visible = True
            progressBar.Style = ProgressBarStyle.Blocks

            Try
                Dim totalFiles = ofd.FileNames.Length
                Dim completed = 0

                For Each localFile In ofd.FileNames
                    Dim fileName = Path.GetFileName(localFile)
                    Dim remotePath = currentPath.TrimEnd("/"c) & "/" & fileName

                    lblStatus.Text = $"Status: Uploading {fileName}... ({completed + 1}/{totalFiles})"
                    lblStatus.ForeColor = Color.Blue

                    Dim success = Await FtpManager.UploadFileAsync(localFile, remotePath)

                    If Not success Then
                        MessageBox.Show($"Failed to upload {fileName}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If

                    completed += 1
                    progressBar.Value = CInt((completed / totalFiles) * 100)
                Next

                lblStatus.Text = $"Status: Uploaded {completed} files successfully"
                lblStatus.ForeColor = Color.Green
                MessageBox.Show($"Uploaded {completed} files", "Upload Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Refresh directory
                Await LoadDirectory(currentPath)
            Catch ex As Exception
                lblStatus.Text = "Status: Upload failed"
                lblStatus.ForeColor = Color.Red
                MessageBox.Show($"Upload error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnUpload.Enabled = True
                progressBar.Visible = False
                progressBar.Value = 0
            End Try
        End Using
    End Sub

End Class