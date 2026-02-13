Public Class restoreform

    Private Sub restoreform_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        fixmysize(Me)
    End Sub

    Private Sub DreamButton3_Click(sender As Object, e As EventArgs) Handles DreamButton3.Click

        If String.IsNullOrEmpty(txtbkp.Text) Or String.IsNullOrEmpty(txtppsa.Text) Then
            MessageBox.Show("Please select both backup and game folders.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Exit Sub
        End If
        'confirm message
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to restore backups? This will overwrite existing files.", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If result = DialogResult.No Then
            Exit Sub
        End If
        'first restore original names from backup folder to temp folder
        updatestatus("Restoring backup files...", 2)
        RestoreRajFiles(txtbkp.Text)
        updatestatus("Copying files back to game folder...", 2)
        CopyRelative(txtbkp.Text, txtppsa.Text, True)
        MakeRajFiles(txtbkp.Text)
        txtppsa.Text = ""
        txtbkp.Text = ""
        MessageBox.Show("Restore completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        updatestatus()

    End Sub

    Private Function getfolder(description As String) As String
        Using fbd As New FolderBrowserDialog()
            fbd.Description = description
            If fbd.ShowDialog() = DialogResult.OK Then
                Return fbd.SelectedPath
            End If
        End Using
        Return String.Empty
    End Function

    Private Sub btnBkp_Click(sender As Object, e As EventArgs) Handles btnBkp.Click
        txtbkp.Text = getfolder("Select Backup Folder")

    End Sub

    Private Sub BtnGame_Click(sender As Object, e As EventArgs) Handles BtnGame.Click
        txtppsa.Text = getfolder("Select Game Folder")
    End Sub

    Private Sub ForeverLabel3_Click(sender As Object, e As EventArgs) Handles ForeverLabel3.Click
        Me.Close()
    End Sub

End Class