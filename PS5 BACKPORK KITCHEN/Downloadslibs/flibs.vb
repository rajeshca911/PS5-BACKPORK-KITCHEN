Imports System.IO
Imports System.Net.Http
Imports System.Runtime
Imports System.Text
Imports Newtonsoft.Json
Imports PS5_BACKPORK_KITCHEN.Architecture.Application

Public Class FakelibRoot
    Public Property meta As MetaInfo
    Public Property archives As List(Of ArchiveInfo)
    Public Property payloads As PayloadsInfo
End Class

Public Class MetaInfo
    Public Property updated As String
    Public Property version As String
End Class

Public Class ArchiveInfo
    Public Property id As String
    Public Property filename As String
    Public Property url As String

    Public Property enabled As Boolean
    Public Property isProtected As Boolean
    Public Property password As String

    <JsonProperty("type")>
    Public Property ArchiveType As String

    Public Property description As String
End Class

Public Class PayloadsInfo
    Public Property fakelibs As List(Of FakelibPayload)
    Public Property standalone As List(Of StandalonePayload)
End Class

Public Class FakelibPayload

    <JsonProperty("fw_version")>
    Public Property Firmware As String

    <JsonProperty("base_url")>
    Public Property BaseUrl As String

    <JsonProperty("is_recommended")>
    Public Property IsRecommended As Boolean

    Public Property files As List(Of String)
End Class

Public Class StandalonePayload
    Public Property name As String
    Public Property enabled As Boolean

    <JsonProperty("base_url")>
    Public Property BaseUrl As String

    Public Property files As List(Of String)
End Class

Public Class FakelibInfo
    Public Property fw As String
    Public Property url As String
    Public Property note As String
    Public Property files As List(Of String)
End Class

Public Class ZipInfo
    Public Property id As String
    Public Property url As String

    <JsonProperty("protected")>
    Public Property IsProtected As Boolean

    Public Property password As String
    Public Property note As String
End Class

Module flibs
    Private _cachedRoot As FakelibRoot = Nothing
    Private _lastJsonFetch As DateTime = DateTime.MinValue
    Private ReadOnly _jsonCacheDuration As TimeSpan = TimeSpan.FromMinutes(30)

    Public Async Function GetFakelibRootAsync() As Task(Of FakelibRoot)
        Dim jsonText = String.Empty
