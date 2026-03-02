Imports System.Diagnostics
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading

''' <summary>
''' Launches Python scripts and streams their output line-by-line.
''' Searches for a Python interpreter in: PATH, venv, bundled python/ folder.
''' Requires Python 3.9 or newer.
''' </summary>
Public Class PythonRunner

    ' ---------------------------------------------------------------------------
    ' Python discovery
    ' ---------------------------------------------------------------------------

    ''' <summary>
    ''' Returns the path to a usable Python 3.9+ executable, or Nothing if none found.
    ''' Search order: bundled python/ sub-folder, virtual-env .venv, PATH.
    ''' </summary>
    Public Shared Function FindPython() As String
        Dim appDir = AppDomain.CurrentDomain.BaseDirectory

        ' 1. Bundled python (shipped alongside the app)
        Dim bundled As String = Path.Combine(appDir, "python", "python.exe")
        If File.Exists(bundled) AndAlso IsPython39OrNewer(bundled) Then Return bundled

        ' 2. Virtual environment created next to the app
        Dim venv As String = Path.Combine(appDir, ".venv", "Scripts", "python.exe")
        If File.Exists(venv) AndAlso IsPython39OrNewer(venv) Then Return venv

        ' 3. System PATH
        For Each candidate In {"python", "python3", "py"}
            Try
                If IsPython39OrNewer(candidate) Then Return candidate
            Catch
                ' Not found in PATH — try next
            End Try
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' Returns True when the given executable is Python 3.9 or newer.
    ''' </summary>
    Private Shared Function IsPython39OrNewer(executable As String) As Boolean
        Try
            Dim info As New ProcessStartInfo(executable, "--version") With {
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }
            Using proc = Process.Start(info)
                ' python --version prints to stdout on 3.4+ and to stderr on older builds.
                Dim output = proc.StandardOutput.ReadToEnd() & proc.StandardError.ReadToEnd()
                proc.WaitForExit(4000)
                If proc.ExitCode <> 0 Then Return False
                Dim m = Regex.Match(output, "Python (\d+)\.(\d+)")
                If Not m.Success Then Return False
                Dim major = Integer.Parse(m.Groups(1).Value)
                Dim minor = Integer.Parse(m.Groups(2).Value)
                Return major > 3 OrElse (major = 3 AndAlso minor >= 9)
            End Using
        Catch
            Return False
        End Try
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
            onError?.Invoke("[ERROR] Python 3.9+ interpreter not found. Install Python 3.9+ or place python.exe in the 'python' sub-folder.")
            Return -1
        End If

        If Not File.Exists(scriptPath) Then
            onError?.Invoke($"[ERROR] Script not found: {scriptPath}")
            Return -1
        End If

        Dim workDir = Path.GetDirectoryName(scriptPath)

        ' -u: force unbuffered stdout/stderr so output streams in real-time.
        Dim psi As New ProcessStartInfo(python, $"-u ""{scriptPath}"" {args}") With {
            .WorkingDirectory = workDir,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True,
            .StandardOutputEncoding = Text.Encoding.UTF8,
            .StandardErrorEncoding = Text.Encoding.UTF8
        }
        ' Belt-and-suspenders: env var also disables Python's internal buffering.
        psi.EnvironmentVariables("PYTHONUNBUFFERED") = "1"
        psi.EnvironmentVariables("PYTHONIOENCODING") = "utf-8"

        Using proc As New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}
            proc.Start()

            ' Read stdout and stderr concurrently
            Dim stdoutTask = ReadStreamAsync(proc.StandardOutput, onOutput, ct)
            Dim stderrTask = ReadStreamAsync(proc.StandardError, onError, ct)

            ' Handle cancellation
            If ct.CanBeCanceled Then
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
            If ct.IsCancellationRequested Then Exit Do
            line = Await reader.ReadLineAsync()
            If line Is Nothing Then Exit Do
            callback?.Invoke(line)
        Loop
    End Function

End Class
