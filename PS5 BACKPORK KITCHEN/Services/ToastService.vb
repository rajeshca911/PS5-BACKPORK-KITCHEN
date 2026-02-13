Public Module ToastService

    Public Sub Show(message As String,
                    Optional type As ToastForm.ToastType = ToastForm.ToastType.Info)

        If Application.OpenForms.Count = 0 Then Return

        Dim owner = Application.OpenForms(0)

        If owner.InvokeRequired Then
            owner.Invoke(Sub() Show(message, type))
            Return
        End If

        Dim toast As New ToastForm(message, type)
        toast.Show()

    End Sub

End Module