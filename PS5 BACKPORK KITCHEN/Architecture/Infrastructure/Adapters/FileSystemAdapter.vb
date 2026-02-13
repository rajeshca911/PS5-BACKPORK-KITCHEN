Imports System.IO

Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' Concrete implementation of IFileSystem using System.IO
    ''' </summary>
    Public Class FileSystemAdapter
        Implements IFileSystem

        Public Async Function ReadAllBytesAsync(path As String) As Task(Of Byte()) _
            Implements IFileSystem.ReadAllBytesAsync
            Return Await File.ReadAllBytesAsync(path)
        End Function

        Public Async Function WriteAllBytesAsync(path As String, bytes As Byte()) As Task _
            Implements IFileSystem.WriteAllBytesAsync
            Await File.WriteAllBytesAsync(path, bytes)
        End Function

        Public Function FileExistsAsync(path As String) As Task(Of Boolean) _
            Implements IFileSystem.FileExistsAsync
            Return Task.FromResult(File.Exists(path))
        End Function

        Public Function DirectoryExistsAsync(path As String) As Task(Of Boolean) _
            Implements IFileSystem.DirectoryExistsAsync
            Return Task.FromResult(Directory.Exists(path))
        End Function

        Public Function GetFilesAsync(directory As String, searchPattern As String, Optional searchOption As SearchOption = SearchOption.TopDirectoryOnly) As Task(Of String()) _
            Implements IFileSystem.GetFilesAsync
            Dim files As String() = IO.Directory.GetFiles(directory, searchPattern, searchOption)
            Return Task.FromResult(Of String())(files)
        End Function

        Public Async Function CopyFileAsync(sourcePath As String, destPath As String, Optional overwrite As Boolean = False) As Task _
            Implements IFileSystem.CopyFileAsync
            Await Task.Run(Sub() File.Copy(sourcePath, destPath, overwrite))
        End Function

        Public Function DeleteFileAsync(path As String) As Task _
            Implements IFileSystem.DeleteFileAsync
            Return Task.Run(Sub() File.Delete(path))
        End Function

        Public Function CreateDirectoryAsync(path As String) As Task _
            Implements IFileSystem.CreateDirectoryAsync
            Return Task.Run(Sub() Directory.CreateDirectory(path))
        End Function

        Public Function GetFileSizeAsync(path As String) As Task(Of Long) _
            Implements IFileSystem.GetFileSizeAsync
            Dim fileInfo = New FileInfo(path)
            Return Task.FromResult(fileInfo.Length)
        End Function
    End Class
End Namespace
