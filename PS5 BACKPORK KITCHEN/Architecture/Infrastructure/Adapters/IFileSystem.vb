Imports System.IO

Namespace Architecture.Infrastructure.Adapters
    ''' <summary>
    ''' Abstraction for file system operations to enable testability and mocking
    ''' </summary>
    Public Interface IFileSystem
        ''' <summary>
        ''' Reads all bytes from a file asynchronously
        ''' </summary>
        Function ReadAllBytesAsync(path As String) As Task(Of Byte())

        ''' <summary>
        ''' Writes all bytes to a file asynchronously
        ''' </summary>
        Function WriteAllBytesAsync(path As String, bytes As Byte()) As Task

        ''' <summary>
        ''' Checks if a file exists
        ''' </summary>
        Function FileExistsAsync(path As String) As Task(Of Boolean)

        ''' <summary>
        ''' Checks if a directory exists
        ''' </summary>
        Function DirectoryExistsAsync(path As String) As Task(Of Boolean)

        ''' <summary>
        ''' Gets files matching a pattern in a directory
        ''' </summary>
        Function GetFilesAsync(directory As String, searchPattern As String, Optional searchOption As SearchOption = SearchOption.TopDirectoryOnly) As Task(Of String())

        ''' <summary>
        ''' Copies a file
        ''' </summary>
        Function CopyFileAsync(sourcePath As String, destPath As String, Optional overwrite As Boolean = False) As Task

        ''' <summary>
        ''' Deletes a file
        ''' </summary>
        Function DeleteFileAsync(path As String) As Task

        ''' <summary>
        ''' Creates a directory
        ''' </summary>
        Function CreateDirectoryAsync(path As String) As Task

        ''' <summary>
        ''' Gets file size in bytes
        ''' </summary>
        Function GetFileSizeAsync(path As String) As Task(Of Long)
    End Interface
End Namespace
