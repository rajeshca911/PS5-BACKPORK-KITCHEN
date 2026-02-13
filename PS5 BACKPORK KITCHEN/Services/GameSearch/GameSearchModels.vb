Imports System.ComponentModel

Namespace Services.GameSearch
    ''' <summary>
    ''' Search result from a game search provider
    ''' </summary>
    Public Class GameSearchResult
        Public Property Title As String
        Public Property Size As String
        Public Property SizeBytes As Long
        Public Property Seeds As Integer
        Public Property Leeches As Integer
        Public Property UploadDate As DateTime
        Public Property Uploader As String
        Public Property SourceProvider As String
        Public Property DownloadUrl As String
        Public Property MagnetLink As String
        Public Property DetailsUrl As String
        Public Property Category As String
        Public Property Platform As String
        Public Property Region As String
        Public Property FirmwareRequired As String

        Public ReadOnly Property DisplaySize As String
            Get
                If SizeBytes <= 0 Then Return Size
                If SizeBytes >= 1073741824 Then ' GB
                    Return $"{SizeBytes / 1073741824.0:F2} GB"
                ElseIf SizeBytes >= 1048576 Then ' MB
                    Return $"{SizeBytes / 1048576.0:F2} MB"
                Else
                    Return $"{SizeBytes / 1024.0:F2} KB"
                End If
            End Get
        End Property

        Public ReadOnly Property HealthScore As Integer
            Get
                If Seeds = 0 Then Return 0
                If Seeds >= 100 Then Return 100
                Return Math.Min(100, Seeds + (Seeds * 10 / Math.Max(1, Leeches)))
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Search parameters
    ''' </summary>
    Public Class GameSearchQuery
        Public Property SearchText As String
        Public Property Platform As GamePlatform = GamePlatform.PS5
        Public Property Category As GameCategory = GameCategory.All
        Public Property SortBy As SearchSortBy = SearchSortBy.Seeds
        Public Property MaxResults As Integer = 50
    End Class

    ''' <summary>
    ''' Game platforms to search
    ''' </summary>
    Public Enum GamePlatform
        All = 0
        PS5 = 1
        PS4 = 2
    End Enum

    ''' <summary>
    ''' Game categories
    ''' </summary>
    Public Enum GameCategory
        All = 0
        Games = 1
        Updates = 2
        DLC = 3
    End Enum

    ''' <summary>
    ''' Sort options
    ''' </summary>
    Public Enum SearchSortBy
        Seeds = 0
        Size = 1
        UploadDate = 2
        Name = 3
    End Enum

    ''' <summary>
    ''' Provider credentials
    ''' </summary>
    Public Class ProviderCredentials
        Public Property Username As String
        Public Property Password As String
        Public Property ApiKey As String
        Public Property SessionCookie As String

        Public ReadOnly Property HasCredentials As Boolean
            Get
                Return Not String.IsNullOrEmpty(Username) AndAlso Not String.IsNullOrEmpty(Password)
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Provider status
    ''' </summary>
    Public Class ProviderStatus
        Public Property IsEnabled As Boolean
        Public Property IsLoggedIn As Boolean
        Public Property LastError As String
        Public Property LastSearchTime As DateTime
        Public Property TotalSearches As Integer
    End Class
End Namespace
