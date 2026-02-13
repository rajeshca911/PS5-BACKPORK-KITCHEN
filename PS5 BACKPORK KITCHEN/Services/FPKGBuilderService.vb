Imports System.IO

''' <summary>
''' Service wrapper for FPKGBuilder with validation and Result pattern.
''' </summary>
Public Class FPKGBuilderService

    ''' <summary>
    ''' Result wrapper for FPKG build operations.
    ''' </summary>
    Public Class BuildResult
        Public Property Success As Boolean
        Public Property ErrorMessage As String
        Public Property OutputPath As String
        Public Property FileCount As Integer
        Public Property TotalSize As Long

        Public Shared Function Ok(outputPath As String, fileCount As Integer, totalSize As Long) As BuildResult
            Return New BuildResult With {
                .Success = True,
                .OutputPath = outputPath,
                .FileCount = fileCount,
                .TotalSize = totalSize
            }
        End Function

        Public Shared Function Fail(message As String) As BuildResult
            Return New BuildResult With {.Success = False, .ErrorMessage = message}
        End Function
    End Class

    ''' <summary>
    ''' Validates the FPKG configuration. Returns an error message or empty string.
    ''' </summary>
    Public Shared Function ValidateConfig(config As FPKGConfig) As String
        If config Is Nothing Then
            Return "Configuration is Nothing."
        End If

        If String.IsNullOrWhiteSpace(config.ContentId) Then
            Return "Content ID is required."
        End If

        If config.ContentId.Length > 36 Then
            Return "Content ID must be 36 characters or less."
        End If

        If String.IsNullOrWhiteSpace(config.Title) Then
            Return "Title is required."
        End If

        If String.IsNullOrWhiteSpace(config.TitleId) Then
            Return "Title ID is required."
        End If

        If config.TitleId.Length > 9 Then
            Return "Title ID must be 9 characters or less."
        End If

        Return ""
    End Function

    ''' <summary>
    ''' Builds an FPKG from a source folder. Validates config first.
    ''' </summary>
    Public Shared Function BuildFromFolder(sourceFolder As String, outputPath As String,
                                            config As FPKGConfig,
                                            Optional progress As IProgress(Of BuildProgress) = Nothing) As BuildResult
        ' Validate config
        Dim validationError = ValidateConfig(config)
        If Not String.IsNullOrEmpty(validationError) Then
            Return BuildResult.Fail(validationError)
        End If

        ' Validate source folder
        If Not Directory.Exists(sourceFolder) Then
            Return BuildResult.Fail($"Source folder not found: {sourceFolder}")
        End If

        ' Check that source folder has files
        Dim files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
        If files.Length = 0 Then
            Return BuildResult.Fail("Source folder is empty.")
        End If

        Try
            Dim builder As New FPKGBuilder()
            builder.Build(sourceFolder, outputPath, config, progress)

            ' Calculate output info
            Dim outputInfo As New FileInfo(outputPath)
            Return BuildResult.Ok(outputPath, files.Length, outputInfo.Length)
        Catch ex As Exception
            Return BuildResult.Fail($"Build failed: {ex.Message}")
        End Try
    End Function

End Class
