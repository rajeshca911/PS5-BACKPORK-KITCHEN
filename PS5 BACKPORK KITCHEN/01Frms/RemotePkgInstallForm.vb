Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Text.Json

''' <summary>
''' Form for remote PKG installation to PS5 via etaHEN DPI.
''' Serves PKG over HTTP and sends install command via TCP to port 9090.
''' </summary>
Public Class RemotePkgInstallForm
    Inherits Form

    Private _installService As RemotePkgInstallService

    ' UI Controls
    Private txtPs5Ip As TextBox
    Private nudPort As NumericUpDown
    Private chkRememberIp As CheckBox
    Private txtPkgPath As TextBox
    Private btnBrowse As Button
    Private btnTestConnection As Button
    Private btnInstall As Button
    Private btnStopServer As Button
    Private prgProgress As ProgressBar
    Private lblProgress As Label
    Private rtbLog As RichTextBox

    Private Shared ReadOnly SettingsPath As String =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "PS5BackporkKitchen", "remote_install.json")

    Public Sub New(Optional pkgFilePath As String = "")
        _installService = New RemotePkgInstallService()
        AddHandler _installService.StatusChanged, AddressOf OnStatusChanged
        AddHandler _installService.FileServeProgress, AddressOf OnFileServeProgress
        AddHandler _installService.FileServeCompleted, AddressOf OnFileServeCompleted
        InitializeComponent()
        LoadSettings()

        If Not String.IsNullOrEmpty(pkgFilePath) Then
            txtPkgPath.Text = pkgFilePath
        End If
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Remote PKG Install (PS5)"
        Me.Size = New Size(580, 520)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Dim y = 15

        ' PS5 IP
        Dim lblIp As New Label With {
            .Text = "PS5 IP Address:",
            .Location = New Point(15, y + 3),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }
        Me.Controls.Add(lblIp)

        txtPs5Ip = New TextBox With {
            .Location = New Point(140, y),
            .Width = 150,
            .Font = New Font("Segoe UI", 9.5F),
            .Text = "192.168.1."
        }
        Me.Controls.Add(txtPs5Ip)

        chkRememberIp = New CheckBox With {
            .Text = "Remember",
            .Location = New Point(300, y + 2),
            .AutoSize = True,
            .Checked = True
        }
        Me.Controls.Add(chkRememberIp)

        btnTestConnection = New Button With {
            .Text = "Test Connection",
            .Location = New Point(410, y - 2),
            .Size = New Size(130, 28),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnTestConnection.Click, AddressOf BtnTestConnection_Click
        Me.Controls.Add(btnTestConnection)
        y += 35

        ' Port
        Dim lblPort As New Label With {
            .Text = "HTTP Port:",
            .Location = New Point(15, y + 3),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }
        Me.Controls.Add(lblPort)

        nudPort = New NumericUpDown With {
            .Location = New Point(140, y),
            .Width = 80,
            .Minimum = 1024,
            .Maximum = 65535,
            .Value = 8080
        }
        Me.Controls.Add(nudPort)

        Dim lblPortNote As New Label With {
            .Text = "(PS5 will download from this port)",
            .Location = New Point(230, y + 3),
            .AutoSize = True,
            .ForeColor = Color.Gray,
            .Font = New Font("Segoe UI", 8)
        }
        Me.Controls.Add(lblPortNote)
        y += 35

        ' PKG file
        Dim lblPkg As New Label With {
            .Text = "PKG File:",
            .Location = New Point(15, y + 3),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9)
        }
        Me.Controls.Add(lblPkg)

        txtPkgPath = New TextBox With {
            .Location = New Point(140, y),
            .Width = 340,
            .Font = New Font("Segoe UI", 9),
            .ReadOnly = True
        }
        Me.Controls.Add(txtPkgPath)

        btnBrowse = New Button With {
            .Text = "...",
            .Location = New Point(490, y - 1),
            .Size = New Size(50, 26),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnBrowse.Click, AddressOf BtnBrowse_Click
        Me.Controls.Add(btnBrowse)
        y += 40

        ' Progress bar
        prgProgress = New ProgressBar With {
            .Location = New Point(15, y),
            .Size = New Size(460, 22),
            .Minimum = 0,
            .Maximum = 100,
            .Style = ProgressBarStyle.Continuous
        }
        Me.Controls.Add(prgProgress)

        lblProgress = New Label With {
            .Location = New Point(480, y + 2),
            .Size = New Size(70, 18),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Text = ""
        }
        Me.Controls.Add(lblProgress)
        y += 30

        ' Action buttons
        btnInstall = New Button With {
            .Text = "Install to PS5",
            .Location = New Point(15, y),
            .Size = New Size(130, 32),
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = Color.FromArgb(40, 130, 80),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnInstall.Click, AddressOf BtnInstall_Click
        Me.Controls.Add(btnInstall)

        btnStopServer = New Button With {
            .Text = "Stop Server",
            .Location = New Point(155, y),
            .Size = New Size(100, 32),
            .BackColor = Color.FromArgb(180, 50, 50),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Enabled = False
        }
        AddHandler btnStopServer.Click, AddressOf BtnStopServer_Click
        Me.Controls.Add(btnStopServer)

        Dim lblLocalIp As New Label With {
            .Text = $"Your IP: {RemotePkgInstallService.GetLocalIpAddress()}",
            .Location = New Point(270, y + 8),
            .AutoSize = True,
            .ForeColor = Color.FromArgb(80, 80, 80),
            .Font = New Font("Segoe UI", 8.5F)
        }
        Me.Controls.Add(lblLocalIp)
        y += 42

        ' Log
        Dim lblLog As New Label With {
            .Text = "Log:",
            .Location = New Point(15, y),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        Me.Controls.Add(lblLog)
        y += 20

        rtbLog = New RichTextBox With {
            .Location = New Point(15, y),
            .Size = New Size(530, Me.ClientSize.Height - y - 10),
            .Font = New Font("Consolas", 8.5F),
            .ReadOnly = True,
            .BackColor = Color.FromArgb(25, 25, 35),
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        Me.Controls.Add(rtbLog)

        AddHandler Me.FormClosing, AddressOf Form_FormClosing
    End Sub

    Private Async Sub BtnTestConnection_Click(sender As Object, e As EventArgs)
        Dim ip = txtPs5Ip.Text.Trim()
        If String.IsNullOrEmpty(ip) Then
            MessageBox.Show("Enter PS5 IP address.", "Input Required",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnTestConnection.Enabled = False
        btnTestConnection.Text = "Testing..."
        LogMessage($"Testing connection to {ip}:9090...", Color.Cyan)

        Dim connected = Await _installService.TestConnectionAsync(ip)

        If connected Then
            LogMessage("Connection successful - etaHEN DPI is running", Color.LimeGreen)
        Else
            LogMessage("Connection failed - check that etaHEN is running with DPI enabled", Color.OrangeRed)
        End If

        btnTestConnection.Enabled = True
        btnTestConnection.Text = "Test Connection"
    End Sub

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Filter = "PKG Files (*.pkg)|*.pkg|All Files (*.*)|*.*"
            ofd.Title = "Select PKG file to install"

            If ofd.ShowDialog() = DialogResult.OK Then
                txtPkgPath.Text = ofd.FileName
            End If
        End Using
    End Sub

    Private Async Sub BtnInstall_Click(sender As Object, e As EventArgs)
        ' Validate inputs
        Dim ps5Ip = txtPs5Ip.Text.Trim()
        Dim pkgPath = txtPkgPath.Text.Trim()
        Dim port = CInt(nudPort.Value)

        If String.IsNullOrEmpty(ps5Ip) Then
            MessageBox.Show("Enter PS5 IP address.", "Input Required",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrEmpty(pkgPath) OrElse Not File.Exists(pkgPath) Then
            MessageBox.Show("Select a valid PKG file.", "Input Required",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnInstall.Enabled = False
        btnStopServer.Enabled = True
        prgProgress.Value = 0
        lblProgress.Text = "0%"

        ' Save settings
        If chkRememberIp.Checked Then SaveSettings()

        Try
            ' Step 1: Start HTTP server
            LogMessage($"Starting HTTP server on port {port}...", Color.Cyan)
            _installService.StartFileServer(pkgPath, port)

            ' Step 2: Build PKG URL
            Dim localIp = RemotePkgInstallService.GetLocalIpAddress()
            Dim fileName = Path.GetFileName(pkgPath)
            Dim pkgUrl = $"http://{localIp}:{port}/{Uri.EscapeDataString(fileName)}"
            LogMessage($"PKG URL: {pkgUrl}", Color.White)

            ' Step 3: Test connection
            LogMessage($"Connecting to PS5 at {ps5Ip}:9090...", Color.Cyan)
            Dim canConnect = Await _installService.TestConnectionAsync(ps5Ip)

            If Not canConnect Then
                LogMessage("Cannot connect to PS5. Ensure etaHEN is running with DPI enabled.", Color.OrangeRed)
                _installService.StopFileServer()
                btnInstall.Enabled = True
                btnStopServer.Enabled = False
                Return
            End If

            ' Step 4: Send install command
            LogMessage("Sending install command...", Color.Cyan)
            Dim response = Await _installService.SendInstallCommandAsync(ps5Ip, pkgUrl)
            LogMessage($"PS5 response: {response}", Color.Yellow)

            ' Check response
            If response.Contains("""res"":""0""") OrElse response.Contains("""status"":""success""") Then
                LogMessage("PS5 accepted the install request. Serving file...", Color.LimeGreen)
                LogMessage("Keep this window open until the transfer completes.", Color.White)
            Else
                LogMessage("PS5 may have rejected the request. Check the response above.", Color.OrangeRed)
                LogMessage("The HTTP server will keep running in case PS5 retries.", Color.Gray)
            End If

        Catch ex As Exception
            LogMessage($"Error: {ex.Message}", Color.Red)
            _installService.StopFileServer()
            btnInstall.Enabled = True
            btnStopServer.Enabled = False
        End Try
    End Sub

    Private Sub BtnStopServer_Click(sender As Object, e As EventArgs)
        _installService.StopFileServer()
        btnInstall.Enabled = True
        btnStopServer.Enabled = False
        LogMessage("Server stopped by user.", Color.Yellow)
    End Sub

    Private Sub OnStatusChanged(message As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() OnStatusChanged(message))
            Return
        End If
        If Me.IsDisposed Then Return

        LogMessage(message, Color.Gray)
    End Sub

    Private Sub OnFileServeProgress(servedBytes As Long, totalBytes As Long)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() OnFileServeProgress(servedBytes, totalBytes))
            Return
        End If
        If Me.IsDisposed Then Return

        If totalBytes > 0 Then
            Dim percent = CInt(Math.Min(100, servedBytes * 100L \ totalBytes))
            prgProgress.Value = percent
            lblProgress.Text = $"{percent}%"
        End If
    End Sub

    Private Sub OnFileServeCompleted()
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() OnFileServeCompleted())
            Return
        End If
        If Me.IsDisposed Then Return

        prgProgress.Value = 100
        lblProgress.Text = "100%"
        LogMessage("File transfer complete! PKG should now be installing on PS5.", Color.LimeGreen)

        ' Auto-stop server after a short delay
        Task.Delay(3000).ContinueWith(Sub()
                                          If Not Me.IsDisposed Then
                                              Me.BeginInvoke(Sub()
                                                                 _installService.StopFileServer()
                                                                 btnInstall.Enabled = True
                                                                 btnStopServer.Enabled = False
                                                             End Sub)
                                          End If
                                      End Sub)
    End Sub

    Private Sub LogMessage(message As String, color As Color)
        If rtbLog Is Nothing OrElse rtbLog.IsDisposed Then Return

        Dim timestamp = DateTime.Now.ToString("HH:mm:ss")
        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionColor = Color.DarkGray
        rtbLog.AppendText($"[{timestamp}] ")
        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionColor = color
        rtbLog.AppendText(message & vbCrLf)
        rtbLog.ScrollToCaret()
    End Sub

    Private Sub LoadSettings()
        Try
            If File.Exists(SettingsPath) Then
                Dim json = File.ReadAllText(SettingsPath)
                Dim doc = JsonDocument.Parse(json)
                Dim root = doc.RootElement

                Dim ip As JsonElement
                If root.TryGetProperty("ps5_ip", ip) Then
                    txtPs5Ip.Text = ip.GetString()
                End If

                Dim port As JsonElement
                If root.TryGetProperty("port", port) Then
                    nudPort.Value = port.GetInt32()
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub SaveSettings()
        Try
            Dim dir = Path.GetDirectoryName(SettingsPath)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim json = $"{{""ps5_ip"":""{txtPs5Ip.Text.Trim()}"",""port"":{CInt(nudPort.Value)}}}"
            File.WriteAllText(SettingsPath, json)
        Catch
        End Try
    End Sub

    Private Sub Form_FormClosing(sender As Object, e As FormClosingEventArgs)
        If _installService.IsServing Then
            Dim result = MessageBox.Show(
                "The HTTP server is still running. Stopping it will cancel the PKG transfer." & vbCrLf & vbCrLf &
                "Stop server and close?",
                "Server Running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If result = DialogResult.No Then
                e.Cancel = True
                Return
            End If
        End If

        _installService?.Dispose()
    End Sub

End Class
