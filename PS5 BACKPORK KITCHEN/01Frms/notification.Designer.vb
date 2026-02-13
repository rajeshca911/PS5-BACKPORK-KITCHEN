<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class notification
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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
        NightForm1 = New ReaLTaiizor.Forms.NightForm()
        HopePictureBox1 = New ReaLTaiizor.Controls.HopePictureBox()
        RichTextBox1 = New RichTextBox()
        Panel1 = New Panel()
        lblheading = New ReaLTaiizor.Controls.HeaderLabel()
        CrownDockPanel1 = New ReaLTaiizor.Docking.Crown.CrownDockPanel()
        HopePictureBox2 = New ReaLTaiizor.Controls.HopePictureBox()
        NightForm1.SuspendLayout()
        CType(HopePictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        Panel1.SuspendLayout()
        CType(HopePictureBox2, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' NightForm1
        ' 
        NightForm1.BackColor = Color.FromArgb(CByte(40), CByte(48), CByte(51))
        NightForm1.Controls.Add(HopePictureBox2)
        NightForm1.Controls.Add(HopePictureBox1)
        NightForm1.Controls.Add(RichTextBox1)
        NightForm1.Controls.Add(Panel1)
        NightForm1.Controls.Add(CrownDockPanel1)
        NightForm1.Dock = DockStyle.Fill
        NightForm1.DrawIcon = False
        NightForm1.Font = New Font("Segoe UI", 9F)
        NightForm1.HeadColor = Color.FromArgb(CByte(50), CByte(58), CByte(61))
        NightForm1.Location = New Point(0, 0)
        NightForm1.MinimumSize = New Size(100, 42)
        NightForm1.Name = "NightForm1"
        NightForm1.Padding = New Padding(0, 31, 0, 0)
        NightForm1.Size = New Size(760, 462)
        NightForm1.TabIndex = 0
        NightForm1.Text = "Message"
        NightForm1.TextAlignment = ReaLTaiizor.Forms.NightForm.Alignment.Left
        NightForm1.TitleBarTextColor = Color.Gainsboro
        ' 
        ' HopePictureBox1
        ' 
        HopePictureBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        HopePictureBox1.BackColor = Color.FromArgb(CByte(192), CByte(196), CByte(204))
        HopePictureBox1.Image = My.Resources.Resources.RED
        HopePictureBox1.Location = New Point(727, 2)
        HopePictureBox1.Name = "HopePictureBox1"
        HopePictureBox1.PixelOffsetType = Drawing2D.PixelOffsetMode.HighQuality
        HopePictureBox1.Size = New Size(30, 25)
        HopePictureBox1.SizeMode = PictureBoxSizeMode.Zoom
        HopePictureBox1.SmoothingType = Drawing2D.SmoothingMode.HighQuality
        HopePictureBox1.TabIndex = 5
        HopePictureBox1.TabStop = False
        HopePictureBox1.TextRenderingType = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        ' 
        ' RichTextBox1
        ' 
        RichTextBox1.BackColor = Color.FromArgb(CByte(40), CByte(48), CByte(51))
        RichTextBox1.BorderStyle = BorderStyle.None
        RichTextBox1.Dock = DockStyle.Fill
        RichTextBox1.Font = New Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        RichTextBox1.ForeColor = Color.White
        RichTextBox1.Location = New Point(0, 75)
        RichTextBox1.Name = "RichTextBox1"
        RichTextBox1.ReadOnly = True
        RichTextBox1.Size = New Size(760, 331)
        RichTextBox1.TabIndex = 3
        RichTextBox1.Text = "FakeLibs must be placed manually inside the tool folder using the firmware number as the folder name."
        ' 
        ' Panel1
        ' 
        Panel1.Controls.Add(lblheading)
        Panel1.Dock = DockStyle.Top
        Panel1.Location = New Point(0, 31)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(760, 44)
        Panel1.TabIndex = 4
        ' 
        ' lblheading
        ' 
        lblheading.AutoSize = True
        lblheading.BackColor = Color.FromArgb(CByte(40), CByte(48), CByte(51))
        lblheading.Font = New Font("Microsoft Sans Serif", 11F, FontStyle.Bold)
        lblheading.ForeColor = Color.FromArgb(CByte(255), CByte(255), CByte(255))
        lblheading.Location = New Point(12, 13)
        lblheading.Name = "lblheading"
        lblheading.Size = New Size(111, 18)
        lblheading.TabIndex = 0
        lblheading.Text = "HeaderLabel1"
        ' 
        ' CrownDockPanel1
        ' 
        CrownDockPanel1.BackColor = Color.FromArgb(CByte(60), CByte(63), CByte(65))
        CrownDockPanel1.Dock = DockStyle.Bottom
        CrownDockPanel1.Location = New Point(0, 406)
        CrownDockPanel1.Name = "CrownDockPanel1"
        CrownDockPanel1.Size = New Size(760, 56)
        CrownDockPanel1.TabIndex = 1
        ' 
        ' HopePictureBox2
        ' 
        HopePictureBox2.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        HopePictureBox2.BackColor = Color.FromArgb(CByte(192), CByte(196), CByte(204))
        HopePictureBox2.Image = My.Resources.Resources.green
        HopePictureBox2.Location = New Point(699, 2)
        HopePictureBox2.Name = "HopePictureBox2"
        HopePictureBox2.PixelOffsetType = Drawing2D.PixelOffsetMode.HighQuality
        HopePictureBox2.Size = New Size(24, 23)
        HopePictureBox2.SizeMode = PictureBoxSizeMode.StretchImage
        HopePictureBox2.SmoothingType = Drawing2D.SmoothingMode.HighQuality
        HopePictureBox2.TabIndex = 6
        HopePictureBox2.TabStop = False
        HopePictureBox2.TextRenderingType = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        ' 
        ' notification
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(760, 462)
        Controls.Add(NightForm1)
        FormBorderStyle = FormBorderStyle.None
        MaximumSize = New Size(1920, 1032)
        MinimumSize = New Size(190, 40)
        Name = "notification"
        StartPosition = FormStartPosition.CenterScreen
        Text = "DungeonForm1"
        TransparencyKey = Color.Fuchsia
        NightForm1.ResumeLayout(False)
        CType(HopePictureBox1, ComponentModel.ISupportInitialize).EndInit()
        Panel1.ResumeLayout(False)
        Panel1.PerformLayout()
        CType(HopePictureBox2, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents NightForm1 As ReaLTaiizor.Forms.NightForm
    Friend WithEvents lblheading As ReaLTaiizor.Controls.HeaderLabel
    Friend WithEvents RichTextBox1 As RichTextBox
    Friend WithEvents CrownDockPanel1 As ReaLTaiizor.Docking.Crown.CrownDockPanel
    Friend WithEvents Panel1 As Panel
    Friend WithEvents HopePictureBox1 As ReaLTaiizor.Controls.HopePictureBox
    Friend WithEvents HopePictureBox2 As ReaLTaiizor.Controls.HopePictureBox
End Class
