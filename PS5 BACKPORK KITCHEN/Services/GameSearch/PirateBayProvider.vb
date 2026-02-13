Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json.Linq

Namespace Services.GameSearch
    ''' <summary>
    ''' PirateBay search provider
    ''' Public access, no login required
    ''' </summary>
    Public Class PirateBayProvider
        Implements IGameSearchProvider

        Private ReadOnly _mirrors As String() = {
            "https://thepiratebay.org",
            "https://piratebay.live",
            "https://thepiratebay10.org",
            "https://thepiratebay.zone",
            "https://tpb.party"
        }

        ' Alternative: use apibay.org API
        Private Const API_URL As String = "https://apibay.org"

        Private _currentMirror As String = "https://thepiratebay.org"
        Private _useApi As Boolean = True
        Private _httpClient As HttpClient
        Private _status As New ProviderStatus() With {.IsEnabled = True}

        Public Sub New()
            Dim handler As New HttpClientHandler With {
                .AllowAutoRedirect = True,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate
            }
            _httpClient = New HttpClient(handler)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json,text/html,*/*")
            _httpClient.Timeout = TimeSpan.FromSeconds(30)
        End Sub

        Public ReadOnly Property Name As String Implements IGameSearchProvider.Name
            Get
                Return "piratebay"
            End Get
        End Property

        Public ReadOnly Property DisplayName As String Implements IGameSearchProvider.DisplayName
            Get
                Return "PirateBay"
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

            Try
                Dim searchTerm = query.SearchText
                If query.Platform = GamePlatform.PS5 Then
                    searchTerm = $"PS5 {searchTerm}"
                ElseIf query.Platform = GamePlatform.PS4 Then
                    searchTerm = $"PS4 {searchTerm}"
                End If

                ' Try API first (apibay.org)
                If _useApi Then
                    results = Await SearchViaApiAsync(searchTerm, query.MaxResults, cancellationToken)
                End If

                ' Fallback to scraping if API fails
                If results.Count = 0 Then
                    _useApi = False
                    results = Await SearchViaScrapingAsync(searchTerm, query.MaxResults, cancellationToken)
                End If

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

        Private Async Function SearchViaApiAsync(searchTerm As String, maxResults As Integer, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult))
            Dim results As New List(Of GameSearchResult)

            Try
                ' apibay.org API: /q.php?q=searchterm
                Dim searchUrl = $"{API_URL}/q.php?q={Uri.EscapeDataString(searchTerm)}"
                Dim response = Await _httpClient.GetAsync(searchUrl, cancellationToken)
                Dim json = Await response.Content.ReadAsStringAsync()

                If String.IsNullOrEmpty(json) OrElse json = "[]" Then Return results

                ' Check for "no results" response (single item with id="0" and name="No results...")
                If json.Contains("""id"":""0""") AndAlso json.Contains("No results") Then
                    Return results
                End If

                ' Parse JSON array using Newtonsoft.Json
                Dim items = JArray.Parse(json)

                For Each item As JObject In items
                    If results.Count >= maxResults Then Exit For

                    Dim result As New GameSearchResult With {
                        .SourceProvider = DisplayName
                    }

                    ' Extract fields
                    Dim name = item.Value(Of String)("name")
                    If String.IsNullOrEmpty(name) Then Continue For

                    result.Title = WebUtility.HtmlDecode(name)

                    Dim hash = item.Value(Of String)("info_hash")
                    If Not String.IsNullOrEmpty(hash) Then
                        result.MagnetLink = $"magnet:?xt=urn:btih:{hash}&dn={Uri.EscapeDataString(result.Title)}"
                    End If

                    Dim id = item.Value(Of String)("id")
                    If Not String.IsNullOrEmpty(id) AndAlso id <> "0" Then
                        result.DetailsUrl = $"{_currentMirror}/description.php?id={id}"
                    End If

                    Dim seeders = item.Value(Of String)("seeders")
                    If Not String.IsNullOrEmpty(seeders) Then
                        Integer.TryParse(seeders, result.Seeds)
                    End If

                    Dim leechers = item.Value(Of String)("leechers")
                    If Not String.IsNullOrEmpty(leechers) Then
                        Integer.TryParse(leechers, result.Leeches)
                    End If

                    Dim size = item.Value(Of String)("size")
                    If Not String.IsNullOrEmpty(size) Then
                        Long.TryParse(size, result.SizeBytes)
                        result.Size = FormatSize(result.SizeBytes)
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
                _status.LastError = $"API error: {ex.Message}"
            End Try

            Return results
        End Function

        Private Async Function SearchViaScrapingAsync(searchTerm As String, maxResults As Integer, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult))
            Dim results As New List(Of GameSearchResult)

            ' URL formats to try for each mirror
            Dim urlFormats = {
                "/search/{0}/1/99/0",       ' Classic format: /search/term/page/sortby/category
                "/search.php?q={0}",         ' Query string format
                "/s/?q={0}&page=0&orderby=99" ' Alternative query format
            }

            ' Try to find working mirror with each URL format
            For Each mirror In _mirrors
                For Each urlFormat In urlFormats
                    Try
                        Dim encodedTerm = Uri.EscapeDataString(searchTerm)
                        Dim searchUrl = mirror & String.Format(urlFormat, encodedTerm)

                        Dim cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(10))
                        Dim linkedCts = Threading.CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken)

                        Dim response = Await _httpClient.GetAsync(searchUrl, linkedCts.Token)

                        If response.IsSuccessStatusCode Then
                            _currentMirror = mirror
                            Dim html = Await response.Content.ReadAsStringAsync()

                            ' Check if we got a real results page
                            If html.Length > 1000 AndAlso html.Contains("magnet:") Then
                                results = ParseHtmlResults(html, maxResults)
                                If results.Count > 0 Then Return results
                            End If
                        End If
                    Catch
                        Continue For
                    End Try
                Next
            Next

            Return results
        End Function

        Private Function ParseHtmlResults(html As String, maxResults As Integer) As List(Of GameSearchResult)
            Dim results As New List(Of GameSearchResult)

            Try
                ' PirateBay search results - multiple pattern attempts
                Dim rowPattern = "<tr[^>]*>.*?</tr>"
                Dim rowMatches = Regex.Matches(html, rowPattern, RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                For Each rowMatch As Match In rowMatches
                    If results.Count >= maxResults Then Exit For

                    Dim rowHtml = rowMatch.Value
                    If Not rowHtml.Contains("magnet:") Then Continue For

                    Dim result As New GameSearchResult With {
                        .SourceProvider = DisplayName
                    }

                    ' Extract title - multiple patterns
                    Dim title As String = ""

                    ' Pattern 1: detLink class
                    Dim titleMatch = Regex.Match(rowHtml, "class=""[^""]*detLink[^""]*""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase)
                    If titleMatch.Success Then
                        title = titleMatch.Groups(1).Value.Trim()
                    End If

                    ' Pattern 2: link to torrent details
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "href=""/torrent/[^""]+""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase)
                        If titleMatch.Success Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    ' Pattern 3: any link before magnet
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "<a[^>]+>([^<]{10,})</a>.*?magnet:", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                        If titleMatch.Success Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    If String.IsNullOrEmpty(title) OrElse title.Length < 5 Then Continue For
                    result.Title = WebUtility.HtmlDecode(title)

                    ' Extract magnet
                    Dim magnetMatch = Regex.Match(rowHtml, "href=""(magnet:\?[^""]+)""", RegexOptions.IgnoreCase)
                    If magnetMatch.Success Then
                        result.MagnetLink = WebUtility.HtmlDecode(magnetMatch.Groups(1).Value)
                    End If

                    ' Extract size - multiple patterns
                    Dim sizeMatch = Regex.Match(rowHtml, "(\d+(?:\.\d+)?)\s*(?:&nbsp;)?\s*(GiB|MiB|KiB|GB|MB|KB|TB)", RegexOptions.IgnoreCase)
                    If sizeMatch.Success Then
                        result.Size = $"{sizeMatch.Groups(1).Value} {sizeMatch.Groups(2).Value}"
                        result.SizeBytes = ParseSizeString(result.Size)
                    End If

                    ' Extract seeds/leeches - multiple patterns
                    ' Pattern 1: td with align
                    Dim slMatch = Regex.Match(rowHtml, "align=""right"">(\d+)</td>\s*<td[^>]*align=""right"">(\d+)</td>", RegexOptions.IgnoreCase)
                    If slMatch.Success Then
                        Integer.TryParse(slMatch.Groups(1).Value, result.Seeds)
                        Integer.TryParse(slMatch.Groups(2).Value, result.Leeches)
                    Else
                        ' Pattern 2: consecutive td with numbers at end of row
                        slMatch = Regex.Match(rowHtml, "<td[^>]*>(\d+)</td>\s*<td[^>]*>(\d+)</td>\s*</tr>", RegexOptions.IgnoreCase)
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

                    results.Add(result)
                Next

            Catch ex As Exception
                _status.LastError = $"Parse error: {ex.Message}"
            End Try

            Return results
        End Function

        Private Function FormatSize(bytes As Long) As String
            If bytes >= 1099511627776 Then Return $"{bytes / 1099511627776.0:F2} TB"
            If bytes >= 1073741824 Then Return $"{bytes / 1073741824.0:F2} GB"
            If bytes >= 1048576 Then Return $"{bytes / 1048576.0:F2} MB"
            If bytes >= 1024 Then Return $"{bytes / 1024.0:F2} KB"
            Return $"{bytes} B"
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

        Public Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String) Implements IGameSearchProvider.GetMagnetLinkAsync
            If Not String.IsNullOrEmpty(result.MagnetLink) Then Return Task.FromResult(result.MagnetLink)
            Return Task.FromResult("")
        End Function

        Public Async Function TestConnectionAsync() As Task(Of Boolean) Implements IGameSearchProvider.TestConnectionAsync
            Try
                ' Test API first
                Dim response = Await _httpClient.GetAsync($"{API_URL}/q.php?q=test")
                If response.IsSuccessStatusCode Then
                    _useApi = True
                    Return True
                End If
            Catch
            End Try

            ' Test mirrors
            For Each mirror In _mirrors
                Try
                    Dim cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(5))
                    Dim response = Await _httpClient.GetAsync(mirror, cts.Token)
                    If response.IsSuccessStatusCode Then
                        _currentMirror = mirror
                        _useApi = False
                        Return True
                    End If
                Catch
                    Continue For
                End Try
            Next

            _status.LastError = "All sources unreachable"
            Return False
        End Function
    End Class
End Namespace
