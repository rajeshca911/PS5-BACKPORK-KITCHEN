Imports System.IO

''' <summary>
''' Service layer wrapping UFS2ImageReader with validation and Result pattern.
''' </summary>
Public Class UFS2ImageService

    ''' <summary>
    ''' Result wrapper for UFS2 operations.
    ''' </summary>
    Public Class UFS2Result
        Public Property Success As Boolean
        Public Property ErrorMessage As String
        Public Property Reader As UFS2ImageReader
        Public Property FileTree As UFS2FileNode

        Public Shared Function Ok(reader As UFS2ImageReader, tree As UFS2FileNode) As UFS2Result
            Return New UFS2Result With {.Success = True, .Reader = reader, .FileTree = tree}
        End Function

        Public Shared Function Fail(message As String) As UFS2Result
            Return New UFS2Result With {.Success = False, .ErrorMessage = message}
        End Function
    End Class

    ''' <summary>
    ''' Validates that the file appears to be a UFS2 image.
    ''' </summary>
    Public Shared Function ValidateImage(filePath As String) As Boolean
        If Not File.Exists(filePath) Then Return False

        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                If fs.Length < UFS2Constants.SUPERBLOCK_OFFSET + UFS2Constants.SUPERBLOCK_SIZE Then
                    Return False
                End If

                Using br As New BinaryReader(fs)
                    fs.Seek(UFS2Constants.SUPERBLOCK_OFFSET + UFS2Constants.SB_MAGIC_OFFSET, SeekOrigin.Begin)
                    Dim magic = br.ReadUInt32()
                    Return magic = UFS2Constants.UFS2_MAGIC
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Opens a UFS2 image, validates it, and builds the file tree.
    ''' Returns a UFS2Result. Caller must dispose the Reader when done.
    ''' </summary>
    Public Shared Function OpenImage(filePath As String,
                                      Optional progress As IProgress(Of String) = Nothing) As UFS2Result
        If Not File.Exists(filePath) Then
            Return UFS2Result.Fail($"File not found: {filePath}")
        End If

        Dim reader As New UFS2ImageReader()
        Try
            progress?.Report("Opening image...")
            reader.Open(filePath)

            progress?.Report("Building file tree...")
            Dim tree = reader.BuildFileTree(progress)

            Return UFS2Result.Ok(reader, tree)
        Catch ex As Exception
            reader.Dispose()
            Return UFS2Result.Fail($"Failed to open UFS2 image: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Extracts a single file from an open reader.
    ''' </summary>
    Public Shared Function ExtractFile(reader As UFS2ImageReader, inodeNumber As UInteger,
                                        outputPath As String) As Boolean
        Try
            reader.ExtractFile(inodeNumber, outputPath)
            Return True
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Extracts all files from the tree to the output directory.
    ''' </summary>
    Public Shared Function ExtractAll(reader As UFS2ImageReader, rootNode As UFS2FileNode,
                                       outputDir As String,
                                       Optional progress As IProgress(Of Integer) = Nothing) As Boolean
        Try
            reader.ExtractAll(rootNode, outputDir, progress)
            Return True
        Catch ex As Exception
            Throw New Exception($"Failed to extract files: {ex.Message}", ex)
        End Try
    End Function

End Class
