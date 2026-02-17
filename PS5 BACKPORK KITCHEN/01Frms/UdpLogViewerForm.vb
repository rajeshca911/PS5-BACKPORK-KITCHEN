Imports System.Drawing
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' UDP Log Viewer — receives text messages from the PS5 payload (udp_logger.c)
''' and displays them in a color-coded RichTextBox.
'''
''' Listens natively on a UDP port using System.Net.Sockets.UdpClient.
''' No Python process required.
'''
''' Configure LOG_SERVER_IP in payload/src/main.c to point to the PC's IP.
''' Default port: 9090 (matches LOG_SERVER_PORT in main.c).
''' </summary>
Public Class UdpLogViewerForm
    Inherits Form

    ' ---------------------------------------------------------------------------
    ' Constants
    ' ---------------------------------------------------------------------------

    Private Const DEFAULT_PORT As Integer = 9090
    Private Const MAX_LOG_LINES As Integer = 5000

    ' ---------------------------------------------------------------------------
    ' UI Controls
    ' ---------------------------------------------------------------------------

    Private WithEvents toolStrip   As ToolStrip
    Private WithEvents btnStart    As ToolStripButton
    Private WithEvents btnStop     As ToolStripButton
    Private WithEvents btnClear    As ToolStripButton
    Private WithEvents btnSaveLog  As ToolStripButton

    Private lblPort  As ToolStripLabel
    Private nudPort  As ToolStripControlHost  ' wraps a NumericUpDown

    Private rtbLog As RichTextBox

    Private statusStrip As StatusStrip
    Private lblStatus   As ToolStripStatusLabel
    Private lblCount    As ToolStripStatusLabel

    ' ---------------------------------------------------------------------------
    ' Runtime
    ' ---------------------------------------------------------------------------

    Private _client     As UdpClient
    Private _cts        As CancellationTokenSource
    Private _msgCount   As Integer = 0

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
        Me.Text = "UDP Log Viewer — PS5 Payload"
        Me.Size = New Size(800, 540)
        Me.MinimumSize = New Size(500, 350)
        Me.StartPosition = FormStartPosition.Manual
        ' Position to the right of the parent if possible
        Me.Location = New Point(
            Screen.PrimaryScreen.WorkingArea.Right - Me.Width - 10,
            Screen.PrimaryScreen.WorkingArea.Top + 40)

        ' --- Port NumericUpDown inside ToolStrip ---
        Dim nud As New NumericUpDown() With {
            .Minimum = 1,
            .Maximum = 65535,
            .Value = DEFAULT_PORT,
            .Width = 70,
            .Height = 22,
            .Font = New Font("Segoe UI", 9)
        }
        nudPort = New ToolStripControlHost(nud)

        ' --- ToolStrip ---
        toolStrip = New ToolStrip()
        lblPort = New ToolStripLabel("Port: ")
        btnStart = New ToolStripButton("▶ Start") With {
            .ForeColor = Color.DarkGreen,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ToolTipText = "Start listening for UDP log messages"
        }
        btnStop = New ToolStripButton("■ Stop") With {
            .ForeColor = Color.DarkRed,
            .Enabled = False,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ToolTipText = "Stop listening"
        }
        btnClear = New ToolStripButton("✕ Clear") With {.ToolTipText = "Clear log"}
        btnSaveLog = New ToolStripButton("💾 Save") With {.ToolTipText = "Save log to file"}
        toolStrip.Items.AddRange({
            lblPort, nudPort,
            New ToolStripSeparator(),
            btnStart, btnStop,
            New ToolStripSeparator(),
            btnClear, btnSaveLog
        })

        ' --- Log ---
        rtbLog = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(12, 12, 22),
            .ForeColor = Color.LightGray,
            .Font = New Font("Consolas", 9),
            .BorderStyle = BorderStyle.None
        }

        ' --- StatusStrip ---
        statusStrip = New StatusStrip()
        lblStatus = New ToolStripStatusLabel("Not listening.") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        lblCount = New ToolStripStatusLabel("Messages: 0") With {.TextAlign = ContentAlignment.MiddleRight}
        statusStrip.Items.AddRange({lblStatus, lblCount})

        Me.Controls.Add(rtbLog)
        Me.Controls.Add(toolStrip)
        Me.Controls.Add(statusStrip)
    End Sub

    ' ---------------------------------------------------------------------------
    ' ToolStrip handlers
    ' ---------------------------------------------------------------------------

    Private Sub BtnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        Dim port As Integer = CInt(DirectCast(nudPort.Control, NumericUpDown).Value)
        StartListening(port)
    End Sub

    Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        StopListening()
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        rtbLog.Clear()
        _msgCount = 0
        UpdateCount()
    End Sub

    Private Sub BtnSaveLog_Click(sender As Object, e As EventArgs) Handles btnSaveLog.Click
        Using dlg As New SaveFileDialog() With {
            .Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            .FileName = $"ps5_payload_{DateTime.Now:yyyyMMdd_HHmmss}.log"
        }
            If dlg.ShowDialog() = DialogResult.OK Then
                IO.File.WriteAllText(dlg.FileName, rtbLog.Text, Encoding.UTF8)
            End If
        End Using
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        StopListening()
        MyBase.OnFormClosed(e)
    End Sub

    ' ---------------------------------------------------------------------------
    ' Listener
    ' ---------------------------------------------------------------------------

    Private Sub StartListening(port As Integer)
        Try
            _cts = New CancellationTokenSource()
            _client = New UdpClient()
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
            _client.Client.Bind(New IPEndPoint(IPAddress.Any, port))

            btnStart.Enabled = False
            btnStop.Enabled = True
            DirectCast(nudPort.Control, NumericUpDown).Enabled = False
            lblStatus.Text = $"Listening on 0.0.0.0:{port} — waiting for PS5 payload..."
            AppendLog($"[UDP] Listener started on port {port}. Configure LOG_SERVER_IP in payload/src/main.c.", Color.Cyan)

            ' Start background receive loop
            Dim token As CancellationToken = _cts.Token
            Task.Run(Sub() ReceiveLoop(token))

        Catch ex As Exception
            AppendLog($"[UDP][ERROR] Cannot bind port {port}: {ex.Message}", Color.Red)
            AppendLog("             Try a different port, or check if another process is using it.", Color.Orange)
        End Try
    End Sub

    Private Sub StopListening()
        _cts?.Cancel()
        Try
            _client?.Close()
        Catch
        End Try
        _client = Nothing
        _cts = Nothing
        SafeInvoke(Sub()
                       btnStart.Enabled = True
                       btnStop.Enabled = False
                       DirectCast(nudPort.Control, NumericUpDown).Enabled = True
                       lblStatus.Text = "Not listening."
                   End Sub)
    End Sub

    Private Sub ReceiveLoop(ct As CancellationToken)
        Dim remoteEp As New IPEndPoint(IPAddress.Any, 0)
        Do While Not ct.IsCancellationRequested
            Try
                Dim bytes() As Byte = _client.Receive(remoteEp)
                Dim msg As String = Encoding.UTF8.GetString(bytes).TrimEnd(ChrW(10), ChrW(13))
                Dim ts As String = DateTime.Now.ToString("HH:mm:ss.fff")
                Dim host As String = remoteEp.Address.ToString()
                Dim formatted As String = $"[{ts}] {host}  {msg}"
                Dim clr As Color = ColorFromMessage(msg)
                SafeAppendLog(formatted, clr)
                SafeInvoke(Sub() UpdateCount())
            Catch ex As SocketException
                If Not ct.IsCancellationRequested Then
                    SafeAppendLog($"[UDP][SOCKET] {ex.Message}", Color.Orange)
                End If
                Exit Do
            Catch ex As ObjectDisposedException
                Exit Do
            Catch ex As Exception
                If Not ct.IsCancellationRequested Then
                    SafeAppendLog($"[UDP][ERROR] {ex.Message}", Color.Red)
                End If
            End Try
        Loop
    End Sub

    ' ---------------------------------------------------------------------------
    ' Helpers
    ' ---------------------------------------------------------------------------

    Private Sub AppendLog(text As String, Optional color As Color = Nothing)
        If color = Nothing Then color = Color.LightGray

        ' Trim log if too long
        If rtbLog.Lines.Length > MAX_LOG_LINES Then
            rtbLog.SelectionStart = 0
            rtbLog.SelectionLength = rtbLog.GetFirstCharIndexFromLine(MAX_LOG_LINES \ 2)
            rtbLog.SelectedText = ""
        End If

        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionLength = 0
        rtbLog.SelectionColor = color
        rtbLog.AppendText(text & Environment.NewLine)
        rtbLog.SelectionColor = rtbLog.ForeColor
        rtbLog.ScrollToCaret()

        _msgCount += 1
    End Sub

    Private Sub SafeAppendLog(text As String, Optional color As Color = Nothing)
        If rtbLog.InvokeRequired Then
            rtbLog.Invoke(Sub() AppendLog(text, color))
        Else
            AppendLog(text, color)
        End If
    End Sub

    Private Sub SafeInvoke(action As Action)
        If Me.InvokeRequired Then
            Me.Invoke(action)
        Else
            action()
        End If
    End Sub

    Private Sub UpdateCount()
        lblCount.Text = $"Messages: {_msgCount}"
    End Sub

    Private Shared Function ColorFromMessage(msg As String) As Color
        Dim up As String = msg.ToUpperInvariant()
        If up.Contains("[ERROR]") OrElse up.Contains("[ERR]") Then Return Color.OrangeRed
        If up.Contains("[WARN]") OrElse up.Contains("[WARNING]") Then Return Color.Yellow
        If up.Contains("[REDIRECT]") Then Return Color.Cyan
        If up.Contains("[HOOK]") Then Return Color.Plum
        If up.Contains("[PAYLOAD]") Then Return Color.LightSkyBlue
        If up.Contains("[OK]") Then Return Color.Lime
        Return Color.LightGray
    End Function

End Class
