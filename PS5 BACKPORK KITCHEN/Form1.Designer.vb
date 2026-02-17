<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    'Inherits System.Windows.Forms.Form
    Inherits ReaLTaiizor.Forms.PoisonForm

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        BtnBrowse = New ReaLTaiizor.Controls.Button()
        TableLayoutPanel1 = New TableLayoutPanel()
        rtbStatus = New RichTextBox()
        TableLayoutPanel4 = New TableLayoutPanel()
        gamepic = New ReaLTaiizor.Controls.HopePictureBox()
        RichGameInfo = New RichTextBox()
        BtnStart = New Button()
        LblStat = New ReaLTaiizor.Controls.PoisonLabel()
        TableLayoutPanel2 = New TableLayoutPanel()
        PoisonLabel2 = New ReaLTaiizor.Controls.PoisonLabel()
        TableLayoutPanel3 = New TableLayoutPanel()
        lblVer = New ReaLTaiizor.Controls.DungeonLabel()
        DungeonLinkLabel2 = New ReaLTaiizor.Controls.DungeonLinkLabel()
        DungeonLinkLabel1 = New ReaLTaiizor.Controls.DungeonLinkLabel()
        StatusPic = New PictureBox()
        chkBackup = New ReaLTaiizor.Controls.DungeonCheckBox()
        Separator2 = New ReaLTaiizor.Controls.Separator()
        MoonButton1 = New ReaLTaiizor.Controls.MoonButton()
        Txtpath = New ReaLTaiizor.Controls.CrownTextBox()
        lblexperiment = New ReaLTaiizor.Controls.PoisonLabel()
        NightLinkLabel1 = New ReaLTaiizor.Controls.NightLinkLabel()
        TableLayoutPanel5 = New TableLayoutPanel()
        cmbPs5Sdk = New ReaLTaiizor.Controls.PoisonComboBox()
        Label1 = New Label()
        lblfw = New Label()
        btnPayloadManager = New Button()
        btnAdvancedOps = New Button()
        btnBatchProcess = New Button()
        btnElfInspector = New Button()
        btnShowStatistics = New Button()
        btnUFS2Image = New Button()
        btnPkgManager = New Button()
        btnAdvancedBackport = New Button()
        lblDragDropHint = New ReaLTaiizor.Controls.MoonLabel()
        chklibcpatch = New ReaLTaiizor.Controls.FoxCheckBoxEdit()
        FlowLayoutPanel1 = New FlowLayoutPanel()
        TableLayoutPanel1.SuspendLayout()
        TableLayoutPanel4.SuspendLayout()
        CType(gamepic, ComponentModel.ISupportInitialize).BeginInit()
        TableLayoutPanel2.SuspendLayout()
        TableLayoutPanel3.SuspendLayout()
        CType(StatusPic, ComponentModel.ISupportInitialize).BeginInit()
        TableLayoutPanel5.SuspendLayout()
        FlowLayoutPanel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' BtnBrowse
        ' 
        BtnBrowse.BackColor = Color.Transparent
        BtnBrowse.BorderColor = Color.FromArgb(CByte(32), CByte(34), CByte(37))
        BtnBrowse.EnteredBorderColor = Color.FromArgb(CByte(165), CByte(37), CByte(37))
        BtnBrowse.EnteredColor = Color.FromArgb(CByte(32), CByte(34), CByte(37))
        BtnBrowse.Font = New Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        BtnBrowse.Image = Nothing
        BtnBrowse.ImageAlign = ContentAlignment.MiddleLeft
        BtnBrowse.InactiveColor = Color.FromArgb(CByte(32), CByte(34), CByte(37))
        BtnBrowse.Location = New Point(615, 84)
        BtnBrowse.Name = "BtnBrowse"
        BtnBrowse.PressedBorderColor = Color.FromArgb(CByte(165), CByte(37), CByte(37))
        BtnBrowse.PressedColor = Color.FromArgb(CByte(165), CByte(37), CByte(37))
        BtnBrowse.Size = New Size(154, 23)
        BtnBrowse.TabIndex = 1
        BtnBrowse.Text = "Browse"
        BtnBrowse.TextAlignment = StringAlignment.Center
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        TableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        TableLayoutPanel1.ColumnCount = 2
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.93834F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 49.06166F))
        TableLayoutPanel1.Controls.Add(rtbStatus, 1, 0)
        TableLayoutPanel1.Controls.Add(TableLayoutPanel4, 0, 0)
        TableLayoutPanel1.Location = New Point(23, 156)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 1
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Size = New Size(842, 168)
        TableLayoutPanel1.TabIndex = 2
        ' 
        ' rtbStatus
        ' 
        rtbStatus.BackColor = Color.White
        rtbStatus.BorderStyle = BorderStyle.None
        rtbStatus.Dock = DockStyle.Fill
        rtbStatus.Location = New Point(432, 4)
        rtbStatus.Name = "rtbStatus"
        rtbStatus.ReadOnly = True
        rtbStatus.Size = New Size(406, 160)
        rtbStatus.TabIndex = 0
        rtbStatus.Text = ""
        ' 
        ' TableLayoutPanel4
        ' 
        TableLayoutPanel4.ColumnCount = 2
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel4.Controls.Add(gamepic, 0, 0)
        TableLayoutPanel4.Controls.Add(RichGameInfo, 1, 0)
        TableLayoutPanel4.Dock = DockStyle.Fill
        TableLayoutPanel4.Location = New Point(4, 4)
        TableLayoutPanel4.Name = "TableLayoutPanel4"
        TableLayoutPanel4.RowCount = 1
        TableLayoutPanel4.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel4.Size = New Size(421, 160)
        TableLayoutPanel4.TabIndex = 1
        ' 
        ' gamepic
        ' 
        gamepic.BackColor = Color.FromArgb(CByte(192), CByte(196), CByte(204))
        gamepic.Dock = DockStyle.Fill
        gamepic.Image = My.Resources.Resources.game_controller
        gamepic.Location = New Point(3, 3)
        gamepic.Name = "gamepic"
        gamepic.PixelOffsetType = Drawing2D.PixelOffsetMode.HighQuality
        gamepic.Size = New Size(204, 154)
        gamepic.SizeMode = PictureBoxSizeMode.Zoom
        gamepic.SmoothingType = Drawing2D.SmoothingMode.HighQuality
        gamepic.TabIndex = 0
        gamepic.TabStop = False
        gamepic.TextRenderingType = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        ' 
        ' RichGameInfo
        ' 
        RichGameInfo.BackColor = SystemColors.ControlLightLight
        RichGameInfo.BorderStyle = BorderStyle.None
        RichGameInfo.Dock = DockStyle.Fill
        RichGameInfo.Location = New Point(213, 3)
        RichGameInfo.Name = "RichGameInfo"
        RichGameInfo.ReadOnly = True
        RichGameInfo.Size = New Size(205, 154)
        RichGameInfo.TabIndex = 1
        RichGameInfo.Text = ""
        ' 
        ' BtnStart
        ' 
        BtnStart.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        BtnStart.Font = New Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        BtnStart.Image = My.Resources.Resources.meat
        BtnStart.ImageAlign = ContentAlignment.MiddleLeft
        BtnStart.Location = New Point(711, 333)
        BtnStart.Name = "BtnStart"
        BtnStart.Size = New Size(147, 51)
        BtnStart.TabIndex = 3
        BtnStart.Text = "Start Cooking"
        BtnStart.TextAlign = ContentAlignment.MiddleRight
        BtnStart.UseVisualStyleBackColor = True
        ' 
        ' LblStat
        ' 
        LblStat.Anchor = AnchorStyles.Bottom
        LblStat.BackColor = Color.MistyRose
        LblStat.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LblStat.Location = New Point(68, 486)
        LblStat.Name = "LblStat"
        LblStat.Size = New Size(760, 19)
        LblStat.TabIndex = 0
        LblStat.Text = "Status:Idle !!"
        LblStat.TextAlign = ContentAlignment.MiddleCenter
        LblStat.UseCustomFont = True
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.Anchor = AnchorStyles.Bottom
        TableLayoutPanel2.ColumnCount = 3
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 7.53246737F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 92.46753F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 16F))
        TableLayoutPanel2.Controls.Add(PoisonLabel2, 0, 0)
        TableLayoutPanel2.Controls.Add(TableLayoutPanel3, 1, 0)
        TableLayoutPanel2.Location = New Point(68, 508)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 46.1538467F))
        TableLayoutPanel2.Size = New Size(770, 46)
        TableLayoutPanel2.TabIndex = 4
        ' 
        ' PoisonLabel2
        ' 
        PoisonLabel2.Dock = DockStyle.Fill
        PoisonLabel2.FlatStyle = FlatStyle.Popup
        PoisonLabel2.Font = New Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        PoisonLabel2.Location = New Point(3, 0)
        PoisonLabel2.Name = "PoisonLabel2"
        PoisonLabel2.Size = New Size(50, 46)
        PoisonLabel2.TabIndex = 2
        PoisonLabel2.Text = "Chef:"
        PoisonLabel2.TextAlign = ContentAlignment.MiddleCenter
        PoisonLabel2.UseCustomFont = True
        ' 
        ' TableLayoutPanel3
        ' 
        TableLayoutPanel3.ColumnCount = 4
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 52.4663658F))
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 47.5336342F))
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 175F))
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 315F))
        TableLayoutPanel3.Controls.Add(lblVer, 3, 0)
        TableLayoutPanel3.Controls.Add(DungeonLinkLabel2, 1, 0)
        TableLayoutPanel3.Controls.Add(DungeonLinkLabel1, 0, 0)
        TableLayoutPanel3.Location = New Point(59, 3)
        TableLayoutPanel3.Name = "TableLayoutPanel3"
        TableLayoutPanel3.RowCount = 1
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.Size = New Size(691, 35)
        TableLayoutPanel3.TabIndex = 3
        ' 
        ' lblVer
        ' 
        lblVer.AutoSize = True
        lblVer.BackColor = Color.Transparent
        lblVer.Dock = DockStyle.Fill
        lblVer.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblVer.ForeColor = Color.FromArgb(CByte(76), CByte(76), CByte(77))
        lblVer.ImageAlign = ContentAlignment.MiddleRight
        lblVer.Location = New Point(378, 0)
        lblVer.Name = "lblVer"
        lblVer.Size = New Size(310, 35)
        lblVer.TabIndex = 13
        lblVer.Text = "Version:2.1.0"
        lblVer.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' DungeonLinkLabel2
        ' 
        DungeonLinkLabel2.ActiveLinkColor = Color.FromArgb(CByte(221), CByte(72), CByte(20))
        DungeonLinkLabel2.AutoSize = True
        DungeonLinkLabel2.BackColor = Color.Transparent
        DungeonLinkLabel2.Dock = DockStyle.Fill
        DungeonLinkLabel2.Font = New Font("Segoe UI", 11F)
        DungeonLinkLabel2.LinkBehavior = LinkBehavior.AlwaysUnderline
        DungeonLinkLabel2.LinkColor = Color.Blue
        DungeonLinkLabel2.Location = New Point(108, 0)
        DungeonLinkLabel2.Name = "DungeonLinkLabel2"
        DungeonLinkLabel2.Size = New Size(89, 35)
        DungeonLinkLabel2.TabIndex = 6
        DungeonLinkLabel2.TabStop = True
        DungeonLinkLabel2.Text = "Credits"
        DungeonLinkLabel2.TextAlign = ContentAlignment.MiddleCenter
        DungeonLinkLabel2.VisitedLinkColor = Color.FromArgb(CByte(240), CByte(119), CByte(70))
        ' 
        ' DungeonLinkLabel1
        ' 
        DungeonLinkLabel1.ActiveLinkColor = Color.FromArgb(CByte(221), CByte(72), CByte(20))
        DungeonLinkLabel1.AutoSize = True
        DungeonLinkLabel1.BackColor = Color.Transparent
        DungeonLinkLabel1.Dock = DockStyle.Fill
        DungeonLinkLabel1.Font = New Font("Segoe UI", 11F)
        DungeonLinkLabel1.LinkBehavior = LinkBehavior.AlwaysUnderline
        DungeonLinkLabel1.LinkColor = Color.Blue
        DungeonLinkLabel1.Location = New Point(3, 0)
        DungeonLinkLabel1.Name = "DungeonLinkLabel1"
        DungeonLinkLabel1.Size = New Size(99, 35)
        DungeonLinkLabel1.TabIndex = 5
        DungeonLinkLabel1.TabStop = True
        DungeonLinkLabel1.Text = "rajeshca911"
        DungeonLinkLabel1.TextAlign = ContentAlignment.MiddleCenter
        DungeonLinkLabel1.VisitedLinkColor = Color.FromArgb(CByte(240), CByte(119), CByte(70))
        ' 
        ' StatusPic
        ' 
        StatusPic.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        StatusPic.Image = My.Resources.Resources.Blue
        StatusPic.Location = New Point(858, 499)
        StatusPic.Name = "StatusPic"
        StatusPic.Size = New Size(28, 23)
        StatusPic.SizeMode = PictureBoxSizeMode.Zoom
        StatusPic.TabIndex = 5
        StatusPic.TabStop = False
        ' 
        ' chkBackup
        ' 
        chkBackup.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        chkBackup.BackColor = Color.Transparent
        chkBackup.Checked = True
        chkBackup.CheckedBackColorA = Color.FromArgb(CByte(213), CByte(85), CByte(32))
        chkBackup.CheckedBackColorB = Color.FromArgb(CByte(224), CByte(123), CByte(82))
        chkBackup.CheckedBorderColor = Color.FromArgb(CByte(182), CByte(88), CByte(55))
        chkBackup.CheckedColor = Color.FromArgb(CByte(255), CByte(255), CByte(255))
        chkBackup.Font = New Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        chkBackup.ForeColor = Color.FromArgb(CByte(76), CByte(76), CByte(95))
        chkBackup.Location = New Point(601, 365)
        chkBackup.Name = "chkBackup"
        chkBackup.Size = New Size(104, 15)
        chkBackup.TabIndex = 5
        chkBackup.Text = "Backup"
        chkBackup.Visible = False
        ' 
        ' Separator2
        ' 
        Separator2.Anchor = AnchorStyles.Bottom
        Separator2.LineColor = Color.Gray
        Separator2.Location = New Point(76, 471)
        Separator2.Name = "Separator2"
        Separator2.Size = New Size(745, 10)
        Separator2.TabIndex = 14
        Separator2.Text = "Separator2"
        ' 
        ' MoonButton1
        ' 
        MoonButton1.Customization = "/////9PT0//w8PD/gICA/w=="
        MoonButton1.Font = New Font("Segoe UI", 9F)
        MoonButton1.Image = Nothing
        MoonButton1.Location = New Point(734, 439)
        MoonButton1.Name = "MoonButton1"
        MoonButton1.NoRounding = False
        MoonButton1.Size = New Size(104, 26)
        MoonButton1.TabIndex = 15
        MoonButton1.Text = "Restore Files"
        MoonButton1.Transparent = False
        MoonButton1.Visible = False
        ' 
        ' Txtpath
        ' 
        Txtpath.AllowDrop = True
        Txtpath.BackColor = Color.White
        Txtpath.BorderStyle = BorderStyle.FixedSingle
        Txtpath.Enabled = False
        Txtpath.ForeColor = SystemColors.ControlText
        Txtpath.Location = New Point(28, 84)
        Txtpath.Name = "Txtpath"
        Txtpath.PlaceholderText = "Please Select PPSAXXXX Folder"
        Txtpath.ReadOnly = True
        Txtpath.Size = New Size(571, 23)
        Txtpath.TabIndex = 16
        ' 
        ' lblexperiment
        ' 
        lblexperiment.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        lblexperiment.AutoSize = True
        lblexperiment.Font = New Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblexperiment.ForeColor = Color.Red
        lblexperiment.Location = New Point(743, 412)
        lblexperiment.Name = "lblexperiment"
        lblexperiment.Size = New Size(95, 17)
        lblexperiment.TabIndex = 19
        lblexperiment.Text = "(Experimental)"
        lblexperiment.UseCustomFont = True
        ' 
        ' NightLinkLabel1
        ' 
        NightLinkLabel1.ActiveLinkColor = Color.FromArgb(CByte(222), CByte(89), CByte(84))
        NightLinkLabel1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        NightLinkLabel1.AutoSize = True
        NightLinkLabel1.BackColor = Color.Transparent
        NightLinkLabel1.Font = New Font("Segoe UI", 9F)
        NightLinkLabel1.LinkBehavior = LinkBehavior.HoverUnderline
        NightLinkLabel1.LinkColor = Color.FromArgb(CByte(242), CByte(93), CByte(89))
        NightLinkLabel1.Location = New Point(455, 327)
        NightLinkLabel1.Name = "NightLinkLabel1"
        NightLinkLabel1.Size = New Size(88, 15)
        NightLinkLabel1.TabIndex = 20
        NightLinkLabel1.TabStop = True
        NightLinkLabel1.Text = "View Patch Log"
        NightLinkLabel1.VisitedLinkColor = Color.FromArgb(CByte(0), CByte(0), CByte(192))
        ' 
        ' TableLayoutPanel5
        ' 
        TableLayoutPanel5.ColumnCount = 3
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 39.5F))
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60.5F))
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 219F))
        TableLayoutPanel5.Controls.Add(cmbPs5Sdk, 1, 0)
        TableLayoutPanel5.Controls.Add(Label1, 0, 0)
        TableLayoutPanel5.Controls.Add(lblfw, 2, 0)
        TableLayoutPanel5.Location = New Point(23, 113)
        TableLayoutPanel5.Name = "TableLayoutPanel5"
        TableLayoutPanel5.RowCount = 1
        TableLayoutPanel5.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel5.Size = New Size(421, 37)
        TableLayoutPanel5.TabIndex = 22
        ' 
        ' cmbPs5Sdk
        ' 
        cmbPs5Sdk.Dock = DockStyle.Fill
        cmbPs5Sdk.FormattingEnabled = True
        cmbPs5Sdk.ItemHeight = 23
        cmbPs5Sdk.Location = New Point(82, 3)
        cmbPs5Sdk.Name = "cmbPs5Sdk"
        cmbPs5Sdk.PromptText = "PS5 FW"
        cmbPs5Sdk.Size = New Size(116, 29)
        cmbPs5Sdk.TabIndex = 26
        cmbPs5Sdk.Text = "PS5 FW"
        cmbPs5Sdk.UseSelectable = True
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Dock = DockStyle.Fill
        Label1.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label1.ForeColor = Color.Black
        Label1.Location = New Point(3, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(73, 37)
        Label1.TabIndex = 1
        Label1.Text = "PS5 FW:"
        Label1.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' lblfw
        ' 
        lblfw.AutoSize = True
        lblfw.Dock = DockStyle.Fill
        lblfw.Font = New Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblfw.ForeColor = Color.Black
        lblfw.Location = New Point(204, 0)
        lblfw.Name = "lblfw"
        lblfw.Size = New Size(214, 37)
        lblfw.TabIndex = 27
        lblfw.Text = "Loading.."
        lblfw.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' btnPayloadManager
        ' 
        btnPayloadManager.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnPayloadManager.FlatStyle = FlatStyle.System
        btnPayloadManager.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnPayloadManager.ForeColor = Color.Black
        btnPayloadManager.Location = New Point(3, 35)
        btnPayloadManager.Name = "btnPayloadManager"
        btnPayloadManager.Size = New Size(167, 26)
        btnPayloadManager.TabIndex = 4
        btnPayloadManager.Text = "btnPayloadManager"
        btnPayloadManager.UseVisualStyleBackColor = False
        ' 
        ' btnAdvancedOps
        ' 
        btnAdvancedOps.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnAdvancedOps.FlatStyle = FlatStyle.System
        btnAdvancedOps.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnAdvancedOps.ForeColor = Color.Black
        btnAdvancedOps.Location = New Point(176, 67)
        btnAdvancedOps.Name = "btnAdvancedOps"
        btnAdvancedOps.Size = New Size(167, 26)
        btnAdvancedOps.TabIndex = 3
        btnAdvancedOps.Text = "btnAdvancedOps"
        btnAdvancedOps.UseVisualStyleBackColor = False
        ' 
        ' btnBatchProcess
        ' 
        btnBatchProcess.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnBatchProcess.FlatStyle = FlatStyle.System
        btnBatchProcess.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnBatchProcess.ForeColor = Color.Black
        btnBatchProcess.Location = New Point(176, 3)
        btnBatchProcess.Name = "btnBatchProcess"
        btnBatchProcess.Size = New Size(167, 26)
        btnBatchProcess.TabIndex = 2
        btnBatchProcess.Text = "btnBatchProcess"
        btnBatchProcess.UseVisualStyleBackColor = False
        ' 
        ' btnElfInspector
        ' 
        btnElfInspector.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnElfInspector.FlatStyle = FlatStyle.System
        btnElfInspector.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnElfInspector.ForeColor = Color.Black
        btnElfInspector.Location = New Point(3, 67)
        btnElfInspector.Name = "btnElfInspector"
        btnElfInspector.Size = New Size(167, 26)
        btnElfInspector.TabIndex = 1
        btnElfInspector.Text = "btnElfInspector"
        btnElfInspector.UseVisualStyleBackColor = False
        ' 
        ' btnShowStatistics
        ' 
        btnShowStatistics.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnShowStatistics.FlatStyle = FlatStyle.System
        btnShowStatistics.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnShowStatistics.ForeColor = Color.Black
        btnShowStatistics.Location = New Point(3, 3)
        btnShowStatistics.Name = "btnShowStatistics"
        btnShowStatistics.Size = New Size(167, 26)
        btnShowStatistics.TabIndex = 0
        btnShowStatistics.Text = "Stats"
        btnShowStatistics.UseVisualStyleBackColor = False
        ' 
        ' btnUFS2Image
        ' 
        btnUFS2Image.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnUFS2Image.FlatStyle = FlatStyle.System
        btnUFS2Image.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnUFS2Image.ForeColor = Color.Black
        btnUFS2Image.Location = New Point(176, 35)
        btnUFS2Image.Name = "btnUFS2Image"
        btnUFS2Image.Size = New Size(167, 26)
        btnUFS2Image.TabIndex = 6
        btnUFS2Image.Text = "btnUFS2Image"
        btnUFS2Image.UseVisualStyleBackColor = False
        btnUFS2Image.Visible = False
        ' 
        ' btnPkgManager
        ' 
        btnPkgManager.BackColor = Color.FromArgb(CByte(137), CByte(206), CByte(248))
        btnPkgManager.FlatStyle = FlatStyle.System
        btnPkgManager.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnPkgManager.ForeColor = Color.Black
        btnPkgManager.Location = New Point(3, 99)
        btnPkgManager.Name = "btnPkgManager"
        btnPkgManager.Size = New Size(167, 26)
        btnPkgManager.TabIndex = 7
        btnPkgManager.Text = "btnPkgManager"
        btnPkgManager.UseVisualStyleBackColor = False
        '
        ' btnAdvancedBackport
        '
        btnAdvancedBackport.BackColor = Color.FromArgb(CByte(200), CByte(160), CByte(255))
        btnAdvancedBackport.FlatStyle = FlatStyle.System
        btnAdvancedBackport.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnAdvancedBackport.ForeColor = Color.Black
        btnAdvancedBackport.Location = New Point(176, 99)
        btnAdvancedBackport.Name = "btnAdvancedBackport"
        btnAdvancedBackport.Size = New Size(167, 26)
        btnAdvancedBackport.TabIndex = 8
        btnAdvancedBackport.Text = "btnAdvancedBackport"
        btnAdvancedBackport.UseVisualStyleBackColor = False
        '
        ' lblDragDropHint
        ' 
        lblDragDropHint.AutoSize = True
        lblDragDropHint.BackColor = Color.Transparent
        lblDragDropHint.ForeColor = Color.Gray
        lblDragDropHint.Location = New Point(30, 66)
        lblDragDropHint.Name = "lblDragDropHint"
        lblDragDropHint.Size = New Size(73, 15)
        lblDragDropHint.TabIndex = 24
        lblDragDropHint.Text = "MoonLabel1"
        ' 
        ' chklibcpatch
        ' 
        chklibcpatch.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        chklibcpatch.BackColor = Color.White
        chklibcpatch.BorderColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
        chklibcpatch.Checked = False
        chklibcpatch.DisabledBorderColor = Color.FromArgb(CByte(230), CByte(230), CByte(230))
        chklibcpatch.DisabledTextColor = Color.FromArgb(CByte(166), CByte(178), CByte(190))
        chklibcpatch.EnabledCalc = True
        chklibcpatch.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        chklibcpatch.ForeColor = Color.FromArgb(CByte(66), CByte(78), CByte(90))
        chklibcpatch.HoverBorderColor = Color.FromArgb(CByte(44), CByte(156), CByte(218))
        chklibcpatch.Location = New Point(718, 390)
        chklibcpatch.Name = "chklibcpatch"
        chklibcpatch.Size = New Size(147, 23)
        chklibcpatch.TabIndex = 25
        chklibcpatch.Text = "libc Patch"
        ' 
        ' FlowLayoutPanel1
        ' 
        FlowLayoutPanel1.Controls.Add(btnShowStatistics)
        FlowLayoutPanel1.Controls.Add(btnBatchProcess)
        FlowLayoutPanel1.Controls.Add(btnPayloadManager)
        FlowLayoutPanel1.Controls.Add(btnUFS2Image)
        FlowLayoutPanel1.Controls.Add(btnElfInspector)
        FlowLayoutPanel1.Controls.Add(btnAdvancedOps)
        FlowLayoutPanel1.Controls.Add(btnPkgManager)
        FlowLayoutPanel1.Controls.Add(btnAdvancedBackport)
        FlowLayoutPanel1.Location = New Point(26, 330)
        FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        FlowLayoutPanel1.Size = New Size(353, 135)
        FlowLayoutPanel1.TabIndex = 26
        ' 
        ' Form1
        ' 
        AllowDrop = True
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(906, 555)
        Controls.Add(FlowLayoutPanel1)
        Controls.Add(chklibcpatch)
        Controls.Add(lblDragDropHint)
        Controls.Add(TableLayoutPanel5)
        Controls.Add(NightLinkLabel1)
        Controls.Add(lblexperiment)
        Controls.Add(Txtpath)
        Controls.Add(MoonButton1)
        Controls.Add(StatusPic)
        Controls.Add(LblStat)
        Controls.Add(Separator2)
        Controls.Add(chkBackup)
        Controls.Add(TableLayoutPanel2)
        Controls.Add(BtnStart)
        Controls.Add(TableLayoutPanel1)
        Controls.Add(BtnBrowse)
        ForeColor = Color.White
        Name = "Form1"
        Text = "Backpork-GUI"
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel4.ResumeLayout(False)
        CType(gamepic, ComponentModel.ISupportInitialize).EndInit()
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel3.ResumeLayout(False)
        TableLayoutPanel3.PerformLayout()
        CType(StatusPic, ComponentModel.ISupportInitialize).EndInit()
        TableLayoutPanel5.ResumeLayout(False)
        TableLayoutPanel5.PerformLayout()
        FlowLayoutPanel1.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub
    Friend WithEvents BtnBrowse As ReaLTaiizor.Controls.Button
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents BtnStart As Button
    Friend WithEvents LblStat As ReaLTaiizor.Controls.PoisonLabel
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents PoisonLabel2 As ReaLTaiizor.Controls.PoisonLabel
    Friend WithEvents TableLayoutPanel3 As TableLayoutPanel
    Friend WithEvents DungeonLinkLabel1 As ReaLTaiizor.Controls.DungeonLinkLabel
    Friend WithEvents StatusPic As PictureBox
    Friend WithEvents rtbStatus As RichTextBox
    Friend WithEvents chkBackup As ReaLTaiizor.Controls.DungeonCheckBox
    Friend WithEvents TableLayoutPanel4 As TableLayoutPanel
    Friend WithEvents gamepic As ReaLTaiizor.Controls.HopePictureBox
    Friend WithEvents RichGameInfo As RichTextBox
    Friend WithEvents Separator2 As ReaLTaiizor.Controls.Separator
    Friend WithEvents DungeonLinkLabel2 As ReaLTaiizor.Controls.DungeonLinkLabel
    Friend WithEvents MoonButton1 As ReaLTaiizor.Controls.MoonButton
    Friend WithEvents Txtpath As ReaLTaiizor.Controls.CrownTextBox
    Friend WithEvents lblexperiment As ReaLTaiizor.Controls.PoisonLabel
    Friend WithEvents NightLinkLabel1 As ReaLTaiizor.Controls.NightLinkLabel
    Friend WithEvents TableLayoutPanel5 As TableLayoutPanel
    Friend WithEvents lblVer As ReaLTaiizor.Controls.DungeonLabel
    Friend WithEvents lblDragDropHint As ReaLTaiizor.Controls.MoonLabel
    Friend WithEvents btnShowStatistics As Button
    Friend WithEvents btnPayloadManager As Button
    Friend WithEvents btnAdvancedOps As Button
    Friend WithEvents btnBatchProcess As Button
    Friend WithEvents btnElfInspector As Button
    Friend WithEvents chklibcpatch As ReaLTaiizor.Controls.FoxCheckBoxEdit
    Friend WithEvents Label1 As Label
    Friend WithEvents cmbPs5Sdk As ReaLTaiizor.Controls.PoisonComboBox
    Friend WithEvents lblfw As Label
    Friend WithEvents btnUFS2Image As Button
    Friend WithEvents btnPkgManager As Button
    Friend WithEvents btnAdvancedBackport As Button
    Friend WithEvents FlowLayoutPanel1 As FlowLayoutPanel

End Class
