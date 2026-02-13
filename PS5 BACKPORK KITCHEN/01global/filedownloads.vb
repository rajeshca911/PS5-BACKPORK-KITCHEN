Imports System.Net.Http
Imports System.Net.Http.Headers
Imports Newtonsoft.Json.Linq
Imports System.IO

Module filedownloads

    Public Async Function DownloadSelfUtil() As Task

        Form1.TableLayoutPanel1.Visible = True
        Dim owner As String = "CyB1K"
        Dim repo As String = "Selfutil-Patched"

        Await DownloadLatestRelease(owner, repo, selfutilpath)

        Dim rtbtext As RichTextBox = Form1.rtbStatus
        Log(rtbtext, "Process completed.")

    End Function

    Async Function DownloadLatestRelease(owner As String, repo As String, Optional appPath As String = "") As Task
        Dim apiUrl As String = $"https://api.github.com/repos/{owner}/{repo}/releases/latest"
        Dim rtbtext As RichTextBox = Form1.rtbStatus

        Log(rtbtext, "*** *** ***", ColorTranslator.FromHtml("#424A52"), False)
        updatestatus("Downloading latest SelfUtil release...", 2)
        Using client As New HttpClient()
            client.DefaultRequestHeaders.UserAgent.Add(
            New ProductInfoHeaderValue("selfUtil", "1.0")
        )

            Try
                Dim response As String = Await client.GetStringAsync(apiUrl)
                Dim releaseJson As JObject = JObject.Parse(response)

                Dim assets As JArray = TryCast(releaseJson("assets"), JArray)

                If assets Is Nothing OrElse assets.Count = 0 Then
                    Log(rtbtext, "No assets found in the latest release.", ColorTranslator.FromHtml("#424A52"), False)
                    Return
                End If
                If appPath = String.Empty Then
                    appPath = AppDomain.CurrentDomain.BaseDirectory

                End If
                If Not Directory.Exists(appPath) Then
                    Directory.CreateDirectory(appPath)
                End If

                Dim downloadedAny As Boolean = False

                For Each asset As JObject In assets
                    Dim fileName As String = asset("name")?.ToString()
                    Dim downloadUrl As String = asset("browser_download_url")?.ToString()

                    If String.IsNullOrEmpty(fileName) OrElse String.IsNullOrEmpty(downloadUrl) Then Continue For

                    If fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) _
                    OrElse fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) Then

                        Dim fullPath As String = Path.Combine(appPath, fileName)
                        'file undo ledo chusdum

                        If File.Exists(fullPath) Then
                            Log(rtbtext, $"Skipped (already exists): {fileName}",
        ColorTranslator.FromHtml("#6A737D"), False)

                            Continue For
                        End If

                        Log(rtbtext, $"Downloading: {fileName}", ColorTranslator.FromHtml("#424A52"), False)
                        updatestatus($"Downloading... {fileName}", 2)
                        Await DownloadFileWithProgress(client, downloadUrl, fullPath, rtbtext)

                        Log(rtbtext, $"Downloaded: {fileName}", ColorTranslator.FromHtml("#424A52"), False)
                        downloadedAny = True
                    End If
                Next

                If Not downloadedAny Then
                    Log(rtbtext, "No downloadable EXE or DLL files found, or all files are already downloaded.", ColorTranslator.FromHtml("#424A52"), False)
                Else
                    Log(rtbtext, "Download successful!", ColorTranslator.FromHtml("#424A52"), False)
                End If
            Catch ex As Exception
                Log(rtbtext, "Error: " & ex.Message, ColorTranslator.FromHtml("#424A52"), False)
            End Try
        End Using
        updatestatus("Download process completed.", 1)
        Log(rtbtext, "*** *** ***", ColorTranslator.FromHtml("#424A52"), False)
    End Function

    Private Async Function DownloadFileWithProgress(
    client As HttpClient,
    url As String,
    destinationPath As String,
    rtbtext As RichTextBox
) As Task

        Using response As HttpResponseMessage =
        Await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)

            response.EnsureSuccessStatusCode()

            Dim totalBytes As Long = If(response.Content.Headers.ContentLength, -1L)
            Dim totalRead As Long = 0
            Dim buffer(8191) As Byte
            Dim lastProgress As Integer = -1

            Using contentStream As Stream = Await response.Content.ReadAsStreamAsync(),
              fileStream As New FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None)

                While True
                    Dim bytesRead As Integer = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                    If bytesRead = 0 Then Exit While

                    Await fileStream.WriteAsync(buffer, 0, bytesRead)
                    totalRead += bytesRead

                    If totalBytes > 0 Then
                        Dim progress As Integer = CInt((totalRead * 100L) / totalBytes)

                        If progress <> lastProgress Then
                            lastProgress = progress
                            'Log(rtbtext, $"Downloading... {progress}%", ColorTranslator.FromHtml("#6A737D"), False)
                            'updatestatus($"Downloading... {progress}%", 2)
                            Form1.Invoke(Sub()
                                             Log(rtbtext, $"Downloading... {progress}%", ColorTranslator.FromHtml("#6A737D"), False)
                                             'updatestatus($"Downloading... {destinationPath}", 2)
                                         End Sub)

                        End If
                    End If
                End While
            End Using
        End Using
    End Function

End Module