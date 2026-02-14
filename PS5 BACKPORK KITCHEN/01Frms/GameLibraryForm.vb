Imports System.Windows.Forms
Imports System.Drawing

Public Class GameLibraryForm
    Inherits Form

    ' UI Controls
    Private WithEvents dgvGames As DataGridView

    Private WithEvents txtSearch As TextBox
    Private WithEvents btnSearch As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnExportCsv As Button
    Private WithEvents btnExportJson As Button
    Private WithEvents btnDelete As Button
    Private WithEvents btnEdit As Button
    Private WithEvents btnOpenFolder As Button
    Private WithEvents cmbStatusFilter As ComboBox
    Private lblStats As Label
    Private lblTitle As Label
    Private pnlTop As Panel
    Private pnlBottom As Panel
    Private pnlStats As Panel

    ' Data
    Private allGames As List(Of GameLibraryManager.GameEntry)

    Private filteredGames As List(Of GameLibraryManager.GameEntry)

    Public Sub New()
        InitializeComponent()
        'LoadGames()
        'UpdateStatistics()
        'ApplyTheme()
    End Sub

    'Load data when the form is READY
    Private Sub GameLibraryForm_Shown(
    sender As Object,
    e As EventArgs
) Handles Me.Shown

        ApplyTheme()
        LoadGames()

    End Sub

    Private Sub InitializeComponent()
        ' Form settings
        Me.Text = "Game Library"
        Me.Size = New Size(1200, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(900, 500)
        ' Me.Icon = My.Resources.icon ' Comment out if icon not available

        ' Top Panel
        pnlTop = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 120,
            .Padding = New Padding(10)
        }

        ' Title Label
        lblTitle = New Label With {
            .Text = "🎮 Game Library",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(10, 10)
        }
        pnlTop.Controls.Add(lblTitle)

        ' Search controls
        Dim lblSearch As New Label With {
            .Text = "Search:",
            .Location = New Point(10, 50),
            .AutoSize = True
        }
        pnlTop.Controls.Add(lblSearch)

        txtSearch = New TextBox With {
            .Location = New Point(70, 47),
            .Width = 250,
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler txtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        pnlTop.Controls.Add(txtSearch)

        btnSearch = New Button With {
            .Text = "🔍 Search",
            .Location = New Point(330, 45),
            .Width = 100,
            .Height = 30
        }
        pnlTop.Controls.Add(btnSearch)

        ' Status filter
        Dim lblFilter As New Label With {
            .Text = "Filter:",
            .Location = New Point(450, 50),
            .AutoSize = True
        }
        pnlTop.Controls.Add(lblFilter)

        cmbStatusFilter = New ComboBox With {
            .Location = New Point(510, 47),
            .Width = 120,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbStatusFilter.Items.Add("All")
        cmbStatusFilter.Items.Add("Success")
        cmbStatusFilter.Items.Add("Failed")
        cmbStatusFilter.Items.Add("Warning")
        cmbStatusFilter.Items.Add("Pending")
        cmbStatusFilter.SelectedIndex = 0
        pnlTop.Controls.Add(cmbStatusFilter)

        btnRefresh = New Button With {
            .Text = "🔄 Refresh",
            .Location = New Point(650, 45),
            .Width = 100,
            .Height = 30
        }
        pnlTop.Controls.Add(btnRefresh)

        ' Action buttons
        btnOpenFolder = New Button With {
            .Text = "📂 Open Folder",
            .Location = New Point(10, 85),
            .Width = 130,
            .Height = 30
        }
        pnlTop.Controls.Add(btnOpenFolder)

        btnEdit = New Button With {
            .Text = "✏️ Edit",
            .Location = New Point(150, 85),
            .Width = 100,
            .Height = 30
        }
        pnlTop.Controls.Add(btnEdit)

        btnDelete = New Button With {
            .Text = "🗑️ Delete",
            .Location = New Point(260, 85),
            .Width = 100,
            .Height = 30,
            .ForeColor = Color.Red
        }
        pnlTop.Controls.Add(btnDelete)

        btnExportCsv = New Button With {
            .Text = "📄 Export CSV",
            .Location = New Point(380, 85),
            .Width = 120,
            .Height = 30
        }
        pnlTop.Controls.Add(btnExportCsv)

        btnExportJson = New Button With {
            .Text = "📋 Export JSON",
            .Location = New Point(510, 85),
            .Width = 120,
            .Height = 30
        }
        pnlTop.Controls.Add(btnExportJson)

        Me.Controls.Add(pnlTop)

        ' DataGridView
        dgvGames = New DataGridView With {
            .Dock = DockStyle.Fill,
            .AutoGenerateColumns = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .RowHeadersVisible = False,
            .AllowUserToResizeRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .Font = New Font("Segoe UI", 9),
            .AllowUserToOrderColumns = True
        }

        dgvGames.RowTemplate.Height = 50

        AddHandler dgvGames.ColumnHeaderMouseClick, AddressOf DgvGames_ColumnHeaderMouseClick

        ' Cover art column
        dgvGames.Columns.Add(New DataGridViewImageColumn With {
            .Name = "colCover",
            .HeaderText = "",
            .Width = 50,
            .ImageLayout = DataGridViewImageCellLayout.Zoom,
            .DefaultCellStyle = New DataGridViewCellStyle With {
                .NullValue = Nothing
            }
        })

        ' Configure columns
        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colId",
            .HeaderText = "ID",
            .DataPropertyName = "Id",
            .Width = 50,
            .Visible = False
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colStatus",
            .HeaderText = "Status",
            .DataPropertyName = "Status",
            .Width = 80
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colTitle",
            .HeaderText = "Game Title",
            .DataPropertyName = "GameTitle",
            .FillWeight = 200
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colGameId",
            .HeaderText = "Game ID",
            .DataPropertyName = "GameId",
            .Width = 120
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colOrigSdk",
            .HeaderText = "Original SDK",
            .DataPropertyName = "OriginalSdk",
            .Width = 100
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colTargetSdk",
            .HeaderText = "Target SDK",
            .DataPropertyName = "TargetSdk",
            .Width = 100
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colPatchDate",
            .HeaderText = "Patch Date",
            .DataPropertyName = "PatchDate",
            .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd HH:mm"}
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colFileCount",
            .HeaderText = "Files",
            .DataPropertyName = "FileCount",
            .Width = 70
        })

        dgvGames.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "colSize",
            .HeaderText = "Size",
            .DataPropertyName = "TotalSize",
            .Width = 100
        })

        Me.Controls.Add(dgvGames)

        ' Bottom Panel - Statistics
        pnlBottom = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 60,
            .Padding = New Padding(10)
        }

        pnlStats = New Panel With {
            .Dock = DockStyle.Fill,
            .BorderStyle = BorderStyle.FixedSingle
        }

        lblStats = New Label With {
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 9),
            .Padding = New Padding(10)
        }
        pnlStats.Controls.Add(lblStats)
        pnlBottom.Controls.Add(pnlStats)
        Me.Controls.Add(pnlBottom)
    End Sub

    Private Sub LoadGames()
        Try
            allGames = GameLibraryManager.GetAllGames()
            If allGames Is Nothing Then
                allGames = New List(Of GameLibraryManager.GameEntry)()
            End If
            filteredGames = New List(Of GameLibraryManager.GameEntry)(allGames)
            UpdateGrid()
        Catch ex As Exception
            ' Initialize empty lists on error
            allGames = New List(Of GameLibraryManager.GameEntry)()
            filteredGames = New List(Of GameLibraryManager.GameEntry)()
            MessageBox.Show($"Error loading games: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub UpdateGrid()
        Try
            ' added to prevent crashes
            If dgvGames Is Nothing Then Exit Sub
            If filteredGames Is Nothing Then Exit Sub

            dgvGames.SuspendLayout()
            ' added to prevent crashes##
            dgvGames.DataSource = Nothing
            dgvGames.DataSource = filteredGames

            ' Format size column
            For Each row As DataGridViewRow In dgvGames.Rows
                If row.Cells("colSize").Value IsNot Nothing Then
                    Dim size As Long = CLng(row.Cells("colSize").Value)
                    row.Cells("colSize").Value = FormatFileSize(size)
                End If

                ' Color code status
                If row.Cells("colStatus").Value IsNot Nothing Then
                    'Dim status = CType(row.Cells("colStatus").Value, GameLibraryManager.GameStatus)
                    Dim status
                    If TypeOf row.Cells("colStatus").Value Is GameLibraryManager.GameStatus Then
                        status = CType(row.Cells("colStatus").Value, GameLibraryManager.GameStatus)
                        ' color logic
                    End If

                    Select Case status
                        Case GameLibraryManager.GameStatus.Success
                            row.Cells("colStatus").Style.ForeColor = Color.Green
                            row.Cells("colStatus").Value = "✓ Success"
                        Case GameLibraryManager.GameStatus.Failed
                            row.Cells("colStatus").Style.ForeColor = Color.Red
                            row.Cells("colStatus").Value = "✗ Failed"
                        Case GameLibraryManager.GameStatus.Warning
                            row.Cells("colStatus").Style.ForeColor = Color.Orange
                            row.Cells("colStatus").Value = "⚠ Warning"
                        Case GameLibraryManager.GameStatus.Pending
                            row.Cells("colStatus").Style.ForeColor = Color.Gray
                            row.Cells("colStatus").Value = "⏳ Pending"
                    End Select
                End If
            Next

            UpdateStatistics()
            LoadCoverArtAsync()
        Catch ex As Exception
            MessageBox.Show($"Error updating grid: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        dgvGames.ResumeLayout()
    End Sub

    Private Async Sub LoadCoverArtAsync()
        If dgvGames Is Nothing OrElse dgvGames.Rows.Count = 0 Then Return

        ' First pass: load cached images synchronously
        For Each row As DataGridViewRow In dgvGames.Rows
            Dim gameId = TryCast(row.Cells("colGameId").Value, String)
            If Not String.IsNullOrEmpty(gameId) Then
                Dim cached = CoverArtService.GetCachedImage(gameId)
                If cached IsNot Nothing Then
                    row.Cells("colCover").Value = cached
                End If
            End If
        Next

        ' Second pass: fetch missing covers asynchronously
        For i As Integer = 0 To dgvGames.Rows.Count - 1
            If i >= dgvGames.Rows.Count Then Exit For
            Dim row = dgvGames.Rows(i)

            If row.Cells("colCover").Value Is Nothing Then
                Dim gameId = TryCast(row.Cells("colGameId").Value, String)
                If Not String.IsNullOrEmpty(gameId) Then
                    Try
                        Dim img = Await CoverArtService.GetCoverArtAsync(gameId)
                        If img IsNot Nothing AndAlso i < dgvGames.Rows.Count Then
                            dgvGames.Rows(i).Cells("colCover").Value = img
                        End If
                    Catch
                    End Try
                End If
            End If
        Next
    End Sub

    Private Sub UpdateStatistics()
        Try
            Dim stats = GameLibraryManager.GetStatistics()

            Dim totalGames = If(stats.ContainsKey("TotalGames"), stats("TotalGames"), 0)
            Dim totalSize = If(stats.ContainsKey("TotalSize"), CLng(stats("TotalSize")), 0L)
            Dim successCount = If(stats.ContainsKey("StatusSuccess"), stats("StatusSuccess"), 0)
            Dim failedCount = If(stats.ContainsKey("StatusFailed"), stats("StatusFailed"), 0)

            Dim showingCount = If(filteredGames IsNot Nothing, filteredGames.Count, 0)
            lblStats.Text = $"📊 Total Games: {totalGames} | ✓ Success: {successCount} | ✗ Failed: {failedCount} | 💾 Total Size: {FormatFileSize(totalSize)} | 🔍 Showing: {showingCount} games"
        Catch ex As Exception
            lblStats.Text = "Error loading statistics"
        End Try
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
        Try
            If String.IsNullOrWhiteSpace(txtSearch.Text) Then
                filteredGames = New List(Of GameLibraryManager.GameEntry)(allGames)
            Else
                filteredGames = GameLibraryManager.SearchGames(txtSearch.Text)
            End If
            UpdateGrid()
        Catch ex As Exception
            MessageBox.Show($"Error searching: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CmbStatusFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbStatusFilter.SelectedIndexChanged
        Try
            ' Ensure allGames is initialized
            If allGames Is Nothing Then
                allGames = New List(Of GameLibraryManager.GameEntry)()
            End If

            Select Case cmbStatusFilter.SelectedIndex
                Case 0 ' All
                    filteredGames = New List(Of GameLibraryManager.GameEntry)(allGames)
                Case 1 ' Success
                    filteredGames = GameLibraryManager.FilterByStatus(GameLibraryManager.GameStatus.Success)
                Case 2 ' Failed
                    filteredGames = GameLibraryManager.FilterByStatus(GameLibraryManager.GameStatus.Failed)
                Case 3 ' Warning
                    filteredGames = GameLibraryManager.FilterByStatus(GameLibraryManager.GameStatus.Warning)
                Case 4 ' Pending
                    filteredGames = GameLibraryManager.FilterByStatus(GameLibraryManager.GameStatus.Pending)
            End Select

            ' Ensure filteredGames is not null
            If filteredGames Is Nothing Then
                filteredGames = New List(Of GameLibraryManager.GameEntry)()
            End If

            UpdateGrid()
        Catch ex As Exception
            MessageBox.Show($"Error filtering: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadGames()
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As EventArgs) Handles btnOpenFolder.Click
        Try
            If dgvGames.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a game first", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim gameId = CInt(dgvGames.SelectedRows(0).Cells("colId").Value)
            Dim game = GameLibraryManager.GetGameById(gameId)

            If game IsNot Nothing AndAlso IO.Directory.Exists(game.FolderPath) Then
                'Process.Start("explorer.exe", game.FolderPath)
                OpenFolder(game.FolderPath)
                GameLibraryManager.UpdateLastAccessed(gameId)
            Else
                MessageBox.Show("Folder not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        Try
            If dgvGames.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a game first", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim gameId = CInt(dgvGames.SelectedRows(0).Cells("colId").Value)
            Dim game = GameLibraryManager.GetGameById(gameId)

            If game IsNot Nothing Then
                Using editForm As New GameEditForm(game)
                    If editForm.ShowDialog() = DialogResult.OK Then
                        LoadGames()
                    End If
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Error editing game: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        Try
            If dgvGames.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a game first", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim result = MessageBox.Show(
                "Are you sure you want to delete this game from the library?" & vbCrLf & vbCrLf &
                "This will NOT delete the game files, only the library entry.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            )

            If result = DialogResult.Yes Then
                Dim gameId = CInt(dgvGames.SelectedRows(0).Cells("colId").Value)
                GameLibraryManager.DeleteGame(gameId)
                LoadGames()
                MessageBox.Show("Game deleted from library", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error deleting game: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnExportCsv_Click(sender As Object, e As EventArgs) Handles btnExportCsv.Click
        Try
            Using sfd As New SaveFileDialog With {
                .Filter = "CSV Files (*.csv)|*.csv",
                .FileName = $"GameLibrary_{DateTime.Now:yyyyMMdd}.csv"
            }
                If sfd.ShowDialog() = DialogResult.OK Then
                    Dim folderPath As String = IO.Path.GetDirectoryName(sfd.FileName)

                    GameLibraryManager.ExportToCsv(sfd.FileName)
                    MessageBox.Show("Library exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    'Process.Start("explorer.exe", $"/select,""{sfd.FileName}""")
                    OpenFolder(folderPath)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error exporting CSV: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnExportJson_Click(sender As Object, e As EventArgs) Handles btnExportJson.Click
        Try
            Using sfd As New SaveFileDialog With {
                .Filter = "JSON Files (*.json)|*.json",
                .FileName = $"GameLibrary_{DateTime.Now:yyyyMMdd}.json"
            }
                If sfd.ShowDialog() = DialogResult.OK Then
                    Dim folderPath As String = IO.Path.GetDirectoryName(sfd.FileName)
                    GameLibraryManager.ExportToJson(sfd.FileName)
                    MessageBox.Show("Library exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    'Process.Start("explorer.exe", $"/select,""{sfd.FileName}""")
                    OpenFolder(folderPath)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error exporting JSON: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DgvGames_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvGames.CellDoubleClick
        If e.RowIndex >= 0 Then
            BtnOpenFolder_Click(sender, e)
        End If
    End Sub

    ''' <summary>
    ''' Real-time search as user types.
    ''' </summary>
    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        Try
            If String.IsNullOrWhiteSpace(txtSearch.Text) Then
                ' Apply current filter
                CmbStatusFilter_SelectedIndexChanged(cmbStatusFilter, EventArgs.Empty)
            Else
                ' Search in current filter
                Dim searchTerm = txtSearch.Text.ToLower()
                If allGames Is Nothing Then allGames = New List(Of GameLibraryManager.GameEntry)()

                filteredGames = allGames.Where(Function(g)
                                                   Return g.GameTitle.ToLower().Contains(searchTerm) OrElse
                                                          g.GameId.ToLower().Contains(searchTerm) OrElse
                                                          g.FolderPath.ToLower().Contains(searchTerm)
                                               End Function).ToList()

                ' Apply status filter on search results
                If cmbStatusFilter.SelectedIndex > 0 Then
                    Dim targetStatus = CType(cmbStatusFilter.SelectedIndex - 1, GameLibraryManager.GameStatus)
                    filteredGames = filteredGames.Where(Function(g) g.Status = targetStatus).ToList()
                End If

                UpdateGrid()
            End If
        Catch ex As Exception
            Debug.WriteLine($"Search error: {ex.Message}")
        End Try
    End Sub

    Private Sub TxtSearch_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSearch.KeyPress
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            BtnSearch_Click(sender, e)
        End If
    End Sub

    ''' <summary>
    ''' Sort DataGridView by clicking column headers.
    ''' </summary>
    Private sortColumn As String = ""

    Private sortAscending As Boolean = True

    Private Sub DgvGames_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        Try
            If e.ColumnIndex < 0 Then Return

            Dim column = dgvGames.Columns(e.ColumnIndex)
            If column.Name = "colId" Then Return ' Don't sort by hidden ID

            ' Toggle sort direction if same column
            If sortColumn = column.DataPropertyName Then
                sortAscending = Not sortAscending
            Else
                sortColumn = column.DataPropertyName
                sortAscending = True
            End If

            ' Sort filtered games
            If filteredGames IsNot Nothing AndAlso filteredGames.Count > 0 Then
                Select Case sortColumn
                    Case "Status"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.Status).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.Status).ToList())
                    Case "GameTitle"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.GameTitle).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.GameTitle).ToList())
                    Case "GameId"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.GameId).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.GameId).ToList())
                    Case "OriginalSdk"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.OriginalSdk).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.OriginalSdk).ToList())
                    Case "TargetSdk"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.TargetSdk).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.TargetSdk).ToList())
                    Case "PatchDate"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.PatchDate).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.PatchDate).ToList())
                    Case "FileCount"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.FileCount).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.FileCount).ToList())
                    Case "TotalSize"
                        filteredGames = If(sortAscending,
                            filteredGames.OrderBy(Function(g) g.TotalSize).ToList(),
                            filteredGames.OrderByDescending(Function(g) g.TotalSize).ToList())
                End Select

                UpdateGrid()

                ' Update column header to show sort indicator
                For Each col As DataGridViewColumn In dgvGames.Columns
                    col.HeaderCell.SortGlyphDirection = SortOrder.None
                Next
                column.HeaderCell.SortGlyphDirection = If(sortAscending, SortOrder.Ascending, SortOrder.Descending)
            End If
        Catch ex As Exception
            Debug.WriteLine($"Sort error: {ex.Message}")
        End Try
    End Sub

    Private Function FormatFileSize(bytes As Long) As String
        Dim suffixes() As String = {"B", "KB", "MB", "GB", "TB"}
        Dim counter As Integer = 0
        Dim number As Decimal = bytes

        While number >= 1024 AndAlso counter < suffixes.Length - 1
            number /= 1024
            counter += 1
        End While

        Return $"{number:N2} {suffixes(counter)}"
    End Function

    Private Sub ApplyTheme()
        Try
            ' Apply current theme from ThemeManager
            ThemeManager.ApplyThemeToForm(Me)

            ' Additional styling for DataGridView
            dgvGames.EnableHeadersVisualStyles = False
            dgvGames.DefaultCellStyle.SelectionBackColor = Color.SteelBlue
            dgvGames.DefaultCellStyle.SelectionForeColor = Color.White
        Catch ex As Exception
            ' Theme application failed - not critical
        End Try
    End Sub

