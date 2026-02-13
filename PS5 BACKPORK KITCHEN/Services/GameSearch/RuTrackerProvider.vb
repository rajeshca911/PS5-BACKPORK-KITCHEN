Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web

Namespace Services.GameSearch
    ''' <summary>
    ''' RuTracker search provider
    ''' Requires account credentials to search
    ''' </summary>
    Public Class RuTrackerProvider
        Implements IGameSearchProvider

        ' Multiple mirrors for fallback
        Private ReadOnly _mirrors As String() = {
            "https://rutracker.net",
            "https://rutracker.org",
            "https://rutracker.nl"
        }

        Private _currentMirror As String = "https://rutracker.net"

        Private _credentials As ProviderCredentials
        Private _httpClient As HttpClient
        Private _cookieContainer As CookieContainer
        Private _isLoggedIn As Boolean = False
        Private _status As New ProviderStatus()
        Private _windows1251 As Encoding

        Public Sub New()
            ' Register encoding provider for windows-1251 (Russian/Cyrillic)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
            _windows1251 = Encoding.GetEncoding(1251)

            _cookieContainer = New CookieContainer()
            Dim handler As New HttpClientHandler With {
                .CookieContainer = _cookieContainer,
                .AllowAutoRedirect = True,
                .UseCookies = True,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate
            }
            _httpClient = New HttpClient(handler)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7")
            _httpClient.Timeout = TimeSpan.FromSeconds(30)
        End Sub

        Public ReadOnly Property Name As String Implements IGameSearchProvider.Name
            Get
                Return "rutracker"
            End Get
        End Property

        Public ReadOnly Property DisplayName As String Implements IGameSearchProvider.DisplayName
            Get
                Return "RuTracker"
            End Get
        End Property

        Public ReadOnly Property RequiresAuthentication As Boolean Implements IGameSearchProvider.RequiresAuthentication
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsLoggedIn As Boolean Implements IGameSearchProvider.IsLoggedIn
            Get
                Return _isLoggedIn
            End Get
        End Property

        Public ReadOnly Property Status As ProviderStatus Implements IGameSearchProvider.Status
            Get
                Return _status
            End Get
        End Property

        Public Sub SetCredentials(credentials As ProviderCredentials) Implements IGameSearchProvider.SetCredentials
            _credentials = credentials
            _isLoggedIn = False
        End Sub

        Public Async Function LoginAsync() As Task(Of Boolean) Implements IGameSearchProvider.LoginAsync
            If _credentials Is Nothing OrElse Not _credentials.HasCredentials Then
                _status.LastError = "No credentials provided"
                Return False
            End If

            ' Try each mirror until one works
            For Each mirror In _mirrors
                Try
                    _currentMirror = mirror
                    Dim loginUrl = $"{mirror}/forum/login.php"

                    ' Prepare login form data (Russian "Вход" for submit button)
                    Dim formData = New Dictionary(Of String, String) From {
                        {"login_username", _credentials.Username},
                        {"login_password", _credentials.Password},
                        {"login", "Вход"}
                    }

                    Dim content = New FormUrlEncodedContent(formData)
                    Dim response = Await _httpClient.PostAsync(loginUrl, content)
                    Dim html = Await ReadResponseAsync(response)

                    ' Check multiple indicators for successful login:
                    ' 1. Cookie bb_session or bb_data
                    Dim cookies = _cookieContainer.GetCookies(New Uri(mirror))
                    Dim hasBbCookie = False
                    For Each cookie As Cookie In cookies
                        If cookie.Name.StartsWith("bb_") Then
                            hasBbCookie = True
                            Exit For
                        End If
                    Next

                    ' 2. Check HTML for login success indicators
                    Dim htmlIndicatesLogin = html.Contains("logged-in") OrElse
                                            html.Contains("logout") OrElse
                                            html.Contains("profile.php") OrElse
                                            html.Contains("privmsg") OrElse
                                            html.Contains("tracker.php") OrElse
                                            (Not html.Contains("login_username") AndAlso Not html.Contains("Неверный пароль"))

                    ' 3. Check if redirected to main page (success)
                    Dim wasRedirected = response.RequestMessage?.RequestUri?.AbsolutePath <> "/forum/login.php"

                    If hasBbCookie OrElse htmlIndicatesLogin OrElse wasRedirected Then
                        _isLoggedIn = True
                        _status.IsLoggedIn = True
                        _status.LastError = ""
                        Return True
                    End If

                    ' Check for specific error messages
                    If html.Contains("Неверный пароль") OrElse html.Contains("Вы ввели неверный пароль") Then
                        _status.LastError = "Wrong password"
                    ElseIf html.Contains("Пользователь не найден") Then
                        _status.LastError = "User not found"
                    End If

                Catch ex As Exception
                    _status.LastError = $"Mirror {mirror}: {ex.Message}"
                    Continue For
                End Try
            Next

            _status.IsLoggedIn = False
            If String.IsNullOrEmpty(_status.LastError) Then
                _status.LastError = "Login failed on all mirrors - check credentials"
            End If
            _isLoggedIn = False
            Return False
        End Function

        ''' <summary>
        ''' Read HTTP response with proper encoding (windows-1251 for RuTracker)
        ''' </summary>
        Private Async Function ReadResponseAsync(response As HttpResponseMessage) As Task(Of String)
            Try
                Dim bytes = Await response.Content.ReadAsByteArrayAsync()
                ' Try windows-1251 first (RuTracker default), fallback to UTF-8
                Try
                    Return _windows1251.GetString(bytes)
                Catch
                    Return Encoding.UTF8.GetString(bytes)
                End Try
            Catch
                Return ""
            End Try
        End Function

        Public Sub Logout() Implements IGameSearchProvider.Logout
            _isLoggedIn = False
            _status.IsLoggedIn = False
            _cookieContainer = New CookieContainer()
        End Sub

        Public Async Function SearchAsync(query As GameSearchQuery, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult)) Implements IGameSearchProvider.SearchAsync
            Dim results As New List(Of GameSearchResult)

            If Not _isLoggedIn Then
                Dim loginSuccess = Await LoginAsync()
                If Not loginSuccess Then
                    Return results
                End If
            End If

            Try
                ' Build search URL - simple format: tracker.php?nm=searchterm
                Dim searchText = query.SearchText
                If query.Platform = GamePlatform.PS5 Then
                    searchText = $"PS5 {searchText}"
                ElseIf query.Platform = GamePlatform.PS4 Then
                    searchText = $"PS4 {searchText}"
                End If

                Dim searchUrl = $"{_currentMirror}/forum/tracker.php?nm={HttpUtility.UrlEncode(searchText)}"

                Dim response = Await _httpClient.GetAsync(searchUrl, cancellationToken)
                Dim html = Await ReadResponseAsync(response)

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

        Private Function ParseSearchResults(html As String, maxResults As Integer) As List(Of GameSearchResult)
            Dim results As New List(Of GameSearchResult)

            Try
                ' Pattern from qBittorrent plugin: <tr id="trs-tr-\d+".*?</tr>
                Dim rowPattern = "<tr\s+id=""trs-tr-\d+""[^>]*>.*?</tr>"
                Dim rowMatches = Regex.Matches(html, rowPattern, RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                ' Debug: store row count
                _status.LastError = $"Found {rowMatches.Count} rows in HTML"

                For Each rowMatch As Match In rowMatches
                    If results.Count >= maxResults Then Exit For

                    Dim rowHtml = rowMatch.Value
                    Dim result As New GameSearchResult With {
                        .SourceProvider = DisplayName
                    }

                    ' Extract ID from data-topic_id attribute
                    Dim idMatch = Regex.Match(rowHtml, "data-topic_id=""(\d+)""", RegexOptions.IgnoreCase)
                    If Not idMatch.Success Then Continue For

                    Dim topicId = idMatch.Groups(1).Value
                    result.DetailsUrl = $"{_currentMirror}/forum/viewtopic.php?t={topicId}"
                    result.DownloadUrl = $"{_currentMirror}/forum/dl.php?t={topicId}"

                    ' Extract title - try multiple patterns
                    Dim title As String = ""

                    ' Pattern 1: data-topic_id followed by text
                    Dim titleMatch = Regex.Match(rowHtml, "data-topic_id=""\d+""[^>]*>([^<]+)<", RegexOptions.IgnoreCase)
                    If titleMatch.Success AndAlso Not String.IsNullOrWhiteSpace(titleMatch.Groups(1).Value) Then
                        title = titleMatch.Groups(1).Value.Trim()
                    End If

                    ' Pattern 2: viewtopic link text
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "viewtopic\.php\?t=\d+[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase)
                        If titleMatch.Success AndAlso Not String.IsNullOrWhiteSpace(titleMatch.Groups(1).Value) Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    ' Pattern 3: torTopic class link
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "class=""[^""]*torTopic[^""]*""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase)
                        If titleMatch.Success AndAlso Not String.IsNullOrWhiteSpace(titleMatch.Groups(1).Value) Then
                            title = titleMatch.Groups(1).Value.Trim()
                        End If
                    End If

                    ' Pattern 4: Any link containing viewtopic
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "<a[^>]*href=""[^""]*viewtopic[^""]*""[^>]*>(.+?)</a>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                        If titleMatch.Success Then
                            ' Remove inner HTML tags
                            title = Regex.Replace(titleMatch.Groups(1).Value, "<[^>]+>", "").Trim()
                        End If
                    End If

                    ' Pattern 5: t-title class
                    If String.IsNullOrEmpty(title) Then
                        titleMatch = Regex.Match(rowHtml, "class=""[^""]*t-title[^""]*""[^>]*>(.+?)</a>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                        If titleMatch.Success Then
                            title = Regex.Replace(titleMatch.Groups(1).Value, "<[^>]+>", "").Trim()
                        End If
                    End If

                    ' Skip if no title found
                    If String.IsNullOrEmpty(title) Then
                        Continue For
                    End If

                    result.Title = WebUtility.HtmlDecode(title)

                    ' Extract data-ts_text values (size, seeds, pub_date in order)
                    Dim tsTextMatches = Regex.Matches(rowHtml, "data-ts_text=""([-\d]+)""", RegexOptions.IgnoreCase)
                    If tsTextMatches.Count >= 1 Then
                        ' First is size in bytes
                        Dim sizeBytes As Long
                        If Long.TryParse(tsTextMatches(0).Groups(1).Value, sizeBytes) Then
                            result.SizeBytes = sizeBytes
                            result.Size = FormatSize(sizeBytes)
                        End If
                    End If
                    If tsTextMatches.Count >= 2 Then
                        ' Second is seeds
                        Integer.TryParse(tsTextMatches(1).Groups(1).Value, result.Seeds)
                    End If

                    ' Extract leeches
                    Dim leechMatch = Regex.Match(rowHtml, "leechmed[^>]*>(\d+)<", RegexOptions.IgnoreCase)
                    If leechMatch.Success Then
                        Integer.TryParse(leechMatch.Groups(1).Value, result.Leeches)
                    End If

                    ' Detect platform from title
                    If result.Title.ToUpper().Contains("PS5") Then
                        result.Platform = "PS5"
                    ElseIf result.Title.ToUpper().Contains("PS4") Then
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

        Private Function FormatSize(bytes As Long) As String
            If bytes >= 1099511627776 Then
                Return $"{bytes / 1099511627776.0:F2} TB"
            ElseIf bytes >= 1073741824 Then
                Return $"{bytes / 1073741824.0:F2} GB"
            ElseIf bytes >= 1048576 Then
                Return $"{bytes / 1048576.0:F2} MB"
            ElseIf bytes >= 1024 Then
                Return $"{bytes / 1024.0:F2} KB"
            Else
                Return $"{bytes} B"
            End If
        End Function

        Public Async Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String) Implements IGameSearchProvider.GetMagnetLinkAsync
            If String.IsNullOrEmpty(result.DetailsUrl) Then Return ""

            Try
                Dim response = Await _httpClient.GetAsync(result.DetailsUrl)
                Dim html = Await ReadResponseAsync(response)

                ' Look for magnet link
                Dim magnetMatch = Regex.Match(html, "magnet:\?xt=urn:[^""'<>\s]+", RegexOptions.IgnoreCase)
                If magnetMatch.Success Then
                    Return magnetMatch.Value
                End If

            Catch ex As Exception
                ' Ignore
            End Try

            Return ""
        End Function

        Public Async Function TestConnectionAsync() As Task(Of Boolean) Implements IGameSearchProvider.TestConnectionAsync
            For Each mirror In _mirrors
                Try
                    Dim response = Await _httpClient.GetAsync(mirror)
                    If response.IsSuccessStatusCode Then
                        _currentMirror = mirror
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
