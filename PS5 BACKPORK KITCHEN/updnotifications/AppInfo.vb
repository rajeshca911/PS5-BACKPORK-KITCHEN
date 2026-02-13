Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text.Json
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Module AppInfo

    Public Const mygithub As String = "rajeshca911"
    Public Const RepoName As String = "PS5-BACKPORK-KITCHEN"

    Public Async Function CheckGitHubUpdateAsync() As Task
        Dim AppVersion As String = My.Application.Info.Version.ToString()
        Try
            Using client As New HttpClient()
                client.DefaultRequestHeaders.UserAgent.ParseAdd(RepoName)

                Dim url =
                $"https://api.github.com/repos/{mygithub}/{RepoName}/releases/latest"

                Dim json = Await client.GetStringAsync(url)
                Using doc = JsonDocument.Parse(json)
                    Dim latestTag = doc.RootElement.GetProperty("tag_name").GetString()
                    Log(Form1.rtbStatus, $"App version : {AppVersion}", Color.Green)
                    Log(Form1.rtbStatus, $"Latest version on GitHub: {latestTag}", Color.Green)
                    If Version.Parse(latestTag.TrimStart("v")) >
                   Version.Parse(AppVersion) Then

                        Dim message As String =
                                $"A new version of the application is available.{vbCrLf}{vbCrLf}" &
                                $"Current version : {AppVersion}{vbCrLf}" &
                                $"Latest version  : {latestTag}{vbCrLf}{vbCrLf}" &
                                "Would you like to visit GitHub to download the update?"

                        Dim response = MessageBox.Show(
                                message,
                                "Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information
                            )

                        If response = DialogResult.Yes Then
                            OpenURL("https://github.com/rajeshca911/PS5-BACKPORK-KITCHEN/releases")
                        End If
                    End If
                End Using
            End Using
            updatestatus()
        Catch ex As Exception
            Log(Form1.rtbStatus, $"Unable to check Updates {ex.ToString}", Color.Red)

        End Try
        'main updatecheck

    End Function

    Public Function EncodeUrlParameter(value As String) As String
        Return Uri.EscapeDataString(value)
    End Function

    'main updatecheck
    Public Async Function CheckUpdateSupabaseAsync() As Task

        updatestatus("Checking Update Server 1 (Supabase)...", 2)

        Dim baseUrl As String =
            "https://ruwcewlkrjyiltgnudaf.supabase.co/rest/v1/AppMasters"

        ' --- API KEY (keep as you already split it) ---
        Dim key As String = $"{k1}.{k2}.{k3}"

        Dim appName As String = RepoName
        Dim encodedAppName As String = Uri.EscapeDataString(appName)

        Dim fullUrl As String =
            $"{baseUrl}?select=*&AppName=eq.{encodedAppName}"

        Dim currentVersion As Version = My.Application.Info.Version

        Try
            Using client As New HttpClient()

                client.DefaultRequestHeaders.Add("apikey", key)
                client.DefaultRequestHeaders.Accept.Add(
                    New MediaTypeWithQualityHeaderValue("application/json")
                )

                Dim response As HttpResponseMessage =
                    Await client.GetAsync(fullUrl)

                response.EnsureSuccessStatusCode()

                Logger.Log(Form1.rtbStatus,
                           "Connected to Update Server 1",
                           Color.Green)

                Dim json As String =
                    Await response.Content.ReadAsStringAsync()

                Dim apps As List(Of SupabaseApp) =
                    System.Text.Json.JsonSerializer.Deserialize(Of List(Of SupabaseApp))(
                        json,
                        New JsonSerializerOptions With {
                            .PropertyNameCaseInsensitive = True
                        })

                If apps Is Nothing OrElse apps.Count = 0 Then
                    Logger.Log(Form1.rtbStatus,
                               "No update data found on server",
                               Color.Orange)
                    Return
                End If

                For Each app In apps

                    Dim onlineVersion As Version =
                        Version.Parse(app.Version.Trim())

                    Logger.Log(Form1.rtbStatus,
                               $"Current Version : {currentVersion}",
                               Color.Blue)

                    Logger.Log(Form1.rtbStatus,
                               $"Found Version   : {onlineVersion}",
                               Color.Blue)

                    If onlineVersion > currentVersion Then
                        Logger.Log(Form1.rtbStatus,
                                   "Update Available !! (Supabase)",
                                   Color.Green)

                        Dim message As String =
                               $"A new version of the application is available.{vbCrLf}{vbCrLf}" &
                               $"Current version : {currentVersion}{vbCrLf}" &
                               $"Latest version  : {app.Version}{vbCrLf}{vbCrLf}" &
                               "Would you like to visit GitHub to download the update?"

                        Dim res = MessageBox.Show(
                                message,
                                "Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information
                            )

                        If res = DialogResult.Yes Then
                            OpenURL("https://github.com/rajeshca911/PS5-BACKPORK-KITCHEN/releases")
                        End If
                    Else
                        Logger.Log(Form1.rtbStatus,
                                   "No Update Available !! (Supabase)",
                                   Color.Green)
                    End If
                Next
            End Using
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus,
                       "Error connecting to Update Server 1: " & ex.Message,
                       Color.Red)
        End Try

    End Function

End Module

Public Class SupabaseApp
    Public Property AppName As String
    Public Property Version As String
    Public Property DL1 As String
    Public Property ForceUpdate As Boolean
    Public Property Message As String
End Class