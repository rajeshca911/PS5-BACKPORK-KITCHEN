Imports System.IO
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters
Public Enum PatchStatus
    Patched
    Skipped
    Failed
End Enum

Public Class ElfPatcher

    Public Shared Function PatchFile(
        fs As FileStream,
        newPs5 As UInteger,
        newPs4 As UInteger,
        ByRef message As String
    ) As Boolean

        Dim segmentCount = ReadUInt(fs, PHT_COUNT_OFFSET, PHT_COUNT_SIZE)
        Dim phtOffset = ReadUInt(fs, PHT_OFFSET_OFFSET, PHT_OFFSET_SIZE)

        For i As Integer = 0 To CInt(segmentCount) - 1

            Dim entryBase = CLng(phtOffset) + i * PHDR_ENTRY_SIZE
            Dim segType = ReadUInt(fs, entryBase + PHDR_TYPE_OFFSET, PHDR_TYPE_SIZE)

            If segType <> PT_SCE_PROCPARAM AndAlso segType <> PT_SCE_MODULE_PARAM Then Continue For

            Dim segOffset = ReadUInt(fs, entryBase + PHDR_OFFSET_OFFSET, PHDR_OFFSET_SIZE)
            Dim paramSize = ReadUInt(fs, CLng(segOffset), 4)

            If paramSize < SCE_PARAM_PS5_SDK_OFFSET + SCE_PARAM_PS_VERSION_SIZE Then
                message = "Invalid param size"
                Return False
            End If

            Dim magic = ReadUInt(fs, CLng(segOffset) + SCE_PARAM_MAGIC_OFFSET, SCE_PARAM_MAGIC_SIZE)

            If segType = PT_SCE_PROCPARAM AndAlso magic <> SCE_PROCESS_PARAM_MAGIC Then Return False
            If segType = PT_SCE_MODULE_PARAM AndAlso magic <> SCE_MODULE_PARAM_MAGIC Then Return False

            Dim oldPs5 = ReadUInt(fs, CLng(segOffset) + SCE_PARAM_PS5_SDK_OFFSET, 4)
            Dim oldPs4 = ReadUInt(fs, CLng(segOffset) + SCE_PARAM_PS4_SDK_OFFSET, 4)

            WriteUInt(fs, CLng(segOffset) + SCE_PARAM_PS5_SDK_OFFSET, 4, newPs5)
            WriteUInt(fs, CLng(segOffset) + SCE_PARAM_PS4_SDK_OFFSET, 4, newPs4)

            message = $"Patched PS5 {oldPs5:X8} → {newPs5:X8}"
            Return True
        Next

        message = "No patchable segment found"
        Return False
    End Function
    Public Shared Function PatchSingleFile(
     filePath As String,
     targetPs5 As UInteger,
     targetPs4 As UInteger,
     ByRef logMessage As String
 ) As PatchStatus


        Dim info = ElfInspector.ReadInfo(filePath)
        Logger.Log(Form1.rtbStatus, $"Elf: {filePath}", Color.Purple)
        If Not info.IsPatchable Then
            logMessage = "Skipped (not patchable)"
            Return PatchStatus.Skipped
        End If
        logMessage = $"SDK check: current={info.Ps5SdkVersion:X8} target={targetPs5:X8}{vbCrLf}"
        Debug.Print($"SDK check: current={info.Ps5SdkVersion:X8} target={targetPs5:X8}{vbCrLf}")

        If Not ShouldPatch(info.Ps5SdkVersion.Value, targetPs5) Then
            logMessage = $"Skipped (already ≤ target SDK {info.Ps5SdkVersion:X8})"
            Return PatchStatus.Skipped
        End If

        'If createBackup Then
        '    Dim bak = filePath & ".bak"
        '    If Not File.Exists(bak) Then
        '        File.Copy(filePath, bak)
        '    End If
        'End If

        Using fs As New FileStream(filePath, FileMode.Open, FileAccess.ReadWrite)
            Dim msg As String = ""
            If Not ElfPatcher.PatchFile(fs, targetPs5, targetPs4, msg) Then
                logMessage = "Patch failed"
                Return PatchStatus.Failed
            End If
        End Using

        logMessage = $"Patched {info.Ps5SdkVersion:X8} → {targetPs5:X8}"
        Return PatchStatus.Patched

    End Function
    'Public Shared Function PatchSingleFile(
    '    filePath As String,
    '    targetPs5 As UInteger,
    '    targetPs4 As UInteger,
    '    ByRef logMessage As String
    ') As Boolean

    '    Dim info = ElfInspector.ReadInfo(filePath)
    '    Logger.Log(Form1.rtbStatus, $"Elf: {filePath}", Color.Purple)
    '    If Not info.IsPatchable Then
    '        logMessage = "Skipped (not patchable)"
    '        Return False
    '    End If

    '    If Not ShouldPatch(info.Ps5SdkVersion.Value, targetPs5) Then
    '        logMessage = $"Skipped (already ≤ target SDK {info.Ps5SdkVersion:X8})"
    '        Return False
    '    End If

    '    'If createBackup Then
    '    '    Dim bak = filePath & ".bak"
    '    '    If Not File.Exists(bak) Then
    '    '        File.Copy(filePath, bak)
    '    '    End If
    '    'End If

    '    Using fs As New FileStream(filePath, FileMode.Open, FileAccess.ReadWrite)
    '        Dim msg As String = ""
    '        If Not ElfPatcher.PatchFile(fs, targetPs5, targetPs4, msg) Then
    '            logMessage = "Patch failed"
    '            Return False
    '        End If
    '    End Using

    '    logMessage = $"Patched {info.Ps5SdkVersion:X8} → {targetPs5:X8}"
    '    Return True
    'End Function

    Public Shared Sub PatchFolder(
        folderPath As String,
        targetPs5 As UInteger,
        targetPs4 As UInteger,
        createBackup As Boolean,
        log As Action(Of String)
    )
        updatestatus($"Patching:{folderPath}", 2)
        Dim files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
        backupDir = Path.Combine(selectedfolder, $"Backup-{Now.ToString("dd-MMM-yy-HH-mm-ss")}")
        For Each file In files

            Dim name = Path.GetFileName(file).ToLower()
            Dim ext = Path.GetExtension(file).ToLower()

            ' ---- Filter ----
            If name <> "eboot.bin" AndAlso
               ext <> ".prx" AndAlso
               ext <> ".sprx" Then
                Continue For
            End If

            Dim resultMsg As String = ""
            'Dim patched = PatchSingleFile(
            '    file,
            '    targetPs5,
            '    targetPs4,
            '    createBackup,
            '    resultMsg
            ')
            'SendToLogFile($"[ELF-SOURCE] MD5 = {ComputeMD5(file)}-{name}")
            Dim patched = PatchElfSmart(file, targetPs5, targetPs4)
            Logger.Log(Form1.rtbStatus, $"Elf: {file}", Color.Purple)
            updatestatus($"Patching:{name}", 3)
            If patched Then

                log($"[PATCHED] {name} → {resultMsg}")
                updatestatus($"[PATCHED] {name}", 2)
                patchedcount += 1
            Else
                'SendToLogFile($"[ELF-SKIPPED] MD5 = {ComputeMD5(file)}-{name}")
                log($"[SKIPPED] {name} → {resultMsg}")
                updatestatus($"[SKIPPED] {name}", 3)
                skippedcount += 1
            End If
        Next

    End Sub

End Class