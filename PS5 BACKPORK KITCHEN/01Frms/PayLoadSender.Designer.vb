<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class PayLoadSender
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
        AirForm1 = New ReaLTaiizor.Forms.AirForm()
        BtnSend = New ReaLTaiizor.Controls.LostButton()
        BtnBrowse = New Button()
        TxtPayLoad = New ReaLTaiizor.Controls.DreamTextBox()
        CrownLabel2 = New ReaLTaiizor.Controls.CrownLabel()
        CrownLabel1 = New ReaLTaiizor.Controls.CrownLabel()
        TableLayoutPanel1 = New TableLayoutPanel()
        TxtPort = New ReaLTaiizor.Controls.SmallTextBox()
        Label1 = New Label()
        txtIP = New ReaLTaiizor.Controls.SmallTextBox()
        AirForm1.SuspendLayout()
        TableLayoutPanel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' AirForm1
        ' 
        AirForm1.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        AirForm1.BorderStyle = FormBorderStyle.None
        AirForm1.Controls.Add(BtnSend)
        AirForm1.Controls.Add(BtnBrowse)
        AirForm1.Controls.Add(TxtPayLoad)
        AirForm1.Controls.Add(CrownLabel2)
        AirForm1.Controls.Add(CrownLabel1)
        AirForm1.Controls.Add(TableLayoutPanel1)
        AirForm1.Customization = "AAAA/1paWv9ycnL/"
        AirForm1.Dock = DockStyle.Fill
        AirForm1.Font = New Font("Segoe UI", 9F)
        AirForm1.Image = Nothing
        AirForm1.Location = New Point(0, 0)
        AirForm1.MinimumSize = New Size(112, 35)
        AirForm1.Movable = True
        AirForm1.Name = "AirForm1"
        AirForm1.NoRounding = False
        AirForm1.Sizable = True
        AirForm1.Size = New Size(333, 222)
        AirForm1.SmartBounds = True
        AirForm1.StartPosition = FormStartPosition.CenterScreen
        AirForm1.TabIndex = 0
        AirForm1.TransparencyKey = Color.Fuchsia
        AirForm1.Transparent = False
        ' 
        ' BtnSend
        ' 
        BtnSend.BackColor = Color.FromArgb(CByte(0), CByte(122), CByte(204))
        BtnSend.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        BtnSend.ForeColor = Color.White
        BtnSend.HoverColor = Color.DodgerBlue
        BtnSend.Image = Nothing
        BtnSend.Location = New Point(26, 149)
        BtnSend.Name = "BtnSend"
        BtnSend.Size = New Size(120, 29)
        BtnSend.TabIndex = 8
        BtnSend.Text = "Send Payload"
        ' 
        ' BtnBrowse
        ' 
        BtnBrowse.BackColor = SystemColors.HotTrack
        BtnBrowse.FlatStyle = FlatStyle.Flat
        BtnBrowse.ForeColor = Color.White
        BtnBrowse.Location = New Point(214, 109)
        BtnBrowse.Name = "BtnBrowse"
        BtnBrowse.Size = New Size(84, 23)
        BtnBrowse.TabIndex = 7
        BtnBrowse.Text = ". . ."
        BtnBrowse.UseVisualStyleBackColor = False
        ' 
        ' TxtPayLoad
        ' 
        TxtPayLoad.BackColor = Color.FromArgb(CByte(41), CByte(41), CByte(41))
        TxtPayLoad.BorderStyle = BorderStyle.FixedSingle
        TxtPayLoad.ColorA = Color.FromArgb(CByte(31), CByte(31), CByte(31))
        TxtPayLoad.ColorB = Color.FromArgb(CByte(41), CByte(41), CByte(41))
        TxtPayLoad.ColorC = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        TxtPayLoad.ColorD = Color.FromArgb(CByte(0), CByte(0), CByte(0), CByte(0))
        TxtPayLoad.ColorE = Color.FromArgb(CByte(25), CByte(255), CByte(255), CByte(255))
        TxtPayLoad.ColorF = Color.Black
        TxtPayLoad.ForeColor = Color.FromArgb(CByte(40), CByte(218), CByte(255))
        TxtPayLoad.Location = New Point(23, 109)
        TxtPayLoad.Name = "TxtPayLoad"
        TxtPayLoad.ReadOnly = True
        TxtPayLoad.Size = New Size(182, 23)
        TxtPayLoad.TabIndex = 6
        ' 
        ' CrownLabel2
        ' 
        CrownLabel2.AutoSize = True
        CrownLabel2.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CrownLabel2.ForeColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        CrownLabel2.Location = New Point(211, 34)
        CrownLabel2.Name = "CrownLabel2"
        CrownLabel2.Size = New Size(42, 21)
        CrownLabel2.TabIndex = 5
        CrownLabel2.Text = "Port"
        ' 
        ' CrownLabel1
        ' 
        CrownLabel1.AutoSize = True
        CrownLabel1.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CrownLabel1.ForeColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        CrownLabel1.Location = New Point(23, 34)
        CrownLabel1.Name = "CrownLabel1"
        CrownLabel1.Size = New Size(48, 21)
        CrownLabel1.TabIndex = 4
        CrownLabel1.Text = "PS IP"
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.ColumnCount = 3
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 84.86487F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15.1351347F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 92F))
        TableLayoutPanel1.Controls.Add(TxtPort, 2, 0)
        TableLayoutPanel1.Controls.Add(Label1, 1, 0)
        TableLayoutPanel1.Controls.Add(txtIP, 0, 0)
        TableLayoutPanel1.Location = New Point(23, 58)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 1
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Size = New Size(278, 45)
        TableLayoutPanel1.TabIndex = 3
        ' 
        ' TxtPort
        ' 
        TxtPort.BackColor = Color.Transparent
        TxtPort.BorderColor = Color.FromArgb(CByte(180), CByte(180), CByte(180))
        TxtPort.CustomBGColor = Color.White
        TxtPort.Dock = DockStyle.Fill
        TxtPort.Font = New Font("Tahoma", 11F)
        TxtPort.ForeColor = Color.DimGray
        TxtPort.Location = New Point(188, 3)
        TxtPort.MaxLength = 32767
        TxtPort.Multiline = False
        TxtPort.Name = "TxtPort"
        TxtPort.ReadOnly = False
        TxtPort.Size = New Size(87, 28)
        TxtPort.SmoothingType = Drawing2D.SmoothingMode.AntiAlias
        TxtPort.TabIndex = 0
        TxtPort.Text = "9021"
        TxtPort.TextAlignment = HorizontalAlignment.Left
        TxtPort.UseSystemPasswordChar = False
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Dock = DockStyle.Fill
        Label1.Font = New Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label1.ForeColor = Color.FromArgb(CByte(0), CByte(122), CByte(204))
        Label1.Location = New Point(160, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(22, 45)
        Label1.TabIndex = 2
        Label1.Text = ":"
        Label1.TextAlign = ContentAlignment.TopCenter
        ' 
        ' txtIP
        ' 
        txtIP.BackColor = Color.Transparent
        txtIP.BorderColor = Color.FromArgb(CByte(180), CByte(180), CByte(180))
        txtIP.CustomBGColor = Color.White
        txtIP.Dock = DockStyle.Fill
        txtIP.Font = New Font("Tahoma", 11F)
        txtIP.ForeColor = Color.DimGray
        txtIP.Location = New Point(3, 3)
        txtIP.MaxLength = 32767
        txtIP.Multiline = False
        txtIP.Name = "txtIP"
        txtIP.ReadOnly = False
        txtIP.Size = New Size(151, 28)
        txtIP.SmoothingType = Drawing2D.SmoothingMode.AntiAlias
        txtIP.TabIndex = 1
        txtIP.Text = "192.168.29.78"
        txtIP.TextAlignment = HorizontalAlignment.Left
        txtIP.UseSystemPasswordChar = False
        ' 
        ' PayLoadSender
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(333, 222)
        Controls.Add(AirForm1)
        FormBorderStyle = FormBorderStyle.None
        Name = "PayLoadSender"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Send Payload"
        TransparencyKey = Color.Fuchsia
        AirForm1.ResumeLayout(False)
        AirForm1.PerformLayout()
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents AirForm1 As ReaLTaiizor.Forms.AirForm
    Friend WithEvents TxtPort As ReaLTaiizor.Controls.SmallTextBox
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents Label1 As Label
    Friend WithEvents txtIP As ReaLTaiizor.Controls.SmallTextBox
    Friend WithEvents CrownLabel2 As ReaLTaiizor.Controls.CrownLabel
    Friend WithEvents CrownLabel1 As ReaLTaiizor.Controls.CrownLabel
    Friend WithEvents TxtPayLoad As ReaLTaiizor.Controls.DreamTextBox
    Friend WithEvents BtnBrowse As Button
    Friend WithEvents BtnSend As ReaLTaiizor.Controls.LostButton
End Class
