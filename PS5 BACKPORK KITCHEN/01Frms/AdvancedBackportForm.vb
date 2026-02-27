Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' Advanced Backport Pipeline — GUI wrapper for scripts/advanced_backport.py.
'''
''' Layout:
'''   Top pane   : configuration panel (folder, firmware versions, options)
'''   Bottom pane: color-coded log RichTextBox
'''   ToolStrip  : Run, Stop, Clear, Open Log Folder
''' </summary>
Public Class AdvancedBackportForm
    Inherits Form

    ' ---------------------------------------------------------------------------
    ' UI Controls (created in code — no Designer dependency)
    ' ---------------------------------------------------------------------------

    Private WithEvents toolStrip       As ToolStrip
    Private WithEvents btnRun          As ToolStripButton
    Private WithEvents btnStop         As ToolStripButton
    Private WithEvents btnClear        As ToolStripButton
    Private WithEvents btnOpenScripts  As ToolStripButton

    Private splitContainer As SplitContainer

    ' Config panel controls
    Private grpConfig          As GroupBox
    Private lblGameFolder      As Label
    Private txtGameFolder      As TextBox
    Private WithEvents btnBrowseFolder As Button
    Private lblFwCurrent       As Label
    Private cmbFwCurrent       As ComboBox
    Private lblFwTarget        As Label
    Private cmbFwTarget        As ComboBox
    Private chkApplyBps        As CheckBox
    Private chkStubMissing     As CheckBox
    Private chkResign          As CheckBox
    Private lblPcIp            As Label
    Private txtPcIp            As TextBox
    Private WithEvents btnStartUdpServer As Button

    ' Log panel
    Private rtbLog As RichTextBox

    ' Status strip
    Private statusStrip As StatusStrip
    Private lblStatus   As ToolStripStatusLabel

    ' Runtime
    Private _cts As CancellationTokenSource
    Private _udpServerTask As Task
    Private _udpServerCts  As CancellationTokenSource

    ' ---------------------------------------------------------------------------
    ' Constructor
    ' ---------------------------------------------------------------------------

    Public Sub New()
        InitializeComponent()
        Me.Text = "Advanced Backport Pipeline"
        Me.Size = New Size(900, 620)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Icon = TryCast(My.Resources.ResourceManager.GetObject("backport"), Icon)
    End Sub

    ' ---------------------------------------------------------------------------
    ' Form initialization
    ' ---------------------------------------------------------------------------

    Private Sub InitializeComponent()
        ' --- ToolStrip ---
        toolStrip = New ToolStrip()
        btnRun = New ToolStripButton("▶ Run") With {.ToolTipText = "Start backport pipeline", .ForeColor = Color.DarkGreen}
        btnStop = New ToolStripButton("■ Stop") With {.ToolTipText = "Stop running pipeline", .Enabled = False, .ForeColor = Color.DarkRed}
        btnClear = New ToolStripButton("✕ Clear Log") With {.ToolTipText = "Clear log output"}
        btnOpenScripts = New ToolStripButton("📂 Scripts Folder") With {.ToolTipText = "Open scripts/ in Explorer"}
        toolStrip.Items.AddRange({btnRun, btnStop, New ToolStripSeparator(), btnClear, btnOpenScripts})

        ' --- Config panel ---
        grpConfig = New GroupBox() With {.Text = "Pipeline Configuration", .Dock = DockStyle.Fill, .Padding = New Padding(8)}

        lblGameFolder = New Label() With {.Text = "Game Folder:", .AutoSize = True, .Location = New Point(10, 24)}
        txtGameFolder = New TextBox() With {.Location = New Point(110, 21), .Width = 440, .ReadOnly = True}
        btnBrowseFolder = New Button() With {.Text = "Browse…", .Location = New Point(558, 19), .Width = 75}

        lblFwCurrent = New Label() With {.Text = "Current FW:", .AutoSize = True, .Location = New Point(10, 56)}
        cmbFwCurrent = New ComboBox() With {.Location = New Point(110, 52), .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList}
        lblFwTarget = New Label() With {.Text = "Target FW:", .AutoSize = True, .Location = New Point(230, 56)}
        cmbFwTarget = New ComboBox() With {.Location = New Point(320, 52), .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList}

        Dim fwVersions As String() = {"1.00", "1.02", "1.05", "1.10", "1.14",
                                       "2.00", "2.20", "2.25", "2.26", "2.50",
                                       "3.00", "3.20", "3.21", "4.00", "4.02",
                                       "4.50", "4.51", "5.00", "5.02", "5.10",
                                       "5.25", "6.00", "6.02", "6.50", "7.00",
                                       "7.01", "7.02", "7.04", "7.20", "7.55",
                                       "7.61", "8.00", "8.52", "9.00", "9.03",
                                       "9.60", "10.00", "10.01", "10.50", "11.00"}
        cmbFwCurrent.Items.AddRange(fwVersions)
        cmbFwTarget.Items.AddRange(fwVersions)
        cmbFwCurrent.SelectedItem = "10.01"
        cmbFwTarget.SelectedItem = "7.61"

        chkApplyBps = New CheckBox() With {.Text = "Apply BPS patches", .AutoSize = True, .Location = New Point(10, 86), .Checked = True}
        chkStubMissing = New CheckBox() With {.Text = "Stub missing symbols (ARM64 ret-zero)", .AutoSize = True, .Location = New Point(190, 86), .Checked = True}
        chkResign = New CheckBox() With {.Text = "Re-sign ELFs", .AutoSize = True, .Location = New Point(460, 86)}

        lblPcIp = New Label() With {.Text = "PC IP (UDP log):", .AutoSize = True, .Location = New Point(10, 116)}
        txtPcIp = New TextBox() With {.Text = "192.168.1.100", .Location = New Point(120, 112), .Width = 130}
        btnStartUdpServer = New Button() With {.Text = "Start UDP Server", .Location = New Point(260, 110), .Width = 130, .Height = 26}

        For Each ctrl As Control In {lblGameFolder, txtGameFolder, btnBrowseFolder,
                                      lblFwCurrent, cmbFwCurrent, lblFwTarget, cmbFwTarget,
                                      chkApplyBps, chkStubMissing, chkResign,
                                      lblPcIp, txtPcIp, btnStartUdpServer}
            grpConfig.Controls.Add(ctrl)
        Next

        ' --- Log RichTextBox ---
        rtbLog = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(20, 20, 30),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 9),
            .BorderStyle = BorderStyle.None
        }

        ' --- SplitContainer ---
        splitContainer = New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Horizontal,
            .SplitterDistance = 155,
            .Panel1MinSize = 120
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
        Using dlg As New FolderBrowserDialog() With {.Description = "Select game folder containing .sprx/.prx/.bin files"}
            If dlg.ShowDialog() = DialogResult.OK Then
                txtGameFolder.Text = dlg.SelectedPath
                DetectGameFirmware(dlg.SelectedPath)
            End If
        End Using
    End Sub

    ''' <summary>
    ''' Reads param.json or param.sfo to auto-detect the required firmware version.
    ''' </summary>
    Private Sub DetectGameFirmware(gameFolder As String)
        Try
            ' Try param.json first (PS5 PKG extracted)
            Dim paramJson = Path.Combine(gameFolder, "sce_sys", "param.json")
            If File.Exists(paramJson) Then
                Dim json = File.ReadAllText(paramJson)
                Dim fwMatch = System.Text.RegularExpressions.Regex.Match(json,
                    """requiredSystemSoftwareVersion""\s*:\s*""0x([0-9A-Fa-f]+)""")
                If fwMatch.Success Then
                    Dim hexVal = fwMatch.Groups(1).Value
                    If hexVal.Length >= 8 Then
                        Dim major = Convert.ToInt32(hexVal.Substring(0, 2), 16)
                        Dim minor = Convert.ToInt32(hexVal.Substring(2, 2), 16)
                        Dim fwStr = $"{major}.{minor:D2}"
                        ' Try to find a matching entry in the dropdown
                        For i = 0 To cmbFwCurrent.Items.Count - 1
                            If cmbFwCurrent.Items(i).ToString() = fwStr Then
                                cmbFwCurrent.SelectedIndex = i
                                AppendLog($"[ABP] Detected game firmware: {fwStr}", Color.Cyan)
                                Return
                            End If
                        Next
                        ' No exact match — find closest
                        Dim detected = Double.Parse(fwStr, Globalization.CultureInfo.InvariantCulture)
                        For i = cmbFwCurrent.Items.Count - 1 To 0 Step -1
                            Dim val = Double.Parse(cmbFwCurrent.Items(i).ToString(), Globalization.CultureInfo.InvariantCulture)
                            If val <= detected Then
                                cmbFwCurrent.SelectedIndex = i
                                AppendLog($"[ABP] Detected game firmware: ~{fwStr} (using {cmbFwCurrent.Items(i)})", Color.Cyan)
                                Return
                            End If
                        Next
                    End If
                End If
            End If
        Catch ex As Exception
            AppendLog($"[ABP] Could not detect firmware: {ex.Message}", Color.DarkOrange)
        End Try
    End Sub

    Private Async Sub BtnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        If String.IsNullOrWhiteSpace(txtGameFolder.Text) OrElse Not Directory.Exists(txtGameFolder.Text) Then
            MessageBox.Show("Select a valid game folder first.", "Invalid Folder",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        SetRunningState(True)
        AppendLog($"[ABP] Starting pipeline: {cmbFwCurrent.Text} → {cmbFwTarget.Text}", Color.Cyan)
        AppendLog($"[ABP] Game folder: {txtGameFolder.Text}", Color.Gray)

        _cts = New CancellationTokenSource()

        Dim pyScript As String = ResolveScriptPath("advanced_backport.py")
        If pyScript Is Nothing Then
            AppendLog("[ERROR] advanced_backport.py not found in scripts/ folder.", Color.Red)
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

        Dim exitCode = Await PythonRunner.RunAsync(
            pyScript, argsBuilder.ToString(),
            onOutput:=Sub(line) SafeAppendLog(line),
            onError:=Sub(line) SafeAppendLog(line, Color.Orange),
            ct:=_cts.Token)

        If exitCode = 0 Then
            AppendLog("[ABP] Pipeline completed successfully.", Color.Lime)
        Else
            AppendLog($"[ABP] Pipeline exited with code {exitCode}.", Color.Red)
        End If

        SetRunningState(False)
    End Sub

    Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        _cts?.Cancel()
        AppendLog("[ABP] Stop requested.", Color.Yellow)
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        rtbLog.Clear()
    End Sub

    Private Sub BtnOpenScripts_Click(sender As Object, e As EventArgs) Handles btnOpenScripts.Click
        Dim scriptsDir = FindScriptsFolder()
        If scriptsDir IsNot Nothing Then
            Process.Start("explorer.exe", scriptsDir)
        Else
            MessageBox.Show("scripts/ folder not found.", "Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Async Sub BtnStartUdpServer_Click(sender As Object, e As EventArgs) Handles btnStartUdpServer.Click
        If _udpServerCts IsNot Nothing AndAlso Not _udpServerCts.IsCancellationRequested Then
            ' Stop server
            _udpServerCts.Cancel()
            btnStartUdpServer.Text = "Start UDP Server"
            AppendLog("[UDP] Server stopped.", Color.Yellow)
            Return
        End If

        Dim udpScript As String = ResolveScriptPath("udp_log_server.py")
        If udpScript Is Nothing Then
            AppendLog("[ERROR] udp_log_server.py not found.", Color.Red)
            Return
        End If

        _udpServerCts = New CancellationTokenSource()
        btnStartUdpServer.Text = "Stop UDP Server"
        AppendLog($"[UDP] Server starting on 0.0.0.0:9090 ...", Color.Cyan)

        _udpServerTask = PythonRunner.RunAsync(
            udpScript, "--port 9090",
            onOutput:=Sub(line) SafeAppendLog(line, Color.DarkCyan),
            onError:=Sub(line) SafeAppendLog(line, Color.Orange),
            ct:=_udpServerCts.Token)

        Await _udpServerTask
        btnStartUdpServer.Text = "Start UDP Server"
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
        Return Color.White
    End Function

    Private Shared Function FindScriptsFolder() As String
        ' Walk up from the application base directory to find scripts/
        Dim dir = AppDomain.CurrentDomain.BaseDirectory
        For i = 0 To 8
            Dim candidate = Path.Combine(dir, "scripts")
            If Directory.Exists(candidate) Then Return Path.GetFullPath(candidate)
            Dim parent = Directory.GetParent(dir)
            If parent Is Nothing Then Exit For
            dir = parent.FullName
        Next
        Return Nothing
    End Function

    Private Shared Function ResolveScriptPath(scriptName As String) As String
        Dim folder = FindScriptsFolder()
        If folder Is Nothing Then Return Nothing
        Dim p = Path.Combine(folder, scriptName)
        Return If(File.Exists(p), p, Nothing)
    End Function

End Class
