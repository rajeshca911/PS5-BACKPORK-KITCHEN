Imports System.IO
Imports System.Xml

Public Class UcUfsImageBuilder
    Private advancedVisible As Boolean = False

    Private Sub UcUfsImageBuilder_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub lnkAdvanced_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkAdvanced.LinkClicked
        advancedVisible = Not advancedVisible
        pnlAdvanced.Visible = advancedVisible

        lnkAdvanced.Text =
            If(advancedVisible,
               "Hide Advanced ▲",
               "Show Advanced ▼")
    End Sub
    Private Sub ApplyPresetLock()
        Dim isCustom = rdoCustom.Checked

        For Each c As Control In pnlAdvanced.Controls
            c.Enabled = isCustom
        Next
    End Sub

    Private Sub rdoPreset_CheckedChanged(sender As Object, e As EventArgs) Handles rdoPreset.CheckedChanged
        ApplyPresetLock()
    End Sub

    Private Sub rdoCustom_CheckedChanged(sender As Object, e As EventArgs) Handles rdoCustom.CheckedChanged
        ApplyPresetLock()
    End Sub
    Private Sub LoadPs5Defaults()
        txtVolumeLabel.Text = "PS5VOL"
        cmbBlockSize.SelectedIndex = 2   '65536
        cmbOptimize.SelectedIndex = 0    'Speed
        txtExtraFlags.Clear()
    End Sub

    Private Sub btnBrowseSource_Click(sender As Object, e As EventArgs) Handles btnBrowseSource.Click
        Using f As New FolderBrowserDialog
            If f.ShowDialog = DialogResult.OK Then
                txtSourceFolder.Text = f.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnBrowseOutput_Click(sender As Object, e As EventArgs) Handles btnBrowseOutput.Click
        Using s As New SaveFileDialog
            s.Filter = "UFS Image (*.img)|*.img|All Files|*.*"
            s.FileName = "image.img"

            If s.ShowDialog = DialogResult.OK Then
                txtOutputImage.Text = s.FileName
            End If
        End Using
    End Sub
    Private Sub ValidateInputs()

        Dim ok =
        Directory.Exists(txtSourceFolder.Text) AndAlso
        Not String.IsNullOrWhiteSpace(txtOutputImage.Text)
        If ok Then
            ok = Directory.EnumerateFileSystemEntries(
            txtSourceFolder.Text).Any()
        End If

        btnGenerate.Enabled = ok

    End Sub

    Private Sub txtSourceFolder_TextChanged(sender As Object, e As EventArgs) Handles txtSourceFolder.TextChanged
        ValidateInputs()
    End Sub

    Private Sub txtOutputImage_TextChanged(sender As Object, e As EventArgs) Handles txtOutputImage.TextChanged
        ValidateInputs()
    End Sub
    Private Sub SetRunningState(running As Boolean)

        btnGenerate.Enabled = Not running
        btnStop.Enabled = running

        txtSourceFolder.Enabled = Not running
        txtOutputImage.Enabled = Not running
        btnBrowseSource.Enabled = Not running
        btnBrowseOutput.Enabled = Not running

    End Sub
    Private Sub Log(msg As String)
        rtbLog.AppendText(msg & Environment.NewLine)
        rtbLog.ScrollToCaret()
    End Sub
    Private Function CollectOptions() As UfsBuildOptions

        Return New UfsBuildOptions With {
        .SourceFolder = txtSourceFolder.Text,
        .OutputFile = txtOutputImage.Text,
        .UsePs5Preset = rdoPreset.Checked,
        .VolumeLabel = txtVolumeLabel.Text,
        .BlockSize = cmbBlockSize.Text,
        .OptimizeMode = cmbOptimize.Text,
        .ExtraFlags = txtExtraFlags.Text
    }

    End Function
    Private Sub UpdateCommandPreview()

        Dim opts = CollectOptions()

        Dim exePath = My.Settings.Default.UfsExePath

        If Not IO.File.Exists(exePath) Then
            txtCommandPreview.Text = "ufs2tool.exe not found"
            Return
        End If

        txtCommandPreview.Text =
        UfsCommandBuilder.Build(opts, exePath)

    End Sub

    Private Sub btnGenerate_Click(sender As Object, e As EventArgs) Handles btnGenerate.Click
        'My.Settings.Default.UfsExePath = txtSourceFolder.Text.Trim
        'My.Settings.Default.Save()
        Dim exePath = Path.Combine(
    Application.StartupPath,
    "tools",
    "ufs2tool.exe")

        If Not File.Exists(exePath) Then
            Log("UFS2Tool not found.")
            MessageBox.Show("UFS2Tool.exe missing in tools folder.")
            Return
        End If

    End Sub
    Private Function CheckUFSTool() As Boolean
        Dim exePath = Path.Combine(
    Application.StartupPath,
    "tools",
    "ufs2tool.exe")
        If Not File.Exists(exePath) Then
            Log("UFS2Tool not found.")
            MessageBox.Show("UFS2Tool.exe missing in tools folder.")
            'first download attempt
            'Dim downloadtool As Boolean = UfsToolDownloader.DownloadUfsTool(exePath)
            Return False
        End If
        Return True
    End Function
End Class
