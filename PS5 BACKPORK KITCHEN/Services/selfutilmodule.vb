Imports System.IO

Module selfutilmodule

    Public Function Getselfutilexepath() As String
        Dim selfutilexe As String = Path.Combine(selfutilpath, "selfutil_patched.exe")
        Return selfutilexe
    End Function

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

            Using process As Process = Process.Start(startInfo)
                process.WaitForExit()

                Dim stdout = process.StandardOutput.ReadToEnd()
                Dim stderr = process.StandardError.ReadToEnd()

                If Not String.IsNullOrWhiteSpace(stdout) Then
                    Logger.Log(Form1.rtbStatus, stdout, Color.Blue)
                End If

                If Not String.IsNullOrWhiteSpace(stderr) Then
                    Logger.Log(Form1.rtbStatus, stderr, Color.Red)
                End If

                If process.ExitCode <> 0 Then
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