End Class

' Simple edit form for game details
Public Class GameEditForm
    Inherits Form

    Private game As GameLibraryManager.GameEntry
    Private WithEvents txtTitle As TextBox
    Private WithEvents txtNotes As TextBox
    Private WithEvents btnSave As Button
    Private WithEvents btnCancel As Button

    Public Sub New(gameEntry As GameLibraryManager.GameEntry)
        Me.game = gameEntry
        InitializeComponent()
        LoadData()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Edit Game Details"
        Me.Size = New Size(500, 300)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Dim lblTitle As New Label With {
            .Text = "Game Title:",
            .Location = New Point(20, 20),
            .AutoSize = True
        }
        Me.Controls.Add(lblTitle)

        txtTitle = New TextBox With {
            .Location = New Point(20, 45),
            .Width = 440,
            .Font = New Font("Segoe UI", 10)
        }
        Me.Controls.Add(txtTitle)

        Dim lblNotes As New Label With {
            .Text = "Notes:",
            .Location = New Point(20, 80),
            .AutoSize = True
        }
        Me.Controls.Add(lblNotes)

        txtNotes = New TextBox With {
            .Location = New Point(20, 105),
            .Width = 440,
            .Height = 100,
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Font = New Font("Segoe UI", 9)
        }
        Me.Controls.Add(txtNotes)

        btnSave = New Button With {
            .Text = "💾 Save",
            .Location = New Point(280, 220),
            .Width = 90,
            .Height = 35
        }
        Me.Controls.Add(btnSave)

        btnCancel = New Button With {
            .Text = "✖ Cancel",
            .Location = New Point(380, 220),
            .Width = 90,
            .Height = 35
        }
        Me.Controls.Add(btnCancel)
    End Sub

    Private Sub LoadData()
        txtTitle.Text = game.GameTitle
        txtNotes.Text = game.Notes
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            game.GameTitle = txtTitle.Text
            game.Notes = txtNotes.Text
            GameLibraryManager.UpdateGame(game)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

End Class