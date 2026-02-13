Module messagemodule

    Public Sub ShowNotification(
      msg As String,
      Optional heading As String = "Attention",
      Optional title As String = "",
      Optional bkcolor As Color = Nothing,
      Optional fcolor As Color = Nothing
  )

        Using frm As New notification
            frm.Text = heading
            frm.lblheading.Text = title

            With frm.RichTextBox1
                .Clear()

                .BackColor = If(bkcolor = Nothing, Color.Black, bkcolor)
                .ForeColor = If(fcolor = Nothing, Color.White, fcolor)
                .Text = msg
                .SelectionStart = 0
            End With

            frm.ShowDialog()
        End Using

    End Sub

End Module