Imports System.Net.Http
Imports System.Text.Json

''' <summary>
''' Enhanced auto-update checker service with user preferences and non-invasive notifications.
''' </summary>
''' <remarks>
''' Created: 2026-01-27
''' Author: DroneTechTI
''' Feature: Auto-Update Checker v1.0
'''
''' FEATURES:
''' - Automatic background check on startup
''' - User-configurable (enable/disable)
''' - "Remind me later" functionality
''' - "Skip this version" option
''' - Non-invasive notification
''' - Fallback to GitHub if Supabase fails
''' </remarks>
Public Class UpdateCheckerService

    Private Const GITHUB_OWNER As String = "rajeshca911"
    Private Const REPO_NAME As String = "PS5-BACKPORK-KITCHEN"
    Private Const RELEASES_URL As String = "https://github.com/rajeshca911/PS5-BACKPORK-KITCHEN/releases"

    ''' <summary>
    ''' Result of an update check operation.
    ''' </summary>
    Public Class UpdateCheckResult
        Public Property UpdateAvailable As Boolean
        Public Property CurrentVersion As String
        Public Property LatestVersion As String
        Public Property ReleaseUrl As String
        Public Property ReleaseNotes As String
        Public Property ErrorMessage As String
        Public Property CheckedSuccessfully As Boolean = True
    End Class

    ''' <summary>
    ''' User's decision about an update.
    ''' </summary>
    Public Enum UpdateDecision
        DownloadNow
        RemindLater
        SkipVersion
        Cancelled
    End Enum

    ''' <summary>
    ''' Checks for updates asynchronously.
    ''' </summary>
    ''' <returns>Update check result</returns>
    Public Shared Async Function CheckForUpdatesAsync() As Task(Of UpdateCheckResult)
        Dim result As New UpdateCheckResult()

        Try
            ' Get current version
            result.CurrentVersion = My.Application.Info.Version.ToString()

            ' Try Supabase first (if configured)
            Try
                Dim supabaseResult = Await CheckSupabaseAsync(result.CurrentVersion)
                If supabaseResult IsNot Nothing Then
                    Return supabaseResult
                End If
            Catch ex As Exception
                Debug.WriteLine($"Supabase check failed: {ex.Message}")
            End Try

            ' Fallback to GitHub
            result = Await CheckGitHubAsync(result.CurrentVersion)
        Catch ex As Exception
            result.CheckedSuccessfully = False
            result.ErrorMessage = ex.Message
            Debug.WriteLine($"Update check failed: {ex.Message}")
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Checks GitHub for the latest release.
    ''' </summary>
    Private Shared Async Function CheckGitHubAsync(currentVersion As String) As Task(Of UpdateCheckResult)
        Dim result As New UpdateCheckResult With {
            .CurrentVersion = currentVersion
        }

        Using client As New HttpClient()
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{REPO_NAME}/1.0")

            Dim url = $"https://api.github.com/repos/{GITHUB_OWNER}/{REPO_NAME}/releases/latest"
            Dim json = Await client.GetStringAsync(url)

            Using doc = JsonDocument.Parse(json)
                Dim root = doc.RootElement

                ' Get version info
                Dim latestTag = root.GetProperty("tag_name").GetString()
                result.LatestVersion = latestTag.TrimStart("v"c)
                result.ReleaseUrl = RELEASES_URL

                ' Get release notes if available
                Dim bodyProp As JsonElement
                If root.TryGetProperty("body", bodyProp) Then
                    result.ReleaseNotes = bodyProp.GetString()
                End If

                ' Compare versions
                Dim currentVer = Version.Parse(currentVersion)
                Dim latestVer = Version.Parse(result.LatestVersion)

                result.UpdateAvailable = (latestVer > currentVer)
                result.CheckedSuccessfully = True
            End Using
        End Using

        Return result
    End Function

    ''' <summary>
    ''' Checks Supabase for update info (if API configured).
    ''' </summary>
    Private Shared Async Function CheckSupabaseAsync(currentVersion As String) As Task(Of UpdateCheckResult)
        ' Only if Supabase is configured
        If String.IsNullOrEmpty(k1) OrElse String.IsNullOrEmpty(k2) Then
            Return Nothing
        End If

        Dim result As New UpdateCheckResult With {
            .CurrentVersion = currentVersion
        }

        Try
            Dim baseUrl = "https://ruwcewlkrjyiltgnudaf.supabase.co/rest/v1/AppMasters"
            Dim key = $"{k1}.{k2}.{k3}"
            Dim encodedAppName = Uri.EscapeDataString(REPO_NAME)
            Dim fullUrl = $"{baseUrl}?select=*&AppName=eq.{encodedAppName}"

            Using client As New HttpClient()
                client.DefaultRequestHeaders.Add("apikey", key)
                client.DefaultRequestHeaders.Accept.Add(
                    New Headers.MediaTypeWithQualityHeaderValue("application/json"))

                Dim response = Await client.GetAsync(fullUrl)
                response.EnsureSuccessStatusCode()

                Dim json = Await response.Content.ReadAsStringAsync()
                Dim apps = System.Text.Json.JsonSerializer.Deserialize(Of List(Of SupabaseApp))(
                    json,
                    New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})

                If apps IsNot Nothing AndAlso apps.Count > 0 Then
                    Dim app = apps(0)
                    result.LatestVersion = app.Version.Trim()
                    result.ReleaseUrl = If(String.IsNullOrEmpty(app.DL1), RELEASES_URL, app.DL1)
                    result.ReleaseNotes = app.Message

                    Dim currentVer = Version.Parse(currentVersion)
                    Dim latestVer = Version.Parse(result.LatestVersion)

                    result.UpdateAvailable = (latestVer > currentVer)
                    result.CheckedSuccessfully = True

                    Return result
                End If
            End Using
        Catch ex As Exception
            Debug.WriteLine($"Supabase check failed: {ex.Message}")
            Return Nothing
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Checks if auto-update checking is enabled.
    ''' </summary>
    Public Shared Function IsAutoUpdateEnabled() As Boolean
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            Return config.CheckForUpdatesOnStartup
        Catch
            Return True ' Default: enabled
        End Try
    End Function

    ''' <summary>
    ''' Sets whether auto-update checking is enabled.
    ''' </summary>
    Public Shared Sub SetAutoUpdateEnabled(enabled As Boolean)
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            config.CheckForUpdatesOnStartup = enabled
            ConfigurationManager.SaveConfiguration(config)
        Catch ex As Exception
            Debug.WriteLine($"Failed to save auto-update setting: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Checks if a specific version should be skipped.
    ''' </summary>
    Public Shared Function IsVersionSkipped(version As String) As Boolean
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            Return config.SkippedUpdateVersion = version
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Marks a version as skipped.
    ''' </summary>
    Public Shared Sub SkipVersion(version As String)
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            config.SkippedUpdateVersion = version
            ConfigurationManager.SaveConfiguration(config)
        Catch ex As Exception
            Debug.WriteLine($"Failed to save skipped version: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Clears the skipped version.
    ''' </summary>
    Public Shared Sub ClearSkippedVersion()
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            config.SkippedUpdateVersion = ""
            ConfigurationManager.SaveConfiguration(config)
        Catch ex As Exception
            Debug.WriteLine($"Failed to clear skipped version: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gets the last time updates were checked.
    ''' </summary>
    Public Shared Function GetLastCheckTime() As DateTime
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            Return config.LastUpdateCheck
        Catch
            Return DateTime.MinValue
        End Try
    End Function

    ''' <summary>
    ''' Updates the last check time to now.
    ''' </summary>
    Public Shared Sub UpdateLastCheckTime()
        Try
            Dim config = ConfigurationManager.LoadConfiguration()
            config.LastUpdateCheck = DateTime.Now
            ConfigurationManager.SaveConfiguration(config)
        Catch ex As Exception
            Debug.WriteLine($"Failed to save last check time: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Checks if enough time has passed since last check (24 hours).
    ''' </summary>
    Public Shared Function ShouldCheckForUpdates() As Boolean
        Dim lastCheck = GetLastCheckTime()
        Dim hoursSinceCheck = (DateTime.Now - lastCheck).TotalHours
        Return hoursSinceCheck >= 24 OrElse lastCheck = DateTime.MinValue
    End Function

End Class