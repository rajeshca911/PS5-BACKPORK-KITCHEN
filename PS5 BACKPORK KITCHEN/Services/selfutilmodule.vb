Imports System.IO
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters

Module selfutilmodule

    Public Function Getselfutilexepath() As String
        Dim selfutilexe As String = Path.Combine(selfutilpath, "selfutil_patched.exe")
        Return selfutilexe
    End Function

    ' ============================================================
    ' PRIMARY: Internal SELF extraction engine (native VB.NET)
    ' ============================================================
    Public Function unpackfile(sourceFile As String, outputElfPath As String) As Boolean

        Try

            If Not File.Exists(sourceFile) Then
                Logger.Log(Form1.rtbStatus, "Source file not found.", Color.Red)
                Return False
            End If

            Dim outDir As String = Path.GetDirectoryName(outputElfPath)
            If Not Directory.Exists(outDir) Then
                Directory.CreateDirectory(outDir)
            End If

            Logger.Log(Form1.rtbStatus, "*** Elf Unpack ***", Color.Blue)
            Logger.Log(Form1.rtbStatus, $"Unpacking → {Path.GetFileName(sourceFile)}", Color.Black)

            Dim fileData As Byte() = File.ReadAllBytes(sourceFile)
            Dim fileType As String = GetFileType(fileData)
            ElfLogger.LogElfDetailsToFile(fileData, Path.GetFileName(sourceFile))
            Dim elfBytes As Byte() = Nothing

            If fileType = "ELF" OrElse fileType = "StrippedELF" Then

                Logger.Log(Form1.rtbStatus, "Input is already a decrypted ELF.", Color.DarkGreen)
                elfBytes = fileData

            ElseIf fileType = "SELF" Then

                Logger.Log(Form1.rtbStatus, "SELF detected. Extracting...", Color.DarkBlue)

                Dim ilogger As ISelfLogger = New ExistingLoggerAdapter(Form1.rtbStatus)
                Dim selfFile As New SelfFile(fileData, ilogger)

                elfBytes = selfFile.ExtractElf()

            Else

                Logger.Log(Form1.rtbStatus, "Unknown or unsupported file type.", Color.Red)
                Return False

            End If

            ' Safety check
            If elfBytes Is Nothing OrElse elfBytes.Length < 64 Then
                Logger.Log(Form1.rtbStatus, "Invalid or empty ELF output.", Color.Red)
                Return False
            End If

            ' Write output
            File.WriteAllBytes(outputElfPath, elfBytes)

            ' Final validation
            If Not File.Exists(outputElfPath) OrElse New FileInfo(outputElfPath).Length = 0 Then
                Logger.Log(Form1.rtbStatus, "Output ELF missing or empty.", Color.Red)
                Return False
            End If

            Logger.Log(Form1.rtbStatus, "*** Elf Unpack End ***", Color.Blue)
            Return True

        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, $"Error unpacking file: {ex.Message}", Color.Red)
            Return False
        End Try

    End Function

    ' ============================================================
    ' FALLBACK: External selfutil_patched.exe (with timeout protection)
    ' ============================================================
    Private Const SELFUTIL_TIMEOUT_MS As Integer = 60000 ' 60 seconds

    Public Function unpackfileExternal(sourcefile As String, outputElfPath As String) As Boolean
        Try
            Dim selfutilexe As String = Getselfutilexepath()
            If Not File.Exists(selfutilexe) Then
                Logger.Log(Form1.rtbStatus, "SelfUtil.exe not found.", Color.Red)
                Return False
            End If

            ' Ensure parent directory exists
            Dim outDir As String = Path.GetDirectoryName(outputElfPath)
            If Not Directory.Exists(outDir) Then
                Directory.CreateDirectory(outDir)
            End If

            Logger.Log(Form1.rtbStatus, "*** Elf Unpack (External) ***", Color.Blue)
            Logger.Log(Form1.rtbStatus, $"Unpacking → {Path.GetFileName(outputElfPath)}", Color.Black)

            Dim startInfo As New ProcessStartInfo() With {
            .FileName = selfutilexe,
            .Arguments = $"--verbose --overwrite --input ""{sourcefile}"" --output ""{outputElfPath}""",
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True
        }

            Dim stdoutText As String = ""
            Dim stderrText As String = ""

            Using proc As Process = Process.Start(startInfo)
                ' Read streams asynchronously to prevent deadlock
                Dim stdoutTask = proc.StandardOutput.ReadToEndAsync()
                Dim stderrTask = proc.StandardError.ReadToEndAsync()

                Dim exited As Boolean = proc.WaitForExit(SELFUTIL_TIMEOUT_MS)

                If Not exited Then
                    ' Process hung (e.g. debug assertion dialog) — kill it
                    Try
                        proc.Kill()
                        proc.WaitForExit(5000)
                    Catch
                        ' Ignore kill errors
                    End Try
                    Logger.Log(Form1.rtbStatus,
                        $"SelfUtil timed out processing {Path.GetFileName(sourcefile)} — the file may be unsupported or corrupted.",
                        Color.Red)
                    Return False
                End If

                stdoutText = If(stdoutTask.Wait(5000), stdoutTask.Result, "")
                stderrText = If(stderrTask.Wait(5000), stderrTask.Result, "")

                If Not String.IsNullOrWhiteSpace(stdoutText) Then
                    Logger.Log(Form1.rtbStatus, stdoutText, Color.Blue)
                End If

                If Not String.IsNullOrWhiteSpace(stderrText) Then
                    Logger.Log(Form1.rtbStatus, stderrText, Color.Red)
                End If

                If proc.ExitCode <> 0 Then
                    Logger.Log(Form1.rtbStatus,
                        $"SelfUtil failed with exit code {proc.ExitCode} for {Path.GetFileName(sourcefile)}",
                        Color.Red)
                    Return False
                End If
            End Using

            ' Final safety check
            If Not File.Exists(outputElfPath) OrElse New FileInfo(outputElfPath).Length = 0 Then
                Logger.Log(Form1.rtbStatus, "Output ELF missing or empty", Color.Red)
                Return False
            End If

            Logger.Log(Form1.rtbStatus, "*** Elf Unpack End ***", Color.Blue)
            Return True
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, $"Error unpacking file: {ex.Message}", Color.Red)
            Return False
        End Try
    End Function

    ' ============================================================
    ' FILE TYPE DETECTION
    ' ============================================================
    Public Function GetFileType(data As Byte()) As String

        If data Is Nothing OrElse data.Length < 4 Then
            Return "Unknown"
        End If

        Dim magic As UInteger = BitConverter.ToUInt32(data, 0)

        Const ELF_MAGIC As UInteger = &H464C457FUI
        Const PS4_SELF As UInteger = &H1D3D154FUI
        Const PS5_SELF As UInteger = &HEEF51454UI

        ' 1️⃣ Raw ELF
        If magic = ELF_MAGIC Then
            Return "ELF"
        End If

        ' 2️⃣ SELF
        If magic = PS4_SELF OrElse magic = PS5_SELF Then
            Return "SELF"
        End If

        ' 3️⃣ Stripped / zeroed ELF detection
        If LooksLikeStrippedElf(data) Then
            Return "StrippedELF"
        End If

        Return "Unsupported"

    End Function

    ' ============================================================
    ' STRIPPED ELF HEURISTIC VALIDATION
    ' ============================================================
    Public Function LooksLikeStrippedElf(data As Byte()) As Boolean

        If data.Length < 64 Then Return False

        Try
            Dim header As Elf64Header =
                BinaryReaderUtil.ToStructure(Of Elf64Header)(data, 0)

            ' Heuristic sanity checks:
            ' - e_ehsize = 64 (ELF64 header size)
            ' - e_phentsize = 56 (Program header entry size for 64-bit)
            ' - e_phnum > 0 and < 100 (reasonable number of program headers)
            ' - e_phoff < file size (program header table within bounds)
            If header.e_ehsize = 64 AndAlso
               header.e_phentsize = 56 AndAlso
               header.e_phnum > 0 AndAlso
               header.e_phnum < 100 AndAlso
               header.e_phoff < data.Length Then
                Return True
            End If

        Catch
            Return False
        End Try

        Return False

    End Function


End Module
