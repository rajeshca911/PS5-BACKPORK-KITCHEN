Imports System.Collections.Specialized.BitVector32
Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
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

                ' -------- Step 1: try plain HTTP fetch --------
                Dim html As String = ""
                Dim cfBlocked As Boolean = True
                Try
                    Dim request As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Get, searchUrl)
                    request.Headers.Add("Referer", BASE_URL & "/")
                    Dim response = Await _httpClient.SendAsync(request, cancellationToken)
                    If response.IsSuccessStatusCode Then
                        html = Await response.Content.ReadAsStringAsync()
                        cfBlocked = html.Contains("Just a moment") OrElse
                                    html.Contains("cf-browser-verification") OrElse
                                    html.Contains("_cf_chl")
                    End If
                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    html = ""
                    cfBlocked = True
                End Try

                ' -------- Step 2: Cloudflare detected → use embedded WebBrowser --------
                ' The built-in WebBrowser/IE engine executes the CF JS challenge automatically.
                ' This mirrors what real browsers do: load the page, let JS run, get clearance.
                If cfBlocked Then
                    _status.LastError = "Cloudflare — trying browser bypass..."
                    Try
                        html = Await FetchWithWebBrowserAsync(searchUrl)
                    Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                        html = ""
                    End Try
                End If

                If String.IsNullOrEmpty(html) OrElse html.Length < 500 Then
                    _status.LastError = If(cfBlocked, "Cloudflare bypass failed — try updating app", "Empty response")
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
                Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                ' Pattern 1: article/post links (WordPress standard)
                Dim patterns As String() = {
                    "<a\s+href=""(https?://dlpsgame\.com/[^""]+/?)""[^>]*>\s*(?:<img[^>]*>)?\s*([^<]{3,}?)\s*</a>",
                    "<h\d[^>]*>\s*<a\s+href=""(https?://dlpsgame\.com/[^""]+/?)""[^>]*>([^<]+)</a>\s*</h\d>",
                    "href=""(https?://dlpsgame\.com/(?!(?:category|tag|author|page|wp-content|feed))[^""]+)"
                }

                For Each pattern In patterns
                    Dim matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                    For Each m As Match In matches
                        Dim url = m.Groups(1).Value.Trim()
                        Dim title = If(m.Groups.Count > 2, WebUtility.HtmlDecode(m.Groups(2).Value.Trim()), "")

                        ' Skip non-game pages
                        If url.Contains("/category/") OrElse url.Contains("/tag/") OrElse
                           url.Contains("/author/") OrElse url.Contains("/page/") OrElse
                           url.Contains("/wp-content/") OrElse url.Contains("/feed/") OrElse
                           url = BASE_URL & "/" OrElse url = BASE_URL Then
                            Continue For
                        End If

                        If seen.Contains(url) Then Continue For
                        seen.Add(url)

                        ' Extract title from URL slug if missing
                        If String.IsNullOrEmpty(title) OrElse title.Length < 3 Then
                            Dim slug = url.TrimEnd("/"c).Split("/"c).Last()
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

                    If results.Count > 0 Then Exit For ' Stop at first working pattern
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
        ''' Uses the embedded WebBrowser (IE/MSHTML) control to load a page and execute its
        ''' JavaScript — including Cloudflare's "Just a moment" challenge.
        ''' The CF JS runs automatically inside MSHTML; once the challenge passes the title
        ''' changes and DocumentCompleted fires with the real page content.
        '''
        ''' Must marshal WebBrowser creation to the UI thread (STA).
        ''' Returns the full HTML after CF clearance, or "" on timeout/error.
        ''' </summary>
        Friend Shared Async Function FetchWithWebBrowserAsync(url As String) As Task(Of String)
            Dim tcs As New TaskCompletionSource(Of String)()

            Dim launchBrowser As Action = Sub()
                Try
                    ' Set IE11 browser emulation for this process so CF JS works properly
                    Try
                        Dim exeName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                        Using key = Registry.CurrentUser.CreateSubKey(
                            "SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION")
                            If key IsNot Nothing Then
                                key.SetValue(exeName, 11001, RegistryValueKind.DWord) ' IE11
                            End If
                        End Using
                    Catch
                        ' Registry write failed — proceed anyway, CF may still work
                    End Try

                    Dim wb As New WebBrowser() With {
                        .ScriptErrorsSuppressed = True,
                        .ScrollBarsEnabled = False,
                        .Size = New Drawing.Size(1280, 800)
                    }

                    ' 22-second timeout — CF challenge usually resolves in 5-10 s
                    Dim timeout As New System.Windows.Forms.Timer() With {.Interval = 22000}

                    Dim cleanedUp As Boolean = False
                    Dim doCleanup As Action = Sub()
                        If cleanedUp Then Return
                        cleanedUp = True
                        Try : timeout.Stop() : timeout.Dispose() : Catch : End Try
                        Try : wb.Stop() : wb.Dispose() : Catch : End Try
                    End Sub

                    AddHandler timeout.Tick, Sub(s, e)
                        ' Timeout reached — return whatever HTML we have
                        Dim html As String = ""
                        Try : html = If(wb.DocumentText, "") : Catch : End Try
                        doCleanup()
                        tcs.TrySetResult(html)
                    End Sub

                    AddHandler wb.DocumentCompleted, Sub(s, e)
                        ' CF challenge page always has "Just a moment" in the title.
                        ' Stay patient until it's gone.
                        Dim title As String = ""
                        Try : title = If(wb.DocumentTitle, "") : Catch : End Try
                        If title.IndexOf("Just a moment", StringComparison.OrdinalIgnoreCase) >= 0 Then
                            Return ' Still on CF challenge — wait for next completion
                        End If

                        ' Got the real page
                        Dim html As String = ""
                        Try : html = If(wb.DocumentText, "") : Catch : End Try
                        doCleanup()
                        tcs.TrySetResult(html)
                    End Sub

                    timeout.Start()
                    wb.Navigate(url)

                Catch ex As Exception
                    tcs.TrySetException(ex)
                End Try
            End Sub

            ' WebBrowser (MSHTML/IE) requires STA — marshal to the UI thread
            Dim mainForm As Form = Nothing
            For Each f As Form In Application.OpenForms
                If f.IsHandleCreated Then
                    mainForm = f
                    Exit For
                End If
            Next

            If mainForm IsNot Nothing Then
                mainForm.Invoke(launchBrowser)   ' synchronous: sets up wb and starts Navigate
            Else
                launchBrowser()  ' already on UI thread (e.g., during startup)
            End If

            ' Await result — the UI message pump continues running while we wait,
            ' so WebBrowser events (DocumentCompleted) fire normally.
            Return Await tcs.Task
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
