Imports PS5_BACKPORK_KITCHEN.Services.GameSearch
Imports System.Windows.Forms
Imports System.Threading

''' <summary>
''' Modal dialog that automatically tries all available hosting services
''' in priority order until one succeeds. Shows a log of attempts and
''' download progress.
''' </summary>
Public Class DownloadAutoFailoverForm
    Inherits Form

    Private ReadOnly _downloadLinks As List(Of HostLink)
    Private ReadOnly _outputFolder As String
    Private _cancellationTokenSource As CancellationTokenSource
    Private _downloadService As DirectDownloadService
    Private _startTime As DateTime

    ' UI Controls
    Private lblCurrentHost As Label
    Private lblFileName As Label
    Private prgDownload As ProgressBar
    Private lblPercent As Label
    Private lblSpeed As Label
    Private lblSize As Label
    Private lblETA As Label
    Private rtbLog As RichTextBox
    Private btnCancel As Button

    ' Priority order for hosts (most reliable first)
    Private Shared ReadOnly HostPriority As String() = {
        "mediafire.com", "1fichier.com", "gofile.io",
        "vikingfile.com", "akirabox.com", "rootz.so", "1cloudfile.com"
    }

    ''' <summary>
    ''' The path of the successfully downloaded file.
    ''' </summary>
    Public Property DownloadedFilePath As String = ""

    Public Sub New(downloadLinks As List(Of HostLink), outputFolder As String)
        _downloadLinks = downloadLinks
        _outputFolder = outputFolder
        _downloadService = New DirectDownloadService()
        _cancellationTokenSource = New CancellationTokenSource()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Download (Auto-Failover)"
        Me.Size = New Size(520, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False

        Dim y = 12

        ' Current host label
        lblCurrentHost = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(475, 22),
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .Text = "Preparing...",
            .AutoEllipsis = True
        }
        Me.Controls.Add(lblCurrentHost)
        y += 26

        ' File name label
        lblFileName = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(475, 18),
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .Text = "",
            .AutoEllipsis = True
        }
        Me.Controls.Add(lblFileName)
        y += 24

        ' Progress bar
        prgDownload = New ProgressBar With {
            .Location = New Point(15, y),
            .Size = New Size(415, 22),
            .Minimum = 0,
            .Maximum = 100,
            .Style = ProgressBarStyle.Continuous
        }
        Me.Controls.Add(prgDownload)

        lblPercent = New Label With {
            .Location = New Point(435, y + 2),
            .Size = New Size(55, 18),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Text = "0%",
            .TextAlign = ContentAlignment.MiddleRight
        }
        Me.Controls.Add(lblPercent)
        y += 28

        ' Speed and ETA
        lblSpeed = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(200, 16),
            .Font = New Font("Segoe UI", 8.25F),
            .Text = "Speed: --"
        }
        Me.Controls.Add(lblSpeed)

        lblETA = New Label With {
            .Location = New Point(250, y),
            .Size = New Size(240, 16),
            .Font = New Font("Segoe UI", 8.25F),
            .Text = "",
            .TextAlign = ContentAlignment.MiddleRight
        }
        Me.Controls.Add(lblETA)
        y += 18

        ' Size label
        lblSize = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(475, 16),
            .Font = New Font("Segoe UI", 8.25F),
            .Text = ""
        }
        Me.Controls.Add(lblSize)
        y += 24

        ' Failover log
        Dim lblLogHeader As New Label With {
            .Location = New Point(15, y),
            .Size = New Size(475, 16),
            .Font = New Font("Segoe UI", 8.25F, FontStyle.Bold),
            .Text = "Host Attempts:"
        }
        Me.Controls.Add(lblLogHeader)
        y += 18

        rtbLog = New RichTextBox With {
            .Location = New Point(15, y),
            .Size = New Size(475, 110),
            .Font = New Font("Consolas", 8.5F),
            .ReadOnly = True,
            .BackColor = Color.FromArgb(30, 30, 30),
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        Me.Controls.Add(rtbLog)
        y += 118

        ' Cancel button
        btnCancel = New Button With {
            .Text = "Cancel",
            .Location = New Point(400, y),
            .Size = New Size(90, 30),
            .BackColor = Color.FromArgb(200, 50, 50),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click
        Me.Controls.Add(btnCancel)

        AddHandler Me.Shown, AddressOf Form_Shown
        AddHandler Me.FormClosing, AddressOf Form_FormClosing
    End Sub

    Private Async Sub Form_Shown(sender As Object, e As EventArgs)
        _startTime = DateTime.Now
        Await StartDownloadWithFailover()
    End Sub

    Private Async Function StartDownloadWithFailover() As Task
        ' Sort links by host priority
        Dim sortedLinks = _downloadLinks.OrderBy(
            Function(l)
                Dim idx = Array.FindIndex(HostPriority,
                    Function(p) l.Url.ToLower().Contains(p))
                Return If(idx >= 0, idx, 999)
            End Function).ToList()

        Dim totalHosts = sortedLinks.Count

        For i = 0 To sortedLinks.Count - 1
            Dim link = sortedLinks(i)
            Dim hostName = link.Host
            Dim hostUrl = link.Url

            ' Update UI
            lblCurrentHost.Text = $"Trying: {hostName} ({i + 1}/{totalHosts})..."
            prgDownload.Value = 0
            lblPercent.Text = "0%"
            lblSpeed.Text = "Speed: --"
            lblETA.Text = ""
            lblSize.Text = ""

            LogAttempt(hostName, $"Connecting to {hostName}...", Color.Cyan)

            Try
                Dim progress As New Progress(Of DownloadProgressInfo)(AddressOf UpdateProgress)
                DownloadedFilePath = Await _downloadService.DownloadFileAsync(
                    hostUrl, _outputFolder, progress, _cancellationTokenSource.Token)

                ' Success!
                LogAttempt(hostName, $"{hostName}: Download complete!", Color.LimeGreen)
                lblCurrentHost.Text = $"Complete: Downloaded from {hostName}"
                Me.DialogResult = DialogResult.OK
                Me.Close()
                Return

            Catch ex As OperationCanceledException
                LogAttempt(hostName, $"{hostName}: Cancelled by user", Color.Yellow)
                CleanupPartialFile()
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return

            Catch ex As Exception
                Dim reason = ex.Message
                If reason.Length > 120 Then reason = reason.Substring(0, 120) & "..."
                LogAttempt(hostName, $"{hostName}: Failed - {reason}", Color.OrangeRed)
            End Try

            ' Pause before trying next host (only reached on failure since success/cancel Return above)
            If i < sortedLinks.Count - 1 Then
                LogAttempt("", "Trying next host...", Color.Gray)
            End If
            Await Task.Delay(500)
        Next

        ' All hosts failed
        LogAttempt("", "All hosts failed.", Color.Red)
        lblCurrentHost.Text = "All download hosts failed"

        Dim msgResult = MessageBox.Show(
            $"All {totalHosts} download hosts failed.{vbCrLf}{vbCrLf}" &
            "Would you like to open the first link in your browser?",
            "Download Failed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

        If msgResult = DialogResult.Yes Then
            Try
                Process.Start(New ProcessStartInfo With {
                    .FileName = sortedLinks(0).Url,
                    .UseShellExecute = True
                })
            Catch
            End Try
        End If

        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Function

    Private Sub UpdateProgress(info As DownloadProgressInfo)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() UpdateProgress(info))
            Return
        End If

        If Me.IsDisposed Then Return

        If Not String.IsNullOrEmpty(info.FileName) Then
            lblFileName.Text = info.FileName
        End If

        If info.PercentComplete >= 0 AndAlso info.PercentComplete <= 100 Then
            prgDownload.Value = info.PercentComplete
            lblPercent.Text = $"{info.PercentComplete}%"
        End If

        If info.SpeedBytesPerSec > 0 Then
            lblSpeed.Text = $"Speed: {DirectDownloadService.FormatSpeed(info.SpeedBytesPerSec)}"
        End If

        If info.TotalBytes > 0 Then
            lblSize.Text = $"{DirectDownloadService.FormatBytes(info.DownloadedBytes)} / {DirectDownloadService.FormatBytes(info.TotalBytes)}"
        ElseIf info.DownloadedBytes > 0 Then
            lblSize.Text = $"Downloaded: {DirectDownloadService.FormatBytes(info.DownloadedBytes)}"
        End If

        If info.SpeedBytesPerSec > 0 AndAlso info.TotalBytes > 0 Then
            Dim remaining = info.TotalBytes - info.DownloadedBytes
            Dim secondsLeft = remaining / info.SpeedBytesPerSec
            If secondsLeft < 60 Then
                lblETA.Text = $"~{CInt(secondsLeft)} sec remaining"
            ElseIf secondsLeft < 3600 Then
                lblETA.Text = $"~{CInt(secondsLeft / 60)} min remaining"
            Else
                lblETA.Text = $"~{CInt(secondsLeft / 3600)}h {CInt((secondsLeft Mod 3600) / 60)}m remaining"
            End If
        End If

        If Not String.IsNullOrEmpty(info.Status) AndAlso
           Not info.Status.StartsWith("Downloading") Then
            lblCurrentHost.Text = info.Status
        End If
    End Sub

    Private Sub LogAttempt(host As String, message As String, color As Color)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() LogAttempt(host, message, color))
            Return
        End If

        If Me.IsDisposed Then Return

        Dim timestamp = DateTime.Now.ToString("HH:mm:ss")
        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionColor = Color.Gray
        rtbLog.AppendText($"[{timestamp}] ")
        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionColor = color
        rtbLog.AppendText(message & vbCrLf)
        rtbLog.ScrollToCaret()
    End Sub

    Private Sub CleanupPartialFile()
        If Not String.IsNullOrEmpty(DownloadedFilePath) AndAlso IO.File.Exists(DownloadedFilePath) Then
            Try
                IO.File.Delete(DownloadedFilePath)
            Catch
            End Try
        End If
        DownloadedFilePath = ""
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        btnCancel.Enabled = False
        btnCancel.Text = "Cancelling..."
        _cancellationTokenSource?.Cancel()
    End Sub

    Private Sub Form_FormClosing(sender As Object, e As FormClosingEventArgs)
        If e.CloseReason = CloseReason.UserClosing AndAlso Me.DialogResult = DialogResult.None Then
            _cancellationTokenSource?.Cancel()
            e.Cancel = True
        End If
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _cancellationTokenSource?.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
