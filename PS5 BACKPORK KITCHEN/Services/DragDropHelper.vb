Imports System.IO

Public Module DragDropHelper

    ''' <summary>
    ''' Enable drag and drop for a control
    ''' </summary>
    Public Sub EnableDragDrop(control As Control, onFolderDropped As Action(Of String))
        Try
            control.AllowDrop = True

            ' Add drag enter handler
            AddHandler control.DragEnter, Sub(sender, e)
                                              HandleDragEnter(e)
                                          End Sub

            ' Add drag drop handler
            AddHandler control.DragDrop, Sub(sender, e)
                                             HandleDragDrop(e, onFolderDropped)
                                         End Sub
        Catch ex As Exception
            ' Silent fail - drag drop is not critical
        End Try
    End Sub

    ''' <summary>
    ''' Handle drag enter event
    ''' </summary>
    Private Sub HandleDragEnter(e As DragEventArgs)
        Try
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim files = CType(e.Data.GetData(DataFormats.FileDrop), String())

                If files IsNot Nothing AndAlso files.Length > 0 Then
                    ' Check if it's a folder
                    If Directory.Exists(files(0)) Then
                        e.Effect = DragDropEffects.Copy
                    Else
                        e.Effect = DragDropEffects.None
                    End If
                Else
                    e.Effect = DragDropEffects.None
                End If
            Else
                e.Effect = DragDropEffects.None
            End If
        Catch ex As Exception
            e.Effect = DragDropEffects.None
        End Try
    End Sub

    ''' <summary>
    ''' Handle drag drop event
    ''' </summary>
    Private Sub HandleDragDrop(e As DragEventArgs, onFolderDropped As Action(Of String))
        Try
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim files = CType(e.Data.GetData(DataFormats.FileDrop), String())

                If files IsNot Nothing AndAlso files.Length > 0 Then
                    Dim folderPath = files(0)

                    ' Validate it's a directory
                    If Directory.Exists(folderPath) Then
                        ' Trigger callback
                        onFolderDropped?.Invoke(folderPath)
                    End If
                End If
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Enable drag and drop for multiple folders
    ''' </summary>
    Public Sub EnableMultiFolderDragDrop(control As Control, onFoldersDropped As Action(Of List(Of String)))
        Try
            control.AllowDrop = True

            ' Add drag enter handler
            AddHandler control.DragEnter, Sub(sender, e)
                                              HandleMultiDragEnter(e)
                                          End Sub

            ' Add drag drop handler
            AddHandler control.DragDrop, Sub(sender, e)
                                             HandleMultiDragDrop(e, onFoldersDropped)
                                         End Sub
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Handle drag enter for multiple folders
    ''' </summary>
    Private Sub HandleMultiDragEnter(e As DragEventArgs)
        Try
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim files = CType(e.Data.GetData(DataFormats.FileDrop), String())

                If files IsNot Nothing AndAlso files.Length > 0 Then
                    ' Check if all are folders
                    Dim allFolders = files.All(Function(f) Directory.Exists(f))

                    If allFolders Then
                        e.Effect = DragDropEffects.Copy
                    Else
                        e.Effect = DragDropEffects.None
                    End If
                Else
                    e.Effect = DragDropEffects.None
                End If
            Else
                e.Effect = DragDropEffects.None
            End If
        Catch ex As Exception
            e.Effect = DragDropEffects.None
        End Try
    End Sub

    ''' <summary>
    ''' Handle drag drop for multiple folders
    ''' </summary>
    Private Sub HandleMultiDragDrop(e As DragEventArgs, onFoldersDropped As Action(Of List(Of String)))
        Try
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim files = CType(e.Data.GetData(DataFormats.FileDrop), String())

                If files IsNot Nothing AndAlso files.Length > 0 Then
                    Dim folders = files.Where(Function(f) Directory.Exists(f)).ToList()

                    If folders.Count > 0 Then
                        ' Trigger callback
                        onFoldersDropped?.Invoke(folders)
                    End If
                End If
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Disable drag and drop for a control
    ''' </summary>
    Public Sub DisableDragDrop(control As Control)
        Try
            control.AllowDrop = False
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Validate dropped folder is a valid game folder
    ''' </summary>
    Public Function ValidateDroppedGameFolder(folderPath As String) As Boolean
        Try
            ' Quick validation - check for common game folder files
            Dim hasEboot = File.Exists(Path.Combine(folderPath, EBOOT_FILENAME))
            Dim hasSceSys = Directory.Exists(Path.Combine(folderPath, "sce_sys"))

            Return hasEboot OrElse hasSceSys
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Show visual feedback on drag over
    ''' </summary>
    Public Sub SetupDragVisualFeedback(control As Control, highlightColor As Color)
        Try
            Dim originalColor = control.BackColor

            AddHandler control.DragEnter, Sub(sender, e)
                                              If e.Effect = DragDropEffects.Copy Then
                                                  control.BackColor = highlightColor
                                              End If
                                          End Sub

            AddHandler control.DragLeave, Sub(sender, e)
                                              control.BackColor = originalColor
                                          End Sub

            AddHandler control.DragDrop, Sub(sender, e)
                                             control.BackColor = originalColor
                                         End Sub
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

End Module