Imports System.Windows.Forms

''' <summary>
''' Centralized tooltip management service for PS5 BACKPORK KITCHEN.
''' Provides consistent, localized tooltips across the application.
''' </summary>
''' <remarks>
''' Created: 2026-01-27
''' Author: DroneTechTI
''' Feature: Tooltip System v1.0
''' </remarks>
Public Class TooltipService
    Implements IDisposable

    Private ReadOnly _tooltip As ToolTip
    Private _disposed As Boolean = False

    ''' <summary>
    ''' Initializes a new instance of the TooltipService.
    ''' </summary>
    Public Sub New()
        _tooltip = New ToolTip() With {
            .AutoPopDelay = 5000,
            .InitialDelay = 500,
            .ReshowDelay = 200,
            .ShowAlways = True,
            .IsBalloon = False,
            .ToolTipIcon = ToolTipIcon.Info,
            .UseAnimation = True,
            .UseFading = True
        }
    End Sub

    ''' <summary>
    ''' Gets or sets the maximum width for tooltip text wrapping.
    ''' </summary>
    Public Property MaxWidth As Integer
        Get
            Return _tooltip.AutomaticDelay
        End Get
        Set(value As Integer)
            _tooltip.AutomaticDelay = value
        End Set
    End Property

    ''' <summary>
    ''' Configures tooltips for main form controls.
    ''' </summary>
    ''' <param name="form">The Form1 instance to configure</param>
    Public Sub ConfigureMainFormTooltips(form As Form1)
        If form Is Nothing Then Throw New ArgumentNullException(NameOf(form))

        Try
            ' === MAIN ACTION BUTTONS ===
            SetTooltip(form, "BtnBrowse",
                "Select PS5 game folder (PPSA* format)" & vbCrLf &
                "Click to browse and select a PS5 homebrew/game directory")

            SetTooltip(form, "BtnStart",
                "Start backporting process" & vbCrLf &
                "Patches selected game to lower firmware version" & vbCrLf &
                "Automatic backup will be created if enabled")

            SetTooltip(form, "Button1",
                "FTP Connection Manager" & vbCrLf &
                "Connect to PS5 via FTP for direct file transfer" & vbCrLf &
                "Pull/push libraries without manual USB copying")

            SetTooltip(form, "MoonButton1",
                "Game Library Manager" & vbCrLf &
                "View and manage your processed games" & vbCrLf &
                "Track backporting history and metadata")

            ' === PATH AND SELECTION ===
            SetTooltip(form, "Txtpath",
                "Game folder path" & vbCrLf &
                "Shows selected game directory" & vbCrLf &
                "You can also drag & drop folders here")

            SetTooltip(form, "cmbPs5Sdk",
                "Target PS5 SDK/Firmware version" & vbCrLf &
                "Select the firmware version to backport to" & vbCrLf &
                "Lower version = more compatible with older firmwares")

            ' === OPTIONS ===
            SetTooltip(form, "chkBackup",
                "Create automatic backup" & vbCrLf &
                "Recommended: Creates backup of original files" & vbCrLf &
                "Backup stored in game folder before patching")

            SetTooltip(form, "chklibcpatch",
                "Apply libc patch (experimental)" & vbCrLf &
                "Advanced option for specific compatibility fixes" & vbCrLf &
                "Enable only if you know what you're doing")

            ' === TOOLBAR FEATURES ===
            SetTooltip(form, "btnRecentFolders",
                "Recent Folders" & vbCrLf &
                "Quick access to previously processed game folders" & vbCrLf &
                "Click to see history")

            SetTooltip(form, "btnTheme",
                "Theme & Settings Menu" & vbCrLf &
                "Customize appearance, language, and access tools" & vbCrLf &
                "Includes Game Library and FTP settings")

            SetTooltip(form, "cmbPresets",
                "Quick Configuration Presets" & vbCrLf &
                "Load predefined settings for common scenarios" & vbCrLf &
                "Balanced, Aggressive, Safe modes available")

            SetTooltip(form, "cmbLanguage",
                "Application Language" & vbCrLf &
                "Change interface language" & vbCrLf &
                "Restart may be required for full effect")

            ' === ADVANCED FEATURES (if controls exist) ===
            SetTooltip(form, "btnShowStatistics",
                "Operation Statistics" & vbCrLf &
                "View detailed statistics of backporting operations" & vbCrLf &
                "Success rate, processing time, and more")

            SetTooltip(form, "btnElfInspector",
                "ELF File Inspector" & vbCrLf &
                "Analyze PS5 ELF binaries in detail" & vbCrLf &
                "View SDK version, libraries, and metadata")

            SetTooltip(form, "btnBatchProcess",
                "Batch Processing Mode" & vbCrLf &
                "Process multiple game folders at once" & vbCrLf &
                "Select multiple directories for sequential processing")
        Catch ex As Exception
            ' Log error but don't crash the application
            Debug.WriteLine($"TooltipService: Error configuring tooltips - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Sets a tooltip for a control by name.
    ''' </summary>
    ''' <param name="form">Parent form containing the control</param>
    ''' <param name="controlName">Name of the control</param>
    ''' <param name="tooltipText">Tooltip text to display</param>
    Private Sub SetTooltip(form As Form, controlName As String, tooltipText As String)
        Try
            ' Find control by name recursively
            Dim control = FindControlRecursive(form, controlName)
            If control IsNot Nothing Then
                _tooltip.SetToolTip(control, tooltipText)
            End If
        Catch ex As Exception
            ' Silently ignore missing controls (they might not exist in all versions)
            Debug.WriteLine($"TooltipService: Control '{controlName}' not found - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Recursively searches for a control by name in a control hierarchy.
    ''' </summary>
    ''' <param name="parent">Parent control to search in</param>
    ''' <param name="controlName">Name of the control to find</param>
    ''' <returns>The found control or Nothing</returns>
    Private Function FindControlRecursive(parent As Control, controlName As String) As Control
        If parent Is Nothing Then Return Nothing
        If parent.Name = controlName Then Return parent

        For Each child As Control In parent.Controls
            Dim found = FindControlRecursive(child, controlName)
            If found IsNot Nothing Then Return found
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' Sets a tooltip for a specific control instance.
    ''' </summary>
    ''' <param name="control">Control to attach tooltip to</param>
    ''' <param name="text">Tooltip text</param>
    Public Sub SetControlTooltip(control As Control, text As String)
        If control IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(text) Then
            _tooltip.SetToolTip(control, text)
        End If
    End Sub

    ''' <summary>
    ''' Removes tooltip from a control.
    ''' </summary>
    ''' <param name="control">Control to remove tooltip from</param>
    Public Sub RemoveTooltip(control As Control)
        If control IsNot Nothing Then
            _tooltip.SetToolTip(control, Nothing)
        End If
    End Sub

    ''' <summary>
    ''' Shows a tooltip at a specific location.
    ''' </summary>
    ''' <param name="text">Tooltip text</param>
    ''' <param name="control">Associated control</param>
    ''' <param name="duration">Display duration in milliseconds</param>
    Public Sub ShowTooltip(text As String, control As Control, Optional duration As Integer = 3000)
        If control IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(text) Then
            _tooltip.Show(text, control, duration)
        End If
    End Sub

    ''' <summary>
    ''' Hides any currently displayed tooltip.
    ''' </summary>
    ''' <param name="control">Control to hide tooltip for</param>
    Public Sub HideTooltip(control As Control)
        If control IsNot Nothing Then
            _tooltip.Hide(control)
        End If
    End Sub

    ''' <summary>
    ''' Disposes the tooltip service and releases resources.
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
                _tooltip?.Dispose()
            End If
            _disposed = True
        End If
    End Sub

End Class