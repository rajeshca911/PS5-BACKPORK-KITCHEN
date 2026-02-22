Public Class CreditsForm

    Private Sub CreditsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        fixmysize(Me)
    End Sub

    Private Sub NightLinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel1.LinkClicked
        'backpork
        OpenURL(backpork)
    End Sub

    Private Sub NightLinkLabel2_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel2.LinkClicked
        OpenURL(selfutilurl)
    End Sub

    Private Sub NightLinkLabel3_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel3.LinkClicked
        OpenURL(Idlesauce)
    End Sub

    Private Sub NightLinkLabel4_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel4.LinkClicked
        OpenURL(makeself)
    End Sub

    Private Sub SkyLabel7_Click(sender As Object, e As EventArgs) Handles SkyLabel7.Click
        OpenURL(mytwitter)
    End Sub

    Private Sub SkyLabel8_Click(sender As Object, e As EventArgs) Handles SkyLabel8.Click
        OpenURL("https://github.com/rajeshca911")
    End Sub

    Private Sub NightLinkLabel5_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel5.LinkClicked
        OpenURL("https://github.com/EchoStretch")
    End Sub

    Private Sub NightLinkLabel6_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NightLinkLabel6.LinkClicked
        OpenURL("https://github.com/DroneTechTI")
    End Sub

End Class