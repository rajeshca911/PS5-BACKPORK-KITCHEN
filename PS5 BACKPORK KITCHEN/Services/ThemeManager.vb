Imports System.IO
Imports Newtonsoft.Json

Public Module ThemeManager

    Public Enum AppTheme
        Light
        Dark
        HighContrast
        System
    End Enum

    Public Structure ThemeColors
        Public BackgroundColor As Color
        Public ForegroundColor As Color
        Public AccentColor As Color
        Public ButtonColor As Color
        Public ButtonHoverColor As Color
        Public TextBoxBackColor As Color
        Public TextBoxForeColor As Color
        Public StatusSuccessColor As Color
        Public StatusWarningColor As Color
        Public StatusErrorColor As Color
        Public StatusInfoColor As Color
        Public BorderColor As Color
        Public PanelBackColor As Color
        Public MenuBackColor As Color
        Public MenuForeColor As Color
    End Structure

    Private _currentTheme As AppTheme = AppTheme.Light
    Private _currentColors As ThemeColors
    Private _backgroundImagePath As String = ""
    Private ReadOnly ThemeFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.json")
    Private ReadOnly BackgroundImageFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "background.json")

    ''' <summary>
    ''' Initialize theme manager
    ''' </summary>
    Public Sub Initialize()
        _currentTheme = LoadThemePreference()
        LoadThemeColors()
    End Sub

    ''' <summary>
    ''' Set application theme
    ''' </summary>
    Public Sub SetTheme(theme As AppTheme)
        _currentTheme = theme
        LoadThemeColors()
        SaveThemePreference()
    End Sub

    ''' <summary>
    ''' Get current theme
    ''' </summary>
    Public Function GetCurrentTheme() As AppTheme
        Return _currentTheme
    End Function

    ''' <summary>
    ''' Get current theme colors
    ''' </summary>
    Public Function GetThemeColors() As ThemeColors
        Return _currentColors
    End Function

    ''' <summary>
    ''' Load theme colors based on current theme
    ''' </summary>
    Private Sub LoadThemeColors()
        Select Case _currentTheme
            Case AppTheme.Light
                LoadLightTheme()
            Case AppTheme.Dark
                LoadDarkTheme()
            Case AppTheme.HighContrast
                LoadHighContrastTheme()
            Case AppTheme.System
                ' Detect system theme
                If IsSystemDarkMode() Then
                    LoadDarkTheme()
                Else
                    LoadLightTheme()
                End If
        End Select
    End Sub

    ''' <summary>
    ''' Light theme colors
    ''' </summary>
    Private Sub LoadLightTheme()
        _currentColors = New ThemeColors With {
            .BackgroundColor = Color.FromArgb(240, 240, 240),
            .ForegroundColor = Color.FromArgb(30, 30, 30),
            .AccentColor = Color.FromArgb(0, 120, 215),
            .ButtonColor = Color.FromArgb(225, 225, 225),
            .ButtonHoverColor = Color.FromArgb(200, 200, 200),
            .TextBoxBackColor = Color.White,
            .TextBoxForeColor = Color.Black,
            .StatusSuccessColor = Color.FromArgb(16, 124, 16),
            .StatusWarningColor = Color.FromArgb(255, 140, 0),
            .StatusErrorColor = Color.FromArgb(196, 43, 28),
            .StatusInfoColor = Color.FromArgb(0, 120, 215),
            .BorderColor = Color.FromArgb(200, 200, 200),
            .PanelBackColor = Color.White,
            .MenuBackColor = Color.FromArgb(245, 245, 245),
            .MenuForeColor = Color.FromArgb(30, 30, 30)
        }
    End Sub

    ''' <summary>
    ''' Dark theme colors - improved for better readability
    ''' </summary>
    Private Sub LoadDarkTheme()
        _currentColors = New ThemeColors With {
            .BackgroundColor = Color.FromArgb(45, 45, 48),
            .ForegroundColor = Color.FromArgb(241, 241, 241),
            .AccentColor = Color.FromArgb(0, 122, 204),
            .ButtonColor = Color.FromArgb(62, 62, 66),
            .ButtonHoverColor = Color.FromArgb(82, 82, 86),
            .TextBoxBackColor = Color.FromArgb(51, 51, 55),
            .TextBoxForeColor = Color.FromArgb(241, 241, 241),
            .StatusSuccessColor = Color.FromArgb(106, 153, 85),
            .StatusWarningColor = Color.FromArgb(206, 145, 120),
            .StatusErrorColor = Color.FromArgb(224, 108, 117),
            .StatusInfoColor = Color.FromArgb(86, 156, 214),
            .BorderColor = Color.FromArgb(63, 63, 70),
            .PanelBackColor = Color.FromArgb(37, 37, 38),
            .MenuBackColor = Color.FromArgb(45, 45, 48),
            .MenuForeColor = Color.FromArgb(241, 241, 241)
        }
    End Sub

    ''' <summary>
    ''' High contrast theme colors - improved for accessibility
    ''' </summary>
    Private Sub LoadHighContrastTheme()
        _currentColors = New ThemeColors With {
            .BackgroundColor = Color.FromArgb(0, 0, 0),
            .ForegroundColor = Color.FromArgb(255, 255, 255),
            .AccentColor = Color.FromArgb(255, 255, 0),
            .ButtonColor = Color.FromArgb(0, 0, 0),
            .ButtonHoverColor = Color.FromArgb(128, 128, 0),
            .TextBoxBackColor = Color.FromArgb(0, 0, 0),
            .TextBoxForeColor = Color.FromArgb(255, 255, 255),
            .StatusSuccessColor = Color.FromArgb(0, 255, 0),
            .StatusWarningColor = Color.FromArgb(255, 255, 0),
            .StatusErrorColor = Color.FromArgb(255, 0, 0),
            .StatusInfoColor = Color.FromArgb(0, 255, 255),
            .BorderColor = Color.FromArgb(255, 255, 255),
            .PanelBackColor = Color.FromArgb(0, 0, 0),
            .MenuBackColor = Color.FromArgb(0, 0, 0),
            .MenuForeColor = Color.FromArgb(255, 255, 255)
        }
    End Sub

    ''' <summary>
    ''' Apply theme to form (preserve BackgroundImage if set)
    ''' </summary>
    Public Sub ApplyThemeToForm(form As Form)
        Try
            Dim colors = GetThemeColors()

            ' Only set BackColor if no BackgroundImage is set
            If form.BackgroundImage Is Nothing Then
                form.BackColor = colors.BackgroundColor
            End If

            form.ForeColor = colors.ForegroundColor

            ' Apply to all controls recursively
            ApplyThemeToControls(form.Controls, colors)
        Catch ex As Exception
            ' Silent fail - theming is not critical
        End Try
    End Sub

    ''' <summary>
    ''' Apply theme to controls recursively (with exclusions for custom controls)
    ''' </summary>
    Private Sub ApplyThemeToControls(controls As Control.ControlCollection, colors As ThemeColors)
        For Each ctrl As Control In controls
            Try
                ' Skip RealTaiizor custom controls (they have their own styling)
                Dim typeName = ctrl.GetType().FullName
                If typeName.StartsWith("ReaLTaiizor.") Then
                    ' Skip custom controls but still process their children
                    If ctrl.HasChildren Then
                        ApplyThemeToControls(ctrl.Controls, colors)
                    End If
                    Continue For
                End If

                ' Apply based on control type
                If TypeOf ctrl Is Button Then
                    Dim btn = CType(ctrl, Button)
                    ' Only apply if not one of our feature buttons (they have custom colors)
                    If Not btn.Text.Contains("📊") AndAlso Not btn.Text.Contains("🔍") AndAlso Not btn.Text.Contains("📦") Then
                        btn.BackColor = colors.ButtonColor
                        btn.ForeColor = colors.ForegroundColor
                        btn.FlatStyle = FlatStyle.Flat
                        btn.FlatAppearance.BorderColor = colors.BorderColor
                    End If

                ElseIf TypeOf ctrl Is TextBox Then
                    Dim txt = CType(ctrl, TextBox)
                    txt.BackColor = colors.TextBoxBackColor
                    txt.ForeColor = colors.TextBoxForeColor
                    txt.BorderStyle = BorderStyle.FixedSingle

                ElseIf TypeOf ctrl Is RichTextBox Then
                    Dim rtb = CType(ctrl, RichTextBox)
                    rtb.BackColor = colors.TextBoxBackColor
                    rtb.ForeColor = colors.TextBoxForeColor

                ElseIf TypeOf ctrl Is LinkLabel Then
                    Dim lnk = CType(ctrl, LinkLabel)
                    ' Ensure link labels are visible
                    lnk.LinkColor = colors.AccentColor
                    lnk.VisitedLinkColor = colors.AccentColor
                    lnk.ActiveLinkColor = colors.StatusWarningColor
                    lnk.BackColor = Color.Transparent

                ElseIf TypeOf ctrl Is Label Then
                    Dim lbl = CType(ctrl, Label)
                    ' Force readable colors for all labels
                    lbl.ForeColor = colors.ForegroundColor
                    ' Make background transparent for better look
                    If lbl.BackColor <> Color.Transparent AndAlso Not lbl.BackColor.Name.StartsWith("Misty") Then
                        lbl.BackColor = Color.Transparent
                    End If

                ElseIf TypeOf ctrl Is Panel Then
                    Dim pnl = CType(ctrl, Panel)
                    pnl.BackColor = colors.PanelBackColor
                    pnl.ForeColor = colors.ForegroundColor

                ElseIf TypeOf ctrl Is GroupBox Then
                    Dim grp = CType(ctrl, GroupBox)
                    grp.ForeColor = colors.ForegroundColor

                ElseIf TypeOf ctrl Is CheckBox Then
                    Dim chk = CType(ctrl, CheckBox)
                    chk.ForeColor = colors.ForegroundColor

                ElseIf TypeOf ctrl Is RadioButton Then
                    Dim rad = CType(ctrl, RadioButton)
                    rad.ForeColor = colors.ForegroundColor

                ElseIf TypeOf ctrl Is ComboBox Then
                    Dim cmb = CType(ctrl, ComboBox)
                    cmb.BackColor = colors.TextBoxBackColor
                    cmb.ForeColor = colors.TextBoxForeColor

                ElseIf TypeOf ctrl Is ListBox Then
                    Dim lst = CType(ctrl, ListBox)
                    lst.BackColor = colors.TextBoxBackColor
                    lst.ForeColor = colors.TextBoxForeColor

                ElseIf TypeOf ctrl Is MenuStrip Then
                    Dim menu = CType(ctrl, MenuStrip)
                    menu.BackColor = colors.MenuBackColor
                    menu.ForeColor = colors.MenuForeColor

                End If

                ' Recursively apply to child controls
                If ctrl.HasChildren Then
                    ApplyThemeToControls(ctrl.Controls, colors)
                End If
            Catch ex As Exception
                ' Continue with next control
            End Try
        Next
    End Sub

    ''' <summary>
    ''' Check if system is in dark mode
    ''' </summary>
    Public Function IsSystemDarkMode() As Boolean
        Try
            ' Try to read Windows 10/11 dark mode setting
            Using key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
                If key IsNot Nothing Then
                    Dim value = key.GetValue("AppsUseLightTheme")
                    If value IsNot Nothing Then
                        Return CInt(value) = 0 ' 0 = Dark mode, 1 = Light mode
                    End If
                End If
            End Using
        Catch ex As Exception
            ' Return light mode as default
        End Try

        Return False
    End Function

    ''' <summary>
    ''' Save theme preference
    ''' </summary>
    Private Sub SaveThemePreference()
        Try
            Dim pref As New Dictionary(Of String, String) From {
                {"theme", _currentTheme.ToString()}
            }
            Dim json = JsonConvert.SerializeObject(pref, Formatting.Indented)
            File.WriteAllText(ThemeFilePath, json)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Load theme preference
    ''' </summary>
    Private Function LoadThemePreference() As AppTheme
        Try
            If File.Exists(ThemeFilePath) Then
                Dim json = File.ReadAllText(ThemeFilePath)
                Dim pref = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(json)
                If pref.ContainsKey("theme") Then
                    Return [Enum].Parse(GetType(AppTheme), pref("theme"))
                End If
            End If
        Catch ex As Exception
            ' Return default
        End Try

        Return AppTheme.Light
    End Function

    ''' <summary>
    ''' Get available themes
    ''' </summary>
    Public Function GetAvailableThemes() As List(Of String)
        Return [Enum].GetNames(GetType(AppTheme)).ToList()
    End Function

    ''' <summary>
    ''' Set custom background image
    ''' </summary>
    Public Sub SetBackgroundImage(imagePath As String)
        Try
            If Not String.IsNullOrEmpty(imagePath) AndAlso File.Exists(imagePath) Then
                _backgroundImagePath = imagePath
                SaveBackgroundImagePreference()
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Get current background image path
    ''' </summary>
    Public Function GetBackgroundImagePath() As String
        Return _backgroundImagePath
    End Function

    ''' <summary>
    ''' Clear background image
    ''' </summary>
    Public Sub ClearBackgroundImage()
        _backgroundImagePath = ""
        SaveBackgroundImagePreference()
    End Sub

    ''' <summary>
    ''' Save background image preference
    ''' </summary>
    Private Sub SaveBackgroundImagePreference()
        Try
            Dim pref As New Dictionary(Of String, String) From {
                {"backgroundImage", _backgroundImagePath}
            }
            Dim json = JsonConvert.SerializeObject(pref, Formatting.Indented)
            File.WriteAllText(BackgroundImageFilePath, json)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Load background image preference
    ''' </summary>
    Public Sub LoadBackgroundImagePreference()
        Try
            If File.Exists(BackgroundImageFilePath) Then
                Dim json = File.ReadAllText(BackgroundImageFilePath)
                Dim pref = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(json)
                If pref.ContainsKey("backgroundImage") Then
                    _backgroundImagePath = pref("backgroundImage")
                End If
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Export current theme colors to JSON
    ''' </summary>
    Public Function ExportThemeColors(filePath As String) As Boolean
        Try
            Dim colors = GetThemeColors()
            Dim colorDict As New Dictionary(Of String, String) From {
                {"BackgroundColor", ColorToHex(colors.BackgroundColor)},
                {"ForegroundColor", ColorToHex(colors.ForegroundColor)},
                {"AccentColor", ColorToHex(colors.AccentColor)},
                {"ButtonColor", ColorToHex(colors.ButtonColor)},
                {"StatusSuccessColor", ColorToHex(colors.StatusSuccessColor)},
                {"StatusWarningColor", ColorToHex(colors.StatusWarningColor)},
                {"StatusErrorColor", ColorToHex(colors.StatusErrorColor)},
                {"StatusInfoColor", ColorToHex(colors.StatusInfoColor)}
            }

            Dim json = JsonConvert.SerializeObject(colorDict, Formatting.Indented)
            File.WriteAllText(filePath, json)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Convert color to hex string
    ''' </summary>
    Private Function ColorToHex(color As Color) As String
        Return $"#{color.R:X2}{color.G:X2}{color.B:X2}"
    End Function

End Module