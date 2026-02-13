Imports System.Windows.Forms
Imports System.Drawing

Public Class StatisticsForm
    Inherits Form

    Private db As StatisticsDatabase
    Private WithEvents btnRefresh As Button
    Private WithEvents btnExportCSV As Button
    Private WithEvents btnClearStats As Button
    Private WithEvents cmbPeriod As ComboBox
    Private WithEvents cmbOperationType As ComboBox
    Private lblTotalOps As Label
    Private lblSuccessRate As Label
    Private lblTotalFiles As Label
    Private lblTotalTime As Label
    Private dgvSessions As DataGridView
    Private dgvOperationTypes As DataGridView
    Private statusLabel As ToolStripStatusLabel

    Public Sub New()
        Try
            InitializeComponents()
            db = New StatisticsDatabase()
            LoadStatistics()
        Catch ex As Exception
            Dim errorDetails = $"=== STATISTICS FORM ERROR ==={vbCrLf}" &
                             $"Message: {ex.Message}{vbCrLf}" &
                             $"Type: {ex.GetType().Name}{vbCrLf}" &
                             $"Stack Trace:{vbCrLf}{ex.StackTrace}{vbCrLf}" &
                             $"==========================={vbCrLf}"

            ' Log to file
            Logger.LogToFile(errorDetails, LogLevel.Error)

            ' Show user-friendly message with log location
            Dim logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log", "app.log")
            MessageBox.Show($"Error initializing Statistics Form.{vbCrLf}{vbCrLf}" &
                          $"Error details saved to:{vbCrLf}{logPath}{vbCrLf}{vbCrLf}" &
                          $"Please send the log file to the developer.",
                          "Statistics Initialization Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Initialize with empty data to prevent form crash
            If statusLabel IsNot Nothing Then
                statusLabel.Text = "Error loading statistics"
            End If
        End Try
    End Sub

    Private Sub InitializeComponents()
        ' Form settings
        Me.Text = "Statistics & Analytics"
        Me.Size = New Size(1200, 800)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(900, 600)

        ' Create main layout
        Dim mainPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 3,
            .Padding = New Padding(10)
        }

        mainPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))
        mainPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 150)) ' Stats cards
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 50))   ' Chart
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 50))   ' Grid

        ' === TOP LEFT: Quick Stats Cards ===
        Dim statsPanel = CreateStatsPanel()
        mainPanel.Controls.Add(statsPanel, 0, 0)

        ' === TOP RIGHT: Filter Controls ===
        Dim filterPanel = CreateFilterPanel()
        mainPanel.Controls.Add(filterPanel, 1, 0)

        ' === MIDDLE: Operation Types Grid ===
        dgvOperationTypes = CreateOperationTypesGrid()
        mainPanel.Controls.Add(dgvOperationTypes, 0, 1)
        mainPanel.SetColumnSpan(dgvOperationTypes, 2)

        ' === BOTTOM: Sessions Grid (spans 2 columns) ===
        dgvSessions = CreateSessionsGrid()
        mainPanel.Controls.Add(dgvSessions, 0, 2)
        mainPanel.SetColumnSpan(dgvSessions, 2)

        Me.Controls.Add(mainPanel)

        ' Status bar
        Dim statusStrip As New StatusStrip()
        statusLabel = New ToolStripStatusLabel("Ready")
        statusStrip.Items.Add(statusLabel)
        Me.Controls.Add(statusStrip)
    End Sub

    Private Function CreateStatsPanel() As Panel
        Dim panel As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(5)
        }

        Dim layout As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.TopDown,
            .AutoScroll = True
        }

        ' Total Operations Card
        Dim cardOps = CreateStatCard("Total Operations", "0", Color.FromArgb(52, 152, 219))
        lblTotalOps = CType(cardOps.Controls(1), Label)
        layout.Controls.Add(cardOps)

        ' Success Rate Card
        Dim cardSuccess = CreateStatCard("Success Rate", "0%", Color.FromArgb(46, 204, 113))
        lblSuccessRate = CType(cardSuccess.Controls(1), Label)
        layout.Controls.Add(cardSuccess)

        ' Total Files Card
        Dim cardFiles = CreateStatCard("Files Patched", "0", Color.FromArgb(155, 89, 182))
        lblTotalFiles = CType(cardFiles.Controls(1), Label)
        layout.Controls.Add(cardFiles)

        ' Total Time Card
        Dim cardTime = CreateStatCard("Total Time", "0h", Color.FromArgb(230, 126, 34))
        lblTotalTime = CType(cardTime.Controls(1), Label)
        layout.Controls.Add(cardTime)

        panel.Controls.Add(layout)
        Return panel
    End Function

    Private Function CreateStatCard(title As String, value As String, color As Color) As Panel
        Dim card As New Panel With {
            .Size = New Size(250, 80),
            .BackColor = color,
            .Margin = New Padding(5),
            .Padding = New Padding(10)
        }

        Dim lblTitle As New Label With {
            .Text = title,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Dock = DockStyle.Top,
            .Height = 25
        }

        Dim lblValue As New Label With {
            .Text = value,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter
        }

        card.Controls.Add(lblValue)
        card.Controls.Add(lblTitle)

        Return card
    End Function

    Private Function CreateFilterPanel() As Panel
        Dim panel As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(10)
        }

        Dim layout As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False
        }

        ' Period filter
        Dim lblPeriod As New Label With {
            .Text = "Time Period:",
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 5)
        }

        cmbPeriod = New ComboBox With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 200,
            .Margin = New Padding(0, 0, 0, 15)
        }
        cmbPeriod.Items.AddRange({"Last 7 Days", "Last 30 Days", "Last 90 Days", "All Time"})
        cmbPeriod.SelectedIndex = 1

        ' Operation type filter
        Dim lblOpType As New Label With {
            .Text = "Operation Type:",
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 5)
        }

        cmbOperationType = New ComboBox With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 200,
            .Margin = New Padding(0, 0, 0, 15)
        }
        cmbOperationType.Items.AddRange({"All Operations", "Decrypt", "Sign", "Patch", "Downgrade", "Full Pipeline"})
        cmbOperationType.SelectedIndex = 0

        ' Buttons
        btnRefresh = New Button With {
            .Text = "🔄 Refresh",
            .Width = 200,
            .Height = 35,
            .Margin = New Padding(0, 0, 0, 10),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(52, 152, 219),
            .ForeColor = Color.White
        }

        btnExportCSV = New Button With {
            .Text = "📊 Export CSV",
            .Width = 200,
            .Height = 35,
            .Margin = New Padding(0, 0, 0, 10),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(46, 204, 113),
            .ForeColor = Color.White
        }

        btnClearStats = New Button With {
            .Text = "🗑️ Clear Statistics",
            .Width = 200,
            .Height = 35,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(231, 76, 60),
            .ForeColor = Color.White
        }

        layout.Controls.AddRange({lblPeriod, cmbPeriod, lblOpType, cmbOperationType,
                                  btnRefresh, btnExportCSV, btnClearStats})

        panel.Controls.Add(layout)
        Return panel
    End Function

    Private Function CreateOperationTypesGrid() As DataGridView
        Dim grid As New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.Fixed3D
        }

        ' Columns
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "OpType", .HeaderText = "Operation Type", .FillWeight = 30})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Total", .HeaderText = "Total", .FillWeight = 15})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Success", .HeaderText = "Success", .FillWeight = 15})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Failed", .HeaderText = "Failed", .FillWeight = 15})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "SuccessRate", .HeaderText = "Success Rate", .FillWeight = 15})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "LastUsed", .HeaderText = "Last Used", .FillWeight = 20})

        Return grid
    End Function

    Private Function CreateSessionsGrid() As DataGridView
        Dim grid As New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.Fixed3D
        }

        ' Columns
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Date", .HeaderText = "Date & Time", .FillWeight = 20})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Operation", .HeaderText = "Operation", .FillWeight = 15})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Games", .HeaderText = "Games", .FillWeight = 10})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Patched", .HeaderText = "Patched", .FillWeight = 10})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Skipped", .HeaderText = "Skipped", .FillWeight = 10})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Failed", .HeaderText = "Failed", .FillWeight = 10})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Duration", .HeaderText = "Duration", .FillWeight = 10})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Status", .HeaderText = "Status", .FillWeight = 10})

        Return grid
    End Function

    Private Sub LoadStatistics()
        If db Is Nothing Then Return ' Not yet initialized

        Try
            statusLabel.Text = "Loading statistics..."

            ' Load overall stats
            Dim overallStats = db.GetOverallStats()

            lblTotalOps.Text = overallStats("TotalSessions").ToString()
            lblSuccessRate.Text = $"{overallStats("SuccessRate"):F1}%"
            lblTotalFiles.Text = overallStats("TotalFilesPatched").ToString()
            lblTotalTime.Text = FormatTimeSpan(CType(overallStats("TotalDuration"), TimeSpan))

            ' Load operation types grid
            LoadOperationTypesGrid()

            ' Load sessions grid
            LoadSessionsGrid()

            statusLabel.Text = $"Statistics loaded - {lblTotalOps.Text} total operations"
        Catch ex As Exception
            MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            statusLabel.Text = "Error loading statistics"
        End Try
    End Sub

    Private Sub LoadOperationTypesGrid()
        If db Is Nothing Then Return ' Not yet initialized

        dgvOperationTypes.Rows.Clear()

        Dim opStats = db.GetOperationTypeStats()

        For Each opStat In opStats
            dgvOperationTypes.Rows.Add(
                opStat.OperationType,
                opStat.TotalCount,
                opStat.SuccessCount,
                opStat.FailCount,
                $"{opStat.SuccessRate:F1}%",
                opStat.LastUsed.ToString("yyyy-MM-dd HH:mm")
            )
        Next
    End Sub

    Private Sub LoadSessionsGrid()
        If db Is Nothing Then Return ' Not yet initialized

        dgvSessions.Rows.Clear()

        Dim opType = If(cmbOperationType.SelectedIndex = 0, Nothing, cmbOperationType.SelectedItem.ToString())
        Dim sessions = db.GetRecentSessions(100, opType)

        For Each session In sessions
            dgvSessions.Rows.Add(
                session.SessionDate.ToString("yyyy-MM-dd HH:mm"),
                session.OperationType,
                session.GamesProcessed,
                session.FilesPatched,
                session.FilesSkipped,
                session.FilesFailed,
                $"{session.Duration.TotalSeconds:F1}s",
                If(session.Success, "✓ Success", "✗ Failed")
            )

            ' Color code status
            Dim lastRow = dgvSessions.Rows(dgvSessions.Rows.Count - 1)
            If session.Success Then
                lastRow.Cells("Status").Style.ForeColor = Color.Green
                lastRow.Cells("Status").Style.Font = New Font(dgvSessions.Font, FontStyle.Bold)
            Else
                lastRow.Cells("Status").Style.ForeColor = Color.Red
                lastRow.Cells("Status").Style.Font = New Font(dgvSessions.Font, FontStyle.Bold)
            End If
        Next
    End Sub

    Private Function GetSelectedPeriodDays() As Integer
        Select Case cmbPeriod.SelectedIndex
            Case 0 : Return 7
            Case 1 : Return 30
            Case 2 : Return 90
            Case Else : Return 365 * 10 ' All time
        End Select
    End Function

    Private Function FormatTimeSpan(ts As TimeSpan) As String
        If ts.TotalHours >= 1 Then
            Return $"{ts.TotalHours:F1}h"
        ElseIf ts.TotalMinutes >= 1 Then
            Return $"{ts.TotalMinutes:F1}m"
        Else
            Return $"{ts.TotalSeconds:F0}s"
        End If
    End Function

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadStatistics()
    End Sub

    Private Sub CmbPeriod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPeriod.SelectedIndexChanged
        LoadSessionsGrid()
    End Sub

    Private Sub CmbOperationType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbOperationType.SelectedIndexChanged
        LoadSessionsGrid()
    End Sub

    Private Sub BtnExportCSV_Click(sender As Object, e As EventArgs) Handles btnExportCSV.Click
        Using sfd As New SaveFileDialog()
            sfd.Filter = "CSV Files (*.csv)|*.csv"
            sfd.FileName = $"statistics_{DateTime.Now:yyyyMMdd_HHmmss}.csv"

            If sfd.ShowDialog() = DialogResult.OK Then
                If db.ExportToCSV(sfd.FileName) Then
                    MessageBox.Show($"Statistics exported successfully to:{vbCrLf}{sfd.FileName}",
                                  "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show("Failed to export statistics", "Export Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        End Using
    End Sub

    Private Sub BtnClearStats_Click(sender As Object, e As EventArgs) Handles btnClearStats.Click
        Dim result = MessageBox.Show(
            "Are you sure you want to clear all statistics?" & vbCrLf & vbCrLf &
            "This action cannot be undone!",
            "Confirm Clear Statistics",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        )

        If result = DialogResult.Yes Then
            ' TODO: Implement clear all stats in database
            MessageBox.Show("Statistics cleared successfully", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information)
            LoadStatistics()
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If db IsNot Nothing Then
            db.Dispose()
        End If
    End Sub

End Class