Imports PS5_BACKPORK_KITCHEN.Services.GameSearch
Imports System.Windows.Forms
Imports System.Threading

''' <summary>
''' Game Search Form - Search for PS5/PS4 games from various sources
''' </summary>
Public Class GameSearchForm
    Inherits Form

    Private _searchManager As GameSearchManager
    Private _cancellationTokenSource As CancellationTokenSource
    Private _searchResults As New List(Of GameSearchResult)

    ' UI Controls
    Private txtSearch As TextBox
    Private cmbPlatform As ComboBox
    Private cmbProvider As ComboBox
    Private cmbSortBy As ComboBox
    Private btnSearch As Button
    Private btnCancel As Button
    Private btnSettings As Button
    Private dgvResults As DataGridView
    Private lblStatus As Label
    Private progressBar As ProgressBar
    Private contextMenu As ContextMenuStrip

    Public Sub New()
        InitializeComponent()
        _searchManager = New GameSearchManager()
        SetupEventHandlers()
        LoadProviders()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Game Search - PS5/PS4"
        Me.Size = New Size(1100, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(900, 500)

        ' Search Panel
        Dim pnlSearch As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .Padding = New Padding(10)
        }

        ' Search textbox
        Dim lblSearch As New Label With {
            .Text = "Search:",
            .Location = New Point(10, 18),
            .AutoSize = True
        }
        pnlSearch.Controls.Add(lblSearch)

        txtSearch = New TextBox With {
            .Location = New Point(70, 15),
            .Size = New Size(300, 25),
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler txtSearch.KeyPress, AddressOf txtSearch_KeyPress
        pnlSearch.Controls.Add(txtSearch)

        ' Platform combo
        Dim lblPlatform As New Label With {
            .Text = "Platform:",
            .Location = New Point(390, 18),
            .AutoSize = True
        }
        pnlSearch.Controls.Add(lblPlatform)

        cmbPlatform = New ComboBox With {
            .Location = New Point(455, 15),
            .Size = New Size(100, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbPlatform.Items.AddRange({"All", "PS5", "PS4"})
        cmbPlatform.SelectedIndex = 1 ' Default PS5
        pnlSearch.Controls.Add(cmbPlatform)

        ' Provider combo
        Dim lblProvider As New Label With {
            .Text = "Source:",
            .Location = New Point(570, 18),
            .AutoSize = True
        }
        pnlSearch.Controls.Add(lblProvider)

        cmbProvider = New ComboBox With {
            .Location = New Point(625, 15),
            .Size = New Size(120, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        pnlSearch.Controls.Add(cmbProvider)

        ' Sort combo
        Dim lblSort As New Label With {
            .Text = "Sort:",
            .Location = New Point(760, 18),
            .AutoSize = True
        }
        pnlSearch.Controls.Add(lblSort)

        cmbSortBy = New ComboBox With {
            .Location = New Point(800, 15),
            .Size = New Size(100, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbSortBy.Items.AddRange({"Seeds", "Size", "Date", "Name"})
        cmbSortBy.SelectedIndex = 0
        pnlSearch.Controls.Add(cmbSortBy)

        ' Search button
        btnSearch = New Button With {
            .Text = "Search",
            .Location = New Point(920, 13),
            .Size = New Size(80, 28),
            .BackColor = Color.FromArgb(0, 122, 204),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        ' Cancel button
        btnCancel = New Button With {
            .Text = "Cancel",
            .Location = New Point(1005, 13),
            .Size = New Size(70, 28),
            .Visible = False,
            .BackColor = Color.FromArgb(200, 50, 50),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        pnlSearch.Controls.Add(btnCancel)

        ' Settings button
        btnSettings = New Button With {
            .Text = "Settings",
            .Location = New Point(10, 48),
            .Size = New Size(80, 25),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnSettings.Click, AddressOf btnSettings_Click
        pnlSearch.Controls.Add(btnSettings)

        ' Progress bar
        progressBar = New ProgressBar With {
            .Location = New Point(100, 50),
            .Size = New Size(970, 20),
            .Style = ProgressBarStyle.Marquee,
            .Visible = False
        }
        pnlSearch.Controls.Add(progressBar)

        Me.Controls.Add(pnlSearch)

        ' Status bar (add FIRST for proper docking order)
        Dim statusPanel As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 25,
            .BackColor = Color.FromArgb(240, 240, 240)
        }

        lblStatus = New Label With {
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Text = "Ready. Enter search term and click Search.",
            .Padding = New Padding(5, 0, 0, 0)
        }
        statusPanel.Controls.Add(lblStatus)
        Me.Controls.Add(statusPanel)

        ' Results DataGridView (add LAST for Fill to work properly)
        dgvResults = New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoGenerateColumns = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.FromArgb(250, 250, 250),
            .GridColor = Color.FromArgb(220, 220, 220),
            .BorderStyle = BorderStyle.FixedSingle,
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            .ColumnHeadersHeight = 35,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            .AllowUserToResizeRows = False,
            .RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
        }
        dgvResults.RowTemplate.Height = 30

        ' Cell style - BLACK text on WHITE background
        dgvResults.DefaultCellStyle.BackColor = Color.White
        dgvResults.DefaultCellStyle.ForeColor = Color.Black
        dgvResults.DefaultCellStyle.Font = New Font("Segoe UI", 9)
        dgvResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204)
        dgvResults.DefaultCellStyle.SelectionForeColor = Color.White
        dgvResults.DefaultCellStyle.Padding = New Padding(3)

        ' Alternate row color
        dgvResults.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
        dgvResults.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black

        ' Header style
        dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48)
        dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgvResults.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        dgvResults.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        dgvResults.EnableHeadersVisualStyles = False

        SetupResultsGrid()
        AddHandler dgvResults.CellDoubleClick, AddressOf dgvResults_CellDoubleClick
        AddHandler dgvResults.MouseClick, AddressOf dgvResults_MouseClick
        Me.Controls.Add(dgvResults)
        dgvResults.BringToFront()

        ' Context menu
        contextMenu = New ContextMenuStrip()

        Dim mnuCopyMagnet As New ToolStripMenuItem("Copy Magnet Link")
        AddHandler mnuCopyMagnet.Click, AddressOf CopyMagnetLink_Click
        contextMenu.Items.Add(mnuCopyMagnet)

        Dim mnuOpenDetails As New ToolStripMenuItem("Open Details Page")
        AddHandler mnuOpenDetails.Click, AddressOf OpenDetails_Click
        contextMenu.Items.Add(mnuOpenDetails)

        contextMenu.Items.Add(New ToolStripSeparator())

        Dim mnuOpenMagnet As New ToolStripMenuItem("Open with Torrent Client")
        AddHandler mnuOpenMagnet.Click, AddressOf OpenMagnet_Click
        contextMenu.Items.Add(mnuOpenMagnet)
    End Sub

    Private Sub SetupResultsGrid()
        dgvResults.Columns.Clear()

        ' Add columns in order - Title FIRST and widest
        Dim colTitle As New DataGridViewTextBoxColumn With {
            .Name = "Title",
            .HeaderText = "Game Title",
            .DataPropertyName = "Title",
            .MinimumWidth = 300,
            .Width = 450,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .FillWeight = 100
        }
        dgvResults.Columns.Add(colTitle)

        Dim colPlatform As New DataGridViewTextBoxColumn With {
            .Name = "Platform",
            .HeaderText = "Platform",
            .DataPropertyName = "Platform",
            .Width = 70,
            .MinimumWidth = 50,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        }
        dgvResults.Columns.Add(colPlatform)

        Dim colSize As New DataGridViewTextBoxColumn With {
            .Name = "Size",
            .HeaderText = "Size",
            .DataPropertyName = "DisplaySize",
            .Width = 90,
            .MinimumWidth = 70,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        }
        dgvResults.Columns.Add(colSize)

        Dim colSeeds As New DataGridViewTextBoxColumn With {
            .Name = "Seeds",
            .HeaderText = "Seeds",
            .DataPropertyName = "Seeds",
            .Width = 60,
            .MinimumWidth = 50,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        }
        colSeeds.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgvResults.Columns.Add(colSeeds)

        Dim colLeeches As New DataGridViewTextBoxColumn With {
            .Name = "Leeches",
            .HeaderText = "Leeches",
            .DataPropertyName = "Leeches",
            .Width = 65,
            .MinimumWidth = 50,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        }
        colLeeches.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgvResults.Columns.Add(colLeeches)

        Dim colFW As New DataGridViewTextBoxColumn With {
            .Name = "FW",
            .HeaderText = "FW",
            .DataPropertyName = "FirmwareRequired",
            .Width = 50,
            .MinimumWidth = 40,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        }
        dgvResults.Columns.Add(colFW)

        Dim colSource As New DataGridViewTextBoxColumn With {
            .Name = "Source",
            .HeaderText = "Source",
            .DataPropertyName = "SourceProvider",
            .Width = 80,
            .MinimumWidth = 60,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        }
        dgvResults.Columns.Add(colSource)
    End Sub

    Private Sub SetupEventHandlers()
        AddHandler _searchManager.SearchStarted, Sub(s, name)
                                                     Me.Invoke(Sub()
                                                                   lblStatus.Text = $"Searching {name}..."
                                                               End Sub)
                                                 End Sub

        AddHandler _searchManager.SearchCompleted, Sub(s, name, count)
                                                       Me.Invoke(Sub()
                                                                     lblStatus.Text = $"Found {count} results from {name}"
                                                                 End Sub)
                                                   End Sub

        AddHandler _searchManager.SearchError, Sub(s, name, errMsg)
                                                   Me.Invoke(Sub()
                                                                 lblStatus.Text = $"Error from {name}: {errMsg}"
                                                             End Sub)
                                               End Sub
    End Sub

    Private Sub LoadProviders()
        cmbProvider.Items.Clear()
        cmbProvider.Items.Add("All Sources")

        For Each provider In _searchManager.Providers.Values
            Dim displayText = provider.DisplayName
            If provider.RequiresAuthentication Then
                displayText &= If(provider.IsLoggedIn, " (Logged In)", " (Login Required)")
            End If
            cmbProvider.Items.Add(displayText)
        Next

        cmbProvider.SelectedIndex = 0
    End Sub

    Private Sub txtSearch_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            btnSearch_Click(sender, EventArgs.Empty)
        End If
    End Sub

    Private Async Sub btnSearch_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSearch.Text) Then
            MessageBox.Show("Please enter a search term.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Setup UI for search
        btnSearch.Enabled = False
        btnCancel.Visible = True
        progressBar.Visible = True
        dgvResults.DataSource = Nothing
        _searchResults.Clear()

        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Dim query As New GameSearchQuery With {
                .SearchText = txtSearch.Text.Trim(),
                .Platform = GetSelectedPlatform(),
                .SortBy = GetSelectedSort(),
                .MaxResults = 100
            }

            Dim selectedProvider = GetSelectedProviderName()

            If String.IsNullOrEmpty(selectedProvider) Then
                ' Search all
                _searchResults = Await _searchManager.SearchAllAsync(query, _cancellationTokenSource.Token)
            Else
                ' Search specific provider
                _searchResults = Await _searchManager.SearchProviderAsync(selectedProvider, query, _cancellationTokenSource.Token)
            End If

            ' Display results
            dgvResults.SuspendLayout()
            dgvResults.DataSource = Nothing
            dgvResults.Rows.Clear()

            ' Add rows manually to ensure data is displayed
            For Each result In _searchResults
                Dim rowIndex = dgvResults.Rows.Add()
                dgvResults.Rows(rowIndex).Cells("Title").Value = result.Title
                dgvResults.Rows(rowIndex).Cells("Platform").Value = result.Platform
                dgvResults.Rows(rowIndex).Cells("Size").Value = result.DisplaySize
                dgvResults.Rows(rowIndex).Cells("Seeds").Value = result.Seeds
                dgvResults.Rows(rowIndex).Cells("Leeches").Value = result.Leeches
                dgvResults.Rows(rowIndex).Cells("FW").Value = result.FirmwareRequired
                dgvResults.Rows(rowIndex).Cells("Source").Value = result.SourceProvider
                dgvResults.Rows(rowIndex).Tag = result ' Store result for later use
            Next

            dgvResults.ResumeLayout()
            dgvResults.Refresh()

            ColorCodeResults()

            lblStatus.Text = $"Found {_searchResults.Count} result(s)"

            ' Debug: show first title in status if available
            If _searchResults.Count > 0 Then
                lblStatus.Text &= $" - First: {_searchResults(0).Title?.Substring(0, Math.Min(50, If(_searchResults(0).Title?.Length, 0)))}"
            End If

        Catch ex As OperationCanceledException
            lblStatus.Text = "Search cancelled"
        Catch ex As Exception
            lblStatus.Text = $"Error: {ex.Message}"
            MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnSearch.Enabled = True
            btnCancel.Visible = False
            progressBar.Visible = False
            _cancellationTokenSource?.Dispose()
            _cancellationTokenSource = Nothing
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        _cancellationTokenSource?.Cancel()
    End Sub

    Private Sub btnSettings_Click(sender As Object, e As EventArgs)
        Using settingsForm As New GameSearchSettingsForm(_searchManager)
            If settingsForm.ShowDialog() = DialogResult.OK Then
                LoadProviders() ' Refresh provider list
            End If
        End Using
    End Sub

    Private Sub ColorCodeResults()
        For Each row As DataGridViewRow In dgvResults.Rows
            Try
                Dim seeds = CInt(row.Cells("Seeds").Value)
                If seeds >= 50 Then
                    row.Cells("Seeds").Style.ForeColor = Color.Green
                ElseIf seeds >= 10 Then
                    row.Cells("Seeds").Style.ForeColor = Color.Orange
                Else
                    row.Cells("Seeds").Style.ForeColor = Color.Red
                End If
            Catch
            End Try
        Next
    End Sub

    Private Function GetSelectedPlatform() As GamePlatform
        Select Case cmbPlatform.SelectedIndex
            Case 1
                Return GamePlatform.PS5
            Case 2
                Return GamePlatform.PS4
            Case Else
                Return GamePlatform.All
        End Select
    End Function

    Private Function GetSelectedSort() As SearchSortBy
        Select Case cmbSortBy.SelectedIndex
            Case 0
                Return SearchSortBy.Seeds
            Case 1
                Return SearchSortBy.Size
            Case 2
                Return SearchSortBy.UploadDate
            Case 3
                Return SearchSortBy.Name
            Case Else
                Return SearchSortBy.Seeds
        End Select
    End Function

    Private Function GetSelectedProviderName() As String
        If cmbProvider.SelectedIndex <= 0 Then Return "" ' "All Sources"

        Dim selected = cmbProvider.SelectedItem.ToString()
        For Each provider In _searchManager.Providers.Values
            If selected.StartsWith(provider.DisplayName) Then
                Return provider.Name
            End If
        Next
        Return ""
    End Function

    Private Function GetSelectedResult() As GameSearchResult
        If dgvResults.SelectedRows.Count = 0 Then Return Nothing
        ' Get from Tag (stored when adding rows manually)
        Return TryCast(dgvResults.SelectedRows(0).Tag, GameSearchResult)
    End Function

    Private Sub dgvResults_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return
        OpenDetails_Click(sender, e)
    End Sub

    Private Sub dgvResults_MouseClick(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then
            Dim hitTest = dgvResults.HitTest(e.X, e.Y)
            If hitTest.RowIndex >= 0 Then
                dgvResults.ClearSelection()
                dgvResults.Rows(hitTest.RowIndex).Selected = True
                contextMenu.Show(dgvResults, e.Location)
            End If
        End If
    End Sub

    Private Async Sub CopyMagnetLink_Click(sender As Object, e As EventArgs)
        Dim result = GetSelectedResult()
        If result Is Nothing Then Return

        Try
            lblStatus.Text = "Getting magnet link..."

            Dim magnet = result.MagnetLink
            If String.IsNullOrEmpty(magnet) Then
                magnet = Await _searchManager.GetMagnetLinkAsync(result)
            End If

            If Not String.IsNullOrEmpty(magnet) Then
                Clipboard.SetText(magnet)
                lblStatus.Text = "Magnet link copied to clipboard!"
                MessageBox.Show("Magnet link copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show("Could not get magnet link for this torrent.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show($"Failed to get magnet link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenDetails_Click(sender As Object, e As EventArgs)
        Dim result = GetSelectedResult()
        If result Is Nothing OrElse String.IsNullOrEmpty(result.DetailsUrl) Then Return

        Try
            Process.Start(New ProcessStartInfo With {
                .FileName = result.DetailsUrl,
                .UseShellExecute = True
            })
        Catch ex As Exception
            MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub OpenMagnet_Click(sender As Object, e As EventArgs)
        Dim result = GetSelectedResult()
        If result Is Nothing Then Return

        Try
            lblStatus.Text = "Getting magnet link..."

            Dim magnet = result.MagnetLink
            If String.IsNullOrEmpty(magnet) Then
                magnet = Await _searchManager.GetMagnetLinkAsync(result)
            End If

            If Not String.IsNullOrEmpty(magnet) Then
                Process.Start(New ProcessStartInfo With {
                    .FileName = magnet,
                    .UseShellExecute = True
                })
                lblStatus.Text = "Opening with torrent client..."
            Else
                MessageBox.Show("Could not get magnet link for this torrent.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show($"Failed to open magnet: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
