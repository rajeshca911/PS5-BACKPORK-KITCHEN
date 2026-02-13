Imports System.IO
Imports Newtonsoft.Json

Public Module RecentFoldersManager

    Public Structure RecentFolderEntry
        Public FolderPath As String
        Public GameName As String
        Public LastUsed As DateTime
        Public LastSdkVersion As String
        Public SuccessfulPatches As Integer
    End Structure

    Private Const MAX_RECENT_FOLDERS As Integer = 10
    Private ReadOnly ConfigPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_folders.json")

    ''' <summary>
    ''' Add folder to recent list
    ''' </summary>
    Public Sub AddRecentFolder(folderPath As String, Optional sdkVersion As String = "", Optional successfulPatches As Integer = 0)
        Try
            Dim recentList = LoadRecentFolders()

            ' Check if folder already exists
            Dim index = recentList.FindIndex(
    Function(f) String.Equals(f.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase))

            Dim existingEntry As RecentFolderEntry = Nothing
            Dim hasExisting As Boolean = (index >= 0)

            If hasExisting Then
                existingEntry = recentList(index)
                recentList.RemoveAt(index)
            End If

            ' Get game name from param.json if exists
            Dim gameName = GetGameNameFromFolder(folderPath)

            ' Add new entry at the beginning
            Dim newEntry As New RecentFolderEntry With {
                .FolderPath = folderPath,
                .GameName = gameName,
                .LastUsed = DateTime.Now,
                .LastSdkVersion = sdkVersion,
                .SuccessfulPatches = If(existingEntry.FolderPath IsNot Nothing, existingEntry.SuccessfulPatches + successfulPatches, successfulPatches)
            }

            recentList.Insert(0, newEntry)

            ' Keep only MAX_RECENT_FOLDERS
            If recentList.Count > MAX_RECENT_FOLDERS Then
                recentList.RemoveRange(MAX_RECENT_FOLDERS, recentList.Count - MAX_RECENT_FOLDERS)
            End If

            SaveRecentFolders(recentList)
        Catch ex As Exception
            ' Silent fail - not critical
        End Try
    End Sub

    ''' <summary>
    ''' Load recent folders list
    ''' </summary>
    Public Function LoadRecentFolders() As List(Of RecentFolderEntry)
        Try
            If File.Exists(ConfigPath) Then
                Dim json = File.ReadAllText(ConfigPath)
                Dim entries = JsonConvert.DeserializeObject(Of List(Of RecentFolderEntry))(json)

                ' Filter out folders that no longer exist
                Return entries.Where(Function(e) Directory.Exists(e.FolderPath)).ToList()
            End If
        Catch ex As Exception
            ' Return empty list on error

            MessageBox.Show(ex.Message, "RecentFolders LOAD ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error)

        End Try

        Return New List(Of RecentFolderEntry)
    End Function

    ''' <summary>
    ''' Save recent folders list
    ''' </summary>
    Private Sub SaveRecentFolders(folders As List(Of RecentFolderEntry))
        Try
            Dim dir = Path.GetDirectoryName(ConfigPath)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim json = JsonConvert.SerializeObject(folders, Formatting.Indented)
            File.WriteAllText(ConfigPath, json)
        Catch ex As Exception
            ' Silent fail
#If DEBUG Then
            MessageBox.Show(
        "FAILED TO SAVE recent folders!" & vbCrLf &
        ex.Message & vbCrLf &
        "Path:" & vbCrLf & ConfigPath,
        "RecentFolders ERROR",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error
    )
#End If
        End Try
    End Sub

    ''' <summary>
    ''' Remove folder from recent list
    ''' </summary>
    Public Sub RemoveRecentFolder(folderPath As String)
        Try
            Dim recentList = LoadRecentFolders()
            recentList.RemoveAll(Function(f) f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
            SaveRecentFolders(recentList)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Clear all recent folders
    ''' </summary>
    Public Sub ClearRecentFolders()
        Try
            If File.Exists(ConfigPath) Then
                File.Delete(ConfigPath)
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Get game name from folder (read param.json)
    ''' </summary>
    Private Function GetGameNameFromFolder(folderPath As String) As String
        Try
            Dim paramPath = Path.Combine(folderPath, "sce_sys", "param.json")
            If File.Exists(paramPath) Then
                Dim json = File.ReadAllText(paramPath)
                Dim paramData = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(json)

                If paramData.ContainsKey("localizedParameters") Then
                    Dim locParams = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(paramData("localizedParameters").ToString())
                    If locParams.ContainsKey("en-US") Then
                        Dim enUS = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(locParams("en-US").ToString())
                        If enUS.ContainsKey("titleName") Then
                            Return enUS("titleName")
                        End If
                    End If
                End If

                If paramData.ContainsKey("contentId") Then
                    Return paramData("contentId").ToString()
                End If
            End If
        Catch ex As Exception
            ' Fallback to folder name
        End Try

        Return Path.GetFileName(folderPath)
    End Function

    ''' <summary>
    ''' Get formatted recent folders for display
    ''' </summary>
    Public Function GetRecentFoldersForDisplay() As List(Of String)
        Dim recentList = LoadRecentFolders()
        Dim displayList As New List(Of String)

        For Each entry In recentList
            Dim displayText = $"{entry.GameName} - {entry.FolderPath}"
            If Not String.IsNullOrEmpty(entry.LastSdkVersion) Then
                displayText &= $" (SDK: {entry.LastSdkVersion})"
            End If
            displayList.Add(displayText)
        Next

        Return displayList
    End Function

    ''' <summary>
    ''' Get folder path from display string
    ''' </summary>
    Public Function GetFolderPathFromDisplay(displayText As String) As String
        Try
            ' Extract path between " - " and optional " (SDK:"
            Dim startIndex = displayText.IndexOf(" - ") + 3
            Dim endIndex = displayText.IndexOf(" (SDK:")

            If endIndex = -1 Then
                Return displayText.Substring(startIndex).Trim()
            Else
                Return displayText.Substring(startIndex, endIndex - startIndex).Trim()
            End If
        Catch ex As Exception
            Return ""
        End Try
    End Function

End Module