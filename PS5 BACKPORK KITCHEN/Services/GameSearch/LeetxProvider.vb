Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions

Namespace Services.GameSearch
    ''' <summary>
    ''' 1337x search provider
    ''' Public access, no login required
    ''' </summary>
    Public Class LeetxProvider
        Implements IGameSearchProvider

        ' Multiple mirrors for fallback
        Private ReadOnly _mirrors As String() = {
            "https://1337x.to",
            "https://1337x.st",
            "https://x1337x.ws",
            "https://1337x.gd",
            "https://1337x.is",
            "https://1337x.wtf"
        }

        Private _currentMirror As String = "https://1337x.to"
        Private _httpClient As HttpClient
        Private _status As New ProviderStatus() With {.IsEnabled = True}

        Public Sub New()
            Dim handler As New HttpClientHandler With {
                .AllowAutoRedirect = True,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate
            }
            _httpClient = New HttpClient(handler)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")
            _httpClient.Timeout = TimeSpan.FromSeconds(30)
        End Sub

        Public ReadOnly Property Name As String Implements IGameSearchProvider.Name
            Get
                Return "1337x"
            End Get
        End Property

        Public ReadOnly Property DisplayName As String Implements IGameSearchProvider.DisplayName
            Get
                Return "1337x"
            End Get
        End Property

        Public ReadOnly Property RequiresAuthentication As Boolean Implements IGameSearchProvider.RequiresAuthentication
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsLoggedIn As Boolean Implements IGameSearchProvider.IsLoggedIn
            Get
                Return True ' No login required
            End Get
        End Property

        Public ReadOnly Property Status As ProviderStatus Implements IGameSearchProvider.Status
            Get
                Return _status
            End Get
        End Property

        Public Sub SetCredentials(credentials As ProviderCredentials) Implements IGameSearchProvider.SetCredentials
            ' Not required
        End Sub

        Public Function LoginAsync() As Task(Of Boolean) Implements IGameSearchProvider.LoginAsync
            Return Task.FromResult(True) ' No login required
        End Function

        Public Sub Logout() Implements IGameSearchProvider.Logout
            ' Not required
        End Sub

        Public Async Function SearchAsync(query As GameSearchQuery, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult)) Implements IGameSearchProvider.SearchAsync
            Dim results As New List(Of GameSearchResult)

            ' Find a working mirror first
            Dim mirrorFound = Await FindWorkingMirrorAsync()
            If Not mirrorFound Then
                _status.LastError = "No working mirror found"
                Return results
            End If

            Try
                ' Build search query with platform
                Dim searchTerm = query.SearchText
                If query.Platform = GamePlatform.PS5 Then
                    searchTerm = $"PS5 {searchTerm}"
                ElseIf query.Platform = GamePlatform.PS4 Then
                    searchTerm = $"PS4 {searchTerm}"
                End If

                ' URL format: /search/QUERY/PAGE/ (properly encode, then spaces become +)
                Dim encodedSearch = Uri.EscapeDataString(searchTerm).Replace("%20", "+")
                Dim searchUrl = $"{_currentMirror}/search/{encodedSearch}/1/"

                Dim response = Await _httpClient.GetAsync(searchUrl, cancellationToken)
                Dim html = Await response.Content.ReadAsStringAsync()

                ' Debug: check if we got results page
                If String.IsNullOrEmpty(html) OrElse html.Length < 1000 Then
                    _status.LastError = "Empty or invalid response"
                    Return results
                End If

                ' Parse results
                results = ParseSearchResults(html, query.MaxResults)

                ' Sort results
                Select Case query.SortBy
                    Case SearchSortBy.Seeds
                        results = results.OrderByDescending(Function(r) r.Seeds).ToList()
                    Case SearchSortBy.Size
                        results = results.OrderByDescending(Function(r) r.SizeBytes).ToList()
                    Case SearchSortBy.UploadDate
                        results = results.OrderByDescending(Function(r) r.UploadDate).ToList()
                    Case SearchSortBy.Name
                        results = results.OrderBy(Function(r) r.Title).ToList()
                End Select

                _status.LastSearchTime = DateTime.Now
                _status.TotalSearches += 1
                _status.LastError = If(results.Count = 0, "No results found", "")

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                _status.LastError = ex.Message
            End Try

            Return results
        End Function

        Private Async Function FindWorkingMirrorAsync() As Task(Of Boolean)
            For Each mirror In _mirrors
                Try
                    Dim cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(5))
                    Dim response = Await _httpClient.GetAsync(mirror, cts.Token)
                    If response.IsSuccessStatusCode Then
                        _currentMirror = mirror
                        Return True
                    End If
                Catch
                    Continue For
                End Try
            Next
            Return False
        End Function

        Private Function ParseSearchResults(html As String, maxResults As Integer) As List(Of GameSearchResult)
            Dim results As New List(Of GameSearchResult)

            Try
                ' Find all torrent links: a[href*="/torrent/"]
                Dim torrentPattern = "<a\s+href=""/torrent/(\d+)/([^/""]+)/""[^>]*>([^<]*)</a>"
                Dim torrentMatches = Regex.Matches(html, torrentPattern, RegexOptions.IgnoreCase)

                ' Find all seeds: td.coll-2
                Dim seedsPattern = "<td\s+class=""coll-2[^""]*""[^>]*>(\d+)</td>"
                Dim seedsMatches = Regex.Matches(html, seedsPattern, RegexOptions.IgnoreCase)

                ' Find all leeches: td.coll-3
                Dim leechPattern = "<td\s+class=""coll-3[^""]*""[^>]*>(\d+)</td>"
                Dim leechMatches = Regex.Matches(html, leechPattern, RegexOptions.IgnoreCase)

                ' Find all sizes: td.coll-4
                Dim sizePattern = "<td\s+class=""coll-4[^""]*""[^>]*>([^<]+)"
                Dim sizeMatches = Regex.Matches(html, sizePattern, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                ' Find all dates: td.coll-date
                Dim datePattern = "<td\s+class=""coll-date[^""]*""[^>]*>([^<]+)</td>"
                Dim dateMatches = Regex.Matches(html, datePattern, RegexOptions.IgnoreCase)

                ' Process matches (skip header row - indices might need adjustment)
                Dim torrentIndex = 0
                For i = 0 To Math.Min(torrentMatches.Count, maxResults) - 1
                    ' Skip non-torrent links (some links might be for categories, etc.)
                    If torrentIndex >= torrentMatches.Count Then Exit For

                    Dim torrentMatch = torrentMatches(torrentIndex)
                    torrentIndex += 1

                    Dim result As New GameSearchResult With {
                        .SourceProvider = DisplayName
                    }

                    ' Extract torrent info
                    Dim torrentId = torrentMatch.Groups(1).Value
                    Dim torrentSlug = torrentMatch.Groups(2).Value
                    result.DetailsUrl = $"{_currentMirror}/torrent/{torrentId}/{torrentSlug}/"

                    ' Get title from link text or decode slug
                    Dim linkText = torrentMatch.Groups(3).Value.Trim()
                    If Not String.IsNullOrEmpty(linkText) Then
                        result.Title = WebUtility.HtmlDecode(linkText)
                    Else
                        result.Title = WebUtility.UrlDecode(torrentSlug.Replace("-", " "))
                    End If

                    ' Skip if title is empty or too short
                    If String.IsNullOrEmpty(result.Title) OrElse result.Title.Length < 3 Then Continue For

                    ' Get seeds (index corresponds to row)
                    If i < seedsMatches.Count Then
                        Integer.TryParse(seedsMatches(i).Groups(1).Value, result.Seeds)
                    End If

                    ' Get leeches
                    If i < leechMatches.Count Then
                        Integer.TryParse(leechMatches(i).Groups(1).Value, result.Leeches)
                    End If

                    ' Get size
                    If i < sizeMatches.Count Then
                        Dim sizeText = Regex.Replace(sizeMatches(i).Groups(1).Value, "<[^>]+>", "").Trim()
                        result.Size = sizeText
                        result.SizeBytes = ParseSizeString(sizeText)
                    End If

                    ' Get date
                    If i < dateMatches.Count Then
                        Dim dateText = dateMatches(i).Groups(1).Value.Trim()
                        DateTime.TryParse(dateText, result.UploadDate)
                    End If

                    ' Detect platform from title
                    Dim upperTitle = result.Title.ToUpper()
                    If upperTitle.Contains("PS5") OrElse upperTitle.Contains("PLAYSTATION 5") Then
                        result.Platform = "PS5"
                    ElseIf upperTitle.Contains("PS4") OrElse upperTitle.Contains("PLAYSTATION 4") Then
                        result.Platform = "PS4"
                    End If

                    ' Extract firmware requirement from title
                    Dim fwMatch = Regex.Match(result.Title, "(\d+\.\d+)\s*(?:FW|Firmware)", RegexOptions.IgnoreCase)
                    If fwMatch.Success Then
                        result.FirmwareRequired = fwMatch.Groups(1).Value
                    End If

                    results.Add(result)
                Next

            Catch ex As Exception
                _status.LastError = $"Parse error: {ex.Message}"
            End Try

            Return results
        End Function

        Private Function ParseSizeString(sizeStr As String) As Long
            Try
                sizeStr = Regex.Replace(sizeStr, "<[^>]+>", "").Trim()

                Dim match = Regex.Match(sizeStr, "(\d+(?:[.,]\d+)?)\s*([KMGT]?i?B)", RegexOptions.IgnoreCase)
                If match.Success Then
                    Dim value As Double
                    Dim numStr = match.Groups(1).Value.Replace(",", ".")
                    Double.TryParse(numStr, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, value)
                    Dim unit = match.Groups(2).Value.ToUpper().Replace("I", "")

                    Select Case unit
                        Case "TB"
                            Return CLng(value * 1099511627776)
                        Case "GB"
                            Return CLng(value * 1073741824)
                        Case "MB"
                            Return CLng(value * 1048576)
                        Case "KB"
                            Return CLng(value * 1024)
                        Case Else
                            Return CLng(value)
                    End Select
                End If
            Catch
            End Try
            Return 0
        End Function

        Public Async Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String) Implements IGameSearchProvider.GetMagnetLinkAsync
            If String.IsNullOrEmpty(result.DetailsUrl) Then Return ""

            Try
                Dim response = Await _httpClient.GetAsync(result.DetailsUrl)
                Dim html = Await response.Content.ReadAsStringAsync()

                ' Look for magnet link: href="magnet:?..."
                Dim magnetMatch = Regex.Match(html, "href=""(magnet:\?xt=urn:btih:[^""]+)""", RegexOptions.IgnoreCase)
                If magnetMatch.Success Then
                    Return WebUtility.HtmlDecode(magnetMatch.Groups(1).Value)
                End If

                ' Try alternative pattern
                magnetMatch = Regex.Match(html, "magnet:\?xt=urn:btih:[a-zA-Z0-9]+[^""'\s<>]*", RegexOptions.IgnoreCase)
                If magnetMatch.Success Then
                    Return magnetMatch.Value
                End If

            Catch ex As Exception
                _status.LastError = $"Magnet error: {ex.Message}"
            End Try

            Return ""
        End Function

        Public Async Function TestConnectionAsync() As Task(Of Boolean) Implements IGameSearchProvider.TestConnectionAsync
            For Each mirror In _mirrors
                Try
                    Dim cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(5))
                    Dim response = Await _httpClient.GetAsync(mirror, cts.Token)
                    If response.IsSuccessStatusCode Then
                        _currentMirror = mirror
                        _status.LastError = ""
                        Return True
                    End If
                Catch ex As Exception
                    Continue For
                End Try
            Next
            _status.LastError = "All mirrors unreachable"
            Return False
        End Function
    End Class
End Namespace
