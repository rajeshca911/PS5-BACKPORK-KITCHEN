Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Repositories
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Application.Services
Imports System.Windows.Forms

''' <summary>
''' Dashboard for viewing patching history and statistics
''' Feature #5: Modern Dashboard
''' </summary>
Public Class DashboardForm
    Inherits Form

    Private _historyRepo As PatchingHistoryRepository
    Private _backupManager As BackupManager

    Public Sub New()
        InitializeComponent()
        Try
            _historyRepo = New PatchingHistoryRepository()
        Catch ex As Exception
            ' If history repo fails, continue anyway (will show empty data)
            MessageBox.Show($"Warning: Could not initialize history database: {ex.Message}{vbCrLf}Dashboard will show empty data.",
                          "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        AddHandler Me.Load, AddressOf DashboardForm_Load
    End Sub

    Public Sub New(historyRepo As PatchingHistoryRepository, Optional backupManager As BackupManager = Nothing)
        InitializeComponent()
        _historyRepo = historyRepo
        _backupManager = backupManager
        AddHandler Me.Load, AddressOf DashboardForm_Load
    End Sub

    Private Sub DashboardForm_Load(sender As Object, e As EventArgs)
        Try
            ' Setup DataGridView
            SetupHistoryGrid()

            ' Load statistics
            LoadStatistics()

            ' Load recent history
            LoadRecentHistory()

        Catch ex As Exception
            MessageBox.Show($"Failed to load dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupHistoryGrid()
        dgvHistory.AutoGenerateColumns = False
        dgvHistory.AllowUserToAddRows = False
        dgvHistory.AllowUserToDeleteRows = False
        dgvHistory.ReadOnly = True
        dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvHistory.MultiSelect = False

        ' Define columns
        dgvHistory.Columns.Clear()

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Timestamp",
            .HeaderText = "Date/Time",
            .DataPropertyName = "Timestamp",
            .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd HH:mm:ss"}
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "OperationType",
            .HeaderText = "Type",
            .DataPropertyName = "OperationType",
            .Width = 100
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "GameName",
            .HeaderText = "Game",
            .DataPropertyName = "GameName",
            .Width = 200,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "TargetSdk",
            .HeaderText = "SDK",
            .DataPropertyName = "TargetSdk",
            .Width = 100,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "X"}
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "TotalFiles",
            .HeaderText = "Total",
            .DataPropertyName = "TotalFiles",
            .Width = 60
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "PatchedFiles",
            .HeaderText = "Patched",
            .DataPropertyName = "PatchedFiles",
            .Width = 60
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "SkippedFiles",
            .HeaderText = "Skipped",
            .DataPropertyName = "SkippedFiles",
            .Width = 60
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "FailedFiles",
            .HeaderText = "Failed",
            .DataPropertyName = "FailedFiles",
            .Width = 60
        })

        dgvHistory.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Duration",
            .HeaderText = "Duration",
            .DataPropertyName = "Duration",
            .Width = 100
        })

        dgvHistory.Columns.Add(New DataGridViewCheckBoxColumn With {
            .Name = "Success",
            .HeaderText = "Success",
            .DataPropertyName = "Success",
            .Width = 60
        })

        ' Add event handler for double-click
        AddHandler dgvHistory.CellDoubleClick, AddressOf dgvHistory_CellDoubleClick
    End Sub

    Private Sub LoadStatistics()
        Try
            ' Check if repository is available
            If _historyRepo Is Nothing Then
                ShowEmptyStatistics()
                Return
            End If

            Dim stats = _historyRepo.GetStatistics()

            ' Check if there's any data
            If stats.TotalOperations = 0 Then
                ' Show "no data" message
                lblTotalOperations.Text = "0"
                lblSuccessfulOps.Text = "0"
                lblFailedOps.Text = "0"
                lblSuccessRate.Text = "0%"
                lblTotalFilesPatched.Text = "0"
                lblTotalFilesSkipped.Text = "0"
                lblTotalFilesFailed.Text = "0"
                lblFirstOperation.Text = "N/A"
                lblLastOperation.Text = "N/A"
                lblMostUsedSdk.Text = "N/A"
                lblAverageDuration.Text = "00:00:00"
                lblAverageFilesPerOp.Text = "0"
                lblSuccessRate.ForeColor = Color.Gray
                Return
            End If

            ' Display statistics
            lblTotalOperations.Text = stats.TotalOperations.ToString()
            lblSuccessfulOps.Text = stats.SuccessfulOperations.ToString()
            lblFailedOps.Text = stats.FailedOperations.ToString()
            lblSuccessRate.Text = $"{stats.SuccessRate:F1}%"

            lblTotalFilesPatched.Text = stats.TotalFilesPatched.ToString()
            lblTotalFilesSkipped.Text = stats.TotalFilesSkipped.ToString()
            lblTotalFilesFailed.Text = stats.TotalFilesFailed.ToString()

            If stats.FirstOperationDate <> DateTime.MinValue Then
                lblFirstOperation.Text = stats.FirstOperationDate.ToString("yyyy-MM-dd HH:mm")
            Else
                lblFirstOperation.Text = "N/A"
            End If

            If stats.LastOperationDate <> DateTime.MinValue Then
                lblLastOperation.Text = stats.LastOperationDate.ToString("yyyy-MM-dd HH:mm")
            Else
                lblLastOperation.Text = "N/A"
            End If

            lblMostUsedSdk.Text = If(String.IsNullOrEmpty(stats.MostUsedSdk), "N/A", stats.MostUsedSdk)
            lblAverageDuration.Text = stats.AverageDuration.ToString()
            lblAverageFilesPerOp.Text = $"{stats.AverageFilesPerOperation:F1}"

            ' Color code success rate
            If stats.SuccessRate >= 90 Then
                lblSuccessRate.ForeColor = Color.Green
            ElseIf stats.SuccessRate >= 70 Then
                lblSuccessRate.ForeColor = Color.Orange
            Else
                lblSuccessRate.ForeColor = Color.Red
            End If

        Catch ex As Exception
            ' Show friendly error message
            ShowEmptyStatistics()
        End Try
    End Sub

    Private Sub ShowEmptyStatistics()
        lblTotalOperations.Text = "0"
        lblSuccessfulOps.Text = "0"
        lblFailedOps.Text = "0"
        lblSuccessRate.Text = "0%"
        lblTotalFilesPatched.Text = "0"
        lblTotalFilesSkipped.Text = "0"
        lblTotalFilesFailed.Text = "0"
        lblFirstOperation.Text = "N/A"
        lblLastOperation.Text = "N/A"
        lblMostUsedSdk.Text = "N/A"
        lblAverageDuration.Text = "00:00:00"
        lblAverageFilesPerOp.Text = "0"
        lblSuccessRate.ForeColor = Color.Gray
    End Sub

    Private Sub LoadRecentHistory(Optional limit As Integer = 100)
        Try
            ' Check if repository is available
            If _historyRepo Is Nothing Then
                lblHistoryCount.Text = "No operations recorded yet. Patch some games to see history!"
                lblHistoryCount.ForeColor = Color.Gray
                Return
            End If

            Dim history = _historyRepo.GetAll(limit)
            dgvHistory.DataSource = history

            If history.Count = 0 Then
                lblHistoryCount.Text = "No operations recorded yet. Patch some games to see history!"
                lblHistoryCount.ForeColor = Color.Orange
                lblHistoryCount.Font = New Font("Segoe UI", 11, FontStyle.Bold)

                ' Show message in grid area too
                dgvHistory.Visible = False
                ShowEmptyHistoryMessage()
            Else
                dgvHistory.Visible = True
                HideEmptyHistoryMessage()
                ' Color code rows
                For Each row As DataGridViewRow In dgvHistory.Rows
                    If row.Cells("Success").Value IsNot Nothing Then
                        Dim success = CBool(row.Cells("Success").Value)
                        If Not success Then
                            row.DefaultCellStyle.BackColor = Color.LightPink
                        End If
                    End If
                Next

                lblHistoryCount.Text = $"Showing {history.Count} most recent operation(s)"
                lblHistoryCount.ForeColor = Color.Black
            End If

        Catch ex As Exception
            lblHistoryCount.Text = "No data available"
            lblHistoryCount.ForeColor = Color.Gray
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs)
        LoadStatistics()
        LoadRecentHistory()
        MessageBox.Show("Dashboard refreshed successfully", "Refresh", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub btnClearOldHistory_Click(sender As Object, e As EventArgs)
        Try
            Dim result = MessageBox.Show("Delete history older than 90 days?", "Clear Old History",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                Dim deleted = _historyRepo.DeleteOlderThan(90)
                MessageBox.Show($"Deleted {deleted} old record(s)", "Clear History",
                              MessageBoxButtons.OK, MessageBoxIcon.Information)
                LoadStatistics()
                LoadRecentHistory()
            End If

        Catch ex As Exception
            MessageBox.Show($"Failed to clear history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnClearAllHistory_Click(sender As Object, e As EventArgs)
        Try
            Dim result = MessageBox.Show("DELETE ALL HISTORY? This cannot be undone!", "Clear All History",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If result = DialogResult.Yes Then
                Dim success = _historyRepo.Clear()
                If success Then
                    MessageBox.Show("All history cleared successfully", "Clear History",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadStatistics()
                    LoadRecentHistory()
                Else
                    MessageBox.Show("Failed to clear history", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If

        Catch ex As Exception
            MessageBox.Show($"Failed to clear history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnExportHistory_Click(sender As Object, e As EventArgs)
        Try
            Dim sfd As New SaveFileDialog With {
                .Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                .Title = "Export History",
                .FileName = $"patching_history_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            }

            If sfd.ShowDialog() = DialogResult.OK Then
                ExportHistoryToCsv(sfd.FileName)
                MessageBox.Show($"History exported successfully to:{vbCrLf}{sfd.FileName}", "Export Complete",
                              MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show($"Failed to export history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportHistoryToCsv(filePath As String)
        Dim history = _historyRepo.GetAll(999999) ' Get all

        Using sw As New IO.StreamWriter(filePath)
            ' Header
            sw.WriteLine("Timestamp,OperationType,GameName,TargetSdk,TotalFiles,PatchedFiles,SkippedFiles,FailedFiles,Duration,Success,BackupPath,ErrorMessage")

            ' Data
            For Each entry In history
                sw.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}," &
                           $"{entry.OperationType}," &
                           $"""{entry.GameName}""," &
                           $"0x{entry.TargetSdk:X}," &
                           $"{entry.TotalFiles}," &
                           $"{entry.PatchedFiles}," &
                           $"{entry.SkippedFiles}," &
                           $"{entry.FailedFiles}," &
                           $"{entry.Duration}," &
                           $"{entry.Success}," &
                           $"""{entry.BackupPath}""," &
                           $"""{If(entry.ErrorMessage, "").Replace("""", "''")}""")
            Next
        End Using
    End Sub

    Private Sub dgvHistory_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then
            Try
                Dim entry = CType(dgvHistory.Rows(e.RowIndex).DataBoundItem, PatchingHistoryEntry)
                ShowHistoryDetails(entry)
            Catch ex As Exception
                ' Ignore
            End Try
        End If
    End Sub

    Private Sub ShowEmptyHistoryMessage()
        If pnlEmptyMessage Is Nothing Then
            ' Create empty message panel
            pnlEmptyMessage = New Panel With {
                .Location = New Point(10, 225),
                .Size = New Size(1170, 400),
                .BackColor = Color.FromArgb(245, 245, 245),
                .BorderStyle = BorderStyle.FixedSingle
            }

            lblEmptyMessage = New Label With {
                .Text = "📊 No Operations Yet" & vbCrLf & vbCrLf &
                       "Start patching games to see statistics and history here!" & vbCrLf & vbCrLf &
                       "All operations will be automatically tracked.",
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .ForeColor = Color.Gray,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Dock = DockStyle.Fill
            }

            pnlEmptyMessage.Controls.Add(lblEmptyMessage)
            Me.Controls.Add(pnlEmptyMessage)
            pnlEmptyMessage.BringToFront()
        End If

        pnlEmptyMessage.Visible = True
    End Sub

    Private Sub HideEmptyHistoryMessage()
        If pnlEmptyMessage IsNot Nothing Then
            pnlEmptyMessage.Visible = False
        End If
    End Sub

    Private Sub ShowHistoryDetails(entry As PatchingHistoryEntry)
        Dim details = $"Operation Details{vbCrLf}{vbCrLf}" &
                     $"ID: {entry.Id}{vbCrLf}" &
                     $"Timestamp: {entry.Timestamp:yyyy-MM-dd HH:mm:ss}{vbCrLf}" &
                     $"Type: {entry.OperationType}{vbCrLf}" &
                     $"Game: {entry.GameName}{vbCrLf}" &
                     $"Source: {entry.SourcePath}{vbCrLf}" &
                     $"Target SDK: 0x{entry.TargetSdk:X}{vbCrLf}{vbCrLf}" &
                     $"Files:{vbCrLf}" &
                     $"  Total: {entry.TotalFiles}{vbCrLf}" &
                     $"  Patched: {entry.PatchedFiles}{vbCrLf}" &
                     $"  Skipped: {entry.SkippedFiles}{vbCrLf}" &
                     $"  Failed: {entry.FailedFiles}{vbCrLf}{vbCrLf}" &
                     $"Duration: {entry.Duration}{vbCrLf}" &
                     $"Success: {entry.Success}{vbCrLf}" &
                     $"Backup: {If(String.IsNullOrEmpty(entry.BackupPath), "None", entry.BackupPath)}{vbCrLf}" &
                     $"Machine: {entry.MachineName}{vbCrLf}" &
                     $"User: {entry.UserName}{vbCrLf}" &
                     $"App Version: {entry.AppVersion}"

        If Not String.IsNullOrEmpty(entry.ErrorMessage) Then
            details &= $"{vbCrLf}{vbCrLf}Error: {entry.ErrorMessage}"
        End If

        MessageBox.Show(details, "Operation Details", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub InitializeComponent()
        Me.dgvHistory = New DataGridView()
        Me.lblTotalOperations = New Label()
        Me.lblSuccessfulOps = New Label()
        Me.lblFailedOps = New Label()
        Me.lblSuccessRate = New Label()
        Me.lblTotalFilesPatched = New Label()
        Me.lblTotalFilesSkipped = New Label()
        Me.lblTotalFilesFailed = New Label()
        Me.lblFirstOperation = New Label()
        Me.lblLastOperation = New Label()
        Me.lblMostUsedSdk = New Label()
        Me.lblAverageDuration = New Label()
        Me.lblAverageFilesPerOp = New Label()
        Me.lblHistoryCount = New Label()
        Me.btnRefresh = New Button()
        Me.btnClearOldHistory = New Button()
        Me.btnClearAllHistory = New Button()
        Me.btnExportHistory = New Button()
        Me.btnClose = New Button()

        CType(Me.dgvHistory, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()

        ' DashboardForm
        Me.ClientSize = New Size(1200, 700)
        Me.Text = "Patching Dashboard - History & Statistics"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(1000, 600)

        ' Statistics Panel
        Dim pnlStats As New Panel With {
            .Location = New Point(10, 10),
            .Size = New Size(1170, 180),
            .BorderStyle = BorderStyle.FixedSingle
        }

        Dim lblStatsTitle As New Label With {
            .Text = "📊 Statistics",
            .Location = New Point(10, 5),
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .AutoSize = True
        }
        pnlStats.Controls.Add(lblStatsTitle)

        ' Row 1: Operations
        AddStatLabel(pnlStats, "Total Operations:", 10, 35)
        Me.lblTotalOperations.Location = New Point(150, 35)
        Me.lblTotalOperations.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        Me.lblTotalOperations.AutoSize = True
        pnlStats.Controls.Add(Me.lblTotalOperations)

        AddStatLabel(pnlStats, "Successful:", 250, 35)
        Me.lblSuccessfulOps.Location = New Point(350, 35)
        Me.lblSuccessfulOps.Font = New Font("Segoe UI", 10)
        Me.lblSuccessfulOps.ForeColor = Color.Green
        Me.lblSuccessfulOps.AutoSize = True
        pnlStats.Controls.Add(Me.lblSuccessfulOps)

        AddStatLabel(pnlStats, "Failed:", 450, 35)
        Me.lblFailedOps.Location = New Point(510, 35)
        Me.lblFailedOps.Font = New Font("Segoe UI", 10)
        Me.lblFailedOps.ForeColor = Color.Red
        Me.lblFailedOps.AutoSize = True
        pnlStats.Controls.Add(Me.lblFailedOps)

        AddStatLabel(pnlStats, "Success Rate:", 610, 35)
        Me.lblSuccessRate.Location = New Point(720, 35)
        Me.lblSuccessRate.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        Me.lblSuccessRate.AutoSize = True
        pnlStats.Controls.Add(Me.lblSuccessRate)

        ' Row 2: Files
        AddStatLabel(pnlStats, "Total Files Patched:", 10, 65)
        Me.lblTotalFilesPatched.Location = New Point(150, 65)
        Me.lblTotalFilesPatched.Font = New Font("Segoe UI", 10)
        Me.lblTotalFilesPatched.AutoSize = True
        pnlStats.Controls.Add(Me.lblTotalFilesPatched)

        AddStatLabel(pnlStats, "Skipped:", 250, 65)
        Me.lblTotalFilesSkipped.Location = New Point(350, 65)
        Me.lblTotalFilesSkipped.Font = New Font("Segoe UI", 10)
        Me.lblTotalFilesSkipped.AutoSize = True
        pnlStats.Controls.Add(Me.lblTotalFilesSkipped)

        AddStatLabel(pnlStats, "Failed:", 450, 65)
        Me.lblTotalFilesFailed.Location = New Point(510, 65)
        Me.lblTotalFilesFailed.Font = New Font("Segoe UI", 10)
        Me.lblTotalFilesFailed.AutoSize = True
        pnlStats.Controls.Add(Me.lblTotalFilesFailed)

        AddStatLabel(pnlStats, "Avg Files/Op:", 610, 65)
        Me.lblAverageFilesPerOp.Location = New Point(720, 65)
        Me.lblAverageFilesPerOp.Font = New Font("Segoe UI", 10)
        Me.lblAverageFilesPerOp.AutoSize = True
        pnlStats.Controls.Add(Me.lblAverageFilesPerOp)

        ' Row 3: Dates and SDK
        AddStatLabel(pnlStats, "First Operation:", 10, 95)
        Me.lblFirstOperation.Location = New Point(150, 95)
        Me.lblFirstOperation.Font = New Font("Segoe UI", 9)
        Me.lblFirstOperation.AutoSize = True
        pnlStats.Controls.Add(Me.lblFirstOperation)

        AddStatLabel(pnlStats, "Last Operation:", 10, 115)
        Me.lblLastOperation.Location = New Point(150, 115)
        Me.lblLastOperation.Font = New Font("Segoe UI", 9)
        Me.lblLastOperation.AutoSize = True
        pnlStats.Controls.Add(Me.lblLastOperation)

        AddStatLabel(pnlStats, "Most Used SDK:", 450, 95)
        Me.lblMostUsedSdk.Location = New Point(570, 95)
        Me.lblMostUsedSdk.Font = New Font("Segoe UI", 9)
        Me.lblMostUsedSdk.AutoSize = True
        pnlStats.Controls.Add(Me.lblMostUsedSdk)

        AddStatLabel(pnlStats, "Avg Duration:", 450, 115)
        Me.lblAverageDuration.Location = New Point(570, 115)
        Me.lblAverageDuration.Font = New Font("Segoe UI", 9)
        Me.lblAverageDuration.AutoSize = True
        pnlStats.Controls.Add(Me.lblAverageDuration)

        ' Buttons
        Me.btnRefresh.Location = New Point(10, 145)
        Me.btnRefresh.Size = New Size(100, 25)
        Me.btnRefresh.Text = "🔄 Refresh"
        AddHandler Me.btnRefresh.Click, AddressOf btnRefresh_Click
        pnlStats.Controls.Add(Me.btnRefresh)

        Me.btnExportHistory.Location = New Point(120, 145)
        Me.btnExportHistory.Size = New Size(120, 25)
        Me.btnExportHistory.Text = "💾 Export CSV"
        AddHandler Me.btnExportHistory.Click, AddressOf btnExportHistory_Click
        pnlStats.Controls.Add(Me.btnExportHistory)

        Me.btnClearOldHistory.Location = New Point(250, 145)
        Me.btnClearOldHistory.Size = New Size(140, 25)
        Me.btnClearOldHistory.Text = "🗑️ Clear Old (90d)"
        AddHandler Me.btnClearOldHistory.Click, AddressOf btnClearOldHistory_Click
        pnlStats.Controls.Add(Me.btnClearOldHistory)

        Me.btnClearAllHistory.Location = New Point(400, 145)
        Me.btnClearAllHistory.Size = New Size(120, 25)
        Me.btnClearAllHistory.Text = "⚠️ Clear All"
        Me.btnClearAllHistory.BackColor = Color.LightCoral
        AddHandler Me.btnClearAllHistory.Click, AddressOf btnClearAllHistory_Click
        pnlStats.Controls.Add(Me.btnClearAllHistory)

        Me.Controls.Add(pnlStats)

        ' History Grid
        Dim lblHistoryTitle As New Label With {
            .Text = "📜 Recent History",
            .Location = New Point(10, 200),
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .AutoSize = True
        }
        Me.Controls.Add(lblHistoryTitle)

        Me.lblHistoryCount.Location = New Point(200, 202)
        Me.lblHistoryCount.Font = New Font("Segoe UI", 10, FontStyle.Italic)
        Me.lblHistoryCount.AutoSize = True
        Me.lblHistoryCount.ForeColor = Color.Gray
        Me.Controls.Add(Me.lblHistoryCount)

        Me.dgvHistory.Location = New Point(10, 225)
        Me.dgvHistory.Size = New Size(1170, 400)
        Me.dgvHistory.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(Me.dgvHistory)

        ' Close Button
        Me.btnClose.Location = New Point(1080, 635)
        Me.btnClose.Size = New Size(100, 30)
        Me.btnClose.Text = "Close"
        Me.btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler Me.btnClose.Click, AddressOf btnClose_Click
        Me.Controls.Add(Me.btnClose)

        CType(Me.dgvHistory, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub AddStatLabel(parent As Panel, text As String, x As Integer, y As Integer)
        Dim lbl As New Label With {
            .Text = text,
            .Location = New Point(x, y),
            .Font = New Font("Segoe UI", 9),
            .AutoSize = True
        }
        parent.Controls.Add(lbl)
    End Sub

    Private dgvHistory As DataGridView
    Private pnlEmptyMessage As Panel
    Private lblEmptyMessage As Label
    Private lblTotalOperations As Label
    Private lblSuccessfulOps As Label
    Private lblFailedOps As Label
    Private lblSuccessRate As Label
    Private lblTotalFilesPatched As Label
    Private lblTotalFilesSkipped As Label
    Private lblTotalFilesFailed As Label
    Private lblFirstOperation As Label
    Private lblLastOperation As Label
    Private lblMostUsedSdk As Label
    Private lblAverageDuration As Label
    Private lblAverageFilesPerOp As Label
    Private lblHistoryCount As Label
    Private WithEvents btnRefresh As Button
    Private WithEvents btnClearOldHistory As Button
    Private WithEvents btnClearAllHistory As Button
    Private WithEvents btnExportHistory As Button
    Private WithEvents btnClose As Button
End Class
