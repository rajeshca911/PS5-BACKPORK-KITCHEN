Imports System.IO
Imports System.Text

Module ElfReader

    Public Function ShouldPatch(
        currentPs5 As UInteger,
        targetPs5 As UInteger
    ) As Boolean
        Return currentPs5 > targetPs5
    End Function

    Public Function HasElfMagic(fs As FileStream) As Boolean
        Dim magic(3) As Byte
        fs.Read(magic, 0, 4)
        fs.Position = 0

        Return magic(0) = &H7F AndAlso
           magic(1) = &H45 AndAlso
           magic(2) = &H4C AndAlso
           magic(3) = &H46
    End Function

    Public Function TryPatchElf(
    elfPath As String,
    newPs5 As UInteger,
    newPs4 As UInteger,
    ByRef msg As String
) As Boolean

        Try
            Using fs As New FileStream(
            elfPath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite)

                If Not HasElfMagic(fs) Then
                    msg = "Not an ELF file"
                    Return False
                End If

                Return ElfPatcher.PatchFile(fs, newPs5, newPs4, msg)
            End Using
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, $"Patch failed: {elfPath}")
            Return False
        End Try

    End Function

    Public Function PatchElfSmart(
    filePath As String,
    newPs5 As UInteger,
    newPs4 As UInteger
) As Boolean

        Dim msg As String = ""
        Dim tempElf = GetTempElfPath(filePath)

        ' 1 Try direct patch (already ELF)
        If TryPatchElf(filePath, newPs5, newPs4, msg) Then
            'SendToLogFile($"[ELF-PATCHED] Patched directly (already ELF)")
            Logger.Log(Form1.rtbStatus, "Patched directly (already ELF)", Color.Green)
            Return True
        End If

        ' 2 Decrypt to UNIQUE temp ELF
        Logger.Log(Form1.rtbStatus, "Decrypting SELF → ELF...", Color.Orange)

        If Not unpackfile(filePath, tempElf) Then
            Logger.Log(Form1.rtbStatus, "SelfUtil decrypt failed", Color.Red)
            Return False
        End If

        If Not File.Exists(tempElf) OrElse New FileInfo(tempElf).Length = 0 Then
            Logger.Log(Form1.rtbStatus, "Temp ELF missing or empty", Color.Red)
            Return False
        End If

        ' 3 Patch decrypted ELF
        If Not TryPatchElf(tempElf, newPs5, newPs4, msg) Then
            Logger.Log(Form1.rtbStatus, "Patch failed after decrypt", Color.Red)
            File.Delete(tempElf)
            Return False

        End If
        'SendToLogFile($"[ELF-PATCHED] MD5 = {ComputeMD5(tempElf)}- {Path.GetFileName(filePath).ToLower()}")
        ' 4 Copy patched ELF back to original location
        SignSingleElf(tempElf)

        ' 5 Copy patched ELF back to original location
        File.Copy(tempElf, filePath, True)
        Debug.Print($"Patched ELF copied back to {filePath}")
        'addiional setting enable for 6xx
        Dim expermental6xx As Boolean = Form1.chklibcpatch.Checked
        If expermental6xx Then
            ' 5.1 Patch PRX strings for 6xx SDKs
            'check file name is libc.prx
            Dim filename As String = Path.GetFileName(filePath).ToLower()
            If filename = "libc.prx" Then
                'SendToLogFile($"[6XX-LIBC-PATCH] Patching libc.prx strings for 6xx SDK")
                'SendToLogFile(filename)
                PatchPrxString(filePath, Encoding.ASCII.GetBytes("4h6F1LLbTiw#A#B"), Encoding.ASCII.GetBytes("IWIBBdTHit4#A#B"))
            End If

        End If

        ' 6 Cleanup
        File.Delete(tempElf)

        Logger.Log(Form1.rtbStatus, "Patched successfully after decrypt", Color.Green)
        Return True
    End Function

    Public Sub SignSingleElf(patchedElfPath As String)
        If IsSignedSelf(patchedElfPath) Then
            'SendToLogFile($"[SKIP-SIGN] Already signed SELF MD5 = {ComputeMD5(patchedElfPath)} - {Path.GetFileName(patchedElfPath)}")
            Return
        End If

        Dim elf As New ElfFile()
        elf.Load(patchedElfPath)

        Dim signed As New SignedElfFile(elf)
        signed.Save(patchedElfPath)

        'SendToLogFile($"[ELF-SIGNED] MD5 = {ComputeMD5(patchedElfPath)} - {Path.GetFileName(patchedElfPath)}")
    End Sub

    Private Function GetTempElfPath(originalPath As String) As String
        Dim name = Path.GetFileName(originalPath)
        Return Path.Combine(TempUnpackFolder, name & ".elf")
    End Function

End Module