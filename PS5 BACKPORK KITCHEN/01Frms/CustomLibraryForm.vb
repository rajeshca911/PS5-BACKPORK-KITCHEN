Imports System.Windows.Forms
Imports System.IO
Imports System.Linq

''' <summary>
''' Form for managing custom PS5/PS4 libraries (.prx, .sprx)
''' Allows adding, removing, enabling/disabling, and organizing custom libraries
''' </summary>
Public Class CustomLibraryForm
    Inherits Form

    ' UI Controls
    Private grpFilter As GroupBox

    Private lblSdkVersion As Label
    Private cmbSdkVersion As ComboBox
    Private chkShowDisabled As CheckBox
    Private chkShowBuiltIn As CheckBox
    Private btnRefresh As Button

    Private lvLibraries As ListView
    Private contextMenu As ContextMenuStrip

    Private grpActions As GroupBox
    Private btnAdd As Button
    Private btnRemove As Button
    Private btnToggleEnabled As Button
    Private btnEditDescription As Button

    Private grpImportExport As GroupBox
    Private btnImport As Button
    Private btnExport As Button

    Private lblStats As Label
    Private btnClose As Button

    ' Current filter settings
    Private currentSdkFilter As Integer = 0 ' 0 = All

    Private showDisabled As Boolean = False
    Private showBuiltIn As Boolean = True

    Public Sub New()
        Me.AutoScaleMode = AutoScaleMode.Dpi
        Me.AutoScaleDimensions = New SizeF(96, 96)
        InitializeComponent()
        LoadLibraries()
    End Sub

    'Private Sub InitializeComponent()
    '    Me.Text = "Custom Library Manager"
    '    Me.Size = New Size(1000, 700)
    '    Me.FormBorderStyle = FormBorderStyle.Sizable
    '    Me.MinimumSize = New Size(1000, 700)
    '    Me.StartPosition = FormStartPosition.CenterParent
    '    Me.Icon = Form1.Icon

    '    Dim yPos = 15

    '    ' Title label
    '    Dim lblTitle As New Label With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(950, 30),
    '        .Text = "📚 Custom Library Manager - Manage PS5/PS4 Libraries",
    '        .Font = New Font("Segoe UI", 14, FontStyle.Bold),
    '        .ForeColor = Color.DarkBlue
    '    }
    '    Me.Controls.Add(lblTitle)
    '    yPos += 40

    '    ' Filter Group
    '    grpFilter = New GroupBox With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(955, 70),
    '        .Text = "Filter"
    '    }

    '    lblSdkVersion = New Label With {
    '        .Location = New Point(15, 30),
    '        .Size = New Size(80, 20),
    '        .Text = "SDK Version:"
    '    }

    '    cmbSdkVersion = New ComboBox With {
    '        .Location = New Point(100, 27),
    '        .Size = New Size(150, 23),
    '        .DropDownStyle = ComboBoxStyle.DropDownList
    '    }
    '    cmbSdkVersion.Items.AddRange(New String() {"All SDKs", "SDK 3", "SDK 4", "SDK 5", "SDK 6", "SDK 7", "SDK 8", "SDK 9"})
    '    cmbSdkVersion.SelectedIndex = 0
    '    AddHandler cmbSdkVersion.SelectedIndexChanged, AddressOf FilterChanged

    '    chkShowDisabled = New CheckBox With {
    '        .Location = New Point(270, 30),
    '        .Size = New Size(140, 20),
    '        .Text = "Show Disabled",
    '        .Checked = False
    '    }
    '    AddHandler chkShowDisabled.CheckedChanged, AddressOf FilterChanged

    '    chkShowBuiltIn = New CheckBox With {
    '        .Location = New Point(430, 30),
    '        .Size = New Size(140, 20),
    '        .Text = "Show Built-in",
    '        .Checked = True
    '    }
    '    AddHandler chkShowBuiltIn.CheckedChanged, AddressOf FilterChanged

    '    btnRefresh = New Button With {
    '        .Location = New Point(850, 25),
    '        .Size = New Size(90, 28),
    '        .Text = "🔄 Refresh",
    '        .BackColor = Color.LightSteelBlue,
    '        .FlatStyle = FlatStyle.Flat
    '    }
    '    AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click

    '    grpFilter.Controls.AddRange({lblSdkVersion, cmbSdkVersion, chkShowDisabled, chkShowBuiltIn, btnRefresh})
    '    Me.Controls.Add(grpFilter)
    '    yPos += 80

    '    ' Libraries ListView
    '    lvLibraries = New ListView With {
    '        .Location = New Point(15, yPos),
    '        .Size = New Size(680, 450),
    '        .View = View.Details,
    '        .FullRowSelect = True,
    '        .GridLines = True,
    '        .CheckBoxes = True,
    '        .MultiSelect = False
    '    }

    '    ' Add columns
    '    lvLibraries.Columns.Add("Enabled", 60)
    '    lvLibraries.Columns.Add("File Name", 180)
    '    lvLibraries.Columns.Add("SDK", 50)
    '    lvLibraries.Columns.Add("Type", 60)
    '    lvLibraries.Columns.Add("Size", 80)
    '    lvLibraries.Columns.Add("Category", 80)
    '    lvLibraries.Columns.Add("Description", 150)

    '    AddHandler lvLibraries.ItemChecked, AddressOf LvLibraries_ItemChecked
    '    AddHandler lvLibraries.DoubleClick, AddressOf LvLibraries_DoubleClick

    '    ' Context menu for right-click
    '    CreateContextMenu()
    '    lvLibraries.ContextMenuStrip = contextMenu

    '    Me.Controls.Add(lvLibraries)

    '    ' Actions Group (on the right)
    '    grpActions = New GroupBox With {
    '        .Location = New Point(705, yPos),
    '        .Size = New Size(265, 200),
    '        .Text = "Actions"
    '    }

    '    btnAdd = New Button With {
    '        .Location = New Point(15, 30),
    '        .Size = New Size(235, 32),
    '        .Text = "➕ Add Library",
    '        .BackColor = Color.FromArgb(144, 238, 144),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 9, FontStyle.Bold)
    '    }
    '    AddHandler btnAdd.Click, AddressOf BtnAdd_Click

    '    btnRemove = New Button With {
    '        .Location = New Point(15, 70),
    '        .Size = New Size(235, 32),
    '        .Text = "➖ Remove Library",
    '        .BackColor = Color.FromArgb(240, 128, 128),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '        .Enabled = False
    '    }
    '    AddHandler btnRemove.Click, AddressOf BtnRemove_Click

    '    btnToggleEnabled = New Button With {
    '        .Location = New Point(15, 110),
    '        .Size = New Size(235, 32),
    '        .Text = "✓ Toggle Enabled",
    '        .BackColor = Color.FromArgb(135, 206, 250),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '        .Enabled = False
    '    }
    '    AddHandler btnToggleEnabled.Click, AddressOf BtnToggleEnabled_Click

    '    btnEditDescription = New Button With {
    '        .Location = New Point(15, 150),
    '        .Size = New Size(235, 32),
    '        .Text = "📝 Edit Description",
    '        .BackColor = Color.FromArgb(255, 215, 0),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 9, FontStyle.Bold),
    '        .Enabled = False
    '    }
    '    AddHandler btnEditDescription.Click, AddressOf BtnEditDescription_Click

    '    grpActions.Controls.AddRange({btnAdd, btnRemove, btnToggleEnabled, btnEditDescription})
    '    Me.Controls.Add(grpActions)

    '    ' Import/Export Group
    '    grpImportExport = New GroupBox With {
    '        .Location = New Point(705, yPos + 210),
    '        .Size = New Size(265, 100),
    '        .Text = "Import / Export"
    '    }

    '    btnImport = New Button With {
    '        .Location = New Point(15, 30),
    '        .Size = New Size(235, 28),
    '        .Text = "📥 Import Config",
    '        .BackColor = Color.LightGray,
    '        .FlatStyle = FlatStyle.Flat
    '    }
    '    AddHandler btnImport.Click, AddressOf BtnImport_Click

    '    btnExport = New Button With {
    '        .Location = New Point(15, 65),
    '        .Size = New Size(235, 28),
    '        .Text = "📤 Export Config",
    '        .BackColor = Color.LightGray,
    '        .FlatStyle = FlatStyle.Flat
    '    }
    '    AddHandler btnExport.Click, AddressOf BtnExport_Click

    '    grpImportExport.Controls.AddRange({btnImport, btnExport})
    '    Me.Controls.Add(grpImportExport)

    '    ' Stats Label
    '    lblStats = New Label With {
    '        .Location = New Point(705, yPos + 320),
    '        .Size = New Size(265, 80),
    '        .Text = "Statistics:" & vbCrLf & "Loading...",
    '        .Font = New Font("Segoe UI", 9),
    '        .ForeColor = Color.DarkGray,
    '        .BorderStyle = BorderStyle.FixedSingle,
    '        .Padding = New Padding(5)
    '    }
    '    Me.Controls.Add(lblStats)

    '    yPos += 460

    '    ' Close button
    '    btnClose = New Button With {
    '        .Location = New Point(870, yPos),
    '        .Size = New Size(100, 35),
    '        .Text = "Close",
    '        .BackColor = Color.FromArgb(240, 128, 128),
    '        .FlatStyle = FlatStyle.Flat,
    '        .Font = New Font("Segoe UI", 10, FontStyle.Bold)
    '    }
    '    AddHandler btnClose.Click, Sub() Me.Close()
    '    Me.Controls.Add(btnClose)

    '    ' Enable selection change event
    '    AddHandler lvLibraries.SelectedIndexChanged, AddressOf LvLibraries_SelectedIndexChanged
    'End Sub
    Private Sub InitializeComponent()

        Me.Text = "Custom Library Manager"
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Icon = Form1.Icon

        ' ===== ROOT LAYOUT =====
        Dim root As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 4,
        .Padding = New Padding(12)
    }

        root.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' title
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' filter
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' main
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' bottom

        Me.Controls.Add(root)

        ' ===== TITLE =====
        Dim lblTitle As New Label With {
        .Text = "📚 Custom Library Manager - Manage PS5/PS4 Libraries",
        .Font = New Font("Segoe UI", 14, FontStyle.Bold),
        .ForeColor = Color.DarkBlue,
        .Dock = DockStyle.Fill,
        .AutoSize = True,
        .Padding = New Padding(0, 0, 0, 8)
    }

        root.Controls.Add(lblTitle, 0, 0)

        ' ===== FILTER GROUP =====
        grpFilter = BuildFilterGroup()
        root.Controls.Add(grpFilter, 0, 1)

        ' ===== MAIN AREA (LEFT LIST + RIGHT PANEL) =====
        Dim mainSplit As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 2
    }

        mainSplit.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))
        mainSplit.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))

        root.Controls.Add(mainSplit, 0, 2)

        ' ===== LIST VIEW =====
        lvLibraries = New ListView With {
        .Dock = DockStyle.Fill,
        .View = View.Details,
        .FullRowSelect = True,
        .GridLines = True,
        .CheckBoxes = True,
        .MultiSelect = False
    }

        lvLibraries.Columns.Add("Enabled", 70)
        lvLibraries.Columns.Add("File Name", 200)
        lvLibraries.Columns.Add("SDK", 60)
        lvLibraries.Columns.Add("Type", 70)
        lvLibraries.Columns.Add("Size", 90)
        lvLibraries.Columns.Add("Category", 100)
        lvLibraries.Columns.Add("Description", 220)

        AddHandler lvLibraries.ItemChecked, AddressOf LvLibraries_ItemChecked
        AddHandler lvLibraries.DoubleClick, AddressOf LvLibraries_DoubleClick
        AddHandler lvLibraries.SelectedIndexChanged, AddressOf LvLibraries_SelectedIndexChanged

        CreateContextMenu()
        lvLibraries.ContextMenuStrip = contextMenu

        mainSplit.Controls.Add(lvLibraries, 0, 0)

        ' ===== RIGHT SIDE STACK =====
        Dim rightPanel As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.TopDown,
        .WrapContents = False,
        .AutoScroll = True
    }

        mainSplit.Controls.Add(rightPanel, 1, 0)

        grpActions = BuildActionsGroup()
        grpImportExport = BuildImportExportGroup()

        lblStats = New Label With {
        .AutoSize = False,
        .Width = 260,
        .Height = 100,
        .BorderStyle = BorderStyle.FixedSingle,
        .Padding = New Padding(6),
        .ForeColor = Color.DarkGray,
        .Text = "Statistics: Loading..."
    }

        rightPanel.Controls.Add(grpActions)
        rightPanel.Controls.Add(grpImportExport)
        rightPanel.Controls.Add(lblStats)

        ' ===== BOTTOM BAR =====
        Dim bottomPanel As New Panel With {
        .Dock = DockStyle.Fill,
        .Height = 45
    }

        btnClose = New Button With {
        .Text = "Close",
        .Width = 120,
        .Height = 32,
        .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
        .BackColor = Color.FromArgb(240, 128, 128),
        .FlatStyle = FlatStyle.Flat
    }

        bottomPanel.Controls.Add(btnClose)
        btnClose.Location = New Point(bottomPanel.Width - btnClose.Width - 5, 5)

        AddHandler bottomPanel.Resize,
        Sub() btnClose.Left = bottomPanel.Width - btnClose.Width - 5

        AddHandler btnClose.Click, Sub() Me.Close()

        root.Controls.Add(bottomPanel, 0, 3)

    End Sub

    Private Function BuildImportExportGroup() As GroupBox

        grpImportExport = New GroupBox With {
        .Text = "Import / Export",
        .Width = 260,
        .Height = 110
    }

        Dim flow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.TopDown
    }

        btnImport = New Button With {.Text = "📥 Import Config", .Width = 220}
        btnExport = New Button With {.Text = "📤 Export Config", .Width = 220}

        AddHandler btnImport.Click, AddressOf BtnImport_Click
        AddHandler btnExport.Click, AddressOf BtnExport_Click

        flow.Controls.AddRange({btnImport, btnExport})
        grpImportExport.Controls.Add(flow)

        Return grpImportExport

    End Function

    Private Function BuildFilterGroup() As GroupBox

        grpFilter = New GroupBox With {
        .Text = "Filter",
        .Dock = DockStyle.Fill,
        .AutoSize = True
    }

        Dim flow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .AutoSize = True
    }

        lblSdkVersion = New Label With {.Text = "SDK Version:", .AutoSize = True}

        cmbSdkVersion = New ComboBox With {
        .Width = 150,
        .DropDownStyle = ComboBoxStyle.DropDownList
    }

        cmbSdkVersion.Items.AddRange(
        {"All SDKs", "SDK 3", "SDK 4", "SDK 5", "SDK 6", "SDK 7", "SDK 8", "SDK 9"})
        cmbSdkVersion.SelectedIndex = 0

        chkShowDisabled = New CheckBox With {.Text = "Show Disabled", .AutoSize = True}
        chkShowBuiltIn = New CheckBox With {.Text = "Show Built-in", .Checked = True, .AutoSize = True}

        btnRefresh = New Button With {.Text = "🔄 Refresh", .Width = 110}

        AddHandler cmbSdkVersion.SelectedIndexChanged, AddressOf FilterChanged
        AddHandler chkShowDisabled.CheckedChanged, AddressOf FilterChanged
        AddHandler chkShowBuiltIn.CheckedChanged, AddressOf FilterChanged
        AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click

        flow.Controls.AddRange({
        lblSdkVersion, cmbSdkVersion,
        chkShowDisabled, chkShowBuiltIn,
        btnRefresh})

        grpFilter.Controls.Add(flow)
        Return grpFilter

    End Function

    Private Function BuildActionsGroup() As GroupBox

        grpActions = New GroupBox With {
        .Text = "Actions",
        .Width = 260,
        .Height = 210
    }

        Dim flow As New FlowLayoutPanel With {
        .Dock = DockStyle.Fill,
        .FlowDirection = FlowDirection.TopDown
    }

        btnAdd = New Button With {.Text = "➕ Add Library", .Width = 220, .Height = 34}
        btnRemove = New Button With {.Text = "➖ Remove Library", .Width = 220, .Height = 34}
        btnToggleEnabled = New Button With {.Text = "✓ Toggle Enabled", .Width = 220, .Height = 34}
        btnEditDescription = New Button With {.Text = "📝 Edit Description", .Width = 220, .Height = 34}

        AddHandler btnAdd.Click, AddressOf BtnAdd_Click
        AddHandler btnRemove.Click, AddressOf BtnRemove_Click
        AddHandler btnToggleEnabled.Click, AddressOf BtnToggleEnabled_Click
        AddHandler btnEditDescription.Click, AddressOf BtnEditDescription_Click

        flow.Controls.AddRange({btnAdd, btnRemove, btnToggleEnabled, btnEditDescription})
        grpActions.Controls.Add(flow)

        Return grpActions

    End Function

    ''' <summary>
    ''' Create context menu for right-click
    ''' </summary>
    Private Sub CreateContextMenu()
        contextMenu = New ContextMenuStrip()

        Dim menuAdd As New ToolStripMenuItem("Add Library")
        AddHandler menuAdd.Click, AddressOf BtnAdd_Click
        contextMenu.Items.Add(menuAdd)

        Dim menuRemove As New ToolStripMenuItem("Remove Library")
        AddHandler menuRemove.Click, AddressOf BtnRemove_Click
        contextMenu.Items.Add(menuRemove)

        contextMenu.Items.Add(New ToolStripSeparator())

        Dim menuToggle As New ToolStripMenuItem("Toggle Enabled")
        AddHandler menuToggle.Click, AddressOf BtnToggleEnabled_Click
        contextMenu.Items.Add(menuToggle)

        Dim menuEdit As New ToolStripMenuItem("Edit Description")
        AddHandler menuEdit.Click, AddressOf BtnEditDescription_Click
        contextMenu.Items.Add(menuEdit)

        contextMenu.Items.Add(New ToolStripSeparator())

        Dim menuRefresh As New ToolStripMenuItem("Refresh")
        AddHandler menuRefresh.Click, AddressOf BtnRefresh_Click
        contextMenu.Items.Add(menuRefresh)
    End Sub

    ''' <summary>
    ''' Load libraries into ListView
    ''' </summary>
    Private Sub LoadLibraries()
        Try
            lvLibraries.Items.Clear()

            ' Get all libraries
            Dim allLibs = LibraryManager.LoadAllLibraries()

            ' Apply filters
            Dim filteredLibs = allLibs.Where(Function(library)
                                                 ' SDK filter
                                                 Dim sdkMatch = (currentSdkFilter = 0) OrElse (library.SdkVersion = currentSdkFilter)

                                                 ' Disabled filter
                                                 Dim disabledMatch = showDisabled OrElse library.IsEnabled

                                                 ' Built-in filter
                                                 Dim builtInMatch = showBuiltIn OrElse Not library.IsBuiltIn

                                                 Return sdkMatch AndAlso disabledMatch AndAlso builtInMatch
                                             End Function).ToList()

            ' Sort by SDK, then by filename
            filteredLibs = filteredLibs.OrderBy(Function(library) library.SdkVersion).ThenBy(Function(library) library.FileName).ToList()

            ' Add to ListView
            For Each library In filteredLibs
                Dim item As New ListViewItem()
                item.Tag = library
                item.Checked = library.IsEnabled

                ' Columns: Enabled, File Name, SDK, Type, Size, Category, Description
                item.SubItems.Add(library.FileName)
                item.SubItems.Add(library.SdkVersion.ToString())
                item.SubItems.Add(library.LibraryType)
                item.SubItems.Add(FormatFileSize(library.FileSize))
                item.SubItems.Add(library.Category)
                item.SubItems.Add(library.Description)

                ' Color coding
                If Not library.IsEnabled Then
                    item.ForeColor = Color.Gray
                ElseIf library.IsBuiltIn Then
                    item.ForeColor = Color.DarkBlue
                Else
                    item.ForeColor = Color.Black
                End If

                lvLibraries.Items.Add(item)
            Next

            ' Update stats
            UpdateStatistics(allLibs, filteredLibs.Count)
        Catch ex As Exception
            MessageBox.Show("Error loading libraries: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Update statistics label
    ''' </summary>
    Private Sub UpdateStatistics(allLibs As List(Of LibraryManager.CustomLibrary), filteredCount As Integer)
        Dim totalCount = allLibs.Count
        Dim enabledCount = allLibs.Where(Function(library) library.IsEnabled).Count()
        Dim customCount = allLibs.Where(Function(library) Not library.IsBuiltIn).Count()
        Dim builtInCount = allLibs.Where(Function(library) library.IsBuiltIn).Count()

        lblStats.Text = "Statistics:" & vbCrLf &
                       "Total: " & totalCount.ToString() & vbCrLf &
                       "Enabled: " & enabledCount.ToString() & vbCrLf &
                       "Custom: " & customCount.ToString() & vbCrLf &
                       "Built-in: " & builtInCount.ToString() & vbCrLf &
                       "Filtered: " & filteredCount.ToString()
    End Sub

    ''' <summary>
    ''' Add new library
    ''' </summary>
    Private Sub BtnAdd_Click(sender As Object, e As EventArgs)
        Try
            ' Ask for SDK version
            Using sdkForm As New Form()
                sdkForm.Text = "Select SDK Version"
                sdkForm.Size = New Size(350, 200)
                sdkForm.StartPosition = FormStartPosition.CenterParent
                sdkForm.FormBorderStyle = FormBorderStyle.FixedDialog
                sdkForm.MaximizeBox = False
                sdkForm.MinimizeBox = False

                Dim lblPrompt As New Label With {
                    .Location = New Point(20, 20),
                    .Size = New Size(300, 40),
                    .Text = "Select the SDK version for the library you want to add:"
                }

                Dim cmbSdk As New ComboBox With {
                    .Location = New Point(20, 70),
                    .Size = New Size(300, 23),
                    .DropDownStyle = ComboBoxStyle.DropDownList
                }
                For i As Integer = 3 To 9
                    cmbSdk.Items.Add("SDK " & i.ToString())
                Next
                cmbSdk.SelectedIndex = 4 ' Default SDK 7

                Dim btnOk As New Button With {
                    .Location = New Point(130, 120),
                    .Size = New Size(80, 30),
                    .Text = "OK",
                    .DialogResult = DialogResult.OK
                }

                Dim btnCancel As New Button With {
                    .Location = New Point(220, 120),
                    .Size = New Size(80, 30),
                    .Text = "Cancel",
                    .DialogResult = DialogResult.Cancel
                }

                sdkForm.Controls.AddRange({lblPrompt, cmbSdk, btnOk, btnCancel})
                sdkForm.AcceptButton = btnOk
                sdkForm.CancelButton = btnCancel

                If sdkForm.ShowDialog() = DialogResult.OK Then
                    Dim selectedSdk = cmbSdk.SelectedIndex + 3 ' SDK 3-9

                    ' Browse for library file
                    Using ofd As New OpenFileDialog()
                        ofd.Title = "Select Library File"
                        ofd.Filter = "Library Files|*.prx;*.sprx|PRX Files|*.prx|SPRX Files|*.sprx|All Files|*.*"
                        ofd.Multiselect = False

                        If ofd.ShowDialog() = DialogResult.OK Then
                            ' Ask for description
                            Dim description = InputBox("Enter a description for this library (optional):", "Description", "")

                            ' Add library
                            Dim newLib = LibraryManager.AddLibrary(ofd.FileName, selectedSdk, description)

                            MessageBox.Show("Library added successfully!" & vbCrLf & vbCrLf &
                                          "Name: " & newLib.FileName & vbCrLf &
                                          "SDK: " & newLib.SdkVersion.ToString() & vbCrLf &
                                          "Size: " & FormatFileSize(newLib.FileSize),
                                          "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                            ' Refresh list
                            LoadLibraries()
                        End If
                    End Using
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Error adding library: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Remove selected library
    ''' </summary>
    Private Sub BtnRemove_Click(sender As Object, e As EventArgs)
        Try
            If lvLibraries.SelectedItems.Count = 0 Then
                MessageBox.Show("Please select a library to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim selectedLib = TryCast(lvLibraries.SelectedItems(0).Tag, LibraryManager.CustomLibrary)
            If selectedLib Is Nothing Then Return

            ' Check if built-in
            If selectedLib.IsBuiltIn Then
                MessageBox.Show("Built-in libraries cannot be removed.", "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Confirm removal
            Dim result = MessageBox.Show("Are you sure you want to remove this library?" & vbCrLf & vbCrLf &
                                        "Name: " & selectedLib.FileName & vbCrLf &
                                        "SDK: " & selectedLib.SdkVersion.ToString() & vbCrLf & vbCrLf &
                                        "This will delete the library file permanently.",
                                        "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                If LibraryManager.RemoveLibrary(selectedLib.Id) Then
                    MessageBox.Show("Library removed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadLibraries()
                Else
                    MessageBox.Show("Failed to remove library.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Error removing library: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Toggle enabled state
    ''' </summary>
    Private Sub BtnToggleEnabled_Click(sender As Object, e As EventArgs)
        Try
            If lvLibraries.SelectedItems.Count = 0 Then
                MessageBox.Show("Please select a library to toggle.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim selectedLib = TryCast(lvLibraries.SelectedItems(0).Tag, LibraryManager.CustomLibrary)
            If selectedLib Is Nothing Then Return

            Dim newState = Not selectedLib.IsEnabled
            If LibraryManager.ToggleLibraryEnabled(selectedLib.Id, newState) Then
                LoadLibraries()
            End If
        Catch ex As Exception
            MessageBox.Show("Error toggling library: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Edit library description
    ''' </summary>
    Private Sub BtnEditDescription_Click(sender As Object, e As EventArgs)
        Try
            If lvLibraries.SelectedItems.Count = 0 Then
                MessageBox.Show("Please select a library to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim selectedLib = TryCast(lvLibraries.SelectedItems(0).Tag, LibraryManager.CustomLibrary)
            If selectedLib Is Nothing Then Return

            Dim newDescription = InputBox("Edit description for: " & selectedLib.FileName, "Edit Description", selectedLib.Description)

            If newDescription <> selectedLib.Description Then
                If LibraryManager.UpdateLibraryDescription(selectedLib.Id, newDescription) Then
                    MessageBox.Show("Description updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadLibraries()
                Else
                    MessageBox.Show("Failed to update description.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Error editing description: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Import configuration
    ''' </summary>
    Private Sub BtnImport_Click(sender As Object, e As EventArgs)
        Try
            Using ofd As New OpenFileDialog()
                ofd.Title = "Import Library Configuration"
                ofd.Filter = "JSON Files|*.json|All Files|*.*"

                If ofd.ShowDialog() = DialogResult.OK Then
                    Dim result = MessageBox.Show("Do you want to merge with existing libraries?" & vbCrLf & vbCrLf &
                                                "Yes = Merge (add new libraries)" & vbCrLf &
                                                "No = Replace (clear existing custom libraries)",
                                                "Import Mode", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)

                    If result = DialogResult.Cancel Then Return

                    Dim mergeMode = (result = DialogResult.Yes)

                    If LibraryManager.ImportConfiguration(ofd.FileName, mergeMode) Then
                        MessageBox.Show("Configuration imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        LoadLibraries()
                    Else
                        MessageBox.Show("Failed to import configuration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Error importing configuration: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Export configuration
    ''' </summary>
    Private Sub BtnExport_Click(sender As Object, e As EventArgs)
        Try
            Using sfd As New SaveFileDialog()
                sfd.Title = "Export Library Configuration"
                sfd.Filter = "JSON Files|*.json|All Files|*.*"
                sfd.DefaultExt = "json"
                sfd.FileName = "library_config_" & DateTime.Now.ToString("yyyyMMdd") & ".json"

                If sfd.ShowDialog() = DialogResult.OK Then
                    If LibraryManager.ExportConfiguration(sfd.FileName) Then
                        MessageBox.Show("Configuration exported successfully!" & vbCrLf & vbCrLf &
                                      "File: " & Path.GetFileName(sfd.FileName),
                                      "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Else
                        MessageBox.Show("Failed to export configuration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Error exporting configuration: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Refresh button click
    ''' </summary>
    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
        LoadLibraries()
    End Sub

    ''' <summary>
    ''' Filter changed event
    ''' </summary>
    Private Sub FilterChanged(sender As Object, e As EventArgs)
        ' Update filter settings
        currentSdkFilter = cmbSdkVersion.SelectedIndex ' 0 = All, 1-7 = SDK 3-9
        If currentSdkFilter > 0 Then
            currentSdkFilter += 2 ' Convert index to SDK number (3-9)
        End If

        showDisabled = chkShowDisabled.Checked
        showBuiltIn = chkShowBuiltIn.Checked

        ' Reload with new filters
        LoadLibraries()
    End Sub

    ''' <summary>
    ''' ListView selection changed
    ''' </summary>
    Private Sub LvLibraries_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim hasSelection = lvLibraries.SelectedItems.Count > 0

        If hasSelection Then
            Dim selectedLib = TryCast(lvLibraries.SelectedItems(0).Tag, LibraryManager.CustomLibrary)
            If selectedLib IsNot Nothing Then
                ' Enable buttons based on library type
                btnRemove.Enabled = Not selectedLib.IsBuiltIn
                btnToggleEnabled.Enabled = Not selectedLib.IsBuiltIn
                btnEditDescription.Enabled = True
            Else
                btnRemove.Enabled = False
                btnToggleEnabled.Enabled = False
                btnEditDescription.Enabled = False
            End If
        Else
            btnRemove.Enabled = False
            btnToggleEnabled.Enabled = False
            btnEditDescription.Enabled = False
        End If
    End Sub

    ''' <summary>
    ''' ListView item checked (enable/disable)
    ''' </summary>
    Private Sub LvLibraries_ItemChecked(sender As Object, e As ItemCheckedEventArgs)
        Try
            Dim library = TryCast(e.Item.Tag, LibraryManager.CustomLibrary)
            If library IsNot Nothing AndAlso Not library.IsBuiltIn Then
                LibraryManager.ToggleLibraryEnabled(library.Id, e.Item.Checked)
            End If
        Catch ex As Exception
            ' Silent fail - will be refreshed
        End Try
    End Sub

    ''' <summary>
    ''' ListView double-click (edit description)
    ''' </summary>
    Private Sub LvLibraries_DoubleClick(sender As Object, e As EventArgs)
        BtnEditDescription_Click(sender, e)
    End Sub

    ''' <summary>
    ''' Format file size
    ''' </summary>
    Private Function FormatFileSize(bytes As Long) As String
        If bytes < 1024 Then
            Return bytes.ToString() & " B"
        ElseIf bytes < 1024 * 1024 Then
            Return (bytes / 1024.0).ToString("F1") & " KB"
        Else
            Return (bytes / (1024.0 * 1024.0)).ToString("F2") & " MB"
        End If
    End Function

End Class