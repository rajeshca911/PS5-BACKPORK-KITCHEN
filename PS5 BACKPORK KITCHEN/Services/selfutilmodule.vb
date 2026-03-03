Imports System.IO
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Infrastructure.Adapters

Module selfutilmodule

    Public Function Getselfutilexepath() As String
        Dim selfutilexe As String = Path.Combine(selfutilpath, "selfutil_patched.exe")
        Return selfutilexe
    End Function

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
            Dim elfBytes As Byte() = Nothing

            If fileType = "ELF" OrElse fileType = "StrippedELF" Then
                Logger.Log(Form1.rtbStatus, "Input is already a decrypted ELF.", Color.DarkGreen)
                elfBytes = fileData

            ElseIf fileType = "SELF" Then
                Logger.Log(Form1.rtbStatus, "SELF detected. Extracting...", Color.DarkBlue)

                ' Try internal SelfFile engine first
                Try
                    Dim ilogger As ISelfLogger = New ExistingLoggerAdapter(Form1.rtbStatus)
                    Dim selfFile As New SelfFile(fileData, ilogger)
                    elfBytes = selfFile.ExtractElf()
                Catch ex As Exception
                    Logger.Log(Form1.rtbStatus, $"Internal extraction failed: {ex.Message}", Color.DarkOrange)
                    elfBytes = Nothing
                End Try

                ' Fallback to external selfutil if internal failed
                If elfBytes Is Nothing OrElse elfBytes.Length < 64 Then
                    Dim selfutilexe As String = Getselfutilexepath()
                    If File.Exists(selfutilexe) Then
                        Logger.Log(Form1.rtbStatus, "Trying external selfutil...", Color.DarkBlue)
                        Return UnpackWithExternalTool(selfutilexe, sourceFile, outputElfPath)
                    Else
                        Logger.Log(Form1.rtbStatus, "SELF extraction failed (no external selfutil available)", Color.Red)
                        Return False
                    End If
                End If
            Else
                Logger.Log(Form1.rtbStatus, $"Unknown or unsupported file type: {fileType}", Color.Red)
                Return False
            End If

            ' Safety check
            If elfBytes Is Nothing OrElse elfBytes.Length < 64 Then
                Logger.Log(Form1.rtbStatus, "Invalid or empty ELF output.", Color.Red)
                Return False
            End If

            File.WriteAllBytes(outputElfPath, elfBytes)

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

    Private Function UnpackWithExternalTool(selfutilexe As String, sourceFile As String, outputElfPath As String) As Boolean
        Try
            Dim startInfo As New ProcessStartInfo() With {
                .FileName = selfutilexe,
                .Arguments = $"--verbose --overwrite --input ""{sourceFile}"" --output ""{outputElfPath}""",
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True
            }

            Using proc As Process = Process.Start(startInfo)
                proc.WaitForExit()
                Dim stdout = proc.StandardOutput.ReadToEnd()
                Dim stderr = proc.StandardError.ReadToEnd()
                If Not String.IsNullOrWhiteSpace(stdout) Then Logger.Log(Form1.rtbStatus, stdout, Color.Blue)
                If Not String.IsNullOrWhiteSpace(stderr) Then Logger.Log(Form1.rtbStatus, stderr, Color.Red)
                If proc.ExitCode <> 0 Then Return False
            End Using

            If Not File.Exists(outputElfPath) OrElse New FileInfo(outputElfPath).Length = 0 Then
                Return False
            End If

            Logger.Log(Form1.rtbStatus, "*** Elf Unpack End ***", Color.Blue)
            Return True
        Catch ex As Exception
            Logger.Log(Form1.rtbStatus, $"External selfutil error: {ex.Message}", Color.Red)
            Return False
        End Try
    End Function


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
    Public Function LooksLikeStrippedElf(data As Byte()) As Boolean

        If data.Length < 64 Then Return False

        Try
            Dim header As Elf64Header =
                BinaryReaderUtil.ToStructure(Of Elf64Header)(data, 0)

            ' Heuristic sanity checks
            If header.e_ehsize = 64 AndAlso
               header.e_phentsize = 56 AndAlso
               header.e_phnum > 0 AndAlso
               header.e_phnum < 100 AndAlso
               header.e_phoff < data.Length Then
                Debug.Print("Stripped Elf")
                Return True
            End If

        Catch
            Return False
        End Try

        Return False

    End Function


End Module