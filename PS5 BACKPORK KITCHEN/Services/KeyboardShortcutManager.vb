Imports System.Windows.Forms

''' <summary>
''' Centralized keyboard shortcut management for PS5 BACKPORK KITCHEN.
''' Provides consistent, discoverable keyboard shortcuts across the application.
''' </summary>
''' <remarks>
''' Created: 2026-01-27
''' Author: DroneTechTI
''' Feature: Keyboard Shortcuts System v1.0
'''
''' SUPPORTED SHORTCUTS:
''' - Ctrl+O: Browse for game folder
''' - Ctrl+S: Start backporting process
''' - Ctrl+R: Show recent folders
''' - Ctrl+L: Open game library
''' - Ctrl+F: Open FTP connection
''' - Ctrl+T: Toggle theme menu
''' - Ctrl+E: ELF Inspector
''' - Ctrl+B: Batch processing
''' - Ctrl+I: Statistics
''' - F1: Show help
''' - F5: Refresh current folder
''' - Escape: Cancel/Close operations
''' </remarks>
Public Class KeyboardShortcutManager
    Implements IDisposable

    Private ReadOnly _form As Form
    Private _disposed As Boolean = False
    Private _shortcuts As Dictionary(Of Keys, ShortcutAction)

    ''' <summary>
    ''' Represents a keyboard shortcut action.
    ''' </summary>
    Public Class ShortcutAction
        Public Property Keys As Keys
        Public Property Description As String
        Public Property Action As Action
        Public Property Enabled As Boolean = True
        Public Property Category As String
    End Class

    ''' <summary>
    ''' Shortcut categories for organization.
    ''' </summary>
    Public Enum ShortcutCategory
        MainActions
        Navigation
        Tools
        View
        Help
    End Enum

    ''' <summary>
    ''' Initializes a new instance of the KeyboardShortcutManager.
    ''' </summary>
    ''' <param name="form">The form to attach shortcuts to</param>
    Public Sub New(form As Form)
        If form Is Nothing Then Throw New ArgumentNullException(NameOf(form))
        _form = form
        _shortcuts = New Dictionary(Of Keys, ShortcutAction)()

        ' Enable key preview to capture all keyboard events
        _form.KeyPreview = True
        AddHandler _form.KeyDown, AddressOf Form_KeyDown
    End Sub

    ''' <summary>
    ''' Registers a keyboard shortcut.
    ''' </summary>
    ''' <param name="keys">The key combination</param>
    ''' <param name="description">Description of the action</param>
    ''' <param name="action">The action to execute</param>
    ''' <param name="category">Category for organization</param>
    Public Sub RegisterShortcut(keys As Keys, description As String, action As Action, Optional category As String = "General")
        If action Is Nothing Then Throw New ArgumentNullException(NameOf(action))

        Dim shortcut As New ShortcutAction With {
            .Keys = keys,
            .Description = description,
            .Action = action,
            .Category = category,
            .Enabled = True
        }

        If _shortcuts.ContainsKey(keys) Then
            _shortcuts(keys) = shortcut
        Else
            _shortcuts.Add(keys, shortcut)
        End If
    End Sub

    ''' <summary>
    ''' Configures default shortcuts for Form1.
    ''' </summary>
    ''' <param name="form">The Form1 instance</param>
    Public Sub ConfigureForm1Shortcuts(form As Form1)
        If form Is Nothing Then Throw New ArgumentNullException(NameOf(form))

        Try
            ' === MAIN ACTIONS ===
            RegisterShortcut(
                Keys.Control Or Keys.O,
                "Browse for game folder",
                Sub()
                    Try
                        ' Trigger Browse button click event
                        Dim method = form.BtnBrowse.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                        If method IsNot Nothing Then
                            method.Invoke(form.BtnBrowse, {EventArgs.Empty})
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+O): {ex.Message}")
                    End Try
                End Sub,
                "Main Actions"
            )

            RegisterShortcut(
                Keys.Control Or Keys.S,
                "Start backporting process",
                Sub()
                    Try
                        If form.BtnStart.Enabled Then
                            ' Trigger Start button click event
                            Dim method = form.BtnStart.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(form.BtnStart, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+S): {ex.Message}")
                    End Try
                End Sub,
                "Main Actions"
            )

            ' === NAVIGATION ===
            RegisterShortcut(
                Keys.Control Or Keys.R,
                "Show recent folders",
                Sub()
                    Try
                        Dim btnRecent = FindControl(Of Button)(form, "btnRecentFolders")
                        If btnRecent IsNot Nothing AndAlso btnRecent.Visible Then
                            Dim method = btnRecent.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(btnRecent, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+R): {ex.Message}")
                    End Try
                End Sub,
                "Navigation"
            )

            RegisterShortcut(
                Keys.Control Or Keys.L,
                "Open Game Library",
                Sub()
                    Try
                        Dim method = form.MoonButton1.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                        If method IsNot Nothing Then
                            method.Invoke(form.MoonButton1, {EventArgs.Empty})
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+L): {ex.Message}")
                    End Try
                End Sub,
                "Navigation"
            )

            RegisterShortcut(
                Keys.Control Or Keys.F,
                "Open FTP Connection",
                Sub()
                    Try
                        'Dim method = form.Button1.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                        'If method IsNot Nothing Then
                        '    method.Invoke(form.Button1, {EventArgs.Empty})
                        'End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+F): {ex.Message}")
                    End Try
                End Sub,
                "Navigation"
            )

            ' === TOOLS ===
            RegisterShortcut(
                Keys.Control Or Keys.T,
                "Toggle theme menu",
                Sub()
                    Try
                        Dim btnTheme = FindControl(Of Button)(form, "btnTheme")
                        If btnTheme IsNot Nothing AndAlso btnTheme.Visible Then
                            Dim method = btnTheme.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(btnTheme, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+T): {ex.Message}")
                    End Try
                End Sub,
                "Tools"
            )

            RegisterShortcut(
                Keys.Control Or Keys.E,
                "ELF Inspector",
                Sub()
                    Try
                        Dim btnElf = FindControl(Of Button)(form, "btnElfInspector")
                        If btnElf IsNot Nothing AndAlso btnElf.Visible Then
                            Dim method = btnElf.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(btnElf, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+E): {ex.Message}")
                    End Try
                End Sub,
                "Tools"
            )

            RegisterShortcut(
                Keys.Control Or Keys.B,
                "Batch Processing",
                Sub()
                    Try
                        Dim btnBatch = FindControl(Of Button)(form, "btnBatchProcess")
                        If btnBatch IsNot Nothing AndAlso btnBatch.Visible Then
                            Dim method = btnBatch.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(btnBatch, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+B): {ex.Message}")
                    End Try
                End Sub,
                "Tools"
            )

            RegisterShortcut(
                Keys.Control Or Keys.I,
                "Show Statistics",
                Sub()
                    Try
                        Dim btnStats = FindControl(Of Button)(form, "btnShowStatistics")
                        If btnStats IsNot Nothing AndAlso btnStats.Visible Then
                            Dim method = btnStats.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(btnStats, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (Ctrl+I): {ex.Message}")
                    End Try
                End Sub,
                "Tools"
            )

            ' === VIEW ===
            RegisterShortcut(
                Keys.F5,
                "Refresh current folder",
                Sub()
                    Try
                        Dim txtPath = FindControl(Of TextBox)(form, "Txtpath")
                        If txtPath IsNot Nothing AndAlso Not String.IsNullOrEmpty(txtPath.Text) Then
                            ' Trigger re-load of folder info by invoking Browse click
                            Dim method = form.BtnBrowse.GetType().GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If method IsNot Nothing Then
                                method.Invoke(form.BtnBrowse, {EventArgs.Empty})
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (F5): {ex.Message}")
                    End Try
                End Sub,
                "View"
            )

            ' === HELP ===
            RegisterShortcut(
                Keys.F1,
                "Show keyboard shortcuts help",
                Sub()
                    Try
                        ShowShortcutsHelp()
                    Catch ex As Exception
                        Debug.WriteLine($"Shortcut error (F1): {ex.Message}")
                    End Try
                End Sub,
                "Help"
            )
        Catch ex As Exception
            Debug.WriteLine($"KeyboardShortcutManager: Error configuring shortcuts - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Handles keyboard events and executes shortcuts.
    ''' </summary>
    Private Sub Form_KeyDown(sender As Object, e As KeyEventArgs)
        Try
            ' Check if this key combination is registered
            If _shortcuts.ContainsKey(e.KeyData) Then
                Dim shortcut = _shortcuts(e.KeyData)

                ' Execute if enabled
                If shortcut.Enabled Then
                    shortcut.Action?.Invoke()
                    e.Handled = True
                    e.SuppressKeyPress = True
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine($"KeyboardShortcutManager: Error processing key - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Finds a control by name recursively.
    ''' </summary>
    Private Function FindControl(Of T As Control)(parent As Control, controlName As String) As T
        If parent Is Nothing Then Return Nothing
        If TypeOf parent Is T AndAlso parent.Name = controlName Then Return DirectCast(parent, T)

        For Each child As Control In parent.Controls
            Dim found = FindControl(Of T)(child, controlName)
            If found IsNot Nothing Then Return found
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' Shows a help dialog with all available shortcuts.
    ''' </summary>
    Public Sub ShowShortcutsHelp()
        Dim helpText As New System.Text.StringBuilder()
        helpText.AppendLine("=== KEYBOARD SHORTCUTS ===")
        helpText.AppendLine()

        ' Group by category
        Dim categories = _shortcuts.Values.GroupBy(Function(s) s.Category).OrderBy(Function(g) g.Key)

        For Each category In categories
            helpText.AppendLine($"【{category.Key}】")
            For Each shortcut In category.OrderBy(Function(s) s.Keys.ToString())
                Dim keyString = GetKeyDisplayString(shortcut.Keys)
                helpText.AppendLine($"  {keyString,-20} - {shortcut.Description}")
            Next
            helpText.AppendLine()
        Next

        MessageBox.Show(helpText.ToString(), "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ''' <summary>
    ''' Converts Keys enum to readable string.
    ''' </summary>
    Private Function GetKeyDisplayString(keys As Keys) As String
        Dim parts As New List(Of String)()

        ' Check modifiers
        If (keys And Keys.Control) = Keys.Control Then parts.Add("Ctrl")
        If (keys And Keys.Shift) = Keys.Shift Then parts.Add("Shift")
        If (keys And Keys.Alt) = Keys.Alt Then parts.Add("Alt")

        ' Get main key
        Dim mainKey = keys And Not (Keys.Control Or Keys.Shift Or Keys.Alt)
        parts.Add(mainKey.ToString())

        Return String.Join("+", parts)
    End Function

    ''' <summary>
    ''' Enables or disables a specific shortcut.
    ''' </summary>
    Public Sub SetShortcutEnabled(keys As Keys, enabled As Boolean)
        If _shortcuts.ContainsKey(keys) Then
            _shortcuts(keys).Enabled = enabled
        End If
    End Sub

    ''' <summary>
    ''' Gets all registered shortcuts.
    ''' </summary>
    Public Function GetAllShortcuts() As IEnumerable(Of ShortcutAction)
        Return _shortcuts.Values.ToList()
    End Function

    ''' <summary>
    ''' Disposes the keyboard shortcut manager.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' Protected dispose implementation.
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                RemoveHandler _form.KeyDown, AddressOf Form_KeyDown
                _shortcuts?.Clear()
            End If
            _disposed = True
        End If
    End Sub

End Class