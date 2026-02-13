Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Modern UI helper components for improved UX.
''' Provides toast notifications, loading overlays, and visual feedback.
''' </summary>
Public Module ModernUIHelpers

    ' ===========================
    ' TOAST NOTIFICATIONS
    ' ===========================

    ''' <summary>Toast notification icon types</summary>
    Public Enum ToastIcon
        Info
        Success
        Warning
        [Error]
    End Enum

    ''' <summary>Show non-intrusive toast notification</summary>
    Public Sub ShowToast(parentForm As Form, message As String, icon As ToastIcon, Optional durationMs As Integer = 3000)
        Try
            ' Create toast form
            Dim toast As New Form With {
                .FormBorderStyle = FormBorderStyle.None,
                .StartPosition = FormStartPosition.Manual,
                .ShowInTaskbar = False,
                .TopMost = True,
                .Size = New Size(350, 80),
                .BackColor = GetToastBackColor(icon),
                .Opacity = 0.0
            }

            ' Position bottom-right corner
            toast.Location = New Point(
                parentForm.Location.X + parentForm.Width - toast.Width - 20,
                parentForm.Location.Y + parentForm.Height - toast.Height - 60
            )

            ' Create icon label
            Dim iconLabel As New Label With {
                .Text = GetToastIconText(icon),
                .Font = New Font("Segoe UI Emoji", 24, FontStyle.Regular),
                .ForeColor = Color.White,
                .Location = New Point(10, 20),
                .Size = New Size(50, 40),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' Create message label
            Dim messageLabel As New Label With {
                .Text = message,
                .Font = New Font("Segoe UI", 10, FontStyle.Regular),
                .ForeColor = Color.White,
                .Location = New Point(65, 10),
                .Size = New Size(270, 60),
                .TextAlign = ContentAlignment.MiddleLeft
            }

            ' Add controls
            toast.Controls.Add(iconLabel)
            toast.Controls.Add(messageLabel)

            ' Add rounded corners (if possible)
            AddRoundedCorners(toast)

            ' Show toast
            toast.Show()

            ' Fade in animation
            Dim fadeInTimer As New Timer With {.Interval = 20}
            AddHandler fadeInTimer.Tick, Sub()
                                             If toast.Opacity < 0.95 Then
                                                 toast.Opacity += 0.1
                                             Else
                                                 fadeInTimer.Stop()

                                                 ' Wait duration then fade out
                                                 Dim waitTimer As New Timer With {.Interval = durationMs}
                                                 AddHandler waitTimer.Tick, Sub()
                                                                                waitTimer.Stop()

                                                                                ' Fade out animation
                                                                                Dim fadeOutTimer As New Timer With {.Interval = 20}
                                                                                AddHandler fadeOutTimer.Tick, Sub()
                                                                                                                  If toast.Opacity > 0.1 Then
                                                                                                                      toast.Opacity -= 0.1
                                                                                                                  Else
                                                                                                                      fadeOutTimer.Stop()
                                                                                                                      toast.Close()
                                                                                                                      toast.Dispose()
                                                                                                                  End If
                                                                                                              End Sub
                                                                                fadeOutTimer.Start()
                                                                            End Sub
                                                 waitTimer.Start()
                                             End If
                                         End Sub
            fadeInTimer.Start()
        Catch ex As Exception
            Logger.LogToFile($"Error showing toast: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    ''' <summary>Get background color for toast type</summary>
    Private Function GetToastBackColor(icon As ToastIcon) As Color
        Select Case icon
            Case ToastIcon.Success
                Return ColorPalette.Success
            Case ToastIcon.Warning
                Return ColorPalette.Warning
            Case ToastIcon.Error
                Return ColorPalette.Error
            Case Else
                Return ColorPalette.Info
        End Select
    End Function

    ''' <summary>Get icon text for toast type</summary>
    Private Function GetToastIconText(icon As ToastIcon) As String
        Select Case icon
            Case ToastIcon.Success
                Return "✓"
            Case ToastIcon.Warning
                Return "⚠"
            Case ToastIcon.Error
                Return "✗"
            Case Else
                Return "ℹ"
        End Select
    End Function

    ' ===========================
    ' LOADING OVERLAY
    ' ===========================

    ''' <summary>Show loading overlay on form</summary>
    Public Function ShowLoadingOverlay(parentForm As Form, message As String) As Form
        Try
            ' Create overlay form
            Dim overlay As New Form With {
                .FormBorderStyle = FormBorderStyle.None,
                .StartPosition = FormStartPosition.Manual,
                .ShowInTaskbar = False,
                .BackColor = ColorPalette.Overlay,
                .Opacity = 0.8,
                .Location = parentForm.Location,
                .Size = parentForm.Size,
                .TopMost = True
            }

            ' Create loading panel
            Dim loadingPanel As New Panel With {
                .Size = New Size(300, 150),
                .BackColor = ColorPalette.DarkSurface,
                .Location = New Point(
                    (overlay.Width - 300) \ 2,
                    (overlay.Height - 150) \ 2
                )
            }

            ' Create spinner label (animated dots)
            Dim spinnerLabel As New Label With {
                .Text = "⣾",
                .Font = New Font("Segoe UI", 32, FontStyle.Bold),
                .ForeColor = ColorPalette.Primary,
                .Location = New Point(125, 20),
                .Size = New Size(50, 50),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' Create message label
            Dim messageLabel As New Label With {
                .Text = message,
                .Font = New Font("Segoe UI", 11, FontStyle.Regular),
                .ForeColor = ColorPalette.DarkText,
                .Location = New Point(20, 90),
                .Size = New Size(260, 40),
                .TextAlign = ContentAlignment.TopCenter
            }

            ' Add spinner animation
            Dim spinnerFrames = {"⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"}
            Dim frameIndex = 0
            Dim spinnerTimer As New Timer With {.Interval = 100}
            AddHandler spinnerTimer.Tick, Sub()
                                              frameIndex = (frameIndex + 1) Mod spinnerFrames.Length
                                              spinnerLabel.Text = spinnerFrames(frameIndex)
                                          End Sub
            spinnerTimer.Start()

            ' Add controls
            loadingPanel.Controls.Add(spinnerLabel)
            loadingPanel.Controls.Add(messageLabel)
            overlay.Controls.Add(loadingPanel)

            ' Add rounded corners to panel
            AddRoundedCorners(loadingPanel)

            ' Show overlay
            overlay.Show()

            ' Store timer reference for cleanup
            overlay.Tag = spinnerTimer

            Return overlay
        Catch ex As Exception
            Logger.LogToFile($"Error showing loading overlay: {ex.Message}", LogLevel.Error)
            Return Nothing
        End Try
    End Function

    ''' <summary>Hide loading overlay</summary>
    Public Sub HideLoadingOverlay(overlay As Form)
        Try
            If overlay IsNot Nothing Then
                ' Stop animation timer
                Dim timer = TryCast(overlay.Tag, Timer)
                If timer IsNot Nothing Then
                    timer.Stop()
                    timer.Dispose()
                End If

                overlay.Close()
                overlay.Dispose()
            End If
        Catch ex As Exception
            Logger.LogToFile($"Error hiding loading overlay: {ex.Message}", LogLevel.Error)
        End Try
    End Sub

    ' ===========================
    ' PROGRESS HELPERS
    ' ===========================

    ''' <summary>Update progress bar with smooth animation</summary>
    Public Sub UpdateProgressSmooth(progressBar As ProgressBar, targetValue As Integer)
        Try
            If progressBar.Value = targetValue Then
                Return
            End If

            Dim timer As New Timer With {.Interval = 10}
            AddHandler timer.Tick, Sub()
                                       If progressBar.Value < targetValue Then
                                           progressBar.Value = Math.Min(progressBar.Value + 1, targetValue)
                                       ElseIf progressBar.Value > targetValue Then
                                           progressBar.Value = Math.Max(progressBar.Value - 1, targetValue)
                                       Else
                                           timer.Stop()
                                           timer.Dispose()
                                       End If
                                   End Sub
            timer.Start()
        Catch ex As Exception
            ' Fallback to instant update
            progressBar.Value = targetValue
        End Try
    End Sub

    ' ===========================
    ' VISUAL EFFECTS
    ' ===========================

    ''' <summary>Add rounded corners to control (Windows 11 style)</summary>
    Private Sub AddRoundedCorners(control As Control, Optional radius As Integer = 10)
        Try
            ' Create rounded rectangle region
            Dim path As New System.Drawing.Drawing2D.GraphicsPath()
            path.AddArc(0, 0, radius, radius, 180, 90)
            path.AddArc(control.Width - radius, 0, radius, radius, 270, 90)
            path.AddArc(control.Width - radius, control.Height - radius, radius, radius, 0, 90)
            path.AddArc(0, control.Height - radius, radius, radius, 90, 90)
            path.CloseFigure()

            control.Region = New Region(path)
        Catch
            ' Ignore errors - rounded corners are optional
        End Try
    End Sub

    ''' <summary>Add button hover effect</summary>
    Public Sub AddButtonHoverEffect(button As Button)
        Try
            Dim originalColor = button.BackColor

            AddHandler button.MouseEnter, Sub()
                                              button.BackColor = ControlPaint.Light(originalColor, 0.1F)
                                          End Sub

            AddHandler button.MouseLeave, Sub()
                                              button.BackColor = originalColor
                                          End Sub
        Catch
            ' Ignore errors
        End Try
    End Sub

    ''' <summary>Flash control to draw attention</summary>
    Public Sub FlashControl(control As Control, Optional times As Integer = 3)
        Try
            Dim originalColor = control.BackColor
            Dim flashCount = 0

            Dim flashTimer As New Timer With {.Interval = 200}
            AddHandler flashTimer.Tick, Sub()
                                            flashCount += 1
                                            If flashCount Mod 2 = 0 Then
                                                control.BackColor = originalColor
                                            Else
                                                control.BackColor = ColorPalette.Warning
                                            End If

                                            If flashCount >= times * 2 Then
                                                flashTimer.Stop()
                                                flashTimer.Dispose()
                                                control.BackColor = originalColor
                                            End If
                                        End Sub
            flashTimer.Start()
        Catch
            ' Ignore errors
        End Try
    End Sub

    ' ===========================
    ' TOOLTIPS
    ' ===========================

    ''' <summary>Create styled tooltip</summary>
    Public Function CreateStyledTooltip() As ToolTip
        Return New ToolTip With {
            .AutoPopDelay = 5000,
            .InitialDelay = 500,
            .ReshowDelay = 200,
            .ShowAlways = True,
            .ToolTipIcon = ToolTipIcon.Info,
            .IsBalloon = True
        }
    End Function

    ''' <summary>Add tooltip with icon to control</summary>
    Public Sub AddTooltipWithIcon(control As Control, text As String, icon As ToolTipIcon)
        Try
            Dim tooltip As New ToolTip With {
                .AutoPopDelay = 5000,
                .InitialDelay = 500,
                .ReshowDelay = 200,
                .ShowAlways = True,
                .ToolTipIcon = icon,
                .IsBalloon = True
            }
            tooltip.SetToolTip(control, text)
        Catch
            ' Ignore errors
        End Try
    End Sub

    ' ===========================
    ' STATUS INDICATORS
    ' ===========================

    ''' <summary>Create status indicator label</summary>
    Public Function CreateStatusIndicator(status As String, statusType As ToastIcon) As Label
        Dim indicator As New Label With {
            .Text = $"{GetToastIconText(statusType)} {status}",
            .Font = New Font("Segoe UI", 9, FontStyle.Regular),
            .ForeColor = GetToastBackColor(statusType),
            .AutoSize = True,
            .Padding = New Padding(5, 2, 5, 2)
        }
        Return indicator
    End Function

End Module