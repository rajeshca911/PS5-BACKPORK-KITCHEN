Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks

''' <summary>
''' Launches Python scripts and streams their output line-by-line.
''' Searches for a Python interpreter in: bundled python/ folder, .venv, PATH.
''' </summary>
Public Class PythonRunner

    ' ---------------------------------------------------------------------------
    ' Python discovery
    ' ---------------------------------------------------------------------------

    ''' <summary>
    ''' Returns the path to a usable python executable, or Nothing if not found.
    ''' Search order: bundled python/ sub-folder, virtual-env .venv, PATH.
    ''' </summary>
    Public Shared Function FindPython() As String
        Dim appDir As String = AppDomain.CurrentDomain.BaseDirectory

        ' 1. Bundled interpreter shipped alongside the app
        Dim bundled As String = Path.Combine(appDir, "python", "python.exe")
        If File.Exists(bundled) Then Return bundled

        ' 2. Virtual environment created next to the app
        Dim venv As String = Path.Combine(appDir, ".venv", "Scripts", "python.exe")
        If File.Exists(venv) Then Return venv

        ' 3. System PATH — try common executable names
        For Each candidate As String In New String() {"python", "python3", "py"}
            Try
                Dim info As New ProcessStartInfo() With {
                    .FileName = candidate,
                    .Arguments = "--version",
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True
                }
                Dim proc As Process = Process.Start(info)
                If proc IsNot Nothing Then
                    proc.WaitForExit(3000)
                    Dim code As Integer = 0
                    Try
                        code = proc.ExitCode
                    Catch
                    End Try
                    proc.Dispose()
                    If code = 0 Then Return candidate
                End If
            Catch
                ' not found — try next candidate
            End Try
        Next

        Return Nothing
    End Function

    ' ---------------------------------------------------------------------------
    ' Async execution (without cancellation)
    ' ---------------------------------------------------------------------------

    ''' <summary>
    ''' Run a Python script and stream stdout/stderr line-by-line.
    ''' </summary>
    Public Shared Function RunAsync(
        scriptPath As String,
        args As String,
        onOutput As Action(Of String),
        onError As Action(Of String)
    ) As Task(Of Integer)
        Return RunAsync(scriptPath, args, onOutput, onError, CancellationToken.None)
    End Function

    ' ---------------------------------------------------------------------------
    ' Async execution (with cancellation)
    ' ---------------------------------------------------------------------------

    ''' <summary>
    ''' Run a Python script and stream stdout/stderr line-by-line, with cancellation support.
    ''' </summary>
    Public Shared Async Function RunAsync(
        scriptPath As String,
        args As String,
        onOutput As Action(Of String),
        onError As Action(Of String),
        ct As CancellationToken
    ) As Task(Of Integer)

        Dim python As String = FindPython()
        If python Is Nothing Then
            If onError IsNot Nothing Then
                onError.Invoke("[ERROR] Python interpreter not found. Install Python 3.9+ or place python.exe in the 'python' sub-folder next to the app.")
            End If
            Return -1
        End If

        If Not File.Exists(scriptPath) Then
            If onError IsNot Nothing Then
                onError.Invoke("[ERROR] Script not found: " & scriptPath)
            End If
            Return -1
        End If

        Dim workDir As String = Path.GetDirectoryName(scriptPath)

        Dim psi As New ProcessStartInfo() With {
            .FileName = python,
            .Arguments = """" & scriptPath & """ " & args,
            .WorkingDirectory = workDir,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True,
            .StandardOutputEncoding = Text.Encoding.UTF8,
            .StandardErrorEncoding = Text.Encoding.UTF8
        }
        psi.Environment("PYTHONIOENCODING") = "utf-8"
        psi.Environment("PYTHONUNBUFFERED") = "1"
        psi.Environment("PYTHONDONTWRITEBYTECODE") = "1"

        Dim proc As New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}
        proc.Start()

        ' Cancel: kill process when token is signalled
        Dim ctReg As CancellationTokenRegistration = Nothing
        If ct <> CancellationToken.None Then
            ctReg = ct.Register(Sub()
                                    Try
                                        If Not proc.HasExited Then proc.Kill()
                                    Catch
                                    End Try
                                End Sub)
        End If

        Dim stdoutTask As Task = ReadStreamAsync(proc.StandardOutput, onOutput, ct)
        Dim stderrTask As Task = ReadStreamAsync(proc.StandardError, onError, ct)

        Await Task.WhenAll(stdoutTask, stderrTask)
        proc.WaitForExit()

        Dim exitCode As Integer = 0
        Try
            exitCode = proc.ExitCode
        Catch
        End Try

        ctReg.Dispose()
        proc.Dispose()
        Return exitCode
    End Function

    ' ---------------------------------------------------------------------------
    ' Synchronous execution (OutputDataReceived events, no async state machine)
    ' ---------------------------------------------------------------------------

    ''' <summary>
    ''' Run a Python script synchronously, streaming output via OutputDataReceived.
    ''' Intended to be called from Task.Run(Sub() ...) so the UI stays responsive.
    ''' Returns the process exit code.
    ''' </summary>
    Public Shared Function RunSync(
        scriptPath As String,
        args As String,
        onOutput As Action(Of String),
        onError As Action(Of String),
        ct As CancellationToken
    ) As Integer

        Dim python As String = FindPython()
        If python Is Nothing Then
            If onOutput IsNot Nothing Then
                onOutput.Invoke("[ERROR] Python interpreter not found. Install Python 3.9+ or place python.exe in the 'python' sub-folder next to the app.")
            End If
            Return -1
        End If

        If Not File.Exists(scriptPath) Then
            If onError IsNot Nothing Then
                onError.Invoke("[ERROR] Script not found: " & scriptPath)
            End If
            Return -1
        End If

        Dim workDir As String = Path.GetDirectoryName(scriptPath)

        Dim psi As New ProcessStartInfo() With {
            .FileName = python,
            .Arguments = """" & scriptPath & """ " & args,
            .WorkingDirectory = workDir,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True,
            .StandardOutputEncoding = Text.Encoding.UTF8,
            .StandardErrorEncoding = Text.Encoding.UTF8
        }
        ' Force Python to use UTF-8 for stdout/stderr so Unicode chars (arrows, etc.) don't crash
        psi.Environment("PYTHONIOENCODING") = "utf-8"
        psi.Environment("PYTHONUNBUFFERED") = "1"
        psi.Environment("PYTHONDONTWRITEBYTECODE") = "1"

        Using proc As New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}
            AddHandler proc.OutputDataReceived, Sub(s As Object, ev As DataReceivedEventArgs)
                                                   If ev.Data IsNot Nothing AndAlso onOutput IsNot Nothing Then
                                                       onOutput.Invoke(ev.Data)
                                                   End If
                                               End Sub
            AddHandler proc.ErrorDataReceived, Sub(s As Object, ev As DataReceivedEventArgs)
                                                   If ev.Data IsNot Nothing AndAlso onError IsNot Nothing Then
                                                       onError.Invoke(ev.Data)
                                                   End If
                                               End Sub

            proc.Start()
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            ' Register cancellation: kill process when token fires
            Dim ctReg As CancellationTokenRegistration = Nothing
            If ct <> CancellationToken.None Then
                ctReg = ct.Register(Sub()
                                        Try
                                            If Not proc.HasExited Then proc.Kill()
                                        Catch
                                        End Try
                                    End Sub)
            End If

            proc.WaitForExit()
            ctReg.Dispose()

            Try
                Return proc.ExitCode
            Catch
                Return -1
            End Try
        End Using
    End Function

    ' ---------------------------------------------------------------------------
    ' Stream reader
    ' ---------------------------------------------------------------------------

    Private Shared Async Function ReadStreamAsync(
        reader As StreamReader,
        callback As Action(Of String),
        ct As CancellationToken
    ) As Task
        Do
            If ct <> CancellationToken.None AndAlso ct.IsCancellationRequested Then Exit Do
            Dim line As String = Await reader.ReadLineAsync()
            If line Is Nothing Then Exit Do
            If callback IsNot Nothing Then callback.Invoke(line)
        Loop
    End Function

End Class
