Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' UFS2 Image Converter form - browse and extract files from UFS2 filesystem images.
''' Layout follows ElfInspectorForm pattern: ToolStrip + SplitContainer (TreeView | DataGridView+RichTextBox) + StatusStrip.
''' </summary>
Public Class UFS2ImageForm
    Inherits Form

    ' ---- UI Controls ----
    Private toolStrip As ToolStrip
    Private btnBrowse As ToolStripButton
    Private btnExtractAll As ToolStripButton
    Private btnExportReport As ToolStripButton
    Private lblImagePath As ToolStripLabel
    Private txtImagePath As ToolStripTextBox

    Private splitContainer As SplitContainer
    Private treeView As TreeView
    Private rightPanel As TableLayoutPanel
    Private dgvFiles As DataGridView
    Private txtDetails As RichTextBox

    Private btnBuildFPKG As ToolStripButton
    Private txtSearch As ToolStripTextBox

    Private contextMenu As ContextMenuStrip
    Private mnuExtractFile As ToolStripMenuItem
    Private mnuExtractDirectory As ToolStripMenuItem
    Private mnuViewHex As ToolStripMenuItem

    Private statusStrip As StatusStrip
    Private lblStatus As ToolStripStatusLabel
    Private lblFileCount As ToolStripStatusLabel
    Private lblTotalSize As ToolStripStatusLabel
    Private progressBar As ToolStripProgressBar

    ' ---- State ----
    Private _reader As UFS2ImageReader
    Private _fileTree As UFS2FileNode
    Private _currentImagePath As String = ""

    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)
        InitializeFormLayout()
    End Sub

    Private Sub InitializeFormLayout()
        Me.Text = "UFS2 Image Converter"
        Me.MinimumSize = New Size(950, 650)
        Me.Size = New Size(1100, 750)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.AllowDrop = True

        AddHandler Me.DragEnter, AddressOf Form_DragEnter
        AddHandler Me.DragDrop, AddressOf Form_DragDrop
        AddHandler Me.FormClosing, AddressOf Form_Closing

        ' Root layout
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3
        }
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Me.Controls.Add(root)

        ' ---- ToolStrip ----
        toolStrip = New ToolStrip With {.GripStyle = ToolStripGripStyle.Hidden, .Dock = DockStyle.Fill}

        lblImagePath = New ToolStripLabel("Image:")
        txtImagePath = New ToolStripTextBox With {.AutoSize = False, .Width = 420, .ReadOnly = True}

        btnBrowse = New ToolStripButton("Browse...") With {.DisplayStyle = ToolStripItemDisplayStyle.Text}
        AddHandler btnBrowse.Click, AddressOf BtnBrowse_Click

        toolStrip.Items.Add(lblImagePath)
        toolStrip.Items.Add(txtImagePath)
        toolStrip.Items.Add(btnBrowse)
        toolStrip.Items.Add(New ToolStripSeparator())

        btnExtractAll = New ToolStripButton("Extract All") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnExtractAll.Click, AddressOf BtnExtractAll_Click
        toolStrip.Items.Add(btnExtractAll)

        btnExportReport = New ToolStripButton("Export Report") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnExportReport.Click, AddressOf BtnExportReport_Click
        toolStrip.Items.Add(btnExportReport)

        btnBuildFPKG = New ToolStripButton("Build FPKG") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnBuildFPKG.Click, AddressOf BtnBuildFPKG_Click
        toolStrip.Items.Add(btnBuildFPKG)

        toolStrip.Items.Add(New ToolStripSeparator())
        toolStrip.Items.Add(New ToolStripLabel("Search:"))
        txtSearch = New ToolStripTextBox With {.AutoSize = False, .Width = 150}
        AddHandler txtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        toolStrip.Items.Add(txtSearch)

        root.Controls.Add(toolStrip, 0, 0)

        ' ---- SplitContainer ----
        splitContainer = New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterDistance = 300
        }
        root.Controls.Add(splitContainer, 0, 1)

        ' Left panel: TreeView
        treeView = New TreeView With {
            .Dock = DockStyle.Fill,
            .ShowLines = True,
            .ShowPlusMinus = True,
            .ShowRootLines = True,
            .HideSelection = False,
            .PathSeparator = "/"
        }
        AddHandler treeView.AfterSelect, AddressOf TreeView_AfterSelect
        splitContainer.Panel1.Controls.Add(treeView)

        ' Right panel: DataGridView + Details
        rightPanel = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2
        }
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 65))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 35))
        splitContainer.Panel2.Controls.Add(rightPanel)

        ' DataGridView for file list
        dgvFiles = New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        BuildGridColumns()
        AddHandler dgvFiles.SelectionChanged, AddressOf DgvFiles_SelectionChanged
        rightPanel.Controls.Add(dgvFiles, 0, 0)

        ' Context menu
        BuildContextMenu()
        dgvFiles.ContextMenuStrip = contextMenu
        treeView.ContextMenuStrip = contextMenu

        ' RichTextBox for details
        txtDetails = New RichTextBox With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .Font = New Font("Consolas", 9),
            .BackColor = Color.FromArgb(250, 250, 250)
        }
        rightPanel.Controls.Add(txtDetails, 0, 1)

        ' ---- StatusStrip ----
        statusStrip = New StatusStrip()
        lblStatus = New ToolStripStatusLabel("Ready") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        lblFileCount = New ToolStripStatusLabel("Files: 0")
        lblTotalSize = New ToolStripStatusLabel("Size: 0 B")
        progressBar = New ToolStripProgressBar With {.Visible = False, .Width = 180}
        statusStrip.Items.AddRange({lblStatus, lblFileCount, lblTotalSize, progressBar})
        root.Controls.Add(statusStrip, 0, 2)
    End Sub

    Private Sub BuildGridColumns()
        dgvFiles.Columns.Add("Name", "Name")
        dgvFiles.Columns.Add("Type", "Type")
        dgvFiles.Columns.Add("Size", "Size")
        dgvFiles.Columns.Add("Modified", "Modified")
        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "InodeNum", .HeaderText = "Inode", .Visible = False})
        dgvFiles.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "FullPath", .HeaderText = "Path", .Visible = False})
    End Sub

    Private Sub BuildContextMenu()
        contextMenu = New ContextMenuStrip()

        mnuExtractFile = New ToolStripMenuItem With {
            .Text = "Extract File(s)",
            .ToolTipText = "Extract selected file(s) to disk"
        }
        AddHandler mnuExtractFile.Click, AddressOf MnuExtractFile_Click

        mnuExtractDirectory = New ToolStripMenuItem With {
            .Text = "Extract Directory",
            .ToolTipText = "Extract the selected directory and all contents"
        }
        AddHandler mnuExtractDirectory.Click, AddressOf MnuExtractDirectory_Click

        mnuViewHex = New ToolStripMenuItem With {
            .Text = "View Hex",
            .ToolTipText = "View first 4KB as hex dump"
        }
        AddHandler mnuViewHex.Click, AddressOf MnuViewHex_Click

        contextMenu.Items.AddRange({mnuExtractFile, mnuExtractDirectory, New ToolStripSeparator(), mnuViewHex})
    End Sub

    ' ===== EVENT HANDLERS =====

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Title = "Open UFS2 Image File"
            ofd.Filter = "UFS2 Images|*.img;*.bin;*.ufs2;*.raw|All Files|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                LoadImage(ofd.FileName)
            End If
        End Using
    End Sub

    Private Sub Form_DragEnter(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub Form_DragDrop(sender As Object, e As DragEventArgs)
        Dim paths = DirectCast(e.Data.GetData(DataFormats.FileDrop), String())
        If paths IsNot Nothing AndAlso paths.Length > 0 Then
            LoadImage(paths(0))
        End If
    End Sub

    Private Sub Form_Closing(sender As Object, e As FormClosingEventArgs)
        CleanupReader()
    End Sub

    Private Sub TreeView_AfterSelect(sender As Object, e As TreeViewEventArgs)
        If e.Node Is Nothing Then Return
        Dim node = TryCast(e.Node.Tag, UFS2FileNode)
        If node Is Nothing Then Return

        PopulateGrid(node)
        ShowNodeDetails(node)
    End Sub

    Private Sub DgvFiles_SelectionChanged(sender As Object, e As EventArgs)
        If dgvFiles.SelectedRows.Count = 0 Then Return
        Dim row = dgvFiles.SelectedRows(0)
        Dim fullPath = row.Cells("FullPath").Value?.ToString()
        If String.IsNullOrEmpty(fullPath) Then Return

        ' Find the node and show details
        Dim inodeStr = row.Cells("InodeNum").Value?.ToString()
        Dim inodeNum As UInteger
        If UInteger.TryParse(inodeStr, inodeNum) Then
            ShowFileDetails(row)
        End If
    End Sub

    ' ===== CORE LOGIC =====

    Private Sub LoadImage(imagePath As String)
        Try
            CleanupReader()
            treeView.Nodes.Clear()
            dgvFiles.Rows.Clear()
            txtDetails.Clear()

            lblStatus.Text = "Opening image..."
            progressBar.Visible = True
            Application.DoEvents()

            _currentImagePath = imagePath
            txtImagePath.Text = imagePath

            Dim progressReporter = New Progress(Of String)(
                Sub(msg) lblStatus.Text = msg)

            Dim result = UFS2ImageService.OpenImage(imagePath, progressReporter)
            If Not result.Success Then
                progressBar.Visible = False
                lblStatus.Text = "Error"
                MessageBox.Show(result.ErrorMessage, "UFS2 Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            _reader = result.Reader
            _fileTree = result.FileTree

            ' Populate TreeView
            PopulateTreeView(_fileTree)

            ' Update status
            Dim totalFiles = _fileTree.TotalFileCount
            Dim totalSize = _fileTree.TotalSize
            lblFileCount.Text = $"Files: {totalFiles}"
            lblTotalSize.Text = $"Size: {FormatSize(totalSize)}"
            lblStatus.Text = $"Loaded: {Path.GetFileName(imagePath)}"

            btnExtractAll.Enabled = True
            btnExportReport.Enabled = True
            btnBuildFPKG.Enabled = True
            progressBar.Visible = False

            ' Show superblock summary
            ShowSuperblockSummary()
        Catch ex As Exception
            progressBar.Visible = False
            lblStatus.Text = "Error"
            MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PopulateTreeView(rootNode As UFS2FileNode)
        treeView.BeginUpdate()
        treeView.Nodes.Clear()

        Dim treeRoot As New TreeNode(rootNode.Name) With {.Tag = rootNode}
        AddChildNodes(treeRoot, rootNode)
        treeView.Nodes.Add(treeRoot)
        treeRoot.Expand()

        treeView.EndUpdate()
    End Sub

    Private Sub AddChildNodes(parentTreeNode As TreeNode, parentFileNode As UFS2FileNode)
        For Each child In parentFileNode.Children.OrderByDescending(Function(c) c.IsDirectory).ThenBy(Function(c) c.Name)
            Dim childTreeNode As New TreeNode(child.Name) With {.Tag = child}
            If child.IsDirectory Then
                childTreeNode.ImageIndex = 0
                AddChildNodes(childTreeNode, child)
            End If
            parentTreeNode.Nodes.Add(childTreeNode)
        Next
    End Sub

    Private Sub PopulateGrid(node As UFS2FileNode)
        dgvFiles.Rows.Clear()

        Dim children = If(node.IsDirectory, node.Children, New List(Of UFS2FileNode)({node}))

        For Each child In children.OrderByDescending(Function(c) c.IsDirectory).ThenBy(Function(c) c.Name)
            Dim rowIdx = dgvFiles.Rows.Add(
                child.Name,
                child.FileType,
                If(child.IsDirectory, $"{child.TotalFileCount} items", FormatSize(child.Size)),
                If(child.ModifiedDate = DateTime.MinValue, "-", child.ModifiedDate.ToString("yyyy-MM-dd HH:mm")),
                child.InodeNumber.ToString(),
                child.FullPath
            )

            If child.IsDirectory Then
                dgvFiles.Rows(rowIdx).DefaultCellStyle.ForeColor = Color.DarkBlue
            End If
        Next
    End Sub

    Private Sub ShowNodeDetails(node As UFS2FileNode)
        txtDetails.Clear()
        AppendStyled("UFS2 File Details" & vbCrLf, Color.Blue, bold:=True)
        txtDetails.AppendText("---" & vbCrLf)
        txtDetails.AppendText($"Name:       {node.Name}" & vbCrLf)
        txtDetails.AppendText($"Path:       {node.FullPath}" & vbCrLf)
        txtDetails.AppendText($"Inode:      {node.InodeNumber}" & vbCrLf)
        txtDetails.AppendText($"Type:       {node.FileType}" & vbCrLf)
        txtDetails.AppendText($"Size:       {FormatSize(node.Size)}" & vbCrLf)
        txtDetails.AppendText($"Permissions:{node.PermissionsString}" & vbCrLf)
        txtDetails.AppendText($"Modified:   {If(node.ModifiedDate = DateTime.MinValue, "-", node.ModifiedDate.ToString())}" & vbCrLf)

        If node.IsDirectory Then
            txtDetails.AppendText(vbCrLf)
            txtDetails.AppendText($"Children:   {node.Children.Count}" & vbCrLf)
            txtDetails.AppendText($"Total Files:{node.TotalFileCount}" & vbCrLf)
            txtDetails.AppendText($"Total Size: {FormatSize(node.TotalSize)}" & vbCrLf)
        End If
    End Sub

    Private Sub ShowFileDetails(row As DataGridViewRow)
        txtDetails.Clear()
        txtDetails.AppendText($"Name:     {row.Cells("Name").Value}" & vbCrLf)
        txtDetails.AppendText($"Type:     {row.Cells("Type").Value}" & vbCrLf)
        txtDetails.AppendText($"Size:     {row.Cells("Size").Value}" & vbCrLf)
        txtDetails.AppendText($"Modified: {row.Cells("Modified").Value}" & vbCrLf)
        txtDetails.AppendText($"Path:     {row.Cells("FullPath").Value}" & vbCrLf)
    End Sub

    Private Sub ShowSuperblockSummary()
        If _reader?.Superblock Is Nothing Then Return
        txtDetails.Clear()
        AppendStyled("UFS2 Image Summary" & vbCrLf, Color.Blue, bold:=True)
        txtDetails.AppendText("---" & vbCrLf)
        txtDetails.AppendText(_reader.Superblock.ToSummary())
        txtDetails.AppendText(vbCrLf)
        txtDetails.AppendText($"Total Files: {_fileTree?.TotalFileCount}" & vbCrLf)
        txtDetails.AppendText($"Total Size:  {FormatSize(If(_fileTree?.TotalSize, 0L))}" & vbCrLf)
    End Sub

    ' ===== EXTRACT OPERATIONS =====

    Private Sub BtnExtractAll_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing OrElse _fileTree Is Nothing Then Return

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select output folder for extracted files"
            If fbd.ShowDialog() <> DialogResult.OK Then Return

            Try
                lblStatus.Text = "Extracting all files..."
                progressBar.Visible = True
                progressBar.Value = 0
                progressBar.Maximum = 100
                Application.DoEvents()

                Dim progressReporter = New Progress(Of Integer)(
                    Sub(pct)
                        progressBar.Value = Math.Min(pct, 100)
                        Application.DoEvents()
                    End Sub)

                UFS2ImageService.ExtractAll(_reader, _fileTree, fbd.SelectedPath, progressReporter)

                progressBar.Visible = False
                lblStatus.Text = "Extraction complete"
                MessageBox.Show($"All files extracted to: {fbd.SelectedPath}", "Extraction Complete",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                progressBar.Visible = False
                lblStatus.Text = "Extraction failed"
                MessageBox.Show($"Extraction error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub MnuExtractFile_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing Then Return

        ' Collect selected inodes from grid
        Dim selectedFiles As New List(Of Tuple(Of UInteger, String))()
        For Each row As DataGridViewRow In dgvFiles.SelectedRows
            Dim inodeStr = row.Cells("InodeNum").Value?.ToString()
            Dim name = row.Cells("Name").Value?.ToString()
            Dim inodeNum As UInteger
            If UInteger.TryParse(inodeStr, inodeNum) Then
                selectedFiles.Add(Tuple.Create(inodeNum, name))
            End If
        Next

        If selectedFiles.Count = 0 Then
            MessageBox.Show("No files selected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select output folder"
            If fbd.ShowDialog() <> DialogResult.OK Then Return

            Dim success = 0
            For Each item In selectedFiles
                Try
                    Dim outputPath = Path.Combine(fbd.SelectedPath, item.Item2)
                    _reader.ExtractFile(item.Item1, outputPath)
                    success += 1
                Catch
                End Try
            Next

            MessageBox.Show($"Extracted {success}/{selectedFiles.Count} file(s)", "Done",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    Private Sub MnuExtractDirectory_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing Then Return

        ' Get selected tree node
        Dim selectedNode = TryCast(treeView.SelectedNode?.Tag, UFS2FileNode)
        If selectedNode Is Nothing OrElse Not selectedNode.IsDirectory Then
            MessageBox.Show("Please select a directory in the tree view", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select output folder"
            If fbd.ShowDialog() <> DialogResult.OK Then Return

            Try
                lblStatus.Text = "Extracting directory..."
                progressBar.Visible = True
                Application.DoEvents()

                Dim progressReporter = New Progress(Of Integer)(
                    Sub(pct)
                        progressBar.Value = Math.Min(pct, 100)
                        Application.DoEvents()
                    End Sub)

                UFS2ImageService.ExtractAll(_reader, selectedNode, fbd.SelectedPath, progressReporter)

                progressBar.Visible = False
                lblStatus.Text = "Directory extracted"
                MessageBox.Show("Directory extracted successfully", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                progressBar.Visible = False
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub BtnExportReport_Click(sender As Object, e As EventArgs)
        If _fileTree Is Nothing Then Return

        Using sfd As New SaveFileDialog()
            sfd.Filter = "Text Files|*.txt|CSV Files|*.csv"
            sfd.FileName = $"ufs2_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt"

            If sfd.ShowDialog() <> DialogResult.OK Then Return

            Try
                Using writer As New StreamWriter(sfd.FileName)
                    If sfd.FileName.EndsWith(".csv") Then
                        writer.WriteLine("Name,Type,Size,Modified,Inode,Path")
                        For Each f In _fileTree.GetAllFiles()
                            writer.WriteLine($"""{f.Name}"",{f.FileType},{f.Size},{f.ModifiedDate},{f.InodeNumber},""{f.FullPath}""")
                        Next
                    Else
                        writer.WriteLine($"UFS2 Image Report: {_currentImagePath}")
                        writer.WriteLine($"Generated: {DateTime.Now}")
                        writer.WriteLine()
                        writer.WriteLine(_reader?.Superblock?.ToSummary())
                        writer.WriteLine()
                        writer.WriteLine($"Total Files: {_fileTree.TotalFileCount}")
                        writer.WriteLine($"Total Size:  {FormatSize(_fileTree.TotalSize)}")
                        writer.WriteLine()
                        For Each f In _fileTree.GetAllFiles()
                            writer.WriteLine($"  {f.FullPath,-60} {FormatSize(f.Size),12}")
                        Next
                    End If
                End Using

                MessageBox.Show($"Report exported to: {sfd.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ' ===== BUILD FPKG =====

    Private Sub BtnBuildFPKG_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing OrElse _fileTree Is Nothing Then Return

        ' Create a default config from the image filename
        Dim imageName = Path.GetFileNameWithoutExtension(_currentImagePath)
        Dim config As New FPKGConfig With {
            .ContentId = $"IV0000-{imageName.Substring(0, Math.Min(9, imageName.Length)).ToUpperInvariant().PadRight(9, "0"c)}_00-{imageName.PadRight(16, "0"c).Substring(0, 16).ToUpperInvariant()}",
            .Title = imageName,
            .TitleId = imageName.Substring(0, Math.Min(9, imageName.Length)).ToUpperInvariant().PadRight(9, "0"c)
        }

        Using builderForm As New FPKGBuilderForm(_currentImagePath, config)
            builderForm.ShowDialog(Me)

            If builderForm.BuildSucceeded Then
                lblStatus.Text = $"FPKG built: {Path.GetFileName(builderForm.OutputFilePath)}"
            End If
        End Using
    End Sub

    ' ===== SEARCH =====

    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        Dim filter = txtSearch.Text.Trim().ToLowerInvariant()

        If String.IsNullOrEmpty(filter) Then
            ' Show all rows
            For Each row As DataGridViewRow In dgvFiles.Rows
                row.Visible = True
            Next
            Return
        End If

        dgvFiles.CurrentCell = Nothing
        For Each row As DataGridViewRow In dgvFiles.Rows
            Dim name = row.Cells("Name").Value?.ToString().ToLowerInvariant()
            Dim path = row.Cells("FullPath").Value?.ToString().ToLowerInvariant()
            row.Visible = (name IsNot Nothing AndAlso name.Contains(filter)) OrElse
                          (path IsNot Nothing AndAlso path.Contains(filter))
        Next
    End Sub

    ' ===== HEX PREVIEW =====

    Private Sub MnuViewHex_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing Then Return
        If dgvFiles.SelectedRows.Count = 0 Then Return

        Dim row = dgvFiles.SelectedRows(0)
        Dim inodeStr = row.Cells("InodeNum").Value?.ToString()
        Dim inodeNum As UInteger
        If Not UInteger.TryParse(inodeStr, inodeNum) Then Return

        Dim name = row.Cells("Name").Value?.ToString()

        Try
            Dim preview = _reader.ReadFilePreview(inodeNum, 4096)
            If preview Is Nothing OrElse preview.Length = 0 Then
                txtDetails.Clear()
                txtDetails.AppendText("(Empty file)")
                Return
            End If

            txtDetails.Clear()
            AppendStyled($"Hex Preview: {name} (first {preview.Length} bytes)" & vbCrLf, Color.Blue, bold:=True)
            txtDetails.AppendText("---" & vbCrLf)
            txtDetails.AppendText(FormatHexDump(preview, 16))
        Catch ex As Exception
            txtDetails.Clear()
            txtDetails.AppendText($"Error reading file: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Formats raw bytes as a hex dump with offset, hex bytes, and ASCII representation.
    ''' </summary>
    Private Shared Function FormatHexDump(data As Byte(), bytesPerLine As Integer) As String
        Dim sb As New System.Text.StringBuilder()
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

    ' ===== HELPERS =====

    Private Sub CleanupReader()
        _reader?.Dispose()
        _reader = Nothing
        _fileTree = Nothing
    End Sub

    Private Sub AppendStyled(text As String, Optional color As Color = Nothing, Optional bold As Boolean = False)
        txtDetails.SelectionStart = txtDetails.TextLength
        txtDetails.SelectionLength = 0
        txtDetails.SelectionColor = If(color = Nothing, txtDetails.ForeColor, color)
        txtDetails.SelectionFont = If(bold, New Font(txtDetails.Font, FontStyle.Bold), txtDetails.Font)
        txtDetails.AppendText(text)
        txtDetails.SelectionFont = txtDetails.Font
        txtDetails.SelectionColor = txtDetails.ForeColor
    End Sub

    Private Shared Function FormatSize(bytes As Long) As String
        If bytes < 1024 Then Return $"{bytes} B"
        If bytes < 1024 * 1024 Then Return $"{bytes / 1024.0:F1} KB"
        If bytes < 1024L * 1024 * 1024 Then Return $"{bytes / (1024.0 * 1024):F1} MB"
        Return $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    End Function

End Class
