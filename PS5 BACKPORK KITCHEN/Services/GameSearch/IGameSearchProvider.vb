Namespace Services.GameSearch
    ''' <summary>
    ''' Interface for game search providers
    ''' </summary>
    Public Interface IGameSearchProvider
        ''' <summary>
        ''' Provider name
        ''' </summary>
        ReadOnly Property Name As String

        ''' <summary>
        ''' Provider display name
        ''' </summary>
        ReadOnly Property DisplayName As String

        ''' <summary>
        ''' Whether this provider requires authentication
        ''' </summary>
        ReadOnly Property RequiresAuthentication As Boolean

        ''' <summary>
        ''' Whether the provider is currently logged in
        ''' </summary>
        ReadOnly Property IsLoggedIn As Boolean

        ''' <summary>
        ''' Provider status
        ''' </summary>
        ReadOnly Property Status As ProviderStatus

        ''' <summary>
        ''' Set credentials for authentication
        ''' </summary>
        Sub SetCredentials(credentials As ProviderCredentials)

        ''' <summary>
        ''' Login to the provider
        ''' </summary>
        Function LoginAsync() As Task(Of Boolean)

        ''' <summary>
        ''' Logout from the provider
        ''' </summary>
        Sub Logout()

        ''' <summary>
        ''' Search for games
        ''' </summary>
        Function SearchAsync(query As GameSearchQuery, cancellationToken As Threading.CancellationToken) As Task(Of List(Of GameSearchResult))

        ''' <summary>
        ''' Get magnet link for a result
        ''' </summary>
        Function GetMagnetLinkAsync(result As GameSearchResult) As Task(Of String)

        ''' <summary>
        ''' Test connection to provider
        ''' </summary>
        Function TestConnectionAsync() As Task(Of Boolean)
    End Interface
End Namespace
