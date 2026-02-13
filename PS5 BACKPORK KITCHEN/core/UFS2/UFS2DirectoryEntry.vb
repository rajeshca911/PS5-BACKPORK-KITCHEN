Imports System.IO
Imports System.Text

''' <summary>
''' Represents a single directory entry in a UFS2 directory block.
''' </summary>
Public Class UFS2DirectoryEntry

    Public Property InodeNumber As UInteger
    Public Property RecordLength As UShort
    Public Property EntryType As Byte
    Public Property NameLength As Byte
    Public Property Name As String

    ''' <summary>
    ''' Reads one directory entry from the current reader position.
    ''' Returns Nothing if the entry is invalid or end-of-block.
    ''' </summary>
    Public Shared Function Load(br As BinaryReader, blockEnd As Long) As UFS2DirectoryEntry
        If br.BaseStream.Position >= blockEnd Then Return Nothing

        Dim startPos = br.BaseStream.Position

        ' Need at least 8 bytes for the fixed header
        If blockEnd - startPos < 8 Then Return Nothing

        Dim entry As New UFS2DirectoryEntry()
        entry.InodeNumber = br.ReadUInt32()
        entry.RecordLength = br.ReadUInt16()
        entry.EntryType = br.ReadByte()
        entry.NameLength = br.ReadByte()

        ' RecordLength 0 means end
        If entry.RecordLength = 0 Then Return Nothing

        ' Read name
        If entry.NameLength > 0 AndAlso entry.NameLength <= entry.RecordLength - 8 Then
            Dim nameBytes = br.ReadBytes(entry.NameLength)
            entry.Name = Encoding.ASCII.GetString(nameBytes)
        Else
            entry.Name = ""
        End If

        ' Advance to next entry (entries are padded to RecordLength)
        Dim nextPos = startPos + entry.RecordLength
        If nextPos > blockEnd Then nextPos = blockEnd
        br.BaseStream.Seek(nextPos, SeekOrigin.Begin)

        Return entry
    End Function

    ''' <summary>
    ''' True if this is a valid entry (non-zero inode, has a name).
    ''' </summary>
    Public ReadOnly Property IsValid As Boolean
        Get
            Return InodeNumber > 0 AndAlso Not String.IsNullOrEmpty(Name)
        End Get
    End Property

    ''' <summary>
    ''' True if this is "." or ".." entry.
    ''' </summary>
    Public ReadOnly Property IsDotEntry As Boolean
        Get
            Return Name = "." OrElse Name = ".."
        End Get
    End Property

    ''' <summary>
    ''' Returns the type name for this entry.
    ''' </summary>
    Public ReadOnly Property TypeName As String
        Get
            Return UFS2Constants.GetDirectoryEntryTypeName(EntryType)
        End Get
    End Property

End Class
