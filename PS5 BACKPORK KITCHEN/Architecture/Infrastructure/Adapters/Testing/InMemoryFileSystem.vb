Imports System.IO
Imports System.Collections.Concurrent

Namespace Architecture.Infrastructure.Adapters.Testing
    ''' <summary>
    ''' In-memory file system for unit testing
    ''' </summary>
    Public Class InMemoryFileSystem
        Implements IFileSystem

        Private ReadOnly _files As New ConcurrentDictionary(Of String, Byte())
        Private ReadOnly _directories As New ConcurrentDictionary(Of String, Boolean)

        Public Function ReadAllBytesAsync(path As String) As Task(Of Byte()) _
            Implements IFileSystem.ReadAllBytesAsync

            If Not _files.ContainsKey(path) Then
                Throw New FileNotFoundException($"File not found: {path}")
            End If

            Return Task.FromResult(Of Byte())(_files(path))
        End Function

        Public Function WriteAllBytesAsync(path As String, bytes As Byte()) As Task _
            Implements IFileSystem.WriteAllBytesAsync

            _files(path) = bytes

            ' Auto-create parent directory
            Dim directory = IO.Path.GetDirectoryName(path)
            If Not String.IsNullOrEmpty(directory) Then
                _directories(directory) = True
            End If

            Return Task.CompletedTask
        End Function

        Public Function FileExistsAsync(path As String) As Task(Of Boolean) _
            Implements IFileSystem.FileExistsAsync
            Return Task.FromResult(Of Boolean)(_files.ContainsKey(path))
        End Function

        Public Function DirectoryExistsAsync(path As String) As Task(Of Boolean) _
            Implements IFileSystem.DirectoryExistsAsync
            Return Task.FromResult(Of Boolean)(_directories.ContainsKey(path))
        End Function

        Public Function GetFilesAsync(directory As String, searchPattern As String, Optional searchOption As SearchOption = SearchOption.TopDirectoryOnly) As Task(Of String()) _
            Implements IFileSystem.GetFilesAsync

            ' Simple pattern matching
            Dim pattern = searchPattern.Replace("*", ".*").Replace("?", ".")
            Dim regex = New Text.RegularExpressions.Regex(pattern)

            Dim matchingFiles = _files.Keys _
                .Where(Function(f) f.StartsWith(directory) AndAlso regex.IsMatch(IO.Path.GetFileName(f))) _
                .ToArray()

            Return Task.FromResult(Of String())(matchingFiles)
        End Function

        Public Function CopyFileAsync(sourcePath As String, destPath As String, Optional overwrite As Boolean = False) As Task _
            Implements IFileSystem.CopyFileAsync

            If Not _files.ContainsKey(sourcePath) Then
                Throw New FileNotFoundException($"Source not found: {sourcePath}")
            End If

            If _files.ContainsKey(destPath) AndAlso Not overwrite Then
                Throw New IOException($"Destination already exists: {destPath}")
            End If

            _files(destPath) = _files(sourcePath).ToArray() ' Deep copy
            Return Task.CompletedTask
        End Function

        Public Function DeleteFileAsync(path As String) As Task _
            Implements IFileSystem.DeleteFileAsync

            Dim ignored As Byte() = Nothing
            _files.TryRemove(path, ignored)
            Return Task.CompletedTask
        End Function

        Public Function CreateDirectoryAsync(path As String) As Task _
            Implements IFileSystem.CreateDirectoryAsync

            _directories(path) = True
            Return Task.CompletedTask
        End Function

        Public Function GetFileSizeAsync(path As String) As Task(Of Long) _
            Implements IFileSystem.GetFileSizeAsync

            If Not _files.ContainsKey(path) Then
                Throw New FileNotFoundException($"File not found: {path}")
            End If

            Return Task.FromResult(Of Long)(CLng(_files(path).Length))
        End Function

        ' Helpers for testing
        Public Sub Clear()
            _files.Clear()
            _directories.Clear()
        End Sub

        Public Function GetAllFiles() As Dictionary(Of String, Byte())
            Return New Dictionary(Of String, Byte())(_files)
        End Function
    End Class
End Namespace
