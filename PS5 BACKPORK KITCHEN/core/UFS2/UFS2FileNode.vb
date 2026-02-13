''' <summary>
''' Tree node model representing a file or directory inside a UFS2 image.
''' Used for populating TreeView and DataGridView in the UI.
''' </summary>
Public Class UFS2FileNode

    Public Property Name As String
    Public Property FullPath As String
    Public Property InodeNumber As UInteger
    Public Property Size As Long
    Public Property IsDirectory As Boolean
    Public Property FileType As String
    Public Property ModifiedDate As DateTime
    Public Property Mode As UShort
    Public Property Children As List(Of UFS2FileNode)

    Public Sub New()
        Children = New List(Of UFS2FileNode)()
    End Sub

    Public Sub New(name As String, fullPath As String, inodeNumber As UInteger,
                   size As Long, isDirectory As Boolean, fileType As String,
                   modifiedDate As DateTime)
        Me.Name = name
        Me.FullPath = fullPath
        Me.InodeNumber = inodeNumber
        Me.Size = size
        Me.IsDirectory = isDirectory
        Me.FileType = fileType
        Me.ModifiedDate = modifiedDate
        Children = New List(Of UFS2FileNode)()
    End Sub

    ''' <summary>
    ''' Returns the total file count under this node (recursive).
    ''' </summary>
    Public ReadOnly Property TotalFileCount As Integer
        Get
            If Not IsDirectory Then Return 1
            Dim count = 0
            For Each child In Children
                count += child.TotalFileCount
            Next
            Return count
        End Get
    End Property

    ''' <summary>
    ''' Returns the total size of all files under this node (recursive).
    ''' </summary>
    Public ReadOnly Property TotalSize As Long
        Get
            If Not IsDirectory Then Return Size
            Dim total As Long = 0
            For Each child In Children
                total += child.TotalSize
            Next
            Return total
        End Get
    End Property

    ''' <summary>
    ''' Recursively collects all file (non-directory) nodes.
    ''' </summary>
    Public Function GetAllFiles() As List(Of UFS2FileNode)
        Dim result As New List(Of UFS2FileNode)()
        If Not IsDirectory Then
            result.Add(Me)
        Else
            For Each child In Children
                result.AddRange(child.GetAllFiles())
            Next
        End If
        Return result
    End Function

    ''' <summary>
    ''' Returns the Unix permission string (e.g., rwxr-xr-x) from the lower 9 bits of Mode.
    ''' </summary>
    Public ReadOnly Property PermissionsString As String
        Get
            Dim perms = Mode And &H1FF  ' lower 9 bits
            Dim sb As New System.Text.StringBuilder(9)
            sb.Append(If((perms And &H100) <> 0, "r"c, "-"c))
            sb.Append(If((perms And &H80) <> 0, "w"c, "-"c))
            sb.Append(If((perms And &H40) <> 0, "x"c, "-"c))
            sb.Append(If((perms And &H20) <> 0, "r"c, "-"c))
            sb.Append(If((perms And &H10) <> 0, "w"c, "-"c))
            sb.Append(If((perms And &H8) <> 0, "x"c, "-"c))
            sb.Append(If((perms And &H4) <> 0, "r"c, "-"c))
            sb.Append(If((perms And &H2) <> 0, "w"c, "-"c))
            sb.Append(If((perms And &H1) <> 0, "x"c, "-"c))
            Return sb.ToString()
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return $"{Name} ({If(IsDirectory, "dir", FormatSize(Size))})"
    End Function

    Private Shared Function FormatSize(bytes As Long) As String
        If bytes < 1024 Then Return $"{bytes} B"
        If bytes < 1024 * 1024 Then Return $"{bytes / 1024.0:F1} KB"
        If bytes < 1024L * 1024 * 1024 Then Return $"{bytes / (1024.0 * 1024):F1} MB"
        Return $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    End Function

End Class
