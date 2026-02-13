Imports System.IO
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Enhanced SFO viewer popup with DataGridView, search filter, export, and optional hex tab.
''' </summary>
Public Class SFOViewerForm
    Inherits Form

    ' ---- UI Controls ----
    Private toolStrip As ToolStrip
    Private txtSearch As ToolStripTextBox
    Private btnExport As ToolStripButton

    Private tabControl As TabControl
    Private tabParams As TabPage
    Private tabHex As TabPage

    Private dgvParams As DataGridView
    Private txtHex As RichTextBox

    ' ---- State ----
    Private ReadOnly _metadata As PKGMetadata
    Private ReadOnly _rawSfoData As Byte()

    ' Well-known SFO keys (displayed in bold)
    Private Shared ReadOnly KnownKeys As HashSet(Of String) = New HashSet(Of String)(
        StringComparer.OrdinalIgnoreCase) From {
        "TITLE", "TITLE_ID", "CONTENT_ID", "APP_VER", "VERSION",
        "CATEGORY", "SYSTEM_VER", "APP_TYPE", "ATTRIBUTE"
    }

    Public Sub New(metadata As PKGMetadata, rawSfoData As Byte())
        _metadata = metadata
        _rawSfoData = rawSfoData

        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)
        InitializeFormLayout()
        PopulateData()
    End Sub

    Private Sub InitializeFormLayout()
        Me.Text = "SFO Parameter Viewer"
        Me.MinimumSize = New Size(650, 450)
        Me.Size = New Size(750, 550)
        Me.StartPosition = FormStartPosition.CenterParent

        ' Root layout
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2
        }
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Me.Controls.Add(root)

        ' ---- ToolStrip ----
        toolStrip = New ToolStrip With {.GripStyle = ToolStripGripStyle.Hidden, .Dock = DockStyle.Fill}

        toolStrip.Items.Add(New ToolStripLabel("Search:"))

        txtSearch = New ToolStripTextBox With {.AutoSize = False, .Width = 200}
        AddHandler txtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        toolStrip.Items.Add(txtSearch)

        toolStrip.Items.Add(New ToolStripSeparator())

        btnExport = New ToolStripButton("Export") With {.DisplayStyle = ToolStripItemDisplayStyle.Text}
        AddHandler btnExport.Click, AddressOf BtnExport_Click
        toolStrip.Items.Add(btnExport)

        root.Controls.Add(toolStrip, 0, 0)

        ' ---- TabControl ----
        tabControl = New TabControl With {.Dock = DockStyle.Fill}

        ' Parameters tab
        tabParams = New TabPage("Parameters")
        dgvParams = New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        dgvParams.Columns.Add("Key", "Key")
        dgvParams.Columns.Add("Value", "Value")
        dgvParams.Columns.Add("Format", "Format")
        dgvParams.Columns.Add("Offset", "Offset")

        dgvParams.Columns("Key").FillWeight = 25
        dgvParams.Columns("Value").FillWeight = 45
        dgvParams.Columns("Format").FillWeight = 15
        dgvParams.Columns("Offset").FillWeight = 15

        tabParams.Controls.Add(dgvParams)
        tabControl.TabPages.Add(tabParams)

        ' Hex tab
        tabHex = New TabPage("Raw Hex")
        txtHex = New RichTextBox With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .Font = New Font("Consolas", 9),
            .BackColor = Color.FromArgb(250, 250, 250),
            .WordWrap = False
        }
        tabHex.Controls.Add(txtHex)
        tabControl.TabPages.Add(tabHex)

        root.Controls.Add(tabControl, 0, 1)
    End Sub

    Private Sub PopulateData()
        If _metadata Is Nothing Then Return

        ' Populate parameters grid
        Dim offset = 0
        For Each kvp In _metadata.AllParams.OrderBy(Function(k) k.Key)
            Dim rowIdx = dgvParams.Rows.Add(
                kvp.Key,
                kvp.Value,
                "UTF-8",
                $"0x{offset:X4}"
            )

            ' Bold for known keys
            If KnownKeys.Contains(kvp.Key) Then
                dgvParams.Rows(rowIdx).DefaultCellStyle.Font = New Font(dgvParams.Font, FontStyle.Bold)
            End If

            offset += 16 ' approximate
        Next

        ' Populate hex view
        If _rawSfoData IsNot Nothing AndAlso _rawSfoData.Length > 0 Then
            txtHex.Text = FormatHexDump(_rawSfoData, 16)
        Else
            txtHex.Text = "(No raw SFO data available)"
        End If
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        Dim filter = txtSearch.Text.Trim().ToLowerInvariant()

        For Each row As DataGridViewRow In dgvParams.Rows
            If String.IsNullOrEmpty(filter) Then
                row.Visible = True
            Else
                Dim key = row.Cells("Key").Value?.ToString().ToLowerInvariant()
                Dim val = row.Cells("Value").Value?.ToString().ToLowerInvariant()
                row.Visible = (key IsNot Nothing AndAlso key.Contains(filter)) OrElse
                              (val IsNot Nothing AndAlso val.Contains(filter))
            End If
        Next
    End Sub

    Private Sub BtnExport_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog()
            sfd.Filter = "Text Files|*.txt|CSV Files|*.csv"
            sfd.FileName = "sfo_params.txt"

            If sfd.ShowDialog() <> DialogResult.OK Then Return

            Try
                Using writer As New StreamWriter(sfd.FileName)
                    If sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) Then
                        writer.WriteLine("Key,Value,Format,Offset")
                        For Each row As DataGridViewRow In dgvParams.Rows
                            writer.WriteLine($"""{row.Cells("Key").Value}"",""{row.Cells("Value").Value}"",{row.Cells("Format").Value},{row.Cells("Offset").Value}")
                        Next
                    Else
                        writer.WriteLine("SFO Parameters")
                        writer.WriteLine("==============")
                        writer.WriteLine()
                        For Each row As DataGridViewRow In dgvParams.Rows
                            writer.WriteLine($"{row.Cells("Key").Value,-24} = {row.Cells("Value").Value}")
                        Next
                    End If
                End Using

                MessageBox.Show($"Exported to: {sfd.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' Formats raw bytes as a hex dump with offset, hex bytes, and ASCII.
    ''' </summary>
    Public Shared Function FormatHexDump(data As Byte(), bytesPerLine As Integer) As String
        Dim sb As New StringBuilder()
        Dim i = 0

        While i < data.Length
            ' Offset
            sb.Append($"{i:X8}  ")

            ' Hex bytes
            Dim lineLen = Math.Min(bytesPerLine, data.Length - i)
            For j = 0 To bytesPerLine - 1
                If j = bytesPerLine \ 2 Then sb.Append(" ")
                If j < lineLen Then
                    sb.Append($"{data(i + j):X2} ")
                Else
                    sb.Append("   ")
                End If
            Next

            ' ASCII
            sb.Append(" |")
            For j = 0 To lineLen - 1
                Dim b = data(i + j)
                sb.Append(If(b >= 32 AndAlso b < 127, ChrW(b), "."c))
            Next
            sb.Append("|")
            sb.AppendLine()

            i += bytesPerLine
        End While

        Return sb.ToString()
    End Function

End Class
