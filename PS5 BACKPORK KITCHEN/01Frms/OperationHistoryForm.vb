Imports System.IO
Imports System.Windows.Forms
Imports Newtonsoft.Json

Public Class OperationHistoryForm
    Inherits Form

    ' -----------------------------
    ' Data model (simple & stable)
    ' -----------------------------
    Private Class OperationHistoryItem
        Public Timestamp As DateTime
        Public GameName As String
        Public SdkVersion As String
        Public FilesPatched As Integer
        Public TotalFiles As Integer
        Public DurationSeconds As Double
        Public Success As Boolean
    End Class

    ' -----------------------------
    ' UI controls
    ' -----------------------------
    Private dgv As DataGridView

    Private cmbFilter As ToolStripComboBox
    Private lblStatus As ToolStripStatusLabel

    Private ReadOnly ReportsPath As String =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports")

    ' -----------------------------
    ' Constructor
    ' -----------------------------
    Public Sub New()
        Text = "Operation History"
        StartPosition = FormStartPosition.CenterParent
        Width = 900
        Height = 600

        BuildUI()
        LoadHistory()
    End Sub

    ' -----------------------------
    ' UI creation
    ' -----------------------------
    Private Sub BuildUI()

        ' ToolStrip
        Dim ts As New ToolStrip()

        Dim btnRefresh As New ToolStripButton("🔄 Refresh")
        Dim btnExport As New ToolStripButton("📤 Export CSV")
        Dim btnClear As New ToolStripButton("🗑 Clear")

        AddHandler btnRefresh.Click, Sub() LoadHistory(cmbFilter.Text)
        AddHandler btnExport.Click, AddressOf ExportCsv
        AddHandler btnClear.Click, AddressOf ClearHistory

        cmbFilter = New ToolStripComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbFilter.Items.AddRange(
            {"All", "Success Only", "Failed Only", "Today", "Last 7 Days"}
        )
        cmbFilter.SelectedIndex = 0
        AddHandler cmbFilter.SelectedIndexChanged,
            Sub() LoadHistory(cmbFilter.Text)

        ts.Items.AddRange({
            btnRefresh,
            btnExport,
            btnClear,
            New ToolStripSeparator(),
            New ToolStripLabel("Filter:"),
            cmbFilter
        })

        ' DataGridView
        dgv = New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        }

        dgv.Columns.Add("Timestamp", "Timestamp")
        dgv.Columns.Add("GameName", "Game")
        dgv.Columns.Add("SDK", "SDK")
        dgv.Columns.Add("Files", "Files")
        dgv.Columns.Add("Duration", "Duration")
        dgv.Columns.Add("Result", "Result")

        ' StatusStrip
        Dim ss As New StatusStrip()
        lblStatus = New ToolStripStatusLabel("Ready")
        ss.Items.Add(lblStatus)

        Controls.Add(dgv)
        Controls.Add(ts)
        Controls.Add(ss)

        ts.Dock = DockStyle.Top
        ss.Dock = DockStyle.Bottom
    End Sub

    ' -----------------------------
    ' Load history
    ' -----------------------------
    Private Sub LoadHistory(Optional filter As String = "All")

        dgv.Rows.Clear()

        If Not Directory.Exists(ReportsPath) Then
            lblStatus.Text = "No history folder found"
            Return
        End If

        Dim files = Directory.GetFiles(ReportsPath, "report_*.json").
            OrderByDescending(Function(f) f).
            Take(200)

        Dim loaded As Integer = 0

        For Each file In files
            Try
                Dim json = System.IO.File.ReadAllText(file)
                Dim item = JsonConvert.DeserializeObject(Of OperationHistoryItem)(json)

                If item Is Nothing Then Continue For
                If Not PassFilter(item, filter) Then Continue For

                Dim resultText = If(item.Success, "✓ Success", "✗ Failed")
                Dim rowIndex = dgv.Rows.Add(
                    item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.GameName,
                    item.SdkVersion,
                    $"{item.FilesPatched}/{item.TotalFiles}",
                    FormatDuration(item.DurationSeconds),
                    resultText
                )

                dgv.Rows(rowIndex).Cells("Result").Style.ForeColor =
                    If(item.Success, Color.Green, Color.Red)

                loaded += 1
            Catch
                ' skip invalid file
            End Try
        Next

        lblStatus.Text =
            If(loaded = 0,
               "No operation history found",
               $"Loaded {loaded} operation(s)")
    End Sub

    ' -----------------------------
    ' Filters
    ' -----------------------------
    Private Function PassFilter(
    item As OperationHistoryItem,
    filter As String
) As Boolean

        Select Case filter
            Case "Success Only"
                Return item.Success
            Case "Failed Only"
                Return Not item.Success
            Case "Today"
                Return item.Timestamp.Date = Date.Today
            Case "Last 7 Days"
                Return item.Timestamp >= Date.Now.AddDays(-7)
            Case Else
                Return True
        End Select

    End Function

    ' -----------------------------
    ' Helpers
    ' -----------------------------
    Private Function FormatDuration(seconds As Double) As String
        Dim ts = TimeSpan.FromSeconds(seconds)
        If ts.TotalSeconds < 60 Then
            Return $"{ts.TotalSeconds:F1}s"
        ElseIf ts.TotalMinutes < 60 Then
            Return $"{ts.TotalMinutes:F1}m"
        Else
            Return $"{ts.TotalHours:F1}h"
        End If
    End Function

    ' -----------------------------
    ' Export
    ' -----------------------------
    Private Sub ExportCsv(sender As Object, e As EventArgs)
        If dgv.Rows.Count = 0 Then Return

        Using sfd As New SaveFileDialog With {
            .Filter = "CSV Files|*.csv",
            .FileName = $"operation_history_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        }
            If sfd.ShowDialog() <> DialogResult.OK Then Return

            Using sw As New StreamWriter(sfd.FileName)
                sw.WriteLine("Timestamp,Game,SDK,Files,Duration,Result")
                For Each row As DataGridViewRow In dgv.Rows
                    sw.WriteLine(String.Join(",",
                        row.Cells("Timestamp").Value,
                        $"""{row.Cells("GameName").Value}""",
                        row.Cells("SDK").Value,
                        row.Cells("Files").Value,
                        row.Cells("Duration").Value,
                        row.Cells("Result").Value
                    ))
                Next
            End Using
        End Using
    End Sub

    ' -----------------------------
    ' Clear history
    ' -----------------------------
    Private Sub ClearHistory(sender As Object, e As EventArgs)
        If MessageBox.Show(
            "Clear all operation history?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) <> DialogResult.Yes Then Return

        If Directory.Exists(ReportsPath) Then
            For Each f In Directory.GetFiles(ReportsPath, "report_*.json")
                File.Delete(f)
            Next
        End If

        LoadHistory()
    End Sub

End Class

Public Class OperationHistoryItem
    Public Timestamp As DateTime
    Public GameName As String
    Public SdkVersion As String
    Public FilesPatched As Integer
    Public TotalFiles As Integer
    Public DurationSeconds As Double
    Public Success As Boolean
End Class