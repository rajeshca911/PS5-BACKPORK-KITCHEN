Imports System.IO
Imports System.Drawing

''' <summary>
''' Service layer wrapping PKGReader with validation and Result pattern.
''' </summary>
Public Class PackageManagerService

    ''' <summary>
    ''' Result wrapper for PKG operations.
    ''' </summary>
    Public Class PKGResult
        Public Property Success As Boolean
        Public Property ErrorMessage As String
        Public Property Reader As PKGReader

        Public Shared Function Ok(reader As PKGReader) As PKGResult
            Return New PKGResult With {.Success = True, .Reader = reader}
        End Function

        Public Shared Function Fail(message As String) As PKGResult
            Return New PKGResult With {.Success = False, .ErrorMessage = message}
        End Function
    End Class

    ''' <summary>
    ''' Validates that the file appears to be a PKG file.
    ''' </summary>
    Public Shared Function ValidatePackage(filePath As String) As Boolean
        If Not File.Exists(filePath) Then Return False

        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                If fs.Length < 4 Then Return False

                Using br As New BinaryReader(fs)
                    ' PKG magic is big-endian
                    Dim bytes = br.ReadBytes(4)
                    Array.Reverse(bytes)
                    Dim magic = BitConverter.ToUInt32(bytes, 0)
                    Return magic = PKGConstants.PKG_MAGIC
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Opens a PKG file, validates it, and parses all structures.
    ''' Returns a PKGResult. Caller must dispose the Reader when done.
    ''' </summary>
    Public Shared Function OpenPackage(filePath As String) As PKGResult
        If Not File.Exists(filePath) Then
            Return PKGResult.Fail($"File not found: {filePath}")
        End If

        Dim reader As New PKGReader()
        Try
            reader.Open(filePath)
            Return PKGResult.Ok(reader)
        Catch ex As Exception
            reader.Dispose()
            Return PKGResult.Fail($"Failed to open PKG: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Returns parsed metadata from an open PKG reader, or Nothing.
    ''' </summary>
    Public Shared Function GetMetadata(reader As PKGReader) As PKGMetadata
        Return reader.Metadata
    End Function

    ''' <summary>
    ''' Extracts a single entry from the PKG. Returns False if encrypted.
    ''' </summary>
    Public Shared Function ExtractEntry(reader As PKGReader, entry As PKGEntry,
                                         outputPath As String) As Boolean
        Try
            Return reader.ExtractEntry(entry, outputPath)
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Extracts all extractable entries to the output directory.
    ''' </summary>
    Public Shared Function ExtractAll(reader As PKGReader, outputDir As String,
                                       Optional progress As IProgress(Of Integer) = Nothing) As Boolean
        Try
            reader.ExtractAll(outputDir, progress)
            Return True
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Retrieves the icon image from the PKG if available.
    ''' </summary>
    Public Shared Function GetIcon(reader As PKGReader) As Image
        Try
            Return reader.GetIcon()
        Catch
            Return Nothing
        End Try
    End Function

End Class
