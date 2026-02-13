Imports System.IO
Imports Newtonsoft.Json

Public Module PresetManager

    Public Structure PatchPreset
        Public PresetName As String
        Public Description As String
        Public TargetPs5Sdk As UInteger
        Public TargetPs4Sdk As UInteger
        Public AutoBackup As Boolean
        Public AutoVerify As Boolean
        Public ExportReport As Boolean
        Public CreatedDate As DateTime
        Public LastUsed As DateTime
        Public TimesUsed As Integer
        Public IsBuiltIn As Boolean
    End Structure

    Private ReadOnly PresetsPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets.json")

    ''' <summary>
    ''' Get all built-in presets
    ''' </summary>
    Public Function GetBuiltInPresets() As List(Of PatchPreset)
        Return New List(Of PatchPreset) From {
            New PatchPreset With {
                .PresetName = "Maximum Compatibility",
                .Description = "Target oldest SDK for maximum compatibility (3.00)",
                .TargetPs5Sdk = &H3000038UI,
                .TargetPs4Sdk = 0,
                .AutoBackup = True,
                .AutoVerify = True,
                .ExportReport = True,
                .IsBuiltIn = True
            },
            New PatchPreset With {
                .PresetName = "Safe Standard",
                .Description = "Balanced compatibility and features (5.00)",
                .TargetPs5Sdk = &H5000038UI,
                .TargetPs4Sdk = 0,
                .AutoBackup = True,
                .AutoVerify = True,
                .ExportReport = False,
                .IsBuiltIn = True
            },
            New PatchPreset With {
                .PresetName = "Modern Features",
                .Description = "Recent SDK with modern features (7.00)",
                .TargetPs5Sdk = &H7000038UI,
                .TargetPs4Sdk = 0,
                .AutoBackup = True,
                .AutoVerify = False,
                .ExportReport = False,
                .IsBuiltIn = True
            },
            New PatchPreset With {
                .PresetName = "Quick Patch",
                .Description = "Fast patching without backup (use with caution!)",
                .TargetPs5Sdk = &H5000038UI,
                .TargetPs4Sdk = 0,
                .AutoBackup = False,
                .AutoVerify = False,
                .ExportReport = False,
                .IsBuiltIn = True
            },
            New PatchPreset With {
                .PresetName = "Professional",
                .Description = "Full backup, verification and reporting",
                .TargetPs5Sdk = &H5000038UI,
                .TargetPs4Sdk = 0,
                .AutoBackup = True,
                .AutoVerify = True,
                .ExportReport = True,
                .IsBuiltIn = True
            }
        }
    End Function

    ''' <summary>
    ''' Load all presets (built-in + custom)
    ''' </summary>
    Public Function LoadAllPresets() As List(Of PatchPreset)
        Dim allPresets = GetBuiltInPresets()

        Try
            If File.Exists(PresetsPath) Then
                Dim json = File.ReadAllText(PresetsPath)
                Dim customPresets = JsonConvert.DeserializeObject(Of List(Of PatchPreset))(json)

                If customPresets IsNot Nothing Then
                    allPresets.AddRange(customPresets)
                End If
            End If
        Catch ex As Exception
            ' Return built-in presets only
        End Try

        Return allPresets
    End Function

    ''' <summary>
    ''' Save custom presets
    ''' </summary>
    Private Sub SaveCustomPresets(customPresets As List(Of PatchPreset))
        Try
            Dim json = JsonConvert.SerializeObject(customPresets, Formatting.Indented)
            File.WriteAllText(PresetsPath, json)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Create new custom preset
    ''' </summary>
    Public Function CreatePreset(preset As PatchPreset) As Boolean
        Try
            ' Validate preset name
            If String.IsNullOrWhiteSpace(preset.PresetName) Then
                Return False
            End If

            Dim customPresets = LoadCustomPresets()

            ' Check for duplicate name
            If customPresets.Any(Function(p) p.PresetName.Equals(preset.PresetName, StringComparison.OrdinalIgnoreCase)) Then
                Return False
            End If

            ' Set metadata
            preset.CreatedDate = DateTime.Now
            preset.LastUsed = DateTime.Now
            preset.TimesUsed = 0
            preset.IsBuiltIn = False

            customPresets.Add(preset)
            SaveCustomPresets(customPresets)

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Update existing preset
    ''' </summary>
    Public Function UpdatePreset(presetName As String, updatedPreset As PatchPreset) As Boolean
        Try
            Dim customPresets = LoadCustomPresets()
            Dim index = customPresets.FindIndex(Function(p) p.PresetName.Equals(presetName, StringComparison.OrdinalIgnoreCase))

            If index >= 0 Then
                ' Preserve metadata
                updatedPreset.CreatedDate = customPresets(index).CreatedDate
                updatedPreset.TimesUsed = customPresets(index).TimesUsed
                updatedPreset.IsBuiltIn = False

                customPresets(index) = updatedPreset
                SaveCustomPresets(customPresets)
                Return True
            End If

            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Delete custom preset
    ''' </summary>
    Public Function DeletePreset(presetName As String) As Boolean
        Try
            Dim customPresets = LoadCustomPresets()
            Dim removed = customPresets.RemoveAll(Function(p) p.PresetName.Equals(presetName, StringComparison.OrdinalIgnoreCase))

            If removed > 0 Then
                SaveCustomPresets(customPresets)
                Return True
            End If

            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Get preset by name
    ''' </summary>
    Public Function GetPreset(presetName As String) As PatchPreset?
        Try
            Dim allPresets = LoadAllPresets()
            Dim preset = allPresets.FirstOrDefault(Function(p) p.PresetName.Equals(presetName, StringComparison.OrdinalIgnoreCase))

            If preset.PresetName IsNot Nothing Then
                Return preset
            End If
        Catch ex As Exception
            ' Return nothing
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Mark preset as used
    ''' </summary>
    Public Sub MarkPresetAsUsed(presetName As String)
        Try
            Dim customPresets = LoadCustomPresets()
            Dim preset = customPresets.FirstOrDefault(Function(p) p.PresetName.Equals(presetName, StringComparison.OrdinalIgnoreCase))

            If preset.PresetName IsNot Nothing Then
                Dim index = customPresets.IndexOf(preset)
                If index >= 0 Then
                    preset.LastUsed = DateTime.Now
                    preset.TimesUsed += 1
                    customPresets(index) = preset
                    SaveCustomPresets(customPresets)
                End If
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Load only custom presets
    ''' </summary>
    Private Function LoadCustomPresets() As List(Of PatchPreset)
        Try
            If File.Exists(PresetsPath) Then
                Dim json = File.ReadAllText(PresetsPath)
                Return JsonConvert.DeserializeObject(Of List(Of PatchPreset))(json)
            End If
        Catch ex As Exception
            ' Return empty list
        End Try

        Return New List(Of PatchPreset)
    End Function

    ''' <summary>
    ''' Import presets from file
    ''' </summary>
    Public Function ImportPresets(filePath As String) As Integer
        Try
            If Not File.Exists(filePath) Then
                Return 0
            End If

            Dim json = File.ReadAllText(filePath)
            Dim importedPresets = JsonConvert.DeserializeObject(Of List(Of PatchPreset))(json)

            If importedPresets Is Nothing OrElse importedPresets.Count = 0 Then
                Return 0
            End If

            Dim customPresets = LoadCustomPresets()
            Dim importedCount = 0

            For Each preset In importedPresets
                ' Skip if name already exists
                If Not customPresets.Any(Function(p) p.PresetName.Equals(preset.PresetName, StringComparison.OrdinalIgnoreCase)) Then
                    preset.IsBuiltIn = False
                    customPresets.Add(preset)
                    importedCount += 1
                End If
            Next

            If importedCount > 0 Then
                SaveCustomPresets(customPresets)
            End If

            Return importedCount
        Catch ex As Exception
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Export presets to file
    ''' </summary>
    Public Function ExportPresets(filePath As String, Optional includeBuiltIn As Boolean = False) As Boolean
        Try
            Dim presetsToExport = If(includeBuiltIn, LoadAllPresets(), LoadCustomPresets())

            If presetsToExport.Count = 0 Then
                Return False
            End If

            Dim json = JsonConvert.SerializeObject(presetsToExport, Formatting.Indented)
            File.WriteAllText(filePath, json)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Get preset names for combo box
    ''' </summary>
    Public Function GetPresetNames() As List(Of String)
        Dim allPresets = LoadAllPresets()
        Return allPresets.Select(Function(p) p.PresetName).ToList()
    End Function

    ''' <summary>
    ''' Clone preset to create a new one
    ''' </summary>
    Public Function ClonePreset(sourcePresetName As String, newPresetName As String) As Boolean
        Try
            Dim sourcePreset = GetPreset(sourcePresetName)

            If Not sourcePreset.HasValue Then
                Return False
            End If

            Dim newPreset = sourcePreset.Value
            newPreset.PresetName = newPresetName
            newPreset.Description = $"Cloned from '{sourcePresetName}'"
            newPreset.IsBuiltIn = False

            Return CreatePreset(newPreset)
        Catch ex As Exception
            Return False
        End Try
    End Function

End Module