Imports System.IO
Imports System.Net.Http
Imports Newtonsoft.Json
Imports SharpCompress.Archives
Imports SharpCompress.Archives.Zip
Imports SharpCompress.Common

Module ziphandler

    'main
    Public Async Function EnsureDependenciesAsync() As Task

        Dim depsDir = Path.Combine(AppContext.BaseDirectory, "deps")
        Dim zipPath = Path.Combine(depsDir, "deps.zip")
        Dim zipUrl = "https://yourserver/deps.zip"
        Dim zipPassword = "ps5-tools"

        If DependenciesPresent(depsDir) Then
            Logger.Log(Form1.rtbStatus, "Dependencies already present ✓", Color.Green)
            Exit Function
        End If

        Logger.Log(Form1.rtbStatus, "Downloading dependencies…")

        Await DownloadFileAsync(zipUrl, zipPath)

        Logger.Log(Form1.rtbStatus, "Extracting dependencies…")

        ExtractArchiveWithPassword(zipPath, depsDir, zipPassword)

        File.Delete(zipPath)

        Logger.Log(Form1.rtbStatus, "Setup completed successfully ✓", Color.Green)
    End Function

    Public Function DependenciesPresent(baseDir As String) As Boolean
        Dim requiredFiles = {
        "libSceAgc.sprx",
        "libSceNpAuth.sprx",
        "ps5-backpork.elf"
    }

        Return requiredFiles.All(
        Function(f) File.Exists(Path.Combine(baseDir, f))
    )
    End Function

    'Download zip
    Public Async Function DownloadFileAsync(
    url As String,
    destination As String
) As Task

        Directory.CreateDirectory(Path.GetDirectoryName(destination))

        Using client As New HttpClient()
            Using response = Await client.GetAsync(url)
                response.EnsureSuccessStatusCode()

                Using fs As New FileStream(destination, FileMode.Create, FileAccess.Write)
                    Await response.Content.CopyToAsync(fs)
                End Using
            End Using
        End Using
    End Function

    'extract zip or rar with password
    Public Sub ExtractArchiveWithPassword(
        archivePath As String,
        extractTo As String,
        password As String
    )
        IO.Directory.CreateDirectory(extractTo)

        Dim options As New SharpCompress.Readers.ReaderOptions() With {
            .Password = password
        }

        ' Use ArchiveFactory to handle both .zip and .rar automatically
        Using archive As SharpCompress.Archives.IArchive =
            SharpCompress.Archives.ArchiveFactory.Open(archivePath, options)

            For Each entry In archive.Entries
                If Not entry.IsDirectory Then
                    entry.WriteToDirectory(
                        extractTo,
                        New SharpCompress.Common.ExtractionOptions With {
                            .ExtractFullPath = True,
                            .Overwrite = True
                        }
                    )
                End If
            Next
        End Using
    End Sub

    Public Sub SaveOperationHistory(item As OperationHistoryItem)

        Dim reportsDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "reports"
    )

        If Not Directory.Exists(reportsDir) Then
            Directory.CreateDirectory(reportsDir)
        End If

        Dim fileName =
        $"report_{item.Timestamp:yyyyMMdd_HHmmss}.json"

        Dim fullPath = Path.Combine(reportsDir, fileName)

        Dim json = JsonConvert.SerializeObject(
        item,
        Formatting.Indented
    )

        File.WriteAllText(fullPath, json)

    End Sub

End Module