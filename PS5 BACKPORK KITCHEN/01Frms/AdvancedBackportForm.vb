Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' Advanced Backport Pipeline — GUI wrapper for scripts/advanced_backport.py.
'''
''' Layout:
'''   Top pane   : configuration panel (folder, firmware versions, options)
'''   Bottom pane: color-coded log RichTextBox
'''   ToolStrip  : Run, Stop, Clear, Open Scripts Folder, UDP Log Viewer
''' </summary>
Public Class AdvancedBackportForm
    Inherits Form

    ' ---------------------------------------------------------------------------
    ' UI Controls
    ' ---------------------------------------------------------------------------

    Private WithEvents toolStrip      As ToolStrip
    Private WithEvents btnRun         As ToolStripButton
    Private WithEvents btnStop        As ToolStripButton
    Private WithEvents btnClear       As ToolStripButton
    Private WithEvents btnOpenScripts As ToolStripButton
    Private WithEvents btnUdpViewer   As ToolStripButton

    Private splitContainer As SplitContainer

    ' Config panel
    Private grpConfig        As GroupBox
    Private lblGameFolder    As Label
    Private txtGameFolder    As TextBox
    Private WithEvents btnBrowseFolder As Button
    Private lblFwCurrent     As Label
    Private cmbFwCurrent     As ComboBox
    Private lblFwTarget      As Label
    Private cmbFwTarget      As ComboBox
    Private chkApplyBps      As CheckBox
    Private chkStubMissing   As CheckBox
    Private chkResign        As CheckBox

    ' Log panel
    Private rtbLog As RichTextBox

    ' Status strip
    Private statusStrip As StatusStrip
    Private lblStatus   As ToolStripStatusLabel

    ' Runtime
    Private _cts As CancellationTokenSource
    Private _udpViewer As UdpLogViewerForm

    ' ---------------------------------------------------------------------------
    ' Constructor
    ' ---------------------------------------------------------------------------

    Public Sub New()
        InitializeComponent()
    End Sub

    ' ---------------------------------------------------------------------------
    ' Form initialization
    ' ---------------------------------------------------------------------------

    Private Sub InitializeComponent()
        Me.Text = "Advanced Backport Pipeline"
        Me.Size = New Size(900, 620)
        Me.MinimumSize = New Size(700, 480)
        Me.StartPosition = FormStartPosition.CenterParent

        ' --- ToolStrip ---
        toolStrip = New ToolStrip()
        btnRun = New ToolStripButton("▶ Run") With {
            .ToolTipText = "Start backport pipeline",
            .ForeColor = Color.DarkGreen,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        btnStop = New ToolStripButton("■ Stop") With {
            .ToolTipText = "Stop running pipeline",
            .Enabled = False,
            .ForeColor = Color.DarkRed,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        btnClear = New ToolStripButton("✕ Clear") With {.ToolTipText = "Clear log"}
        btnOpenScripts = New ToolStripButton("📂 Scripts") With {.ToolTipText = "Open scripts/ folder in Explorer"}
        btnUdpViewer = New ToolStripButton("📡 UDP Log Viewer") With {
            .ToolTipText = "Open UDP log viewer (listens on port 9090 for payload logs)"
        }
        toolStrip.Items.AddRange({
            btnRun, btnStop,
            New ToolStripSeparator(),
            btnClear, btnOpenScripts,
            New ToolStripSeparator(),
            btnUdpViewer
        })

        ' --- Config GroupBox ---
        grpConfig = New GroupBox() With {
            .Text = "Pipeline Configuration",
            .Dock = DockStyle.Fill,
            .Padding = New Padding(8)
        }

        lblGameFolder = New Label() With {
            .Text = "Game Folder:",
            .AutoSize = True,
            .Location = New Point(10, 26)
        }
        txtGameFolder = New TextBox() With {
            .Location = New Point(110, 23),
            .Width = 430,
            .ReadOnly = True
        }
        btnBrowseFolder = New Button() With {
            .Text = "Browse…",
            .Location = New Point(548, 21),
            .Width = 75,
            .Height = 24
        }

        lblFwCurrent = New Label() With {
            .Text = "Current FW:",
            .AutoSize = True,
            .Location = New Point(10, 58)
        }
        cmbFwCurrent = New ComboBox() With {
            .Location = New Point(110, 54),
            .Width = 100,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        lblFwTarget = New Label() With {
            .Text = "Target FW:",
            .AutoSize = True,
            .Location = New Point(228, 58)
        }
        cmbFwTarget = New ComboBox() With {
            .Location = New Point(320, 54),
            .Width = 100,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }

        Dim fwVersions As String() = {
            "1.00", "1.02", "1.05", "1.10", "1.14",
            "2.00", "2.20", "2.25", "2.26", "2.50",
            "3.00", "3.20", "3.21", "4.00", "4.02",
            "4.50", "4.51", "5.00", "5.02", "5.10",
            "5.25", "6.00", "6.02", "6.50", "7.00",
            "7.01", "7.02", "7.04", "7.20", "7.55",
            "7.61", "8.00", "8.52", "9.00", "9.03",
            "9.60", "10.00", "10.01", "10.50", "11.00"
        }
        cmbFwCurrent.Items.AddRange(fwVersions)
        cmbFwTarget.Items.AddRange(fwVersions)
        cmbFwCurrent.SelectedItem = "10.01"
        cmbFwTarget.SelectedItem = "7.61"

        chkApplyBps = New CheckBox() With {
            .Text = "Apply BPS patches",
            .AutoSize = True,
            .Location = New Point(10, 90),
            .Checked = True
        }
        chkStubMissing = New CheckBox() With {
            .Text = "Stub missing symbols (ARM64 ret-zero)",
            .AutoSize = True,
            .Location = New Point(190, 90),
            .Checked = True
        }
        chkResign = New CheckBox() With {
            .Text = "Re-sign ELFs",
            .AutoSize = True,
            .Location = New Point(460, 90)
        }

        For Each ctrl As Control In {
            lblGameFolder, txtGameFolder, btnBrowseFolder,
            lblFwCurrent, cmbFwCurrent, lblFwTarget, cmbFwTarget,
            chkApplyBps, chkStubMissing, chkResign
        }
            grpConfig.Controls.Add(ctrl)
        Next

        ' --- Log RichTextBox ---
        rtbLog = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(18, 18, 28),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 9),
            .BorderStyle = BorderStyle.None
        }

        ' --- SplitContainer ---
        splitContainer = New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Horizontal,
            .SplitterDistance = 125,
            .Panel1MinSize = 110
        }
        splitContainer.Panel1.Controls.Add(grpConfig)
        splitContainer.Panel2.Controls.Add(rtbLog)

        ' --- StatusStrip ---
        statusStrip = New StatusStrip()
        lblStatus = New ToolStripStatusLabel("Ready.")
        statusStrip.Items.Add(lblStatus)

        Me.Controls.Add(splitContainer)
        Me.Controls.Add(toolStrip)
        Me.Controls.Add(statusStrip)
    End Sub

    ' ---------------------------------------------------------------------------
    ' Event handlers
    ' ---------------------------------------------------------------------------

    Private Sub BtnBrowseFolder_Click(sender As Object, e As EventArgs) Handles btnBrowseFolder.Click
        Using dlg As New FolderBrowserDialog() With {
            .Description = "Select game folder containing .sprx / .prx / .bin files"
        }
            If dlg.ShowDialog() = DialogResult.OK Then
                txtGameFolder.Text = dlg.SelectedPath
                TryAutoDetectFirmware(dlg.SelectedPath)
            End If
        End Using
    End Sub

    ''' <summary>
    ''' Reads sce_sys/param.sfo from the game folder and auto-sets cmbFwCurrent
    ''' from the SYSTEM_VER field (minimum firmware required by the game).
    ''' </summary>
    Private Sub TryAutoDetectFirmware(gameFolder As String)
        Try
            ' Check the standard location first
            Dim sfoPath As String = Path.Combine(gameFolder, "sce_sys", "param.sfo")

            ' Fall back to a recursive search if not found at the standard path
            If Not File.Exists(sfoPath) Then
                Dim hits() As String = Directory.GetFiles(gameFolder, "param.sfo",
                                                          SearchOption.AllDirectories)
                If hits.Length = 0 Then
                    AppendLog("[AUTO] param.sfo not found in selected folder.", Color.Yellow)
                    Return
                End If
                sfoPath = hits(0)
            End If

            AppendLog($"[AUTO] Reading param.sfo: {sfoPath}", Color.Gray)

            Dim meta As New PKGMetadata()
            meta.LoadFromBytes(File.ReadAllBytes(sfoPath))

            If String.IsNullOrEmpty(meta.MinFirmware) Then Return

            ' MinFirmware is in the form "X.XX" — find the closest entry in the combo
            Dim detected As String = meta.MinFirmware
            AppendLog($"[AUTO] param.sfo detected MinFirmware: {detected}", Color.Lime)

            ' Try exact match first, then major.minor match
            If cmbFwCurrent.Items.Contains(detected) Then
                cmbFwCurrent.SelectedItem = detected
            Else
                ' Find the closest version >= detected
                Dim detectedVal As Double = 0
                Double.TryParse(detected, Globalization.NumberStyles.Any,
                                Globalization.CultureInfo.InvariantCulture, detectedVal)
                For Each item As Object In cmbFwCurrent.Items
                    Dim v As Double = 0
                    If Double.TryParse(item.ToString(), Globalization.NumberStyles.Any,
                                      Globalization.CultureInfo.InvariantCulture, v) Then
                        If Math.Abs(v - detectedVal) < 0.1 Then
                            cmbFwCurrent.SelectedItem = item
                            Exit For
                        End If
                    End If
                Next
            End If

            AppendLog($"[AUTO] Current FW set to: {cmbFwCurrent.Text}", Color.Lime)
        Catch ex As Exception
            AppendLog($"[AUTO] Could not read param.sfo: {ex.Message}", Color.Yellow)
        End Try
    End Sub

    Private Async Sub BtnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        If String.IsNullOrWhiteSpace(txtGameFolder.Text) OrElse
           Not Directory.Exists(txtGameFolder.Text) Then
            MessageBox.Show("Select a valid game folder first.",
                            "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        SetRunningState(True)
        AppendLog($"[ABP] Pipeline starting: {cmbFwCurrent.Text} → {cmbFwTarget.Text}", Color.Cyan)
        AppendLog($"[ABP] Game folder: {txtGameFolder.Text}", Color.Gray)

        _cts = New CancellationTokenSource()

        Dim scriptPath As String = ScriptPath("advanced_backport.py")
        If scriptPath Is Nothing Then
            AppendLog("[ERROR] advanced_backport.py not found in scripts/ folder.", Color.Red)
            AppendLog("       Make sure the scripts/ directory is next to the application.", Color.Orange)
            SetRunningState(False)
            Return
        End If

        Dim argsBuilder As New StringBuilder()
        argsBuilder.Append($"--game-folder ""{txtGameFolder.Text}"" ")
        argsBuilder.Append($"--fw-current {cmbFwCurrent.Text} ")
        argsBuilder.Append($"--fw-target {cmbFwTarget.Text} ")
        If chkApplyBps.Checked Then argsBuilder.Append("--apply-bps ")
        If chkStubMissing.Checked Then argsBuilder.Append("--stub-missing ")
        If chkResign.Checked Then argsBuilder.Append("--resign ")
        argsBuilder.Append("--no-color")

        Dim pipelineArgs As String = argsBuilder.ToString()
        Dim pipelineOut As Action(Of String) = Sub(line) SafeAppendLog(line)
        Dim pipelineErr As Action(Of String) = Sub(line) SafeAppendLog(line, Color.Orange)
        Dim pipelineCt As CancellationToken = _cts.Token

        ' Delegate the await to a Function returning Task to avoid VB.NET Conversions.ToInteger
        ' in the async state machine when awaiting Task(Of Integer) inside Async Sub.
        Await RunPipelineHelperAsync(scriptPath, pipelineArgs, pipelineOut, pipelineErr, pipelineCt)

        SetRunningState(False)
    End Sub

    ''' <summary>
    ''' Wraps PythonRunner.RunAsync so BtnRun_Click (Async Sub) awaits a plain Task,
    ''' avoiding the VB.NET Option-Strict-Off implicit Conversions.ToInteger bug.
    ''' </summary>
    Private Async Function RunPipelineHelperAsync(
        scriptPath As String,
        args As String,
        onOutput As Action(Of String),
        onError As Action(Of String),
        ct As CancellationToken
    ) As Task
        Dim runTask As Task(Of Integer) = PythonRunner.RunAsync(scriptPath, args, onOutput, onError, ct)
        Await runTask
        Dim exitCode As Integer = runTask.Result
        If exitCode = 0 Then
            SafeAppendLog("[ABP] Pipeline completed successfully.", Color.Lime)
        Else
            SafeAppendLog($"[ABP] Pipeline exited with code {exitCode}.", Color.Red)
        End If
    End Function

    Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        _cts?.Cancel()
        AppendLog("[ABP] Stop requested by user.", Color.Yellow)
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        rtbLog.Clear()
    End Sub

    Private Sub BtnOpenScripts_Click(sender As Object, e As EventArgs) Handles btnOpenScripts.Click
        Dim dir As String = ScriptsFolder()
        If dir IsNot Nothing Then
            Process.Start("explorer.exe", dir)
        Else
            MessageBox.Show(
                "scripts/ folder not found." & Environment.NewLine &
                "Expected location: <repo root>\scripts\",
                "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    ''' <summary>
    ''' Open (or bring to front) the UDP Log Viewer, which listens on port 9090
    ''' for messages from the C payload running on the PS5.
    ''' No Python subprocess required — listener is native VB.NET.
    ''' </summary>
    Private Sub BtnUdpViewer_Click(sender As Object, e As EventArgs) Handles btnUdpViewer.Click
        If _udpViewer IsNot Nothing AndAlso Not _udpViewer.IsDisposed Then
            _udpViewer.BringToFront()
            Return
        End If
        _udpViewer = New UdpLogViewerForm()
        _udpViewer.Show(Me)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _cts?.Cancel()
        If _udpViewer IsNot Nothing AndAlso Not _udpViewer.IsDisposed Then
            _udpViewer.Close()
        End If
        MyBase.OnFormClosed(e)
    End Sub

    ' ---------------------------------------------------------------------------
    ' Helpers
    ' ---------------------------------------------------------------------------

    Private Sub SetRunningState(running As Boolean)
        btnRun.Enabled = Not running
        btnStop.Enabled = running
        lblStatus.Text = If(running, "Running pipeline…", "Ready.")
    End Sub

    Private Sub AppendLog(text As String, Optional color As Color = Nothing)
        If color = Nothing Then color = Color.White
        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionLength = 0
        rtbLog.SelectionColor = color
        rtbLog.AppendText(text & Environment.NewLine)
        rtbLog.SelectionColor = rtbLog.ForeColor
        rtbLog.ScrollToCaret()
    End Sub

    Private Sub SafeAppendLog(text As String, Optional color As Color = Nothing)
        If color = Nothing Then color = ColorFromText(text)
        If rtbLog.InvokeRequired Then
            rtbLog.Invoke(Sub() AppendLog(text, color))
        Else
            AppendLog(text, color)
        End If
    End Sub

    Private Shared Function ColorFromText(text As String) As Color
        Dim up = text.ToUpperInvariant()
        If up.Contains("[ERROR]") OrElse up.Contains("[ERR]") Then Return Color.Red
        If up.Contains("[WARN]") Then Return Color.Yellow
        If up.Contains("[OK]") OrElse up.Contains("SUCCESS") Then Return Color.Lime
        If up.Contains("[STUB]") Then Return Color.Plum
        If up.Contains("[BPS]") Then Return Color.SkyBlue
        If up.Contains("[SDK]") Then Return Color.LightGreen
        If up.Contains("[REDIRECT]") Then Return Color.Cyan
        If up.Contains("[PAYLOAD]") OrElse up.Contains("[HOOK]") Then Return Color.Cyan
        Return Color.White
    End Function

    ''' <summary>
    ''' Search for the scripts/ directory by walking up from the exe location.
    ''' Works in both Debug (bin\Debug\net8.0-windows\) and installed layouts.
    ''' </summary>
    Friend Shared Function ScriptsFolder() As String
        Dim dir As String = AppDomain.CurrentDomain.BaseDirectory
        For i As Integer = 0 To 6
            Dim candidate As String = Path.GetFullPath(Path.Combine(dir, "scripts"))
            If Directory.Exists(candidate) Then Return candidate
            Dim parent As String = Path.GetDirectoryName(dir)
            If parent Is Nothing OrElse parent = dir Then Exit For
            dir = parent
        Next
        Return Nothing
    End Function

    Private Shared Function ScriptPath(scriptName As String) As String
        Dim folder As String = ScriptsFolder()
        If folder Is Nothing Then Return Nothing
        Dim p As String = Path.Combine(folder, scriptName)
        If File.Exists(p) Then Return p
        Return Nothing
    End Function

End Class
