Imports System.IO

Public Module SdkDetector

    Public Structure SdkVersionInfo
        Public Ps5SdkVersion As UInteger
        Public Ps4SdkVersion As UInteger
        Public FileName As String
        Public IsValid As Boolean
        Public ErrorMessage As String
    End Structure

    Public Structure FolderSdkAnalysis
        Public HighestPs5Sdk As UInteger
        Public LowestPs5Sdk As UInteger
        Public AveragePs5Sdk As UInteger
        Public MostCommonPs5Sdk As UInteger
        Public FilesAnalyzed As Integer
        Public FileVersions As Dictionary(Of String, SdkVersionInfo)
        Public RecommendedTargetSdk As UInteger
        Public IsConsistent As Boolean
    End Structure

    ''' <summary>
    ''' Auto-detect SDK version from a single file
    ''' </summary>
    Public Function DetectSdkFromFile(filePath As String) As SdkVersionInfo
        Dim info As New SdkVersionInfo With {
            .FileName = Path.GetFileName(filePath),
            .IsValid = False,
            .Ps5SdkVersion = 0,
            .Ps4SdkVersion = 0
        }

        Try
            ' Use existing ElfInspector class to get SDK info
            Dim elfInfo = ElfInspector.ReadInfo(filePath)

            If elfInfo IsNot Nothing Then
                If elfInfo.Ps5SdkVersion.HasValue Then
                    info.Ps5SdkVersion = elfInfo.Ps5SdkVersion.Value
                    info.IsValid = True
                End If

                If elfInfo.Ps4SdkVersion.HasValue Then
                    info.Ps4SdkVersion = elfInfo.Ps4SdkVersion.Value
                End If
            Else
                info.ErrorMessage = "Failed to read ELF information"
            End If
        Catch ex As Exception
            info.ErrorMessage = ex.Message
            info.IsValid = False
        End Try

        Return info
    End Function

    ''' <summary>
    ''' Analyze all ELF files in folder and detect SDK versions
    ''' </summary>
    Public Function AnalyzeFolderSdkVersions(folderPath As String) As FolderSdkAnalysis
        Dim analysis As New FolderSdkAnalysis With {
            .HighestPs5Sdk = 0,
            .LowestPs5Sdk = UInteger.MaxValue,
            .FilesAnalyzed = 0,
            .FileVersions = New Dictionary(Of String, SdkVersionInfo),
            .IsConsistent = True
        }

        Try
            ' Get all patchable files
            Dim files As New List(Of String)

            ' Add eboot.bin
            Dim ebotPath = Path.Combine(folderPath, EBOOT_FILENAME)
            If File.Exists(ebotPath) Then
                files.Add(ebotPath)
            End If

            ' Add .prx and .sprx files
            For Each ext In PATCHABLE_EXTENSIONS
                Dim foundFiles = Directory.GetFiles(folderPath, $"*{ext}", SearchOption.AllDirectories)
                files.AddRange(foundFiles)
            Next

            If files.Count = 0 Then
                Return analysis
            End If

            Dim validVersions As New List(Of UInteger)
            Dim versionCounts As New Dictionary(Of UInteger, Integer)

            ' Analyze each file
            For Each file In files
                Dim sdkInfo = DetectSdkFromFile(file)

                If sdkInfo.IsValid AndAlso sdkInfo.Ps5SdkVersion > 0 Then
                    analysis.FilesAnalyzed += 1
                    analysis.FileVersions(Path.GetFileName(file)) = sdkInfo

                    validVersions.Add(sdkInfo.Ps5SdkVersion)

                    ' Track highest and lowest
                    If sdkInfo.Ps5SdkVersion > analysis.HighestPs5Sdk Then
                        analysis.HighestPs5Sdk = sdkInfo.Ps5SdkVersion
                    End If

                    If sdkInfo.Ps5SdkVersion < analysis.LowestPs5Sdk Then
                        analysis.LowestPs5Sdk = sdkInfo.Ps5SdkVersion
                    End If

                    ' Count occurrences for most common
                    If versionCounts.ContainsKey(sdkInfo.Ps5SdkVersion) Then
                        versionCounts(sdkInfo.Ps5SdkVersion) += 1
                    Else
                        versionCounts(sdkInfo.Ps5SdkVersion) = 1
                    End If
                End If
            Next

            If validVersions.Count > 0 Then
                ' Calculate average
                Dim sum As Long = 0
                For Each v In validVersions
                    sum += v
                Next
                analysis.AveragePs5Sdk = CUInt(sum / validVersions.Count)

                ' Find most common version
                Dim maxCount = versionCounts.Values.Max()
                analysis.MostCommonPs5Sdk = versionCounts.First(Function(kvp) kvp.Value = maxCount).Key

                ' Check if versions are consistent (all same or within reasonable range)
                Dim versionRange = analysis.HighestPs5Sdk - analysis.LowestPs5Sdk
                analysis.IsConsistent = (versionRange = 0) ' All files have same SDK

                ' Recommend target SDK (use the lowest to ensure compatibility)
                analysis.RecommendedTargetSdk = analysis.LowestPs5Sdk
            Else
                ' No valid versions found, reset lowest
                analysis.LowestPs5Sdk = 0
            End If
        Catch ex As Exception
            ' Return partial analysis
        End Try

        Return analysis
    End Function

    ''' <summary>
    ''' Get human-readable SDK version string
    ''' </summary>
    Public Function FormatSdkVersion(version As UInteger) As String
        Try
            Dim major = (version >> 24) And &HFF
            Dim minor = (version >> 16) And &HFF
            Dim patch = version And &HFFFF
            Return $"{major}.{minor:D2}.{patch:D4}"
        Catch ex As Exception
            Return version.ToString("X8")
        End Try
    End Function

    ''' <summary>
    ''' Generate analysis report
    ''' </summary>
    Public Function GenerateAnalysisReport(analysis As FolderSdkAnalysis) As String
        Dim sb As New Text.StringBuilder()

        sb.AppendLine("=======================================")
        sb.AppendLine("  SDK VERSION ANALYSIS REPORT")
        sb.AppendLine("=======================================")
        sb.AppendLine()

        If analysis.FilesAnalyzed = 0 Then
            sb.AppendLine("No ELF files found or analyzed.")
            Return sb.ToString()
        End If

        sb.AppendLine($"Files Analyzed: {analysis.FilesAnalyzed}")
        sb.AppendLine($"Version Consistency: {If(analysis.IsConsistent, "+ All files use same SDK", "! Mixed SDK versions detected")}")
        sb.AppendLine()

        sb.AppendLine("SDK Version Statistics:")
        sb.AppendLine($"  Highest: {FormatSdkVersion(analysis.HighestPs5Sdk)} (0x{analysis.HighestPs5Sdk:X8})")
        sb.AppendLine($"  Lowest:  {FormatSdkVersion(analysis.LowestPs5Sdk)} (0x{analysis.LowestPs5Sdk:X8})")
        sb.AppendLine($"  Average: {FormatSdkVersion(analysis.AveragePs5Sdk)} (0x{analysis.AveragePs5Sdk:X8})")
        sb.AppendLine($"  Most Common: {FormatSdkVersion(analysis.MostCommonPs5Sdk)} (0x{analysis.MostCommonPs5Sdk:X8})")
        sb.AppendLine()

        sb.AppendLine($"+ Recommended Target SDK: {FormatSdkVersion(analysis.RecommendedTargetSdk)} (0x{analysis.RecommendedTargetSdk:X8})")
        sb.AppendLine()

        sb.AppendLine("Per-File Details:")
        For Each kvp In analysis.FileVersions
            Dim fileInfo = kvp.Value
            sb.AppendLine($"  {kvp.Key}: {FormatSdkVersion(fileInfo.Ps5SdkVersion)}")
        Next

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Quick check if folder needs patching
    ''' </summary>
    Public Function NeedsPatchingToTarget(folderPath As String, targetSdk As UInteger) As Boolean
        Try
            Dim analysis = AnalyzeFolderSdkVersions(folderPath)
            Return analysis.FilesAnalyzed > 0 AndAlso analysis.LowestPs5Sdk > targetSdk
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Suggest optimal target SDK based on common homebrew compatibility
    ''' </summary>
    Public Function SuggestOptimalTargetSdk(currentSdk As UInteger) As UInteger
        ' Common stable SDK versions for compatibility
        Dim stableVersions() As UInteger = {
            &H3000038UI,  ' 3.00.0056
            &H4000038UI,  ' 4.00.0056
            &H4500038UI,  ' 4.50.0056
            &H5000038UI,  ' 5.00.0056
            &H7000038UI   ' 7.00.0056 (current popular choice)
        }

        ' Find the highest stable version that's lower than current
        For i = stableVersions.Length - 1 To 0 Step -1
            If stableVersions(i) < currentSdk Then
                Return stableVersions(i)
            End If
        Next

        ' If current is already low, return it
        Return currentSdk
    End Function

End Module