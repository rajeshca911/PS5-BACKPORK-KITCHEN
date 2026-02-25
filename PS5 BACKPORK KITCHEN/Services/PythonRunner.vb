Imports System.Diagnostics
Imports System.IO
Imports System.Threading

''' <summary>
''' Launches Python scripts and streams their output line-by-line.
''' Searches for a Python interpreter in: PATH, venv, bundled python/ folder.
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
        Dim appDir = AppDomain.CurrentDomain.BaseDirectory

        ' 1. Bundled python (shipped alongside the app)
        Dim bundled As String = Path.Combine(appDir, "python", "python.exe")
        If File.Exists(bundled) Then Return bundled

        ' 2. Virtual environment created next to the app
        Dim venv As String = Path.Combine(appDir, ".venv", "Scripts", "python.exe")
        If File.Exists(venv) Then Return venv

        ' 3. System PATH
        For Each candidate In {"python", "python3", "py"}
            Try
                Dim info As New ProcessStartInfo(candidate, "--version") With {
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True
                }
                Using proc = Process.Start(info)
                    proc.WaitForExit(3000)
                    If proc.ExitCode = 0 Then Return candidate
                End Using
            Catch
                ' Not found in PATH — try next
            End Try
        Next

        Return Nothing
    End Function

    ' ---------------------------------------------------------------------------
    ' Async execution
    ' ---------------------------------------------------------------------------

    ''' <summary>
    ''' Run a Python script and stream stdout/stderr line-by-line.
    ''' </summary>
    ''' <param name="scriptPath">Absolute path to the .py script.</param>
    ''' <param name="args">Command-line arguments string.</param>
    ''' <param name="onOutput">Callback invoked for each stdout line.</param>
    ''' <param name="onError">Callback invoked for each stderr line.</param>
    ''' <param name="ct">CancellationToken to abort the process.</param>
    ''' <returns>Process exit code (0 = success).</returns>
    Public Shared Async Function RunAsync(
        scriptPath As String,
        args As String,
        onOutput As Action(Of String),
        onError As Action(Of String),
        Optional ct As CancellationToken = Nothing
    ) As Task(Of Integer)

        Dim python = FindPython()
        If python Is Nothing Then
            onError?.Invoke("[ERROR] Python interpreter not found. Install Python 3.9+ or place python.exe in the 'python' sub-folder.")
            Return -1
        End If

        If Not File.Exists(scriptPath) Then
            onError?.Invoke($"[ERROR] Script not found: {scriptPath}")
            Return -1
        End If

        Dim workDir = Path.GetDirectoryName(scriptPath)

        Dim psi As New ProcessStartInfo(python, $"""{scriptPath}"" {args}") With {
            .WorkingDirectory = workDir,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True,
            .StandardOutputEncoding = Text.Encoding.UTF8,
            .StandardErrorEncoding = Text.Encoding.UTF8
        }

        Using proc As New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}
            proc.Start()

            ' Read stdout and stderr concurrently
            Dim stdoutTask = ReadStreamAsync(proc.StandardOutput, onOutput, ct)
            Dim stderrTask = ReadStreamAsync(proc.StandardError, onError, ct)

            ' Handle cancellation
            If ct <> Nothing Then
                ct.Register(Sub()
                                Try
                                    If Not proc.HasExited Then proc.Kill(entireProcessTree:=True)
                                Catch
                                End Try
                            End Sub)
            End If

            Await Task.WhenAll(stdoutTask, stderrTask)
            proc.WaitForExit()
            Return proc.ExitCode
        End Using
    End Function

    Private Shared Async Function ReadStreamAsync(
        reader As IO.StreamReader,
        callback As Action(Of String),
        ct As CancellationToken
    ) As Task
        Dim line As String
        Do
            If ct <> Nothing AndAlso ct.IsCancellationRequested Then Exit Do
            line = Await reader.ReadLineAsync()
            If line Is Nothing Then Exit Do
            callback?.Invoke(line)
        Loop
    End Function

End Class
