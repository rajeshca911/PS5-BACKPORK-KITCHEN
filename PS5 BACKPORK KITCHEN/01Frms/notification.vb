Public Class notification

    Private Sub HopePictureBox1_Click(sender As Object, e As EventArgs) Handles HopePictureBox1.Click
        Close()
    End Sub

    Private Sub notification_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub HopePictureBox2_Click(sender As Object, e As EventArgs) Handles HopePictureBox2.Click
        If Me.WindowState = FormWindowState.Normal Then
            Me.WindowState = FormWindowState.Maximized
        Else
            Me.WindowState = FormWindowState.Normal
        End If
    End Sub

End Class