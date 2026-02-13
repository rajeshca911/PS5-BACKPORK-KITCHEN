Imports System.IO

''' <summary>
''' Orchestrates the UFS2 -> extract -> FPKG conversion pipeline.
''' </summary>
Public Class ConversionService

    ''' <summary>
    ''' Result wrapper for UFS2-to-FPKG conversion.
    ''' </summary>
    Public Class ConversionResult
        Public Property Success As Boolean
        Public Property ErrorMessage As String
        Public Property OutputPath As String
        Public Property ExtractedFiles As Integer
        Public Property TotalSize As Long

        Public Shared Function Ok(outputPath As String, extractedFiles As Integer, totalSize As Long) As ConversionResult
            Return New ConversionResult With {
                .Success = True,
                .OutputPath = outputPath,
                .ExtractedFiles = extractedFiles,
                .TotalSize = totalSize
            }
        End Function

        Public Shared Function Fail(message As String) As ConversionResult
            Return New ConversionResult With {.Success = False, .ErrorMessage = message}
        End Function
    End Class

    ''' <summary>
    ''' Converts a UFS2 image to an FPKG.
    ''' Step 1: Extract UFS2 to temp folder
    ''' Step 2: Build FPKG from temp folder
    ''' Step 3: Cleanup temp folder
    ''' </summary>
    Public Shared Function ConvertUFS2ToFPKG(ufs2ImagePath As String, outputPkgPath As String,
                                              config As FPKGConfig,
                                              Optional progress As IProgress(Of BuildProgress) = Nothing) As ConversionResult
        ' Validate inputs
        If Not File.Exists(ufs2ImagePath) Then
            Return ConversionResult.Fail($"UFS2 image not found: {ufs2ImagePath}")
        End If

        Dim validationError = FPKGBuilderService.ValidateConfig(config)
        If Not String.IsNullOrEmpty(validationError) Then
            Return ConversionResult.Fail(validationError)
        End If

        Dim tempFolder = Path.Combine(Path.GetTempPath(), $"fpkg_convert_{Guid.NewGuid():N}")

        Try
            ' Step 1: Extract UFS2 to temp folder
            progress?.Report(New BuildProgress With {
                .Stage = "Extracting UFS2 image...",
                .PercentComplete = 0
            })

            Dim openResult = UFS2ImageService.OpenImage(ufs2ImagePath,
                New Progress(Of String)(Sub(msg)
                                            progress?.Report(New BuildProgress With {
                                                .Stage = msg,
                                                .PercentComplete = 5
                                            })
                                        End Sub))

            If Not openResult.Success Then
                Return ConversionResult.Fail($"Failed to open UFS2 image: {openResult.ErrorMessage}")
            End If

            Dim extractedCount = 0
            Try
                Dim extractProgress = New Progress(Of Integer)(
                    Sub(pct)
                        progress?.Report(New BuildProgress With {
                            .Stage = "Extracting files...",
                            .PercentComplete = CInt(pct * 0.4)  ' 0-40% for extraction
                        })
                    End Sub)

                UFS2ImageService.ExtractAll(openResult.Reader, openResult.FileTree, tempFolder, extractProgress)
                extractedCount = openResult.FileTree.TotalFileCount
            Finally
                openResult.Reader?.Dispose()
            End Try

            ' Step 2: Build FPKG from temp folder
            progress?.Report(New BuildProgress With {
                .Stage = "Building FPKG...",
                .PercentComplete = 45
            })

            Dim buildProgress = New Progress(Of BuildProgress)(
                Sub(bp)
                    ' Remap build progress to 45-95% range
                    Dim remappedPct = 45 + CInt(bp.PercentComplete * 0.5)
                    progress?.Report(New BuildProgress With {
                        .Stage = bp.Stage,
                        .PercentComplete = remappedPct,
                        .CurrentFile = bp.CurrentFile
                    })
                End Sub)

            Dim buildResult = FPKGBuilderService.BuildFromFolder(tempFolder, outputPkgPath, config, buildProgress)

            If Not buildResult.Success Then
                Return ConversionResult.Fail($"FPKG build failed: {buildResult.ErrorMessage}")
            End If

            progress?.Report(New BuildProgress With {
                .Stage = "Conversion complete",
                .PercentComplete = 100
            })

            Return ConversionResult.Ok(outputPkgPath, extractedCount, buildResult.TotalSize)

        Catch ex As Exception
            Return ConversionResult.Fail($"Conversion failed: {ex.Message}")
        Finally
            ' Step 3: Cleanup temp folder
            Try
                If Directory.Exists(tempFolder) Then
                    Directory.Delete(tempFolder, True)
                End If
            Catch
                ' Ignore cleanup errors
            End Try
        End Try
    End Function

End Class
