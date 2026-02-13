Public Class ToastForm

    Private fadeTimer As Timer
    Private lifeTimer As Timer

    Public Enum ToastType
        Success
        [Error]
        Info
    End Enum

    Public Sub New(message As String, type As ToastType)

        InitializeComponent()

        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.TopMost = True
        Me.ShowInTaskbar = False
        Me.Width = 320
        Me.Height = 80
        Me.Opacity = 0

        Dim lbl As New Label With {
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Text = message
        }

        Me.Controls.Add(lbl)

        Select Case type
            Case ToastType.Success
                Me.BackColor = Color.FromArgb(46, 204, 113)

            Case ToastType.Error
                Me.BackColor = Color.FromArgb(231, 76, 60)

            Case ToastType.Info
                Me.BackColor = Color.FromArgb(52, 152, 219)
        End Select

    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Dim area = Screen.PrimaryScreen.WorkingArea

        Me.Left = area.Right - Me.Width - 10
        Me.Top = area.Bottom

        ' Slide up
        Dim slideTimer As New Timer With {.Interval = 10}

        AddHandler slideTimer.Tick,
        Sub()
            If Me.Top > area.Bottom - Me.Height - 10 Then
                Me.Top -= 5
                Me.Opacity += 0.08
            Else
                slideTimer.Stop()
                StartLifeTimer()
            End If
        End Sub

        slideTimer.Start()

    End Sub

    Private Sub StartLifeTimer()

        lifeTimer = New Timer With {.Interval = 2500}

        AddHandler lifeTimer.Tick,
        Sub()
            lifeTimer.Stop()
            StartFadeOut()
        End Sub

        lifeTimer.Start()

    End Sub

    Private Sub StartFadeOut()

        fadeTimer = New Timer With {.Interval = 20}

        AddHandler fadeTimer.Tick,
        Sub()
            If Me.Opacity > 0 Then
                Me.Opacity -= 0.05
            Else
                fadeTimer.Stop()
                Me.Close()
            End If
        End Sub

        fadeTimer.Start()

    End Sub

End Class