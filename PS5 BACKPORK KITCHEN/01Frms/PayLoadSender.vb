Imports System.Net
Imports PS5_BACKPORK_KITCHEN.PayloadSenderService
Imports PS5_BACKPORK_KITCHEN.PS5_Payload_Sender
Imports ReaLTaiizor.Controls

Public Class PayLoadSender

    Private Sub AirForm1_Click(sender As Object, e As EventArgs) Handles AirForm1.Click

    End Sub

    Private _tooltipService As TooltipService

    Private Sub PayLoadSender_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub BtnSend_Click(Bsender As Object, e As EventArgs) Handles BtnSend.Click
        Try
            ' Validate inputs
            If String.IsNullOrWhiteSpace(TxtPayLoad.Text) OrElse
           String.IsNullOrWhiteSpace(txtIP.Text) OrElse
           String.IsNullOrWhiteSpace(TxtPort.Text) Then

                ToastService.Show("Enter IP, Port and Payload.", ToastForm.ToastType.Error)
                Exit Sub
            End If

            ' Validate file exists
            Dim payloadPath As String = TxtPayLoad.Text.Trim()

            If Not IO.File.Exists(payloadPath) Then
                ToastService.Show("Payload file not found.", ToastForm.ToastType.Error)
                Exit Sub
            End If

            ' Validate IP
            Dim ipAddress As IPAddress = Nothing
            If Not IPAddress.TryParse(txtIP.Text.Trim(), ipAddress) Then
                ToastService.Show("Invalid IP address.", ToastForm.ToastType.Error)
                Exit Sub
            End If

            ' Parse port (fallback to default)
            Dim portNo As Integer
            If Not Integer.TryParse(TxtPort.Text.Trim(), portNo) Then
                portNo = 9021
            End If

            ' Improve UX
            BtnSend.Enabled = False
            BtnSend.Text = "Sending..."

            ToastService.Show("Connecting...", ToastForm.ToastType.Info)

            ' ALWAYS use Using for IDisposable classes
            Using sender As New TcpPayloadSender()

                If Not sender.Connect(txtIP.Text.Trim(), 9021) Then
                    ToastService.Show(sender.LastError, ToastForm.ToastType.Error)
                    Exit Sub
                End If

                If sender.SendPayload(TxtPayLoad.Text.Trim()) Then
                    ToastService.Show("Payload sent successfully!", ToastForm.ToastType.Success)
                Else
                    ToastService.Show(sender.LastError, ToastForm.ToastType.Error)
                End If

            End Using
        Catch ex As Exception

            ' Avoid MessageBox in tools unless critical
            ToastService.Show($"Unexpected error: {ex.Message}", ToastForm.ToastType.Error)
        Finally

            BtnSend.Enabled = True
            BtnSend.Text = "Send"

        End Try
    End Sub

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs) Handles BtnBrowse.Click
        'ToastService.Show("Payload sent!", ToastForm.ToastType.Success)
        Dim openFileDialog As New OpenFileDialog()
        openFileDialog.Title = "Select Payload File"
        openFileDialog.Filter = "Payload Files (*.bin;*.elf)|*.bin;*.elf|All Files (*.*)|*.*"

        If openFileDialog.ShowDialog() = DialogResult.OK Then
            TxtPayLoad.Text = openFileDialog.FileName
        End If
    End Sub

End Class