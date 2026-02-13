Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions

Namespace Services.GameSearch
    ''' <summary>
    ''' TorrentGalaxy search provider
    ''' Public access, no login required
    ''' Good source for PS5/PS4 games
    ''' </summary>
    Public Class TorrentGalaxyProvider
        Implements IGameSearchProvider

        Private ReadOnly _mirrors As String() = {
            "https://torrentgalaxy.to",
            "https://tgx.rs",
            "https://torrentgalaxy.mx"
        }

        Private _currentMirror As String = "https://torrentgalaxy.to"
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
                Return "torrentgalaxy"
            End Get
        End Property

        Public ReadOnly Property DisplayName As String Implements IGameSearchProvider.DisplayName
            Get
                Return "TorrentGalaxy"
            End Get
        End Property

        Public ReadOnly Property RequiresAuthentication As Boolean Implements IGameSearchProvider.RequiresAuthentication
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsLoggedIn As Boolean Implements IGameSearchProvider.IsLoggedIn
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Status As ProviderStatus Implements IGameSearchProvider.Status
            Get
                Return _status
            End Get
        End Property

        Public Sub SetCredentials(credentials As ProviderCredentials) Implements IGameSearchProvider.SetCredentials
        End Sub

        Public Function LoginAsync() As Task(Of Boolean) Implements IGameSearchProvider.LoginAsync
            Return Task.FromResult(True)
        End Function

        Public Sub Logout() Implements IGameSearchProvider.Logout
        End Sub

        Public Async Function SearchAsync(query As GameSearchQuery, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult)) Implements IGameSearchProvider.SearchAsync
            Dim results As New List(Of GameSearchResult)

            Dim mirrorFound = Await FindWorkingMirrorAsync()
            If Not mirrorFound Then
                _status.LastError = "No working mirror found"
                Return results
            End If

            Try
                Dim searchTerm = query.SearchText
                If query.Platform = GamePlatform.PS5 Then
                    searchTerm = $"PS5 {searchTerm}"
                ElseIf query.Platform = GamePlatform.PS4 Then
                    searchTerm = $"PS4 {searchTerm}"
                End If

                ' TorrentGalaxy search URL format
                Dim encodedSearch = Uri.EscapeDataString(searchTerm).Replace("%20", "+")
                Dim searchUrl = $"{_currentMirror}/torrents.php?search={encodedSearch}&sort=seeders&order=desc&page=0"

                Dim response = Await _httpClient.GetAsync(searchUrl, cancellationToken)
                Dim html = Await response.Content.ReadAsStringAsync()

                If String.IsNullOrEmpty(html) OrElse html.Length < 1000 Then
                    _status.LastError = "Empty response"
                    Return results
                End If

                results = ParseSearchResults(html, query.MaxResults)

                Select Case query.SortBy
                    Case SearchSortBy.Seeds
                        results = results.OrderByDescending(Function(r) r.Seeds).ToList()
                    Case SearchSortBy.Size
                        results = results.OrderByDescending(Function(r) r.SizeBytes).ToList()
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
                ' TorrentGalaxy uses divs with class "tgxtablerow" for each result
                ' Try multiple row patterns
                Dim rowPattern = "<div[^>]*class=""[^""]*tgxtablerow[^""]*""[^>]*>(.*?)</div>\s*</div>\s*</div>"
                Dim rowMatches = Regex.Matches(html, rowPattern, RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                ' If first pattern fails, try alternative
                If rowMatches.Count = 0 Then
                    rowPattern = "<div[^>]*tgxtablerow[^>]*>.*?(?=<div[^>]*tgxtablerow|<div[^>]*class=""pagination""|$)"
                    rowMatches = Regex.Matches(html, rowPattern, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                End If

                ' Try table-based layout as fallback
                If rowMatches.Count = 0 Then
                    rowPattern = "<tr[^>]*>.*?</tr>"
                    rowMatches = Regex.Matches(html, rowPattern, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                End If

                _status.LastError = $"Found {rowMatches.Count} rows"

                For Each rowMatch As Match In rowMatches
                    If results.Count >= maxResults Then Exit For

                    Dim rowHtml = rowMatch.Value

                    ' Skip header rows or rows without torrent links
                    If Not rowHtml.Contains("/torrent/") AndAlso Not rowHtml.Contains("magnet:") Then Continue For

                    Dim result As New GameSearchResult With {
                        .SourceProvider = DisplayName
                    }

                    ' Extract title - multiple patterns
                    Dim title As String = ""

                    ' Pattern 1: Link with txlight class containing bold text
                    Dim titleMatch = Regex.Match(rowHtml, "<a[^>]*class=""[^""]*txlight[^""]*""[^>]*>.*?<b>([^<]+)</b>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    If titleMatch.Success Then
                        title = titleMatch.Groups(1).Value.Trim()
                    End If

                    ' Pattern 2: Link to /torrent/ with text content
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "<a[^>]*href=""[^""]*torrent[^""]*""[^>]*title=""([^""]+)""", RegexOptions.IgnoreCase)
                        If titleMatch.Success Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    ' Pattern 3: Any bold text longer than 10 chars
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "<a[^>]*href=""[^""]*torrent[^""]*""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase)
                        If titleMatch.Success AndAlso titleMatch.Groups(1).Value.Trim().Length > 5 Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    ' Pattern 4: Bold text in row
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "<b>([^<]{10,})</b>", RegexOptions.IgnoreCase)
                        If titleMatch.Success Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    If String.IsNullOrEmpty(title) OrElse title.Length < 5 Then Continue For

                    result.Title = WebUtility.HtmlDecode(title)

                    ' Extract torrent link
                    Dim linkMatch = Regex.Match(rowHtml, "href=""(/torrent/\d+/[^""]+)""", RegexOptions.IgnoreCase)
                    If linkMatch.Success Then
                        result.DetailsUrl = $"{_currentMirror}{linkMatch.Groups(1).Value}"
                    End If

                    ' Extract magnet link
                    Dim magnetMatch = Regex.Match(rowHtml, "href=""(magnet:\?[^""]+)""", RegexOptions.IgnoreCase)
                    If magnetMatch.Success Then
                        result.MagnetLink = WebUtility.HtmlDecode(magnetMatch.Groups(1).Value)
                    End If

                    ' Extract size - multiple patterns
                    Dim sizeMatch = Regex.Match(rowHtml, "(\d+(?:\.\d+)?)\s*(GB|MB|KB|TB|GiB|MiB)", RegexOptions.IgnoreCase)
                    If sizeMatch.Success Then
                        result.Size = $"{sizeMatch.Groups(1).Value} {sizeMatch.Groups(2).Value}"
                        result.SizeBytes = ParseSizeString(result.Size)
                    End If

                    ' Extract seeds - look for green/colored numbers
                    Dim seedMatch = Regex.Match(rowHtml, "color[^>]*green[^>]*>.*?<b>(\d+)</b>|class=""[^""]*seed[^""]*""[^>]*>(\d+)<|[Ss]eed[^>]*>(\d+)<", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    If seedMatch.Success Then
                        Dim seedVal = If(Not String.IsNullOrEmpty(seedMatch.Groups(1).Value), seedMatch.Groups(1).Value,
                                        If(Not String.IsNullOrEmpty(seedMatch.Groups(2).Value), seedMatch.Groups(2).Value, seedMatch.Groups(3).Value))
                        Integer.TryParse(seedVal, result.Seeds)
                    End If

                    ' Extract leeches - look for red/colored numbers
                    Dim leechMatch = Regex.Match(rowHtml, "color[^>]*red[^>]*>.*?<b>(\d+)</b>|class=""[^""]*leech[^""]*""[^>]*>(\d+)<|[Ll]eech[^>]*>(\d+)<", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    If leechMatch.Success Then
                        Dim leechVal = If(Not String.IsNullOrEmpty(leechMatch.Groups(1).Value), leechMatch.Groups(1).Value,
                                         If(Not String.IsNullOrEmpty(leechMatch.Groups(2).Value), leechMatch.Groups(2).Value, leechMatch.Groups(3).Value))
                        Integer.TryParse(leechVal, result.Leeches)
                    End If

                    ' Fallback: look for S: and L: pattern
                    If result.Seeds = 0 Then
                        Dim slMatch = Regex.Match(rowHtml, "(\d+)\s*[/|]\s*(\d+)", RegexOptions.IgnoreCase)
                        If slMatch.Success Then
                            Integer.TryParse(slMatch.Groups(1).Value, result.Seeds)
                            Integer.TryParse(slMatch.Groups(2).Value, result.Leeches)
                        End If
                    End If

                    ' Detect platform
                    Dim upperTitle = result.Title.ToUpper()
                    If upperTitle.Contains("PS5") Then
                        result.Platform = "PS5"
                    ElseIf upperTitle.Contains("PS4") Then
                        result.Platform = "PS4"
                    End If

                    ' Extract firmware
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
                Dim match = Regex.Match(sizeStr, "(\d+(?:[.,]\d+)?)\s*([KMGT]?i?B)", RegexOptions.IgnoreCase)
                If match.Success Then
                    Dim value As Double
                    Dim numStr = match.Groups(1).Value.Replace(",", ".")
                    Double.TryParse(numStr, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, value)
                    Dim unit = match.Groups(2).Value.ToUpper().Replace("I", "")

                    Select Case unit
                        Case "TB" : Return CLng(value * 1099511627776)
                        Case "GB" : Return CLng(value * 1073741824)
                        Case "MB" : Return CLng(value * 1048576)
                        Case "KB" : Return CLng(value * 1024)
                        Case Else : Return CLng(value)
                    End Select
                End If
            Catch
            End Try
            Return 0
        End Function

        Public Async Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String) Implements IGameSearchProvider.GetMagnetLinkAsync
            If Not String.IsNullOrEmpty(result.MagnetLink) Then Return result.MagnetLink
            If String.IsNullOrEmpty(result.DetailsUrl) Then Return ""

            Try
                Dim response = Await _httpClient.GetAsync(result.DetailsUrl)
                Dim html = Await response.Content.ReadAsStringAsync()

                Dim magnetMatch = Regex.Match(html, "href=""(magnet:\?xt=urn:btih:[^""]+)""", RegexOptions.IgnoreCase)
                If magnetMatch.Success Then
                    Return WebUtility.HtmlDecode(magnetMatch.Groups(1).Value)
                End If
            Catch
            End Try

            Return ""
        End Function

        Public Async Function TestConnectionAsync() As Task(Of Boolean) Implements IGameSearchProvider.TestConnectionAsync
            Return Await FindWorkingMirrorAsync()
        End Function
    End Class
End Namespace
