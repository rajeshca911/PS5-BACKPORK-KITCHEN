Imports System.Collections.Specialized.BitVector32
Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.Web.WebView2.WinForms
Imports Microsoft.Win32

Namespace Services.GameSearch

    ''' <summary>
    ''' DLPSGame.com search provider for PS5/PS4 game downloads.
    ''' Scrapes game listings and extracts direct download links from
    ''' multiple hosting services (1fichier, Mediafire, Gofile, etc.).
    ''' </summary>
    Public Class HostLink
        Public Property Host As String
        Public Property Url As String
    End Class
    Public Class DLPSGameProvider
        Implements IGameSearchProvider


        Private Const BASE_URL As String = "https://dlpsgame.com"

        ' Known hosting domains for download link extraction
        Private Shared ReadOnly HostDomains As String() = {
            "1fichier.com", "mediafire.com", "www.mediafire.com",
            "gofile.io", "akirabox.com", "vikingfile.com",
            "rootz.so", "www.rootz.so", "1cloudfile.com",
            "buzzheavier.com", "datanodes.to", "filecrypt.cc",
            "pixeldrain.com", "cyberfile.is", "uploadhaven.com",
            "fikper.com", "rapidgator.net", "nitroflare.com",
            "turbobit.net", "katfile.com", "ddownload.com"
        }

        Private _httpClient As HttpClient
        Private _status As New ProviderStatus() With {.IsEnabled = True}

        Public Sub New()
            Dim handler As New HttpClientHandler With {
                .AllowAutoRedirect = True,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate,
                .CookieContainer = New Net.CookieContainer(),
                .UseCookies = True
            }
            _httpClient = New HttpClient(handler)
            ' Chrome 131 fingerprint headers — required to bypass Cloudflare bot detection
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36")
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7")
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br")
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua",
                """Google Chrome"";v=""131"", ""Chromium"";v=""131"", ""Not_A Brand"";v=""24""")
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0")
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", """Windows""")
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "document")
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate")
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "none")
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-user", "?1")
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1")
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0")
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive")
            _httpClient.Timeout = TimeSpan.FromSeconds(45)
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

                Debug.WriteLine($"[DLPS] Search URL: {searchUrl}")

                ' -------- Step 1: try plain HTTP fetch --------
                Dim html As String = ""
                Dim cfBlocked As Boolean = True
                Try
                    Dim request As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Get, searchUrl)
                    request.Headers.Add("Referer", BASE_URL & "/")
                    Dim response = Await _httpClient.SendAsync(request, cancellationToken)
                    Debug.WriteLine($"[DLPS] HTTP status: {response.StatusCode}")
                    If response.IsSuccessStatusCode Then
                        html = Await response.Content.ReadAsStringAsync()
                        cfBlocked = html.Contains("Just a moment") OrElse
                                    html.Contains("cf-browser-verification") OrElse
                                    html.Contains("_cf_chl")
                        Debug.WriteLine($"[DLPS] HTML length: {html.Length}, CF blocked: {cfBlocked}")
                    End If
                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    Debug.WriteLine($"[DLPS] HTTP error: {ex.Message}")
                    html = ""
                    cfBlocked = True
                End Try

                ' -------- Step 2: parse HTML if HTTP succeeded --------
                Dim listingResults As New List(Of GameSearchResult)

                If Not cfBlocked AndAlso Not String.IsNullOrEmpty(html) AndAlso html.Length >= 500 Then
                    listingResults = ParseListingPage(html)
                    Debug.WriteLine($"[DLPS] ParseListingPage returned {listingResults.Count} results")
                End If

                ' -------- Step 3: CF blocked → use Python DrissionPage bypass --------
                ' DrissionPage uses a real browser engine with anti-detection to solve
                ' Cloudflare Turnstile challenges that WebView2 cannot handle.
                If listingResults.Count = 0 AndAlso cfBlocked Then
                    _status.LastError = "Cloudflare — trying browser bypass..."
                    Debug.WriteLine("[DLPS] CF blocked — launching Python browser bypass...")
                    Try
                        listingResults = Await RunPythonSearchAsync(searchTerm, query.MaxResults, cancellationToken)
                        Debug.WriteLine($"[DLPS] Python search returned {listingResults.Count} results")
                    Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                        Debug.WriteLine($"[DLPS] Python search error: {ex.Message}")
                    End Try
                End If

                ' -------- Step 3b: fallback to WebView2 if Python not available --------
                If listingResults.Count = 0 AndAlso cfBlocked Then
                    Debug.WriteLine("[DLPS] Python returned 0 — trying WebView2 fallback...")
                    Try
                        listingResults = Await ExtractLinksViaJsAsync(searchUrl)
                        Debug.WriteLine($"[DLPS] WebView2 returned {listingResults.Count} results")
                    Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                        Debug.WriteLine($"[DLPS] WebView2 error: {ex.Message}")
                    End Try
                End If

                If listingResults.Count = 0 Then
                    _status.LastError = If(cfBlocked, "Cloudflare bypass failed or no results", "No results found")
                    Debug.WriteLine($"[DLPS] FINAL: 0 results — {_status.LastError}")
                    Return results
                End If

                Debug.WriteLine($"[DLPS] Found {listingResults.Count} listing results")

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
        ''' Handles both absolute and relative URLs, single/double quotes.
        ''' </summary>
        Private Function ParseListingPage(html As String) As List(Of GameSearchResult)
            Dim results As New List(Of GameSearchResult)

            Try
                Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                ' Non-game URL segments to skip
                Dim skipSegments = {"/category/", "/tag/", "/author/", "/page/",
                                    "/wp-content/", "/feed/", "/wp-json/", "/wp-login/",
                                    "/wp-admin/", "/wp-includes/", "/comments/", "#"}

                ' Patterns handle both absolute (https://dlpsgame.com/...) and relative (/...) URLs
                ' and both double quotes and single quotes
                Dim patterns As String() = {
                    "<h\d[^>]*>\s*<a\s+[^>]*href=[""']((?:https?://(?:www\.)?dlpsgame\.com)?/[^""']+)[""'][^>]*>\s*([^<]+?)\s*</a>",
                    "<a\s+[^>]*href=[""']((?:https?://(?:www\.)?dlpsgame\.com)?/[^""']+)[""'][^>]*>\s*(?:<img[^>]*>)?\s*([^<]{3,}?)\s*</a>",
                    "href=[""'](https?://(?:www\.)?dlpsgame\.com/[^""']+)[""']",
                    "href=[""'](/[a-z0-9][\w-]*(?:-ps[45])?[\w-]*/?\??[^""']*)[""']"
                }

                For Each pattern In patterns
                    Dim matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                    For Each m As Match In matches
                        Dim rawUrl = m.Groups(1).Value.Trim()

                        ' Normalize relative → absolute
                        Dim url = rawUrl
                        If url.StartsWith("/") AndAlso Not url.StartsWith("//") Then
                            url = BASE_URL & url
                        End If

                        ' Must be a dlpsgame URL
                        If Not url.StartsWith("https://dlpsgame.com", StringComparison.OrdinalIgnoreCase) AndAlso
                           Not url.StartsWith("https://www.dlpsgame.com", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        End If

                        ' Skip non-game pages
                        Dim skip = False
                        For Each seg In skipSegments
                            If url.IndexOf(seg, StringComparison.OrdinalIgnoreCase) >= 0 Then
                                skip = True
                                Exit For
                            End If
                        Next
                        If skip Then Continue For

                        ' Skip root URL
                        Dim normalized = url.Replace("://www.", "://").TrimEnd("/"c)
                        If normalized = "https://dlpsgame.com" Then Continue For

                        If seen.Contains(normalized) Then Continue For
                        seen.Add(normalized)

                        Dim title = If(m.Groups.Count > 2, WebUtility.HtmlDecode(m.Groups(2).Value.Trim()), "")

                        ' Extract title from URL slug if missing
                        If String.IsNullOrEmpty(title) OrElse title.Length < 3 Then
                            Dim slug = url.TrimEnd("/"c).Split("/"c).Last()
                            ' Remove query string
                            Dim qIdx = slug.IndexOf("?"c)
                            If qIdx >= 0 Then slug = slug.Substring(0, qIdx)
                            title = slug.Replace("-", " ").Replace("_", " ")
                            title = Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title)
                        End If

                        If title.Length < 3 Then Continue For

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

                    If results.Count > 0 Then Exit For
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

            ' Extract region/PKG ID (multiple formats)
            Dim pkgIdMatch = Regex.Match(html,
                "((?:PPSA|CUSA)\d{5})\s*[-–]?\s*(USA|EUR|JPN|ASIA|JP|EU|US)?",
                RegexOptions.IgnoreCase)
            Dim region = If(pkgIdMatch.Success AndAlso pkgIdMatch.Groups(2).Success,
                           pkgIdMatch.Groups(2).Value.ToUpper(), listing.Region)
            Dim pkgId = If(pkgIdMatch.Success, pkgIdMatch.Groups(1).Value, "")

            ' Extract firmware requirement (multiple formats)
            Dim fwMatch = Regex.Match(html,
                "(?:Works\s+on|Firmware|FW|Requires?)[^0-9]*(\d+\.\d+)",
                RegexOptions.IgnoreCase)
            Dim firmware = If(fwMatch.Success, fwMatch.Groups(1).Value, "")

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
                .Uploader = If(section.Links.Count > 0,
                    String.Join(" | ", section.Links.Select(Function(l) $"{l.HostName}: {l.Url}")),
                    "")
    }

                    ' ✅ ADD THIS — YOU MISSED IT
                    For Each l In section.Links
                        result.DownloadLinks.Add(New HostLink With {
            .Host = l.HostName,
            .Url = l.Url
        })
                    Next

                    If section.Links.Count > 0 Then
                        result.MagnetLink = section.Links(0).Url
                    End If

                    results.Add(result)

                Next


                '' keep first link as primary
                'If allLinks.Count > 0 Then
                '    result.MagnetLink = allLinks(0).Url
                'End If

                'results.Add(result)

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
                ' Try plain HTTP first
                Dim cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(15))
                Dim req As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Get, BASE_URL)
                req.Headers.Add("Referer", BASE_URL & "/")
                Dim response = Await _httpClient.SendAsync(req, cts.Token)
                If response.IsSuccessStatusCode Then
                    Dim body = Await response.Content.ReadAsStringAsync()
                    If Not (body.Contains("Just a moment") OrElse body.Contains("_cf_chl")) Then
                        _status.LastError = ""
                        Return True
                    End If
                End If

                ' HTTP blocked by CF — try browser bypass on the homepage
                Dim browserHtml = Await FetchWithWebBrowserAsync(BASE_URL)
                If Not String.IsNullOrEmpty(browserHtml) AndAlso browserHtml.Length > 500 AndAlso
                   Not browserHtml.Contains("Just a moment") Then
                    _status.LastError = ""
                    Return True
                End If

                _status.LastError = "Cloudflare — browser bypass also failed"
            Catch ex As Exception
                _status.LastError = ex.Message
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Uses WebView2 (Chromium/Edge engine) to load a page and execute its JavaScript,
        ''' including Cloudflare's "Just a moment" managed challenge.
        ''' Returns the full HTML after CF clearance, or "" on timeout/error.
        ''' </summary>
        Friend Shared Async Function FetchWithWebBrowserAsync(url As String) As Task(Of String)
            Dim tcs As New TaskCompletionSource(Of String)()

            Dim doWork As Action = Sub()
                Try
                    Dim frm As New Form() With {
                        .Width = 1, .Height = 1,
                        .ShowInTaskbar = False, .Opacity = 0,
                        .FormBorderStyle = FormBorderStyle.None,
                        .StartPosition = FormStartPosition.Manual,
                        .Location = New Drawing.Point(-10000, -10000)
                    }

                    Dim wv As New WebView2() With {.Dock = DockStyle.Fill}
                    frm.Controls.Add(wv)

                    Dim cleanedUp As Boolean = False
                    Dim doCleanup As Action = Sub()
                        If cleanedUp Then Return
                        cleanedUp = True
                        Try : frm.Close() : frm.Dispose() : Catch : End Try
                    End Sub

                    Dim pollTimer As New Timer() With {.Interval = 2000}
                    Dim pollCount As Integer = 0
                    Dim cfClearedAt As Integer = -1
                    Dim navigationDone As Boolean = False

                    AddHandler pollTimer.Tick, Async Sub(s, e)
                        pollCount += 1
                        pollTimer.Stop()

                        Try
                            If wv.CoreWebView2 Is Nothing Then
                                Debug.WriteLine($"[DLPS-WV] Poll #{pollCount}: CoreWebView2 is null")
                                If pollCount >= 20 Then
                                    doCleanup()
                                    tcs.TrySetResult("")
                                    Return
                                End If
                                If Not cleanedUp Then pollTimer.Start()
                                Return
                            End If

                            Dim title = If(wv.CoreWebView2.DocumentTitle, "")
                            Debug.WriteLine($"[DLPS-WV] Poll #{pollCount}: title='{title}' navDone={navigationDone}")

                            Dim cfDone = navigationDone AndAlso
                                         Not title.Contains("Just a moment") AndAlso
                                         pollCount > 2

                            If cfDone AndAlso cfClearedAt < 0 Then
                                cfClearedAt = pollCount
                                Debug.WriteLine($"[DLPS-WV] CF cleared at poll #{pollCount}")
                            End If

                            Dim contentReady = (cfClearedAt > 0 AndAlso pollCount >= cfClearedAt + 2)

                            If contentReady OrElse pollCount >= 20 Then
                                Dim jsResult = Await wv.CoreWebView2.ExecuteScriptAsync(
                                    "document.documentElement.outerHTML")

                                Dim html As String = ""
                                If jsResult IsNot Nothing AndAlso jsResult <> "null" Then
                                    html = Newtonsoft.Json.JsonConvert.DeserializeObject(Of String)(jsResult)
                                End If

                                Debug.WriteLine($"[DLPS-WV] Extracted HTML length: {If(html IsNot Nothing, html.Length, 0)}")
                                doCleanup()
                                tcs.TrySetResult(If(html, ""))
                                Return
                            End If
                        Catch ex As Exception
                            Debug.WriteLine($"[DLPS-WV] Poll error: {ex.Message}")
                            If pollCount >= 20 Then
                                doCleanup()
                                tcs.TrySetResult("")
                                Return
                            End If
                        End Try

                        If Not cleanedUp Then pollTimer.Start()
                    End Sub

                    AddHandler frm.Shown, Async Sub(s, e)
                        Try
                            Debug.WriteLine("[DLPS-WV] Initializing WebView2...")
                            Await wv.EnsureCoreWebView2Async()
                            Debug.WriteLine("[DLPS-WV] WebView2 initialized OK")

                            AddHandler wv.NavigationCompleted, Sub(s2, e2)
                                navigationDone = True
                                Debug.WriteLine($"[DLPS-WV] Navigation completed: success={e2.IsSuccess}, status={e2.HttpStatusCode}")
                            End Sub

                            wv.CoreWebView2.Navigate(url)
                            Debug.WriteLine($"[DLPS-WV] Navigating to {url}")
                            pollTimer.Start()

                        Catch ex As Exception
                            Debug.WriteLine($"[DLPS-WV] Init error: {ex.Message}")
                            doCleanup()
                            tcs.TrySetResult("")
                        End Try
                    End Sub

                    frm.Show()

                Catch ex As Exception
                    Debug.WriteLine($"[DLPS-WV] Form error: {ex.Message}")
                    tcs.TrySetResult("")
                End Try
            End Sub

            ' WebView2 must run on the UI thread
            Dim mainForm As Form = Nothing
            For Each f As Form In Application.OpenForms
                If f.IsHandleCreated Then mainForm = f : Exit For
            Next

            If mainForm IsNot Nothing AndAlso mainForm.InvokeRequired Then
                mainForm.BeginInvoke(doWork)
            Else
                doWork()
            End If

            Return Await tcs.Task
        End Function

        ''' <summary>
        ''' Uses WebView2 to navigate to the URL, bypass CF, then extract
        ''' game links directly from the DOM via JavaScript queries.
        ''' </summary>
        Private Shared Async Function ExtractLinksViaJsAsync(url As String) As Task(Of List(Of GameSearchResult))
            Dim tcs As New TaskCompletionSource(Of List(Of GameSearchResult))()
            Dim emptyResult As New List(Of GameSearchResult)

            Dim doWork As Action = Sub()
                Try
                    Dim frm As New Form() With {
                        .Width = 1, .Height = 1,
                        .ShowInTaskbar = False, .Opacity = 0,
                        .FormBorderStyle = FormBorderStyle.None,
                        .StartPosition = FormStartPosition.Manual,
                        .Location = New Drawing.Point(-10000, -10000)
                    }

                    Dim wv As New WebView2() With {.Dock = DockStyle.Fill}
                    frm.Controls.Add(wv)

                    Dim cleanedUp As Boolean = False
                    Dim doCleanup As Action = Sub()
                        If cleanedUp Then Return
                        cleanedUp = True
                        Try : frm.Close() : frm.Dispose() : Catch : End Try
                    End Sub

                    Dim pollTimer As New Timer() With {.Interval = 2500}
                    Dim pollCount As Integer = 0
                    Dim cfClearedAt As Integer = -1
                    Dim navigationDone As Boolean = False

                    AddHandler pollTimer.Tick, Async Sub(s, e)
                        pollCount += 1
                        pollTimer.Stop()

                        Try
                            If wv.CoreWebView2 Is Nothing Then
                                Debug.WriteLine($"[DLPS-JS] Poll #{pollCount}: CoreWebView2 is null")
                                If pollCount >= 20 Then
                                    doCleanup()
                                    tcs.TrySetResult(emptyResult)
                                    Return
                                End If
                                If Not cleanedUp Then pollTimer.Start()
                                Return
                            End If

                            Dim title = If(wv.CoreWebView2.DocumentTitle, "")
                            Debug.WriteLine($"[DLPS-JS] Poll #{pollCount}: title='{title}' navDone={navigationDone}")

                            Dim cfDone = navigationDone AndAlso
                                         Not title.Contains("Just a moment") AndAlso
                                         pollCount > 2

                            If cfDone AndAlso cfClearedAt < 0 Then
                                cfClearedAt = pollCount
                                Debug.WriteLine($"[DLPS-JS] CF cleared at poll #{pollCount}")
                            End If

                            Dim contentReady = (cfClearedAt > 0 AndAlso pollCount >= cfClearedAt + 2)

                            ' Timeout: max ~50 seconds (20 polls × 2.5s)
                            If pollCount >= 20 AndAlso Not cfDone Then
                                Debug.WriteLine("[DLPS-JS] Timeout — CF never cleared")
                                doCleanup()
                                tcs.TrySetResult(emptyResult)
                                Return
                            End If

                            If contentReady OrElse (cfDone AndAlso pollCount >= 20) Then
                                ' Extract game links from DOM
                                Dim js = "(function(){" &
                                         "var skip=['/category/','/tag/','/author/','/page/','/wp-content/','/feed/','/wp-json/','/wp-login/','/wp-admin/','#','javascript:'];" &
                                         "var base='dlpsgame.com';" &
                                         "var root='https://dlpsgame.com/';" &
                                         "var seen={}; var res=[];" &
                                         "var links=document.querySelectorAll('a[href]');" &
                                         "for(var i=0;i<links.length;i++){" &
                                         "  var a=links[i]; var h=a.href||'';" &
                                         "  if(!h.includes(base))continue;" &
                                         "  if(h===root||h==='https://dlpsgame.com'||h==='https://www.dlpsgame.com/'||h==='https://www.dlpsgame.com')continue;" &
                                         "  var bad=false;" &
                                         "  for(var j=0;j<skip.length;j++){if(h.includes(skip[j])){bad=true;break;}}" &
                                         "  if(bad)continue;" &
                                         "  var norm=h.replace('://www.','://').replace(/\/$/,'');" &
                                         "  if(seen[norm])continue; seen[norm]=1;" &
                                         "  var t=(a.textContent||'').trim().replace(/\s+/g,' ').substring(0,200);" &
                                         "  if(t.length<3){var parts=norm.split('/');t=parts[parts.length-1].replace(/-/g,' ');}" &
                                         "  res.push({u:h,t:t});" &
                                         "}" &
                                         "return JSON.stringify(res);" &
                                         "})()"

                                Dim jsResult = Await wv.CoreWebView2.ExecuteScriptAsync(js)
                                Debug.WriteLine($"[DLPS-JS] JS result length: {If(jsResult IsNot Nothing, jsResult.Length, 0)}")

                                Dim extracted As New List(Of GameSearchResult)
                                If jsResult IsNot Nothing AndAlso jsResult <> "null" AndAlso jsResult <> """""" AndAlso jsResult <> "[]" Then
                                    ' ExecuteScriptAsync returns a JSON-encoded string — unwrap first
                                    Dim jsonStr = Newtonsoft.Json.JsonConvert.DeserializeObject(Of String)(jsResult)
                                    Debug.WriteLine($"[DLPS-JS] Unwrapped JSON length: {If(jsonStr IsNot Nothing, jsonStr.Length, 0)}")

                                    If Not String.IsNullOrEmpty(jsonStr) AndAlso jsonStr <> "[]" Then
                                        Dim items = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of JsLink))(jsonStr)
                                        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                                        If items IsNot Nothing Then
                                            Debug.WriteLine($"[DLPS-JS] Parsed {items.Count} link items")
                                            For Each entry In items
                                                If String.IsNullOrEmpty(entry.u) Then Continue For
                                                If seen.Contains(entry.u) Then Continue For
                                                seen.Add(entry.u)

                                                Dim gameTitle = If(Not String.IsNullOrEmpty(entry.t) AndAlso entry.t.Length >= 3,
                                                                   entry.t,
                                                                   entry.u.TrimEnd("/"c).Split("/"c).Last().Replace("-", " "))
                                                gameTitle = Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(gameTitle)

                                                Dim plat = ""
                                                If gameTitle.ToUpper().Contains("PS5") OrElse entry.u.Contains("-ps5") Then plat = "PS5"
                                                If gameTitle.ToUpper().Contains("PS4") OrElse entry.u.Contains("-ps4") Then plat = "PS4"

                                                extracted.Add(New GameSearchResult With {
                                                    .Title = gameTitle,
                                                    .DetailsUrl = entry.u,
                                                    .SourceProvider = "DLPSGame",
                                                    .Platform = plat,
                                                    .Category = "Game"
                                                })
                                            Next
                                        End If
                                    End If
                                End If

                                Debug.WriteLine($"[DLPS-JS] Extracted {extracted.Count} game results")
                                doCleanup()
                                tcs.TrySetResult(extracted)
                                Return
                            End If
                        Catch ex As Exception
                            Debug.WriteLine($"[DLPS-JS] Poll error: {ex.Message}")
                            If pollCount >= 20 Then
                                doCleanup()
                                tcs.TrySetResult(emptyResult)
                                Return
                            End If
                        End Try

                        If Not cleanedUp Then pollTimer.Start()
                    End Sub

                    AddHandler frm.Shown, Async Sub(s, e)
                        Try
                            Debug.WriteLine("[DLPS-JS] Initializing WebView2...")
                            Await wv.EnsureCoreWebView2Async()
                            Debug.WriteLine("[DLPS-JS] WebView2 initialized OK")

                            AddHandler wv.NavigationCompleted, Sub(s2, e2)
                                navigationDone = True
                                Debug.WriteLine($"[DLPS-JS] Navigation completed: success={e2.IsSuccess}, status={e2.HttpStatusCode}")
                            End Sub

                            wv.CoreWebView2.Navigate(url)
                            Debug.WriteLine($"[DLPS-JS] Navigating to {url}")
                            pollTimer.Start()

                        Catch ex As Exception
                            Debug.WriteLine($"[DLPS-JS] Init error: {ex.Message}")
                            doCleanup()
                            tcs.TrySetResult(emptyResult)
                        End Try
                    End Sub

                    frm.Show()
                Catch ex As Exception
                    Debug.WriteLine($"[DLPS-JS] Form error: {ex.Message}")
                    tcs.TrySetResult(emptyResult)
                End Try
            End Sub

            Dim mainForm As Form = Nothing
            For Each f As Form In Application.OpenForms
                If f.IsHandleCreated Then mainForm = f : Exit For
            Next

            If mainForm IsNot Nothing AndAlso mainForm.InvokeRequired Then
                mainForm.BeginInvoke(doWork)
            Else
                doWork()
            End If

            Return Await tcs.Task
        End Function

        ''' <summary>
        ''' Runs the Python dlps_search.py script which uses DrissionPage (anti-detection
        ''' browser automation) to bypass Cloudflare Turnstile and scrape search results.
        ''' Returns parsed game listings from JSON output.
        ''' </summary>
        Private Shared Async Function RunPythonSearchAsync(
            searchTerm As String,
            maxResults As Integer,
            ct As Threading.CancellationToken
        ) As Task(Of List(Of GameSearchResult))

            Dim results As New List(Of GameSearchResult)

            ' Find the Python script
            Dim scriptPath = FindScriptPath("dlps_search.py")
            If scriptPath Is Nothing Then
                Debug.WriteLine("[DLPS-PY] dlps_search.py not found in scripts/ folder")
                Return results
            End If

            ' Find Python interpreter
            Dim pythonPath = PythonRunner.FindPython()
            If String.IsNullOrEmpty(pythonPath) Then
                Debug.WriteLine("[DLPS-PY] Python not found")
                Return results
            End If

            ' Build args
            Dim escapedQuery = searchTerm.Replace("""", "\""")
            Dim arguments = $"""{scriptPath}"" --query ""{escapedQuery}"" --max {maxResults}"

            Debug.WriteLine($"[DLPS-PY] Running: {pythonPath} {arguments}")

            ' Capture all stdout into a single string
            Dim outputBuilder As New Text.StringBuilder()
            Dim errorBuilder As New Text.StringBuilder()

            Try
                Dim exitCode = Await PythonRunner.RunAsync(
                    scriptPath, $"--query ""{escapedQuery}"" --max {maxResults}",
                    onOutput:=Sub(line) outputBuilder.AppendLine(line),
                    onError:=Sub(line)
                                errorBuilder.AppendLine(line)
                                Debug.WriteLine($"[DLPS-PY] stderr: {line}")
                            End Sub,
                    ct:=ct)

                Debug.WriteLine($"[DLPS-PY] Exit code: {exitCode}, output length: {outputBuilder.Length}")

                If exitCode <> 0 Then
                    Debug.WriteLine($"[DLPS-PY] Script failed: {errorBuilder}")
                    Return results
                End If

                ' Parse JSON output
                Dim jsonOutput = outputBuilder.ToString().Trim()
                If String.IsNullOrEmpty(jsonOutput) Then Return results

                ' Find the JSON object (skip any non-JSON lines from stderr/warnings)
                Dim jsonStart = jsonOutput.IndexOf("{"c)
                If jsonStart < 0 Then Return results
                jsonOutput = jsonOutput.Substring(jsonStart)

                Dim parsed = Newtonsoft.Json.Linq.JObject.Parse(jsonOutput)

                ' Check for error
                Dim errToken = parsed("error")
                If errToken IsNot Nothing AndAlso errToken.Type <> Newtonsoft.Json.Linq.JTokenType.Null Then
                    Debug.WriteLine($"[DLPS-PY] Script error: {CStr(errToken)}")
                End If

                ' Parse results array
                Dim items = parsed("results")
                If items Is Nothing Then Return results

                For Each item As Newtonsoft.Json.Linq.JObject In items
                    Dim title = CStr(If(item("title"), ""))
                    Dim url = CStr(If(item("url"), ""))
                    If String.IsNullOrEmpty(title) OrElse String.IsNullOrEmpty(url) Then Continue For

                    results.Add(New GameSearchResult With {
                        .Title = title,
                        .DetailsUrl = url,
                        .SourceProvider = "DLPSGame",
                        .Platform = CStr(If(item("platform"), "")),
                        .Category = CStr(If(item("category"), "Game"))
                    })
                Next

                Debug.WriteLine($"[DLPS-PY] Parsed {results.Count} results from JSON")

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                Debug.WriteLine($"[DLPS-PY] Error: {ex.Message}")
            End Try

            Return results
        End Function

        ''' <summary>
        ''' Finds a script file in the scripts/ folder relative to the application.
        ''' </summary>
        Private Shared Function FindScriptPath(scriptName As String) As String
            Dim dir = AppDomain.CurrentDomain.BaseDirectory
            For i = 0 To 8
                Dim candidate = IO.Path.Combine(dir, "scripts", scriptName)
                If IO.File.Exists(candidate) Then Return IO.Path.GetFullPath(candidate)
                Dim parent = IO.Directory.GetParent(dir)
                If parent Is Nothing Then Exit For
                dir = parent.FullName
            Next
            Return Nothing
        End Function

        Private Class JsLink
            Public Property u As String
            Public Property t As String
        End Class

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
