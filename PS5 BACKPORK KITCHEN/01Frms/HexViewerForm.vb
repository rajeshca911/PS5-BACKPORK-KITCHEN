Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Standalone hex viewer for binary file inspection.
''' Shows offset | hex bytes | ASCII representation.
''' </summary>
Public Class HexViewerForm
    Inherits Form

    Private _filePath As String
    Private _fileData As Byte()
    Private _maxBytes As Integer

    Private rtbHex As RichTextBox
    Private toolStrip As ToolStrip
    Private txtGoTo As ToolStripTextBox
    Private btnGoTo As ToolStripButton
    Private txtSearch As ToolStripTextBox
    Private btnSearch As ToolStripButton
    Private statusStrip As StatusStrip
    Private lblInfo As ToolStripStatusLabel
    Private lblOffset As ToolStripStatusLabel

    Private _currentSearchIndex As Integer = 0

    Public Sub New(filePath As String, Optional maxBytes As Integer = 1048576)
        _filePath = filePath
        _maxBytes = maxBytes
        InitializeComponent()
        LoadFile()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = $"Hex Viewer - {Path.GetFileName(_filePath)}"
        Me.Size = New Size(820, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(600, 400)

        ' ToolStrip
        toolStrip = New ToolStrip()

        toolStrip.Items.Add(New ToolStripLabel("Go To Offset:"))
        txtGoTo = New ToolStripTextBox With {.Width = 80}
        txtGoTo.ToolTipText = "Enter hex offset (e.g. 1A0)"
        AddHandler txtGoTo.KeyDown, Sub(s, e)
                                        If e.KeyCode = Keys.Enter Then GoToOffset()
                                    End Sub
        toolStrip.Items.Add(txtGoTo)

        btnGoTo = New ToolStripButton("Go")
        AddHandler btnGoTo.Click, Sub(s, e) GoToOffset()
        toolStrip.Items.Add(btnGoTo)

        toolStrip.Items.Add(New ToolStripSeparator())

        toolStrip.Items.Add(New ToolStripLabel("Search Hex:"))
        txtSearch = New ToolStripTextBox With {.Width = 120}
        txtSearch.ToolTipText = "Enter hex bytes to search (e.g. 7F 45 4C 46)"
        AddHandler txtSearch.KeyDown, Sub(s, e)
                                          If e.KeyCode = Keys.Enter Then SearchHex()
                                      End Sub
        toolStrip.Items.Add(txtSearch)

        btnSearch = New ToolStripButton("Find Next")
        AddHandler btnSearch.Click, Sub(s, e) SearchHex()
        toolStrip.Items.Add(btnSearch)

        Me.Controls.Add(toolStrip)

        ' Hex display
        rtbHex = New RichTextBox With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Consolas", 10),
            .ReadOnly = True,
            .WordWrap = False,
            .BackColor = Color.FromArgb(25, 25, 35),
            .ForeColor = Color.FromArgb(200, 200, 200),
            .BorderStyle = BorderStyle.None
        }
        Me.Controls.Add(rtbHex)

        ' StatusStrip
        statusStrip = New StatusStrip()
        lblInfo = New ToolStripStatusLabel("") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        lblOffset = New ToolStripStatusLabel("Offset: 0x00000000")
        statusStrip.Items.AddRange({lblInfo, lblOffset})
        Me.Controls.Add(statusStrip)

        ' Ensure proper z-order (toolbar on top, status on bottom)
        rtbHex.BringToFront()
    End Sub

    Private Sub LoadFile()
        Try
            Using fs As New FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim readSize = CInt(Math.Min(fs.Length, _maxBytes))
                _fileData = New Byte(readSize - 1) {}
                fs.Read(_fileData, 0, readSize)

                Dim sizeStr = FormatSize(fs.Length)
                Dim showingStr = If(readSize < fs.Length,
                    $"Showing first {FormatSize(readSize)} of {sizeStr}",
                    $"Showing all {sizeStr}")
                lblInfo.Text = $"{Path.GetFileName(_filePath)}  |  {showingStr}"
            End Using

            RenderHex(0)
        Catch ex As Exception
            MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RenderHex(startOffset As Integer)
        If _fileData Is Nothing Then Return

        rtbHex.Clear()
        Dim sb As New System.Text.StringBuilder(65536)

        ' Header
        sb.Append("Offset    ")
        For i = 0 To 15
            sb.Append(i.ToString("X2"))
            sb.Append(" "c)
            If i = 7 Then sb.Append(" "c)
        Next
        sb.AppendLine("  ASCII")
        sb.AppendLine(New String("─"c, 76))

        Dim endOffset = Math.Min(_fileData.Length, startOffset + 16384)

        For offset = startOffset To endOffset - 1 Step 16
            sb.Append(offset.ToString("X8"))
            sb.Append("  ")

            Dim ascii As New System.Text.StringBuilder(16)
            For i = 0 To 15
                If offset + i < _fileData.Length Then
                    Dim b = _fileData(offset + i)
                    sb.Append(b.ToString("X2"))
                    sb.Append(" "c)
                    ascii.Append(If(b >= 32 AndAlso b < 127, ChrW(b), "."c))
                Else
                    sb.Append("   ")
                End If
                If i = 7 Then sb.Append(" "c)
            Next

            sb.Append(" "c)
            sb.AppendLine(ascii.ToString())
        Next

        If endOffset < _fileData.Length Then
            sb.AppendLine()
            sb.AppendLine($"... ({FormatSize(_fileData.Length)} total)")
        End If

        rtbHex.Text = sb.ToString()
        lblOffset.Text = $"Offset: 0x{startOffset:X8}"
    End Sub

    Private Sub GoToOffset()
        If _fileData Is Nothing Then Return

        Dim offsetStr = txtGoTo.Text.Trim().Replace("0x", "").Replace("&H", "")
        Dim offset As Integer
        If Integer.TryParse(offsetStr, Globalization.NumberStyles.HexNumber, Nothing, offset) Then
            If offset >= 0 AndAlso offset < _fileData.Length Then
                ' Re-render starting at this offset (aligned to 16-byte boundary)
                offset = (offset \ 16) * 16
                RenderHex(offset)
            Else
                MessageBox.Show($"Offset 0x{offset:X} is beyond file size ({FormatSize(_fileData.Length)}).",
                              "Invalid Offset", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Else
            MessageBox.Show("Enter a valid hexadecimal offset (e.g. 1A0, 0xFF00).",
                          "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub SearchHex()
        If _fileData Is Nothing Then Return

        Dim searchStr = txtSearch.Text.Trim().Replace(" ", "")
        If String.IsNullOrEmpty(searchStr) OrElse searchStr.Length Mod 2 <> 0 Then
            MessageBox.Show("Enter hex bytes separated by spaces (e.g. 7F 45 4C 46).",
                          "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Parse hex string to byte array
        Dim searchBytes As Byte()
        Try
            searchBytes = New Byte(searchStr.Length \ 2 - 1) {}
            For i = 0 To searchBytes.Length - 1
                searchBytes(i) = Convert.ToByte(searchStr.Substring(i * 2, 2), 16)
            Next
        Catch
            MessageBox.Show("Invalid hex input.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        ' Search from current position
        For i = _currentSearchIndex To _fileData.Length - searchBytes.Length
            Dim found = True
            For j = 0 To searchBytes.Length - 1
                If _fileData(i + j) <> searchBytes(j) Then
                    found = False
                    Exit For
                End If
            Next

            If found Then
                _currentSearchIndex = i + 1
                Dim lineOffset = (i \ 16) * 16
                RenderHex(lineOffset)
                lblOffset.Text = $"Found at 0x{i:X8}"
                Return
            End If
        Next

        ' Wrap around
        If _currentSearchIndex > 0 Then
            _currentSearchIndex = 0
            MessageBox.Show("Reached end of file. Search will restart from beginning.",
                          "Search", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            MessageBox.Show("Pattern not found.", "Search",
                          MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Shared Function FormatSize(bytes As Long) As String
        If bytes < 1024 Then Return $"{bytes} B"
        If bytes < 1024 * 1024 Then Return $"{bytes / 1024.0:F1} KB"
        If bytes < 1024L * 1024 * 1024 Then Return $"{bytes / (1024.0 * 1024):F1} MB"
        Return $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    End Function
End Class
