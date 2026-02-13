Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions

Namespace Services.GameSearch
    ''' <summary>
    ''' DLPSGame.com search provider for PS5/PS4 game downloads.
    ''' Scrapes game listings and extracts direct download links from
    ''' multiple hosting services (1fichier, Mediafire, Gofile, etc.).
    ''' </summary>
    Public Class DLPSGameProvider
        Implements IGameSearchProvider

        Private Const BASE_URL As String = "https://dlpsgame.com"

        ' Known hosting domains for download link extraction
        Private Shared ReadOnly HostDomains As String() = {
            "1fichier.com", "mediafire.com", "www.mediafire.com",
            "gofile.io", "akirabox.com", "vikingfile.com",
            "rootz.so", "www.rootz.so", "1cloudfile.com"
        }

        Private _httpClient As HttpClient
        Private _status As New ProviderStatus() With {.IsEnabled = True}

        Public Sub New()
            Dim handler As New HttpClientHandler With {
                .AllowAutoRedirect = True,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate
            }
            _httpClient = New HttpClient(handler)
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")
            _httpClient.Timeout = TimeSpan.FromSeconds(30)
        End Sub

        Public ReadOnly Property Name As String Implements IGameSearchProvider.Name
            Get
                Return "DLPSGame"
            End Get
        End Property

        Public ReadOnly Property DisplayName As String Implements IGameSearchProvider.DisplayName
            Get
                Return "DLPSGame"
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
                Dim searchTerm = query.SearchText?.Trim()
                If String.IsNullOrEmpty(searchTerm) Then Return results

                ' Build search URL
                Dim encodedSearch = Uri.EscapeDataString(searchTerm)
                Dim searchUrl = $"{BASE_URL}/?s={encodedSearch}"

                Dim response = Await _httpClient.GetAsync(searchUrl, cancellationToken)
                If Not response.IsSuccessStatusCode Then
                    _status.LastError = $"HTTP {CInt(response.StatusCode)}"
                    Return results
                End If

                Dim html = Await response.Content.ReadAsStringAsync()
                If String.IsNullOrEmpty(html) OrElse html.Length < 500 Then
                    _status.LastError = "Empty response"
                    Return results
                End If

                ' Parse search listing results
                Dim listingResults = ParseListingPage(html)

                ' Filter by platform if requested
                If query.Platform = GamePlatform.PS5 Then
                    listingResults = listingResults.Where(
                        Function(r) r.Title.IndexOf("PS5", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                    r.DetailsUrl.IndexOf("-ps5", StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList()
                ElseIf query.Platform = GamePlatform.PS4 Then
                    listingResults = listingResults.Where(
                        Function(r) r.Title.IndexOf("PS4", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                    r.DetailsUrl.IndexOf("-ps4", StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList()
                End If

                ' For each listing result, fetch the game page to get download links
                Dim maxToFetch = Math.Min(listingResults.Count, query.MaxResults)
                For i = 0 To maxToFetch - 1
                    If cancellationToken.IsCancellationRequested Then Exit For

                    Dim listing = listingResults(i)
                    Try
                        Dim gameResults = Await FetchGamePageAsync(listing, cancellationToken)
                        results.AddRange(gameResults)
                    Catch ex As OperationCanceledException
                        Throw
                    Catch
                        ' Skip failed game pages, continue with next
                        results.Add(listing)
                    End Try

                    ' Small delay between requests to be respectful
                    If i < maxToFetch - 1 Then
                        Await Task.Delay(300, cancellationToken)
                    End If
                Next

                ' Sort results
                Select Case query.SortBy
                    Case SearchSortBy.Name
                        results = results.OrderBy(Function(r) r.Title).ToList()
                    Case SearchSortBy.Size
                        results = results.OrderByDescending(Function(r) r.SizeBytes).ToList()
                    Case SearchSortBy.UploadDate
                        results = results.OrderByDescending(Function(r) r.UploadDate).ToList()
                End Select

                _status.LastSearchTime = DateTime.Now
                _status.TotalSearches += 1
                _status.LastError = If(results.Count = 0, "No results found", "")

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                _status.LastError = ex.Message
            End Try

            Return results.Take(query.MaxResults).ToList()
        End Function

        ''' <summary>
        ''' Parses the search/category listing page to extract game links and titles.
        ''' </summary>
        Private Function ParseListingPage(html As String) As List(Of GameSearchResult)
            Dim results As New List(Of GameSearchResult)

            Try
                ' Pattern: <a href="https://dlpsgame.com/game-slug/">Title</a> within g-col divs
                ' Also matches links with images followed by title links
                Dim linkPattern = "<a\s+href=""(https?://dlpsgame\.com/[^""]+/?)""[^>]*>\s*(?:<img[^>]*>)?\s*([^<]*?)\s*</a>"
                Dim matches = Regex.Matches(html, linkPattern, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                For Each m As Match In matches
                    Dim url = m.Groups(1).Value.Trim()
                    Dim title = WebUtility.HtmlDecode(m.Groups(2).Value.Trim())

                    ' Skip non-game pages (categories, tags, author, page links)
                    If url.Contains("/category/") OrElse url.Contains("/tag/") OrElse
                       url.Contains("/author/") OrElse url.Contains("/page/") OrElse
                       url = BASE_URL & "/" OrElse url = BASE_URL Then
                        Continue For
                    End If

                    ' Skip duplicate URLs
                    If seen.Contains(url) Then Continue For
                    seen.Add(url)

                    ' If title is empty, extract from URL slug
                    If String.IsNullOrEmpty(title) Then
                        Dim slug = url.TrimEnd("/"c).Split("/"c).Last()
                        title = slug.Replace("-", " ")
                        title = Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title)
                    End If

                    ' Skip very short or meaningless titles
                    If title.Length < 3 Then Continue For

                    ' Detect platform
                    Dim platform = ""
                    Dim upperTitle = title.ToUpper()
                    If upperTitle.Contains("PS5") OrElse url.Contains("-ps5") Then
                        platform = "PS5"
                    ElseIf upperTitle.Contains("PS4") OrElse url.Contains("-ps4") Then
                        platform = "PS4"
                    End If

                    results.Add(New GameSearchResult With {
                        .Title = title,
                        .DetailsUrl = url,
                        .SourceProvider = DisplayName,
                        .Platform = platform,
                        .Category = "Game"
                    })
                Next
            Catch ex As Exception
                _status.LastError = $"Parse error: {ex.Message}"
            End Try

            Return results
        End Function

        ''' <summary>
        ''' Fetches a game detail page and extracts download links, metadata, and version info.
        ''' Returns one GameSearchResult per download section (game, update, DLC, backport).
        ''' </summary>
        Private Async Function FetchGamePageAsync(listing As GameSearchResult, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult))
            Dim results As New List(Of GameSearchResult)

            Dim response = Await _httpClient.GetAsync(listing.DetailsUrl, cancellationToken)
            If Not response.IsSuccessStatusCode Then
                results.Add(listing)
                Return results
            End If

            Dim html = Await response.Content.ReadAsStringAsync()

            ' Extract metadata
            Dim gameTitle = listing.Title
            Dim titleMatch = Regex.Match(html, "<h1[^>]*>([^<]+)</h1>", RegexOptions.IgnoreCase)
            If titleMatch.Success Then
                gameTitle = WebUtility.HtmlDecode(titleMatch.Groups(1).Value.Trim())
            End If

            ' Extract region/PKG ID
            Dim pkgIdMatch = Regex.Match(html, "((?:PPSA|CUSA)\d{5})\s*[-–]\s*(USA|EUR|JPN|ASIA|JP)", RegexOptions.IgnoreCase)
            Dim region = If(pkgIdMatch.Success, pkgIdMatch.Groups(2).Value.ToUpper(), listing.Region)
            Dim pkgId = If(pkgIdMatch.Success, pkgIdMatch.Groups(1).Value, "")

            ' Extract firmware requirement
            Dim fwMatch = Regex.Match(html, "Works\s+on\s+(\d+)\.xx\s+and\s+higher", RegexOptions.IgnoreCase)
            Dim firmware = If(fwMatch.Success, $"{fwMatch.Groups(1).Value}.00", "")

            ' Extract genre
            Dim genreMatch = Regex.Match(html, "GENRE\s*:\s*([^\r\n<]+)", RegexOptions.IgnoreCase)
            Dim genre = If(genreMatch.Success, genreMatch.Groups(1).Value.Trim(), "")

            ' Extract password (always DLPSGAME.COM)
            Dim pwMatch = Regex.Match(html, "Password\s*:\s*([^\r\n<]+)", RegexOptions.IgnoreCase)
            Dim password = If(pwMatch.Success, pwMatch.Groups(1).Value.Trim(), "")

            ' Find all download links by host domain
            Dim allLinks = ExtractDownloadLinks(html)

            If allLinks.Count = 0 Then
                ' No download links found, return listing as-is
                listing.Title = gameTitle
                listing.Region = region
                listing.FirmwareRequired = firmware
                results.Add(listing)
                Return results
            End If

            ' Try to parse structured download sections (Game, Update, DLC, Backport)
            Dim sections = ParseDownloadSections(html, allLinks)

            If sections.Count > 0 Then
                For Each section In sections
                    Dim result As New GameSearchResult With {
                        .Title = $"{gameTitle} [{section.Label}]",
                        .DetailsUrl = listing.DetailsUrl,
                        .SourceProvider = DisplayName,
                        .Platform = listing.Platform,
                        .Region = region,
                        .FirmwareRequired = firmware,
                        .Category = section.Label,
                        .DownloadUrl = listing.DetailsUrl,
                        .Size = If(Not String.IsNullOrEmpty(password), $"Password: {password}", ""),
                        .Uploader = If(section.Links.Count > 0, String.Join(" | ", section.Links.Select(Function(l) $"{l.HostName}: {l.Url}")), "")
                    }

                    ' Set MagnetLink to the first available download link (for copy functionality)
                    If section.Links.Count > 0 Then
                        result.MagnetLink = section.Links(0).Url
                    End If

                    results.Add(result)
                Next
            Else
                ' Flat list of download links - create single result
                Dim result As New GameSearchResult With {
                    .Title = gameTitle,
                    .DetailsUrl = listing.DetailsUrl,
                    .SourceProvider = DisplayName,
                    .Platform = listing.Platform,
                    .Region = region,
                    .FirmwareRequired = firmware,
                    .Category = "Game",
                    .DownloadUrl = listing.DetailsUrl,
                    .Size = If(Not String.IsNullOrEmpty(password), $"Password: {password}", ""),
                    .Uploader = String.Join(" | ", allLinks.Take(6).Select(Function(l) $"{l.HostName}: {l.Url}"))
                }

                If allLinks.Count > 0 Then
                    result.MagnetLink = allLinks(0).Url
                End If

                results.Add(result)
            End If

            Return results
        End Function

        ''' <summary>
        ''' Extracts all download links from hosting services found in the HTML.
        ''' </summary>
        Private Function ExtractDownloadLinks(html As String) As List(Of DownloadLink)
            Dim links As New List(Of DownloadLink)
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            ' Match all anchor href attributes
            Dim hrefPattern = "<a\s+[^>]*href=""(https?://[^""]+)""[^>]*>"
            Dim matches = Regex.Matches(html, hrefPattern, RegexOptions.IgnoreCase)

            For Each m As Match In matches
                Dim url = m.Groups(1).Value.Trim()

                For Each domain In HostDomains
                    If url.IndexOf(domain, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        If Not seen.Contains(url) Then
                            seen.Add(url)
                            links.Add(New DownloadLink With {
                                .Url = url,
                                .HostName = GetHostDisplayName(domain)
                            })
                        End If
                        Exit For
                    End If
                Next
            Next

            Return links
        End Function

        ''' <summary>
        ''' Parses structured download sections (Game, Update, DLC, Backport).
        ''' </summary>
        Private Function ParseDownloadSections(html As String, allLinks As List(Of DownloadLink)) As List(Of DownloadSection)
            Dim sections As New List(Of DownloadSection)

            ' Pattern: "Game (vX.XX) :" or "Update (vX.XX) :" or "DLC :" or "Backport X.xx :"
            Dim sectionPattern = "(Game|Update|DLC|Backport\s*\d*\.?\w*)\s*(?:\(v([^)]+)\))?\s*:"
            Dim sectionMatches = Regex.Matches(html, sectionPattern, RegexOptions.IgnoreCase)

            If sectionMatches.Count = 0 Then Return sections

            For i = 0 To sectionMatches.Count - 1
                Dim sMatch = sectionMatches(i)
                Dim label = sMatch.Groups(1).Value.Trim()
                Dim version = If(sMatch.Groups(2).Success, sMatch.Groups(2).Value.Trim(), "")

                If Not String.IsNullOrEmpty(version) Then
                    label = $"{label} v{version}"
                End If

                ' Get the HTML segment between this section and the next
                Dim startPos = sMatch.Index
                Dim endPos = If(i < sectionMatches.Count - 1, sectionMatches(i + 1).Index, Math.Min(startPos + 2000, html.Length))
                Dim segment = html.Substring(startPos, endPos - startPos)

                ' Extract links from this segment
                Dim sectionLinks = ExtractDownloadLinks(segment)

                If sectionLinks.Count > 0 Then
                    sections.Add(New DownloadSection With {
                        .Label = label,
                        .Links = sectionLinks
                    })
                End If
            Next

            Return sections
        End Function

        ''' <summary>
        ''' Returns a user-friendly name for a hosting domain.
        ''' </summary>
        Private Shared Function GetHostDisplayName(domain As String) As String
            Select Case domain.ToLower().Replace("www.", "")
                Case "1fichier.com" : Return "1Fichier"
                Case "mediafire.com" : Return "Mediafire"
                Case "gofile.io" : Return "Gofile"
                Case "akirabox.com" : Return "Akirabox"
                Case "vikingfile.com" : Return "Vikingfile"
                Case "rootz.so" : Return "Rootz"
                Case "1cloudfile.com" : Return "1CloudFile"
                Case Else : Return domain
            End Select
        End Function

        ''' <summary>
        ''' Returns the first download link URL for the result (used by copy magnet/download).
        ''' For DLPS results, this is a direct hosting link, not a magnet.
        ''' </summary>
        Public Async Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String) Implements IGameSearchProvider.GetMagnetLinkAsync
            ' If we already have a link stored
            If Not String.IsNullOrEmpty(result.MagnetLink) Then
                Return result.MagnetLink
            End If

            ' Otherwise fetch the game page and get the first download link
            If String.IsNullOrEmpty(result.DetailsUrl) Then Return ""

            Try
                Dim response = Await _httpClient.GetAsync(result.DetailsUrl)
                If Not response.IsSuccessStatusCode Then Return ""

                Dim html = Await response.Content.ReadAsStringAsync()
                Dim links = ExtractDownloadLinks(html)
                If links.Count > 0 Then
                    Return links(0).Url
                End If
            Catch
            End Try

            Return ""
        End Function

        Public Async Function TestConnectionAsync() As Task(Of Boolean) Implements IGameSearchProvider.TestConnectionAsync
            Try
                Dim cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(10))
                Dim response = Await _httpClient.GetAsync(BASE_URL, cts.Token)
                If response.IsSuccessStatusCode Then
                    _status.LastError = ""
                    Return True
                End If
            Catch ex As Exception
                _status.LastError = ex.Message
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Parses the Uploader field ("Host: URL | Host: URL") back into a list of (host, url) pairs.
        ''' </summary>
        Public Shared Function ParseDownloadLinks(uploaderField As String) As List(Of KeyValuePair(Of String, String))
            Dim links As New List(Of KeyValuePair(Of String, String))
            If String.IsNullOrEmpty(uploaderField) Then Return links

            Dim parts = uploaderField.Split({" | "}, StringSplitOptions.RemoveEmptyEntries)
            For Each part In parts
                Dim colonIdx = part.IndexOf(": ")
                If colonIdx > 0 Then
                    Dim hostName = part.Substring(0, colonIdx).Trim()
                    Dim url = part.Substring(colonIdx + 2).Trim()
                    If Not String.IsNullOrEmpty(url) Then
                        links.Add(New KeyValuePair(Of String, String)(hostName, url))
                    End If
                End If
            Next

            Return links
        End Function

        ' ---- Internal classes ----

        Private Class DownloadLink
            Public Property Url As String
            Public Property HostName As String
        End Class

        Private Class DownloadSection
            Public Property Label As String
            Public Property Links As New List(Of DownloadLink)
        End Class
    End Class
End Namespace
