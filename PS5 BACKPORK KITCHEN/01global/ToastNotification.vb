Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>
''' Modern toast notification system for user feedback
''' </summary>
Public Class ToastNotification
    Inherits Form

    Public Enum ToastType
        Success
        Info
        Warning
        [Error]
    End Enum

    Private currentType As ToastType
    Private message As String
    Private WithEvents fadeTimer As Timer
    Private opacity As Double = 0

    Public Sub New(message As String, type As ToastType, Optional durationMs As Integer = 3000)
        Me.message = message
        Me.currentType = type

        InitializeToast()
        PositionToast()

        ' Auto-close timer
        Dim closeTimer As New Timer With {.Interval = durationMs}
        AddHandler closeTimer.Tick, Sub()
                                        closeTimer.Stop()
                                        FadeOut()
                                    End Sub
        closeTimer.Start()

        ' Fade in
        FadeIn()
    End Sub

    Private Sub InitializeToast()
        ' Form settings
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.Size = New Size(350, 80)
        Me.ShowInTaskbar = False
        Me.TopMost = True
        Me.opacity = 0

        ' Get colors based on type
        Dim bgColor As Color
        Dim iconText As String

        Select Case currentType
            Case ToastType.Success
                bgColor = Color.FromArgb(46, 204, 113)
                iconText = "✓"
            Case ToastType.Info
                bgColor = Color.FromArgb(52, 152, 219)
                iconText = "ℹ"
            Case ToastType.Warning
                bgColor = Color.FromArgb(241, 196, 15)
                iconText = "⚠"
            Case ToastType.Error
                bgColor = Color.FromArgb(231, 76, 60)
                iconText = "✖"
        End Select

        Me.BackColor = bgColor

        ' Create panel with rounded corners
        Dim pnl As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = bgColor,
            .Padding = New Padding(15, 10, 15, 10)
        }

        ' Icon label
        Dim lblIcon As New Label With {
            .Text = iconText,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(15, 20)
        }
        pnl.Controls.Add(lblIcon)

        '' Message label
        'Dim lblMessage As New Label With {
        '    .Text = message,
        '    .Font = New Font("Segoe UI", 10, FontStyle.Regular),
        '    .ForeColor = Color.White,
        '    .Location = New Point(60, 15),
        '    .MaximumSize = New Size(270, 50),
        '    .AutoSize = True
        '}
        Dim msgFont As New Font("Segoe UI", 10, FontStyle.Regular)

        Dim maxTextWidth As Integer = 420

        Dim textSize As Size = TextRenderer.MeasureText(
    message,
    msgFont,
    New Size(maxTextWidth, Integer.MaxValue),
    TextFormatFlags.WordBreak)

        Dim lblMessage As New Label With {
    .Text = message,
    .Font = msgFont,
    .ForeColor = Color.White,
    .Location = New Point(60, 15),
    .Size = textSize,
    .AutoSize = False
}

        pnl.Controls.Add(lblMessage)

        ' Close button (×)
        Dim btnClose As New Label With {
            .Text = "×",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(Me.Width - 30, 5),
            .Cursor = Cursors.Hand
        }
        AddHandler btnClose.Click, Sub() FadeOut()
        pnl.Controls.Add(btnClose)

        Me.Controls.Add(pnl)

        ' Round corners
        Dim path As New GraphicsPath()
        Dim radius As Integer = 10
        path.AddArc(0, 0, radius, radius, 180, 90)
        path.AddArc(Me.Width - radius, 0, radius, radius, 270, 90)
        path.AddArc(Me.Width - radius, Me.Height - radius, radius, radius, 0, 90)
        path.AddArc(0, Me.Height - radius, radius, radius, 90, 90)
        path.CloseFigure()
        Me.Region = New Region(path)

        ' Click to close
        AddHandler Me.Click, Sub() FadeOut()
        AddHandler pnl.Click, Sub() FadeOut()
        AddHandler lblMessage.Click, Sub() FadeOut()
    End Sub

    Private Sub PositionToast()
        ' Position at bottom-right of screen with margin
        Dim workingArea = Screen.PrimaryScreen.WorkingArea
        Dim margin = 20

        ' Stack toasts if multiple are shown
        Dim existingToasts = Application.OpenForms.OfType(Of ToastNotification)().Count()
        Dim verticalOffset = existingToasts * (Me.Height + 10)

        Me.Left = workingArea.Right - Me.Width - margin
        Me.Top = workingArea.Bottom - Me.Height - margin - verticalOffset
    End Sub

    Private Sub FadeIn()
        fadeTimer = New Timer With {.Interval = 20}
        AddHandler fadeTimer.Tick, Sub(s, e)
                                       opacity += 0.05
                                       If opacity >= 1 Then
                                           opacity = 1
                                           fadeTimer.Stop()
                                       End If
                                       Me.opacity = opacity
                                   End Sub
        fadeTimer.Start()
    End Sub

    Private Sub FadeOut()
        fadeTimer?.Stop()
        fadeTimer = New Timer With {.Interval = 20}
        AddHandler fadeTimer.Tick, Sub(s, e)
                                       opacity -= 0.05
                                       If opacity <= 0 Then
                                           opacity = 0
                                           fadeTimer.Stop()
                                           Me.Close()
                                           Me.Dispose()
                                       End If
                                       Me.opacity = opacity
                                   End Sub
        fadeTimer.Start()
    End Sub

    ''' <summary>
    ''' Show a toast notification
    ''' </summary>
    Public Shared Sub Show(message As String, type As ToastType, Optional durationMs As Integer = 3000)
        Dim toast As New ToastNotification(message, type, durationMs)
        CType(toast, Form).Show()
    End Sub

    ''' <summary>
    ''' Show success toast
    ''' </summary>
    Public Shared Sub ShowSuccess(message As String, Optional durationMs As Integer = 3000)
        Show(message, ToastType.Success, durationMs)
    End Sub

    ''' <summary>
    ''' Show info toast
    ''' </summary>
    Public Shared Sub ShowInfo(message As String, Optional durationMs As Integer = 3000)
        Show(message, ToastType.Info, durationMs)
    End Sub

    ''' <summary>
    ''' Show warning toast
    ''' </summary>
    Public Shared Sub ShowWarning(message As String, Optional durationMs As Integer = 3000)
        Show(message, ToastType.Warning, durationMs)
    End Sub

    ''' <summary>
    ''' Show error toast
    ''' </summary>
    Public Shared Sub ShowError(message As String, Optional durationMs As Integer = 4000)
        Show(message, ToastType.Error, durationMs)
    End Sub

End Class