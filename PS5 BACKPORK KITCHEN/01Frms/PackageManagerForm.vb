Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' PKG/FPKG Package Manager form - inspect and extract PlayStation package files.
''' Layout: ToolStrip + SplitContainer (info panel | file list + details) + StatusStrip.
''' </summary>
Public Class PackageManagerForm
    Inherits Form

    ' ---- UI Controls ----
    Private toolStrip As ToolStrip
    Private btnBrowse As ToolStripButton
    Private btnExtractAll As ToolStripButton
    Private btnViewSfo As ToolStripButton
    Private btnValidate As ToolStripButton
    Private btnViewBackground As ToolStripButton
    Private lblPkgPath As ToolStripLabel
    Private txtPkgPath As ToolStripTextBox

    Private splitContainer As SplitContainer

    ' Left panel: icon + info
    Private leftPanel As TableLayoutPanel
    Private picIcon As PictureBox
    Private grpInfo As GroupBox
    Private infoPanel As TableLayoutPanel
    Private lblTitle As Label
    Private lblContentId As Label
    Private lblTitleId As Label
    Private lblVersion As Label
    Private lblCategory As Label
    Private lblPkgType As Label
    Private lblContentType As Label
    Private lblDrmType As Label
    Private lblPkgSize As Label
    Private lblEntryCount As Label

    ' Right panel: grid + details
    Private rightPanel As TableLayoutPanel
    Private dgvEntries As DataGridView
    Private txtDetails As RichTextBox

    Private statusStrip As StatusStrip
    Private lblStatus As ToolStripStatusLabel
    Private lblEntryCountStatus As ToolStripStatusLabel
    Private lblPkgTypeStatus As ToolStripStatusLabel
    Private progressBar As ToolStripProgressBar

    ' ---- State ----
    Private _reader As PKGReader
    Private _currentPkgPath As String = ""

    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)
        InitializeFormLayout()
    End Sub

    Private Sub InitializeFormLayout()
        Me.Text = "PKG/FPKG Package Manager"
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

        lblPkgPath = New ToolStripLabel("PKG:")
        txtPkgPath = New ToolStripTextBox With {.AutoSize = False, .Width = 420, .ReadOnly = True}

        btnBrowse = New ToolStripButton("Browse...") With {.DisplayStyle = ToolStripItemDisplayStyle.Text}
        AddHandler btnBrowse.Click, AddressOf BtnBrowse_Click

        toolStrip.Items.Add(lblPkgPath)
        toolStrip.Items.Add(txtPkgPath)
        toolStrip.Items.Add(btnBrowse)
        toolStrip.Items.Add(New ToolStripSeparator())

        btnExtractAll = New ToolStripButton("Extract All") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnExtractAll.Click, AddressOf BtnExtractAll_Click
        toolStrip.Items.Add(btnExtractAll)

        btnViewSfo = New ToolStripButton("View SFO") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnViewSfo.Click, AddressOf BtnViewSfo_Click
        toolStrip.Items.Add(btnViewSfo)

        btnValidate = New ToolStripButton("Validate") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnValidate.Click, AddressOf BtnValidate_Click
        toolStrip.Items.Add(btnValidate)

        btnViewBackground = New ToolStripButton("View Pic1") With {.DisplayStyle = ToolStripItemDisplayStyle.Text, .Enabled = False}
        AddHandler btnViewBackground.Click, AddressOf BtnViewBackground_Click
        toolStrip.Items.Add(btnViewBackground)

        root.Controls.Add(toolStrip, 0, 0)

        ' ---- SplitContainer ----
        splitContainer = New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterDistance = 300
        }
        root.Controls.Add(splitContainer, 0, 1)

        ' ---- Left Panel: Icon + Info ----
        leftPanel = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2
        }
        leftPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 200))
        leftPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        splitContainer.Panel1.Controls.Add(leftPanel)

        ' PictureBox for icon0
        picIcon = New PictureBox With {
            .Dock = DockStyle.Fill,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.FromArgb(240, 240, 240),
            .BorderStyle = BorderStyle.FixedSingle
        }
        leftPanel.Controls.Add(picIcon, 0, 0)

        ' Info GroupBox
        grpInfo = New GroupBox With {
            .Text = "Package Info",
            .Dock = DockStyle.Fill
        }
        leftPanel.Controls.Add(grpInfo, 0, 1)

        BuildInfoPanel()

        ' ---- Right Panel: Grid + Details ----
        rightPanel = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2
        }
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 65))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 35))
        splitContainer.Panel2.Controls.Add(rightPanel)

        ' DataGridView for entries
        dgvEntries = New DataGridView With {
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
        AddHandler dgvEntries.SelectionChanged, AddressOf DgvEntries_SelectionChanged
        BuildEntryContextMenu()
        rightPanel.Controls.Add(dgvEntries, 0, 0)

        ' Details
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
        lblEntryCountStatus = New ToolStripStatusLabel("Entries: 0")
        lblPkgTypeStatus = New ToolStripStatusLabel("")
        progressBar = New ToolStripProgressBar With {.Visible = False, .Width = 180}
        statusStrip.Items.AddRange({lblStatus, lblEntryCountStatus, lblPkgTypeStatus, progressBar})
        root.Controls.Add(statusStrip, 0, 2)
    End Sub

    Private Sub BuildInfoPanel()
        infoPanel = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 10,
            .AutoScroll = True
        }
        infoPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))
        infoPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

        Dim rowIdx = 0
        lblTitle = AddInfoRow(infoPanel, "Title:", rowIdx) : rowIdx += 1
        lblContentId = AddInfoRow(infoPanel, "Content ID:", rowIdx) : rowIdx += 1
        lblTitleId = AddInfoRow(infoPanel, "Title ID:", rowIdx) : rowIdx += 1
        lblVersion = AddInfoRow(infoPanel, "Version:", rowIdx) : rowIdx += 1
        lblCategory = AddInfoRow(infoPanel, "Category:", rowIdx) : rowIdx += 1
        lblPkgType = AddInfoRow(infoPanel, "PKG Type:", rowIdx) : rowIdx += 1
        lblContentType = AddInfoRow(infoPanel, "Content:", rowIdx) : rowIdx += 1
        lblDrmType = AddInfoRow(infoPanel, "DRM:", rowIdx) : rowIdx += 1
        lblPkgSize = AddInfoRow(infoPanel, "Size:", rowIdx) : rowIdx += 1
        lblEntryCount = AddInfoRow(infoPanel, "Entries:", rowIdx)

        grpInfo.Controls.Add(infoPanel)
    End Sub

    Private Function AddInfoRow(panel As TableLayoutPanel, labelText As String, row As Integer) As Label
        Dim lbl As New Label With {
            .Text = labelText,
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panel.Controls.Add(lbl, 0, row)

        Dim val As New Label With {
            .Text = "-",
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 8.5F),
            .TextAlign = ContentAlignment.MiddleLeft,
            .AutoEllipsis = True
        }
        panel.Controls.Add(val, 1, row)
        Return val
    End Function

    Private Sub BuildGridColumns()
        dgvEntries.Columns.Add("FileName", "File Name")
        dgvEntries.Columns.Add("EntryId", "ID")
        dgvEntries.Columns.Add("Size", "Size")
        dgvEntries.Columns.Add("Status", "Status")
        dgvEntries.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "DataOffset", .HeaderText = "Offset", .Visible = False})
        dgvEntries.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Index", .HeaderText = "Index", .Visible = False})

        dgvEntries.Columns("EntryId").FillWeight = 15
        dgvEntries.Columns("Size").FillWeight = 15
        dgvEntries.Columns("Status").FillWeight = 15
    End Sub

    ' ===== EVENT HANDLERS =====

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Title = "Open PKG File"
            ofd.Filter = "PKG Files|*.pkg|All Files|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                LoadPackage(ofd.FileName)
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
            Dim ext = Path.GetExtension(paths(0)).ToLowerInvariant()
            If ext = ".pkg" Then
                LoadPackage(paths(0))
            End If
        End If
    End Sub

    Private Sub Form_Closing(sender As Object, e As FormClosingEventArgs)
        CleanupReader()
    End Sub

    Private Sub DgvEntries_SelectionChanged(sender As Object, e As EventArgs)
        If dgvEntries.SelectedRows.Count = 0 Then Return
        Dim row = dgvEntries.SelectedRows(0)
        ShowEntryDetails(row)
    End Sub

    ' ===== CORE LOGIC =====

    Private Sub LoadPackage(pkgPath As String)
        Try
            CleanupReader()
            dgvEntries.Rows.Clear()
            txtDetails.Clear()
            ClearInfoLabels()

            lblStatus.Text = "Opening PKG..."
            progressBar.Visible = True
            Application.DoEvents()

            _currentPkgPath = pkgPath
            txtPkgPath.Text = pkgPath

            Dim result = PackageManagerService.OpenPackage(pkgPath)
            If Not result.Success Then
                progressBar.Visible = False
                lblStatus.Text = "Error"
                MessageBox.Show(result.ErrorMessage, "PKG Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            _reader = result.Reader

            ' Populate info panel
            PopulateInfoPanel()

            ' Load icon
            Dim icon = PackageManagerService.GetIcon(_reader)
            If icon IsNot Nothing Then
                picIcon.Image = icon
            Else
                picIcon.Image = Nothing
            End If

            ' Populate entry grid
            PopulateEntryGrid()

            ' Update status
            lblEntryCountStatus.Text = $"Entries: {_reader.EntryTable.Entries.Count}"
            lblPkgTypeStatus.Text = _reader.Header.PackageTypeString

            If _reader.Header.IsFPKG Then
                lblPkgTypeStatus.ForeColor = Color.Green
            Else
                lblPkgTypeStatus.ForeColor = Color.Red
            End If

            btnExtractAll.Enabled = _reader.Header.IsFPKG
            btnViewSfo.Enabled = _reader.Metadata IsNot Nothing
            btnValidate.Enabled = True
            btnViewBackground.Enabled = _reader.EntryTable.Entries.Any(
                Function(ent) ent.Id = PKGConstants.ENTRY_ID_PIC1_PNG)
            lblStatus.Text = $"Loaded: {Path.GetFileName(pkgPath)}"
            progressBar.Visible = False

            ' Show initial summary
            ShowPackageSummary()
        Catch ex As Exception
            progressBar.Visible = False
            lblStatus.Text = "Error"
            MessageBox.Show($"Error loading PKG: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PopulateInfoPanel()
        Dim h = _reader.Header
        Dim m = _reader.Metadata

        If m IsNot Nothing Then
            lblTitle.Text = If(String.IsNullOrEmpty(m.Title), "-", m.Title)
            lblContentId.Text = If(String.IsNullOrEmpty(m.ContentId), h.ContentId, m.ContentId)
            lblTitleId.Text = If(String.IsNullOrEmpty(m.TitleId), "-", m.TitleId)
            lblVersion.Text = If(String.IsNullOrEmpty(m.AppVersion), m.Version, $"{m.AppVersion} (ver {m.Version})")
            lblCategory.Text = If(String.IsNullOrEmpty(m.Category), "-", m.Category)
        Else
            lblTitle.Text = "-"
            lblContentId.Text = h.ContentId
            lblTitleId.Text = "-"
            lblVersion.Text = "-"
            lblCategory.Text = "-"
        End If

        lblPkgType.Text = h.PackageTypeString
        lblContentType.Text = h.ContentTypeString
        lblDrmType.Text = $"0x{h.DrmType:X}"
        lblPkgSize.Text = FormatSize(h.PackageSize)
        lblEntryCount.Text = h.EntryCount.ToString()

        ' Color the PKG type label
        If h.IsFPKG Then
            lblPkgType.ForeColor = Color.Green
        Else
            lblPkgType.ForeColor = Color.Red
        End If
    End Sub

    Private Sub PopulateEntryGrid()
        dgvEntries.Rows.Clear()

        For i = 0 To _reader.EntryTable.Entries.Count - 1
            Dim entry = _reader.EntryTable.Entries(i)
            Dim status As String
            Dim statusColor As Color

            If _reader.Header.IsFPKG Then
                status = "Extractable"
                statusColor = Color.DarkGreen
            ElseIf entry.IsEncrypted Then
                status = "Encrypted"
                statusColor = Color.Red
            Else
                status = "Metadata"
                statusColor = Color.DarkBlue
            End If

            Dim rowIdx = dgvEntries.Rows.Add(
                entry.FileName,
                $"0x{entry.Id:X4}",
                entry.SizeString,
                status,
                $"0x{entry.DataOffset:X}",
                i.ToString()
            )

            ' Color-code rows
            dgvEntries.Rows(rowIdx).Cells("Status").Style.ForeColor = statusColor
            If entry.IsEncrypted AndAlso Not _reader.Header.IsFPKG Then
                dgvEntries.Rows(rowIdx).DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240)
            End If
        Next
    End Sub

    Private Sub ShowEntryDetails(row As DataGridViewRow)
        txtDetails.Clear()
        Dim indexStr = row.Cells("Index").Value?.ToString()
        Dim idx As Integer
        If Not Integer.TryParse(indexStr, idx) Then Return
        If idx < 0 OrElse idx >= _reader.EntryTable.Entries.Count Then Return

        Dim entry = _reader.EntryTable.Entries(idx)

        AppendStyled("PKG Entry Details" & vbCrLf, Color.Blue, bold:=True)
        txtDetails.AppendText("---" & vbCrLf)
        txtDetails.AppendText($"File Name:    {entry.FileName}" & vbCrLf)
        txtDetails.AppendText($"Entry ID:     0x{entry.Id:X4}" & vbCrLf)
        txtDetails.AppendText($"Data Offset:  0x{entry.DataOffset:X}" & vbCrLf)
        txtDetails.AppendText($"Data Size:    {entry.SizeString} ({entry.DataSize} bytes)" & vbCrLf)
        txtDetails.AppendText($"Flags1:       0x{entry.Flags1:X8}" & vbCrLf)
        txtDetails.AppendText($"Flags2:       0x{entry.Flags2:X8}" & vbCrLf)
        txtDetails.AppendText($"Encrypted:    {entry.IsEncrypted}" & vbCrLf)
    End Sub

    Private Sub ShowPackageSummary()
        txtDetails.Clear()
        AppendStyled("Package Summary" & vbCrLf, Color.Blue, bold:=True)
        txtDetails.AppendText("---" & vbCrLf)

        Dim h = _reader.Header
        txtDetails.AppendText($"Magic:         0x{h.Magic:X8}" & vbCrLf)
        txtDetails.AppendText($"Content ID:    {h.ContentId}" & vbCrLf)
        txtDetails.AppendText($"Package Type:  {h.PackageTypeString}" & vbCrLf)
        txtDetails.AppendText($"Content Type:  {h.ContentTypeString}" & vbCrLf)
        txtDetails.AppendText($"DRM Type:      0x{h.DrmType:X}" & vbCrLf)
        txtDetails.AppendText($"Entry Count:   {h.EntryCount}" & vbCrLf)
        txtDetails.AppendText($"Package Size:  {FormatSize(h.PackageSize)}" & vbCrLf)

        If _reader.Metadata IsNot Nothing Then
            txtDetails.AppendText(vbCrLf)
            AppendStyled("SFO Metadata" & vbCrLf, Color.DarkBlue, bold:=True)
            txtDetails.AppendText("---" & vbCrLf)
            For Each kvp In _reader.Metadata.AllParams
                txtDetails.AppendText($"{kvp.Key,-20} = {kvp.Value}" & vbCrLf)
            Next
        End If

        If Not h.IsFPKG Then
            txtDetails.AppendText(vbCrLf)
            AppendStyled("WARNING: This is a retail (encrypted) PKG." & vbCrLf, Color.Red, bold:=True)
            txtDetails.AppendText("Only metadata is available. File extraction requires an FPKG." & vbCrLf)
        End If
    End Sub

    ' ===== EXTRACT OPERATIONS =====

    Private Sub BtnExtractAll_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing Then Return

        If Not _reader.Header.IsFPKG Then
            MessageBox.Show("Only FPKG files can be extracted. This is a retail (encrypted) PKG.",
                            "Cannot Extract", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select output folder for extracted entries"
            If fbd.ShowDialog() <> DialogResult.OK Then Return

            Try
                lblStatus.Text = "Extracting all entries..."
                progressBar.Visible = True
                progressBar.Value = 0
                progressBar.Maximum = 100
                Application.DoEvents()

                Dim progressReporter = New Progress(Of Integer)(
                    Sub(pct)
                        progressBar.Value = Math.Min(pct, 100)
                        Application.DoEvents()
                    End Sub)

                PackageManagerService.ExtractAll(_reader, fbd.SelectedPath, progressReporter)

                progressBar.Visible = False
                lblStatus.Text = "Extraction complete"
                MessageBox.Show($"All entries extracted to: {fbd.SelectedPath}", "Extraction Complete",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                progressBar.Visible = False
                lblStatus.Text = "Extraction failed"
                MessageBox.Show($"Extraction error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub BtnViewSfo_Click(sender As Object, e As EventArgs)
        If _reader?.Metadata Is Nothing Then Return

        ' Get raw SFO data for hex view
        Dim rawSfoData As Byte() = Nothing
        Dim sfoEntry = _reader.EntryTable.Entries.FirstOrDefault(
            Function(ent) ent.Id = PKGConstants.ENTRY_ID_PARAM_SFO)
        If sfoEntry IsNot Nothing Then
            Try
                rawSfoData = _reader.ReadEntryData(sfoEntry)
            Catch
            End Try
        End If

        Using viewer As New SFOViewerForm(_reader.Metadata, rawSfoData)
            viewer.ShowDialog(Me)
        End Using
    End Sub

    Private Sub BtnValidate_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing Then Return

        txtDetails.Clear()
        AppendStyled("PKG Validation Report" & vbCrLf, Color.Blue, bold:=True)
        txtDetails.AppendText("=========================" & vbCrLf & vbCrLf)

        Dim errors As New List(Of String)()
        Dim warnings As New List(Of String)()
        Dim h = _reader.Header

        ' Check magic
        If h.Magic = PKGConstants.PKG_MAGIC Then
            txtDetails.AppendText("  [OK] Magic: 0x7F434E54" & vbCrLf)
        Else
            errors.Add($"Invalid magic: 0x{h.Magic:X8}")
        End If

        ' Check entry count
        If h.EntryCount > 0 AndAlso h.EntryCount < 10000 Then
            txtDetails.AppendText($"  [OK] Entry count: {h.EntryCount}" & vbCrLf)
        Else
            warnings.Add($"Unusual entry count: {h.EntryCount}")
        End If

        ' Check table offset
        If h.TableOffset > 0 AndAlso CLng(h.TableOffset) < h.PackageSize Then
            txtDetails.AppendText($"  [OK] Table offset: 0x{h.TableOffset:X}" & vbCrLf)
        Else
            errors.Add($"Table offset out of bounds: 0x{h.TableOffset:X}")
        End If

        ' Check body offset
        If h.BodyOffset > 0 AndAlso CLng(h.BodyOffset) <= h.PackageSize Then
            txtDetails.AppendText($"  [OK] Body offset: 0x{h.BodyOffset:X}" & vbCrLf)
        Else
            errors.Add($"Body offset out of bounds: 0x{h.BodyOffset:X}")
        End If

        ' Check each entry's data offset and size
        Dim entryErrors = 0
        For i = 0 To _reader.EntryTable.Entries.Count - 1
            Dim entry = _reader.EntryTable.Entries(i)
            Dim endPos = CLng(entry.DataOffset) + CLng(entry.DataSize)
            If endPos > h.PackageSize Then
                errors.Add($"Entry {i} (0x{entry.Id:X4}): data extends beyond file (offset 0x{entry.DataOffset:X} + size {entry.DataSize} > {h.PackageSize})")
                entryErrors += 1
                If entryErrors >= 5 Then
                    errors.Add("... (additional entry errors omitted)")
                    Exit For
                End If
            End If
        Next

        If entryErrors = 0 Then
            txtDetails.AppendText($"  [OK] All {_reader.EntryTable.Entries.Count} entries within file bounds" & vbCrLf)
        End If

        ' Check for overlapping entries
        Dim sorted = _reader.EntryTable.Entries.Where(Function(ent) ent.DataSize > 0).OrderBy(Function(ent) ent.DataOffset).ToList()
        Dim overlapCount = 0
        For i = 0 To sorted.Count - 2
            Dim endCurrent = sorted(i).DataOffset + sorted(i).DataSize
            If endCurrent > sorted(i + 1).DataOffset Then
                warnings.Add($"Entries 0x{sorted(i).Id:X4} and 0x{sorted(i + 1).Id:X4} overlap")
                overlapCount += 1
                If overlapCount >= 3 Then Exit For
            End If
        Next

        If overlapCount = 0 Then
            txtDetails.AppendText("  [OK] No overlapping entries" & vbCrLf)
        End If

        ' Summary
        txtDetails.AppendText(vbCrLf)
        If errors.Count = 0 AndAlso warnings.Count = 0 Then
            AppendStyled("VALIDATION PASSED - No issues found." & vbCrLf, Color.Green, bold:=True)
        Else
            If errors.Count > 0 Then
                AppendStyled($"ERRORS ({errors.Count}):" & vbCrLf, Color.Red, bold:=True)
                For Each errMsg In errors
                    txtDetails.AppendText($"  [ERROR] {errMsg}" & vbCrLf)
                Next
            End If
            If warnings.Count > 0 Then
                AppendStyled($"WARNINGS ({warnings.Count}):" & vbCrLf, Color.DarkOrange, bold:=True)
                For Each warn In warnings
                    txtDetails.AppendText($"  [WARN] {warn}" & vbCrLf)
                Next
            End If
        End If

        lblStatus.Text = $"Validation: {errors.Count} error(s), {warnings.Count} warning(s)"
    End Sub

    Private Sub BtnViewBackground_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing Then Return

        Dim pic1Entry = _reader.EntryTable.Entries.FirstOrDefault(
            Function(ent) ent.Id = PKGConstants.ENTRY_ID_PIC1_PNG)

        If pic1Entry Is Nothing Then
            MessageBox.Show("No pic1.png found in this package.", "Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim data = _reader.ReadEntryData(pic1Entry)
            If data Is Nothing OrElse data.Length = 0 Then
                MessageBox.Show("Cannot read pic1.png (encrypted or empty).", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using ms As New MemoryStream(data)
                Dim img = Drawing.Image.FromStream(ms)

                Using viewer As New Form()
                    viewer.Text = "Background Image (pic1.png)"
                    viewer.Size = New Size(800, 500)
                    viewer.StartPosition = FormStartPosition.CenterParent

                    Dim pb As New PictureBox With {
                        .Dock = DockStyle.Fill,
                        .SizeMode = PictureBoxSizeMode.Zoom,
                        .Image = img
                    }
                    viewer.Controls.Add(pb)
                    viewer.ShowDialog(Me)

                    pb.Image = Nothing
                    img.Dispose()
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading pic1.png: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ===== HELPERS =====

    Private Sub CleanupReader()
        If picIcon.Image IsNot Nothing Then
            Dim oldImg = picIcon.Image
            picIcon.Image = Nothing
            oldImg.Dispose()
        End If
        _reader?.Dispose()
        _reader = Nothing
    End Sub

    Private Sub ClearInfoLabels()
        lblTitle.Text = "-"
        lblContentId.Text = "-"
        lblTitleId.Text = "-"
        lblVersion.Text = "-"
        lblCategory.Text = "-"
        lblPkgType.Text = "-"
        lblContentType.Text = "-"
        lblDrmType.Text = "-"
        lblPkgSize.Text = "-"
        lblEntryCount.Text = "-"
        lblPkgType.ForeColor = SystemColors.ControlText
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

    ' ===== ENTRY CONTEXT MENU =====

    Private Sub BuildEntryContextMenu()
        Dim menu As New ContextMenuStrip()

        Dim mnuExtract As New ToolStripMenuItem("Extract Selected")
        AddHandler mnuExtract.Click, AddressOf MnuExtractSelected_Click
        menu.Items.Add(mnuExtract)

        Dim mnuExtractTo As New ToolStripMenuItem("Extract Selected To...")
        AddHandler mnuExtractTo.Click, AddressOf MnuExtractSelectedTo_Click
        menu.Items.Add(mnuExtractTo)

        menu.Items.Add(New ToolStripSeparator())

        Dim mnuPreview As New ToolStripMenuItem("Preview")
        AddHandler mnuPreview.Click, AddressOf MnuPreview_Click
        menu.Items.Add(mnuPreview)

        AddHandler menu.Opening, Sub(s, e)
                                     Dim hasSelection = dgvEntries.SelectedRows.Count > 0
                                     Dim isFpkg = _reader IsNot Nothing AndAlso _reader.Header.IsFPKG
                                     mnuExtract.Enabled = hasSelection AndAlso isFpkg
                                     mnuExtractTo.Enabled = hasSelection AndAlso isFpkg
                                     mnuPreview.Enabled = hasSelection AndAlso isFpkg
                                     Dim count = dgvEntries.SelectedRows.Count
                                     mnuExtract.Text = If(count > 1, $"Extract Selected ({count} files)", "Extract Selected")
                                 End Sub

        dgvEntries.ContextMenuStrip = menu
    End Sub

    Private Sub MnuExtractSelected_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing OrElse Not _reader.Header.IsFPKG Then
            MessageBox.Show("Only FPKG files support extraction.", "Extract",
                          MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select output folder for extracted files"
            If fbd.ShowDialog() <> DialogResult.OK Then Return
            ExtractSelectedEntries(fbd.SelectedPath)
        End Using
    End Sub

    Private Sub MnuExtractSelectedTo_Click(sender As Object, e As EventArgs)
        MnuExtractSelected_Click(sender, e)
    End Sub

    Private Sub ExtractSelectedEntries(outputDir As String)
        Dim selectedEntries As New List(Of PKGEntry)

        For Each row As DataGridViewRow In dgvEntries.SelectedRows
            Dim idxStr = row.Cells("Index").Value?.ToString()
            Dim idx As Integer
            If Integer.TryParse(idxStr, idx) AndAlso
               idx >= 0 AndAlso idx < _reader.EntryTable.Entries.Count Then
                selectedEntries.Add(_reader.EntryTable.Entries(idx))
            End If
        Next

        If selectedEntries.Count = 0 Then
            MessageBox.Show("No entries selected.", "Extract", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        lblStatus.Text = $"Extracting {selectedEntries.Count} entries..."
        progressBar.Visible = True
        progressBar.Maximum = selectedEntries.Count
        progressBar.Value = 0
        Application.DoEvents()

        Dim successCount = 0
        Dim failCount = 0

        For Each entry In selectedEntries
            Dim fileName = If(String.IsNullOrEmpty(entry.FileName),
                              $"entry_0x{entry.Id:X4}", entry.FileName)
            For Each c In Path.GetInvalidFileNameChars()
                fileName = fileName.Replace(c, "_"c)
            Next
            Dim outputPath = Path.Combine(outputDir, fileName)

            Try
                If _reader.ExtractEntry(entry, outputPath) Then
                    successCount += 1
                Else
                    failCount += 1
                End If
            Catch
                failCount += 1
            End Try

            progressBar.Value += 1
            Application.DoEvents()
        Next

        progressBar.Visible = False
        lblStatus.Text = $"Extracted {successCount}/{selectedEntries.Count} entries"

        Dim msg = $"Extracted {successCount} entries to:{vbCrLf}{outputDir}"
        If failCount > 0 Then msg &= $"{vbCrLf}{vbCrLf}{failCount} entries failed (encrypted or invalid)"
        MessageBox.Show(msg, "Extraction Complete",
                      MessageBoxButtons.OK,
                      If(failCount > 0, MessageBoxIcon.Warning, MessageBoxIcon.Information))
    End Sub

    Private Sub MnuPreview_Click(sender As Object, e As EventArgs)
        If _reader Is Nothing OrElse dgvEntries.SelectedRows.Count = 0 Then Return

        Dim row = dgvEntries.SelectedRows(0)
        Dim idxStr = row.Cells("Index").Value?.ToString()
        Dim idx As Integer
        If Not Integer.TryParse(idxStr, idx) Then Return
        If idx < 0 OrElse idx >= _reader.EntryTable.Entries.Count Then Return

        Dim entry = _reader.EntryTable.Entries(idx)

        txtDetails.Clear()
        AppendStyled($"Preview: {entry.FileName}" & vbCrLf, Color.Blue, True)
        AppendStyled($"Entry ID: 0x{entry.Id:X4}  |  Size: {FormatSize(CLng(entry.DataSize))}  |  Offset: 0x{entry.DataOffset:X}" & vbCrLf, Color.Gray)
        txtDetails.AppendText("─────────────────────────────────────────" & vbCrLf)

        If entry.IsEncrypted Then
            AppendStyled("This entry is encrypted and cannot be previewed." & vbCrLf, Color.Red, True)
            Return
        End If

        Dim data As Byte()
        Try
            data = _reader.ReadEntryData(entry)
        Catch ex As Exception
            AppendStyled($"Error reading entry: {ex.Message}" & vbCrLf, Color.Red)
            Return
        End Try

        If data Is Nothing OrElse data.Length = 0 Then
            AppendStyled("Entry contains no data." & vbCrLf, Color.Gray)
            Return
        End If

        ' Detect if content is text (no null bytes in first 512 bytes, except at very end)
        Dim previewSize = Math.Min(data.Length, 4096)
        Dim checkSize = Math.Min(data.Length, 512)
        Dim isText = True
        For i = 0 To checkSize - 1
            Dim b = data(i)
            If b = 0 AndAlso i < checkSize - 1 Then
                isText = False
                Exit For
            End If
            If b < 32 AndAlso b <> 10 AndAlso b <> 13 AndAlso b <> 9 AndAlso b <> 0 Then
                isText = False
                Exit For
            End If
        Next

        If isText Then
            AppendStyled("[Text Preview]" & vbCrLf & vbCrLf, Color.DarkGreen, True)
            Dim text = System.Text.Encoding.UTF8.GetString(data, 0, previewSize)
            txtDetails.AppendText(text)
        Else
            AppendStyled("[Hex Preview]" & vbCrLf & vbCrLf, Color.DarkGreen, True)
            Dim hexSize = Math.Min(data.Length, 1024)
            For offset = 0 To hexSize - 1 Step 16
                Dim hexPart As New System.Text.StringBuilder()
                Dim ascPart As New System.Text.StringBuilder()
                For i = 0 To 15
                    If offset + i < data.Length Then
                        hexPart.Append(data(offset + i).ToString("X2"))
                        hexPart.Append(" "c)
                        Dim b = data(offset + i)
                        ascPart.Append(If(b >= 32 AndAlso b < 127, ChrW(b), "."c))
                    Else
                        hexPart.Append("   ")
                    End If
                    If i = 7 Then hexPart.Append(" "c)
                Next
                txtDetails.AppendText($"{offset:X8}  {hexPart}  {ascPart}" & vbCrLf)
            Next

            If data.Length > 1024 Then
                AppendStyled($"{vbCrLf}... ({FormatSize(data.Length)} total, showing first 1 KB)" & vbCrLf, Color.Gray)
            End If
        End If
    End Sub

End Class
