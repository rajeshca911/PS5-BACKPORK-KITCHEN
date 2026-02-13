Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions

Namespace Services.GameSearch
    ''' <summary>
    ''' Download progress information reported during file transfers.
    ''' </summary>
    Public Class DownloadProgressInfo
        Public Property FileName As String = ""
        Public Property TotalBytes As Long
        Public Property DownloadedBytes As Long
        Public Property SpeedBytesPerSec As Double
        Public Property PercentComplete As Integer
        Public Property Status As String = ""
    End Class

    ''' <summary>
    ''' Multi-host download service that resolves hosting URLs to direct
    ''' download links and streams files to disk. Supports Mediafire,
    ''' VikingFile, Gofile, 1fichier, Akirabox, and Rootz.
    ''' </summary>
    Public Class DirectDownloadService
        Private ReadOnly _httpClient As HttpClient
        Private ReadOnly _noRedirectClient As HttpClient
        Private ReadOnly _cookieContainer As CookieContainer

        Private Const BROWSER_UA As String =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"

        Public Sub New()
            _cookieContainer = New CookieContainer()

            ' Standard client with auto-redirect (most hosts)
            Dim handler As New HttpClientHandler With {
                .AllowAutoRedirect = True,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate,
                .CookieContainer = _cookieContainer,
                .UseCookies = True
            }
            _httpClient = New HttpClient(handler)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", BROWSER_UA)
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")
            _httpClient.Timeout = TimeSpan.FromMinutes(30)

            ' No-redirect client for hosts that need manual redirect handling (1fichier)
            Dim noRedirectHandler As New HttpClientHandler With {
                .AllowAutoRedirect = False,
                .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate,
                .CookieContainer = _cookieContainer,
                .UseCookies = True
            }
            _noRedirectClient = New HttpClient(noRedirectHandler)
            _noRedirectClient.DefaultRequestHeaders.Add("User-Agent", BROWSER_UA)
            _noRedirectClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            _noRedirectClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")
            _noRedirectClient.Timeout = TimeSpan.FromMinutes(30)
        End Sub

        ''' <summary>
        ''' Downloads a file from the given hosting URL to the output folder.
        ''' Routes to the correct host handler based on the domain.
        ''' </summary>
        Public Async Function DownloadFileAsync(hostUrl As String, outputFolder As String,
                                                 progress As IProgress(Of DownloadProgressInfo),
                                                 cancellationToken As Threading.CancellationToken) As Task(Of String)

            Dim uri As New Uri(hostUrl)
            Dim host = uri.Host.ToLower().Replace("www.", "")

            Select Case True
                Case host.Contains("mediafire.com")
                    Return Await DownloadMediafireAsync(hostUrl, outputFolder, progress, cancellationToken)
                Case host.Contains("gofile.io")
                    Return Await DownloadGofileAsync(hostUrl, outputFolder, progress, cancellationToken)
                Case host.Contains("vikingfile.com")
                    Return Await DownloadVikingFileAsync(hostUrl, outputFolder, progress, cancellationToken)
                Case host.Contains("1fichier.com")
                    Return Await Download1FichierAsync(hostUrl, outputFolder, progress, cancellationToken)
                Case host.Contains("akirabox.com")
                    Return Await DownloadAkiraboxAsync(hostUrl, outputFolder, progress, cancellationToken)
                Case host.Contains("rootz.so")
                    Return Await DownloadRootzAsync(hostUrl, outputFolder, progress, cancellationToken)
                Case Else
                    ' Try a generic direct download
                    Return Await DownloadGenericAsync(hostUrl, outputFolder, progress, cancellationToken)
            End Select
        End Function

        ''' <summary>
        ''' Returns the host display name for a URL.
        ''' </summary>
        Public Shared Function GetHostName(url As String) As String
            Try
                Dim uri As New Uri(url)
                Dim host = uri.Host.ToLower().Replace("www.", "")
                Select Case True
                    Case host.Contains("mediafire.com") : Return "Mediafire"
                    Case host.Contains("gofile.io") : Return "Gofile"
                    Case host.Contains("vikingfile.com") : Return "VikingFile"
                    Case host.Contains("1fichier.com") : Return "1Fichier"
                    Case host.Contains("akirabox.com") : Return "Akirabox"
                    Case host.Contains("rootz.so") : Return "Rootz"
                    Case host.Contains("1cloudfile.com") : Return "1CloudFile"
                    Case Else : Return host
                End Select
            Catch
                Return "Unknown"
            End Try
        End Function

#Region "Mediafire Handler"
        ''' <summary>
        ''' Downloads from Mediafire by parsing the download page for the direct link.
        ''' </summary>
        Private Async Function DownloadMediafireAsync(url As String, outputFolder As String,
                                                       progress As IProgress(Of DownloadProgressInfo),
                                                       ct As Threading.CancellationToken) As Task(Of String)
            progress?.Report(New DownloadProgressInfo With {.Status = "Resolving Mediafire link...", .FileName = ""})

            ' Fetch the Mediafire page
            Dim pageHtml = Await _httpClient.GetStringAsync(url)

            ' Parse direct download link from aria-label="Download file"
            Dim dlMatch = Regex.Match(pageHtml, "<a[^>]*aria-label=""Download file""[^>]*href=""([^""]+)""", RegexOptions.IgnoreCase)
            If Not dlMatch.Success Then
                ' Alternative pattern
                dlMatch = Regex.Match(pageHtml, "href=""(https?://download\d*\.mediafire\.com/[^""]+)""", RegexOptions.IgnoreCase)
            End If

            If Not dlMatch.Success Then
                Throw New Exception("Could not find Mediafire download link. The file may have been removed.")
            End If

            Dim directUrl = WebUtility.HtmlDecode(dlMatch.Groups(1).Value)

            ' Extract filename from the URL
            Dim fileName = ExtractFileNameFromUrl(directUrl)
            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Mediafire...", .FileName = fileName})

            Return Await StreamDownloadAsync(directUrl, outputFolder, fileName, progress, ct)
        End Function