#If DEBUG Then
        Dim jsonfile As String = "E:\Proj\PS5 BACKPORK KITCHEN\fakelibs.json"

        ' Check if the file actually exists before trying to read it
        If System.IO.File.Exists(jsonfile) Then

            jsonText = System.IO.File.ReadAllText(jsonfile)

            _cachedRoot = Newtonsoft.Json.JsonConvert.DeserializeObject(Of FakelibRoot)(jsonText)
            _lastJsonFetch = DateTime.Now

            Return _cachedRoot
        Else

            Debug.WriteLine("DEBUG ERROR: fakelibs.json not found at " & jsonfile)
            MessageBox.Show("DEBUG ERROR: fakelibs.json not found at " & jsonfile, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End If
#End If


        If _cachedRoot IsNot Nothing AndAlso
           DateTime.Now - _lastJsonFetch < _jsonCacheDuration Then
            Return _cachedRoot
        End If

        Using client As New HttpClient()
            jsonText = Await client.GetStringAsync(fakelibJsonUrl)
            _cachedRoot = JsonConvert.DeserializeObject(Of FakelibRoot)(jsonText)
            _lastJsonFetch = DateTime.Now
        End Using

        Return _cachedRoot
        '   Dim jsonText As String =
        'Await File.ReadAllTextAsync("C:\Users\rajes\OneDrive\Documents\fakelibs.json")

        '   Return JsonConvert.DeserializeObject(Of FakelibRoot)(jsonText)

    End Function

    'download fakelibs for current fw but not using from 23.01-2026 since dependency archive contains it
    Public Async Function DownloadFakelibsAsync() As Task

        Dim root = Await GetFakelibRootAsync()

        Dim fwEntry =
        root.payloads.fakelibs.
            FirstOrDefault(Function(f) f.Firmware = fwMajor.ToString())

        If fwEntry Is Nothing Then
            Logger.Log(Form1.rtbStatus,
                   $"No fakelibs defined for firmware {fwMajor}",
                   Color.Red)
            Return
        End If

        Dim baseUrl As String = Base64Decode(fwEntry.BaseUrl)

        Dim destinationRoot =
        Path.Combine(AppContext.BaseDirectory,
                     fwMajor.ToString(),
                     "fakelib")

        Directory.CreateDirectory(destinationRoot)

        Using client As New HttpClient()

            For Each fileName In fwEntry.files

                Dim fullUrl = $"{baseUrl.TrimEnd("/"c)}/{fileName}"
                Dim localPath = Path.Combine(destinationRoot, fileName)

                If File.Exists(localPath) Then
                    Logger.Log(Form1.rtbStatus,
                           $"Skipped: {fileName}",
                           Color.Black)
                    Continue For
                End If

                Try
                    Dim data = Await client.GetByteArrayAsync(fullUrl)
                    Await File.WriteAllBytesAsync(localPath, data)

                    Logger.Log(Form1.rtbStatus,
                           $"Downloaded: {fileName}",
                           Color.Green)
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus,
                           $"Failed: {fileName} – {ex.Message}",
                           Color.Red)

                    MessageBox.Show(
                    $"Failed to download:{Environment.NewLine}{fileName}",
                    "Download Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

                    Exit For
                End Try
            Next
        End Using
    End Function

    Public Async Function DownloadAllArchivesAsync() As Task

        Dim root = Await GetFakelibRootAsync()

        If root.archives Is Nothing OrElse root.archives.Count = 0 Then
            Logger.Log(Form1.rtbStatus,
                   "No archives defined in JSON",
                   Color.Red)
            Return
        End If

        Dim archiveDir =
        Path.Combine(AppContext.BaseDirectory, "archives")

        Directory.CreateDirectory(archiveDir)

        Using client As New HttpClient()
            Dim downloadOk As Boolean = False
            Dim extractOk As Boolean = True

            For Each archive In root.archives

                If Not archive.enabled Then
                    Logger.Log(Form1.rtbStatus,
                           $"Skipped (disabled): {archive.id}",
                           Color.Gray)
                    Continue For
                End If

                Dim archiveUrl As String = Base64Decode(archive.url)
                Dim localPath = Path.Combine(archiveDir, archive.filename)

                If File.Exists(localPath) Then
                    downloadOk = True
                    Logger.Log(Form1.rtbStatus,
                           $"Already exists: {archive.filename}",
                           Color.Black)
                    Continue For
                End If

                Try
                    Logger.Log(Form1.rtbStatus,
                           $"Downloading: {archive.filename}",
                           Color.Black)

                    Dim data = Await client.GetByteArrayAsync(archiveUrl)
                    Await File.WriteAllBytesAsync(localPath, data)
                    downloadOk = True
                    Logger.Log(Form1.rtbStatus,
                           $"Downloaded: {archive.filename}",
                           Color.Green)
                    Try
                        Logger.Log(Form1.rtbStatus,
                           $"Extracting: {archive.filename}",
                           Color.Green)
                        Dim pwd As String = Base64Decode(archive.password)
                        ' Decode password (will be "" if no password)
                        pwd = If(archive.isProtected, Base64Decode(archive.password), "")

                        ' Call the same method for everything
                        ExtractArchive(
                            localPath,
                            AppContext.BaseDirectory,
                            pwd
                        )

                        '                If archive.isProtected Then
                        '                    ExtractArchiveWithPassword(
                        '                    localPath,
                        '                    AppContext.BaseDirectory,
                        '                    pwd
                        '                )
                        '                Else
                        '                    ' Extract WITHOUT Password
                        '                    ExtractArchive(
                        '    localPath,
                        '    AppContext.BaseDirectory
                        ')
                        '                End If
                    Catch ex As Exception
                        extractOk = False
                        Logger.Log(Form1.rtbStatus,
                           $"Extraction failed: {archive.filename} – {ex.Message}",
                           Color.Red)
                    End Try
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus,
                           $"Failed: {archive.filename} – {ex.Message}",
                           Color.Red)

                End Try
                If archive.ArchiveType = "libs" AndAlso (Not downloadOk OrElse Not extractOk) Then
                    ShowNotification(GetFakelibManualNotice(),
                                     "📢 FUTURE FAKELIB SUPPORT NOTICE",
                                     "FakeLib Setup Guide")
                End If
            Next

        End Using
    End Function

    Private Function GetFakelibManualNotice() As String

        Return "For future firmware support, FakeLibs must be placed manually " &
        "inside the tool folder using the firmware number as the folder name." & vbCrLf & vbCrLf &
        "📂 Required Folder Structure:" & vbCrLf &
        "Tool Folder" & vbCrLf &
        "  ├─ 7" & vbCrLf &
        "  │   └─ fakelib" & vbCrLf &
        "  ├─ 6" & vbCrLf &
        "  │   └─ fakelib" & vbCrLf &
        "  └─ ..." & vbCrLf & vbCrLf &
        "⚠️ Notes:" & vbCrLf &
        "- Firmware folder name must match the firmware version" & vbCrLf &
        "- fakelib folder name must remain exactly 'fakelib'" & vbCrLf &
        "- You can add multiple firmware folders as needed" & vbCrLf & vbCrLf &
        "This structure allows automatic detection of FakeLibs " &
        "for upcoming firmware versions."
    End Function

    Public Async Function DownloadAllStandaloneAsync() As Task
        Try

            Dim root = Await GetFakelibRootAsync()

            If root.payloads.standalone Is Nothing OrElse
           root.payloads.standalone.Count = 0 Then

                Logger.Log(Form1.rtbStatus,
                       "No standalone items defined",
                       Color.Red)
                Return
            End If

            For Each item In root.payloads.standalone

                ' Skip disabled entries
                If Not item.enabled Then
                    Logger.Log(Form1.rtbStatus,
                           $"Skipped (disabled): {item.name}",
                           Color.Gray)
                    Continue For
                End If

                Await DownloadStandaloneItemAsync(item)
            Next
        Catch ex As Exception
            Log(Form1.rtbStatus,
                $"Error downloading standalone items: {ex.Message}",
                Color.Red)
        End Try
    End Function

    Private Async Function DownloadStandaloneItemAsync(item As StandalonePayload) As Task

        Dim baseUrl As String = Base64Decode(item.BaseUrl)

        Dim destinationRoot =
        Path.Combine(AppContext.BaseDirectory, item.name)

        Directory.CreateDirectory(destinationRoot)

        Using client As New HttpClient()

            For Each fileName In item.files

                Dim fullUrl = $"{baseUrl.TrimEnd("/"c)}/{fileName}"
                Dim localPath = Path.Combine(destinationRoot, fileName)

                If File.Exists(localPath) Then
                    Logger.Log(Form1.rtbStatus,
                           $"Skipped: {item.name}/{fileName}",
                           Color.Black)
                    Continue For
                End If

                Try
                    Dim data = Await client.GetByteArrayAsync(fullUrl)
                    Await File.WriteAllBytesAsync(localPath, data)

                    Logger.Log(Form1.rtbStatus,
                           $"Downloaded: {item.name}/{fileName}",
                           Color.Green)
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus,
                           $"Failed: {item.name}/{fileName} – {ex.Message}",
                           Color.Red)
                    Exit For
                End Try
            Next

        End Using
    End Function

    Public Function Base64Encode(input As String) As String
        Return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
    End Function

    Public Function Base64Decode(input As String) As String
        Return Encoding.UTF8.GetString(Convert.FromBase64String(input))
    End Function

End Module