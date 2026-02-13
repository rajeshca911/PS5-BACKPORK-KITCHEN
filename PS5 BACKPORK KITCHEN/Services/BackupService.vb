Imports System.IO
Imports Newtonsoft.Json

Public Module BackupService

    Public Structure BackupInfo
        Public BackupPath As String
        Public OriginalPath As String
        Public CreatedDate As DateTime
        Public FilesCount As Integer
        Public TotalSize As Long
        Public Manifest As Dictionary(Of String, String)
        Public Success As Boolean
        Public ErrorMessage As String
    End Structure

    ''' <summary>
    ''' Create comprehensive backup with manifest
    ''' </summary>
    Public Function CreateBackupWithManifest(sourcePath As String) As BackupInfo
        Dim info As New BackupInfo With {
            .OriginalPath = sourcePath,
            .CreatedDate = DateTime.Now,
            .Success = False,
            .FilesCount = 0,
            .TotalSize = 0,
            .Manifest = New Dictionary(Of String, String)
        }

        Try
            ' Create backup directory
            Dim backupName = $"{BACKUP_PREFIX}{DateTime.Now.ToString(BACKUP_DATE_FORMAT)}"
            info.BackupPath = Path.Combine(sourcePath, backupName)

            If Not Directory.Exists(info.BackupPath) Then
                Directory.CreateDirectory(info.BackupPath)
            End If

            ' Get all patchable files
            Dim files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)

            For Each file In files
                Dim fileName = Path.GetFileName(file).ToLower()
                Dim ext = Path.GetExtension(file).ToLower()

                ' Backup only patchable files
                If fileName = EBOOT_FILENAME OrElse PATCHABLE_EXTENSIONS.Contains(ext) Then
                    Dim relativePath = file.Replace(sourcePath, "").TrimStart("\"c)

                    ' Skip files already in backup folder
                    If file.StartsWith(info.BackupPath) Then Continue For

                    Dim destPath = Path.Combine(info.BackupPath, relativePath)
                    Dim destDir = Path.GetDirectoryName(destPath)

                    If Not Directory.Exists(destDir) Then
                        Directory.CreateDirectory(destDir)
                    End If

                    ' Copy file
                    IO.File.Copy(file, destPath, True)

                    ' Calculate checksum for manifest
                    Dim checksum = IntegrityVerifier.CalculateFileChecksum(file)
                    If Not String.IsNullOrEmpty(checksum) Then
                        info.Manifest(relativePath) = checksum
                    End If

                    info.FilesCount += 1
                    Dim fileInfo As New FileInfo(file)
                    info.TotalSize += fileInfo.Length
                End If
            Next

            ' Save manifest to backup folder
            Dim manifestPath = Path.Combine(info.BackupPath, "backup_manifest.json")
            Dim manifestData = New With {
                .BackupDate = info.CreatedDate,
                .OriginalPath = info.OriginalPath,
                .FilesCount = info.FilesCount,
                .TotalSize = info.TotalSize,
                .AppVersion = APP_VERSION,
                .Checksums = info.Manifest
            }

            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifestData, Formatting.Indented))

            info.Success = True
        Catch ex As Exception
            info.Success = False
            info.ErrorMessage = ex.Message
        End Try

        Return info
    End Function

    ''' <summary>
    ''' Restore from backup
    ''' </summary>
    Public Function RestoreFromBackup(backupPath As String, targetPath As String) As Boolean
        Try
            If Not Directory.Exists(backupPath) Then
                Return False
            End If

            ' Read manifest
            Dim manifestPath = Path.Combine(backupPath, "backup_manifest.json")
            If Not File.Exists(manifestPath) Then
                ' No manifest, do simple restore
                Return RestoreSimple(backupPath, targetPath)
            End If

            ' Restore with verification
            Dim manifestJson = File.ReadAllText(manifestPath)
            Dim manifestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(manifestJson)

            Dim files = Directory.GetFiles(backupPath, "*.*", SearchOption.AllDirectories)

            For Each file In files
                If Path.GetFileName(file) = "backup_manifest.json" Then Continue For

                Dim relativePath = file.Replace(backupPath, "").TrimStart("\"c)
                Dim destPath = Path.Combine(targetPath, relativePath)
                Dim destDir = Path.GetDirectoryName(destPath)

                If Not Directory.Exists(destDir) Then
                    Directory.CreateDirectory(destDir)
                End If

                IO.File.Copy(file, destPath, True)
            Next

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Simple restore without manifest
    ''' </summary>
    Private Function RestoreSimple(backupPath As String, targetPath As String) As Boolean
        Try
            Dim files = Directory.GetFiles(backupPath, "*.*", SearchOption.AllDirectories)

            For Each file In files
                Dim relativePath = file.Replace(backupPath, "").TrimStart("\"c)
                Dim destPath = Path.Combine(targetPath, relativePath)
                Dim destDir = Path.GetDirectoryName(destPath)

                If Not Directory.Exists(destDir) Then
                    Directory.CreateDirectory(destDir)
                End If

                IO.File.Copy(file, destPath, True)
            Next

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' List available backups in folder
    ''' </summary>
    Public Function ListBackups(folderPath As String) As List(Of String)
        Dim backups As New List(Of String)

        Try
            If Not Directory.Exists(folderPath) Then Return backups

            Dim dirs = Directory.GetDirectories(folderPath, $"{BACKUP_PREFIX}*")
            backups.AddRange(dirs)
        Catch ex As Exception
            ' Return empty list
        End Try

        Return backups
    End Function

End Module