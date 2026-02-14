Imports System.IO
Imports System.Net.Http
Imports System.Drawing

''' <summary>
''' Fetches and caches game cover art thumbnails from PlayStation TMDB CDN.
''' Uses Title ID (e.g. PPSA12345) to look up icon0.png.
''' </summary>
Public Class CoverArtService

    Private Shared ReadOnly _httpClient As New HttpClient()
    Private Shared ReadOnly _cacheDir As String =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "PS5BackporkKitchen", "covers")

    Shared Sub New()
        _httpClient.Timeout = TimeSpan.FromSeconds(10)
        If Not Directory.Exists(_cacheDir) Then
            Directory.CreateDirectory(_cacheDir)
        End If
    End Sub

    ''' <summary>
    ''' Returns cached cover art image synchronously, or Nothing if not cached.
    ''' </summary>
    Public Shared Function GetCachedImage(gameId As String) As Image
        If String.IsNullOrEmpty(gameId) Then Return Nothing

        Dim cachePath = Path.Combine(_cacheDir, $"{gameId}.png")
        If File.Exists(cachePath) Then
            Try
                Using fs As New FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Return Image.FromStream(fs)
                End Using
            Catch
                Return Nothing
            End Try
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Fetches cover art from TMDB CDN, caches it, and returns the image.
    ''' Tries multiple URL patterns used by PS4/PS5 titles.
    ''' </summary>
    Public Shared Async Function GetCoverArtAsync(gameId As String) As Task(Of Image)
        If String.IsNullOrEmpty(gameId) Then Return Nothing

        ' Check cache first
        Dim cached = GetCachedImage(gameId)
        If cached IsNot Nothing Then Return cached

        ' Check for negative cache (previously failed lookups)
        Dim failMarker = Path.Combine(_cacheDir, $"{gameId}.notfound")
        If File.Exists(failMarker) Then
            Dim age = DateTime.Now - File.GetLastWriteTime(failMarker)
            If age.TotalHours < 24 Then Return Nothing
        End If

        ' Try multiple TMDB CDN URL patterns
        Dim urls As String() = {
            $"https://tmdb.np.dl.playstation.net/tmdb2/{gameId}_00_1/{gameId}_00_1/icon0.png",
            $"https://tmdb.np.dl.playstation.net/tmdb2/{gameId}_00/{gameId}_00/icon0.png"
        }

        For Each url As String In urls
            Try
                Dim response = Await _httpClient.GetAsync(url)
                If response.IsSuccessStatusCode Then
                    Dim data = Await response.Content.ReadAsByteArrayAsync()
                    If data.Length > 100 Then
                        ' Save to cache
                        Dim cachePath = Path.Combine(_cacheDir, $"{gameId}.png")
                        File.WriteAllBytes(cachePath, data)

                        Using ms As New MemoryStream(data)
                            Return Image.FromStream(ms)
                        End Using
                    End If
                End If
            Catch
                ' Try next URL
            End Try
        Next

        ' Mark as not found to avoid retrying too often
        Try
            File.WriteAllText(failMarker, "")
        Catch
        End Try

        Return Nothing
    End Function

End Class