#End Region

#Region "Gofile Handler"
        ''' <summary>
        ''' Downloads from Gofile using the guest account API flow.
        ''' </summary>
        Private Async Function DownloadGofileAsync(url As String, outputFolder As String,
                                                    progress As IProgress(Of DownloadProgressInfo),
                                                    ct As Threading.CancellationToken) As Task(Of String)
            progress?.Report(New DownloadProgressInfo With {.Status = "Connecting to Gofile API...", .FileName = ""})

            ' Step 1: Create guest account to get token
            Dim accountResponse = Await _httpClient.GetStringAsync("https://api.gofile.io/accounts")
            Dim tokenMatch = Regex.Match(accountResponse, """token""\s*:\s*""([^""]+)""")
            If Not tokenMatch.Success Then
                Throw New Exception("Could not get Gofile guest token.")
            End If
            Dim token = tokenMatch.Groups(1).Value

            ' Step 2: Get websiteToken from JS
            Dim websiteToken = ""
            Try
                Dim jsContent = Await _httpClient.GetStringAsync("https://gofile.io/dist/js/alljs.js")
                Dim wtMatch = Regex.Match(jsContent, "websiteToken\s*[=:]\s*""([^""]+)""")
                If wtMatch.Success Then
                    websiteToken = wtMatch.Groups(1).Value
                End If
            Catch
                ' Use fallback
            End Try

            ' If we couldn't get websiteToken, try a known fallback
            If String.IsNullOrEmpty(websiteToken) Then
                websiteToken = "4fd6sg89d7s6"
            End If

            ' Step 3: Extract content ID from URL
            Dim contentId = url.TrimEnd("/"c).Split("/"c).Last()

            ' Step 4: Get content info
            Dim contentUrl = $"https://api.gofile.io/contents/{contentId}?wt={websiteToken}"
            Dim request As New HttpRequestMessage(HttpMethod.Get, contentUrl)
            request.Headers.Add("Authorization", $"Bearer {token}")
            Dim contentResponse = Await _httpClient.SendAsync(request, ct)
            Dim contentJson = Await contentResponse.Content.ReadAsStringAsync()

            ' Parse file download link from the JSON response
            Dim linkMatch = Regex.Match(contentJson, """link""\s*:\s*""([^""]+)""")
            If Not linkMatch.Success Then
                Throw New Exception("Could not find download link in Gofile response.")
            End If
            Dim directUrl = linkMatch.Groups(1).Value.Replace("\/", "/")

            ' Extract filename
            Dim nameMatch = Regex.Match(contentJson, """name""\s*:\s*""([^""]+)""")
            Dim fileName = If(nameMatch.Success, nameMatch.Groups(1).Value, ExtractFileNameFromUrl(directUrl))

            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Gofile...", .FileName = fileName})

            ' Step 5: Download with cookie
            Dim dlRequest As New HttpRequestMessage(HttpMethod.Get, directUrl)
            dlRequest.Headers.Add("Cookie", $"accountToken={token}")

            Dim response = Await _httpClient.SendAsync(dlRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            response.EnsureSuccessStatusCode()

            Dim outputPath = GetSafeOutputPath(outputFolder, fileName)
            Await StreamResponseToFileAsync(response, outputPath, progress, ct)
            Return outputPath
        End Function
#End Region

#Region "VikingFile Handler"
        ''' <summary>
        ''' Downloads from VikingFile using the API get-server endpoint.
        ''' VikingFile landing pages use Cloudflare Turnstile CAPTCHA which blocks
        ''' automated HTTP requests, so we use the API to resolve a download URL.
        ''' Falls back to browser if all automated methods fail.
        ''' </summary>
        Private Async Function DownloadVikingFileAsync(url As String, outputFolder As String,
                                                        progress As IProgress(Of DownloadProgressInfo),
                                                        ct As Threading.CancellationToken) As Task(Of String)
            progress?.Report(New DownloadProgressInfo With {.Status = "Resolving VikingFile link...", .FileName = ""})

            ' Extract the file hash from the URL (e.g., vikingfile.com/f/HASH)
            Dim fileHash = ""
            Dim uri As New Uri(url)
            Dim segments = uri.AbsolutePath.Trim("/"c).Split("/"c)
            If segments.Length >= 2 AndAlso segments(0).ToLower() = "f" Then
                fileHash = segments(1)
            ElseIf segments.Length >= 1 Then
                fileHash = segments(segments.Length - 1)
            End If

            If String.IsNullOrEmpty(fileHash) Then
                Throw New Exception("Could not extract file hash from VikingFile URL.")
            End If

            Dim fileName = ""
            Dim downloadUrl = ""

            ' Strategy 1: Try the API get-server endpoint
            Try
                Dim apiContent As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
                    {"hash", fileHash}
                })
                Dim apiRequest As New HttpRequestMessage(HttpMethod.Post, "https://vikingfile.com/api/get-server")
                apiRequest.Content = apiContent
                apiRequest.Headers.Add("Origin", "https://vikingfile.com")
                apiRequest.Headers.Referrer = New Uri("https://vikingfile.com/")

                Dim apiResponse = Await _httpClient.SendAsync(apiRequest, ct)
                If apiResponse.IsSuccessStatusCode Then
                    Dim apiJson = Await apiResponse.Content.ReadAsStringAsync()

                    ' Parse URL from response
                    Dim urlMatch = Regex.Match(apiJson, """url""\s*:\s*""([^""]+)""")
                    If urlMatch.Success Then
                        downloadUrl = urlMatch.Groups(1).Value.Replace("\/", "/")
                    End If

                    ' Parse server from response
                    If String.IsNullOrEmpty(downloadUrl) Then
                        Dim serverMatch = Regex.Match(apiJson, """server""\s*:\s*""([^""]+)""")
                        If serverMatch.Success Then
                            Dim server = serverMatch.Groups(1).Value.Replace("\/", "/")
                            downloadUrl = $"{server}/f/{fileHash}"
                        End If
                    End If

                    ' Parse file name from response
                    Dim nameMatch = Regex.Match(apiJson, """name""\s*:\s*""([^""]+)""")
                    If nameMatch.Success Then
                        fileName = nameMatch.Groups(1).Value
                    End If
                End If
            Catch
                ' API call failed, try next strategy
            End Try

            ' Strategy 2: Try check-file API for metadata, then construct download URL
            If String.IsNullOrEmpty(downloadUrl) Then
                Try
                    Dim checkContent As New StringContent(
                        $"{{""hash"":""{fileHash}""}}",
                        Text.Encoding.UTF8, "application/json")
                    Dim checkRequest As New HttpRequestMessage(HttpMethod.Post, "https://vikingfile.com/api/check-file")
                    checkRequest.Content = checkContent
                    checkRequest.Headers.Add("Origin", "https://vikingfile.com")

                    Dim checkResponse = Await _httpClient.SendAsync(checkRequest, ct)
                    If checkResponse.IsSuccessStatusCode Then
                        Dim checkJson = Await checkResponse.Content.ReadAsStringAsync()
                        Dim nameMatch = Regex.Match(checkJson, """name""\s*:\s*""([^""]+)""")
                        If nameMatch.Success Then
                            fileName = nameMatch.Groups(1).Value
                        End If
                    End If
                Catch
                End Try
            End If

            ' Strategy 3: Try direct GET on vik1ngfile.site (alternate CDN domain) with no-redirect
            If String.IsNullOrEmpty(downloadUrl) Then
                Try
                    Dim cdnUrl = $"https://vik1ngfile.site/f/{fileHash}"
                    Dim cdnRequest As New HttpRequestMessage(HttpMethod.Get, cdnUrl)
                    cdnRequest.Headers.Referrer = New Uri("https://vikingfile.com/")
                    Dim cdnResponse = Await _noRedirectClient.SendAsync(cdnRequest, ct)

                    If CInt(cdnResponse.StatusCode) >= 300 AndAlso CInt(cdnResponse.StatusCode) < 400 Then
                        Dim location = cdnResponse.Headers.Location?.ToString()
                        If Not String.IsNullOrEmpty(location) Then
                            If Not location.StartsWith("http") Then
                                location = New Uri(New Uri(cdnUrl), location).ToString()
                            End If
                            ' If it redirects to another CDN/download URL, use it
                            If Not location.Contains("turnstile") AndAlso Not location.Contains("challenge") Then
                                downloadUrl = location
                            End If
                        End If
                    ElseIf cdnResponse.IsSuccessStatusCode Then
                        ' Check if the response is the actual file (not HTML)
                        Dim contentType = cdnResponse.Content.Headers.ContentType?.MediaType
                        If contentType IsNot Nothing AndAlso Not contentType.Contains("text/html") Then
                            ' It's the actual file!
                            If String.IsNullOrEmpty(fileName) Then fileName = ExtractFileNameFromUrl(url)
                            Dim headerFn = ExtractFileNameFromResponse(cdnResponse)
                            If Not String.IsNullOrEmpty(headerFn) Then fileName = headerFn

                            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from VikingFile...", .FileName = fileName})
                            Dim outputPath = GetSafeOutputPath(outputFolder, fileName)
                            Await StreamResponseToFileAsync(cdnResponse, outputPath, progress, ct)
                            Return outputPath
                        End If

                        ' HTML page - try to parse a download link from it
                        Dim html = Await cdnResponse.Content.ReadAsStringAsync()
                        Dim dlMatch = Regex.Match(html, "href=""([^""]+)""\s*[^>]*(?:download|btn-download)", RegexOptions.IgnoreCase)
                        If dlMatch.Success Then
                            downloadUrl = WebUtility.HtmlDecode(dlMatch.Groups(1).Value)
                            If Not downloadUrl.StartsWith("http") Then
                                downloadUrl = New Uri(New Uri(cdnUrl), downloadUrl).ToString()
                            End If
                        End If
                    End If
                Catch
                End Try
            End If

            ' If we still have no download URL, Cloudflare CAPTCHA is blocking us
            If String.IsNullOrEmpty(downloadUrl) Then
                Throw New Exception(
                    "VikingFile uses Cloudflare Turnstile CAPTCHA which blocks automated downloads. " &
                    "Please use 'Open Details Page' to download manually in your browser.")
            End If

            ' Download the resolved URL
            If String.IsNullOrEmpty(fileName) Then fileName = ExtractFileNameFromUrl(downloadUrl)
            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from VikingFile...", .FileName = fileName})

            Dim dlReq As New HttpRequestMessage(HttpMethod.Get, downloadUrl)
            dlReq.Headers.Referrer = New Uri("https://vikingfile.com/")
            Dim dlResp = Await _httpClient.SendAsync(dlReq, HttpCompletionOption.ResponseHeadersRead, ct)
            dlResp.EnsureSuccessStatusCode()

            ValidateDownloadResponse(dlResp)

            Dim finalName = ExtractFileNameFromResponse(dlResp)
            If Not String.IsNullOrEmpty(finalName) Then fileName = finalName

            Dim outPath = GetSafeOutputPath(outputFolder, fileName)
            Await StreamResponseToFileAsync(dlResp, outPath, progress, ct)
            Return outPath
        End Function
#End Region

#Region "1fichier Handler"
        ''' <summary>
        ''' Downloads from 1fichier using manual redirect handling.
        ''' Uses _noRedirectClient to capture 302 Location headers that contain
        ''' the direct CDN download URL. Multiple POST strategies are attempted.
        ''' </summary>
        Private Async Function Download1FichierAsync(url As String, outputFolder As String,
                                                      progress As IProgress(Of DownloadProgressInfo),
                                                      ct As Threading.CancellationToken) As Task(Of String)
            progress?.Report(New DownloadProgressInfo With {.Status = "Resolving 1fichier link...", .FileName = ""})

            ' Force English page via cookie
            _cookieContainer.Add(New Uri("https://1fichier.com"), New Cookie("LG", "en"))

            ' Step 1: GET the page with NO auto-redirect to check for immediate 302
            Dim getRequest As New HttpRequestMessage(HttpMethod.Get, url)
            getRequest.Headers.Referrer = New Uri("https://1fichier.com/")
            Dim pageResponse = Await _noRedirectClient.SendAsync(getRequest, ct)

            ' If 302 → Location header IS the direct download link (hotlink)
            If CInt(pageResponse.StatusCode) >= 300 AndAlso CInt(pageResponse.StatusCode) < 400 Then
                Dim locationUrl = pageResponse.Headers.Location?.ToString()
                If Not String.IsNullOrEmpty(locationUrl) Then
                    If Not locationUrl.StartsWith("http") Then
                        locationUrl = New Uri(New Uri(url), locationUrl).ToString()
                    End If
                    Dim fn = ExtractFileNameFromUrl(locationUrl)
                    progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from 1fichier...", .FileName = fn})
                    Return Await StreamDownloadAsync(locationUrl, outputFolder, fn, progress, ct)
                End If
            End If

            ' Got 200 - read the HTML page
            Dim pageHtml = Await pageResponse.Content.ReadAsStringAsync()

            ' Check for errors
            If pageHtml.Contains("file could not be found") OrElse pageHtml.Contains("has been removed") Then
                Throw New Exception("File not found on 1fichier. It may have been deleted.")
            End If

            ' Check for wait timer
            Dim waitMatch = Regex.Match(pageHtml, "(?:must wait|devez attendre)\D*(\d+)\s*min", RegexOptions.IgnoreCase)
            If waitMatch.Success Then
                Throw New Exception($"1fichier rate limit: please wait {waitMatch.Groups(1).Value} minutes before downloading.")
            End If

            ' Check for JS countdown timer
            Dim countdownMatch = Regex.Match(pageHtml, "var\s+count\s*=\s*(\d+)", RegexOptions.IgnoreCase)
            Dim waitSeconds = If(countdownMatch.Success, Math.Min(Integer.Parse(countdownMatch.Groups(1).Value) + 1, 90), 3)

            ' Extract hidden adz field if present
            Dim adzValue = ""
            Dim adzMatch = Regex.Match(pageHtml, "name=""adz""\s+value=""([^""]*?)""", RegexOptions.IgnoreCase)
            If adzMatch.Success Then
                adzValue = adzMatch.Groups(1).Value
            End If

            ' Wait for countdown
            progress?.Report(New DownloadProgressInfo With {.Status = $"Waiting {waitSeconds}s for 1fichier...", .FileName = ""})
            Await Task.Delay(waitSeconds * 1000, ct)

            ' Step 2: Try multiple POST strategies to get the download link
            Dim directUrl = ""

            ' Strategy A: POST with dl_no_ssl=on (most common free download approach)
            directUrl = Await Try1FichierPost(url, New Dictionary(Of String, String) From {
                {"dl_no_ssl", "on"}, {"dlinline", "on"}
            }, ct)

            ' Strategy B: POST with adz field if found
            If String.IsNullOrEmpty(directUrl) AndAlso Not String.IsNullOrEmpty(adzValue) Then
                directUrl = Await Try1FichierPost(url, New Dictionary(Of String, String) From {
                    {"submit", "Download"}, {"adz", adzValue}
                }, ct)
            End If

            ' Strategy C: POST with minimal body
            If String.IsNullOrEmpty(directUrl) Then
                directUrl = Await Try1FichierPost(url, New Dictionary(Of String, String) From {
                    {"a", "1"}
                }, ct)
            End If

            ' Strategy D: POST empty body (JDownloader approach)
            If String.IsNullOrEmpty(directUrl) Then
                directUrl = Await Try1FichierPost(url, New Dictionary(Of String, String)(), ct)
            End If

            If String.IsNullOrEmpty(directUrl) Then
                Throw New Exception(
                    "Could not resolve 1fichier download link after trying all strategies. " &
                    "The file may require a premium account or the site may have changed.")
            End If

            Dim fileName = ExtractFileNameFromUrl(directUrl)
            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from 1fichier...", .FileName = fileName})

            ' Step 3: Download from the resolved direct URL with Referer
            Dim dlRequest As New HttpRequestMessage(HttpMethod.Get, directUrl)
            dlRequest.Headers.Referrer = New Uri(url)
            Dim dlResponse = Await _httpClient.SendAsync(dlRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            dlResponse.EnsureSuccessStatusCode()

            ' Validate we're getting a real file, not HTML
            ValidateDownloadResponse(dlResponse)

            Dim headerFileName = ExtractFileNameFromResponse(dlResponse)
            If Not String.IsNullOrEmpty(headerFileName) Then fileName = headerFileName

            Dim outputPath = GetSafeOutputPath(outputFolder, fileName)
            Await StreamResponseToFileAsync(dlResponse, outputPath, progress, ct)
            Return outputPath
        End Function

        ''' <summary>
        ''' Attempts a single 1fichier POST strategy and returns the direct URL or empty string.
        ''' Uses _noRedirectClient to capture 302 Location headers.
        ''' </summary>
        Private Async Function Try1FichierPost(url As String, formFields As Dictionary(Of String, String),
                                                ct As Threading.CancellationToken) As Task(Of String)
            Try
                Dim postRequest As New HttpRequestMessage(HttpMethod.Post, url)
                postRequest.Headers.Referrer = New Uri(url)
                postRequest.Content = New FormUrlEncodedContent(formFields)

                Dim postResponse = Await _noRedirectClient.SendAsync(postRequest, ct)

                ' Check for 302 redirect - Location header IS the direct download URL
                If CInt(postResponse.StatusCode) >= 300 AndAlso CInt(postResponse.StatusCode) < 400 Then
                    Dim locationUrl = postResponse.Headers.Location?.ToString()
                    If Not String.IsNullOrEmpty(locationUrl) Then
                        If Not locationUrl.StartsWith("http") Then
                            locationUrl = New Uri(New Uri(url), locationUrl).ToString()
                        End If
                        ' Verify it looks like a CDN/download URL, not a page redirect
                        If locationUrl.Contains("cdn") OrElse locationUrl.Contains("1fichier.com/") OrElse
                           Not locationUrl.Contains("?") Then
                            Return locationUrl
                        End If
                    End If
                End If

                ' Check response body for download link (HTML with <a class="ok"> or similar)
                Dim responseHtml = Await postResponse.Content.ReadAsStringAsync()

                ' Pattern 1: <a class="ok btn-general btn-orange" href="...">
                Dim dlMatch = Regex.Match(responseHtml, "<a[^>]*class=""[^""]*ok[^""]*""[^>]*href=""([^""]+)""", RegexOptions.IgnoreCase)
                If dlMatch.Success Then
                    Return WebUtility.HtmlDecode(dlMatch.Groups(1).Value)
                End If

                ' Pattern 2: Click here to download
                dlMatch = Regex.Match(responseHtml, "<a\s+href=""([^""]+)""[^>]*>\s*Click here to download", RegexOptions.IgnoreCase)
                If dlMatch.Success Then
                    Return WebUtility.HtmlDecode(dlMatch.Groups(1).Value)
                End If

                ' Pattern 3: Any CDN-like 1fichier URL
                dlMatch = Regex.Match(responseHtml, """(https?://\w+\.1fichier\.com/[^""]+)""", RegexOptions.IgnoreCase)
                If dlMatch.Success Then
                    Dim candidate = WebUtility.HtmlDecode(dlMatch.Groups(1).Value)
                    ' Skip if it's just the same page URL
                    If Not candidate.Contains("?") Then
                        Return candidate
                    End If
                End If

                ' Pattern 4: window.location redirect in JavaScript
                dlMatch = Regex.Match(responseHtml, "window\.location\s*=\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase)
                If dlMatch.Success Then
                    Return WebUtility.HtmlDecode(dlMatch.Groups(1).Value)
                End If

            Catch
                ' Strategy failed, return empty to try next
            End Try

            Return ""
        End Function
#End Region

#Region "Akirabox Handler"
        ''' <summary>
        ''' Downloads from Akirabox using the Laravel CSRF token flow.
        ''' Akirabox is behind Cloudflare and uses a multi-step download process:
        ''' 1) GET file page → parse CSRF token + cookies
        ''' 2) Wait for countdown timer
        ''' 3) POST with CSRF token → get signed download URL
        ''' 4) Stream download from signed URL
        ''' </summary>
        Private Async Function DownloadAkiraboxAsync(url As String, outputFolder As String,
                                                      progress As IProgress(Of DownloadProgressInfo),
                                                      ct As Threading.CancellationToken) As Task(Of String)
            progress?.Report(New DownloadProgressInfo With {.Status = "Connecting to Akirabox...", .FileName = ""})

            ' Normalize URL - ensure it ends with /file if it's a file code
            Dim fileUrl = url.TrimEnd("/"c)
            If Not fileUrl.EndsWith("/file", StringComparison.OrdinalIgnoreCase) Then
                ' Extract file code from URL patterns like akirabox.com/XXXXX or akirabox.com/XXXXX/filename
                Dim uri As New Uri(fileUrl)
                Dim segments = uri.AbsolutePath.Trim("/"c).Split("/"c)
                If segments.Length >= 1 AndAlso Not String.IsNullOrEmpty(segments(0)) Then
                    fileUrl = $"https://akirabox.com/{segments(0)}/file"
                End If
            End If

            ' Step 1: GET the file page with proper Referer to capture session cookies
            Dim request As New HttpRequestMessage(HttpMethod.Get, fileUrl)
            request.Headers.Referrer = New Uri("https://akirabox.com/")
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            request.Headers.Add("Sec-Fetch-Dest", "document")
            request.Headers.Add("Sec-Fetch-Mode", "navigate")
            request.Headers.Add("Sec-Fetch-Site", "same-origin")

            Dim pageResponse = Await _httpClient.SendAsync(request, ct)
            If Not pageResponse.IsSuccessStatusCode Then
                ' Cloudflare may be blocking - fall back to browser
                Throw New Exception(
                    $"Akirabox returned {CInt(pageResponse.StatusCode)}. " &
                    "The site uses Cloudflare protection which blocks automated downloads. " &
                    "Please use 'Open Details Page' to download manually in your browser.")
            End If

            Dim pageHtml = Await pageResponse.Content.ReadAsStringAsync()

            ' Step 2: Parse CSRF token from meta tag or hidden input
            Dim csrfToken = ""
            Dim csrfMeta = Regex.Match(pageHtml, "<meta\s+name=""csrf-token""\s+content=""([^""]+)""", RegexOptions.IgnoreCase)
            If csrfMeta.Success Then
                csrfToken = csrfMeta.Groups(1).Value
            Else
                Dim csrfInput = Regex.Match(pageHtml, "<input[^>]*name=""_token""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase)
                If csrfInput.Success Then
                    csrfToken = csrfInput.Groups(1).Value
                End If
            End If

            ' Also extract the XSRF-TOKEN from cookies
            Dim xsrfToken = ""
            Dim cookies = _cookieContainer.GetCookies(New Uri("https://akirabox.com/"))
            For Each cookie As Cookie In cookies
                If cookie.Name = "XSRF-TOKEN" Then
                    xsrfToken = WebUtility.UrlDecode(cookie.Value)
                    Exit For
                End If
            Next

            ' Check if we already have a signed download link on the page
            Dim signedMatch = Regex.Match(pageHtml, "href=""(https?://akirabox\.(?:com|to)/download/[^""]+)""", RegexOptions.IgnoreCase)
            If signedMatch.Success Then
                Dim signedUrl = WebUtility.HtmlDecode(signedMatch.Groups(1).Value)
                Dim fileName = ExtractFileNameFromUrl(signedUrl)
                progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Akirabox...", .FileName = fileName})
                Return Await StreamDownloadAsync(signedUrl, outputFolder, fileName, progress, ct)
            End If

            ' Step 3: Wait for countdown timer (typically 5-10 seconds)
            progress?.Report(New DownloadProgressInfo With {.Status = "Waiting for Akirabox countdown...", .FileName = ""})
            Dim waitMatch = Regex.Match(pageHtml, "(?:countdown|timer|wait)\D*(\d+)", RegexOptions.IgnoreCase)
            Dim waitSeconds = If(waitMatch.Success, Math.Min(Integer.Parse(waitMatch.Groups(1).Value), 15), 6)
            Await Task.Delay(waitSeconds * 1000, ct)

            ' Step 4: POST the download form with CSRF token
            Dim formAction = ""
            Dim formMatch = Regex.Match(pageHtml, "<form[^>]*action=""([^""]+)""[^>]*method=""post""", RegexOptions.IgnoreCase)
            If formMatch.Success Then
                formAction = WebUtility.HtmlDecode(formMatch.Groups(1).Value)
                If Not formAction.StartsWith("http") Then
                    formAction = $"https://akirabox.com{formAction}"
                End If
            Else
                formAction = fileUrl
            End If

            Dim postRequest As New HttpRequestMessage(HttpMethod.Post, formAction)
            postRequest.Headers.Referrer = New Uri(fileUrl)
            postRequest.Headers.Add("Sec-Fetch-Dest", "document")
            postRequest.Headers.Add("Sec-Fetch-Mode", "navigate")
            postRequest.Headers.Add("Sec-Fetch-Site", "same-origin")
            If Not String.IsNullOrEmpty(xsrfToken) Then
                postRequest.Headers.Add("X-XSRF-TOKEN", xsrfToken)
            End If

            Dim formData As New Dictionary(Of String, String)
            If Not String.IsNullOrEmpty(csrfToken) Then
                formData.Add("_token", csrfToken)
            End If
            postRequest.Content = New FormUrlEncodedContent(formData)

            Dim postResponse = Await _httpClient.SendAsync(postRequest, ct)
            Dim postHtml = Await postResponse.Content.ReadAsStringAsync()

            ' Step 5: Look for the signed download URL in the response
            signedMatch = Regex.Match(postHtml, "href=""(https?://akirabox\.(?:com|to)/download/[^""]+)""", RegexOptions.IgnoreCase)
            If Not signedMatch.Success Then
                ' Try looking for any download link
                signedMatch = Regex.Match(postHtml, """(https?://akirabox\.(?:com|to)/download/[^""]+)""", RegexOptions.IgnoreCase)
            End If
            If Not signedMatch.Success Then
                ' Check if the response itself is the file (redirect to download)
                If postResponse.Content.Headers.ContentType?.MediaType IsNot Nothing AndAlso
                   Not postResponse.Content.Headers.ContentType.MediaType.Contains("text/html") Then
                    Dim fileName2 = ExtractFileNameFromResponse(postResponse)
                    If String.IsNullOrEmpty(fileName2) Then fileName2 = ExtractFileNameFromUrl(url)
                    progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Akirabox...", .FileName = fileName2})
                    Dim outputPath2 = GetSafeOutputPath(outputFolder, fileName2)
                    Await StreamResponseToFileAsync(postResponse, outputPath2, progress, ct)
                    Return outputPath2
                End If

                Throw New Exception(
                    "Could not resolve Akirabox download link. " &
                    "The site may require browser interaction. " &
                    "Please use 'Open Details Page' to download manually.")
            End If

            Dim directUrl = WebUtility.HtmlDecode(signedMatch.Groups(1).Value)
            Dim dlFileName = ExtractFileNameFromUrl(directUrl)
            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Akirabox...", .FileName = dlFileName})

            ' Step 6: Download from signed URL with proper headers
            Dim dlRequest As New HttpRequestMessage(HttpMethod.Get, directUrl)
            dlRequest.Headers.Referrer = New Uri(fileUrl)
            Dim dlResponse = Await _httpClient.SendAsync(dlRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            dlResponse.EnsureSuccessStatusCode()

            ' Check filename from Content-Disposition
            Dim headerFileName = ExtractFileNameFromResponse(dlResponse)
            If Not String.IsNullOrEmpty(headerFileName) Then
                dlFileName = headerFileName
            End If

            Dim outputPath = GetSafeOutputPath(outputFolder, dlFileName)
            Await StreamResponseToFileAsync(dlResponse, outputPath, progress, ct)
            Return outputPath
        End Function
#End Region

#Region "Rootz Handler"
        ''' <summary>
        ''' Downloads from Rootz by parsing the download page with proper headers.
        ''' </summary>
        Private Async Function DownloadRootzAsync(url As String, outputFolder As String,
                                                    progress As IProgress(Of DownloadProgressInfo),
                                                    ct As Threading.CancellationToken) As Task(Of String)
            progress?.Report(New DownloadProgressInfo With {.Status = "Resolving Rootz link...", .FileName = ""})

            ' Fetch the page with proper Referer
            Dim request As New HttpRequestMessage(HttpMethod.Get, url)
            request.Headers.Referrer = New Uri("https://rootz.so/")

            Dim pageResponse = Await _httpClient.SendAsync(request, ct)
            If Not pageResponse.IsSuccessStatusCode Then
                Throw New Exception(
                    $"Rootz returned {CInt(pageResponse.StatusCode)}. " &
                    "Please use 'Open Details Page' to download manually in your browser.")
            End If
            Dim pageHtml = Await pageResponse.Content.ReadAsStringAsync()

            ' Look for direct download link
            Dim dlMatch = Regex.Match(pageHtml, "href=""(https?://[^""]+\.(?:pkg|zip|rar|7z|bin|iso)[^""]*)""", RegexOptions.IgnoreCase)
            If Not dlMatch.Success Then
                ' Try download button
                dlMatch = Regex.Match(pageHtml, "<a[^>]*(?:class=""[^""]*download[^""]*""|id=""[^""]*download[^""]*"")[^>]*href=""([^""]+)""", RegexOptions.IgnoreCase)
            End If
            If Not dlMatch.Success Then
                ' Try form-based download with token
                dlMatch = Regex.Match(pageHtml, "<form[^>]*action=""([^""]+)""[^>]*>.*?<input[^>]*name=""token""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                If dlMatch.Success Then
                    Dim formUrl = dlMatch.Groups(1).Value
                    If Not formUrl.StartsWith("http") Then
                        Dim baseUri As New Uri(url)
                        formUrl = New Uri(baseUri, formUrl).ToString()
                    End If
                    Dim formContent As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
                        {"token", dlMatch.Groups(2).Value}
                    })
                    Dim postRequest As New HttpRequestMessage(HttpMethod.Post, formUrl)
                    postRequest.Content = formContent
                    postRequest.Headers.Referrer = New Uri(url)
                    Dim postResponse = Await _httpClient.SendAsync(postRequest, ct)
                    If postResponse.IsSuccessStatusCode Then
                        Dim fileName2 = ExtractFileNameFromResponse(postResponse)
                        If String.IsNullOrEmpty(fileName2) Then fileName2 = ExtractFileNameFromUrl(url)
                        progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Rootz...", .FileName = fileName2})
                        Dim outputPath2 = GetSafeOutputPath(outputFolder, fileName2)
                        Await StreamResponseToFileAsync(postResponse, outputPath2, progress, ct)
                        Return outputPath2
                    End If
                End If
                Throw New Exception(
                    "Could not find Rootz download link. " &
                    "Please use 'Open Details Page' to download manually in your browser.")
            End If

            Dim directUrl = WebUtility.HtmlDecode(dlMatch.Groups(1).Value)
            If Not directUrl.StartsWith("http") Then
                Dim baseUri As New Uri(url)
                directUrl = New Uri(baseUri, directUrl).ToString()
            End If

            Dim fileName = ExtractFileNameFromUrl(directUrl)
            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading from Rootz...", .FileName = fileName})

            Return Await StreamDownloadAsync(directUrl, outputFolder, fileName, progress, ct)
        End Function
#End Region

#Region "Generic Handler"
        ''' <summary>
        ''' Attempts a generic direct download for unsupported hosts.
        ''' </summary>
        Private Async Function DownloadGenericAsync(url As String, outputFolder As String,
                                                     progress As IProgress(Of DownloadProgressInfo),
                                                     ct As Threading.CancellationToken) As Task(Of String)
            Dim fileName = ExtractFileNameFromUrl(url)
            progress?.Report(New DownloadProgressInfo With {.Status = "Downloading...", .FileName = fileName})

            Return Await StreamDownloadAsync(url, outputFolder, fileName, progress, ct)
        End Function
#End Region

#Region "Streaming Helpers"
        ''' <summary>
        ''' Downloads a URL to a file with progress reporting using streaming.
        ''' </summary>
        Private Async Function StreamDownloadAsync(url As String, outputFolder As String,
                                                    fileName As String,
                                                    progress As IProgress(Of DownloadProgressInfo),
                                                    ct As Threading.CancellationToken,
                                                    Optional referer As String = Nothing) As Task(Of String)

            Dim request As New HttpRequestMessage(HttpMethod.Get, url)
            If Not String.IsNullOrEmpty(referer) Then
                request.Headers.Referrer = New Uri(referer)
            End If

            Dim response = Await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            response.EnsureSuccessStatusCode()

            ' Validate we're getting a real file, not HTML/favicon
            ValidateDownloadResponse(response)

            ' Try to get filename from Content-Disposition header
            Dim headerFileName = ExtractFileNameFromResponse(response)
            If Not String.IsNullOrEmpty(headerFileName) Then
                fileName = headerFileName
            End If

            If String.IsNullOrEmpty(fileName) OrElse fileName = "download" Then
                fileName = $"download_{DateTime.Now:yyyyMMdd_HHmmss}"
            End If

            Dim outputPath = GetSafeOutputPath(outputFolder, fileName)
            Await StreamResponseToFileAsync(response, outputPath, progress, ct)
            Return outputPath
        End Function

        ''' <summary>
        ''' Streams an HTTP response to a file with progress reporting.
        ''' </summary>
        Private Async Function StreamResponseToFileAsync(response As HttpResponseMessage, outputPath As String,
                                                          progress As IProgress(Of DownloadProgressInfo),
                                                          ct As Threading.CancellationToken) As Task
            Dim totalBytes = response.Content.Headers.ContentLength
            Dim downloaded As Long = 0
            Dim lastProgressTime = DateTime.Now
            Dim lastBytes As Long = 0
            Dim fileName = Path.GetFileName(outputPath)

            Using stream = Await response.Content.ReadAsStreamAsync()
                Using fileStream As New FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, True)
                    Dim buffer(65535) As Byte
                    Dim bytesRead As Integer

                    Do
                        bytesRead = Await stream.ReadAsync(buffer, 0, buffer.Length, ct)
                        If bytesRead = 0 Then Exit Do

                        Await fileStream.WriteAsync(buffer, 0, bytesRead, ct)
                        downloaded += bytesRead

                        ' Report progress every 250ms
                        Dim now = DateTime.Now
                        If (now - lastProgressTime).TotalMilliseconds >= 250 Then
                            Dim elapsed = (now - lastProgressTime).TotalSeconds
                            Dim speed = If(elapsed > 0, (downloaded - lastBytes) / elapsed, 0)
                            Dim percent = If(totalBytes.HasValue AndAlso totalBytes.Value > 0,
                                CInt(downloaded * 100 / totalBytes.Value), 0)

                            progress?.Report(New DownloadProgressInfo With {
                                .FileName = fileName,
                                .TotalBytes = If(totalBytes.HasValue, totalBytes.Value, 0),
                                .DownloadedBytes = downloaded,
                                .SpeedBytesPerSec = speed,
                                .PercentComplete = percent,
                                .Status = "Downloading..."
                            })

                            lastProgressTime = now
                            lastBytes = downloaded
                        End If
                    Loop
                End Using
            End Using

            ' Final progress report
            progress?.Report(New DownloadProgressInfo With {
                .FileName = fileName,
                .TotalBytes = If(totalBytes.HasValue, totalBytes.Value, downloaded),
                .DownloadedBytes = downloaded,
                .SpeedBytesPerSec = 0,
                .PercentComplete = 100,
                .Status = "Complete"
            })
        End Function
#End Region

#Region "Utility Methods"
        ''' <summary>
        ''' Validates that a download response contains actual file content,
        ''' not an HTML page, favicon, or other small non-file content.
        ''' </summary>
        Private Shared Sub ValidateDownloadResponse(response As HttpResponseMessage)
            Dim contentType = response.Content.Headers.ContentType?.MediaType?.ToLower()
            Dim contentLength = response.Content.Headers.ContentLength

            ' Reject HTML responses - we're downloading a file, not a page
            If contentType IsNot Nothing AndAlso
               (contentType.Contains("text/html") OrElse contentType.Contains("text/xml")) Then
                Throw New Exception(
                    "The server returned an HTML page instead of a file. " &
                    "The download link may have expired or requires browser interaction. " &
                    "Please use 'Open Details Page' to download manually.")
            End If

            ' Reject suspiciously small responses (favicon, error pages, etc.)
            ' Game PKGs are typically > 100MB; even small files should be > 100KB
            If contentLength.HasValue AndAlso contentLength.Value < 50000 Then
                ' Check if it could be an image/favicon
                If contentType IsNot Nothing AndAlso
                   (contentType.Contains("image/") OrElse contentType.Contains("text/plain")) Then
                    Throw New Exception(
                        $"The server returned a small file ({contentLength.Value} bytes, type: {contentType}) " &
                        "instead of the expected game file. " &
                        "Please use 'Open Details Page' to download manually.")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Extracts a filename from a URL path.
        ''' </summary>
        Private Shared Function ExtractFileNameFromUrl(url As String) As String
            Try
                Dim uri As New Uri(url)
                Dim path = uri.AbsolutePath.TrimEnd("/"c)
                Dim name = path.Split("/"c).Last()
                name = WebUtility.UrlDecode(name)
                name = SanitizeFileName(name)
                If Not String.IsNullOrEmpty(name) AndAlso name.Length > 2 Then
                    Return name
                End If
            Catch
            End Try
            Return $"download_{DateTime.Now:yyyyMMdd_HHmmss}"
        End Function

        ''' <summary>
        ''' Extracts filename from Content-Disposition header.
        ''' </summary>
        Private Shared Function ExtractFileNameFromResponse(response As HttpResponseMessage) As String
            Try
                Dim cd = response.Content.Headers.ContentDisposition
                If cd IsNot Nothing Then
                    Dim name = If(cd.FileNameStar, cd.FileName)
                    If Not String.IsNullOrEmpty(name) Then
                        name = name.Trim(""""c, " "c)
                        Return SanitizeFileName(name)
                    End If
                End If
            Catch
            End Try
            Return ""
        End Function

        ''' <summary>
        ''' Removes invalid characters from a filename.
        ''' </summary>
        Private Shared Function SanitizeFileName(name As String) As String
            If String.IsNullOrEmpty(name) Then Return ""
            For Each c In Path.GetInvalidFileNameChars()
                name = name.Replace(c, "_"c)
            Next
            ' Limit length
            If name.Length > 200 Then
                Dim ext = Path.GetExtension(name)
                name = name.Substring(0, 200 - ext.Length) & ext
            End If
            Return name.Trim()
        End Function

        ''' <summary>
        ''' Gets a safe output path, avoiding overwriting existing files.
        ''' </summary>
        Private Shared Function GetSafeOutputPath(folder As String, fileName As String) As String
            Dim outputPath = Path.Combine(folder, fileName)
            If Not File.Exists(outputPath) Then Return outputPath

            Dim nameWithoutExt = Path.GetFileNameWithoutExtension(fileName)
            Dim ext = Path.GetExtension(fileName)
            Dim counter = 1

            Do
                outputPath = Path.Combine(folder, $"{nameWithoutExt} ({counter}){ext}")
                counter += 1
            Loop While File.Exists(outputPath)

            Return outputPath
        End Function

        ''' <summary>
        ''' Formats a byte count to a human-readable string.
        ''' </summary>
        Public Shared Function FormatBytes(bytes As Long) As String
            If bytes >= 1073741824 Then
                Return $"{bytes / 1073741824.0:F2} GB"
            ElseIf bytes >= 1048576 Then
                Return $"{bytes / 1048576.0:F2} MB"
            ElseIf bytes >= 1024 Then
                Return $"{bytes / 1024.0:F2} KB"
            Else
                Return $"{bytes} B"
            End If
        End Function

        ''' <summary>
        ''' Formats a speed in bytes/sec to a human-readable string.
        ''' </summary>
        Public Shared Function FormatSpeed(bytesPerSec As Double) As String
            If bytesPerSec >= 1073741824 Then
                Return $"{bytesPerSec / 1073741824.0:F1} GB/s"
            ElseIf bytesPerSec >= 1048576 Then
                Return $"{bytesPerSec / 1048576.0:F1} MB/s"
            ElseIf bytesPerSec >= 1024 Then
                Return $"{bytesPerSec / 1024.0:F1} KB/s"
            Else
                Return $"{bytesPerSec:F0} B/s"
            End If
        End Function
#End Region
    End Class
End Namespace
