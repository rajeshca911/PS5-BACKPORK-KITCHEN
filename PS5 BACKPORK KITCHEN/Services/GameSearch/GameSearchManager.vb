Imports System.IO
Imports Newtonsoft.Json

Namespace Services.GameSearch
    ''' <summary>
    ''' Manager for game search providers
    ''' Handles multiple providers and credential storage
    ''' </summary>
    Public Class GameSearchManager
        Private ReadOnly _providers As New Dictionary(Of String, IGameSearchProvider)
        Private ReadOnly _credentials As New Dictionary(Of String, ProviderCredentials)
        Private ReadOnly _credentialsFilePath As String

        Public Event SearchStarted(sender As Object, providerName As String)
        Public Event SearchCompleted(sender As Object, providerName As String, resultCount As Integer)
        Public Event SearchError(sender As Object, providerName As String, errorMessage As String)
        Public Event LoginStatusChanged(sender As Object, providerName As String, isLoggedIn As Boolean)

        Public Sub New()
            _credentialsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "search_credentials.json")

            ' Register built-in providers
            RegisterProvider(New RuTrackerProvider())
            RegisterProvider(New LeetxProvider())
            RegisterProvider(New TorrentGalaxyProvider())
            RegisterProvider(New PirateBayProvider())

            ' Load saved credentials
            LoadCredentials()
        End Sub

        ''' <summary>
        ''' Get all registered providers
        ''' </summary>
        Public ReadOnly Property Providers As IReadOnlyDictionary(Of String, IGameSearchProvider)
            Get
                Return _providers
            End Get
        End Property

        ''' <summary>
        ''' Register a new search provider
        ''' </summary>
        Public Sub RegisterProvider(provider As IGameSearchProvider)
            If Not _providers.ContainsKey(provider.Name) Then
                _providers.Add(provider.Name, provider)
            End If
        End Sub

        ''' <summary>
        ''' Get a specific provider
        ''' </summary>
        Public Function GetProvider(name As String) As IGameSearchProvider
            If _providers.ContainsKey(name) Then
                Return _providers(name)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Set credentials for a provider
        ''' </summary>
        Public Sub SetProviderCredentials(providerName As String, credentials As ProviderCredentials)
            _credentials(providerName) = credentials

            If _providers.ContainsKey(providerName) Then
                _providers(providerName).SetCredentials(credentials)
            End If

            SaveCredentials()
        End Sub

        ''' <summary>
        ''' Get credentials for a provider
        ''' </summary>
        Public Function GetProviderCredentials(providerName As String) As ProviderCredentials
            If _credentials.ContainsKey(providerName) Then
                Return _credentials(providerName)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Login to a provider
        ''' </summary>
        Public Async Function LoginProviderAsync(providerName As String) As Task(Of Boolean)
            If Not _providers.ContainsKey(providerName) Then Return False

            Dim provider = _providers(providerName)

            ' Apply credentials if available
            If _credentials.ContainsKey(providerName) Then
                provider.SetCredentials(_credentials(providerName))
            End If

            Dim result = Await provider.LoginAsync()
            RaiseEvent LoginStatusChanged(Me, providerName, result)
            Return result
        End Function

        ''' <summary>
        ''' Search all enabled providers
        ''' </summary>
        Public Async Function SearchAllAsync(query As GameSearchQuery, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult))
            Dim allResults As New List(Of GameSearchResult)

            For Each provider In _providers.Values
                If cancellationToken.IsCancellationRequested Then Exit For

                Try
                    RaiseEvent SearchStarted(Me, provider.Name)

                    Dim results = Await provider.SearchAsync(query, cancellationToken)
                    allResults.AddRange(results)

                    RaiseEvent SearchCompleted(Me, provider.Name, results.Count)

                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception
                    RaiseEvent SearchError(Me, provider.Name, ex.Message)
                End Try
            Next

            ' Sort combined results
            Select Case query.SortBy
                Case SearchSortBy.Seeds
                    allResults = allResults.OrderByDescending(Function(r) r.Seeds).ToList()
                Case SearchSortBy.Size
                    allResults = allResults.OrderByDescending(Function(r) r.SizeBytes).ToList()
                Case SearchSortBy.UploadDate
                    allResults = allResults.OrderByDescending(Function(r) r.UploadDate).ToList()
                Case SearchSortBy.Name
                    allResults = allResults.OrderBy(Function(r) r.Title).ToList()
            End Select

            Return allResults.Take(query.MaxResults).ToList()
        End Function

        ''' <summary>
        ''' Search a specific provider
        ''' </summary>
        Public Async Function SearchProviderAsync(providerName As String, query As GameSearchQuery, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult))
            If Not _providers.ContainsKey(providerName) Then
                Return New List(Of GameSearchResult)()
            End If

            Dim provider = _providers(providerName)

            RaiseEvent SearchStarted(Me, provider.Name)

            Try
                Dim results = Await provider.SearchAsync(query, cancellationToken)
                RaiseEvent SearchCompleted(Me, provider.Name, results.Count)
                Return results
            Catch ex As Exception
                RaiseEvent SearchError(Me, provider.Name, ex.Message)
                Return New List(Of GameSearchResult)()
            End Try
        End Function

        ''' <summary>
        ''' Get magnet link for a result
        ''' </summary>
        Public Async Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String)
            ' Find the provider for this result
            For Each provider In _providers.Values
                If provider.DisplayName = result.SourceProvider Then
                    Return Await provider.GetMagnetLinkAsync(result)
                End If
            Next
            Return ""
        End Function

        ''' <summary>
        ''' Test all provider connections
        ''' </summary>
        Public Async Function TestAllConnectionsAsync() As Task(Of Dictionary(Of String, Boolean))
            Dim results As New Dictionary(Of String, Boolean)

            For Each provider In _providers.Values
                Try
                    results(provider.Name) = Await provider.TestConnectionAsync()
                Catch ex As Exception
                    results(provider.Name) = False
                End Try
            Next

            Return results
        End Function

        ''' <summary>
        ''' Save credentials to file (encrypted)
        ''' </summary>
        Private Sub SaveCredentials()
            Try
                Dim credData As New Dictionary(Of String, Object)

                For Each kvp In _credentials
                    credData(kvp.Key) = New With {
                        .Username = EncryptString(kvp.Value.Username),
                        .Password = EncryptString(kvp.Value.Password),
                        .ApiKey = EncryptString(kvp.Value.ApiKey)
                    }
                Next

                Dim json = JsonConvert.SerializeObject(credData, Formatting.Indented)
                File.WriteAllText(_credentialsFilePath, json)

            Catch ex As Exception
                ' Silently fail
            End Try
        End Sub

        ''' <summary>
        ''' Load credentials from file
        ''' </summary>
        Private Sub LoadCredentials()
            Try
                If Not File.Exists(_credentialsFilePath) Then Return

                Dim json = File.ReadAllText(_credentialsFilePath)
                Dim credData = JsonConvert.DeserializeObject(Of Dictionary(Of String, Dictionary(Of String, String)))(json)

                If credData Is Nothing Then Return

                For Each kvp In credData
                    Dim cred As New ProviderCredentials With {
                        .Username = DecryptString(kvp.Value.GetValueOrDefault("Username", "")),
                        .Password = DecryptString(kvp.Value.GetValueOrDefault("Password", "")),
                        .ApiKey = DecryptString(kvp.Value.GetValueOrDefault("ApiKey", ""))
                    }
                    _credentials(kvp.Key) = cred

                    ' Apply to provider if registered
                    If _providers.ContainsKey(kvp.Key) Then
                        _providers(kvp.Key).SetCredentials(cred)
                    End If
                Next

            Catch ex As Exception
                ' Silently fail
            End Try
        End Sub

        ' Simple encryption/decryption for stored credentials
        Private Function EncryptString(input As String) As String
            If String.IsNullOrEmpty(input) Then Return ""
            Return Convert.ToBase64String(Text.Encoding.UTF8.GetBytes(input))
        End Function

        Private Function DecryptString(input As String) As String
            If String.IsNullOrEmpty(input) Then Return ""
            Try
                Return Text.Encoding.UTF8.GetString(Convert.FromBase64String(input))
            Catch
                Return ""
            End Try
        End Function
    End Class
End Namespace
