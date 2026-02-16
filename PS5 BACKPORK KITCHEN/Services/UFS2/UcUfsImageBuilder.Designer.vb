<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UcUfsImageBuilder
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        tlpMain = New TableLayoutPanel()
        grpOutput = New GroupBox()
        TableLayoutPanel2 = New TableLayoutPanel()
        txtOutputImage = New TextBox()
        btnBrowseOutput = New Button()
        pnlHeader = New Panel()
        lblSub = New Label()
        lblTitle = New Label()
        grpSource = New GroupBox()
        TableLayoutPanel1 = New TableLayoutPanel()
        txtSourceFolder = New TextBox()
        btnBrowseSource = New Button()
        FlowLayoutPanel1 = New FlowLayoutPanel()
        rdoPreset = New RadioButton()
        rdoCustom = New RadioButton()
        Label1 = New Label()
        lnkAdvanced = New LinkLabel()
        pnlAdvanced = New Panel()
        TableLayoutPanel3 = New TableLayoutPanel()
        Label5 = New Label()
        cmbOptimize = New ComboBox()
        Label4 = New Label()
        Label3 = New Label()
        Label2 = New Label()
        txtVolumeLabel = New TextBox()
        cmbBlockSize = New ComboBox()
        txtExtraFlags = New TextBox()
        tlpExec = New TableLayoutPanel()
        TableLayoutPanel4 = New TableLayoutPanel()
        btnOpenOutput = New Button()
        btnStop = New Button()
        btnGenerate = New Button()
        TableLayoutPanel5 = New TableLayoutPanel()
        pbBuild = New ProgressBar()
        lblStatus = New Label()
        TableLayoutPanel6 = New TableLayoutPanel()
        rtbLog = New RichTextBox()
        txtCommandPreview = New TextBox()
        tlpMain.SuspendLayout()
        grpOutput.SuspendLayout()
        TableLayoutPanel2.SuspendLayout()
        pnlHeader.SuspendLayout()
        grpSource.SuspendLayout()
        TableLayoutPanel1.SuspendLayout()
        FlowLayoutPanel1.SuspendLayout()
        pnlAdvanced.SuspendLayout()
        TableLayoutPanel3.SuspendLayout()
        tlpExec.SuspendLayout()
        TableLayoutPanel4.SuspendLayout()
        TableLayoutPanel5.SuspendLayout()
        TableLayoutPanel6.SuspendLayout()
        SuspendLayout()
        ' 
        ' tlpMain
        ' 
        tlpMain.ColumnCount = 1
        tlpMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100F))
        tlpMain.Controls.Add(grpOutput, 0, 2)
        tlpMain.Controls.Add(pnlHeader, 0, 0)
        tlpMain.Controls.Add(grpSource, 0, 1)
        tlpMain.Controls.Add(FlowLayoutPanel1, 0, 3)
        tlpMain.Controls.Add(tlpExec, 0, 5)
        tlpMain.Dock = DockStyle.Fill
        tlpMain.Location = New Point(0, 0)
        tlpMain.Name = "tlpMain"
        tlpMain.Padding = New Padding(12)
        tlpMain.RowCount = 6
        tlpMain.RowStyles.Add(New RowStyle(SizeType.Absolute, 42F))
        tlpMain.RowStyles.Add(New RowStyle(SizeType.Absolute, 82F))
        tlpMain.RowStyles.Add(New RowStyle(SizeType.Absolute, 82F))
        tlpMain.RowStyles.Add(New RowStyle(SizeType.Absolute, 46F))
        tlpMain.RowStyles.Add(New RowStyle())
        tlpMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        tlpMain.Size = New Size(814, 560)
        tlpMain.TabIndex = 0
        ' 
        ' grpOutput
        ' 
        grpOutput.Controls.Add(TableLayoutPanel2)
        grpOutput.Location = New Point(15, 139)
        grpOutput.Name = "grpOutput"
        grpOutput.Size = New Size(512, 76)
        grpOutput.TabIndex = 2
        grpOutput.TabStop = False
        grpOutput.Text = "Source Folder"
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.ColumnCount = 2
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 110F))
        TableLayoutPanel2.Controls.Add(txtOutputImage, 0, 0)
        TableLayoutPanel2.Controls.Add(btnBrowseOutput, 1, 0)
        TableLayoutPanel2.Dock = DockStyle.Fill
        TableLayoutPanel2.Location = New Point(3, 19)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        TableLayoutPanel2.Size = New Size(506, 54)
        TableLayoutPanel2.TabIndex = 0
        ' 
        ' txtOutputImage
        ' 
        txtOutputImage.BackColor = Color.FromArgb(CByte(40), CByte(40), CByte(42))
        txtOutputImage.BorderStyle = BorderStyle.FixedSingle
        txtOutputImage.Dock = DockStyle.Fill
        txtOutputImage.Location = New Point(3, 3)
        txtOutputImage.Name = "txtOutputImage"
        txtOutputImage.ReadOnly = True
        txtOutputImage.Size = New Size(390, 23)
        txtOutputImage.TabIndex = 0
        ' 
        ' btnBrowseOutput
        ' 
        btnBrowseOutput.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(65))
        btnBrowseOutput.FlatStyle = FlatStyle.Flat
        btnBrowseOutput.Location = New Point(399, 3)
        btnBrowseOutput.Name = "btnBrowseOutput"
        btnBrowseOutput.Size = New Size(100, 23)
        btnBrowseOutput.TabIndex = 1
        btnBrowseOutput.Text = "Save As…" & vbCrLf
        btnBrowseOutput.UseVisualStyleBackColor = False
        ' 
        ' pnlHeader
        ' 
        pnlHeader.Controls.Add(lblSub)
        pnlHeader.Controls.Add(lblTitle)
        pnlHeader.Location = New Point(15, 15)
        pnlHeader.Name = "pnlHeader"
        pnlHeader.Size = New Size(557, 36)
        pnlHeader.TabIndex = 0
        ' 
        ' lblSub
        ' 
        lblSub.AutoSize = True
        lblSub.Font = New Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblSub.ForeColor = Color.DarkGray
        lblSub.Location = New Point(20, 23)
        lblSub.Name = "lblSub"
        lblSub.Size = New Size(298, 13)
        lblSub.TabIndex = 1
        lblSub.Text = "Create PlayStation-readable UFS image from game folder"
        ' 
        ' lblTitle
        ' 
        lblTitle.AutoSize = True
        lblTitle.Dock = DockStyle.Top
        lblTitle.Font = New Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblTitle.Location = New Point(0, 0)
        lblTitle.Name = "lblTitle"
        lblTitle.Size = New Size(169, 25)
        lblTitle.TabIndex = 0
        lblTitle.Text = "UFS Image Builder"
        ' 
        ' grpSource
        ' 
        grpSource.Controls.Add(TableLayoutPanel1)
        grpSource.Location = New Point(15, 57)
        grpSource.Name = "grpSource"
        grpSource.Size = New Size(512, 76)
        grpSource.TabIndex = 1
        grpSource.TabStop = False
        grpSource.Text = "Source Folder"
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.ColumnCount = 2
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 110F))
        TableLayoutPanel1.Controls.Add(txtSourceFolder, 0, 0)
        TableLayoutPanel1.Controls.Add(btnBrowseSource, 1, 0)
        TableLayoutPanel1.Dock = DockStyle.Fill
        TableLayoutPanel1.Location = New Point(3, 19)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 1
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        TableLayoutPanel1.Size = New Size(506, 54)
        TableLayoutPanel1.TabIndex = 0
        ' 
        ' txtSourceFolder
        ' 
        txtSourceFolder.BackColor = Color.FromArgb(CByte(40), CByte(40), CByte(42))
        txtSourceFolder.BorderStyle = BorderStyle.FixedSingle
        txtSourceFolder.Dock = DockStyle.Fill
        txtSourceFolder.Location = New Point(3, 3)
        txtSourceFolder.Name = "txtSourceFolder"
        txtSourceFolder.ReadOnly = True
        txtSourceFolder.Size = New Size(390, 23)
        txtSourceFolder.TabIndex = 0
        ' 
        ' btnBrowseSource
        ' 
        btnBrowseSource.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(65))
        btnBrowseSource.FlatStyle = FlatStyle.Flat
        btnBrowseSource.Location = New Point(399, 3)
        btnBrowseSource.Name = "btnBrowseSource"
        btnBrowseSource.Size = New Size(100, 23)
        btnBrowseSource.TabIndex = 1
        btnBrowseSource.Text = "Browse…"
        btnBrowseSource.UseVisualStyleBackColor = False
        ' 
        ' FlowLayoutPanel1
        ' 
        FlowLayoutPanel1.Controls.Add(rdoPreset)
        FlowLayoutPanel1.Controls.Add(rdoCustom)
        FlowLayoutPanel1.Controls.Add(Label1)
        FlowLayoutPanel1.Controls.Add(lnkAdvanced)
        FlowLayoutPanel1.Controls.Add(pnlAdvanced)
        FlowLayoutPanel1.Dock = DockStyle.Fill
        FlowLayoutPanel1.Location = New Point(15, 221)
        FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        FlowLayoutPanel1.Size = New Size(784, 40)
        FlowLayoutPanel1.TabIndex = 3
        ' 
        ' rdoPreset
        ' 
        rdoPreset.AutoSize = True
        rdoPreset.Checked = True
        rdoPreset.Location = New Point(3, 3)
        rdoPreset.Name = "rdoPreset"
        rdoPreset.Size = New Size(128, 19)
        rdoPreset.TabIndex = 0
        rdoPreset.TabStop = True
        rdoPreset.Text = "PS5 Recommended"
        rdoPreset.UseVisualStyleBackColor = True
        ' 
        ' rdoCustom
        ' 
        rdoCustom.AutoSize = True
        rdoCustom.Location = New Point(137, 3)
        rdoCustom.Name = "rdoCustom"
        rdoCustom.Size = New Size(112, 19)
        rdoCustom.TabIndex = 1
        rdoCustom.Text = "Custom Settings"
        rdoCustom.UseVisualStyleBackColor = True
        ' 
        ' Label1
        ' 
        Label1.Location = New Point(255, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(20, 15)
        Label1.TabIndex = 2
        Label1.Text = " "
        ' 
        ' lnkAdvanced
        ' 
        lnkAdvanced.AutoSize = True
        lnkAdvanced.Dock = DockStyle.Bottom
        lnkAdvanced.Font = New Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lnkAdvanced.LinkColor = Color.LightSkyBlue
        lnkAdvanced.Location = New Point(281, 124)
        lnkAdvanced.Name = "lnkAdvanced"
        lnkAdvanced.Size = New Size(106, 15)
        lnkAdvanced.TabIndex = 3
        lnkAdvanced.TabStop = True
        lnkAdvanced.Text = "Show Advanced ▼"
        ' 
        ' pnlAdvanced
        ' 
        pnlAdvanced.BackColor = Color.FromArgb(CByte(34), CByte(34), CByte(36))
        pnlAdvanced.Controls.Add(TableLayoutPanel3)
        pnlAdvanced.Location = New Point(393, 3)
        pnlAdvanced.Name = "pnlAdvanced"
        pnlAdvanced.Padding = New Padding(8)
        pnlAdvanced.Size = New Size(282, 133)
        pnlAdvanced.TabIndex = 4
        pnlAdvanced.Visible = False
        ' 
        ' TableLayoutPanel3
        ' 
        TableLayoutPanel3.AutoSize = True
        TableLayoutPanel3.ColumnCount = 2
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.Controls.Add(Label5, 0, 3)
        TableLayoutPanel3.Controls.Add(cmbOptimize, 1, 2)
        TableLayoutPanel3.Controls.Add(Label4, 0, 2)
        TableLayoutPanel3.Controls.Add(Label3, 0, 1)
        TableLayoutPanel3.Controls.Add(Label2, 0, 0)
        TableLayoutPanel3.Controls.Add(txtVolumeLabel, 1, 0)
        TableLayoutPanel3.Controls.Add(cmbBlockSize, 1, 1)
        TableLayoutPanel3.Controls.Add(txtExtraFlags, 1, 3)
        TableLayoutPanel3.Location = New Point(11, 11)
        TableLayoutPanel3.Name = "TableLayoutPanel3"
        TableLayoutPanel3.RowCount = 4
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.Size = New Size(254, 122)
        TableLayoutPanel3.TabIndex = 0
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(3, 102)
        Label5.Name = "Label5"
        Label5.Size = New Size(83, 15)
        Label5.TabIndex = 6
        Label5.Text = "Extra CLI Flags"
        ' 
        ' cmbOptimize
        ' 
        cmbOptimize.FormattingEnabled = True
        cmbOptimize.Items.AddRange(New Object() {"Speed", "Space", "Balanced"})
        cmbOptimize.Location = New Point(130, 85)
        cmbOptimize.Name = "cmbOptimize"
        cmbOptimize.Size = New Size(121, 23)
        cmbOptimize.TabIndex = 5
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(3, 82)
        Label4.Name = "Label4"
        Label4.Size = New Size(75, 15)
        Label4.TabIndex = 4
        Label4.Text = "Optimize For"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(3, 41)
        Label3.Name = "Label3"
        Label3.Size = New Size(59, 15)
        Label3.TabIndex = 2
        Label3.Text = "Block Size"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(3, 0)
        Label2.Name = "Label2"
        Label2.Size = New Size(78, 15)
        Label2.TabIndex = 0
        Label2.Text = "Volume Label"
        ' 
        ' txtVolumeLabel
        ' 
        txtVolumeLabel.Location = New Point(130, 3)
        txtVolumeLabel.Name = "txtVolumeLabel"
        txtVolumeLabel.Size = New Size(93, 23)
        txtVolumeLabel.TabIndex = 1
        ' 
        ' cmbBlockSize
        ' 
        cmbBlockSize.FormattingEnabled = True
        cmbBlockSize.Items.AddRange(New Object() {"Auto", "32768", "65536", "131072"})
        cmbBlockSize.Location = New Point(130, 44)
        cmbBlockSize.Name = "cmbBlockSize"
        cmbBlockSize.Size = New Size(121, 23)
        cmbBlockSize.TabIndex = 3
        ' 
        ' txtExtraFlags
        ' 
        txtExtraFlags.Location = New Point(130, 105)
        txtExtraFlags.Name = "txtExtraFlags"
        txtExtraFlags.PlaceholderText = "--flag value"
        txtExtraFlags.Size = New Size(100, 23)
        txtExtraFlags.TabIndex = 7
        ' 
        ' tlpExec
        ' 
        tlpExec.ColumnCount = 1
        tlpExec.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        tlpExec.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        tlpExec.Controls.Add(TableLayoutPanel4, 0, 2)
        tlpExec.Controls.Add(TableLayoutPanel5, 0, 0)
        tlpExec.Controls.Add(TableLayoutPanel6, 0, 1)
        tlpExec.Dock = DockStyle.Fill
        tlpExec.Location = New Point(15, 267)
        tlpExec.Name = "tlpExec"
        tlpExec.RowCount = 3
        tlpExec.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        tlpExec.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        tlpExec.RowStyles.Add(New RowStyle(SizeType.Absolute, 42F))
        tlpExec.Size = New Size(784, 278)
        tlpExec.TabIndex = 4
        ' 
        ' TableLayoutPanel4
        ' 
        TableLayoutPanel4.ColumnCount = 4
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150F))
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150F))
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150F))
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle())
        TableLayoutPanel4.Controls.Add(btnOpenOutput, 2, 0)
        TableLayoutPanel4.Controls.Add(btnStop, 1, 0)
        TableLayoutPanel4.Controls.Add(btnGenerate, 0, 0)
        TableLayoutPanel4.Dock = DockStyle.Fill
        TableLayoutPanel4.Location = New Point(3, 239)
        TableLayoutPanel4.Name = "TableLayoutPanel4"
        TableLayoutPanel4.RowCount = 1
        TableLayoutPanel4.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        TableLayoutPanel4.Size = New Size(778, 36)
        TableLayoutPanel4.TabIndex = 2
        ' 
        ' btnOpenOutput
        ' 
        btnOpenOutput.BackColor = SystemColors.Highlight
        btnOpenOutput.Dock = DockStyle.Fill
        btnOpenOutput.FlatStyle = FlatStyle.Flat
        btnOpenOutput.ForeColor = Color.White
        btnOpenOutput.Location = New Point(303, 3)
        btnOpenOutput.Name = "btnOpenOutput"
        btnOpenOutput.Size = New Size(144, 30)
        btnOpenOutput.TabIndex = 2
        btnOpenOutput.Text = "Open Folder"
        btnOpenOutput.UseVisualStyleBackColor = False
        ' 
        ' btnStop
        ' 
        btnStop.BackColor = Color.FromArgb(CByte(90), CByte(50), CByte(50))
        btnStop.Dock = DockStyle.Fill
        btnStop.Enabled = False
        btnStop.FlatStyle = FlatStyle.Flat
        btnStop.ForeColor = Color.White
        btnStop.Location = New Point(153, 3)
        btnStop.Name = "btnStop"
        btnStop.Size = New Size(144, 30)
        btnStop.TabIndex = 1
        btnStop.Text = "Stop"
        btnStop.UseVisualStyleBackColor = False
        ' 
        ' btnGenerate
        ' 
        btnGenerate.BackColor = SystemColors.Highlight
        btnGenerate.Dock = DockStyle.Fill
        btnGenerate.Enabled = False
        btnGenerate.FlatStyle = FlatStyle.Flat
        btnGenerate.ForeColor = Color.White
        btnGenerate.Location = New Point(3, 3)
        btnGenerate.Name = "btnGenerate"
        btnGenerate.Size = New Size(144, 30)
        btnGenerate.TabIndex = 0
        btnGenerate.Text = "Generate Image"
        btnGenerate.UseVisualStyleBackColor = False
        ' 
        ' TableLayoutPanel5
        ' 
        TableLayoutPanel5.ColumnCount = 3
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150F))
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150F))
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle())
        TableLayoutPanel5.Controls.Add(pbBuild, 0, 0)
        TableLayoutPanel5.Controls.Add(lblStatus, 1, 0)
        TableLayoutPanel5.Dock = DockStyle.Fill
        TableLayoutPanel5.Location = New Point(3, 3)
        TableLayoutPanel5.Name = "TableLayoutPanel5"
        TableLayoutPanel5.RowCount = 1
        TableLayoutPanel5.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        TableLayoutPanel5.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel5.Size = New Size(778, 22)
        TableLayoutPanel5.TabIndex = 3
        ' 
        ' pbBuild
        ' 
        pbBuild.Dock = DockStyle.Fill
        pbBuild.Location = New Point(3, 3)
        pbBuild.Name = "pbBuild"
        pbBuild.Size = New Size(144, 16)
        pbBuild.TabIndex = 0
        ' 
        ' lblStatus
        ' 
        lblStatus.AutoSize = True
        lblStatus.Dock = DockStyle.Left
        lblStatus.Location = New Point(153, 0)
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(39, 22)
        lblStatus.TabIndex = 1
        lblStatus.Text = "Ready"
        ' 
        ' TableLayoutPanel6
        ' 
        TableLayoutPanel6.ColumnCount = 1
        TableLayoutPanel6.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100F))
        TableLayoutPanel6.Controls.Add(rtbLog, 0, 1)
        TableLayoutPanel6.Controls.Add(txtCommandPreview, 0, 0)
        TableLayoutPanel6.Dock = DockStyle.Fill
        TableLayoutPanel6.Location = New Point(3, 31)
        TableLayoutPanel6.Name = "TableLayoutPanel6"
        TableLayoutPanel6.RowCount = 2
        TableLayoutPanel6.RowStyles.Add(New RowStyle(SizeType.Absolute, 25F))
        TableLayoutPanel6.RowStyles.Add(New RowStyle())
        TableLayoutPanel6.Size = New Size(778, 202)
        TableLayoutPanel6.TabIndex = 4
        ' 
        ' rtbLog
        ' 
        rtbLog.BackColor = Color.FromArgb(CByte(18), CByte(18), CByte(18))
        rtbLog.BorderStyle = BorderStyle.None
        rtbLog.Dock = DockStyle.Fill
        rtbLog.Font = New Font("Consolas", 9F)
        rtbLog.ForeColor = Color.Gainsboro
        rtbLog.Location = New Point(3, 28)
        rtbLog.Name = "rtbLog"
        rtbLog.ReadOnly = True
        rtbLog.Size = New Size(772, 171)
        rtbLog.TabIndex = 3
        rtbLog.Text = ""
        ' 
        ' txtCommandPreview
        ' 
        txtCommandPreview.BackColor = Color.FromArgb(CByte(40), CByte(40), CByte(42))
        txtCommandPreview.BorderStyle = BorderStyle.FixedSingle
        txtCommandPreview.Dock = DockStyle.Fill
        txtCommandPreview.Font = New Font("Consolas", 8.25F)
        txtCommandPreview.Location = New Point(3, 3)
        txtCommandPreview.Name = "txtCommandPreview"
        txtCommandPreview.ReadOnly = True
        txtCommandPreview.Size = New Size(772, 20)
        txtCommandPreview.TabIndex = 1
        ' 
        ' UcUfsImageBuilder
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(28), CByte(28), CByte(30))
        Controls.Add(tlpMain)
        ForeColor = Color.Gainsboro
        Name = "UcUfsImageBuilder"
        Size = New Size(814, 560)
        tlpMain.ResumeLayout(False)
        grpOutput.ResumeLayout(False)
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel2.PerformLayout()
        pnlHeader.ResumeLayout(False)
        pnlHeader.PerformLayout()
        grpSource.ResumeLayout(False)
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        FlowLayoutPanel1.ResumeLayout(False)
        FlowLayoutPanel1.PerformLayout()
        pnlAdvanced.ResumeLayout(False)
        pnlAdvanced.PerformLayout()
        TableLayoutPanel3.ResumeLayout(False)
        TableLayoutPanel3.PerformLayout()
        tlpExec.ResumeLayout(False)
        TableLayoutPanel4.ResumeLayout(False)
        TableLayoutPanel5.ResumeLayout(False)
        TableLayoutPanel5.PerformLayout()
        TableLayoutPanel6.ResumeLayout(False)
        TableLayoutPanel6.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents tlpMain As TableLayoutPanel
    Friend WithEvents pnlHeader As Panel
    Friend WithEvents lblTitle As Label
    Friend WithEvents lblSub As Label
    Friend WithEvents grpSource As GroupBox
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents txtSourceFolder As TextBox
    Friend WithEvents grpOutput As GroupBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents txtOutputImage As TextBox
    Friend WithEvents btnBrowseOutput As Button
    Friend WithEvents btnBrowseSource As Button
    Friend WithEvents FlowLayoutPanel1 As FlowLayoutPanel
    Friend WithEvents rdoPreset As RadioButton
    Friend WithEvents rdoCustom As RadioButton
    Friend WithEvents Label1 As Label
    Friend WithEvents lnkAdvanced As LinkLabel
    Friend WithEvents pnlAdvanced As Panel
    Friend WithEvents TableLayoutPanel3 As TableLayoutPanel
    Friend WithEvents Label5 As Label
    Friend WithEvents cmbOptimize As ComboBox
    Friend WithEvents Label4 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents txtVolumeLabel As TextBox
    Friend WithEvents cmbBlockSize As ComboBox
    Friend WithEvents txtExtraFlags As TextBox
    Friend WithEvents tlpExec As TableLayoutPanel
    Friend WithEvents TableLayoutPanel4 As TableLayoutPanel
    Friend WithEvents btnGenerate As Button
    Friend WithEvents btnOpenOutput As Button
    Friend WithEvents btnStop As Button
    Friend WithEvents TableLayoutPanel5 As TableLayoutPanel
    Friend WithEvents pbBuild As ProgressBar
    Friend WithEvents lblStatus As Label
    Friend WithEvents TableLayoutPanel6 As TableLayoutPanel
    Friend WithEvents txtCommandPreview As TextBox
    Friend WithEvents rtbLog As RichTextBox

End Class
