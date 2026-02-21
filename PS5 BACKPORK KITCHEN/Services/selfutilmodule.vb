Imports System.IO

Module selfutilmodule

    Public Function Getselfutilexepath() As String
        Dim selfutilexe As String = Path.Combine(selfutilpath, "selfutil_patched.exe")
        Return selfutilexe
    End Function

    Private Const SELFUTIL_TIMEOUT_MS As Integer = 60000 ' 60 seconds

    Public Function unpackfile(sourcefile As String, outputElfPath As String) As Boolean
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

            Logger.Log(Form1.rtbStatus, "*** Elf Unpack ***", Color.Blue)
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

End Module