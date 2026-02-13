Imports PS5_BACKPORK_KITCHEN.Services.GameSearch
Imports System.Windows.Forms
Imports System.Threading

''' <summary>
''' Modal dialog showing download progress for a single file.
''' Displays filename, host, progress bar, speed, size, and ETA.
''' </summary>
Public Class DownloadProgressForm
    Inherits Form

    Private ReadOnly _downloadUrl As String
    Private ReadOnly _outputFolder As String
    Private _cancellationTokenSource As CancellationTokenSource
    Private _downloadService As DirectDownloadService
    Private _startTime As DateTime

    ' UI Controls
    Private lblFileName As Label
    Private lblHost As Label
    Private prgDownload As ProgressBar
    Private lblPercent As Label
    Private lblSpeed As Label
    Private lblSize As Label
    Private lblETA As Label
    Private btnCancel As Button

    ''' <summary>
    ''' The path of the successfully downloaded file.
    ''' </summary>
    Public Property DownloadedFilePath As String = ""

    Public Sub New(downloadUrl As String, outputFolder As String)
        _downloadUrl = downloadUrl
        _outputFolder = outputFolder
        _downloadService = New DirectDownloadService()
        _cancellationTokenSource = New CancellationTokenSource()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Download Progress"
        Me.Size = New Size(480, 260)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False

        Dim y = 15

        ' File name label
        lblFileName = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(440, 20),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Text = "Preparing download...",
            .AutoEllipsis = True
        }
        Me.Controls.Add(lblFileName)
        y += 25

        ' Host label
        lblHost = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(440, 18),
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.FromArgb(100, 100, 100),
            .Text = $"From: {DirectDownloadService.GetHostName(_downloadUrl)}"
        }
        Me.Controls.Add(lblHost)
        y += 28

        ' Progress bar
        prgDownload = New ProgressBar With {
            .Location = New Point(15, y),
            .Size = New Size(380, 25),
            .Minimum = 0,
            .Maximum = 100,
            .Style = ProgressBarStyle.Continuous
        }
        Me.Controls.Add(prgDownload)

        ' Percent label (next to progress bar)
        lblPercent = New Label With {
            .Location = New Point(400, y + 3),
            .Size = New Size(50, 20),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Text = "0%",
            .TextAlign = ContentAlignment.MiddleRight
        }
        Me.Controls.Add(lblPercent)
        y += 35

        ' Speed label
        lblSpeed = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(200, 18),
            .Font = New Font("Segoe UI", 8.5F),
            .Text = "Speed: --"
        }
        Me.Controls.Add(lblSpeed)

        ' ETA label
        lblETA = New Label With {
            .Location = New Point(220, y),
            .Size = New Size(230, 18),
            .Font = New Font("Segoe UI", 8.5F),
            .Text = "ETA: calculating...",
            .TextAlign = ContentAlignment.MiddleRight
        }
        Me.Controls.Add(lblETA)
        y += 22

        ' Size label
        lblSize = New Label With {
            .Location = New Point(15, y),
            .Size = New Size(435, 18),
            .Font = New Font("Segoe UI", 8.5F),
            .Text = "Size: --"
        }
        Me.Controls.Add(lblSize)
        y += 30

        ' Cancel button
        btnCancel = New Button With {
            .Text = "Cancel",
            .Location = New Point(365, y),
            .Size = New Size(85, 30),
            .BackColor = Color.FromArgb(200, 50, 50),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)

        AddHandler Me.Shown, AddressOf DownloadProgressForm_Shown
        AddHandler Me.FormClosing, AddressOf DownloadProgressForm_FormClosing
    End Sub

    Private Async Sub DownloadProgressForm_Shown(sender As Object, e As EventArgs)
        _startTime = DateTime.Now

        Dim progress As New Progress(Of DownloadProgressInfo)(AddressOf UpdateProgress)

        Try
            DownloadedFilePath = Await _downloadService.DownloadFileAsync(
                _downloadUrl, _outputFolder, progress, _cancellationTokenSource.Token)

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As OperationCanceledException
            ' Clean up partial file
            If Not String.IsNullOrEmpty(DownloadedFilePath) AndAlso IO.File.Exists(DownloadedFilePath) Then
                Try
                    IO.File.Delete(DownloadedFilePath)
                Catch
                End Try
            End If
            DownloadedFilePath = ""
            Me.DialogResult = DialogResult.Cancel
            Me.Close()

        Catch ex As Exception
            ' If the error suggests using browser, offer to open the URL
            If ex.Message.Contains("browser") OrElse ex.Message.Contains("403") OrElse
               ex.Message.Contains("Cloudflare") OrElse ex.Message.Contains("Forbidden") OrElse
               ex.Message.Contains("HTML page instead") OrElse ex.Message.Contains("small file") OrElse
               ex.Message.Contains("CAPTCHA") OrElse ex.Message.Contains("manually") Then
                Dim msgResult = MessageBox.Show(
                    $"Direct download failed: {ex.Message}{vbCrLf}{vbCrLf}Would you like to open the link in your browser instead?",
                    "Download Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If msgResult = DialogResult.Yes Then
                    Try
                        Process.Start(New ProcessStartInfo With {
                            .FileName = _downloadUrl,
                            .UseShellExecute = True
                        })
                    Catch
                    End Try
                End If
            Else
                MessageBox.Show($"Download failed: {ex.Message}", "Download Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
            DownloadedFilePath = ""
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Try
    End Sub

    Private Sub UpdateProgress(info As DownloadProgressInfo)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() UpdateProgress(info))
            Return
        End If

        If Me.IsDisposed Then Return

        ' Update filename
        If Not String.IsNullOrEmpty(info.FileName) Then
            lblFileName.Text = info.FileName
        End If

        ' Update progress bar
        If info.PercentComplete >= 0 AndAlso info.PercentComplete <= 100 Then
            prgDownload.Value = info.PercentComplete
            lblPercent.Text = $"{info.PercentComplete}%"
        End If

        ' Update speed
        If info.SpeedBytesPerSec > 0 Then
            lblSpeed.Text = $"Speed: {DirectDownloadService.FormatSpeed(info.SpeedBytesPerSec)}"
        End If

        ' Update size
        If info.TotalBytes > 0 Then
            lblSize.Text = $"Size: {DirectDownloadService.FormatBytes(info.DownloadedBytes)} / {DirectDownloadService.FormatBytes(info.TotalBytes)}"
        ElseIf info.DownloadedBytes > 0 Then
            lblSize.Text = $"Downloaded: {DirectDownloadService.FormatBytes(info.DownloadedBytes)}"
        End If

        ' Update ETA
        If info.SpeedBytesPerSec > 0 AndAlso info.TotalBytes > 0 Then
            Dim remaining = info.TotalBytes - info.DownloadedBytes
            Dim secondsLeft = remaining / info.SpeedBytesPerSec
            If secondsLeft < 60 Then
                lblETA.Text = $"~{CInt(secondsLeft)} sec remaining"
            ElseIf secondsLeft < 3600 Then
                lblETA.Text = $"~{CInt(secondsLeft / 60)} min remaining"
            Else
                Dim hours = CInt(secondsLeft / 3600)
                Dim mins = CInt((secondsLeft Mod 3600) / 60)
                lblETA.Text = $"~{hours}h {mins}m remaining"
            End If
        End If

        ' Update status
        If Not String.IsNullOrEmpty(info.Status) Then
            lblHost.Text = info.Status
        End If
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        btnCancel.Enabled = False
        btnCancel.Text = "Cancelling..."
        _cancellationTokenSource?.Cancel()
    End Sub

    Private Sub DownloadProgressForm_FormClosing(sender As Object, e As FormClosingEventArgs)
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
