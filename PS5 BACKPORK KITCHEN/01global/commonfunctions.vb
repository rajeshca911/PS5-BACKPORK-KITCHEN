Imports System.IO
Imports System.Text
Imports System.Text.Json

Module commonfunctions


    Public Sub OpenFolder(folderpath As String)

        Try

            If Not Directory.Exists(folderpath) Then
                Directory.CreateDirectory(folderpath)
            End If

            Dim psi As New ProcessStartInfo With {
                .FileName = folderpath,
                .UseShellExecute = True
            }

            Process.Start(psi)
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus,
                        $"Error opening log folder: {ex.Message}",
                        Color.Red)
            MessageBox.Show($"Error opening log folder: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error)
        End Try

    End Sub

    Public Sub fixmysize(frm As Form)
        On Error Resume Next
        frm.MinimumSize = frm.Size
        frm.MaximumSize = frm.Size
    End Sub

    Public Sub OpenURL(url As String)
        Dim psi As New ProcessStartInfo()
        psi.FileName = url
        psi.UseShellExecute = True
        Process.Start(psi)
    End Sub

    Public Sub updatestatus(Optional status As String = "", Optional clr As Integer = 1)

        If Form1.InvokeRequired Then
            Form1.Invoke(New Action(Sub()
                                        updatestatus(status, clr)
                                    End Sub))
            Return
        End If

        Select Case clr
            Case 1
                Form1.StatusPic.Image = My.Resources.green
                Form1.LblStat.Text = "Idle..!!"

            Case 2
                Form1.StatusPic.Image = My.Resources.Blue
                Form1.LblStat.Text = status

            Case 3
                Form1.StatusPic.Image = My.Resources.RED
                Form1.LblStat.Text = "Error..!!"

            Case Else
                Form1.StatusPic.Image = My.Resources.green
        End Select

        If Not String.IsNullOrEmpty(status) Then
            Form1.LblStat.Text = status
        End If
        Application.DoEvents()

    End Sub

    'Public Function GetFirmwareMajor(ps5Sdk As UInteger) As Integer
    '    Return CInt((ps5Sdk >> 24) And &HFFUI)
    'End Function
    Public Function GetFirmwareMajor(ps5Sdk As UInteger) As Integer
        Return CInt((ps5Sdk >> 24) And &HFFUI)
    End Function

    Public Function ToFirmware(ps5Sdk As UInteger) As String
        Dim majorHex As UInteger = (ps5Sdk >> 24) And &HFFUI
        Dim minorHex As UInteger = (ps5Sdk >> 16) And &HFFUI

        Return $"{majorHex:X}.{minorHex:X2}"
    End Function

    Public Function ReadEssentialInfo(paramJsonPath As String) As ParamInfo

        Dim info As New ParamInfo()

        If Not File.Exists(paramJsonPath) Then
            Return Nothing
        End If

        Dim jsonText = File.ReadAllText(paramJsonPath)

        Using doc As JsonDocument = JsonDocument.Parse(jsonText)
            Dim root = doc.RootElement

            If root.TryGetProperty("contentId", Nothing) Then
                info.ContentId = root.GetProperty("contentId").GetString()
            End If

            If root.TryGetProperty("contentVersion", Nothing) Then
                info.ContentVersion = root.GetProperty("contentVersion").GetString()
            End If

            If root.TryGetProperty("originContentVersion", Nothing) Then
                info.OriginContentVersion = root.GetProperty("originContentVersion").GetString()
            End If

            If root.TryGetProperty("titleId", Nothing) Then
                info.TitleId = root.GetProperty("titleId").GetString()
            End If

            If root.TryGetProperty("localizedParameters", Nothing) Then
                Dim loc = root.GetProperty("localizedParameters")

                If loc.TryGetProperty("en-US", Nothing) Then
                    info.Title = loc.GetProperty("en-US").GetProperty("titleName").GetString()

                ElseIf loc.TryGetProperty("en-GB", Nothing) Then
                    info.Title = loc.GetProperty("en-GB").GetProperty("titleName").GetString()

                ElseIf loc.TryGetProperty("defaultLanguage", Nothing) Then
                    Dim defLang = loc.GetProperty("defaultLanguage").GetString()
                    If loc.TryGetProperty(defLang, Nothing) Then
                        info.Title = loc.GetProperty(defLang).GetProperty("titleName").GetString()
                    End If
                End If
            End If
        End Using

        Return info
    End Function

    Public Class ParamInfo
        Public Property Title As String
        Public Property ContentId As String
        Public Property ContentVersion As String
        Public Property OriginContentVersion As String
        Public Property TitleId As String
    End Class

    Public Sub CopyRelative(
    sourceRoot As String,
    targetRoot As String,
    Optional overwrite As Boolean = True,
    Optional allowedExtensions As String() = Nothing,
    Optional dryRun As Boolean = False
)

        For Each srcFile In Directory.GetFiles(
        sourceRoot, "*.*", SearchOption.AllDirectories)

            Try
                If allowedExtensions IsNot Nothing Then
                    Dim ext = Path.GetExtension(srcFile).ToLower()
                    If Not allowedExtensions.Contains(ext) Then Continue For
                End If

                Dim relativePath As String =
                Path.GetRelativePath(sourceRoot, srcFile)

                Dim destFile As String =
                Path.Combine(targetRoot, relativePath)

                Dim destDir As String = Path.GetDirectoryName(destFile)
                Directory.CreateDirectory(destDir)

                If dryRun Then
                    Logger.Log(Form1.rtbStatus,
                    $"[DRY] {relativePath}", Color.Gray)
                    Continue For
                End If

                File.Copy(srcFile, destFile, overwrite)

                Logger.Log(Form1.rtbStatus,
                $"Copied: {srcFile} → {destFile}", Color.Green)
            Catch ex As Exception
                Logger.Log(Form1.rtbStatus,
                $"Failed: {srcFile} → {ex.Message}", Color.Red)
            End Try

        Next

    End Sub

    Public Sub RestoreRajFiles(rootFolder As String)

        For Each rajFile In Directory.GetFiles(
        rootFolder, "*.raj", SearchOption.AllDirectories)

            Try

                Dim originalFile As String =
                Path.Combine(
                    Path.GetDirectoryName(rajFile),
                    Path.GetFileNameWithoutExtension(rajFile)
                )

                If File.Exists(originalFile) Then
                    Continue For
                End If

                File.Move(rajFile, originalFile)
            Catch ex As Exception
                Logger.Log(Form1.rtbStatus,
                $"Failed to restore: {rajFile} → {ex.Message}",
                Color.Red)
            End Try

        Next

    End Sub

    Public Sub MakeRajFiles(rootFolder As String)

        For Each filePath In Directory.GetFiles(
        rootFolder, "*.*", SearchOption.AllDirectories)

            Try

                If filePath.EndsWith(".raj", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                Dim rajFile As String = filePath & ".raj"

                If File.Exists(rajFile) Then
                    Continue For
                End If

                File.Move(filePath, rajFile)
            Catch ex As Exception
                Logger.Log(Form1.rtbStatus,
                $"Failed to convert to .raj: {filePath} → {ex.Message}",
                Color.Red)
            End Try

        Next

    End Sub

    Public Sub SignAndSaveRelative(
    elfInputPath As String,
    tempRoot As String,
    gameRoot As String
)

        If Not File.Exists(elfInputPath) Then
            Throw New FileNotFoundException("Input ELF not found", elfInputPath)
        End If

        If Not Directory.Exists(tempRoot) Then
            Throw New DirectoryNotFoundException($"Temp root not found: {tempRoot}")
        End If

        If Not Directory.Exists(gameRoot) Then
            Throw New DirectoryNotFoundException($"Game root not found: {gameRoot}")
        End If

        Dim relativePath As String =
        Path.GetRelativePath(tempRoot, elfInputPath)

        Dim destinationPath As String =
        Path.Combine(gameRoot, relativePath)

        Dim destinationDir As String =
        Path.GetDirectoryName(destinationPath)

        Directory.CreateDirectory(destinationDir)

        Dim elf As New ElfFile()
        elf.Load(elfInputPath)

        Dim selfFile As New SignedElfFile(elf)
        'enable if you want backup with .bak extension
        'If File.Exists(destinationPath) Then
        '    File.Copy(destinationPath, destinationPath & ".bak", overwrite:=True)
        '    Logger.Log(Form1.rtbStatus, $"Renamed:{destinationPath & ".bak"}", Color.Black)
        'End If

        selfFile.Save(destinationPath)
        Dim postHash = ComputeSHA256(destinationPath)
        Logger.Log(
    Form1.rtbStatus,
    $"[POST-SIGN] {relativePath} | SHA256={postHash}",
    Color.DarkGreen
)

        Debug.WriteLine($"Signed & replaced: {relativePath}")
        Logger.Log(Form1.rtbStatus, $"Signed & replaced: {relativePath}", Color.DarkMagenta)

    End Sub

    Public Sub SignAndSaveAllRelative(tempRoot As String, gameRoot As String)

        If Not Directory.Exists(tempRoot) OrElse Not Directory.Exists(gameRoot) Then
            Throw New DirectoryNotFoundException("Source or Destination root not found.")
        End If

        Dim allowedExtensions As String() = {".elf", ".bin", ".sprx", ".prx"}

        Dim files = Directory.GetFiles(tempRoot, "*.*", SearchOption.AllDirectories)
        Logger.Log(Form1.rtbStatus, $"Found {files.Length} total files. Filtering for executables...", Color.Blue)
        updatestatus($"Signing: {files.Length}", 2)

        For Each tempFile In files

            Dim relativePath As String = Path.GetRelativePath(tempRoot, tempFile)
            Dim destinationPath As String = Path.Combine(gameRoot, relativePath)
            Dim destinationDir As String = Path.GetDirectoryName(destinationPath)
            Dim ext As String = Path.GetExtension(tempFile).ToLower()
            Dim name = Path.GetFileName(tempFile).ToLower()
            Directory.CreateDirectory(destinationDir)

            If allowedExtensions.Contains(ext) Then

                '   malli check cheyyali
                If IsSignedSelf(tempFile) Then
                    Logger.Log(
                    Form1.rtbStatus,
                    $"[SKIP-SIGN] Already signed SELF: {relativePath}",
                    Color.Orange
                )

                    ' Just copy it as-is
                    File.Copy(tempFile, destinationPath, True)

                    'SendToLogFile($"[SKIP-SIGN] Already signed SELF MD5 = {ComputeMD5(tempFile)}-{name}")
                    Continue For
                End If

                Try
                    Logger.Log(Form1.rtbStatus, $"Signing: {relativePath}", Color.Black)

                    Dim elf As New ElfFile()
                    elf.Load(tempFile)

                    Dim selfFile As New SignedElfFile(elf)
                    selfFile.Save(destinationPath)

                    Logger.Log(Form1.rtbStatus, $"DONE: {relativePath}", Color.DarkMagenta)
                    'SendToLogFile($"[ELF-Signed] MD5 = {ComputeMD5(destinationPath)}-{name}")
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus, $"FAILED to sign {relativePath}: {ex.Message}", Color.Red)
                    'SendToLogFile($"[ELF-Failed] MD5 = {ComputeMD5(destinationPath)}-{name}")
                    Debug.Print($"FAILED to sign {tempFile}: {ex.Message}")
                End Try
            Else
                Logger.Log(Form1.rtbStatus, $"Copied (Skipped Signing): {relativePath}", Color.Gray)
                File.Copy(tempFile, destinationPath, True)
            End If

        Next
        Logger.Log(Form1.rtbStatus, "[PIPELINE-END] Re-checking PRX hashes", Color.Blue)

    End Sub

    Public Sub DebugSignSingleFile(
    patchedElfPath As String,
    outputSelfPath As String
)
        Dim elf As New ElfFile()
        elf.Load(patchedElfPath)

        Dim signed As New SignedElfFile(
        elf,
        paid:=&H3100000000000002UL,
        ptype:=SignedElfExInfo.PTYPE_FAKE,
        appVersion:=0,
        fwVersion:=0
    )

        signed.Save(outputSelfPath)

        Logger.Log(Form1.rtbStatus, "Signed single ELF for hash comparison", Color.Blue)
    End Sub

    Public Function IsSignedSelf(path As String) As Boolean
        Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            If fs.Length < 4 Then Return False

            Dim magic(3) As Byte
            fs.Read(magic, 0, 4)

            ' SELF magic: 4F 15 3D 1D
            Return magic(0) = &H4F AndAlso
               magic(1) = &H15 AndAlso
               magic(2) = &H3D AndAlso
               magic(3) = &H1D
        End Using
    End Function

    Public Sub CopyFileRelative(sourceFile As String, sourceRoot As String, destRoot As String)
        Try

            Dim relativePath As String = Path.GetRelativePath(sourceRoot, sourceFile)

            Dim destinationPath As String = Path.Combine(destRoot, relativePath)

            Dim destinationDir As String = Path.GetDirectoryName(destinationPath)
            If Not Directory.Exists(destinationDir) Then
                Directory.CreateDirectory(destinationDir)
            End If

            File.Copy(sourceFile, destinationPath, True)
        Catch ex As Exception
            Throw New Exception($"Failed to copy {sourceFile} relatively: {ex.Message}")
        End Try
    End Sub

    Public Sub CopyFromsource(sourceroot As String, targetRoot As String)

        Dim allowedExtensions As String() = {".elf", ".prx", ".sprx"}

        For Each tempFile In Directory.GetFiles(sourceroot, "*.*", SearchOption.AllDirectories)
            Dim fileName As String = Path.GetFileName(tempFile).ToLower()
            Dim extension As String = Path.GetExtension(tempFile).ToLower()

            If allowedExtensions.Contains(extension) OrElse fileName = "eboot.bin" Then

                Dim relativePath As String = Path.GetRelativePath(sourceroot, tempFile)

                Dim targetFile As String = Path.Combine(targetRoot, relativePath)

                Dim targetDir As String = Path.GetDirectoryName(targetFile)
                If Not Directory.Exists(targetDir) Then
                    Directory.CreateDirectory(targetDir)
                End If

                File.Copy(tempFile, targetFile, overwrite:=True)

                Logger.Log(Form1.rtbStatus, $"Copied: {tempFile} => {targetFile}", Color.Green)
            End If
        Next
    End Sub

    Public Function ComputeMD5(path As String) As String
        If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
            Return "N/A"
        End If

        Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using md5 = Security.Cryptography.MD5.Create()
                Dim hash = md5.ComputeHash(fs)
                Return BitConverter.ToString(hash).
                Replace("-", "").
                ToLowerInvariant()
            End Using
        End Using
    End Function

    Public Function ComputeSHA256(path As String) As String
        Using fs = File.OpenRead(path)
            Using sha = Security.Cryptography.SHA256.Create()
                Dim hash = sha.ComputeHash(fs)
                Return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
            End Using
        End Using
    End Function

    Public Function FolderHasContents(folderPath As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(folderPath) Then Return False
            If Not Directory.Exists(folderPath) Then Return False

            Return Directory.EnumerateFileSystemEntries(folderPath).Any()
        Catch ex As UnauthorizedAccessException
            Logger.Log(Form1.rtbStatus, $"Access denied to folder: {folderPath}", Color.Red)
            Return False
        Catch ex As IOException
            Logger.Log(Form1.rtbStatus, $"I/O error accessing folder: {folderPath}", Color.Red)
            Return False
        Catch
            Logger.Log(Form1.rtbStatus, $"Error accessing folder: {folderPath}", Color.Red)
            Return False
        End Try
    End Function

    Public Sub PatchPrxString(
     prxPath As String,
     oldBytes As Byte(),
     newBytes As Byte()
 )
        If Not File.Exists(prxPath) Then
            Throw New FileNotFoundException("PRX file not found.")
        End If

        If oldBytes.Length <> newBytes.Length Then
            Throw New ArgumentException("Replacement must be same length.")
        End If

        Dim data As Byte() = File.ReadAllBytes(prxPath)

        ' vethuku babu vethuku
        For i As Integer = 0 To data.Length - oldBytes.Length

            ' allow both old & new
            If data(i) <> oldBytes(0) AndAlso data(i) <> newBytes(0) Then Continue For

            ' ---- already patched check ----
            Dim alreadyPatched As Boolean = True
            For j As Integer = 0 To newBytes.Length - 1
                If data(i + j) <> newBytes(j) Then
                    alreadyPatched = False
                    Exit For
                End If
            Next

            If alreadyPatched Then
                'SendToLogFile($"[6XX-LIBC-PATCH] PRX already patched for 6xx SDK MD5={ComputeMD5(prxPath)}")
                Logger.Log(Form1.rtbStatus,
                   $"PRX already patched @ 0x{i:X}",
                   Color.Purple)
                Exit Sub
            End If

            ' matching chek
            Dim match As Boolean = True
            For j As Integer = 1 To oldBytes.Length - 1
                If data(i + j) <> oldBytes(j) Then
                    match = False
                    Exit For
                End If
            Next

            If match Then
                File.Copy(prxPath, prxPath & ".bak", True)
                Buffer.BlockCopy(newBytes, 0, data, i, newBytes.Length)
                File.WriteAllBytes(prxPath, data)
                'SendToLogFile($"[6XX-LIBC-PATCH] Patched libc.prx strings for 6xx SDK MD5={ComputeMD5(prxPath)}")
                Logger.Log(Form1.rtbStatus,
                   $"Patched PRX at offset 0x{i:X}
                   Old '{Encoding.ASCII.GetString(oldBytes)}'
                   New '{Encoding.ASCII.GetString(newBytes)}'",
                   Color.Green)
                Exit Sub
            End If
        Next

        Logger.Log(
            Form1.rtbStatus,
            "Patch pattern not found.",
            Color.Red
        )
        'SendToLogFile($"[6XX-LIBC-PATCH] Patch pattern not found in {prxPath} MD5={ComputeMD5(prxPath)}")
    End Sub

    Public Sub LoadLogs()
        Dim frm As New notification With {
            .Text = "Log Viewer"
        }

        frm.lblheading.Text = "Backport Log File"

        Dim rtb = frm.RichTextBox1
        rtb.ReadOnly = True
        rtb.Clear()

        Try
            If Not File.Exists(LOG_FILENAME) Then
                rtb.Text = "⚠ No log file found."
            Else
                'opens safe no error even if file is locked by another process
                Using fs As New FileStream(
     LOG_FILENAME,
     FileMode.Open,
     FileAccess.Read,
     FileShare.ReadWrite
 )
                    Using sr As New StreamReader(fs)
                        rtb.Text = sr.ReadToEnd()
                    End Using
                End Using

            End If

            ' Scroll to latest entry
            rtb.SelectionStart = rtb.TextLength
            rtb.ScrollToCaret()
        Catch ex As Exception
            rtb.Text = "❌ Failed to load log file." & Environment.NewLine & ex.Message
        End Try

        frm.ShowDialog()
    End Sub

End Module