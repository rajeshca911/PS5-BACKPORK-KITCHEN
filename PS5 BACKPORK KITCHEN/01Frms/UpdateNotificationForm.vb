Imports System.Windows.Forms

''' <summary>
''' Modern, non-invasive update notification dialog.
''' </summary>
Public Class UpdateNotificationForm
    Inherits Form

    Private lblTitle As Label
    Private lblCurrentVersion As Label
    Private lblNewVersion As Label
    Private txtReleaseNotes As TextBox
    Private btnDownload As Button
    Private btnRemindLater As Button
    Private btnSkipVersion As Button
    Private chkDontShowAgain As CheckBox

    Private _result As UpdateCheckerService.UpdateDecision = UpdateCheckerService.UpdateDecision.Cancelled

    Public ReadOnly Property UserDecision As UpdateCheckerService.UpdateDecision
        Get
            Return _result
        End Get
    End Property

    Public Sub New(updateInfo As UpdateCheckerService.UpdateCheckResult)
        InitializeComponent()
        PopulateUpdateInfo(updateInfo)
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Update Available"
        Me.Size = New Size(550, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowIcon = True
        Me.BackColor = Color.White

        ' Title
        lblTitle = New Label With {
            .Text = "🎉 A New Version is Available!",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.FromArgb(0, 120, 215),
            .Location = New Point(20, 20),
            .Size = New Size(510, 30),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblTitle)

        ' Current version label
        lblCurrentVersion = New Label With {
            .Text = "Current Version: ",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(20, 70),
            .Size = New Size(510, 20)
        }
        Me.Controls.Add(lblCurrentVersion)

        ' New version label
        lblNewVersion = New Label With {
            .Text = "Latest Version: ",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.FromArgb(0, 150, 0),
            .Location = New Point(20, 95),
            .Size = New Size(510, 20)
        }
        Me.Controls.Add(lblNewVersion)

        ' Release notes
        Dim lblReleaseNotesTitle = New Label With {
            .Text = "What's New:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, 130),
            .Size = New Size(510, 20)
        }
        Me.Controls.Add(lblReleaseNotesTitle)

        txtReleaseNotes = New TextBox With {
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .Location = New Point(20, 155),
            .Size = New Size(510, 130),
            .Font = New Font("Consolas", 9),
            .BackColor = Color.FromArgb(245, 245, 245),
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtReleaseNotes)

        ' Checkbox
        chkDontShowAgain = New CheckBox With {
            .Text = "Don't check for updates automatically",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 295),
            .Size = New Size(300, 20)
        }
        Me.Controls.Add(chkDontShowAgain)

        ' Buttons
        btnDownload = New Button With {
            .Text = "Download Now",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(150, 35),
            .Location = New Point(20, 325),
            .BackColor = Color.FromArgb(0, 120, 215),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnDownload.FlatAppearance.BorderSize = 0
        AddHandler btnDownload.Click, AddressOf BtnDownload_Click
        Me.Controls.Add(btnDownload)

        btnRemindLater = New Button With {
            .Text = "Remind Me Later",
            .Font = New Font("Segoe UI", 9),
            .Size = New Size(150, 35),
            .Location = New Point(190, 325),
            .BackColor = Color.FromArgb(240, 240, 240),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        AddHandler btnRemindLater.Click, AddressOf BtnRemindLater_Click
        Me.Controls.Add(btnRemindLater)

        btnSkipVersion = New Button With {
            .Text = "Skip This Version",
            .Font = New Font("Segoe UI", 9),
            .Size = New Size(150, 35),
            .Location = New Point(360, 325),
            .BackColor = Color.FromArgb(240, 240, 240),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        AddHandler btnSkipVersion.Click, AddressOf BtnSkipVersion_Click
        Me.Controls.Add(btnSkipVersion)
    End Sub

    Private Sub PopulateUpdateInfo(updateInfo As UpdateCheckerService.UpdateCheckResult)
        lblCurrentVersion.Text = $"Current Version: v{updateInfo.CurrentVersion}"
        lblNewVersion.Text = $"Latest Version: v{updateInfo.LatestVersion}"

        If Not String.IsNullOrEmpty(updateInfo.ReleaseNotes) Then
            ' Format release notes nicely
            Dim notes = updateInfo.ReleaseNotes
            If notes.Length > 500 Then
                notes = notes.Substring(0, 500) & vbCrLf & vbCrLf & "... (see full release notes on GitHub)"
            End If
            txtReleaseNotes.Text = notes
        Else
            txtReleaseNotes.Text = "See full release notes on GitHub."
        End If
    End Sub

    Private Sub BtnDownload_Click(sender As Object, e As EventArgs)
        _result = UpdateCheckerService.UpdateDecision.DownloadNow

        If chkDontShowAgain.Checked Then
            UpdateCheckerService.SetAutoUpdateEnabled(False)
        End If

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub BtnRemindLater_Click(sender As Object, e As EventArgs)
        _result = UpdateCheckerService.UpdateDecision.RemindLater

        If chkDontShowAgain.Checked Then
            UpdateCheckerService.SetAutoUpdateEnabled(False)
        End If

        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub BtnSkipVersion_Click(sender As Object, e As EventArgs)
        _result = UpdateCheckerService.UpdateDecision.SkipVersion

        If chkDontShowAgain.Checked Then
            UpdateCheckerService.SetAutoUpdateEnabled(False)
        End If

        Me.DialogResult = DialogResult.Ignore
        Me.Close()
    End Sub

End Class